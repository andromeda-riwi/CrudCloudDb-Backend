namespace CCD.Core;

public class Plan
{
    public int Id { get; set; } // Un simple int es suficiente aquí (1=Gratuito, 2=Intermedio, 3=Avanzado)
    public string Name { get; set; } = string.Empty;
    public int DatabaseLimitPerEngine { get; set; } // Límite de BD (2, 5, 10)
    public decimal Price { get; set; } // Precio mensual (0, 5000, 10000)
    public string MercadoPagoPriceId { get; set; } = string.Empty; // ID del precio/suscripción en Mercado Pago
    
    // Propiedad de navegación: Un plan puede tener muchos usuarios
    public ICollection<User> Users { get; set; } = new List<User>();
}