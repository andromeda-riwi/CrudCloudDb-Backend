namespace CCD.Core.Interfaces;

public interface IAuditService
{
    Task LogAsync(string action, string entityType, string? entityId, Guid? userId, string? details, string ipAddress);
    Task<List<CCD.Core.AuditLog>> GetLogsAsync(Guid? userId = null, int skip = 0, int take = 50);
}

