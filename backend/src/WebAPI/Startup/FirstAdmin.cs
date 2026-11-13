using Application.Interfaces.Services;
using Infrastructure.Settings;
using Microsoft.Extensions.Options;

namespace WebAPI.Startup;

/// <summary>
/// The first Admin (FR-U3), at startup: created from the Admin settings when no active Admin exists. Without either,
/// nobody could sign in to the staff app, so the API stops and says which settings to set.
/// </summary>
public static class FirstAdmin
{
    public static async Task EnsureAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<IUserService>();
        var admin = scope.ServiceProvider.GetRequiredService<IOptions<AdminOptions>>().Value;

        if (admin.IsSet && await users.EnsureFirstAdminAsync(admin.Name!, admin.Email!, admin.Password!))
        {
            scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(FirstAdmin))
                .LogInformation("Created the first Admin account from the Admin settings");
            return;
        }

        if (!await users.AnyActiveAdminAsync())
            throw new InvalidOperationException(
                "No active Admin account exists. Set Admin:Name, Admin:Email and Admin:Password (Admin__Name, " +
                "Admin__Email and Admin__Password in .env) to create the first one.");
    }
}
