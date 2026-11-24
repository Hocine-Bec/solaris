using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

/// <summary>
/// EF Core's defaults on purpose (P4a's before): a surrogate key and the index the foreign key brings, so nothing
/// stops one slot being stored twice. Only the names are explicit, as every table's are.
/// </summary>
public class EnergyReadingConfig : IEntityTypeConfiguration<EnergyReading>
{
    public void Configure(EntityTypeBuilder<EnergyReading> builder)
    {
        builder.ToTable("energy_readings");

        builder.HasKey(r => r.Id).HasName("pk_energy_readings");

        builder.Property(r => r.Id)
            .HasColumnName("id");

        builder.Property(r => r.InstallationId)
            .HasColumnName("installation_id")
            .IsRequired();

        builder.Property(r => r.RecordedAt)
            .HasColumnName("recorded_at")
            .IsRequired();

        builder.Property(r => r.EnergyKwh)
            .HasColumnName("energy_kwh")
            .HasPrecision(10, 4)
            .IsRequired();

        builder.HasOne(r => r.Installation)
            .WithMany()
            .HasForeignKey(r => r.InstallationId)
            .HasConstraintName("fk_energy_readings_installations_installation_id")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(r => r.InstallationId)
            .HasDatabaseName("ix_energy_readings_installation_id");
    }
}
