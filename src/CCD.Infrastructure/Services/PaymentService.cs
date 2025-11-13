using CCD.Core.Dtos;
using CCD.Core.Interfaces;
using CCD.Infrastructure.Data;
using MercadoPago.Client.Payment;
using MercadoPago.Client.Preference;
using MercadoPago.Resource.Preference;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging; 

namespace CCD.Infrastructure.Services;

public class PaymentService : IPaymentService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<PaymentService> _logger;
    private readonly IEmailService _emailService;
    
    public PaymentService(ApplicationDbContext context, ILogger<PaymentService> logger, IEmailService emailService)
    {
        _context = context;
        _logger = logger;
        _emailService = emailService;
    }
    
    public async Task<CreatePreferenceResponseDto?> CreatePreferenceAsync(int planId, Guid userId)
    {
        var plan = await _context.Plans.FindAsync(planId);
        if (plan == null || plan.Price <= 0) return null;

        var user = await _context.Users.FindAsync(userId);
        if (user == null) return null;

        var request = new PreferenceRequest
        {
            Items = new List<PreferenceItemRequest>
            {
                new() {
                    Title = $"Plan {plan.Name} - CrudCloudDb",
                    Description = "Suscripción mensual al plan " + plan.Name,
                    Quantity = 1,
                    CurrencyId = "COP",
                    UnitPrice = plan.Price,
                },
            },
            Payer = new PreferencePayerRequest
            {
                Email = user.Email,
                Name = user.Name,
                Surname = user.LastName
            },
            BackUrls = new PreferenceBackUrlsRequest
            {
                Success = "https://andromeda.andrescortes.dev/dashboard?payment_status=success",
                Failure = "https://andromeda.andrescortes.dev/dashboard?payment_status=failure",
                Pending = "https://andromeda.andrescortes.dev/dashboard?payment_status=pending"
            },
            AutoReturn = "approved",
            ExternalReference = user.Id.ToString(),
            Metadata = new Dictionary<string, object>
            {
                ["user_id"] = user.Id.ToString(),
                ["plan_id"] = plan.Id
            }
        };
        
        var client = new PreferenceClient();
        Preference preference = await client.CreateAsync(request);
        return new CreatePreferenceResponseDto
        {
            PreferenceId = preference.Id,
            InitPoint = preference.InitPoint,
        };
    }
    
    public async Task ProcessPaymentNotificationAsync(long paymentId)
    {
        try
        {
            var paymentClient = new PaymentClient();
            var payment = await paymentClient.GetAsync(paymentId);

            if (payment == null)
            {
                _logger.LogWarning("No se encontró el pago con ID {PaymentId} en Mercado Pago.", paymentId);
                return;
            }

            if (payment.Status == "approved")
            {
                _logger.LogInformation("Pago {PaymentId} APROBADO.", paymentId);

                if (!Guid.TryParse(payment.ExternalReference, out var userId))
                {
                    _logger.LogError("El ExternalReference '{ExternalReference}' del pago {PaymentId} no es un GUID válido.", payment.ExternalReference, paymentId);
                    return;
                }
                
                var metadata = payment.Metadata;
                if (metadata == null || !metadata.ContainsKey("plan_id") || !int.TryParse(metadata["plan_id"].ToString(), out var planId))
                {
                    _logger.LogError("Los metadatos del pago {PaymentId} no contienen un 'plan_id' válido.", paymentId);
                    return;
                }

                var user = await _context.Users
                    .Include(u => u.Plan)
                    .FirstOrDefaultAsync(u => u.Id == userId);
                    
                if (user != null)
                {
                    var oldPlanId = user.PlanId;
                    var oldPlan = await _context.Plans.FindAsync(oldPlanId);
                    var oldPlanName = oldPlan?.Name ?? "Desconocido";
                    
                    var newPlan = await _context.Plans.FindAsync(planId);
                    if (newPlan == null)
                    {
                        _logger.LogError("El plan {PlanId} no existe.", planId);
                        return;
                    }
                    
                    user.PlanId = planId;
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("El usuario {UserId} ha sido actualizado al plan {PlanId} exitosamente.", userId, planId);
                    
                    // Enviar correo de notificación de cambio de plan
                    try
                    {
                        await _emailService.SendPlanChangeEmailAsync(
                            user.Email,
                            user.UserName,
                            oldPlanName,
                            newPlan.Name,
                            newPlan.Price);
                        
                        _logger.LogInformation($"Correo de cambio de plan enviado a {user.Email}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error al enviar correo de cambio de plan");
                        // No fallar el cambio de plan si el correo no se puede enviar
                    }
                }
                else
                {
                    _logger.LogWarning("Se recibió un pago aprobado para un usuario no existente: {UserId}", userId);
                }
            }
            else
            {
                _logger.LogInformation("El estado del pago {PaymentId} es '{Status}'. No se requiere acción.", paymentId, payment.Status);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al procesar la notificación del pago ID {PaymentId}.", paymentId);
        }
    }
}