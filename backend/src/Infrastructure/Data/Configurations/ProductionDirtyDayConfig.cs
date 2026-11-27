using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

public class ProductionDirtyDayConfig : IEntityTypeConfiguration<ProductionDirtyDay>
{
    public void Configure(EntityTypeBuilder<ProductionDirtyDay> builder)
    {
        builder.ToTable("production_dirty_days");

        builder.HasKey(d => new { d.InstallationId, d.Day }).HasName("pk_production_dirty_days");

        builder.Property(d => d.InstallationId)
            .HasColumnName("installation_id");

        builder.Property(d => d.Day)
            .HasColumnName("day");

        builder.HasOne(d => d.Installation)
            .WithMany()
            .HasForeignKey(d => d.InstallationId)
            .HasConstraintName("fk_production_dirty_days_installations_installation_id")
            .OnDelete(DeleteBehavior.Cascade);
    }
}
