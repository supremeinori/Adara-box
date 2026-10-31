using System;

namespace AdaraBox.Core.Common;

/// <summary>
/// Abstraction for system time to allow deterministic testing.
/// </summary>
public interface IClock
{
    DateTimeOffset UtcNow { get; }
}

public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
