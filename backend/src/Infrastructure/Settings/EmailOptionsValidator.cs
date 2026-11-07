using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Infrastructure.Settings;

/// <summary>
/// The email rules data annotations can't express, checked at startup with the others: the Gmail settings are
/// required only when Gmail sends, and Recording, which sends nothing, is refused in Production.
/// </summary>
public sealed class EmailOptionsValidator(IHostEnvironment environment) : IValidateOptions<EmailOptions>
{
    public ValidateOptionsResult Validate(string? name, EmailOptions options)
    {
        var failures = new List<string>();

        if (options.Provider == EmailProvider.Gmail)
        {
            var results = new List<ValidationResult>();
            Validator.TryValidateObject(options.Gmail, new ValidationContext(options.Gmail), results,
                validateAllProperties: true);
            failures.AddRange(results.SelectMany(result => result.MemberNames)
                .Select(member => $"Email:Gmail:{member} is required when Email:Provider is Gmail."));
        }

        if (options.Provider == EmailProvider.Recording && environment.IsProduction())
            failures.Add("Email:Provider can't be Recording in Production: Recording sends no email.");

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }
}
