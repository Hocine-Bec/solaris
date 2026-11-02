using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

public class DocumentConfiguration : IEntityTypeConfiguration<Document>
{
    public void Configure(EntityTypeBuilder<Document> builder)
    {
        builder.ToTable("documents");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.FileName)
            .HasColumnName("file_name")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(d => d.FilePath)
            .HasColumnName("file_path")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(d => d.FileType)
            .HasColumnName("file_type")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(d => d.FileSize)
            .HasColumnName("file_size")
            .IsRequired();

        builder.Property(d => d.Type)
            .HasColumnName("type")
            .HasConversion<string>()
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(d => d.UploadedAt)
            .HasColumnName("uploaded_at")
            .IsRequired();

        builder.Property(d => d.Description)
            .HasColumnName("description")
            .HasMaxLength(500);

        builder.Property(d => d.Notes)
            .HasColumnName("notes")
            .HasColumnType("text");

        // Indexes
        builder.HasIndex(d => d.CustomerId)
            .HasDatabaseName("ix_documents_customer_id");

        builder.HasIndex(d => d.InstallationId)
            .HasDatabaseName("ix_documents_installation_id");

        builder.HasIndex(d => d.TicketId)
            .HasDatabaseName("ix_documents_ticket_id");

        builder.HasIndex(d => d.Type)
            .HasDatabaseName("ix_documents_type");

        builder.HasIndex(d => d.UploadedAt)
            .HasDatabaseName("ix_documents_uploaded_at");

        // Relationships
        builder.HasOne(d => d.UploadedBy)
            .WithMany()
            .HasForeignKey(d => d.UploadedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Note: Customer, Installation, and Ticket relationships 
        // are already configured in their respective configurations
    }
}