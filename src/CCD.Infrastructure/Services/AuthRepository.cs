using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CCD.Core;
using CCD.Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace CCD.Infrastructure.Data
{
    public class AuthRepository : IAuthRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _config;

        public AuthRepository(ApplicationDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        public async Task<User?> Register(User user, string password)
        {
            // 1. Hashear la contraseña usando BCrypt. WorkFactor 12 es un buen estándar de seguridad.
            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(password, 12);

            // 2. Asignar el hash al objeto de usuario.
            user.PasswordHash = hashedPassword;
            
            // 3. Asignar el plan gratuito por defecto al nuevo usuario. Asumimos que el PlanId=1 es el gratuito.
            user.PlanId = 1;

            // 4. Añadir el usuario al contexto de la base de datos y guardar los cambios.
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            return user;
        }

        public async Task<string?> Login(string identifier, string password)
        {
            // 1. Buscar al usuario por email O por username, de forma case-insensitive.
            var user = await _context.Users.FirstOrDefaultAsync(u => 
                u.Email.ToLower() == identifier.ToLower() || 
                u.UserName.ToLower() == identifier.ToLower()
            );

            // 2. Si no se encuentra usuario o la contraseña no coincide, devolver nulo.
            // BCrypt.Verify se encarga de comparar el hash guardado con la contraseña ingresada.
            if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            {
                return null;
            }

            // 3. Si las credenciales son correctas, crear y devolver el token JWT.
            return CreateToken(user);
        }

        public async Task<bool> UserExists(string emailOrUsername)
        {
            // Comprueba si algún usuario coincide con el email o el username de forma case-insensitive.
            // AnyAsync es muy eficiente para esto.
            return await _context.Users.AnyAsync(u => 
                u.Email.ToLower() == emailOrUsername.ToLower() || 
                u.UserName.ToLower() == emailOrUsername.ToLower()
            );
        }

        // Método privado para generar el token JWT
        private string CreateToken(User user)
        {
            // 1. Crear los "claims": información que queremos guardar dentro del token.
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()), // El ID del usuario
                new Claim(ClaimTypes.Name, user.UserName) // El nombre de usuario
            };

            // 2. Obtener la clave secreta desde appsettings.json
            var tokenKeyString = _config.GetSection("AppSettings:Token").Value;
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(tokenKeyString!));

            // 3. Crear las credenciales de firma con un algoritmo de seguridad fuerte.
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);

            // 4. Crear el descriptor del token, que une todos los componentes.
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.Now.AddDays(1), // El token será válido por 1 día.
                SigningCredentials = creds
            };

            // 5. Crear y escribir el token como un string.
            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);

            return tokenHandler.WriteToken(token);
        }
    }
}