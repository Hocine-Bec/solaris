using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

public class InstallationConfig : IEntityTypeConfiguration<Installation>
{
    public void Configure(EntityTypeBuilder<Installation> builder)
    {
        builder.ToTable("installations");

        builder.HasKey(i => i.Id)
            .HasName("pk_installations");

        builder.Property(i => i.Id)
            .HasColumnName("id");

        builder.Property(i => i.CustomerId)
            .HasColumnName("customer_id");

        builder.Property(i => i.AddressId)
            .HasColumnName("address_id");

        builder.Property(i => i.LeadId)
            .HasColumnName("lead_id");

        builder.Property(i => i.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        // An IANA name, such as America/Chicago
        builder.Property(i => i.TimeZone)
            .HasColumnName("time_zone")
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(i => i.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.HasIndex(i => i.CustomerId)
            .HasDatabaseName("ix_installations_customer_id");

        builder.HasIndex(i => i.AddressId)
            .HasDatabaseName("ix_installations_address_id");

        builder.HasIndex(i => i.LeadId)
            .HasDatabaseName("ix_installations_lead_id");

        builder.HasIndex(i => i.Status)
            .HasDatabaseName("ix_installations_status");

        builder.HasOne(i => i.Customer)
            .WithMany(c => c.Installations)
            .HasForeignKey(i => i.CustomerId)
            .HasConstraintName("fk_installations_customers_customer_id")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.Address)
            .WithMany()
            .HasForeignKey(i => i.AddressId)
            .HasConstraintName("fk_installations_addresses_address_id")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.Lead)
            .WithMany()
            .HasForeignKey(i => i.LeadId)
            .HasConstraintName("fk_installations_leads_lead_id")
            .OnDelete(DeleteBehavior.Restrict);
    }
}
