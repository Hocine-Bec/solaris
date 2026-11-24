using Application.Common.Models;

namespace Application.Errors;

public static class DeviceErrors
{
    /// <summary>
    /// Every refused device key (BR-T2): none sent, not written as a key, an unknown prefix, a wrong secret, or an
    /// installation no longer Active. One answer, so a caller learns nothing about which part was wrong.
    /// </summary>
    public static readonly Error InvalidKey =
        new(ErrorKind.Unauthorized, "device.invalid_key", "This device key isn't valid. Ask the office for the installation's current key.");
}
