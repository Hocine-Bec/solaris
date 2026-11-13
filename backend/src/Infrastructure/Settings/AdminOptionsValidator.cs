using System.ComponentModel.DataAnnotations;
using Domain.Common;
using Microsoft.Extensions.Options;

namespace Infrastructure.Settings;

/// <summary>
/// The first Admin's settings, checked at startup with the others: none at all, or all three, with a valid email, a
/// name that fits, and a password that follows the password rule. Whether they are needed (no Admin yet) is known only
/// once the database is read, at startup too (<c>Program.cs</c>).
/// </summary>
public sealed class AdminOptionsValidator : IValidateOptions<AdminOptions>
{
    public ValidateOptionsResult Validate(string? name, AdminOptions options)
    {
        if (!options.IsSet)
            return ValidateOptionsResult.Success;

        var failures = new List<string>();
        if (string.IsNullOrWhiteSpace(options.Name))
            failures.Add("Admin:Name is required when the first Admin is set.");
        else if (options.Name.Trim().Length > 100)
            failures.Add("Admin:Name must be at most 100 characters.");

        if (string.IsNullOrWhiteSpace(options.Email))
            failures.Add("Admin:Email is required when the first Admin is set.");
        else if (!new EmailAddressAttribute().IsValid(options.Email.Trim()) || options.Email.Trim().Length > 255)
            failures.Add("Admin:Email must be an email address of at most 255 characters.");

        foreach (var problem in PasswordRule.Problems(options.Password))
            failures.Add($"Admin:Password: {problem}.");

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }
}
