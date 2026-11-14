using System.Diagnostics;
using System.Text;
using Application.Errors;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Hangfire;
using Hangfire.PostgreSql;
using Infrastructure.Auth;
using Infrastructure.Data;
using Infrastructure.Measurement;
using Infrastructure.Repositories;
using Infrastructure.Services;
using Infrastructure.Settings;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Infrastructure;

public static class DependencyInjection
{
    /// <summary>Marks a request whose token was valid but whose account is deactivated or gone.</summary>
    private const string SignedOutKey = "solaris.signed_out";

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
        services.AddOptions<AdminOptions>()
            .Bind(configuration.GetSection(AdminOptions.SectionName))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<AdminOptions>, AdminOptionsValidator>();
        services.AddOptions<EmailOptions>()
            .Bind(configuration.GetSection(EmailOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<EmailOptions>, EmailOptionsValidator>();
        services.AddOptions<NotificationsOptions>()
            .Bind(configuration.GetSection(NotificationsOptions.SectionName))
            .Validate<IHostEnvironment>(
                (options, environment) =>
                    options.Mode != NotificationsMode.Synchronous || environment.IsDevelopment(),
                "Notifications:Mode can be Synchronous only in Development: it exists for measurements.")
            .ValidateOnStart();

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
                // The claims keep the names the token gives them (sub, email, role), rather than .NET's long URIs.
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidAudience = jwt.Audience,
                    ValidIssuer = jwt.Issuer,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Secret)),
                    ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                    NameClaimType = Session.UserIdClaim,
                    RoleClaimType = Session.RoleClaim,
                    ClockSkew = TimeSpan.Zero
                };
                // The staff app's session travels in a cookie (ADR 7); the API reference, tests and scripts send the
                // Authorization header, which wins when both are there.
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        if (string.IsNullOrEmpty(context.Request.Headers.Authorization) &&
                            context.Request.Cookies.TryGetValue(Session.CookieName, out var token))
                            context.Token = token;
                        return Task.CompletedTask;
                    },
                    // A valid token isn't enough: the account must still exist and be active, so deactivating an
                    // account ends its sessions at their next request (ADR 7).
                    OnTokenValidated = async context =>
                    {
                        var users = context.HttpContext.RequestServices.GetRequiredService<IUserRepo>();
                        var id = context.Principal?.FindFirst(Session.UserIdClaim)?.Value;
                        if (int.TryParse(id, out var userId) && await users.IsActiveAsync(userId))
                            return;
                        context.HttpContext.Items[SignedOutKey] = true;
                        context.Fail("The account was deactivated or no longer exists.");
                    },
                    // That case answers auth.signed_out, as /me does; any other 401 keeps the framework's answer.
                    OnChallenge = async context =>
                    {
                        if (!context.HttpContext.Items.ContainsKey(SignedOutKey))
                            return;
                        context.HandleResponse();
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        var problem = new ProblemDetails
                        {
                            Status = StatusCodes.Status401Unauthorized,
                            Detail = AuthErrors.SignedOut.Message,
                            Extensions = { ["code"] = AuthErrors.SignedOut.Code }
                        };
                        await context.HttpContext.RequestServices.GetRequiredService<IProblemDetailsService>()
                            .WriteAsync(new ProblemDetailsContext { HttpContext = context.HttpContext, ProblemDetails = problem });
                    }
                };
            });

        // Staff accounts: passwords and session tokens
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<ISessionTokenIssuer, SessionTokenIssuer>();

        //Repositories
        services.AddScoped<IAddressRepo, AddressRepo>();
        services.AddScoped<ICustomerRepo, CustomerRepo>();
        services.AddScoped<ILeadRepo, LeadRepo>();
        services.AddScoped<IUserRepo, UserRepo>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        
        // Email: the provider's sender (Email:Provider), which the measurement harness wraps in Development
        services.AddSingleton<IGmailAuthService, GmailAuthService>();
        services.AddScoped<IEmailSender>(CreateProviderSender);
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<IEmailQueueHandler, EmailQueueHandler>();
        services.AddScoped<ILeadNotifier>(CreateLeadNotifier);

        return services;
    }

    /// <summary>The notifier of the configured mode (Notifications:Mode).</summary>
    private static ILeadNotifier CreateLeadNotifier(IServiceProvider provider) =>
        provider.GetRequiredService<IOptions<NotificationsOptions>>().Value.Mode switch
        {
            NotificationsMode.Hangfire => ActivatorUtilities.CreateInstance<HangfireLeadNotifier>(provider),
            NotificationsMode.Synchronous => ActivatorUtilities.CreateInstance<SynchronousLeadNotifier>(provider),
            var other => throw new UnreachableException($"No notifier for the notifications mode {other}.")
        };

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