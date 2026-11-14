using Domain.Entities;

namespace Application.Interfaces.Services;

/// <summary>
/// Hands a new lead's two notifications, the welcome email and the office's alert, to whatever delivers them; the
/// configured mode decides which (docs/operations.md, <c>Notifications__Mode</c>).
/// </summary>
public interface ILeadNotifier
{
    /// <summary>The lead as just saved, with its address.</summary>
    Task NotifyNewLeadAsync(Lead lead);
}
