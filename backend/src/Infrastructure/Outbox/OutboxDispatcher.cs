using Infrastructure.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Outbox;

/// <summary>
/// Sends the outbox's messages in the background (BR-N2): a batch every 2 s, and the next one at once while batches
/// are full. Several processes can run it side by side: each batch locks its own messages. Idle in the measurement
/// harness's synchronous mode, which writes no outbox messages.
/// </summary>
public sealed class OutboxDispatcher(IServiceScopeFactory scopes, IOptions<NotificationsOptions> options,
    TimeProvider time, ILogger<OutboxDispatcher> logger) : BackgroundService
{
    public static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(2);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (options.Value.Mode != NotificationsMode.Outbox)
            return;

        while (!stoppingToken.IsCancellationRequested)
        {
            var takeMoreNow = false;
            try
            {
                await using var scope = scopes.CreateAsyncScope();
                var batch = await scope.ServiceProvider.GetRequiredService<OutboxProcessor>().SendDueAsync(stoppingToken);
                takeMoreNow = batch.TakeMoreNow;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                // The database, most likely: nothing was sent from this batch that isn't still Pending
                logger.LogError(ex, "The outbox dispatcher couldn't process a batch; it tries again in {Seconds} s",
                    PollInterval.TotalSeconds);
            }

            if (takeMoreNow)
                continue;
            try
            {
                await Task.Delay(PollInterval, time, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }
}
