// --- Imports necesarios para toda la funcionalidad ---
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using CCD.Core;
using CCD.Core.Interfaces;
using CCD.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

// Esta clase contiene la implementación real (la "cocina") de la lógica de autenticación.
// Implementa el contrato definido en IAuthRepository.
public class AuthRepository : IAuthRepository
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _config;
    private readonly IEmailService _emailService;
    private readonly ILogger<AuthRepository> _logger;
    private readonly IWebhookService _webhookService;

    public AuthRepository(ApplicationDbContext context, IConfiguration config, IEmailService emailService, ILogger<AuthRepository> logger, IWebhookService webhookService)
    {
        _context = context;
        _config = config;
        _emailService = emailService;
        _logger = logger;
        _webhookService = webhookService;
    }

    // --- LÓGICA DE REGISTRO ---
    // Devuelve Task<User?> para indicar que el resultado puede ser un usuario o nulo.
    public async Task<User?> Register(User user, string password)
    {
        Console.WriteLine($"[AuthRepo] Intentando registrar usuario: {user.Email}, UserName: {user.UserName}");
        
        // Verificar si el email o username ya existen
        if (await UserExists(user.Email))
        {
            Console.WriteLine($"[AuthRepo] El email ya existe: {user.Email}");
            return null;
        }

        if (await UserNameExists(user.UserName))
        {
            Console.WriteLine($"[AuthRepo] El nombre de usuario ya existe: {user.UserName}");
            return null;
        }

        Console.WriteLine($"[AuthRepo] Creando hash de password...");
        CreatePasswordHash(password, out byte[] passwordHash, out byte[] passwordSalt);

        Console.WriteLine($"[AuthRepo] Hash creado. Length: {passwordHash.Length}, Salt length: {passwordSalt.Length}");
        
        user.Id = Guid.NewGuid(); // Asegurarse de que el usuario tenga un ID
        user.PasswordHash = passwordHash;
        user.PasswordSalt = passwordSalt;

        Console.WriteLine($"[AuthRepo] Usuario ID generado: {user.Id}");
        Console.WriteLine($"[AuthRepo] PlanId del usuario: {user.PlanId}");
        
        try
        {
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            Console.WriteLine($"[AuthRepo] Usuario guardado exitosamente: {user.Email}");
            
            // Disparar webhook de creación de cuenta
            try
            {
                await _webhookService.TriggerWebhooksAsync("user.created", new { UserId = user.Id, user.Email, user.UserName });
                _logger.LogInformation($"Webhook 'user.created' disparado para usuario {user.Id}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al disparar webhook 'user.created' para usuario {user.Id}");
            }

            // Generar token de verificación y enviar correo
            // NOTA: El correo de bienvenida se enviará DESPUÉS de verificar el email
            try
            {
                var verificationToken = await GenerateEmailVerificationTokenAsync(user.Id);
                await _emailService.SendEmailVerificationAsync(user.Email, user.UserName, verificationToken);
                _logger.LogInformation($"Correo de verificación enviado a {user.Email}");
            }
            catch (Exception ex)
            {
                // No fallar el registro si el correo no se puede enviar
                _logger.LogError(ex, $"Error al enviar correo de verificación a {user.Email}");
            }
            
            return user;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al registrar usuario");
            throw; // Relanzar la excepción para manejarla en el controlador
        }
    }

    // --- LÓGICA DE LOGIN ---
    // Devuelve Task<string?> para indicar que el resultado puede ser un token (string) o nulo.
    public async Task<string?> Login(string identifier, string password, bool isEmail = true)
    {
        Console.WriteLine($"[AuthRepo] Buscando usuario por {(isEmail ? "email" : "username")}: {identifier}");
        
        // Buscar usuario por email o username según el parámetro
        User? user;
        if (isEmail)
        {
            user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == identifier.ToLower());
        }
        else
        {
            user = await _context.Users.FirstOrDefaultAsync(u => u.UserName.ToLower() == identifier.ToLower());
        }

        if (user == null)
        {
            Console.WriteLine($"[AuthRepo] Usuario no encontrado: {identifier}");
            return null; // Usuario no encontrado
        }

        Console.WriteLine($"[AuthRepo] Usuario encontrado. ID: {user.Id}, Email: {user.Email}, UserName: {user.UserName}");
        
        // Verificar que el hash y salt existan
        if (user.PasswordHash == null || user.PasswordHash.Length == 0 || 
            user.PasswordSalt == null || user.PasswordSalt.Length == 0)
        {
            Console.WriteLine($"[AuthRepo] Datos de password corruptos para: {identifier}");
            Console.WriteLine($"[AuthRepo] PasswordHash length: {user.PasswordHash?.Length ?? 0}");
            Console.WriteLine($"[AuthRepo] PasswordSalt length: {user.PasswordSalt?.Length ?? 0}");
            return null; // Datos de password corruptos
        }

        Console.WriteLine($"[AuthRepo] Verificando password para: {identifier}");
        if (!VerifyPasswordHash(password, user.PasswordHash, user.PasswordSalt))
        {
            Console.WriteLine($"[AuthRepo] Password incorrecto para: {identifier}");
            return null; // Contraseña incorrecta
        }

        Console.WriteLine($"[AuthRepo] Password correcto. Verificando estado de email para: {identifier}");
        
        // VERIFICACIÓN OBLIGATORIA: Bloquear login si el email no está verificado
        if (!user.EmailVerified)
        {
            _logger.LogWarning($"Usuario {user.Id} intentó iniciar sesión sin verificar email - BLOQUEADO");
            // Retornar null para indicar que el login está bloqueado
            // El controlador manejará este caso y retornará un mensaje apropiado
            return null;
        }
        
        Console.WriteLine($"[AuthRepo] Email verificado. Generando token para: {identifier}");
        string token = CreateToken(user);
        return token;
    }


    // --- MÉTODOS PRIVADOS DE AYUDA ---

    public async Task<bool> UserExists(string email)
    {
        return await _context.Users.AnyAsync(u => u.Email.ToLower() == email.ToLower());
    }

    public async Task<bool> UserNameExists(string userName)
    {
        return await _context.Users.AnyAsync(u => u.UserName.ToLower() == userName.ToLower());
    }

    private void CreatePasswordHash(string password, out byte[] passwordHash, out byte[] passwordSalt)
    {
        using (var hmac = new HMACSHA512())
        {
            passwordSalt = hmac.Key;
            passwordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
        }
    }

    private bool VerifyPasswordHash(string password, byte[] passwordHash, byte[] passwordSalt)
    {
        using (var hmac = new HMACSHA512(passwordSalt))
        {
            var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
            return computedHash.SequenceEqual(passwordHash);
        }
    }

    private string CreateToken(User user)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.Name), // ← Nombre del usuario para mostrar en el frontend
            new Claim("userName", user.UserName)    // ← Username adicional
        };

        var appSettingsToken = _config.GetSection("AppSettings:Token").Value;
        if (string.IsNullOrEmpty(appSettingsToken))
            throw new Exception("La clave del token 'AppSettings:Token' no está configurada.");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(appSettingsToken));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.Now.AddDays(1),
            SigningCredentials = creds
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);

        return tokenHandler.WriteToken(token);
    }

    // --- VERIFICACIÓN DE EMAIL ---

    public async Task<string> GenerateEmailVerificationTokenAsync(Guid userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            throw new Exception("Usuario no encontrado.");
        }

        // Generar token seguro
        var token = Guid.NewGuid().ToString("N");
        user.EmailVerificationToken = token;
        user.EmailVerificationTokenExpiry = DateTime.UtcNow.AddHours(24); // Expira en 24 horas

        await _context.SaveChangesAsync();
        _logger.LogInformation($"Token de verificación generado para usuario {userId}");
        
        return token;
    }

    public async Task<bool> VerifyEmailAsync(string token)
    {
        if (string.IsNullOrEmpty(token))
        {
            return false;
        }

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.EmailVerificationToken == token);

        if (user == null)
        {
            _logger.LogWarning($"Intento de verificación con token inválido: {token}");
            return false;
        }

        // Verificar expiración
        if (user.EmailVerificationTokenExpiry == null || user.EmailVerificationTokenExpiry < DateTime.UtcNow)
        {
            _logger.LogWarning($"Intento de verificación con token expirado para usuario {user.Id}");
            // Limpiar token expirado
            user.EmailVerificationToken = null;
            user.EmailVerificationTokenExpiry = null;
            await _context.SaveChangesAsync();
            return false;
        }

        // Marcar email como verificado y limpiar token
        user.EmailVerified = true;
        user.EmailVerificationToken = null;
        user.EmailVerificationTokenExpiry = null;
        await _context.SaveChangesAsync();

        _logger.LogInformation($"Email verificado exitosamente para usuario {user.Id}");
        
        // Enviar correo de bienvenida DESPUÉS de verificar el email
        // Esto asegura que el usuario pueda usar el sistema cuando reciba la bienvenida
        try
        {
            await _emailService.SendWelcomeEmailAsync(user.Email, user.UserName ?? user.Name);
            _logger.LogInformation($"Correo de bienvenida enviado a {user.Email} después de verificación");
        }
        catch (Exception ex)
        {
            // No fallar la verificación si el correo no se puede enviar
            _logger.LogError(ex, $"Error al enviar correo de bienvenida a {user.Email} después de verificación");
        }
        
        return true;
    }

    public async Task<bool> IsEmailVerifiedAsync(Guid userId)
    {
        var user = await _context.Users.FindAsync(userId);
        return user?.EmailVerified ?? false;
    }

    // --- RECUPERACIÓN DE CONTRASEÑA ---

    public async Task<string?> GeneratePasswordResetTokenAsync(string email)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());

        if (user == null)
        {
            // Por seguridad, no revelamos si el email existe o no
            _logger.LogWarning($"Intento de recuperación de contraseña para email no encontrado: {email}");
            return null;
        }

        // Generar token seguro
        var token = Guid.NewGuid().ToString("N");
        user.PasswordResetToken = token;
        user.PasswordResetTokenExpiry = DateTime.UtcNow.AddHours(1); // Expira en 1 hora

        await _context.SaveChangesAsync();
        _logger.LogInformation($"Token de recuperación generado para usuario {user.Id}");
        
        return token;
    }

    public async Task<bool> ResetPasswordAsync(string token, string newPassword)
    {
        if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(newPassword))
        {
            return false;
        }

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.PasswordResetToken == token);

        if (user == null)
        {
            _logger.LogWarning($"Intento de reseteo con token inválido: {token}");
            return false;
        }

        // Verificar expiración
        if (user.PasswordResetTokenExpiry == null || user.PasswordResetTokenExpiry < DateTime.UtcNow)
        {
            _logger.LogWarning($"Intento de reseteo con token expirado para usuario {user.Id}");
            // Limpiar token expirado
            user.PasswordResetToken = null;
            user.PasswordResetTokenExpiry = null;
            await _context.SaveChangesAsync();
            return false;
        }

        // Actualizar contraseña
        CreatePasswordHash(newPassword, out byte[] passwordHash, out byte[] passwordSalt);
        user.PasswordHash = passwordHash;
        user.PasswordSalt = passwordSalt;
        
        // Limpiar token
        user.PasswordResetToken = null;
        user.PasswordResetTokenExpiry = null;
        
        await _context.SaveChangesAsync();

        _logger.LogInformation($"Contraseña restablecida exitosamente para usuario {user.Id}");
        return true;
    }

    public async Task<User?> GetUserByEmailAsync(string email)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());
    }

    public async Task<User?> GetUserByUserNameAsync(string userName)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.UserName.ToLower() == userName.ToLower());
    }

    /// <summary>
    /// Cambiar contraseña del usuario (requiere contraseña actual)
    /// </summary>
    public async Task<bool> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword)
    {
        if (string.IsNullOrEmpty(currentPassword) || string.IsNullOrEmpty(newPassword))
        {
            return false;
        }

        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            _logger.LogWarning($"Intento de cambio de contraseña para usuario no encontrado: {userId}");
            return false;
        }

        // Verificar que la contraseña actual sea correcta
        if (!VerifyPasswordHash(currentPassword, user.PasswordHash, user.PasswordSalt))
        {
            _logger.LogWarning($"Intento de cambio de contraseña con contraseña incorrecta para usuario {userId}");
            return false;
        }

        // Generar nuevo hash y salt
        CreatePasswordHash(newPassword, out byte[] newPasswordHash, out byte[] newPasswordSalt);
        user.PasswordHash = newPasswordHash;
        user.PasswordSalt = newPasswordSalt;

        await _context.SaveChangesAsync();

        _logger.LogInformation($"Contraseña cambiada exitosamente para usuario {userId}");
        return true;
    }
}