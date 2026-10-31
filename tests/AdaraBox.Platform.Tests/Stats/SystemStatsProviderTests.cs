using System.Threading.Tasks;
using AdaraBox.Platform.Stats;
using Xunit;

namespace AdaraBox.Platform.Tests.Stats;

public class SystemStatsProviderTests
{
    [Fact]
    public async Task ReadAsync_ReturnsValidSnapshot_WithoutThrowing()
    {
        var provider = new SystemStatsProvider();

        // First read establishes baseline for CPU
        var snapshot1 = await provider.ReadAsync();
        Assert.NotNull(snapshot1);
        Assert.NotNull(snapshot1.Drives);

        // Memory should be successfully read on Windows
        Assert.NotNull(snapshot1.MemoryTotalBytes);
        Assert.NotNull(snapshot1.MemoryUsedBytes);
        Assert.True(snapshot1.MemoryTotalBytes > 0);
        Assert.True(snapshot1.MemoryUsedBytes >= 0);

        // Second read calculates CPU usage
        await Task.Delay(50);
        var snapshot2 = await provider.ReadAsync();
        Assert.NotNull(snapshot2);

        // If CPU is available, it should be between 0 and 100
        if (snapshot2.CpuPercent.HasValue)
        {
            Assert.InRange(snapshot2.CpuPercent.Value, 0.0, 100.0);
        }
    }
}
