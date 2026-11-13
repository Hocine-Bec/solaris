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
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        //FluentValidation
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
        
        //Mapster Config
        // 1. Get/create global config
        var config = TypeAdapterConfig.GlobalSettings;
    
        // 2. Scan for custom mappings (IRegister classes)
        config.Scan(Assembly.GetExecutingAssembly());
    
        // 3. Register for DI
        services.AddSingleton(config);
        services.AddScoped<IMapper, ServiceMapper>();
        
        //Services
        services.AddScoped<IAddressService, AddressService>();
        services.AddScoped<ILeadService, LeadService>();
        services.AddScoped<IUserService, UserService>();
        
        return services;
    }
}