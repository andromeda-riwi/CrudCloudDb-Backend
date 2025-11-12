using CCD.Core.Dtos;

namespace CCD.Core.Interfaces;

public interface IDatabaseProvisioner
{
    // Métodos principales
    Task<DatabaseConnectionDetails?> CreateDatabaseAsync(string engine, Guid userId);
    Task<bool> DeleteDatabaseAsync(string engine, string databaseName, string username);
    Task<DatabaseConnectionDetails?> GetDatabaseCredentialsAsync(string engine, string databaseName, string username);
    
    // Métodos específicos para cada motor de base de datos
    Task<DatabaseConnectionDetails> CreatePostgreSqlDatabaseAsync(Guid userId);
    Task<DatabaseConnectionDetails> CreateMySqlDatabaseAsync(Guid userId);
    Task<DatabaseConnectionDetails> CreateSqlServerDatabaseAsync(Guid userId);
    
    Task<bool> DeletePostgreSqlDatabaseAsync(string databaseName, string username);
    Task<bool> DeleteMySqlDatabaseAsync(string databaseName, string username);
    Task<bool> DeleteSqlServerDatabaseAsync(string databaseName, string username);
    
    string GenerateSecurePassword();
}