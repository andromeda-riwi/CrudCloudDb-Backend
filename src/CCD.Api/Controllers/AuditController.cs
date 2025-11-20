using System.Security.Claims;
using CCD.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CCD.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class AuditController : ControllerBase
{
    private readonly IAuditService _auditService;

    public AuditController(IAuditService auditService)
    {
        _auditService = auditService;
    }

    /// <summary>
    /// Obtiene los logs de auditoría del usuario actual
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetMyLogs([FromQuery] int skip = 0, [FromQuery] int take = 50)
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
        {
            return Unauthorized();
        }

        var logs = await _auditService.GetLogsAsync(userId, skip, take);
        return Ok(logs);
    }

    /// <summary>
    /// Obtiene todos los logs de auditoría (solo para administradores en el futuro)
    /// </summary>
    [HttpGet("all")]
    public async Task<IActionResult> GetAllLogs([FromQuery] int skip = 0, [FromQuery] int take = 50)
    {
        var logs = await _auditService.GetLogsAsync(null, skip, take);
        return Ok(logs);
    }
}

