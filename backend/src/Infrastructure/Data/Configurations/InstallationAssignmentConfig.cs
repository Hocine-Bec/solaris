using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

/// <summary>Technicians' parts on installations (FR-I5, FR-I6).</summary>
public class InstallationAssignmentConfig : IEntityTypeConfiguration<InstallationAssignment>
{
    public void Configure(EntityTypeBuilder<InstallationAssignment> builder)
    {
        builder.ToTable("installation_assignments");

        builder.HasKey(a => a.Id).HasName("pk_installation_assignments");
        builder.Property(a => a.Id).HasColumnName("id");
        builder.Property(a => a.InstallationId).HasColumnName("installation_id");
        builder.Property(a => a.TechnicianId).HasColumnName("technician_id");
        builder.Property(a => a.Role).HasColumnName("role").HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(a => a.AssignedAt).HasColumnName("assigned_at").IsRequired();
        builder.Property(a => a.AssignedByUserId).HasColumnName("assigned_by_user_id");
        builder.Property(a => a.CompletedAt).HasColumnName("completed_at");
        builder.Property(a => a.CompletedByUserId).HasColumnName("completed_by_user_id");
        builder.Ignore(a => a.IsCompleted);
        builder.Ignore(a => a.Stage);

        // An assignment removed from its installation (a mistaken one, never completed) is deleted by EF; the database
        // deletes nothing on its own (installations are never deleted)
        builder.HasOne(a => a.Installation)
            .WithMany(i => i.Assignments)
            .HasForeignKey(a => a.InstallationId)
            .HasConstraintName("fk_installation_assignments_installations_installation_id")
            .OnDelete(DeleteBehavior.ClientCascade);

        builder.HasOne(a => a.Technician)
            .WithMany()
            .HasForeignKey(a => a.TechnicianId)
            .HasConstraintName("fk_installation_assignments_users_technician_id")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.AssignedBy)
            .WithMany()
            .HasForeignKey(a => a.AssignedByUserId)
            .HasConstraintName("fk_installation_assignments_users_assigned_by_user_id")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.CompletedBy)
            .WithMany()
            .HasForeignKey(a => a.CompletedByUserId)
            .HasConstraintName("fk_installation_assignments_users_completed_by_user_id")
            .OnDelete(DeleteBehavior.Restrict);

        // One technician once per role on an installation, even when two Admins assign at the same moment
        builder.HasIndex(a => new { a.InstallationId, a.TechnicianId, a.Role })
            .IsUnique()
            .HasDatabaseName("ux_installation_assignments_installation_technician_role");
        builder.HasIndex(a => a.TechnicianId).HasDatabaseName("ix_installation_assignments_technician_id");
        builder.HasIndex(a => a.AssignedByUserId).HasDatabaseName("ix_installation_assignments_assigned_by_user_id");
        builder.HasIndex(a => a.CompletedByUserId).HasDatabaseName("ix_installation_assignments_completed_by_user_id");
    }
}
