using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AdaraBox.Core.Stats;
using NSubstitute;
using Xunit;

namespace AdaraBox.Core.Tests.Stats;

public class StatsPollingServiceTests
{
    [Fact]
    public void Start_TriggersReadAsync_AndInvokesSnapshotAvailable()
    {
        var provider = Substitute.For<ISystemStatsProvider>();
        var dummySnapshot = new StatsSnapshot(
            DateTimeOffset.UtcNow,
            25.0,
            4000000000,
            16000000000,
            Array.Empty<DriveUsage>(),
            null);

        provider.ReadAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(dummySnapshot));

        using var service = new StatsPollingService(provider, intervalMs: 50);

        StatsSnapshot? received = null;
        using var signal = new ManualResetEventSlim(false);
        service.SnapshotAvailable += (s, snap) =>
        {
            received = snap;
            signal.Set();
        };

        service.Start();
        Assert.True(service.IsPolling);

        var signaled = signal.Wait(1000);
        Assert.True(signaled);
        Assert.NotNull(received);
        Assert.Equal(25.0, received.CpuPercent);

        service.Stop();
        Assert.False(service.IsPolling);
    }

    [Fact]
    public void Stop_WhenNotStarted_DoesNotThrow()
    {
        var provider = Substitute.For<ISystemStatsProvider>();
        using var service = new StatsPollingService(provider, intervalMs: 50);

        service.Stop();
        Assert.False(service.IsPolling);
    }
}
