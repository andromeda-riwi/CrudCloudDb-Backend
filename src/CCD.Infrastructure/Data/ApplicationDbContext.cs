using CCD.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration; 
using System.IO; 

namespace CCD.Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }
    
    public DbSet<User> Users { get; set; }
    public DbSet<Plan> Plans { get; set; }
    public DbSet<DatabaseInstance> DatabaseInstances { get; set; }
    public DbSet<Webhook> Webhooks { get; set; }
    public DbSet<WebhookEvent> WebhookEvents { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.Entity<Plan>(entity =>
        {
            entity.ToTable("Plans");

            entity.HasKey(p => p.Id);

            entity.Property(p => p.Name).IsRequired().HasMaxLength(50);
            
            entity.Property(p => p.Price).HasColumnType("decimal(18,2)");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");
            entity.HasKey(u => u.Id);

            entity.HasIndex(u => u.Email).IsUnique();
            entity.Property(u => u.Email).IsRequired();
            entity.Property(u => u.PasswordHash).IsRequired();
            entity.Property(u => u.PasswordSalt).IsRequired();
        });
        
        modelBuilder.Entity<DatabaseInstance>(entity =>
        {
            entity.ToTable("DatabaseInstances");
            entity.HasKey(d => d.Id);
            entity.Property(d => d.Name).IsRequired();
            entity.Property(d => d.Engine).IsRequired();
            entity.Property(d => d.TimeZoneId).IsRequired();
        });

        modelBuilder.Entity<Webhook>(entity =>
        {
            entity.ToTable("Webhooks");
            entity.HasKey(w => w.Id);
            entity.HasIndex(w => w.UserId);
            entity.Property(w => w.Url).IsRequired();
            entity.Property(w => w.Secret).IsRequired();
        });

        modelBuilder.Entity<WebhookEvent>(entity =>
        {
            entity.ToTable("WebhookEvents");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.WebhookId);
            entity.Property(e => e.EventType).IsRequired();
            entity.Property(e => e.Payload).IsRequired();
        });
        
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.ToTable("AuditLogs");
            entity.HasKey(a => a.Id);
            entity.HasIndex(a => a.UserId);
            entity.HasIndex(a => a.Timestamp);
            entity.Property(a => a.Action).IsRequired().HasMaxLength(100);
            entity.Property(a => a.EntityType).IsRequired().HasMaxLength(50);
            entity.Property(a => a.IpAddress).HasMaxLength(50);
        });
        
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

        modelBuilder.Entity<User>()
            .HasMany(u => u.Webhooks)
            .WithOne(w => w.User)
            .HasForeignKey(w => w.UserId)
            .IsRequired();

        modelBuilder.Entity<Webhook>()
            .HasMany(w => w.Events)
            .WithOne(e => e.Webhook)
            .HasForeignKey(e => e.WebhookId)
            .IsRequired();

 
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