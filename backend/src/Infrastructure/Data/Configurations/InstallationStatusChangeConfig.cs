using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

/// <summary>An installation's history (BR-I3): one row per status change, only ever appended.</summary>
public class InstallationStatusChangeConfig : IEntityTypeConfiguration<InstallationStatusChange>
{
    public void Configure(EntityTypeBuilder<InstallationStatusChange> builder)
    {
        builder.ToTable("installation_status_history");

        builder.HasKey(h => h.Id).HasName("pk_installation_status_history");
        builder.Property(h => h.Id).HasColumnName("id");
        builder.Property(h => h.InstallationId).HasColumnName("installation_id");
        builder.Property(h => h.FromStatus).HasColumnName("from_status").HasConversion<string>().HasMaxLength(50);
        builder.Property(h => h.ToStatus).HasColumnName("to_status").HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(h => h.ChangedAt).HasColumnName("changed_at").IsRequired();
        builder.Property(h => h.ChangedByUserId).HasColumnName("changed_by_user_id");
        builder.Property(h => h.Note).HasColumnName("note").HasMaxLength(Installation.MaxNoteLength);

        builder.HasOne<Installation>()
            .WithMany(i => i.History)
            .HasForeignKey(h => h.InstallationId)
            .HasConstraintName("fk_installation_status_history_installations_installation_id")
            .OnDelete(DeleteBehavior.Restrict);

        // Accounts aren't deleted by the API; if one is, its changes stay, without a name
        builder.HasOne(h => h.ChangedBy)
            .WithMany()
            .HasForeignKey(h => h.ChangedByUserId)
            .HasConstraintName("fk_installation_status_history_users_changed_by_user_id")
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(h => new { h.InstallationId, h.ChangedAt })
            .HasDatabaseName("ix_installation_status_history_installation_id_changed_at");
        builder.HasIndex(h => h.ChangedByUserId)
            .HasDatabaseName("ix_installation_status_history_changed_by_user_id");
    }
}
