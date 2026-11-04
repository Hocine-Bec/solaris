using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

public class CustomerConfig : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        // Table name
        builder.ToTable("customers");

        // Primary key
        builder.HasKey(c => c.Id)
            .HasName("pk_customers");

        // Properties configuration
        builder.Property(c => c.Id)
            .HasColumnName("id");

        builder.Property(c => c.FirstName)
            .HasColumnName("first_name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(c => c.LastName)
            .HasColumnName("last_name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(c => c.Email)
            .HasColumnName("email")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(c => c.PhoneNumber)
            .HasColumnName("phone_number")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(c => c.Status)
            .HasColumnName("status")
            .HasConversion<string>() // Store enum as string in database
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(c => c.RegistrationDate)
            .HasColumnName("registration_date")
            .IsRequired();

        builder.Property(c => c.LastActivityDate)
            .HasColumnName("last_activity_date");

        builder.Property(c => c.ContactAddressId)
            .HasColumnName("contact_address_id");

        // Indexes for performance
        builder.HasIndex(c => c.Email)
            .IsUnique()
            .HasDatabaseName("ix_customers_email");

        builder.HasIndex(c => c.Status)
            .HasDatabaseName("ix_customers_status");

        builder.HasIndex(c => c.ContactAddressId)
            .HasDatabaseName("ix_customers_contact_address_id");

        // Relationships

        // One Customer can have one ContactAddress (optional)
        builder.HasOne(c => c.ContactAddress)
            .WithMany()
            .HasForeignKey(c => c.ContactAddressId)
            .HasConstraintName("fk_customers_addresses_contact_address_id")
            .OnDelete(DeleteBehavior.SetNull); // If address deleted, set to null
    }
}