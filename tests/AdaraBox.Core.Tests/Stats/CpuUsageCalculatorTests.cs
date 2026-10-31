using System;
using AdaraBox.Core.Stats;
using Xunit;

namespace AdaraBox.Core.Tests.Stats;

public class CpuUsageCalculatorTests
{
    [Fact]
    public void CalculateUsagePercent_Normal50Percent_ReturnsExpected()
    {
        // prev: idle=100, kernel=200, user=0
        // current: idle=150, kernel=300, user=0
        // deltaIdle = 50, deltaKernel = 100, deltaUser = 0 => total = 100
        // usage = 1 - 50/100 = 50%
        var prev = new CpuUsageCalculator.SystemTimesSample(100, 200, 0);
        var current = new CpuUsageCalculator.SystemTimesSample(150, 300, 0);

        var result = CpuUsageCalculator.CalculateUsagePercent(prev, current);

        Assert.NotNull(result);
        Assert.Equal(50.0, result.Value, 1);
    }

    [Fact]
    public void CalculateUsagePercent_ZeroUsage_ReturnsZero()
    {
        // deltaIdle = 100, deltaKernel = 100, deltaUser = 0 => total = 100, idle = 100 => 0%
        var prev = new CpuUsageCalculator.SystemTimesSample(100, 200, 0);
        var current = new CpuUsageCalculator.SystemTimesSample(200, 300, 0);

        var result = CpuUsageCalculator.CalculateUsagePercent(prev, current);

        Assert.NotNull(result);
        Assert.Equal(0.0, result.Value, 1);
    }

    [Fact]
    public void CalculateUsagePercent_FullUsage_Returns100()
    {
        // deltaIdle = 0, deltaKernel = 100, deltaUser = 50 => total = 150, idle = 0 => 100%
        var prev = new CpuUsageCalculator.SystemTimesSample(100, 200, 50);
        var current = new CpuUsageCalculator.SystemTimesSample(100, 300, 100);

        var result = CpuUsageCalculator.CalculateUsagePercent(prev, current);

        Assert.NotNull(result);
        Assert.Equal(100.0, result.Value, 1);
    }

    [Fact]
    public void CalculateUsagePercent_ZeroTotalDelta_ReturnsNull()
    {
        var prev = new CpuUsageCalculator.SystemTimesSample(100, 200, 50);
        var current = new CpuUsageCalculator.SystemTimesSample(100, 200, 50);

        var result = CpuUsageCalculator.CalculateUsagePercent(prev, current);

        Assert.Null(result);
    }

    [Fact]
    public void CalculateUsagePercent_CounterWrapOrBackward_ReturnsNull()
    {
        var prev = new CpuUsageCalculator.SystemTimesSample(200, 300, 50);
        var current = new CpuUsageCalculator.SystemTimesSample(100, 300, 50); // idle decreased

        var result = CpuUsageCalculator.CalculateUsagePercent(prev, current);

        Assert.Null(result);
    }

    [Fact]
    public void CalculateUsagePercent_IdleExceedsTotal_ClampedToZero()
    {
        // deltaIdle = 120, deltaKernel = 50, deltaUser = 50 => total = 100, idle = 120
        var prev = new CpuUsageCalculator.SystemTimesSample(0, 0, 0);
        var current = new CpuUsageCalculator.SystemTimesSample(120, 50, 50);

        var result = CpuUsageCalculator.CalculateUsagePercent(prev, current);

        Assert.NotNull(result);
        Assert.Equal(0.0, result.Value);
    }
}
