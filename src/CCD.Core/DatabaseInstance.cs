namespace CCD.Core;

public class DatabaseInstance
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty; // Nombre de la base de datos
    public string Engine { get; set; } = string.Empty; // Motor (postgresql, mysql, sqlserver)
    public string Status { get; set; } = string.Empty; // Estado (Active, Deleted, etc.)
    public string DbUsername { get; set; } = string.Empty; // Usuario de la base de datos
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string TimeZoneId { get; set; } = "UTC"; // Zona horaria del usuario al crear la instancia
    public bool CredentialsViewed { get; set; } = false; // Flag para controlar si las credenciales ya fueron vistas
    
    // Relación con el Usuario
    public Guid UserId { get; set; }

    // Aquí usamos 'null!' (el operador "damn-it") para decirle al compilador:
    // "Confía en mí, sé que esta propiedad 'User' no será nula cuando la usemos de verdad,
    // porque Entity Framework se encargará de cargarla".
    public User User { get; set; } = null!;
}