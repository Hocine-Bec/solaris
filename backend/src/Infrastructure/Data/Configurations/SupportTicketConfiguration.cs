using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

public class SupportTicketConfiguration : IEntityTypeConfiguration<SupportTicket>
{
    public void Configure(EntityTypeBuilder<SupportTicket> builder)
    {
        builder.ToTable("support_tickets");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Title)
            .HasColumnName("title")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(t => t.Description)
            .HasColumnName("description")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(t => t.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(t => t.Priority)
            .HasColumnName("priority")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(t => t.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(t => t.AssignedAt)
            .HasColumnName("assigned_at");

        builder.Property(t => t.ResolvedAt)
            .HasColumnName("resolved_at");

        builder.Property(t => t.ClosedAt)
            .HasColumnName("closed_at");

        builder.Property(t => t.ResolutionNotes)
            .HasColumnName("resolution_notes")
            .HasColumnType("text");

        // Indexes
        builder.HasIndex(t => t.CustomerId)
            .HasDatabaseName("ix_support_tickets_customer_id");

        builder.HasIndex(t => t.Status)
            .HasDatabaseName("ix_support_tickets_status");

        builder.HasIndex(t => t.Priority)
            .HasDatabaseName("ix_support_tickets_priority");

        builder.HasIndex(t => t.AssignedToUserId)
            .HasDatabaseName("ix_support_tickets_assigned_to_user_id");

        builder.HasIndex(t => t.CreatedAt)
            .HasDatabaseName("ix_support_tickets_created_at");

        // Relationships
        builder.HasOne(t => t.AssignedTo)
            .WithMany(u => u.AssignedTickets)
            .HasForeignKey(t => t.AssignedToUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(t => t.Installation)
            .WithMany()
            .HasForeignKey(t => t.InstallationId)
            .OnDelete(DeleteBehavior.SetNull);

        // Documents relationship
        builder.HasMany(t => t.Documents)
            .WithOne(d => d.Ticket)
            .HasForeignKey(d => d.TicketId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}