using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

public class InstallationTechnicianConfiguration : IEntityTypeConfiguration<InstallationTechnician>
{
    public void Configure(EntityTypeBuilder<InstallationTechnician> builder)
    {
        builder.ToTable("installation_technicians");

        builder.HasKey(it => it.Id);

        builder.Property(it => it.AssignedDate)
            .HasColumnName("assigned_date")
            .IsRequired();

        builder.Property(it => it.CompletedDate)
            .HasColumnName("completed_date");

        builder.Property(it => it.Role)
            .HasColumnName("role")
            .HasMaxLength(100);

        builder.Property(it => it.Notes)
            .HasColumnName("notes")
            .HasColumnType("text");

        // Indexes
        builder.HasIndex(it => it.InstallationId)
            .HasDatabaseName("ix_installation_technicians_installation_id");

        builder.HasIndex(it => it.TechnicianId)
            .HasDatabaseName("ix_installation_technicians_technician_id");

        builder.HasIndex(it => new { it.InstallationId, it.TechnicianId })
            .HasDatabaseName("ix_installation_technicians_installation_technician");

        // Relationships configured in InstallationConfiguration and UserConfiguration
    }
}