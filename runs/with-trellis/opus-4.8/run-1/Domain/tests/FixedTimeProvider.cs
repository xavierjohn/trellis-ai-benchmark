namespace OrderManagement.Domain.Tests;

/// <summary>A <see cref="TimeProvider"/> returning a fixed instant for deterministic tests.</summary>
internal sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;
}
