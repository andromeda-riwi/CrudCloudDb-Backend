namespace CCD.Core
{
    public class DatabaseInstance
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Engine { get; set; } = string.Empty;

        // --- AÑADE ESTAS PROPIEDADES PARA GUARDAR LOS DETALLES DE CONEXIÓN ---
        public string Host { get; set; } = string.Empty;
        public int Port { get; set; }
        public string DbUsername { get; set; } = string.Empty;
        // Nota: Omitimos la contraseña por seguridad, como planeamos.
         public string Status { get; set; } = string.Empty;


        // Clave foránea para la relación con User
        public Guid UserId { get; set; }
        // Propiedad de navegación a User
        public User User { get; set; } = null!;
    }
}