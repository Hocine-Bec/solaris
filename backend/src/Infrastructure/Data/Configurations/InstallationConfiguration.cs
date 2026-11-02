using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

public class InstallationConfiguration : IEntityTypeConfiguration<Installation>
{
    public void Configure(EntityTypeBuilder<Installation> builder)
    {
        builder.ToTable("installations");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.Id)
            .HasColumnName("id");

        builder.Property(i => i.ProjectName)
            .HasColumnName("project_name")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(i => i.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(i => i.StartDate)
            .HasColumnName("start_date")
            .IsRequired();

        builder.Property(i => i.CompletionDate)
            .HasColumnName("completion_date");

        builder.Property(i => i.SystemSizeKw)
            .HasColumnName("system_size_kw")
            .HasPrecision(10, 2)
            .IsRequired();

        builder.Property(i => i.PanelCount)
            .HasColumnName("panel_count")
            .IsRequired();

        builder.Property(i => i.InverterType)
            .HasColumnName("inverter_type")
            .HasMaxLength(100);

        builder.Property(i => i.Notes)
            .HasColumnName("notes")
            .HasColumnType("text");

        builder.Property(i => i.CustomerId)
            .HasColumnName("customer_id")
            .IsRequired();

        builder.Property(i => i.InstallationAddressId)
            .HasColumnName("installation_address_id")
            .IsRequired();

        // Indexes
        builder.HasIndex(i => i.CustomerId)
            .HasDatabaseName("ix_installations_customer_id");

        builder.HasIndex(i => i.Status)
            .HasDatabaseName("ix_installations_status");

        builder.HasIndex(i => new { i.Status, i.StartDate })
            .HasDatabaseName("ix_installations_status_start_date");

        // Relationships

        // Many Installations belong to one Customer (already configured in CustomerConfiguration)
        // We just need to configure the other side

        // One Installation has one InstallationAddress (required)
        builder.HasOne(i => i.InstallationAddress)
            .WithMany()
            .HasForeignKey(i => i.InstallationAddressId)
            .OnDelete(DeleteBehavior.Restrict); // Can't delete address if installation exists

        // One Installation has many StatusHistory records
        builder.HasMany(i => i.StatusHistory)
            .WithOne(h => h.Installation)
            .HasForeignKey(h => h.InstallationId)
            .OnDelete(DeleteBehavior.Cascade);

        // One Installation has many AssignedTechnicians (through junction table)
        builder.HasMany(i => i.AssignedTechnicians)
            .WithOne(it => it.Installation)
            .HasForeignKey(it => it.InstallationId)
            .OnDelete(DeleteBehavior.Cascade);

        // One Installation has many EnergyProductions
        builder.HasMany(i => i.EnergyProductions)
            .WithOne(ep => ep.Installation)
            .HasForeignKey(ep => ep.InstallationId)
            .OnDelete(DeleteBehavior.Cascade);

        // One Installation has many Equipment
        builder.HasMany(i => i.Equipment)
            .WithOne(e => e.Installation)
            .HasForeignKey(e => e.InstallationId)
            .OnDelete(DeleteBehavior.SetNull);

        // One Installation has many Documents
        builder.HasMany(i => i.Documents)
            .WithOne(d => d.Installation)
            .HasForeignKey(d => d.InstallationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}