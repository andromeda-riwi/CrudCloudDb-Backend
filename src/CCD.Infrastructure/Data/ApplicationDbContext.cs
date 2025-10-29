using CCD.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration; // <-- Importante añadir esto
using System.IO; // <-- Importante añadir esto

namespace CCD.Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    // --- CONSTRUCTOR PRINCIPAL ---
    // Este es el que usa tu aplicación en tiempo de ejecución, inyectado desde Program.cs
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    // --- CONSTRUCTOR SECUNDARIO (para herramientas de diseño) ---
    // ▼▼▼ AÑADE ESTE CONSTRUCTOR VACÍO ▼▼▼
    // Este constructor no hace nada, permitiendo que el método OnConfiguring se encargue.
    public ApplicationDbContext() { }
    // ▲▲▲ ▲▲▲ ▲▲▲ ▲▲▲ ▲▲▲ ▲▲▲ ▲▲▲ ▲▲▲ ▲▲▲

    // Tablas que Entity Framework gestionará
    public DbSet<User> Users { get; set; }
    public DbSet<Plan> Plans { get; set; }
    public DbSet<DatabaseInstance> DatabaseInstances { get; set; }
    
    // --- MÉTODO DE CONFIGURACIÓN DE RESPALDO ---
    // ▼▼▼ AÑADE ESTE MÉTODO COMPLETO ▼▼▼
    // Este método solo se llama si el DbContext se crea usando el constructor vacío.
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            // Construimos la configuración para encontrar el appsettings.json
            IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "../CCD.Api"))
                .AddJsonFile("appsettings.json")
                .Build();
            
            // Obtenemos la cadena de conexión
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            
            // Configuramos el proveedor de base de datos
            optionsBuilder.UseNpgsql(connectionString);
        }
    }
    // ▲▲▲ ▲▲▲ ▲▲▲ ▲▲▲ ▲▲▲ ▲▲▲ ▲▲▲ ▲▲▲ ▲▲▲
}