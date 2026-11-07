using Application.Interfaces.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Measurement;

public static class MeasurementHarness
{
    /// <summary>
    /// The measurement harness (docs/measurements.md §4), registered in Development only: the provider's sender is
    /// wrapped so a mail-server fault can be injected (on at startup when <c>FaultInjection:Enabled</c> is true), and
    /// every email the mail server accepts is written to <c>email_delivery_log</c>. Call it after <c>AddInfrastructure</c>.
    /// </summary>
    public static IServiceCollection AddMeasurementHarness(this IServiceCollection services)
    {
        services.AddSingleton(provider => new EmailFaultSwitch(
            provider.GetRequiredService<ILogger<EmailFaultSwitch>>(),
            onAtStartup: provider.GetService<IConfiguration>()?.GetValue<bool>("FaultInjection:Enabled") ?? false));
        services.Replace(ServiceDescriptor.Scoped<IEmailSender>(provider =>
            new DeliveryLoggingEmailSender(
                new FaultInjectingEmailSender(DependencyInjection.CreateProviderSender(provider),
                    provider.GetRequiredService<EmailFaultSwitch>()),
                provider.GetRequiredService<IServiceScopeFactory>())));
        return services;
    }
}
