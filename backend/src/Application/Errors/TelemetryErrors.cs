using Application.Common.Models;

namespace Application.Errors;

public static class TelemetryErrors
{
    /// <summary>
    /// The key authenticated, but its installation is gone or no longer Active between the check and the save: the
    /// device is told the same thing a wrong key is told, since from its side nothing else is actionable (BR-T2).
    /// </summary>
    public static readonly Error NotAccepting =
        new(ErrorKind.Unauthorized, "device.invalid_key", "This device key isn't valid. Ask the office for the installation's current key.");
}
