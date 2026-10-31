using System.Threading;
using System.Threading.Tasks;

namespace AdaraBox.Core.Stats;

/// <summary>
/// Service provider for hardware utilization metrics.
/// </summary>
public interface ISystemStatsProvider
{
    /// <summary>
    /// Reads hardware metrics safely without throwing unhandled exceptions.
    /// Unavailable individual metrics return null values (FR-ST-06).
    /// </summary>
    Task<StatsSnapshot> ReadAsync(CancellationToken ct = default);
}
