using System;
using System.Collections.Generic;
using AdaraBox.App.ViewModels;
using AdaraBox.Core.Stats;
using NSubstitute;
using Xunit;

namespace AdaraBox.App.Tests;

public class StatsPanelViewModelTests
{
    [Fact]
    public void DriveItemViewModel_HasReadWriteProperties_ToPreventWpfTwoWayBindingExceptions()
    {
        var drive = new DriveUsage("C:\\", 1_000_000_000, 400_000_000);
        var vm = new DriveItemViewModel(drive);

        Assert.Equal("C:\\", vm.Name);
        Assert.Equal(60.0, vm.PercentValue);
        Assert.Contains("60%", vm.PercentText);

        // Verify properties can be set without throwing, ensuring WPF TwoWay/RangeBase binding does not crash
        vm.PercentValue = 75.0;
        Assert.Equal(75.0, vm.PercentValue);

        vm.Name = "D:\\";
        Assert.Equal("D:\\", vm.Name);
    }

    [Fact]
    public void ApplySnapshot_PopulatesMetricsAndDrivesCorrectly()
    {
        var mockProvider = Substitute.For<ISystemStatsProvider>();
        var statsVm = new StatsPanelViewModel(mockProvider);

        var drives = new List<DriveUsage>
        {
            new("C:\\", 100_000_000, 50_000_000),
            new("D:\\", 200_000_000, 100_000_000)
        };
        var snapshot = new StatsSnapshot(
            DateTimeOffset.UtcNow,
            CpuPercent: 25.5,
            MemoryUsedBytes: 8_000_000_000,
            MemoryTotalBytes: 16_000_000_000,
            Drives: drives,
            GpuPercent: 12.3);

        statsVm.ApplySnapshot(snapshot);

        Assert.True(statsVm.IsCpuAvailable);
        Assert.Equal(25.5, statsVm.CpuValue);

        Assert.True(statsVm.IsMemoryAvailable);
        Assert.Equal(50.0, statsVm.MemoryValue);

        Assert.True(statsVm.IsGpuAvailable);
        Assert.Equal(12.3, statsVm.GpuValue);

        Assert.Equal(2, statsVm.Drives.Count);
        Assert.Equal("C:\\", statsVm.Drives[0].Name);
        Assert.Equal("D:\\", statsVm.Drives[1].Name);
    }
}
