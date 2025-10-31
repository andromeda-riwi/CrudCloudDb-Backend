using CCD.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration; // <-- Importante añadir esto
using System.IO; // <-- Importante añadir esto

namespace CCD.Infrastructure.Data
{
    public class ApplicationDbContext : DbContext
    {
        // --- CONSTRUCTOR ÚNICO ---
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        // --- DEFINICIÓN DE LAS TABLAS (DbSets) ---
        public DbSet<User> Users { get; set; }
        public DbSet<Plan> Plans { get; set; }
        public DbSet<DatabaseInstance> DatabaseInstances { get; set; }

        // --- CONFIGURACIÓN DEL MODELO DE DATOS (OnModelCreating) ---
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // --- Configuración de la Entidad 'Plan' ---
            modelBuilder.Entity<Plan>(entity =>
            {
                entity.ToTable("Plans");
                entity.HasKey(p => p.Id);
                entity.Property(p => p.Name).IsRequired().HasMaxLength(50);
                entity.Property(p => p.Price).HasColumnType("decimal(18,2)");
            });

            // --- Configuración de la Entidad 'User' ---
            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("Users");
                entity.HasKey(u => u.Id);

                entity.HasIndex(u => u.Email).IsUnique();
                entity.Property(u => u.Email).IsRequired();

                // --- ESTA ES LA CORRECCIÓN ---
                entity.Property(u => u.PasswordHash).IsRequired();
                // La siguiente línea ha sido eliminada porque PasswordSalt ya no existe en la clase User.
                // entity.Property(u => u.PasswordSalt).IsRequired();
                // -----------------------------
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
            modelBuilder.Entity<Plan>()
                .HasMany(p => p.Users)
                .WithOne(u => u.Plan)
                .HasForeignKey(u => u.PlanId)
                .IsRequired();

            modelBuilder.Entity<User>()
                .HasMany(u => u.DatabaseInstances)
                .WithOne(d => d.User)
                .HasForeignKey(d => d.UserId)
                .IsRequired();

            // --- POBLACIÓN DE DATOS INICIALES (SEEDING) ---
            modelBuilder.Entity<Plan>().HasData(
                new Plan 
                { 
                    Id = 1, 
                    Name = "Gratuito", 
                    DatabaseLimitPerEngine = 2, 
                    Price = 0.00m, 
                    MercadoPagoPriceId = "N/A"
                },
                new Plan 
                { 
                    Id = 2, 
                    Name = "Intermedio", 
                    DatabaseLimitPerEngine = 5, 
                    Price = 5000.00m, 
                    MercadoPagoPriceId = "price_id_intermedio"
                },
                new Plan 
                { 
                    Id = 3, 
                    Name = "Avanzado", 
                    DatabaseLimitPerEngine = 10, 
                    Price = 10000.00m, 
                    MercadoPagoPriceId = "price_id_avanzado"
                }
            );
        }
    }
}