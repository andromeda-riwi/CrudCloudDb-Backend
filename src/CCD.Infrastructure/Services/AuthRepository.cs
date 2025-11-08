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

    public AuthRepository(ApplicationDbContext context, IConfiguration config, IEmailService emailService, ILogger<AuthRepository> logger)
    {
        _context = context;
        _config = config;
        _emailService = emailService;
        _logger = logger;
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
            
            // Enviar correo de bienvenida
            try
            {
                await _emailService.SendWelcomeEmailAsync(user.Email, user.UserName);
                _logger.LogInformation($"Correo de bienvenida enviado a {user.Email}");
            }
            catch (Exception ex)
            {
                // No fallar el registro si el correo no se puede enviar
                _logger.LogError(ex, $"Error al enviar correo de bienvenida a {user.Email}");
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

        Console.WriteLine($"[AuthRepo] Password correcto. Generando token para: {identifier}");
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
}