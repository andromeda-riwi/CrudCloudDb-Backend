using System.Security.Claims;
using CCD.Core.Dtos;
using CCD.Core.Interfaces;
using CCD.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CCD.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(
        IPaymentService paymentService,
        ApplicationDbContext context,
        ILogger<PaymentsController> logger)
    {
        _paymentService = paymentService;
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Crear preferencia de pago en Mercado Pago
    /// POST /api/payments/preference
    /// </summary>
    [HttpPost("preference")]
    public async Task<IActionResult> CreatePreference(CreatePreferenceRequestDto requestDto)
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out var userId))
        {
            return Unauthorized(new { message = "Token de usuario inválido." });
        }

        if (requestDto.PlanId == 1)
        {
            return BadRequest(new { message = "No se puede generar una preferencia de pago para el plan gratuito." });
        }
        
        try
        {
            var response = await _paymentService.CreatePreferenceAsync(requestDto.PlanId, userId);

            if (response == null)
            {
                return BadRequest(new { message = $"El plan con ID {requestDto.PlanId} no es válido o no se encontró." });
            }

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear preferencia de pago");
            return StatusCode(500, new { message = "Ocurrió un error al comunicarse con el servicio de pagos." });
        }
    }

    /// <summary>
    /// Obtener historial de pagos del usuario
    /// GET /api/payments/history
    /// </summary>
    [HttpGet("history")]
    public async Task<IActionResult> GetPaymentHistory()
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out var userId))
        {
            return Unauthorized(new { message = "Token de usuario inválido." });
        }

        try
        {
            // Obtener el usuario con su información de planes
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                return NotFound("Usuario no encontrado.");
            }

            // TODO: Implementar tabla de PaymentHistory en la BD
            // Por ahora, retornamos un historial vacío
            // En el futuro, esto debería traer datos de una tabla de pagos/transacciones

            var paymentHistory = new List<PaymentHistoryDto>();

            return Ok(new
            {
                totalPayments = paymentHistory.Count,
                payments = paymentHistory
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener historial de pagos");
            return StatusCode(500, new { message = "Error al obtener el historial de pagos." });
        }
    }

    /// <summary>
    /// Obtener planes disponibles
    /// GET /api/payments/plans
    /// </summary>
    [HttpGet("plans")]
    [AllowAnonymous]
    public async Task<IActionResult> GetAvailablePlans()
    {
        try
        {
            var plans = await _context.Plans
                .Select(p => new
                {
                    p.Id,
                    p.Name,
                    p.Price,
                    p.DatabaseLimitPerEngine
                })
                .ToListAsync();

            return Ok(plans);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener planes");
            return StatusCode(500, new { message = "Error al obtener los planes." });
        }
    }
}