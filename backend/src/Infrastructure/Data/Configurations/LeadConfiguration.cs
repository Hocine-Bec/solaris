using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

public class LeadConfiguration : IEntityTypeConfiguration<Lead>
{
    /// <summary>The partial unique index behind BR-L1: a lead refused by it is a duplicate of an open one.</summary>
    public const string OpenEmailIndex = "ux_leads_open_normalized_email";

    public void Configure(EntityTypeBuilder<Lead> builder)
    {
        builder.ToTable("leads");

        builder.Ignore(l => l.FullName);
        builder.Ignore(l => l.IsOpen);
        builder.Ignore(l => l.NextStatuses);

        builder.HasKey(l => l.Id)
            .HasName("pk_leads");

        builder.Property(l => l.Id)
            .HasColumnName("id");

        builder.Property(l => l.FirstName)
            .HasColumnName("first_name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(l => l.LastName)
            .HasColumnName("last_name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(l => l.Email)
            .HasColumnName("email")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(l => l.NormalizedEmail)
            .HasColumnName("normalized_email")
            .HasMaxLength(255)
            .IsRequired();

        // PostgreSQL's xmin system column: a save made on an older version fails (no column of its own)
        builder.Property(l => l.Version)
            .IsRowVersion();

        builder.Property(l => l.PhoneNumber)
            .HasColumnName("phone_number")
            .HasMaxLength(20)
            .IsRequired();
        
        builder.Property(l => l.AddressId)
            .HasColumnName("address_id")
            .IsRequired();

        builder.Property(l => l.PropertyType)
            .HasColumnName("property_type")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(l => l.IsPropertyOwner)
            .HasColumnName("is_property_owner")
            .IsRequired();

        builder.Property(l => l.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(l => l.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(l => l.ContactedAt)
            .HasColumnName("contacted_at");

        builder.Property(l => l.ClosedAt)
            .HasColumnName("closed_at");

        builder.Property(l => l.MonthlyBillRange)
            .HasColumnName("monthly_bill_range")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(l => l.BestTimeToContact)
            .HasColumnName("best_time_to_contact")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(l => l.Notes)
            .HasColumnName("notes")
            .HasColumnType("text");

        builder.Property(l => l.CustomerId)
            .HasColumnName("customer_id");

        // Indexes
        builder.HasIndex(l => l.Status)
            .HasDatabaseName("ix_leads_status");

        // BR-L1, enforced by the database: one open lead per email, even when the form is sent many times at once
        builder.HasIndex(l => l.NormalizedEmail)
            .IsUnique()
            .HasFilter("status IN ('New', 'Contacted')")
            .HasDatabaseName(OpenEmailIndex);

        builder.HasIndex(l => l.CreatedAt)
            .HasDatabaseName("ix_leads_created_at");

        builder.HasIndex(l => l.CustomerId)
            .HasDatabaseName("ix_leads_customer_id");

        builder.HasIndex(l => new { l.Status, l.CreatedAt })
            .HasDatabaseName("ix_leads_status_created_at");

        builder.HasIndex(l => l.AddressId)
            .HasDatabaseName("ix_leads_address_id");

        // Relationships

        // One Lead has one Address (required)
        builder.HasOne(l => l.Address)
            .WithMany()
            .HasForeignKey(l => l.AddressId)
            .HasConstraintName("fk_leads_addresses_address_id")
            .OnDelete(DeleteBehavior.Restrict);

        // One Lead can be converted to one Customer (optional)
        builder.HasOne(l => l.Customer)
            .WithMany()
            .HasForeignKey(l => l.CustomerId)
            .HasConstraintName("fk_leads_customers_customer_id")
            .OnDelete(DeleteBehavior.SetNull);
    }
}