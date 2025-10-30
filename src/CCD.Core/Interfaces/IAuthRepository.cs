using CCD.Core;

namespace CCD.Core.Interfaces;

public interface IAuthRepository
{
    // El '?' después de User indica que el método puede devolver un usuario o nulo.
    // Esto es útil para manejar el caso en que el registro falla (ej: email ya existe).
    Task<User?> Register(User user, string password);

    // El '?' después de string indica que el método puede devolver un token o nulo
    // si el login falla.
    Task<string?> Login(string email, string password);

    Task<bool> UserExists(string email);
}