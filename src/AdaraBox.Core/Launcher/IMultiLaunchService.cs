using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AdaraBox.Core.Resources;

namespace AdaraBox.Core.Launcher;

/// <summary>
/// Service responsible for launching multiple resources sequentially in background.
/// </summary>
public interface IMultiLaunchService
{
    /// <summary>
    /// Launches all specified resources sequentially. One failure DOES NOT stop subsequent items.
    /// </summary>
    Task<LaunchSummary> LaunchAllAsync(
        IReadOnlyList<Resource> resources,
        IProgress<LaunchResult>? progress = null,
        CancellationToken ct = default);
}
