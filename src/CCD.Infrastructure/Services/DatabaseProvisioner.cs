using CCD.Core;
using CCD.Core.Interfaces;
using CCD.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CCD.Infrastructure.Services
{
    public class DatabaseProvisioner : IDatabaseProvisioner
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _config;

        public DatabaseProvisioner(ApplicationDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        public async Task<DatabaseInstance?> CreateDatabaseAsync(User user, string engine)
        {
            if (engine.ToLower() != "postgresql")
            {
                throw new NotImplementedException("El motor de base de datos no es soportado.");
            }

            var userDbCountForEngine = await _context.DatabaseInstances
                .CountAsync(db => db.UserId == user.Id && db.Engine.ToLower() == engine.ToLower());

            var userPlan = await _context.Plans.FindAsync(user.PlanId);

            if (userPlan == null || userDbCountForEngine >= userPlan.DatabaseLimitPerEngine)
            {
                return null;
            }

            var adminConnectionString = _config.GetConnectionString("AdminPostgresConnection");
            if (string.IsNullOrEmpty(adminConnectionString))
            {
                throw new InvalidOperationException("La cadena de conexión 'AdminPostgresConnection' no está configurada.");
            }

            var dbName = $"user_{user.Id.ToString().Substring(0, 8)}_{Guid.NewGuid().ToString().Substring(0, 4)}";
            var dbUser = $"user_{Guid.NewGuid().ToString("N").Substring(0, 12)}";
            var dbPassword = GenerateSecurePassword();

            await using var connection = new NpgsqlConnection(adminConnectionString);
            await connection.OpenAsync();
            
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
            
            var newDbInstance = new DatabaseInstance
            {
                Id = Guid.NewGuid(),
                Name = dbName,
                Engine = engine,
                Host = connection.Host!,
                Port = connection.Port,
                DbUsername = dbUser,
                Status = "active",
                UserId = user.Id
            };

            await _context.DatabaseInstances.AddAsync(newDbInstance);
            await _context.SaveChangesAsync();
            
            return newDbInstance;
        }

        public async Task<bool> DeleteDatabaseAsync(Guid instanceId, User user)
        {
            var dbInstance = await _context.DatabaseInstances
                .FirstOrDefaultAsync(db => db.Id == instanceId && db.UserId == user.Id);

            if (dbInstance == null)
            {
                return false;
            }

            if (dbInstance.Engine.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    var adminConnectionString = _config.GetConnectionString("AdminPostgresConnection");
                    if (string.IsNullOrEmpty(adminConnectionString))
                        throw new InvalidOperationException("La cadena de conexión 'AdminPostgresConnection' no está configurada.");

                    await using var connection = new NpgsqlConnection(adminConnectionString);
                    await connection.OpenAsync();

                    var terminateConnectionsCommand = $"SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = '{dbInstance.Name}';";
                    var dropDbCommand = $"DROP DATABASE \"{dbInstance.Name}\";";
                    var dropUserCommand = $"DROP USER \"{dbInstance.DbUsername}\";";

                    await using (var cmd = new NpgsqlCommand(terminateConnectionsCommand, connection)) await cmd.ExecuteNonQueryAsync();
                    await using (var cmd = new NpgsqlCommand(dropDbCommand, connection)) await cmd.ExecuteNonQueryAsync();
                    await using (var cmd = new NpgsqlCommand(dropUserCommand, connection)) await cmd.ExecuteNonQueryAsync();
                    
                    _context.DatabaseInstances.Remove(dbInstance);
                    await _context.SaveChangesAsync();
                    
                    return true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error al borrar la base de datos {dbInstance.Name}: {ex.Message}");
                    return false;
                }
            }
            
            return false;
        }

        public async Task<IEnumerable<DatabaseInstance>> GetUserDatabasesAsync(Guid userId)
        {
            return await _context.DatabaseInstances
                .Where(db => db.UserId == userId)
                .ToListAsync();
        }

        private string GenerateSecurePassword()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*()";
            var random = new Random();
            var password = new char[16];
            password[0] = chars[random.Next(0, 26)];
            password[1] = chars[random.Next(26, 52)];
            password[2] = chars[random.Next(52, 62)];
            password[3] = chars[random.Next(62, chars.Length)];
            for (int i = 4; i < password.Length; i++)
            {
                password[i] = chars[random.Next(chars.Length)];
            }
            for (int i = password.Length - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                var temp = password[i];
                password[i] = password[j];
                password[j] = temp;
            }
            return new string(password);
        }
    } // <-- Llave de cierre de la CLASE
} // <-- Llave de cierre del NAMESPACE