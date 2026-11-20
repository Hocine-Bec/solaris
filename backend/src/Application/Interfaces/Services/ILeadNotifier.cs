using Domain.Entities;

namespace Application.Interfaces.Services;

/// <summary>
/// How a new lead's two notifications, the welcome email and the office's alert, leave: the configured mode decides
/// (docs/operations.md, <c>Notifications__Mode</c>). The outbox adds them to the lead's own save; the measurement
/// harness's synchronous mode sends them once the lead is saved.
/// </summary>
public interface ILeadNotifier
{
    /// <summary>Before the lead is saved: what must be saved with it, in the same transaction.</summary>
    void OnSaving(Lead lead);

    /// <summary>Once the lead is saved, with its address.</summary>
    Task OnSavedAsync(Lead lead);
}
