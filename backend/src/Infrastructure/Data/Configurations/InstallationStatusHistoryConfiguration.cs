using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

public class InstallationStatusHistoryConfiguration : IEntityTypeConfiguration<InstallationStatusHistory>
{
    public void Configure(EntityTypeBuilder<InstallationStatusHistory> builder)
    {
        builder.ToTable("installation_status_history");

        builder.HasKey(h => h.Id);

        builder.Property(h => h.FromStatus)
            .HasColumnName("from_status")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(h => h.ToStatus)
            .HasColumnName("to_status")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(h => h.ChangedAt)
            .HasColumnName("changed_at")
            .IsRequired();

        builder.Property(h => h.Notes)
            .HasColumnName("notes")
            .HasColumnType("text");

        // Indexes
        builder.HasIndex(h => h.InstallationId)
            .HasDatabaseName("ix_installation_status_history_installation_id");

        builder.HasIndex(h => h.ChangedAt)
            .HasDatabaseName("ix_installation_status_history_changed_at");

        builder.HasIndex(h => new { h.InstallationId, h.ChangedAt })
            .HasDatabaseName("ix_installation_status_history_installation_changed");

        // Relationships configured in InstallationConfiguration and UserConfiguration
    }
}