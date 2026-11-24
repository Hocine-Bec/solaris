using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;

namespace Application.Common.Security;

/// <summary>
/// An installation's device key (BR-T2, ADR 14): <c>&lt;prefix&gt;.&lt;secret&gt;</c>. The prefix, 12 random characters
/// of lowercase base32, names the key and finds its installation; the secret, 32 random bytes written in base64url
/// (43 characters), proves it. Only the secret's SHA-256 is stored: the secret is random, not a password a person
/// picks, so a slow hash would add nothing but time to every upload.
/// </summary>
public static class DeviceKeys
{
    public const int PrefixLength = 12;
    public const int SecretLength = 43;
    private const string PrefixAlphabet = "abcdefghijklmnopqrstuvwxyz234567";

    /// <summary>A new key: the text shown once, and what is stored of it.</summary>
    public static NewDeviceKey Create()
    {
        var prefix = RandomNumberGenerator.GetString(PrefixAlphabet, PrefixLength);
        var secret = Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(32));
        return new NewDeviceKey($"{prefix}.{secret}", prefix, Hash(secret));
    }

    /// <summary>The key's two parts, if it is written as a key is; anything else is no key at all.</summary>
    public static bool TryParse(string? text, out string prefix, out string secret)
    {
        prefix = secret = string.Empty;
        if (text is null || text.Length != PrefixLength + 1 + SecretLength || text[PrefixLength] != '.')
            return false;
        var p = text.AsSpan(0, PrefixLength);
        var s = text.AsSpan(PrefixLength + 1);
        if (p.ContainsAnyExcept(PrefixAlphabet) || !Base64Url.IsValid(s, out var length) || length != 32)
            return false;
        (prefix, secret) = (p.ToString(), s.ToString());
        return true;
    }

    public static byte[] Hash(string secret) => SHA256.HashData(Encoding.ASCII.GetBytes(secret));

    /// <summary>Whether the secret is the one this hash was made from, compared in constant time.</summary>
    public static bool Matches(string secret, byte[] storedHash) =>
        CryptographicOperations.FixedTimeEquals(Hash(secret), storedHash);
}

/// <summary>A key just made: <see cref="Text"/> is shown once and never stored.</summary>
public sealed record NewDeviceKey(string Text, string Prefix, byte[] Hash);
