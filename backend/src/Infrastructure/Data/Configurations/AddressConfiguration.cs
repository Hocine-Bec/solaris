using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

public class AddressConfiguration : IEntityTypeConfiguration<Address>
{
    public void Configure(EntityTypeBuilder<Address> builder)
    {
        builder.ToTable("addresses");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Id)
            .HasColumnName("id");

        builder.Property(a => a.Street)
            .HasColumnName("street")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(a => a.City)
            .HasColumnName("city")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(a => a.State)
            .HasColumnName("state")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(a => a.ZipCode)
            .HasColumnName("zip_code")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(a => a.Country)
            .HasColumnName("country")
            .HasMaxLength(100)
            .IsRequired();
        
        builder.Property(a => a.Latitude)
            .HasColumnName("latitude")
            .HasPrecision(10, 7); // Total 10 digits, 7 after decimal

        builder.Property(a => a.Longitude)
            .HasColumnName("longitude")
            .HasPrecision(10, 7);
        
        builder.Property(a => a.UniqueAddressHash)
            .HasColumnName("unique_address_hash")
            .HasMaxLength(64);
        
        // Indexes
        builder.HasIndex(a => new { a.City, a.State })
            .HasDatabaseName("ix_addresses_city_state");

        builder.HasIndex(a => a.UniqueAddressHash)
            .HasDatabaseName("ux_addresses_unique_address_hash")
            .IsUnique();
        
        builder.HasIndex(a => a.ZipCode)
            .HasDatabaseName("ix_addresses_zip_code");
    }
}