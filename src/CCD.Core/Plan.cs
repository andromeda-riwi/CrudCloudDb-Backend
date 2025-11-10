namespace CCD.Core;

public class Plan
{
    public int Id { get; set; } 
    public string Name { get; set; } = string.Empty;
    public int DatabaseLimitPerEngine { get; set; } 
    public decimal Price { get; set; } 
    public string MercadoPagoPriceId { get; set; } = string.Empty; 
    
    public ICollection<User> Users { get; set; } = new List<User>();
}