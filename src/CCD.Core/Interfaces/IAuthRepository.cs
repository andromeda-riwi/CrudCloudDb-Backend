﻿﻿using CCD.Core;

namespace CCD.Core.Interfaces;

public interface IAuthRepository
{
    // El '?' después de User indica que el método puede devolver un usuario o nulo.
    // Esto es útil para manejar el caso en que el registro falla (ej: email ya existe).
    Task<User?> Register(User user, string password);

    // El '?' después de string indica que el método puede devolver un token o nulo
    // si el login falla.
    // identifier: puede ser email o username
    // isEmail: true si identifier es un email, false si es un username
    Task<string?> Login(string identifier, string password, bool isEmail = true);

    Task<bool> UserExists(string email);
    
    // Verificación de email
    Task<string> GenerateEmailVerificationTokenAsync(Guid userId);
    Task<bool> VerifyEmailAsync(string token);
    Task<bool> IsEmailVerifiedAsync(Guid userId);
    
    // Recuperación de contraseña
    Task<string?> GeneratePasswordResetTokenAsync(string email);
    Task<bool> ResetPasswordAsync(string token, string newPassword);
    Task<User?> GetUserByEmailAsync(string email);
    Task<User?> GetUserByUserNameAsync(string userName);
    
    // Cambiar contraseña (requiere contraseña actual)
    Task<bool> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword);
}