using CCD.Core;

namespace CCD.Core.Interfaces
{
    public interface IAuthRepository
    {
        Task<User?> Register(User user, string password);

        // --- ESTA ES LA FIRMA CORRECTA ---
        // Recibe el identificador (email o username) y la contraseña.
        Task<string?> Login(string identifier, string password);

        // Actualizamos esto también para que sea coherente.
        Task<bool> UserExists(string emailOrUsername);
    }
}