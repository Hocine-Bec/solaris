using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

public class EquipmentConfiguration : IEntityTypeConfiguration<Equipment>
{
    public void Configure(EntityTypeBuilder<Equipment> builder)
    {
        builder.ToTable("equipment");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Type)
            .HasColumnName("type")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.Model)
            .HasColumnName("model")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.SerialNumber)
            .HasColumnName("serial_number")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.Manufacturer)
            .HasColumnName("manufacturer")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.PurchaseDate)
            .HasColumnName("purchase_date")
            .IsRequired();

        builder.Property(e => e.WarrantyExpiryDate)
            .HasColumnName("warranty_expiry_date");

        builder.Property(e => e.Cost)
            .HasColumnName("cost")
            .HasPrecision(10, 2)
            .IsRequired();

        builder.Property(e => e.Specifications)
            .HasColumnName("specifications")
            .HasColumnType("jsonb"); // PostgreSQL JSON column

        // Indexes
        builder.HasIndex(e => e.SerialNumber)
            .IsUnique()
            .HasDatabaseName("ix_equipment_serial_number");

        builder.HasIndex(e => e.Type)
            .HasDatabaseName("ix_equipment_type");

        builder.HasIndex(e => e.Status)
            .HasDatabaseName("ix_equipment_status");

        builder.HasIndex(e => e.InstallationId)
            .HasDatabaseName("ix_equipment_installation_id");

        // Relationship configured in InstallationConfiguration
    }
}