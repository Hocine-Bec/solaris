using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using Application.Interfaces.Services;
using Application.Services;
using Mapster;
using MapsterMapper;

namespace Application;

public static class DependencyInjection
{
    private static readonly Lazy<TypeAdapterConfig> MappingsRegistered = new(() =>
    {
        var config = TypeAdapterConfig.GlobalSettings;
        config.Scan(Assembly.GetExecutingAssembly());
        return config;
    });

    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        //FluentValidation
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
        
        // Mapster: the services map with .Adapt(), which reads the process-wide GlobalSettings. The mappings go in once
        // per process: registering them again while another host in the same process maps (the integration tests
        // start many at once) made Mapster throw "TypeAdapter.Adapt was already called".
        var config = MappingsRegistered.Value;
        services.AddSingleton(config);
        services.AddScoped<IMapper, ServiceMapper>();
        
        //Services
        services.AddScoped<ILeadService, LeadService>();
        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<IInstallationService, InstallationService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<INotificationService, NotificationService>();
        
        return services;
    }
}