using Application.Interfaces.Services;
using Domain.Entities;
using Microsoft.Extensions.Options;
using Identity = Microsoft.AspNetCore.Identity;

namespace Infrastructure.Auth;

/// <summary>
/// ASP.NET Core's password hasher, from the shared framework: PBKDF2 with HMAC-SHA512, a 128-bit salt and a 256-bit
/// subkey, each hash carrying its own parameters. Set to 220,000 iterations, the OWASP Password Storage Cheat Sheet's
/// figure for PBKDF2-HMAC-SHA512 (the framework's default is 100,000); a hash made with fewer is replaced at the next
/// sign-in.
/// </summary>
public sealed class PasswordHasher : IPasswordHasher
{
    public const int Iterations = 220_000;

    private static readonly Identity.PasswordHasher<User> Hasher =
        new(Options.Create(new Identity.PasswordHasherOptions { IterationCount = Iterations }));

    // The hasher doesn't use the account; it only takes one.
    private static readonly User Nobody = new();

    // A hash no password matches in practice, made once, for emails with no account.
    private static readonly Lazy<string> NothingHash = new(() => Hasher.HashPassword(Nobody, Guid.NewGuid().ToString()));

    public string Hash(string password) => Hasher.HashPassword(Nobody, password);

    public PasswordCheck Verify(string hash, string password) =>
        Hasher.VerifyHashedPassword(Nobody, hash, password) switch
        {
            Identity.PasswordVerificationResult.Success => PasswordCheck.Succeeded,
            Identity.PasswordVerificationResult.SuccessRehashNeeded => PasswordCheck.SucceededRehashNeeded,
            _ => PasswordCheck.Failed
        };

    public void VerifyNothing(string password) => Hasher.VerifyHashedPassword(Nobody, NothingHash.Value, password);
}
