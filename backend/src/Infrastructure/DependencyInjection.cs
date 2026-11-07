using System.Diagnostics;
using System.Text;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Hangfire;
using Hangfire.PostgreSql;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Infrastructure.Services;
using Infrastructure.Settings;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Typed settings, validated when the host starts: a missing or invalid one stops the API and names the key.
        services.AddOptions<ConnectionStringsOptions>()
            .Bind(configuration.GetSection(ConnectionStringsOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddOptions<EmailOptions>()
            .Bind(configuration.GetSection(EmailOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<EmailOptions>, EmailOptionsValidator>();

        services.AddDbContext<AppDbContext>((provider, options) => options
            .UseNpgsql(provider.GetRequiredService<IOptions<ConnectionStringsOptions>>().Value.Default)
            // EF Core logs a failed command, save or query as an error even when the code expects the failure (a
            // unique index rejecting a duplicate, which the unit of work turns into SaveOutcome.Duplicate). Whoever
            // catches the exception decides: an expected failure isn't an error, and an unexpected one reaches the
            // API's exception handler, which logs it once. Raise the EF Core categories to Debug to see these events.
            .ConfigureWarnings(warnings => warnings.Log(
                (RelationalEventId.CommandError, LogLevel.Debug),
                (RelationalEventId.ConnectionError, LogLevel.Debug),
                (CoreEventId.SaveChangesFailed, LogLevel.Debug),
                (CoreEventId.QueryIterationFailed, LogLevel.Debug))));

        //Add Hangfire services
        services.AddHangfire((provider, config) =>
            config.SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UsePostgreSqlStorage(options =>
                {
                    options.UseNpgsqlConnection(
                        provider.GetRequiredService<IOptions<ConnectionStringsOptions>>().Value.Default);
                }, new PostgreSqlStorageOptions()
                {
                    QueuePollInterval = TimeSpan.FromSeconds(1),
                    PrepareSchemaIfNecessary = true,
                    InvisibilityTimeout = TimeSpan.FromMinutes(5),
                    UseNativeDatabaseTransactions = true,
                    EnableTransactionScopeEnlistment = true,
                    SchemaName = "hangfire"
                })
        );
        
        services.AddHangfireServer();

        //Configure Jwt settings
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        }).AddJwtBearer();

        // The bearer options read the validated JwtOptions instead of the configuration directly.
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>((options, jwtOptions) =>
            {
                var jwt = jwtOptions.Value;
                options.SaveToken = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidAudience = jwt.Audience,
                    ValidIssuer = jwt.Issuer,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Secret)),
                    ClockSkew = TimeSpan.Zero
                };
            });
        
        //Repositories
        services.AddScoped<IAddressRepo, AddressRepo>();
        services.AddScoped<ICustomerRepo, CustomerRepo>();
        services.AddScoped<ILeadRepo, LeadRepo>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        
        // Email: the provider's sender (Email:Provider), which the measurement harness wraps in Development
        services.AddSingleton<IGmailAuthService, GmailAuthService>();
        services.AddScoped<IEmailSender>(CreateProviderSender);
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<IEmailQueueHandler, EmailQueueHandler>();

        return services;
    }

    /// <summary>The sender of the configured provider, without any wrapper.</summary>
    internal static IEmailSender CreateProviderSender(IServiceProvider provider)
    {
        var options = provider.GetRequiredService<IOptions<EmailOptions>>();
        return options.Value.Provider switch
        {
            EmailProvider.Gmail => new GmailSmtpEmailSender(provider.GetRequiredService<IGmailAuthService>(), options),
            EmailProvider.Recording => new RecordingEmailSender(options),
            var other => throw new UnreachableException($"No sender for the email provider {other}.")
        };
    }
}