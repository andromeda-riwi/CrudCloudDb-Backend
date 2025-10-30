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
using Microsoft.IdentityModel.Tokens;

namespace CCD.Infrastructure.Services;

// Esta clase contiene la implementación real (la "cocina") de la lógica de autenticación.
// Implementa el contrato definido en IAuthRepository.
public class AuthRepository : IAuthRepository
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _config;

    public AuthRepository(ApplicationDbContext context, IConfiguration config)
    {
        _context = context;
        _config = config;
    }

    // --- LÓGICA DE REGISTRO ---
    // Devuelve Task<User?> para indicar que el resultado puede ser un usuario o nulo.
    public async Task<User?> Register(User user, string password)
    {
        if (await UserExists(user.Email))
            return null; // Devuelve null si el usuario ya existe

        CreatePasswordHash(password, out byte[] passwordHash, out byte[] passwordSalt);

        user.PasswordHash = passwordHash;
        user.PasswordSalt = passwordSalt;

        // Asignar el plan gratuito por defecto (buscar "Gratuito" del seeding o "Free")
        var freePlan = await _context.Plans.FirstOrDefaultAsync(p => p.Name == "Gratuito" || p.Name == "Free");
        if (freePlan == null)
        {
            // Si no existe ningún plan gratuito, usar el primero del seeding (Id = 1)
            freePlan = await _context.Plans.FirstOrDefaultAsync(p => p.Id == 1);
            if (freePlan == null)
            {
                // Último recurso: crear un plan gratuito
                freePlan = new Plan
                {
                    Name = "Gratuito",
                    DatabaseLimitPerEngine = 2,
                    Price = 0,
                    MercadoPagoPriceId = "N/A",
                    MaxDatabases = 2,
                    IsActive = true
                };
                await _context.Plans.AddAsync(freePlan);
                await _context.SaveChangesAsync();
            }
        }
        
        user.PlanId = freePlan.Id;

        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        return user;
    }

    // --- LÓGICA DE LOGIN ---
    // Devuelve Task<string?> para indicar que el resultado puede ser un token (string) o nulo.
    public async Task<string?> Login(string email, string password)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());

        if (user == null || !VerifyPasswordHash(password, user.PasswordHash, user.PasswordSalt))
        {
            return null; // Devuelve null si las credenciales son inválidas
        }

        string token = CreateToken(user);
        return token;
    }


    // --- MÉTODOS PRIVADOS DE AYUDA ---

    public async Task<bool> UserExists(string email)
    {
        return await _context.Users.AnyAsync(u => u.Email.ToLower() == email.ToLower());
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
            new Claim(ClaimTypes.Email, user.Email)
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