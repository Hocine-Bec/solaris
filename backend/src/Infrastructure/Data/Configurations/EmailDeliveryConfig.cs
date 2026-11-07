using Infrastructure.Measurement;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

public class EmailDeliveryConfig : IEntityTypeConfiguration<EmailDelivery>
{
    public void Configure(EntityTypeBuilder<EmailDelivery> builder)
    {
        builder.ToTable("email_delivery_log");

        builder.HasKey(d => d.Id)
            .HasName("pk_email_delivery_log");

        builder.Property(d => d.Id)
            .HasColumnName("id");

        // No unique index on the key: P2 counts the duplicates a re-send creates.
        builder.Property(d => d.NotificationKey)
            .HasColumnName("notification_key")
            .IsRequired();

        builder.Property(d => d.Recipient)
            .HasColumnName("recipient")
            .IsRequired();

        builder.Property(d => d.Subject)
            .HasColumnName("subject")
            .IsRequired();

        builder.Property(d => d.SentAt)
            .HasColumnName("sent_at")
            .IsRequired();
    }
}
