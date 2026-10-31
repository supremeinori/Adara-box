using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AdaraBox.Core.Launcher;
using AdaraBox.Core.Resources;
using Xunit;

namespace AdaraBox.Core.Tests;

public class MultiLaunchServiceTests
{
    private class FakeLauncher : IResourceLauncher
    {
        public Func<Resource, CancellationToken, Task<LaunchResult>> LaunchFunc { get; set; } =
            (res, ct) => Task.FromResult(new LaunchResult(res.Id, res.Name, true));

        public List<Guid> LaunchedResourceIds { get; } = new();

        public Task<LaunchResult> LaunchAsync(Resource resource, CancellationToken ct = default)
        {
            LaunchedResourceIds.Add(resource.Id);
            return LaunchFunc(resource, ct);
        }
    }

    private static Resource CreateResource(string name, ResourceType type = ResourceType.File)
    {
        return new Resource(
            Guid.NewGuid(),
            Guid.NewGuid(),
            name,
            type,
            $"C:\\test\\{name}",
            0,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task LaunchAllAsync_AllSucceed_ReturnsSuccessSummary()
    {
        // Arrange
        var fakeLauncher = new FakeLauncher();
        var service = new MultiLaunchService(fakeLauncher, TimeSpan.Zero);
        var r1 = CreateResource("Doc1.txt");
        var r2 = CreateResource("Doc2.txt");
        var r3 = CreateResource("Doc3.txt");
        var resources = new[] { r1, r2, r3 };

        // Act
        var summary = await service.LaunchAllAsync(resources);

        // Assert
        Assert.Equal(3, summary.Results.Count);
        Assert.Equal(3, summary.SuccessCount);
        Assert.Equal(0, summary.FailureCount);
        Assert.Equal(r1.Id, summary.Results[0].ResourceId);
        Assert.Equal(r2.Id, summary.Results[1].ResourceId);
        Assert.Equal(r3.Id, summary.Results[2].ResourceId);
    }

    [Fact]
    public async Task LaunchAllAsync_OneItemFails_RemainingItemsStillExecuted()
    {
        // Arrange (FR-OF-06)
        var fakeLauncher = new FakeLauncher();
        var r1 = CreateResource("App1.exe");
        var r2 = CreateResource("Missing.exe");
        var r3 = CreateResource("Folder3");
        var resources = new[] { r1, r2, r3 };

        fakeLauncher.LaunchFunc = (res, ct) =>
        {
            if (res.Id == r2.Id)
            {
                return Task.FromResult(new LaunchResult(
                    res.Id,
                    res.Name,
                    Success: false,
                    Reason: LaunchFailureReason.NotFound,
                    Message: "File tidak ditemukan"));
            }
            return Task.FromResult(new LaunchResult(res.Id, res.Name, Success: true));
        };

        var service = new MultiLaunchService(fakeLauncher, TimeSpan.Zero);

        // Act
        var summary = await service.LaunchAllAsync(resources);

        // Assert
        Assert.Equal(3, summary.Results.Count);
        Assert.Equal(2, summary.SuccessCount);
        Assert.Equal(1, summary.FailureCount);
        Assert.True(summary.Results[0].Success);
        Assert.False(summary.Results[1].Success);
        Assert.Equal(LaunchFailureReason.NotFound, summary.Results[1].Reason);
        Assert.True(summary.Results[2].Success);
        Assert.Equal(3, fakeLauncher.LaunchedResourceIds.Count);
    }

    [Fact]
    public async Task LaunchAllAsync_LauncherThrowsException_WrappedAsUnknownAndDoesNotLeak()
    {
        // Arrange
        var fakeLauncher = new FakeLauncher();
        var r1 = CreateResource("Faulty.exe");
        var r2 = CreateResource("Good.exe");

        fakeLauncher.LaunchFunc = (res, ct) =>
        {
            if (res.Id == r1.Id)
            {
                throw new InvalidOperationException("Boom in launcher!");
            }
            return Task.FromResult(new LaunchResult(res.Id, res.Name, Success: true));
        };

        var service = new MultiLaunchService(fakeLauncher, TimeSpan.Zero);

        // Act
        var summary = await service.LaunchAllAsync(new[] { r1, r2 });

        // Assert
        Assert.Equal(2, summary.Results.Count);
        Assert.False(summary.Results[0].Success);
        Assert.Equal(LaunchFailureReason.Unknown, summary.Results[0].Reason);
        Assert.True(summary.Results[1].Success);
    }

    [Fact]
    public async Task LaunchAllAsync_ResultOrder_MatchesInputOrder()
    {
        // Arrange
        var fakeLauncher = new FakeLauncher();
        var service = new MultiLaunchService(fakeLauncher, TimeSpan.Zero);
        var resources = new[]
        {
            CreateResource("A"),
            CreateResource("B"),
            CreateResource("C"),
            CreateResource("D")
        };

        // Act
        var summary = await service.LaunchAllAsync(resources);

        // Assert
        for (int i = 0; i < resources.Length; i++)
        {
            Assert.Equal(resources[i].Id, summary.Results[i].ResourceId);
            Assert.Equal(resources[i].Name, summary.Results[i].ResourceName);
        }
    }

    [Fact]
    public async Task LaunchAllAsync_CancellationRequested_StopsSubsequentItems()
    {
        // Arrange
        var fakeLauncher = new FakeLauncher();
        var cts = new CancellationTokenSource();
        var r1 = CreateResource("First.txt");
        var r2 = CreateResource("Second.txt");
        var r3 = CreateResource("Third.txt");

        fakeLauncher.LaunchFunc = (res, ct) =>
        {
            if (res.Id == r1.Id)
            {
                cts.Cancel(); // Cancel after first item
            }
            return Task.FromResult(new LaunchResult(res.Id, res.Name, Success: true));
        };

        var service = new MultiLaunchService(fakeLauncher, TimeSpan.Zero);

        // Act
        var summary = await service.LaunchAllAsync(new[] { r1, r2, r3 }, ct: cts.Token);

        // Assert
        Assert.Single(summary.Results);
        Assert.Equal(r1.Id, summary.Results[0].ResourceId);
        Assert.Single(fakeLauncher.LaunchedResourceIds);
    }

    [Fact]
    public async Task LaunchAllAsync_ReportsProgress_ForEachItem()
    {
        // Arrange
        var fakeLauncher = new FakeLauncher();
        var service = new MultiLaunchService(fakeLauncher, TimeSpan.Zero);
        var resources = new[]
        {
            CreateResource("Item1"),
            CreateResource("Item2")
        };

        var reported = new List<LaunchResult>();
        var progress = new Progress<LaunchResult>(item => reported.Add(item));

        // Act
        var summary = await service.LaunchAllAsync(resources, progress);

        // Give async progress dispatch a moment to finish invoking callbacks
        for (var i = 0; i < 20 && reported.Count < 2; i++)
        {
            await Task.Delay(15);
        }

        // Assert
        Assert.Equal(2, summary.Results.Count);
        Assert.Equal(2, reported.Count);
        Assert.Equal(resources[0].Id, reported[0].ResourceId);
        Assert.Equal(resources[1].Id, reported[1].ResourceId);
    }

    [Fact]
    public async Task LaunchAllAsync_EmptyOrNullInput_ReturnsEmptySummary()
    {
        // Arrange
        var fakeLauncher = new FakeLauncher();
        var service = new MultiLaunchService(fakeLauncher, TimeSpan.Zero);

        // Act
        var emptySummary = await service.LaunchAllAsync(Array.Empty<Resource>());
        var nullSummary = await service.LaunchAllAsync(null!);

        // Assert
        Assert.Empty(emptySummary.Results);
        Assert.Equal(0, emptySummary.SuccessCount);
        Assert.Equal(0, emptySummary.FailureCount);

        Assert.Empty(nullSummary.Results);
        Assert.Equal(0, nullSummary.SuccessCount);
        Assert.Equal(0, nullSummary.FailureCount);
    }
}
