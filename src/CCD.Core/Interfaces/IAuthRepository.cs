namespace CCD.Core.Interfaces;

public interface IAuthRepository
{
    Task<User?> Register(User user, string password);
    Task<string?> Login(string email, string password);
}