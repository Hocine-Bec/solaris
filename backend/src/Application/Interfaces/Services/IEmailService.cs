using Domain.Entities;

namespace Application.Interfaces.Services;

/// <summary>Renders a new lead's two emails and sends them through <see cref="IEmailSender"/>.</summary>
public interface IEmailService
{
    /// <summary>To the person who asked for the quote: what happens next, and what they sent. Needs the lead's address.</summary>
    Task SendLeadWelcomeAsync(Lead lead);

    /// <summary>To the office: who asked, where, when to call; replies go to the person. Needs the lead's address.</summary>
    Task SendNewLeadAlertAsync(Lead lead);
}
