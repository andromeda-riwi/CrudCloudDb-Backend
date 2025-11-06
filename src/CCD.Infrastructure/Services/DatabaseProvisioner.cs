using CCD.Core.Dtos;
using CCD.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
using MongoDB.Driver;
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
        return engine.ToLower() switch
        {
            "postgresql" => await CreatePostgresDatabaseAsync(userId),
            "mongodb" => await CreateMongoDatabaseAsync(userId),
            _ => throw new NotImplementedException($"El motor de base de datos '{engine}' no es soportado actualmente.")
        };
    }

    private async Task<DatabaseConnectionDetails> CreatePostgresDatabaseAsync(Guid userId)
    {
        // 1. Obtener la cadena de conexión del SUPERUSUARIO desde appsettings.json
        var adminConnectionString = _config.GetConnectionString("AdminPostgresConnection");
        
        // VALIDACIÓN: Verificar que la cadena de conexión esté configurada
        if (string.IsNullOrEmpty(adminConnectionString))
        {
            throw new InvalidOperationException(
                "La cadena de conexión 'AdminPostgresConnection' no está configurada en appsettings.json. " +
                "Esta conexión debe apuntar al usuario 'postgres' con permisos de superusuario para crear bases de datos."
            );
        }

        // 2. Generar credenciales seguras y aleatorias
        var dbName = $"user_{userId.ToString().Substring(0, 8)}_{Guid.NewGuid().ToString().Substring(0, 4)}";
        var dbUser = $"user_{Guid.NewGuid().ToString("N").Substring(0, 12)}";
        var dbPassword = GenerateSecurePassword();

        // 3. Conectarse al servidor PostgreSQL con Npgsql
        await using var connection = new NpgsqlConnection(adminConnectionString);
        await connection.OpenAsync();
        
        // 4. Ejecutar comandos SQL usando parámetros para evitar inyección SQL
        // Nota: Para CREATE USER y CREATE DATABASE, PostgreSQL no soporta parámetros directamente,
        // pero validamos que los nombres generados sean seguros (solo GUIDs y formato controlado)
        var escapedUser = dbUser.Replace("\"", "\"\"");
        var escapedDbName = dbName.Replace("\"", "\"\"");
        var escapedPassword = dbPassword.Replace("'", "''"); // Escapar comillas simples en la contraseña
        
        var createUserCommand = $"CREATE USER \"{escapedUser}\" WITH PASSWORD '{escapedPassword}';";
        var createDbCommand = $"CREATE DATABASE \"{escapedDbName}\" OWNER \"{escapedUser}\";";

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
            Host = connection.Host ?? "49.12.100.202",
            Port = connection.Port,
            DatabaseName = dbName,
            Username = dbUser,
            Password = dbPassword
        };
    }

    private async Task<DatabaseConnectionDetails> CreateMongoDatabaseAsync(Guid userId)
    {
        // 1. Obtener la cadena de conexión del ADMIN desde appsettings.json
        var adminConnectionString = _config.GetConnectionString("AdminMongoConnection");
        
        // VALIDACIÓN: Verificar que la cadena de conexión esté configurada
        if (string.IsNullOrEmpty(adminConnectionString))
        {
            throw new InvalidOperationException(
                "La cadena de conexión 'AdminMongoConnection' no está configurada en appsettings.json. " +
                "Esta conexión debe apuntar al usuario 'root' con permisos de administrador para crear bases de datos y usuarios."
            );
        }

        // 2. Generar credenciales seguras y aleatorias
        var dbName = $"user_{userId.ToString().Substring(0, 8)}_{Guid.NewGuid().ToString().Substring(0, 4)}";
        var dbUser = $"user_{Guid.NewGuid().ToString("N").Substring(0, 12)}";
        var dbPassword = GenerateSecurePassword();

        // 3. Conectarse al servidor MongoDB como administrador
        var client = new MongoClient(adminConnectionString);

        // 4. Crear la base de datos (en MongoDB se crea automáticamente al insertar el primer documento)
        var targetDb = client.GetDatabase(dbName);
        
        // 5. Crear usuario específico para esta base de datos con roles readWrite y dbAdmin
        // El comando createUser debe ejecutarse en el contexto de la base de datos objetivo
        // Primero creamos una colección temporal para asegurar que la BD existe
        var tempCollection = targetDb.GetCollection<BsonDocument>("_temp_init");
        await tempCollection.InsertOneAsync(new BsonDocument { { "_id", ObjectId.GenerateNewId() }, { "temp", true } });
        
        var createUserCommand = new BsonDocument
        {
            { "createUser", dbUser },
            { "pwd", dbPassword },
            { "roles", new BsonArray
                {
                    new BsonDocument { { "role", "readWrite" }, { "db", dbName } },
                    new BsonDocument { { "role", "dbAdmin" }, { "db", dbName } }
                }
            }
        };

        // Ejecutar el comando createUser desde la base de datos objetivo
        var result = await targetDb.RunCommandAsync<BsonDocument>(new BsonDocumentCommand<BsonDocument>(createUserCommand));

        // 6. Eliminar la colección temporal y crear una colección permanente de metadatos
        await targetDb.DropCollectionAsync("_temp_init");
        
        // Crear colección permanente de metadatos para que la BD aparezca en MongoDB Compass
        var metadataCollection = targetDb.GetCollection<BsonDocument>("_metadata");
        await metadataCollection.InsertOneAsync(new BsonDocument 
        { 
            { "_id", ObjectId.GenerateNewId() }, 
            { "createdAt", DateTime.UtcNow },
            { "createdBy", "CCD-Platform" },
            { "userId", userId.ToString() },
            { "databaseName", dbName },
            { "username", dbUser },
            { "status", "Active" },
            { "description", "Base de datos creada por CrudCloudDb Platform" }
        });

        // 7. Extraer host y puerto de la cadena de conexión
        var uri = new Uri(adminConnectionString);
        var host = uri.Host;
        var port = uri.Port > 0 ? uri.Port : 27017;

        // 8. Devolver los detalles de la conexión
        return new DatabaseConnectionDetails
        {
            Host = host,
            Port = port,
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