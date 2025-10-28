namespace CCD.Core;

public class DatabaseInstance
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty; // Valor por defecto
    public string Engine { get; set; } = string.Empty; // Valor por defecto
    public string Status { get; set; } = string.Empty; // Valor por defecto
    
    // Relación con el Usuario
    public Guid UserId { get; set; }

    // Aquí usamos 'null!' (el operador "damn-it") para decirle al compilador:
    // "Confía en mí, sé que esta propiedad 'User' no será nula cuando la usemos de verdad,
    // porque Entity Framework se encargará de cargarla".
    public User User { get; set; } = null!;
}