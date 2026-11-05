using FluentAssertions;
using Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace IntegrationTests.Infrastructure;

// The settings AddInfrastructure binds: a missing or invalid one fails startup validation, which names it.
public class SettingsValidationTests
{
    private static Dictionary<string, string?> CompleteSettings() => new()
    {
        ["ConnectionStrings:Default"] = "Host=localhost;Database=solaris;Username=postgres;Password=unused",
        ["Jwt:Secret"] = new string('s', 32),
        ["Jwt:Issuer"] = "solaris-api",
        ["Jwt:Audience"] = "solaris-staff",
        ["Email:FromName"] = "Solaris",
        ["Email:FromAddress"] = "sender@example.com",
        ["Email:Smtp:Host"] = "smtp.example.com",
        ["Email:Smtp:Port"] = "587",
        ["Email:Gmail:ClientId"] = "client-id",
        ["Email:Gmail:ClientSecret"] = "client-secret",
        ["Email:Gmail:RefreshToken"] = "refresh-token"
    };

    // What the API runs right after it is built (Program.cs), before it touches the database.
    private static Action ValidateStartup(Dictionary<string, string?> settings)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        var provider = new ServiceCollection().AddInfrastructure(configuration).BuildServiceProvider();
        return () => provider.GetRequiredService<IStartupValidator>().Validate();
    }

    [Fact]
    public void Startup_AllSettingsPresent_PassesValidation()
    {
        // Act
        var validate = ValidateStartup(CompleteSettings());

        // Assert
        validate.Should().NotThrow();
    }

    [Theory]
    [InlineData("ConnectionStrings:Default", "Default")]
    [InlineData("Jwt:Secret", "Secret")]
    [InlineData("Jwt:Issuer", "Issuer")]
    [InlineData("Jwt:Audience", "Audience")]
    [InlineData("Email:FromName", "FromName")]
    [InlineData("Email:FromAddress", "FromAddress")]
    [InlineData("Email:Smtp:Host", "Host")]
    [InlineData("Email:Smtp:Port", "Port")]
    [InlineData("Email:Gmail:ClientId", "ClientId")]
    [InlineData("Email:Gmail:ClientSecret", "ClientSecret")]
    [InlineData("Email:Gmail:RefreshToken", "RefreshToken")]
    public void Startup_SettingMissing_FailsNamingTheSetting(string key, string member)
    {
        // Arrange
        var settings = CompleteSettings();
        settings.Remove(key);

        // Act
        var validate = ValidateStartup(settings);

        // Assert
        validate.Should().Throw<OptionsValidationException>().WithMessage($"*{member}*");
    }

    [Theory]
    [InlineData("Jwt:Secret", "31-characters-is-one-too-short!", "Secret")]
    [InlineData("Email:FromAddress", "not-an-email-address", "FromAddress")]
    [InlineData("Email:Smtp:Port", "70000", "Port")]
    public void Startup_SettingInvalid_FailsNamingTheSetting(string key, string value, string member)
    {
        // Arrange
        var settings = CompleteSettings();
        settings[key] = value;

        // Act
        var validate = ValidateStartup(settings);

        // Assert
        validate.Should().Throw<OptionsValidationException>().WithMessage($"*{member}*");
    }
}
