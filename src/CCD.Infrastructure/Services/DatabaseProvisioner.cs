using CCD.Core.Dtos;
using CCD.Core.Interfaces;
using Microsoft.Extensions.Configuration;
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
        return new DatabaseConnectionDetails
        {
            Host = connection.Host,
            Port = connection.Port,
            DatabaseName = dbName,
            Username = dbUser,
            Password = dbPassword,
            Engine = "PostgreSQL"
        };
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
        return new DatabaseConnectionDetails
        {
            Host = connection.DataSource.Split(':')[0], // Extraer host sin puerto
            Port = connection.ServerVersion != null ? 3306 : 3306, // Puerto por defecto MySQL
            DatabaseName = dbName,
            Username = dbUser,
            Password = dbPassword,
            Engine = "MySQL"
        };
    }

    public string GenerateSecurePassword()
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
        return new DatabaseConnectionDetails
        {
            Host = connection.DataSource.Split(',')[0], // Extraer host sin puerto
            Port = 1433, // Puerto por defecto SQL Server
            DatabaseName = dbName,
            Username = dbUser,
            Password = dbPassword,
            Engine = "SQL Server"
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
}