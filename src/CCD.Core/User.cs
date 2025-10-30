namespace CCD.Core
{
    public class User
    {
        public Guid Id { get; set; }
        
        public string Name { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        
        // --- ESTA ES LA CORRECCIÓN ---
        // PasswordHash ahora es un string, para ser compatible con BCrypt.
        public string PasswordHash { get; set; } = string.Empty;
        // La propiedad PasswordSalt ya no es necesaria y debe ser eliminada.
        // -----------------------------
        
        // Relación con el Plan
        public int PlanId { get; set; } // Foreign Key
        public Plan Plan { get; set; } = null!; // Navigation Property

        // Propiedad de navegación
        public ICollection<DatabaseInstance> DatabaseInstances { get; set; } = new List<DatabaseInstance>();
    }
}