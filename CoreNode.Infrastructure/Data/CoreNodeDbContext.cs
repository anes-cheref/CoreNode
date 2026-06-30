using CoreNode.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CoreNode.Infrastructure.Data;

public class CoreNodeDbContext : DbContext
{   
    // Le constructeur qui permet de passer la chaîne de connexion
    public CoreNodeDbContext(DbContextOptions<CoreNodeDbContext> options) : base(options){ }
    
    public DbSet<Tenant> Tenants { get; set; }
    public DbSet<VirtualMachine> VirtualMachines { get; set; }
    public DbSet<ProxmoxTask> Tasks { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<VirtualMachine>()
            .Property(v => v.Status)
            .HasConversion<string>();

        modelBuilder.Entity<ProxmoxTask>()
            .Property(t => t.Type)
            .HasConversion<string>();

        modelBuilder.Entity<ProxmoxTask>()
            .Property(t => t.Status)
            .HasConversion<string>();
    }
}