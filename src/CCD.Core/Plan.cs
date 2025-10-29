namespace CCD.Core;

public class Plan
{
    public int Id { get; set; } // Auto-increment integer en la base de datos
    public string Name { get; set; } = string.Empty; // "Free", "Basic", "Premium", etc.
    public int DatabaseLimitPerEngine { get; set; } // Límite por motor de base de datos
    public decimal Price { get; set; } // Precio del plan
    public string MercadoPagoPriceId { get; set; } = string.Empty; // ID de precio en Mercado Pago
    public int MaxDatabases { get; set; } // Límite total de bases de datos permitidas
    public bool IsActive { get; set; } = true;
}

