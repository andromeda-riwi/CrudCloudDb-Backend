using CCD.Core.Dtos;
using CCD.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Npgsql;
using MySql.Data.MySqlClient;
using Microsoft.Data.SqlClient;
using MongoDB.Bson;
using MongoDB.Driver;
using CCD.Core.Interfaces;

namespace CCD.Infrastructure.Services;

public class DatabaseProvisioner : IDatabaseProvisioner
{
    private readonly IConfiguration _config;
    private readonly IWebhookService _webhookService;

    public DatabaseProvisioner(IConfiguration config, IWebhookService webhookService)
    {
        _config = config;
        _webhookService = webhookService;
    }

    public async Task<DatabaseConnectionDetails?> CreateDatabaseAsync(string engine, Guid userId)
    {
        var engineLower = engine.ToLower();
        
        if (engineLower == "postgresql")
        {
            return await CreatePostgreSqlDatabaseAsync(userId);
        }
        else if (engineLower == "mysql")
        {
            return await CreateMySqlDatabaseAsync(userId);
        }
        else if (engineLower == "sqlserver")
        {
            return await CreateSqlServerDatabaseAsync(userId);
        }
        else if (engineLower == "mongodb")
        {
            return await CreateMongoDatabaseAsync(userId);
        }
        else
        {
            throw new NotImplementedException($"El motor de base de datos '{engine}' no es soportado.");
        }
    }

    public async Task<DatabaseConnectionDetails> CreatePostgreSqlDatabaseAsync(Guid userId)
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
        
        // 4. Ejecutar comandos SQL para crear usuario y base de datos
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

        // 5. Revocar permisos de DROP DATABASE para que solo se pueda eliminar vía API
        var revokeDropCommand = $"REVOKE CREATE ON DATABASE \"{dbName}\" FROM \"{dbUser}\";";
        await using (var cmd = new NpgsqlCommand(revokeDropCommand, connection))
        {
            await cmd.ExecuteNonQueryAsync();
        }

        // Conectarse a la nueva base de datos para configurar permisos del schema
        var dbConnectionString = $"Host={connection.Host};Port={connection.Port};Database={dbName};Username={dbUser};Password={dbPassword}";
        await using var dbConnection = new NpgsqlConnection(dbConnectionString);
        await dbConnection.OpenAsync();

        // Configurar permisos seguros para el usuario
        var grantSchemaCommand = @$"
            -- Permisos básicos sobre tablas existentes
            GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA public TO ""{dbUser}"";
            
            -- Permisos sobre secuencias (para campos autoincrementales)
            GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA public TO ""{dbUser}"";
            
            -- Permisos por defecto para tablas futuras
            ALTER DEFAULT PRIVILEGES IN SCHEMA public 
            GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO ""{dbUser}"";
            
            -- Permisos por defecto para secuencias futuras
            ALTER DEFAULT PRIVILEGES IN SCHEMA public 
            GRANT USAGE, SELECT ON SEQUENCES TO ""{dbUser}"";";
            
        // Aplicar permisos
        await using (var cmd = new NpgsqlCommand(grantSchemaCommand, dbConnection))
        {
            await cmd.ExecuteNonQueryAsync();
        }

        // 5. Devolver los detalles de la conexión
        var connectionDetails = new DatabaseConnectionDetails
        {
            Host = connection.Host,
            Port = connection.Port,
            DatabaseName = dbName,
            Username = dbUser,
            Password = dbPassword,
            Engine = "PostgreSQL"
        };

        // Disparar webhook de creación de base de datos
        try
        {
            await _webhookService.TriggerWebhooksAsync("database.created", new { UserId = userId, connectionDetails.Engine, connectionDetails.DatabaseName, connectionDetails.Username, connectionDetails.Host, connectionDetails.Port });
        }
        catch (Exception ex)
        {
            // No fallar la operación si el webhook no se puede enviar
            Console.WriteLine($"Error al disparar webhook 'database.created': {ex.Message}");
        }

        return connectionDetails;
    }

    public async Task<DatabaseConnectionDetails> CreateMySqlDatabaseAsync(Guid userId)
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
        var connectionDetails = new DatabaseConnectionDetails
        {
            Host = connection.DataSource.Split(':')[0], // Extraer host sin puerto
            Port = connection.ServerVersion != null ? 3306 : 3306, // Puerto por defecto MySQL
            DatabaseName = dbName,
            Username = dbUser,
            Password = dbPassword,
            Engine = "MySQL"
        };

        // Disparar webhook de creación de base de datos
        try
        {
            await _webhookService.TriggerWebhooksAsync("database.created", new { UserId = userId, connectionDetails.Engine, connectionDetails.DatabaseName, connectionDetails.Username, connectionDetails.Host, connectionDetails.Port });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al disparar webhook 'database.created': {ex.Message}");
        }

        return connectionDetails;
    }

    public async Task<DatabaseConnectionDetails> CreateSqlServerDatabaseAsync(Guid userId)
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
        var connectionDetails = new DatabaseConnectionDetails
        {
            Host = connection.DataSource.Split(',')[0], // Extraer host sin puerto
            Port = 1433, // Puerto por defecto SQL Server
            DatabaseName = dbName,
            Username = dbUser,
            Password = dbPassword,
            Engine = "SQL Server"
        };

        // Disparar webhook de creación de base de datos
        try
        {
            await _webhookService.TriggerWebhooksAsync("database.created", new { UserId = userId, connectionDetails.Engine, connectionDetails.DatabaseName, connectionDetails.Username, connectionDetails.Host, connectionDetails.Port });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al disparar webhook 'database.created': {ex.Message}");
        }

        return connectionDetails;
    }

    public async Task<DatabaseConnectionDetails> CreateMongoDatabaseAsync(Guid userId)
    {
        var adminConnectionString = _config.GetConnectionString("AdminMongoConnection");
        if (string.IsNullOrEmpty(adminConnectionString))
        {
            throw new InvalidOperationException(
                "La cadena de conexión 'AdminMongoConnection' no está configurada. " +
                "Esta conexión debe apuntar a un usuario administrador con permisos para crear bases de datos y usuarios."
            );
        }

        var dbName = $"user_{userId.ToString().Substring(0, 8)}_{Guid.NewGuid().ToString().Substring(0, 4)}";
        var dbUser = $"user_{Guid.NewGuid().ToString("N").Substring(0, 12)}";
        var dbPassword = GenerateSecurePassword();

        var client = new MongoClient(adminConnectionString);
        var mongoUrl = new MongoUrl(adminConnectionString);
        var targetDb = client.GetDatabase(dbName);

        var tempCollection = targetDb.GetCollection<BsonDocument>("_temp_init");
        await tempCollection.InsertOneAsync(new BsonDocument
        {
            { "_id", ObjectId.GenerateNewId() },
            { "temp", true }
        });

        var createUserCommand = new BsonDocument
        {
            { "createUser", dbUser },
            { "pwd", dbPassword },
            {
                "roles",
                new BsonArray
                {
                    new BsonDocument { { "role", "readWrite" }, { "db", dbName } },
                    new BsonDocument { { "role", "dbAdmin" }, { "db", dbName } }
                }
            }
        };

        await targetDb.RunCommandAsync<BsonDocument>(new BsonDocumentCommand<BsonDocument>(createUserCommand));
        await targetDb.DropCollectionAsync("_temp_init");

        var metadataCollection = targetDb.GetCollection<BsonDocument>("_metadata");
        await metadataCollection.InsertOneAsync(new BsonDocument
        {
            { "_id", ObjectId.GenerateNewId() },
            { "createdAt", DateTime.UtcNow },
            { "createdBy", "CrudCloudDb" },
            { "userId", userId.ToString() },
            { "databaseName", dbName },
            { "username", dbUser },
            { "status", "Active" },
            { "description", "Base de datos creada por CrudCloudDb" }
        });

        var host = mongoUrl.Server.Host;
        var port = mongoUrl.Server.Port;

        return new DatabaseConnectionDetails
        {
            Host = host,
            Port = port > 0 ? port : 27017,
            DatabaseName = dbName,
            Username = dbUser,
            Password = dbPassword,
            Engine = "MongoDB"
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
        else if (engineLower == "mongodb")
        {
            var result = await DeleteMongoDatabaseAsync(databaseName, username);
            // Disparar webhook de eliminación de base de datos
            try
            {
                await _webhookService.TriggerWebhooksAsync("database.deleted", new { Engine = engine, DatabaseName = databaseName, Username = username, IsSuccess = result });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al disparar webhook 'database.deleted': {ex.Message}");
            }
            return result;
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

    public async Task<bool> DeletePostgreSqlDatabaseAsync(string databaseName, string username)
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

    public async Task<bool> DeleteMySqlDatabaseAsync(string databaseName, string username)
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

    public async Task<bool> DeleteSqlServerDatabaseAsync(string databaseName, string username)
    {
        var adminConnectionString = _config.GetConnectionString("AdminSqlServerConnection");
        if (string.IsNullOrEmpty(adminConnectionString))
        {
            Console.WriteLine("Error: No se encontró la cadena de conexión 'AdminSqlServerConnection'");
            return false;
        }

        Console.WriteLine($"Intentando eliminar base de datos SQL Server: {databaseName}, Usuario: {username}");
        
        try
        {
            Console.WriteLine("Estableciendo conexión con el servidor SQL...");
            using var connection = new SqlConnection(adminConnectionString);
            await connection.OpenAsync();
            Console.WriteLine("Conexión establecida correctamente");

            try
            {
                // 1. Verificar si la base de datos existe
                var dbExistsCmd = new SqlCommand(
                    $"SELECT 1 FROM sys.databases WHERE name = @dbName", 
                    connection);
                dbExistsCmd.Parameters.AddWithValue("@dbName", databaseName);
                
                var dbExists = await dbExistsCmd.ExecuteScalarAsync() != null;
                Console.WriteLine($"Base de datos {databaseName} existe: {dbExists}");

                if (!dbExists)
                {
                    Console.WriteLine($"La base de datos {databaseName} no existe, procediendo a limpiar el login");
                }
                else
                {
                    // 2. Cerrar todas las conexiones existentes
                    Console.WriteLine("Cerrando conexiones activas...");
                    var killSessionsSql = @"
                        DECLARE @killSessions NVARCHAR(MAX) = '';
                        SELECT @killSessions = @killSessions + 'KILL ' + CAST(session_id AS NVARCHAR(10)) + '; '
                        FROM sys.dm_exec_sessions 
                        WHERE DB_NAME(database_id) = @dbName;
                        
                        IF LEN(@killSessions) > 0
                            EXEC sp_executesql @killSessions;";

                    using var killCmd = new SqlCommand(killSessionsSql, connection);
                    killCmd.Parameters.AddWithValue("@dbName", databaseName);
                    await killCmd.ExecuteNonQueryAsync();
                    Console.WriteLine("Conexiones cerradas");

                    // 3. Poner la base de datos en modo SINGLE_USER
                    Console.WriteLine("Poniendo base de datos en modo SINGLE_USER...");
                    var setSingleUserCmd = new SqlCommand(
                        $"ALTER DATABASE [{databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;", 
                        connection);
                    await setSingleUserCmd.ExecuteNonQueryAsync();
                    Console.WriteLine("Base de datos en modo SINGLE_USER");

                    // 4. Eliminar la base de datos
                    Console.WriteLine("Eliminando base de datos...");
                    var dropDbCmd = new SqlCommand(
                        $"DROP DATABASE IF EXISTS [{databaseName}];", 
                        connection);
                    await dropDbCmd.ExecuteNonQueryAsync();
                    Console.WriteLine("Base de datos eliminada");
                }

                // 5. Eliminar el login de SQL Server si existe
                Console.WriteLine("Verificando login de usuario...");
                var loginExistsCmd = new SqlCommand(
                    "SELECT 1 FROM sys.server_principals WHERE name = @username", 
                    connection);
                loginExistsCmd.Parameters.AddWithValue("@username", username);
                
                var loginExists = await loginExistsCmd.ExecuteScalarAsync() != null;
                Console.WriteLine($"Login {username} existe: {loginExists}");

                if (loginExists)
                {
                    Console.WriteLine("Eliminando login de usuario...");
                    var dropLoginCmd = new SqlCommand(
                        $"DROP LOGIN [{username}]", 
                        connection);
                    await dropLoginCmd.ExecuteNonQueryAsync();
                    Console.WriteLine("Login eliminado");
                }

                Console.WriteLine("Eliminación completada con éxito");
                return true;
            }
            catch (SqlException sqlEx)
            {
                Console.WriteLine($"Error de SQL al eliminar la base de datos {databaseName}:");
                Console.WriteLine($"Número de error: {sqlEx.Number}");
                Console.WriteLine($"Mensaje: {sqlEx.Message}");
                Console.WriteLine($"Procedimiento: {sqlEx.Procedure}");
                Console.WriteLine($"Número de línea: {sqlEx.LineNumber}");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error inesperado al eliminar la base de datos {databaseName}:");
                Console.WriteLine(ex.ToString());
                return false;
            }
        }
        catch (SqlException sqlEx)
        {
            Console.WriteLine($"Error de conexión SQL:");
            Console.WriteLine($"Número de error: {sqlEx.Number}");
            Console.WriteLine($"Mensaje: {sqlEx.Message}");
            Console.WriteLine($"Servidor: {sqlEx.Server}");
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error de conexión inesperado: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
            return false;
        }
    }

    public async Task<bool> DeleteMongoDatabaseAsync(string databaseName, string username)
    {
        var adminConnectionString = _config.GetConnectionString("AdminMongoConnection");
        if (string.IsNullOrEmpty(adminConnectionString))
        {
            return false;
        }

        try
        {
            var client = new MongoClient(adminConnectionString);
            var targetDb = client.GetDatabase(databaseName);

            try
            {
                var dropUserCommand = new BsonDocument { { "dropUser", username } };
                await targetDb.RunCommandAsync<BsonDocument>(new BsonDocumentCommand<BsonDocument>(dropUserCommand));
            }
            catch
            {
                // Si el usuario no existe continuamos con la eliminación de la base de datos
            }

            await client.DropDatabaseAsync(databaseName);
            return true;
        }
        catch
        {
            return false;
        }
    }
    
    public async Task<DatabaseConnectionDetails?> GetDatabaseCredentialsAsync(string engine, string databaseName, string username)
    {
        var engineLower = engine.ToLower();
        
        try
        {
            if (engineLower == "postgresql")
            {
                return await GetPostgreSqlCredentialsAsync(databaseName, username);
            }
            else if (engineLower == "mysql")
            {
                return await GetMySqlCredentialsAsync(databaseName, username);
            }
        else if (engineLower == "sqlserver")
            {
                return await GetSqlServerCredentialsAsync(databaseName, username);
            }
        else if (engineLower == "mongodb")
        {
            return await GetMongoCredentialsAsync(databaseName, username);
        }
            else
            {
                throw new NotImplementedException($"El motor de base de datos '{engine}' no es soportado.");
            }
        }
        catch (Exception)
        {
            return null;
        }
    }

    private async Task<DatabaseConnectionDetails?> GetPostgreSqlCredentialsAsync(string databaseName, string username)
    {
        var adminConnectionString = _config.GetConnectionString("AdminPostgresConnection");
        if (string.IsNullOrEmpty(adminConnectionString))
        {
            return null;
        }

        await using var connection = new NpgsqlConnection(adminConnectionString);
        await connection.OpenAsync();

        // Verificar que la base de datos existe
        var checkDbCommand = $"SELECT 1 FROM pg_database WHERE datname = '{databaseName}';";
        await using (var cmd = new NpgsqlCommand(checkDbCommand, connection))
        {
            var result = await cmd.ExecuteScalarAsync();
            if (result == null)
            {
                return null;
            }
        }

        // Retornar las credenciales (la contraseña no se puede recuperar, así que mostramos un mensaje)
        return new DatabaseConnectionDetails
        {
            Host = connection.Host,
            Port = connection.Port,
            DatabaseName = databaseName,
            Username = username,
            Password = "******",
            Engine = "PostgreSQL"
        };
    }

    private async Task<DatabaseConnectionDetails?> GetMySqlCredentialsAsync(string databaseName, string username)
    {
        var adminConnectionString = _config.GetConnectionString("AdminMySqlConnection");
        if (string.IsNullOrEmpty(adminConnectionString))
        {
            return null;
        }

        await using var connection = new MySqlConnection(adminConnectionString);
        await connection.OpenAsync();

        // Verificar que la base de datos existe
        var checkDbCommand = $"SELECT SCHEMA_NAME FROM INFORMATION_SCHEMA.SCHEMATA WHERE SCHEMA_NAME = '{databaseName}';";
        await using (var cmd = new MySqlCommand(checkDbCommand, connection))
        {
            var result = await cmd.ExecuteScalarAsync();
            if (result == null)
            {
                return null;
            }
        }

        // Retornar las credenciales
        return new DatabaseConnectionDetails
        {
            Host = connection.DataSource.Split(':')[0],
            Port = 3306,
            DatabaseName = databaseName,
            Username = username,
            Password = "******",
            Engine = "MySQL"
        };
    }

    private async Task<DatabaseConnectionDetails?> GetSqlServerCredentialsAsync(string databaseName, string username)
    {
        var adminConnectionString = _config.GetConnectionString("AdminSqlServerConnection");
        if (string.IsNullOrEmpty(adminConnectionString))
        {
            return null;
        }

        await using var connection = new SqlConnection(adminConnectionString);
        await connection.OpenAsync();

        // Verificar que la base de datos existe
        var checkDbCommand = $"SELECT name FROM sys.databases WHERE name = '{databaseName}';";
        await using (var cmd = new SqlCommand(checkDbCommand, connection))
        {
            var result = await cmd.ExecuteScalarAsync();
            if (result == null)
            {
                return null;
            }
        }

        // Retornar las credenciales
        return new DatabaseConnectionDetails
        {
            Host = connection.DataSource.Split(',')[0],
            Port = 1433,
            DatabaseName = databaseName,
            Username = username,
            Password = "******",
            Engine = "SQL Server"
        };
    }

    private async Task<DatabaseConnectionDetails?> GetMongoCredentialsAsync(string databaseName, string username)
    {
        var adminConnectionString = _config.GetConnectionString("AdminMongoConnection");
        if (string.IsNullOrEmpty(adminConnectionString))
        {
            return null;
        }

        var client = new MongoClient(adminConnectionString);

        var databaseNames = await (await client.ListDatabaseNamesAsync()).ToListAsync();
        if (!databaseNames.Contains(databaseName))
        {
            return null;
        }

        var mongoUrl = new MongoUrl(adminConnectionString);
        var host = mongoUrl.Server.Host;
        var port = mongoUrl.Server.Port;

        return new DatabaseConnectionDetails
        {
            Host = host,
            Port = port > 0 ? port : 27017,
            DatabaseName = databaseName,
            Username = username,
            Password = "******",
            Engine = "MongoDB"
        };
    }

    /// <summary>
    /// Rotar credenciales de una base de datos
    /// Genera nuevas credenciales para el usuario existente
    /// </summary>
    public async Task<DatabaseConnectionDetails?> RotateDatabaseCredentialsAsync(string engine, string databaseName, string oldUsername)
    {
        var engineLower = engine.ToLower();

        try
        {
            if (engineLower == "postgresql")
            {
                return await RotatePostgreSqlCredentialsAsync(databaseName, oldUsername);
            }
            else if (engineLower == "mysql")
            {
                return await RotateMySqlCredentialsAsync(databaseName, oldUsername);
            }
            else if (engineLower == "sqlserver")
            {
                return await RotateSqlServerCredentialsAsync(databaseName, oldUsername);
            }
            else if (engineLower == "mongodb")
            {
                return await RotateMongoCredentialsAsync(databaseName, oldUsername);
            }
            else
            {
                throw new NotImplementedException($"Rotación no soportada para '{engine}'");
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"Error al rotar credenciales: {ex.Message}", ex);
        }
    }

    private async Task<DatabaseConnectionDetails?> RotatePostgreSqlCredentialsAsync(string databaseName, string oldUsername)
    {
        var adminConnectionString = _config.GetConnectionString("AdminPostgresConnection");
        var newUsername = $"user_{Guid.NewGuid().ToString().Substring(0, 8)}";
        var newPassword = GenerateSecurePassword();

        using (var connection = new NpgsqlConnection(adminConnectionString))
        {
            await connection.OpenAsync();
            using (var cmd = new NpgsqlCommand($"DROP ROLE IF EXISTS {oldUsername};", connection))
            {
                await cmd.ExecuteNonQueryAsync();
            }

            using (var cmd = new NpgsqlCommand($"CREATE ROLE {newUsername} WITH LOGIN PASSWORD '{newPassword}';", connection))
            {
                await cmd.ExecuteNonQueryAsync();
            }

            using (var cmd = new NpgsqlCommand($"GRANT CONNECT ON DATABASE {databaseName} TO {newUsername};", connection))
            {
                await cmd.ExecuteNonQueryAsync();
            }

            using (var cmd = new NpgsqlCommand($"GRANT USAGE ON SCHEMA public TO {newUsername}; GRANT CREATE ON SCHEMA public TO {newUsername};", connection))
            {
                await cmd.ExecuteNonQueryAsync();
            }
        }

        return new DatabaseConnectionDetails
        {
            Host = connection.Host,
            Port = connection.Port,
            DatabaseName = databaseName,
            Username = newUsername,
            Password = newPassword,
            Engine = "PostgreSQL"
        };
    }

    private async Task<DatabaseConnectionDetails?> RotateMySqlCredentialsAsync(string databaseName, string oldUsername)
    {
        var adminConnectionString = _config.GetConnectionString("AdminMySqlConnection");
        var newUsername = $"user_{Guid.NewGuid().ToString().Substring(0, 8)}";
        var newPassword = GenerateSecurePassword();

        using (var connection = new MySqlConnection(adminConnectionString))
        {
            await connection.OpenAsync();
            
            using (var cmd = new MySqlCommand($"DROP USER IF EXISTS '{oldUsername}'@'%';", connection))
            {
                await cmd.ExecuteNonQueryAsync();
            }

            using (var cmd = new MySqlCommand($"CREATE USER '{newUsername}'@'%' IDENTIFIED BY '{newPassword}';", connection))
            {
                await cmd.ExecuteNonQueryAsync();
            }

            using (var cmd = new MySqlCommand($"GRANT ALL PRIVILEGES ON {databaseName}.* TO '{newUsername}'@'%';", connection))
            {
                await cmd.ExecuteNonQueryAsync();
            }

            using (var cmd = new MySqlCommand("FLUSH PRIVILEGES;", connection))
            {
                await cmd.ExecuteNonQueryAsync();
            }
        }

        return new DatabaseConnectionDetails
        {
            Host = connection.Host,
            Port = connection.Port,
            DatabaseName = databaseName,
            Username = newUsername,
            Password = newPassword,
            Engine = "MySQL"
        };
    }

    private async Task<DatabaseConnectionDetails?> RotateSqlServerCredentialsAsync(string databaseName, string oldUsername)
    {
        var adminConnectionString = _config.GetConnectionString("AdminSqlServerConnection");
        var newUsername = $"user_{Guid.NewGuid().ToString().Substring(0, 8)}";
        var newPassword = GenerateSecurePassword();

        using (var connection = new SqlConnection(adminConnectionString))
        {
            await connection.OpenAsync();

            using (var cmd = new SqlCommand($"DROP LOGIN [{oldUsername}];", connection))
            {
                try { await cmd.ExecuteNonQueryAsync(); } catch { }
            }

            using (var cmd = new SqlCommand($"CREATE LOGIN [{newUsername}] WITH PASSWORD = '{newPassword}';", connection))
            {
                await cmd.ExecuteNonQueryAsync();
            }

            using (var cmd = new SqlCommand($"CREATE USER [{newUsername}] FOR LOGIN [{newUsername}];", connection))
            {
                try { await cmd.ExecuteNonQueryAsync(); } catch { }
            }

            using (var cmd = new SqlCommand($"ALTER ROLE db_owner ADD MEMBER [{newUsername}];", connection))
            {
                await cmd.ExecuteNonQueryAsync();
            }
        }

        return new DatabaseConnectionDetails
        {
            Host = connection.DataSource,
            Port = 1433,
            DatabaseName = databaseName,
            Username = newUsername,
            Password = newPassword,
            Engine = "SQL Server"
        };
    }

    private async Task<DatabaseConnectionDetails?> RotateMongoCredentialsAsync(string databaseName, string oldUsername)
    {
        var adminConnectionString = _config.GetConnectionString("AdminMongoConnection");
        var newUsername = $"user_{Guid.NewGuid().ToString().Substring(0, 8)}";
        var newPassword = GenerateSecurePassword();

        var client = new MongoClient(adminConnectionString);
        var db = client.GetDatabase("admin");
        var usersCollection = db.GetCollection("system.users");

        try
        {
            await db.RunCommandAsync(new BsonDocument("dropUser", oldUsername));
        }
        catch { }

        await db.RunCommandAsync(new BsonDocument
        {
            { "createUser", newUsername },
            { "pwd", newPassword },
            { "roles", new BsonArray { new BsonDocument { { "role", "readWrite" }, { "db", databaseName } } } }
        });

        return new DatabaseConnectionDetails
        {
            Host = adminConnectionString.Split("@")[1].Split(":")[0],
            Port = 27017,
            DatabaseName = databaseName,
            Username = newUsername,
            Password = newPassword,
            Engine = "MongoDB"
        };
    }

    private string GenerateSecurePassword()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%";
        var random = new Random();
        return new string(Enumerable.Range(0, 16).Select(_ => chars[random.Next(chars.Length)]).ToArray());
    }
}