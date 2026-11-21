using Application.Common.Models;
using Domain.Entities;
using Domain.Enums;

namespace Application.Interfaces.Services;

/// <summary>
/// Renders a new lead's two emails and an installation's switch-on email, and sends them through
/// <see cref="IEmailSender"/>. The outbox passes its message's id, which becomes the email's Message-ID.
/// </summary>
public interface IEmailService
{
    /// <summary>To the person who asked for the quote: what happens next, and what they sent. Needs the lead's address.</summary>
    Task SendLeadWelcomeAsync(Lead lead, Guid? outboxMessageId = null, CancellationToken cancellationToken = default);

    /// <summary>To the office: who asked, where, when to call; replies go to the person. Needs the lead's address.</summary>
    Task SendNewLeadAlertAsync(Lead lead, Guid? outboxMessageId = null, CancellationToken cancellationToken = default);

    /// <summary>Who an email about this lead goes to: the person, or the office.</summary>
    EmailAddress RecipientOf(OutboxMessageType type, Lead lead);

    /// <summary>To the customer: their system is switched on (FR-I7). Needs the installation's customer and address.</summary>
    Task SendInstallationActivatedAsync(Installation installation, Guid? outboxMessageId = null,
        CancellationToken cancellationToken = default);

    /// <summary>Who an installation's email goes to: its customer.</summary>
    EmailAddress RecipientOf(Installation installation);
}
