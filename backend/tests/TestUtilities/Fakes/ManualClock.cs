namespace TestUtilities.Fakes;

/// <summary>A clock a test sets and moves on by hand; timers and delays still run on real time.</summary>
public sealed class ManualClock(DateTimeOffset now) : TimeProvider
{
    public DateTimeOffset Now { get; set; } = now;

    public DateTime UtcNow => Now.UtcDateTime;

    public override DateTimeOffset GetUtcNow() => Now;

    public void Advance(TimeSpan by) => Now += by;
}
