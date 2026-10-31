using System.Collections.Generic;
using System.Linq;

namespace AdaraBox.Core.Launcher;

/// <summary>
/// Aggregated summary of multiple launched resources.
/// </summary>
public sealed record LaunchSummary(IReadOnlyList<LaunchResult> Results)
{
    public int SuccessCount => Results.Count(r => r.Success);
    public int FailureCount => Results.Count(r => !r.Success);
}
