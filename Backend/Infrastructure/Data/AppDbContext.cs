using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Customer> Customers { get; set; }
    public DbSet<Address> Addresses { get; set; }
    public DbSet<Installation> Installations { get; set; }
    public DbSet<InstallationStatusHistory> InstallationStatusHistories { get; set; }
    public DbSet<InstallationTechnician> InstallationTechnicians { get; set; }
    public DbSet<Equipment> Equipment { get; set; }
    public DbSet<EnergyProduction> EnergyProductions { get; set; }
    public DbSet<WeatherData> WeatherData { get; set; }
    public DbSet<SupportTicket> SupportTickets { get; set; }
    public DbSet<Document> Documents { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<Lead> Leads { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}