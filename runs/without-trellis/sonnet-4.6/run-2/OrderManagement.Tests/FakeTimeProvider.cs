namespace OrderManagement.Tests;

/// <summary>
/// A test double for TimeProvider that returns a fixed or controllable time.
/// </summary>
public class FakeTimeProvider : TimeProvider
{
    private DateTimeOffset _now;

    public FakeTimeProvider(DateTime utcNow)
        => _now = new DateTimeOffset(utcNow, TimeSpan.Zero);

    public FakeTimeProvider(DateTimeOffset now)
        => _now = now;

    public override DateTimeOffset GetUtcNow() => _now;

    public void Advance(TimeSpan amount) => _now = _now.Add(amount);

    public void SetUtcNow(DateTime utcNow) => _now = new DateTimeOffset(utcNow, TimeSpan.Zero);
}
