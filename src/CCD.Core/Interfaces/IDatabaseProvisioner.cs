using CCD.Core.Dtos; // Esta línea es crucial

namespace CCD.Core.Interfaces;

public interface IDatabaseProvisioner
{
    Task<DatabaseConnectionDetails?> CreateDatabaseAsync(string engine, Guid userId);
    Task<bool> DeleteDatabaseAsync(string engine, string databaseName, string username);
}