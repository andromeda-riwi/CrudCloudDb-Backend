namespace CCD.Core;

public class User
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public byte[] PasswordHash { get; set; } = Array.Empty<byte>();
    public byte[] PasswordSalt { get; set; } = Array.Empty<byte>();
    
    // Relación con el Plan
    public int PlanId { get; set; } // Cambiado a int para coincidir con la base de datos
    public Plan Plan { get; set; } = null!;
}