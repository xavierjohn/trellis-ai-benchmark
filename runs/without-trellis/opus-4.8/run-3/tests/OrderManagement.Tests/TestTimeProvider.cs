namespace OrderManagement.Tests;

/// <summary>A <see cref="TimeProvider"/> whose current time can be set explicitly in tests.</summary>
public sealed class TestTimeProvider(DateTimeOffset now) : TimeProvider
{
    private DateTimeOffset _now = now;

    public TestTimeProvider() : this(DateTimeOffset.Parse("2026-06-12T00:00:00Z")) { }

    public override DateTimeOffset GetUtcNow() => _now;

    public void Set(DateTimeOffset value) => _now = value;

    public void Advance(TimeSpan by) => _now += by;
}
