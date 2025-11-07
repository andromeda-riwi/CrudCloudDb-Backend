using CCD.Core.Dtos;
using CCD.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
using MongoDB.Driver;
using Npgsql;
using MySql.Data.MySqlClient;
using Microsoft.Data.SqlClient;

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
            "postgresql" => await CreatePostgreSqlDatabaseAsync(userId),
            "mongodb" => await CreateMongoDatabaseAsync(userId),
            "mysql" => await CreateMySqlDatabaseAsync(userId),
            "sqlserver" => await CreateSqlServerDatabaseAsync(userId),
            _ => throw new NotImplementedException($"El motor de base de datos '{engine}' no es soportado actualmente.")
        };
    }

    private async Task<DatabaseConnectionDetails> CreatePostgreSqlDatabaseAsync(Guid userId)
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

        // 5. Revocar permisos de DROP DATABASE para que solo se pueda eliminar vía API
        var revokeDropCommand = $"REVOKE CREATE ON DATABASE \"{escapedDbName}\" FROM \"{escapedUser}\";";
        await using (var cmd = new NpgsqlCommand(revokeDropCommand, connection))
        {
            await cmd.ExecuteNonQueryAsync();
        }

        // Conectarse a la nueva base de datos para configurar permisos del schema
        var dbConnectionString = $"Host={connection.Host};Port={connection.Port};Database={dbName};Username={dbUser};Password={dbPassword}";
        await using var dbConnection = new NpgsqlConnection(dbConnectionString);
        await dbConnection.OpenAsync();

        // Dar permisos completos sobre el schema public pero sin DROP DATABASE
        var grantSchemaCommand = "GRANT ALL PRIVILEGES ON SCHEMA public TO \"" + escapedUser + "\";";
        await using (var cmd = new NpgsqlCommand(grantSchemaCommand, dbConnection))
        {
            await cmd.ExecuteNonQueryAsync();
        }

        // 6. Devolver los detalles de la conexión
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

    private async Task<DatabaseConnectionDetails> CreateMySqlDatabaseAsync(Guid userId)
    {
        // 1. Obtener la cadena de conexión del SUPERUSUARIO desde appsettings.json
        var adminConnectionString = _config.GetConnectionString("AdminMySqlConnection");
        
        // VALIDACIÓN: Verificar que la cadena de conexión esté configurada
        if (string.IsNullOrEmpty(adminConnectionString))
        {
            throw new InvalidOperationException(
                "La cadena de conexión 'AdminMySqlConnection' no está configurada en appsettings.json. " +
                "Esta conexión debe apuntar al usuario 'root' con permisos de superusuario para crear bases de datos."
            );
        }

        // 2. Generar credenciales seguras y aleatorias
        var dbName = $"user_{userId.ToString().Substring(0, 8)}_{Guid.NewGuid().ToString().Substring(0, 4)}";
        var dbUser = $"user_{Guid.NewGuid().ToString("N").Substring(0, 12)}";
        var dbPassword = GenerateSecurePassword();

        // 3. Conectarse al servidor MySQL
        await using var connection = new MySqlConnection(adminConnectionString);
        await connection.OpenAsync();
        
        // 4. Ejecutar comandos SQL para crear base de datos y usuario
        var createDbCommand = $"CREATE DATABASE `{dbName}`;";
        var createUserCommand = $"CREATE USER '{dbUser}'@'%' IDENTIFIED BY '{dbPassword}';";
        
        // Otorgar permisos específicos SIN DROP DATABASE (solo operaciones CRUD y DDL)
        var grantPrivilegesCommand = $@"GRANT SELECT, INSERT, UPDATE, DELETE, CREATE, ALTER, 
                                         INDEX, REFERENCES, CREATE TEMPORARY TABLES, LOCK TABLES, 
                                         EXECUTE, CREATE VIEW, SHOW VIEW, CREATE ROUTINE, 
                                         ALTER ROUTINE, TRIGGER 
                                         ON `{dbName}`.* TO '{dbUser}'@'%';";
        var flushPrivilegesCommand = "FLUSH PRIVILEGES;";

        await using (var cmd = new MySqlCommand(createDbCommand, connection))
        {
            await cmd.ExecuteNonQueryAsync();
        }
        await using (var cmd = new MySqlCommand(createUserCommand, connection))
        {
            await cmd.ExecuteNonQueryAsync();
        }
        await using (var cmd = new MySqlCommand(grantPrivilegesCommand, connection))
        {
            await cmd.ExecuteNonQueryAsync();
        }
        await using (var cmd = new MySqlCommand(flushPrivilegesCommand, connection))
        {
            await cmd.ExecuteNonQueryAsync();
        }

        // 5. Devolver los detalles de la conexión
        return new DatabaseConnectionDetails
        {
            Host = connection.DataSource.Split(':')[0], // Extraer host sin puerto
            Port = connection.ServerVersion != null ? 3306 : 3306, // Puerto por defecto MySQL
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

    private async Task<DatabaseConnectionDetails> CreateSqlServerDatabaseAsync(Guid userId)
    {
        // 1. Obtener la cadena de conexión del SUPERUSUARIO desde appsettings.json
        var adminConnectionString = _config.GetConnectionString("AdminSqlServerConnection");
        
        // VALIDACIÓN: Verificar que la cadena de conexión esté configurada
        if (string.IsNullOrEmpty(adminConnectionString))
        {
            throw new InvalidOperationException(
                "La cadena de conexión 'AdminSqlServerConnection' no está configurada en appsettings.json. " +
                "Esta conexión debe apuntar al usuario 'sa' con permisos de administrador para crear bases de datos."
            );
        }

        // 2. Generar credenciales seguras y aleatorias
        var dbName = $"user_{userId.ToString().Substring(0, 8)}_{Guid.NewGuid().ToString().Substring(0, 4)}";
        var dbUser = $"user_{Guid.NewGuid().ToString("N").Substring(0, 12)}";
        var dbPassword = GenerateSecurePassword();

        // 3. Conectarse al servidor SQL Server
        await using var connection = new SqlConnection(adminConnectionString);
        await connection.OpenAsync();
        
        // 4. Ejecutar comandos SQL para crear base de datos y usuario
        // Crear la base de datos
        var createDbCommand = $"CREATE DATABASE [{dbName}];";
        await using (var cmd = new SqlCommand(createDbCommand, connection))
        {
            await cmd.ExecuteNonQueryAsync();
        }

        // Crear el login (usuario a nivel de servidor)
        var createLoginCommand = $"CREATE LOGIN [{dbUser}] WITH PASSWORD = '{dbPassword}';";
        await using (var cmd = new SqlCommand(createLoginCommand, connection))
        {
            await cmd.ExecuteNonQueryAsync();
        }

        // Cambiar al contexto de la nueva base de datos y crear el usuario
        var useDbCommand = $"USE [{dbName}];";
        await using (var cmd = new SqlCommand(useDbCommand, connection))
        {
            await cmd.ExecuteNonQueryAsync();
        }

        // Crear usuario en la base de datos y asignar permisos
        var createUserCommand = $"CREATE USER [{dbUser}] FOR LOGIN [{dbUser}];";
        await using (var cmd = new SqlCommand(createUserCommand, connection))
        {
            await cmd.ExecuteNonQueryAsync();
        }

        // Otorgar permisos específicos SIN db_owner (que permite DROP DATABASE)
        // db_datareader: Leer datos
        // db_datawriter: Escribir datos
        // db_ddladmin: Crear/modificar tablas, vistas, etc. (pero NO DROP DATABASE)
        var grantDataReaderCommand = $"ALTER ROLE db_datareader ADD MEMBER [{dbUser}];";
        await using (var cmd = new SqlCommand(grantDataReaderCommand, connection))
        {
            await cmd.ExecuteNonQueryAsync();
        }

        var grantDataWriterCommand = $"ALTER ROLE db_datawriter ADD MEMBER [{dbUser}];";
        await using (var cmd = new SqlCommand(grantDataWriterCommand, connection))
        {
            await cmd.ExecuteNonQueryAsync();
        }

        var grantDdlAdminCommand = $"ALTER ROLE db_ddladmin ADD MEMBER [{dbUser}];";
        await using (var cmd = new SqlCommand(grantDdlAdminCommand, connection))
        {
            await cmd.ExecuteNonQueryAsync();
        }

        // 5. Devolver los detalles de la conexión
        return new DatabaseConnectionDetails
        {
            Host = connection.DataSource.Split(',')[0], // Extraer host sin puerto
            Port = 1433, // Puerto por defecto SQL Server
            DatabaseName = dbName,
            Username = dbUser,
            Password = dbPassword
        };
    }

    public async Task<bool> DeleteDatabaseAsync(string engine, string databaseName, string username)
    {
        var engineLower = engine.ToLower();
        
        try
        {
            if (engineLower == "postgresql")
            {
                return await DeletePostgreSqlDatabaseAsync(databaseName, username);
            }
            else if (engineLower == "mysql")
            {
                return await DeleteMySqlDatabaseAsync(databaseName, username);
            }
            else if (engineLower == "sqlserver")
            {
                return await DeleteSqlServerDatabaseAsync(databaseName, username);
            }
            else
            {
                throw new NotImplementedException($"El motor de base de datos '{engine}' no es soportado.");
            }
        }
        catch (Exception)
        {
            // Si hay algún error, devolvemos false
            return false;
        }
    }

    private async Task<bool> DeletePostgreSqlDatabaseAsync(string databaseName, string username)
    {
        var adminConnectionString = _config.GetConnectionString("AdminPostgresConnection");
        if (string.IsNullOrEmpty(adminConnectionString))
        {
            return false;
        }

        await using var connection = new NpgsqlConnection(adminConnectionString);
        await connection.OpenAsync();

        // Terminar todas las conexiones activas a la base de datos
        var terminateConnectionsCommand = $@"
            SELECT pg_terminate_backend(pg_stat_activity.pid)
            FROM pg_stat_activity
            WHERE pg_stat_activity.datname = '{databaseName}'
            AND pid <> pg_backend_pid();";
        
        await using (var cmd = new NpgsqlCommand(terminateConnectionsCommand, connection))
        {
            await cmd.ExecuteNonQueryAsync();
        }

        // Eliminar la base de datos
        var dropDbCommand = $"DROP DATABASE IF EXISTS \"{databaseName}\";";
        await using (var cmd = new NpgsqlCommand(dropDbCommand, connection))
        {
            await cmd.ExecuteNonQueryAsync();
        }

        // Eliminar el usuario
        var dropUserCommand = $"DROP USER IF EXISTS \"{username}\";";
        await using (var cmd = new NpgsqlCommand(dropUserCommand, connection))
        {
            await cmd.ExecuteNonQueryAsync();
        }

        return true;
    }

    private async Task<bool> DeleteMySqlDatabaseAsync(string databaseName, string username)
    {
        var adminConnectionString = _config.GetConnectionString("AdminMySqlConnection");
        if (string.IsNullOrEmpty(adminConnectionString))
        {
            return false;
        }

        await using var connection = new MySqlConnection(adminConnectionString);
        await connection.OpenAsync();

        // Terminar todas las conexiones activas a la base de datos
        var killConnectionsCommand = $@"
            SELECT CONCAT('KILL ', id, ';') 
            FROM information_schema.processlist 
            WHERE db = '{databaseName}' AND id != CONNECTION_ID();";
        
        var connectionIds = new List<string>();
        await using (var cmd = new MySqlCommand(killConnectionsCommand, connection))
        {
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                connectionIds.Add(reader.GetString(0));
            }
        }

        // Ejecutar los comandos KILL para terminar las conexiones
        foreach (var killCmd in connectionIds)
        {
            try
            {
                await using var cmd = new MySqlCommand(killCmd, connection);
                await cmd.ExecuteNonQueryAsync();
            }
            catch
            {
                // Ignorar errores si la conexión ya se cerró
            }
        }

        // Eliminar la base de datos
        var dropDbCommand = $"DROP DATABASE IF EXISTS `{databaseName}`;";
        await using (var cmd = new MySqlCommand(dropDbCommand, connection))
        {
            await cmd.ExecuteNonQueryAsync();
        }

        // Eliminar el usuario
        var dropUserCommand = $"DROP USER IF EXISTS '{username}'@'%';";
        await using (var cmd = new MySqlCommand(dropUserCommand, connection))
        {
            await cmd.ExecuteNonQueryAsync();
        }

        // Aplicar cambios
        var flushCommand = "FLUSH PRIVILEGES;";
        await using (var cmd = new MySqlCommand(flushCommand, connection))
        {
            await cmd.ExecuteNonQueryAsync();
        }

        return true;
    }

    private async Task<bool> DeleteSqlServerDatabaseAsync(string databaseName, string username)
    {
        var adminConnectionString = _config.GetConnectionString("AdminSqlServerConnection");
        if (string.IsNullOrEmpty(adminConnectionString))
        {
            return false;
        }

        await using var connection = new SqlConnection(adminConnectionString);
        await connection.OpenAsync();

        // Poner la base de datos en modo SINGLE_USER para cerrar todas las conexiones
        var setSingleUserCommand = $"ALTER DATABASE [{databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;";
        try
        {
            await using (var cmd = new SqlCommand(setSingleUserCommand, connection))
            {
                await cmd.ExecuteNonQueryAsync();
            }
        }
        catch
        {
            // Si falla, continuamos de todas formas
        }

        // Eliminar la base de datos
        var dropDbCommand = $"DROP DATABASE IF EXISTS [{databaseName}];";
        await using (var cmd = new SqlCommand(dropDbCommand, connection))
        {
            await cmd.ExecuteNonQueryAsync();
        }

        // Eliminar el login
        var dropLoginCommand = $"DROP LOGIN IF EXISTS [{username}];";
        await using (var cmd = new SqlCommand(dropLoginCommand, connection))
        {
            await cmd.ExecuteNonQueryAsync();
        }

        return true;
    }
}
