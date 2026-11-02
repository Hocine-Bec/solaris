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
        builder.HasKey(c => c.Id);

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

        // Relationships
            
        // One Customer can have one ContactAddress (optional)
        builder.HasOne(c => c.ContactAddress)
            .WithMany()
            .HasForeignKey(c => c.ContactAddressId)
            .OnDelete(DeleteBehavior.SetNull); // If address deleted, set to null

        // One Customer can have many Installations
        builder.HasMany(c => c.Installations)
            .WithOne(i => i.Customer)
            .HasForeignKey(i => i.CustomerId)
            .OnDelete(DeleteBehavior.Restrict); // Can't delete customer if they have installations

        // One Customer can have many Documents
        builder.HasMany(c => c.Documents)
            .WithOne(d => d.Customer)
            .HasForeignKey(d => d.CustomerId)
            .OnDelete(DeleteBehavior.Cascade); // Delete documents when customer is deleted

        // One Customer can have many SupportTickets
        builder.HasMany(c => c.SupportTickets)
            .WithOne(t => t.Customer)
            .HasForeignKey(t => t.CustomerId)
            .OnDelete(DeleteBehavior.Restrict); // Can't delete customer if they have tickets
    }
}