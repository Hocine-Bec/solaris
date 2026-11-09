namespace Application.Interfaces.Services;

/// <summary>
/// Hands a new lead's two notifications, the welcome email and the sales alert, to whatever delivers them; the
/// configured mode decides which (docs/operations.md, <c>Notifications__Mode</c>).
/// </summary>
public interface ILeadNotifier
{
    Task NotifyNewLeadAsync(int leadId, string email, string firstName, string fullName, string phoneNumber,
        string address);
}
