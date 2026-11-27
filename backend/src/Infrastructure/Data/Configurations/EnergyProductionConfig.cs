using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

/// <summary>
/// One row per installation and local day (FR-P1): the pair is the key. The fleet's view reads one day across every
/// installation, which the key, led by the installation, can't serve: that is the index on the day.
/// </summary>
public class EnergyProductionConfig : IEntityTypeConfiguration<EnergyProduction>
{
    public void Configure(EntityTypeBuilder<EnergyProduction> builder)
    {
        builder.ToTable("energy_production");

        builder.HasKey(p => new { p.InstallationId, p.Day }).HasName("pk_energy_production");

        builder.Property(p => p.InstallationId).HasColumnName("installation_id");
        builder.Property(p => p.Day).HasColumnName("day");
        builder.Property(p => p.ActualWh).HasColumnName("actual_wh").IsRequired();
        builder.Property(p => p.ReadingCount).HasColumnName("reading_count").IsRequired();
        builder.Property(p => p.ComputedAt).HasColumnName("computed_at").IsRequired();

        builder.HasIndex(p => p.Day).HasDatabaseName("ix_energy_production_day");

        builder.HasOne(p => p.Installation)
            .WithMany()
            .HasForeignKey(p => p.InstallationId)
            .HasConstraintName("fk_energy_production_installations_installation_id")
            .OnDelete(DeleteBehavior.Cascade);
    }
}
