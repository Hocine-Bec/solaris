using Domain.Entities;
using Infrastructure.Measurement;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Customer> Customers { get; set; }
    public DbSet<Address> Addresses { get; set; }
    public DbSet<Lead> Leads { get; set; }
    public DbSet<Installation> Installations { get; set; }
    public DbSet<User> Users { get; set; }

    /// <summary>Notifications waiting to be sent, saved with the event that causes them (BR-N1).</summary>
    public DbSet<OutboxMessage> OutboxMessages { get; set; }

    /// <summary>Written only by the measurement harness, in Development; empty elsewhere.</summary>
    public DbSet<EmailDelivery> EmailDeliveries { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}