using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AdaraBox.Core.Resources;
using AdaraBox.Core.Workspaces;
using AdaraBox.Data.Infrastructure;
using AdaraBox.Data.Migrations;
using AdaraBox.Data.Repositories;
using Microsoft.Data.Sqlite;
using Xunit;
using Xunit.Abstractions;

namespace AdaraBox.Data.Tests;

public class PerformanceBenchmarkTests : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly string _testDbPath;
    private readonly SqliteConnectionFactory _factory;
    private readonly DatabaseMigrator _migrator;
    private readonly SqliteWorkspaceRepository _workspaceRepo;
    private readonly SqliteResourceRepository _resourceRepo;
    private readonly DateTimeOffset _testTime = DateTimeOffset.UtcNow;

    public PerformanceBenchmarkTests(ITestOutputHelper output)
    {
        _output = output;
        _testDbPath = Path.Combine(Path.GetTempPath(), $"adarabox_perf_{Guid.NewGuid():N}.db");

        _factory = new SqliteConnectionFactory(_testDbPath);
        _migrator = new DatabaseMigrator(_factory);
        _workspaceRepo = new SqliteWorkspaceRepository(_factory);
        _resourceRepo = new SqliteResourceRepository(_factory);

        _migrator.Migrate();
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        try
        {
            if (File.Exists(_testDbPath)) File.Delete(_testDbPath);
        }
        catch { }
    }

    [Theory]
    [InlineData(50)]
    [InlineData(200)]
    [InlineData(500)]
    public async Task Benchmark_LoadWorkspaceWithNResources(int resourceCount)
    {
        // 1. Arrange: Create workspace and N resources
        var workspace = new Workspace(Guid.NewGuid(), $"Workspace {resourceCount}", _testTime, _testTime, 1);
        await _workspaceRepo.AddAsync(workspace);

        for (int i = 0; i < resourceCount; i++)
        {
            var res = new Resource(
                id: Guid.NewGuid(),
                workspaceId: workspace.Id,
                name: $"Resource {i}",
                type: i % 2 == 0 ? ResourceType.File : ResourceType.Link,
                target: i % 2 == 0 ? $@"C:\Tools\tool_{i}.exe" : $"https://example.com/item/{i}",
                sortOrder: i,
                createdAt: _testTime,
                updatedAt: _testTime);
            await _resourceRepo.AddAsync(res);
        }

        // 2. Act: Measure retrieval time of workspace resources
        var sw = Stopwatch.StartNew();
        var resources = (await _resourceRepo.GetByWorkspaceAsync(workspace.Id)).ToList();
        sw.Stop();

        _output.WriteLine($"[PERF] Loading {resourceCount} resources took: {sw.ElapsedMilliseconds} ms (target for 200 items: < 300 ms)");

        // 3. Assert
        Assert.Equal(resourceCount, resources.Count);
        if (resourceCount == 200)
        {
            Assert.True(sw.ElapsedMilliseconds < 300, $"Loading 200 items took {sw.ElapsedMilliseconds} ms, expected < 300 ms");
        }
    }

    [Fact]
    public async Task Benchmark_OpenCloseCycle100Times_MemoryStaysStable()
    {
        // Setup a workspace with 100 items
        var workspace = new Workspace(Guid.NewGuid(), "Cycle Benchmark", _testTime, _testTime, 1);
        await _workspaceRepo.AddAsync(workspace);

        for (int i = 0; i < 100; i++)
        {
            var res = new Resource(
                id: Guid.NewGuid(),
                workspaceId: workspace.Id,
                name: $"Res {i}",
                type: ResourceType.Link,
                target: $"https://example.com/{i}",
                sortOrder: i,
                createdAt: _testTime,
                updatedAt: _testTime);
            await _resourceRepo.AddAsync(res);
        }

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        long initialMemory = GC.GetTotalMemory(true);

        for (int cycle = 0; cycle < 100; cycle++)
        {
            var items = (await _resourceRepo.GetByWorkspaceAsync(workspace.Id)).ToList();
            Assert.Equal(100, items.Count);
        }

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        long finalMemory = GC.GetTotalMemory(true);

        long diffBytes = finalMemory - initialMemory;
        double diffMb = diffBytes / (1024.0 * 1024.0);

        _output.WriteLine($"[PERF] Memory change after 100 open/close cycles: {diffMb:F2} MB (initial: {initialMemory / (1024 * 1024.0):F2} MB, final: {finalMemory / (1024 * 1024.0):F2} MB)");

        // Assert memory does not leak monotonically (< 15 MB)
        Assert.True(diffMb < 15.0, $"Memory grew by {diffMb:F2} MB after 100 cycles");
    }
}
