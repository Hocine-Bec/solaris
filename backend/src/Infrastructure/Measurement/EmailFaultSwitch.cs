using Microsoft.Extensions.Logging;

namespace Infrastructure.Measurement;

/// <summary>
/// Whether the mail server is made "unreachable" for a measurement. Its state at startup comes from
/// <c>FaultInjection:Enabled</c> (off by default), so an API restarted mid-measurement keeps the fault; a
/// Development-only endpoint toggles it while the API runs.
/// </summary>
public sealed class EmailFaultSwitch
{
    private readonly ILogger<EmailFaultSwitch> _log;
    private volatile bool _isOn;

    public EmailFaultSwitch(ILogger<EmailFaultSwitch> log, bool onAtStartup = false)
    {
        _log = log;
        _isOn = onAtStartup;
        if (onAtStartup)
            _log.LogWarning("Email fault injection is on at startup (FaultInjection:Enabled)");
    }

    public bool IsOn => _isOn;

    public void Set(bool on)
    {
        _isOn = on;
        _log.LogWarning("Email fault injection is now {State}", on ? "on" : "off");
    }
}
