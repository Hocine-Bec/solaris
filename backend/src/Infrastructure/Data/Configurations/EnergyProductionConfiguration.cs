using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

public class EnergyProductionConfiguration : IEntityTypeConfiguration<EnergyProduction>
{
    public void Configure(EntityTypeBuilder<EnergyProduction> builder)
    {
        builder.ToTable("energy_production");

        builder.HasKey(ep => ep.Id);

        builder.Property(ep => ep.ProductionDate)
            .HasColumnName("production_date")
            .IsRequired();

        builder.Property(ep => ep.ActualProductionKwh)
            .HasColumnName("actual_production_kwh")
            .HasPrecision(10, 2)
            .IsRequired();

        builder.Property(ep => ep.ExpectedProductionKwh)
            .HasColumnName("expected_production_kwh")
            .HasPrecision(10, 2)
            .IsRequired();

        builder.Property(ep => ep.HealthStatus)
            .HasColumnName("health_status")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(ep => ep.Notes)
            .HasColumnName("notes")
            .HasColumnType("text");

        // Indexes
        builder.HasIndex(ep => ep.InstallationId)
            .HasDatabaseName("ix_energy_production_installation_id");

        builder.HasIndex(ep => ep.ProductionDate)
            .HasDatabaseName("ix_energy_production_date");

        // Unique constraint: one production record per installation per date
        builder.HasIndex(ep => new { ep.InstallationId, ep.ProductionDate })
            .IsUnique()
            .HasDatabaseName("ix_energy_production_installation_date");

        // Relationships
        builder.HasOne(ep => ep.WeatherData)
            .WithMany()
            .HasForeignKey(ep => ep.WeatherDataId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}