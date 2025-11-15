using System.Security.Claims;
using CCD.Api.Dtos;
using CCD.Core.Dtos;
using CCD.Core.Interfaces;
using CCD.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CreatePreferenceRequestDto = CCD.Core.Dtos.CreatePreferenceRequestDto;
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

    [HttpPost("confirm")]
    public async Task<IActionResult> ConfirmPayment(ConfirmPaymentRequestDto request)
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out _))
        {
            return Unauthorized(new { message = "Token de usuario inválido." });
        }

        try
        {
            await _paymentService.ProcessPaymentNotificationAsync(request.PaymentId);
            return Ok(new { message = "Pago confirmado. Tu plan se actualizará si el pago está aprobado." });
        }
        catch (Exception)
        {
            return StatusCode(500, new { message = "No fue posible confirmar el pago. Intenta nuevamente o contacta a soporte." });
        }
    }
}