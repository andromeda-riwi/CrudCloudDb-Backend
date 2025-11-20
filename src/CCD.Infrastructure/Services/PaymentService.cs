using CCD.Core.Dtos;
using CCD.Core.Interfaces;
using CCD.Infrastructure.Data;
using MercadoPago.Client.Payment;
using MercadoPago.Client.Preference;
using MercadoPago.Resource.Preference;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace CCD.Infrastructure.Services;

public class PaymentService : IPaymentService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<PaymentService> _logger;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;
    
    public PaymentService(
        ApplicationDbContext context, 
        ILogger<PaymentService> logger, 
        IEmailService emailService,
        IConfiguration configuration)
    {
        _context = context;
        _logger = logger;
        _emailService = emailService;
        _configuration = configuration;
    }
    
    public async Task<CreatePreferenceResponseDto?> CreatePreferenceAsync(int planId, Guid userId)
    {
        var plan = await _context.Plans.FindAsync(planId);
        if (plan == null || plan.Price <= 0) return null;

        var user = await _context.Users.FindAsync(userId);
        if (user == null) return null;

        var dashboardUrl = _configuration["App:DashboardUrl"] ?? "https://andromeda.andrescortes.dev";
        
        _logger.LogInformation("📦 Creando preferencia de pago para usuario {UserId} - Plan {PlanId} ({PlanName})", userId, planId, plan.Name);
        _logger.LogInformation("🔗 Dashboard URL configurada: {DashboardUrl}", dashboardUrl);

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
                Success = $"{dashboardUrl}/dashboard?payment_status=success",
                Failure = $"{dashboardUrl}/dashboard?payment_status=failure",
                Pending = $"{dashboardUrl}/dashboard?payment_status=pending"
            },
            AutoReturn = "approved",
            ExternalReference = user.Id.ToString(),
            Metadata = new Dictionary<string, object>
            {
                ["user_id"] = user.Id.ToString(),
                ["plan_id"] = plan.Id
            }
        };
        
        _logger.LogInformation("✅ Preferencia creada - External Reference: {ExternalReference}, Metadata: user_id={UserId}, plan_id={PlanId}", 
            user.Id, user.Id, plan.Id);
        
        var client = new PreferenceClient();
        Preference preference = await client.CreateAsync(request);
        
        _logger.LogInformation("💳 Preferencia de MercadoPago creada exitosamente - ID: {PreferenceId}", preference.Id);
        
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
            _logger.LogInformation("🔍 Iniciando procesamiento de notificación para pago ID: {PaymentId}", paymentId);
            
            var paymentClient = new PaymentClient();
            var payment = await paymentClient.GetAsync(paymentId);

            if (payment == null)
            {
                _logger.LogWarning("❌ No se encontró el pago con ID {PaymentId} en Mercado Pago.", paymentId);
                return;
            }

            _logger.LogInformation("📊 Estado del pago {PaymentId}: {Status}", paymentId, payment.Status);
            _logger.LogInformation("📋 ExternalReference: {ExternalReference}", payment.ExternalReference);
            _logger.LogInformation("📋 Metadata: {Metadata}", payment.Metadata != null ? string.Join(", ", payment.Metadata.Select(kvp => $"{kvp.Key}={kvp.Value}")) : "null");

            if (payment.Status == "approved")
            {
                _logger.LogInformation("✅ Pago {PaymentId} APROBADO. Procesando actualización de plan...", paymentId);

                if (string.IsNullOrEmpty(payment.ExternalReference))
                {
                    _logger.LogError("❌ El ExternalReference del pago {PaymentId} está vacío.", paymentId);
                    return;
                }

                if (!Guid.TryParse(payment.ExternalReference, out var userId))
                {
                    _logger.LogError("❌ El ExternalReference '{ExternalReference}' del pago {PaymentId} no es un GUID válido.", payment.ExternalReference, paymentId);
                    return;
                }
                
                _logger.LogInformation("👤 Usuario ID extraído: {UserId}", userId);
                
                var metadata = payment.Metadata;
                if (metadata == null || !metadata.ContainsKey("plan_id"))
                {
                    _logger.LogError("❌ Los metadatos del pago {PaymentId} no contienen 'plan_id'. Metadata: {Metadata}", 
                        paymentId, metadata != null ? string.Join(", ", metadata.Select(kvp => $"{kvp.Key}={kvp.Value}")) : "null");
                    return;
                }

                if (!int.TryParse(metadata["plan_id"].ToString(), out var planId))
                {
                    _logger.LogError("❌ El plan_id '{PlanId}' en metadata no es un entero válido.", metadata["plan_id"]);
                    return;
                }

                _logger.LogInformation("📦 Plan ID extraído: {PlanId}", planId);

                var user = await _context.Users
                    .Include(u => u.Plan)
                    .FirstOrDefaultAsync(u => u.Id == userId);
                    
                if (user == null)
                {
                    _logger.LogWarning("❌ Se recibió un pago aprobado para un usuario no existente: {UserId}", userId);
                    return;
                }

                _logger.LogInformation("👤 Usuario encontrado: {UserName} (Email: {Email})", user.UserName, user.Email);
                
                var oldPlanId = user.PlanId;
                var oldPlan = await _context.Plans.FindAsync(oldPlanId);
                var oldPlanName = oldPlan?.Name ?? "Desconocido";
                
                _logger.LogInformation("📊 Plan actual del usuario: {OldPlanId} ({OldPlanName})", oldPlanId, oldPlanName);
                
                var newPlan = await _context.Plans.FindAsync(planId);
                if (newPlan == null)
                {
                    _logger.LogError("❌ El plan {PlanId} no existe en la base de datos.", planId);
                    return;
                }
                
                _logger.LogInformation("📦 Nuevo plan encontrado: {NewPlanName} (ID: {PlanId})", newPlan.Name, planId);
                
                // Actualizar el plan del usuario
                user.PlanId = planId;
                
                _logger.LogInformation("💾 Guardando cambio de plan para el usuario {UserId}...", userId);
                
                var saveResult = await _context.SaveChangesAsync();
                
                _logger.LogInformation("✅ Plan actualizado exitosamente. Cambios guardados: {SaveResult}. Usuario {UserId} actualizado de plan {OldPlanName} a {NewPlanName}.", 
                    saveResult, userId, oldPlanName, newPlan.Name);
                
                // Enviar correo de notificación de cambio de plan
                try
                {
                    _logger.LogInformation("📧 Enviando correo de confirmación de cambio de plan a {Email}...", user.Email);
                    
                    await _emailService.SendPlanChangeEmailAsync(
                        user.Email,
                        user.UserName,
                        oldPlanName,
                        newPlan.Name,
                        newPlan.Price);
                    
                    _logger.LogInformation("✅ Correo de cambio de plan enviado exitosamente a {Email}", user.Email);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "⚠️ Error al enviar correo de cambio de plan (el cambio de plan fue exitoso)");
                    // No fallar el cambio de plan si el correo no se puede enviar
                }
            }
            else
            {
                _logger.LogInformation("ℹ️ El estado del pago {PaymentId} es '{Status}'. No se requiere acción.", paymentId, payment.Status);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error crítico al procesar la notificación del pago ID {PaymentId}.", paymentId);
            throw; // Re-lanzar para que se registre en logs de nivel superior
        }
    }
}