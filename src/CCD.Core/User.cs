﻿namespace CCD.Core;

public class User
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public byte[] PasswordHash { get; set; } = Array.Empty<byte>();
    public byte[] PasswordSalt { get; set; } = Array.Empty<byte>();

    // Relación con el Plan
    public int PlanId { get; set; } = 1; // Foreign Key
    public Plan Plan { get; set; } = null!; // Navigation Property

    // Propiedad de navegación: Un usuario puede tener muchas instancias de BD
    public ICollection<DatabaseInstance> DatabaseInstances { get; set; } = new List<DatabaseInstance>();
}