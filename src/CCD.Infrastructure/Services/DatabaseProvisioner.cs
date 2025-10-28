using CCD.Core.Dtos;
using CCD.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace CCD.Infrastructure.Services;

public class DatabaseProvisioner : IDatabaseProvisioner
{
    private readonly IConfiguration _config;

    public DatabaseProvisioner(IConfiguration config)
    {
        _config = config;
    }

    public async Task<DatabaseConnectionDetails?> CreateDatabaseAsync(string engine, Guid userId)
    {
        if (engine.ToLower() != "postgresql")
        {
            // Por ahora, solo soportamos PostgreSQL
            throw new NotImplementedException("El motor de base de datos no es soportado.");
        }

        // 1. Obtener la cadena de conexión del SUPERUSUARIO desde appsettings.json
        var adminConnectionString = _config.GetConnectionString("AdminPostgresConnection");

        // 2. Generar credenciales seguras y aleatorias
        var dbName = $"user_{userId.ToString().Substring(0, 8)}_{Guid.NewGuid().ToString().Substring(0, 4)}";
        var dbUser = $"user_{Guid.NewGuid().ToString("N").Substring(0, 12)}";
        var dbPassword = GenerateSecurePassword();

        // 3. Conectarse al servidor PostgreSQL con Npgsql
        await using var connection = new NpgsqlConnection(adminConnectionString);
        await connection.OpenAsync();
        
        // 4. Ejecutar comandos SQL para crear usuario y base de datos
        // ¡OJO! Usar parámetros para evitar inyección SQL
        var createUserCommand = $"CREATE USER \"{dbUser}\" WITH PASSWORD '{dbPassword}';";
        var createDbCommand = $"CREATE DATABASE \"{dbName}\" OWNER \"{dbUser}\";";

        await using (var cmd = new NpgsqlCommand(createUserCommand, connection))
        {
            await cmd.ExecuteNonQueryAsync();
        }
        await using (var cmd = new NpgsqlCommand(createDbCommand, connection))
        {
            await cmd.ExecuteNonQueryAsync();
        }

        // 5. Devolver los detalles de la conexión
        return new DatabaseConnectionDetails
        {
            Host = connection.Host,
            Port = connection.Port,
            DatabaseName = dbName,
            Username = dbUser,
            Password = dbPassword
        };
    }

    private string GenerateSecurePassword()
    {
        // Implementación mejorada para generar contraseñas seguras
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*()";
        var random = new Random();
        var password = new char[16];

        // Asegurar al menos un carácter de cada tipo
        password[0] = chars[random.Next(0, 26)]; // Mayúscula
        password[1] = chars[random.Next(26, 52)]; // Minúscula
        password[2] = chars[random.Next(52, 62)]; // Número
        password[3] = chars[random.Next(62, chars.Length)]; // Símbolo

        // Llenar el resto con caracteres aleatorios
        for (int i = 4; i < password.Length; i++)
        {
            password[i] = chars[random.Next(chars.Length)];
        }

        // Mezclar la contraseña
        for (int i = password.Length - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            var temp = password[i];
            password[i] = password[j];
            password[j] = temp;
        }

        return new string(password);
    }
}