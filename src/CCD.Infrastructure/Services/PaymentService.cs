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
        _logger.LogInformation("🛒 Creando preferencia de pago para usuario {UserId} y plan {PlanId}", userId, planId);
        
        var plan = await _context.Plans.FindAsync(planId);
        if (plan == null)
        {
            _logger.LogWarning("❌ Plan {PlanId} no encontrado", planId);
            return null;
        }
        
        if (plan.Price <= 0)
        {
            _logger.LogWarning("❌ El plan {PlanName} tiene precio 0 o negativo", plan.Name);
            return null;
        }

        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            _logger.LogWarning("❌ Usuario {UserId} no encontrado", userId);
            return null;
        }

        _logger.LogInformation("✅ Plan encontrado: {PlanName} - Precio: ${Price} COP", plan.Name, plan.Price);
        _logger.LogInformation("✅ Usuario encontrado: {Email}", user.Email);

        var request = new PreferenceRequest
        {
            Items = new List<PreferenceItemRequest>
            {
                new() {
                    Title = $"Plan {plan.Name} - CrudCloudDb",
                    Description = $"Suscripción al plan {plan.Name}",
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
                ["plan_id"] = plan.Id.ToString() // ⚠️ IMPORTANTE: Guardar como string
            }
        };
        
        // 📋 LOG DE DEBUG: Mostrar la estructura completa que se enviará a Mercado Pago
        var metadataJson = System.Text.Json.JsonSerializer.Serialize(request.Metadata, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
        _logger.LogWarning("📦 Metadatos a enviar a Mercado Pago:\n{Metadata}", metadataJson);
        _logger.LogInformation("🔗 ExternalReference: {ExternalReference}", request.ExternalReference);
        _logger.LogInformation("📧 Email del pagador: {Email}", user.Email);
        
        var client = new PreferenceClient();
        Preference preference = await client.CreateAsync(request);
        
        _logger.LogInformation("✅ Preferencia creada exitosamente con ID: {PreferenceId}", preference.Id);
        _logger.LogInformation("🌐 Init Point: {InitPoint}", preference.InitPoint);
        
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
            _logger.LogInformation("🔔 Notificación recibida para pago ID: {PaymentId}", paymentId);
            
            // ⚠️ IMPORTANTE: Dar tiempo a Mercado Pago para procesar el pago antes de consultarlo
            _logger.LogInformation("⏳ Esperando 3 segundos antes de consultar el pago...");
            await Task.Delay(3000);
            
            var paymentClient = new PaymentClient();
            var payment = await paymentClient.GetAsync(paymentId);

            if (payment == null)
            {
                _logger.LogWarning("❌ No se encontró el pago con ID {PaymentId} en Mercado Pago.", paymentId);
                return;
            }

            _logger.LogInformation("📊 Estado del pago {PaymentId}: {Status}", paymentId, payment.Status);

            if (payment.Status == "approved")
            {
                _logger.LogInformation("✅ Pago {PaymentId} APROBADO. Procesando actualización de plan...", paymentId);

                // Validar ExternalReference (userId)
                if (string.IsNullOrEmpty(payment.ExternalReference))
                {
                    _logger.LogError("❌ El pago {PaymentId} no tiene ExternalReference.", paymentId);
                    return;
                }

                if (!Guid.TryParse(payment.ExternalReference, out var userId))
                {
                    _logger.LogError("❌ El ExternalReference '{ExternalReference}' del pago {PaymentId} no es un GUID válido.", payment.ExternalReference, paymentId);
                    return;
                }
                
                _logger.LogInformation("👤 Usuario identificado: {UserId}", userId);
                
                // Extraer y validar metadatos
                var metadata = payment.Metadata;
                _logger.LogInformation("📦 Metadatos del pago: {Metadata}", 
                    metadata != null ? System.Text.Json.JsonSerializer.Serialize(metadata) : "null");
                
                if (metadata == null || !metadata.ContainsKey("plan_id"))
                {
                    _logger.LogError("❌ Los metadatos del pago {PaymentId} no contienen 'plan_id'.", paymentId);
                    return;
                }

                var planIdValue = metadata["plan_id"]?.ToString();
                _logger.LogInformation("📋 Plan ID extraído de metadatos: {PlanId}", planIdValue);
                
                if (string.IsNullOrEmpty(planIdValue) || !int.TryParse(planIdValue, out var planId))
                {
                    _logger.LogError("❌ El 'plan_id' en metadatos no es válido: {PlanIdValue}", planIdValue);
                    return;
                }

                // Buscar usuario
                var user = await _context.Users
                    .Include(u => u.Plan)
                    .FirstOrDefaultAsync(u => u.Id == userId);
                    
                if (user == null)
                {
                    _logger.LogWarning("❌ Usuario {UserId} no encontrado en la base de datos.", userId);
                    return;
                }
                
                _logger.LogInformation("✅ Usuario encontrado: {Email}, Plan actual: {CurrentPlan}", user.Email, user.Plan?.Name ?? "Sin plan");
                
                // Buscar nuevo plan
                var newPlan = await _context.Plans.FindAsync(planId);
                if (newPlan == null)
                {
                    _logger.LogError("❌ El plan {PlanId} no existe en la base de datos.", planId);
                    return;
                }
                
                _logger.LogInformation("📋 Nuevo plan encontrado: {PlanName} (ID: {PlanId})", newPlan.Name, newPlan.Id);
                
                // Guardar información del plan anterior
                var oldPlanId = user.PlanId;
                var oldPlan = await _context.Plans.FindAsync(oldPlanId);
                var oldPlanName = oldPlan?.Name ?? "Desconocido";
                
                // Actualizar plan del usuario
                user.PlanId = planId;
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("🎉 El usuario {UserId} ({Email}) ha sido actualizado del plan '{OldPlan}' al plan '{NewPlan}' exitosamente.", 
                    userId, user.Email, oldPlanName, newPlan.Name);
                
                // Enviar correo de notificación de cambio de plan
                try
                {
                    await _emailService.SendPlanChangeEmailAsync(
                        user.Email,
                        user.UserName,
                        oldPlanName,
                        newPlan.Name,
                        newPlan.Price);
                    
                    _logger.LogInformation("📧 Correo de cambio de plan enviado a {Email}", user.Email);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "⚠️ Error al enviar correo de cambio de plan a {Email}", user.Email);
                    // No fallar el cambio de plan si el correo no se puede enviar
                }
            }
            else
            {
                _logger.LogInformation("⏸️ El estado del pago {PaymentId} es '{Status}'. No se requiere acción.", paymentId, payment.Status);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "💥 Error crítico al procesar la notificación del pago ID {PaymentId}.", paymentId);
            throw; // Re-lanzar para que Mercado Pago reintente
        }
    }
}