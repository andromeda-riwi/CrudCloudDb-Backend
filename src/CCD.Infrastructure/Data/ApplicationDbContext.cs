using CCD.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration; // <-- Importante añadir esto
using System.IO; // <-- Importante añadir esto

namespace CCD.Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    // --- CONSTRUCTOR ÚNICO ---
    // Este constructor es utilizado tanto por la aplicación en tiempo de ejecución
    // como por las herramientas de diseño de EF Core (por ejemplo, para crear migraciones).
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    // --- DEFINICIÓN DE LAS TABLAS (DbSets) ---
    // Cada DbSet<T> representa una tabla en la base de datos.
    public DbSet<User> Users { get; set; }
    public DbSet<Plan> Plans { get; set; }
    public DbSet<DatabaseInstance> DatabaseInstances { get; set; }

    // --- CONFIGURACIÓN DEL MODELO DE DATOS (OnModelCreating) ---
    // Este método se utiliza para configurar el modelo de datos de forma explícita.
    // Aquí definimos relaciones, claves, índices y datos iniciales (seeding).
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // --- Configuración de la Entidad 'Plan' ---
        modelBuilder.Entity<Plan>(entity =>
        {
            // Define el nombre de la tabla
            entity.ToTable("Plans");

            // Configura la clave primaria
            entity.HasKey(p => p.Id);

            // Configura la propiedad 'Name' para que sea requerida y tenga un máximo de 50 caracteres
            entity.Property(p => p.Name).IsRequired().HasMaxLength(50);
            
            // Configura la propiedad 'Price' con precisión para valores decimales
            entity.Property(p => p.Price).HasColumnType("decimal(18,2)");
        });

        // --- Configuración de la Entidad 'User' ---
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");
            entity.HasKey(u => u.Id);

            // Crea un índice único en la columna 'Email' para asegurar que no haya correos duplicados
            entity.HasIndex(u => u.Email).IsUnique();
            entity.Property(u => u.Email).IsRequired();

            entity.Property(u => u.PasswordHash).IsRequired();
            entity.Property(u => u.PasswordSalt).IsRequired();
        });

        // --- Configuración de la Entidad 'DatabaseInstance' ---
        modelBuilder.Entity<DatabaseInstance>(entity =>
        {
            entity.ToTable("DatabaseInstances");
            entity.HasKey(d => d.Id);
            entity.Property(d => d.Name).IsRequired();
            entity.Property(d => d.Engine).IsRequired();
        });

        // --- DEFINICIÓN DE RELACIONES ---

        // Relación Uno-a-Muchos: Un Plan tiene muchos Usuarios
        modelBuilder.Entity<Plan>()
            .HasMany(p => p.Users)      // Un Plan tiene una colección de Usuarios
            .WithOne(u => u.Plan)       // Cada Usuario tiene una sola propiedad de navegación 'Plan'
            .HasForeignKey(u => u.PlanId) // La clave foránea en la tabla 'Users' es 'PlanId'
            .IsRequired();              // Un usuario debe tener un plan

        // Relación Uno-a-Muchos: Un Usuario tiene muchas DatabaseInstances
        modelBuilder.Entity<User>()
            .HasMany(u => u.DatabaseInstances)
            .WithOne(d => d.User)
            .HasForeignKey(d => d.UserId)
            .IsRequired();

        // --- POBLACIÓN DE DATOS INICIALES (SEEDING) ---
        // Estos datos se insertarán en la tabla 'Plans' la primera vez que se cree la base de datos.
        modelBuilder.Entity<Plan>().HasData(
            new Plan 
            { 
                Id = 1, 
                Name = "Gratuito", 
                DatabaseLimitPerEngine = 2, 
                Price = 0.00m, // Correcto
                MercadoPagoPriceId = "N/A"
            },
            new Plan 
            { 
                Id = 2, 
                Name = "Intermedio", 
                DatabaseLimitPerEngine = 5, 
                Price = 5000.00m, 
                MercadoPagoPriceId = "price_id_intermedio" // Este valor no se usa en tu lógica actual, pero es bueno tenerlo
            },
            new Plan 
            { 
                Id = 3, 
                Name = "Avanzado", 
                DatabaseLimitPerEngine = 10, 
                Price = 10000.00m, // Correcto: $10.000 COP
                MercadoPagoPriceId = "price_id_avanzado" // Este valor no se usa en tu lógica actual
            }
        );
    }
}