using Application.Common.Models;
using Application.Interfaces.Services;
using Infrastructure.Settings;
using Microsoft.Extensions.Options;

namespace Infrastructure.Services;

/// <summary>
/// <c>Email:Provider=Recording</c>: sends nothing. It waits <c>Email:SimulatedLatencyMs</c> per email, standing in
/// for a real mail server in measurements and local runs (docs/measurements.md §4).
/// </summary>
public sealed class RecordingEmailSender(IOptions<EmailOptions> options) : IEmailSender
{
    private readonly TimeSpan _latency = TimeSpan.FromMilliseconds(options.Value.SimulatedLatencyMs);

    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default) =>
        Task.Delay(_latency, cancellationToken);
}
