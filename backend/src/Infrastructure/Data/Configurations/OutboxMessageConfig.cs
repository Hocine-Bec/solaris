using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

public class OutboxMessageConfig : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("outbox_messages");

        builder.HasKey(m => m.Id)
            .HasName("pk_outbox_messages");

        // Made by the entity (version 7, time-ordered), so it can be the email's Message-ID before the row is saved
        builder.Property(m => m.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(m => m.Type)
            .HasColumnName("type")
            .HasConversion<string>()
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(m => m.LeadId)
            .HasColumnName("lead_id");

        builder.Property(m => m.InstallationId)
            .HasColumnName("installation_id");

        // A message is about a lead or an installation, never both or neither
        builder.ToTable(t => t.HasCheckConstraint("ck_outbox_messages_one_subject", "num_nonnulls(lead_id, installation_id) = 1"));

        builder.Property(m => m.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(m => m.Attempts)
            .HasColumnName("attempts")
            .IsRequired();

        builder.Property(m => m.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(m => m.GiveUpAt)
            .HasColumnName("give_up_at")
            .IsRequired();

        builder.Property(m => m.NextAttemptAt)
            .HasColumnName("next_attempt_at")
            .IsRequired();

        builder.Property(m => m.LastAttemptAt)
            .HasColumnName("last_attempt_at");

        builder.Property(m => m.SentAt)
            .HasColumnName("sent_at");

        builder.Property(m => m.LastError)
            .HasColumnName("last_error")
            .HasMaxLength(OutboxMessage.MaxErrorLength);

        builder.Property(m => m.RetriedAt)
            .HasColumnName("retried_at");

        builder.Property(m => m.RetriedByUserId)
            .HasColumnName("retried_by_user_id");

        builder.Ignore(m => m.NotificationKey);

        // Who retried it; accounts aren't deleted by the API, but if one is, its retries stay, without a name
        builder.HasOne(m => m.RetriedBy)
            .WithMany()
            .HasForeignKey(m => m.RetriedByUserId)
            .HasConstraintName("fk_outbox_messages_users_retried_by_user_id")
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(m => m.RetriedByUserId)
            .HasDatabaseName("ix_outbox_messages_retried_by_user_id");

        // A lead with notifications to send can't be deleted (nothing deletes leads today)
        builder.HasOne(m => m.Lead)
            .WithMany()
            .HasForeignKey(m => m.LeadId)
            .HasConstraintName("fk_outbox_messages_leads_lead_id")
            .OnDelete(DeleteBehavior.Restrict);

        // One message of each kind per lead: a second welcome email for the same lead can't be saved
        builder.HasIndex(m => new { m.LeadId, m.Type })
            .IsUnique()
            .HasDatabaseName("ux_outbox_messages_lead_id_type");

        // An installation with notifications to send can't be deleted (nothing deletes installations)
        builder.HasOne(m => m.Installation)
            .WithMany()
            .HasForeignKey(m => m.InstallationId)
            .HasConstraintName("fk_outbox_messages_installations_installation_id")
            .OnDelete(DeleteBehavior.Restrict);

        // One message of each kind per installation: a second switch-on email can't be saved
        builder.HasIndex(m => new { m.InstallationId, m.Type })
            .IsUnique()
            .HasDatabaseName("ux_outbox_messages_installation_id_type");

        // What the dispatcher reads: the pending messages, by when they are due; sent and failed ones aren't in it
        builder.HasIndex(m => m.NextAttemptAt)
            .HasFilter($"status = '{nameof(OutboxMessageStatus.Pending)}'")
            .HasDatabaseName("ix_outbox_messages_due");

        // The Admin's list of failed ones, newest failure first (FR-N2)
        builder.HasIndex(m => m.LastAttemptAt)
            .HasFilter($"status = '{nameof(OutboxMessageStatus.Failed)}'")
            .HasDatabaseName("ix_outbox_messages_failed");

        // The cleanup job's: sent ones, by when they were sent
        builder.HasIndex(m => m.SentAt)
            .HasFilter($"status = '{nameof(OutboxMessageStatus.Sent)}'")
            .HasDatabaseName("ix_outbox_messages_sent");
    }
}
