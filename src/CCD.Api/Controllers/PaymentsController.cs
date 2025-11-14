using System.Security.Claims;
using CCD.Api.Dtos;
using CCD.Core.Dtos;
using CCD.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CreatePreferenceRequestDto = CCD.Core.Dtos.CreatePreferenceRequestDto;

namespace CCD.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _paymentService;

    public PaymentsController(IPaymentService paymentService)
    {
        _paymentService = paymentService;
    }

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
        catch (Exception)
        {
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