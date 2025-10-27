using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

public class LeadConfiguration : IEntityTypeConfiguration<Lead>
{
    public void Configure(EntityTypeBuilder<Lead> builder)
    {
        builder.ToTable("leads");

        builder.HasKey(l => l.Id);

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

        builder.Property(l => l.ConvertedAt)
            .HasColumnName("converted_at");

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

        builder.HasIndex(l => l.CreatedAt)
            .HasDatabaseName("ix_leads_created_at");

        builder.HasIndex(l => l.CustomerId)
            .HasDatabaseName("ix_leads_customer_id");

        builder.HasIndex(l => new { l.Status, l.CreatedAt })
            .HasDatabaseName("ix_leads_status_created_at");

        // Relationships

        // One Lead has one Address (required)
        builder.HasOne(l => l.Address)
            .WithMany()
            .HasForeignKey(l => l.AddressId)
            .OnDelete(DeleteBehavior.Restrict);

        // One Lead can be converted to one Customer (optional)
        builder.HasOne(l => l.Customer)
            .WithMany()
            .HasForeignKey(l => l.CustomerId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}