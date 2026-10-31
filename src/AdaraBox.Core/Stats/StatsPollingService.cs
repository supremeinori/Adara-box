using System;
using System.Threading;
using System.Threading.Tasks;
using AdaraBox.Core.Common;

namespace AdaraBox.Core.Stats;

/// <summary>
/// Background polling coordinator that retrieves system stats only when requested (active).
/// Conforms to NFR-PF-04 (zero polling when hidden/closed).
/// </summary>
public sealed class StatsPollingService : IDisposable
{
    private readonly ISystemStatsProvider _provider;
    private readonly int _intervalMs;
    private CancellationTokenSource? _cts;
    private Task? _pollingTask;
    private int _isReading;

    public event EventHandler<StatsSnapshot>? SnapshotAvailable;

    public bool IsPolling => _cts != null && !_cts.IsCancellationRequested;

    public StatsPollingService(ISystemStatsProvider provider, int intervalMs = UiConstants.StatsPollingIntervalMs)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        _intervalMs = intervalMs > 0 ? intervalMs : UiConstants.StatsPollingIntervalMs;
    }

    public void Start()
    {
        if (IsPolling) return;

        _cts = new CancellationTokenSource();
        var token = _cts.Token;

        _pollingTask = Task.Run(async () =>
        {
            // Initial read immediately
            await TickAsync(token).ConfigureAwait(false);

            using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(_intervalMs));
            while (!token.IsCancellationRequested)
            {
                try
                {
                    if (await timer.WaitForNextTickAsync(token).ConfigureAwait(false))
                    {
                        await TickAsync(token).ConfigureAwait(false);
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }, token);
    }

    public void Stop()
    {
        if (_cts != null)
        {
            _cts.Cancel();
            _cts.Dispose();
            _cts = null;
        }
    }

    private async Task TickAsync(CancellationToken token)
    {
        // Avoid overlapping reads
        if (Interlocked.CompareExchange(ref _isReading, 1, 0) != 0)
        {
            return;
        }

        try
        {
            if (token.IsCancellationRequested) return;

            var snapshot = await _provider.ReadAsync(token).ConfigureAwait(false);
            SnapshotAvailable?.Invoke(this, snapshot);
        }
        catch (OperationCanceledException)
        {
            // Normal during shutdown
        }
        catch
        {
            // Provider should never throw, but keep guard
        }
        finally
        {
            Interlocked.Exchange(ref _isReading, 0);
        }
    }

    public void Dispose()
    {
        Stop();
    }
}
