﻿using CCD.Core;

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
}