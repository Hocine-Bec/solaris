using FluentAssertions;
using Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
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
    private static Action ValidateStartup(Dictionary<string, string?> settings, string environment = "Development")
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        var provider = new ServiceCollection()
            .AddSingleton<IHostEnvironment>(new TestHostEnvironment(environment))
            .AddInfrastructure(configuration)
            .BuildServiceProvider();
        return () => provider.GetRequiredService<IStartupValidator>().Validate();
    }

    private static Dictionary<string, string?> RecordingSettingsWithoutGmail()
    {
        var settings = CompleteSettings();
        settings["Email:Provider"] = "Recording";
        foreach (var key in settings.Keys.Where(k => k.StartsWith("Email:Gmail:")).ToList())
            settings.Remove(key);
        return settings;
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

    [Fact]
    public void Startup_RecordingProviderWithoutGmailSettings_PassesValidation()
    {
        // Act
        var validate = ValidateStartup(RecordingSettingsWithoutGmail());

        // Assert - the Gmail settings are required only when Gmail sends
        validate.Should().NotThrow();
    }

    [Fact]
    public void Startup_RecordingProviderInProduction_FailsNamingTheProvider()
    {
        // Act
        var validate = ValidateStartup(RecordingSettingsWithoutGmail(), environment: "Production");

        // Assert - Recording sends no email, so production must never run with it
        validate.Should().Throw<OptionsValidationException>().WithMessage("*Email:Provider*Production*");
    }

    [Fact]
    public void Startup_SynchronousNotificationsInDevelopment_PassesValidation()
    {
        // Arrange
        var settings = CompleteSettings();
        settings["Notifications:Mode"] = "Synchronous";

        // Act
        var validate = ValidateStartup(settings);

        // Assert
        validate.Should().NotThrow();
    }

    [Theory]
    [InlineData("Testing")]
    [InlineData("Production")]
    public void Startup_SynchronousNotificationsOutsideDevelopment_FailsNamingTheMode(string environment)
    {
        // Arrange
        var settings = CompleteSettings();
        settings["Notifications:Mode"] = "Synchronous";

        // Act
        var validate = ValidateStartup(settings, environment);

        // Assert - every lead request would wait for its emails: a measurement's "before", nothing to deploy
        validate.Should().Throw<OptionsValidationException>().WithMessage("*Notifications:Mode*Development*");
    }

    [Theory]
    [InlineData("Jwt:Secret", "31-characters-is-one-too-short!", "Secret")]
    [InlineData("Email:SimulatedLatencyMs", "-1", "SimulatedLatencyMs")]
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

    private static Dictionary<string, string?> WithFirstAdmin(string? name = "Rosa Castillo",
        string? email = "rosa.castillo@example.com", string? password = "Solaris#2026")
    {
        var settings = CompleteSettings();
        settings["Admin:Name"] = name;
        settings["Admin:Email"] = email;
        settings["Admin:Password"] = password;
        return settings;
    }

    [Fact]
    public void Startup_FirstAdminSet_PassesValidation()
    {
        // Act
        var validate = ValidateStartup(WithFirstAdmin());

        // Assert
        validate.Should().NotThrow();
    }

    [Fact]
    public void Startup_FirstAdminOnlyPartlySet_FailsNamingTheMissingSettings()
    {
        // Act
        var validate = ValidateStartup(WithFirstAdmin(name: null, password: null));

        // Assert - all three or none: a half-set first Admin is a mistake, not a choice
        validate.Should().Throw<OptionsValidationException>()
            .WithMessage("*Admin:Name is required*").And.Message.Should().Contain("Admin:Password: Password is required");
    }

    [Theory]
    [InlineData("solaris#2026", "Admin:Password: Password must contain an uppercase letter")]
    [InlineData("Solaris2026", "Admin:Password: Password must contain a special character")]
    [InlineData("Sol#26", "Admin:Password: Password must be at least 8 characters")]
    public void Startup_FirstAdminPasswordBreakingTheRule_FailsNamingTheProblem(string password, string problem)
    {
        // Act
        var validate = ValidateStartup(WithFirstAdmin(password: password));

        // Assert
        validate.Should().Throw<OptionsValidationException>().WithMessage($"*{problem}*");
    }

    [Fact]
    public void Startup_FirstAdminEmailInvalid_FailsNamingTheEmail()
    {
        // Act
        var validate = ValidateStartup(WithFirstAdmin(email: "rosa.castillo"));

        // Assert
        validate.Should().Throw<OptionsValidationException>().WithMessage("*Admin:Email must be an email address*");
    }
}

internal sealed class TestHostEnvironment(string environmentName) : IHostEnvironment
{
    public string EnvironmentName { get; set; } = environmentName;
    public string ApplicationName { get; set; } = "WebAPI";
    public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
}
