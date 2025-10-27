using System.Text;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Infrastructure.Services;
using Infrastructure.Settings;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtSetting>(options =>
        {
            options.Secret = Environment.GetEnvironmentVariable("SECRET_KEY") ??
                             throw new InvalidOperationException("SECRET_KEY environment variable is not set");
            options.Issuer = Environment.GetEnvironmentVariable("ISSUER") ??
                             throw new InvalidOperationException("ISSUER environment variable is not set");
            options.Audience = Environment.GetEnvironmentVariable("AUDIENCE") ??
                               throw new InvalidOperationException("AUDIENCE environment variable is not set");
            options.LifeTime =
                int.Parse(Environment.GetEnvironmentVariable("EXPIRY_MINUTES") ?? "15");
            options.RefreshTokenLifeTime =
                int.Parse(Environment.GetEnvironmentVariable("REFRESH_TOKEN_LIFETIME_DAYS") ?? "7");
        });

        var connectionString = Environment.GetEnvironmentVariable("DEFAULTCONNECTION");
        services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));


        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        }).AddJwtBearer(options =>
        {
            options.SaveToken = true;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidAudience = Environment.GetEnvironmentVariable("AUDIENCE"),
                ValidIssuer = Environment.GetEnvironmentVariable("ISSUER"),
                IssuerSigningKey =
                    new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Environment.GetEnvironmentVariable("SECRET_KEY")!)),
                ClockSkew = TimeSpan.Zero
            };
        });

        //Repositories
        services.AddScoped<IAddressRepo, AddressRepo>();
        services.AddScoped<ICustomerRepo, CustomerRepo>();
        services.AddScoped<IDocumentRepo, DocumentRepo>();
        services.AddScoped<IEnergyProductionRepo, EnergyProductionRepo>();
        services.AddScoped<IEquipmentRepo, EquipmentRepo>();
        services.AddScoped<IInstallationRepo, InstallationRepo>();
        services.AddScoped<IInstallationStatusHistoryRepo, InstallationStatusHistoryRepo>();
        services.AddScoped<IInstallationTechnicianRepo, InstallationTechnicianRepo>();
        services.AddScoped<ISupportTicketRepo, SupportTicketRepo>();
        services.AddScoped<IUserRepo, UserRepo>();
        services.AddScoped<IWeatherDataRepo, WeatherDataRepo>();
        services.AddScoped<ILeadRepo, LeadRepo>();
        
        // Gmail Services
        services.AddSingleton<IGmailAuthService, GmailAuthService>();
        services.AddScoped<IEmailService, EmailService>();
        
        
        return services;
    }
}