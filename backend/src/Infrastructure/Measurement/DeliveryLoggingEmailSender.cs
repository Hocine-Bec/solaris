using Application.Common.Models;
using Application.Interfaces.Services;
using Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Measurement;

/// <summary>
/// The outermost sender, and the only writer of <c>email_delivery_log</c> (docs/measurements.md §4): once the inner
/// sender returns, the mail server has accepted the email, so it writes one row.
/// </summary>
public sealed class DeliveryLoggingEmailSender(IEmailSender inner, IServiceScopeFactory scopes) : IEmailSender
{
    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        await inner.SendAsync(message, cancellationToken);

        // A context of its own, so the row is its own transaction: it stays even if the caller's work is rolled back
        // or the process dies before the caller commits. Not cancellable: the email has already gone.
        await using var scope = scopes.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        context.EmailDeliveries.Add(new EmailDelivery
        {
            NotificationKey = message.NotificationKey,
            Recipient = message.ToAddress,
            Subject = message.Subject,
            SentAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync(CancellationToken.None);
    }
}
