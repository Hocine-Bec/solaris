using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

/// <summary>
/// The installation and the slot are the key (BR-T1): two devices sending one slot at the same moment can't both
/// store it, and the index the key brings is the one every read of a system's readings wants.
/// </summary>
public class EnergyReadingConfig : IEntityTypeConfiguration<EnergyReading>
{
    public void Configure(EntityTypeBuilder<EnergyReading> builder)
    {
        builder.ToTable("energy_readings");

        builder.HasKey(r => new { r.InstallationId, r.SlotStart }).HasName("pk_energy_readings");

        builder.Property(r => r.InstallationId)
            .HasColumnName("installation_id");

        builder.Property(r => r.SlotStart)
            .HasColumnName("slot_start");

        builder.Property(r => r.EnergyWh)
            .HasColumnName("energy_wh")
            .IsRequired();

        builder.Ignore(r => r.Kwh);

        builder.HasOne(r => r.Installation)
            .WithMany()
            .HasForeignKey(r => r.InstallationId)
            .HasConstraintName("fk_energy_readings_installations_installation_id")
            .OnDelete(DeleteBehavior.Cascade);
    }
}
