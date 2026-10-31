using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AdaraBox.Core.Resources;
using AdaraBox.Core.Workspaces;
using AdaraBox.Data.Infrastructure;
using AdaraBox.Data.Migrations;
using AdaraBox.Data.Repositories;
using Xunit;

namespace AdaraBox.Data.Tests;

public class ResourceDataTests : IDisposable
{
    private readonly string _testDbPath;
    private readonly SqliteConnectionFactory _factory;
    private readonly SqliteWorkspaceRepository _workspaceRepo;
    private readonly SqliteResourceRepository _resourceRepo;
    private readonly DateTimeOffset _testTime = new(2026, 10, 31, 10, 0, 0, TimeSpan.Zero);

    public ResourceDataTests()
    {
        _testDbPath = Path.Combine(Path.GetTempPath(), $"adarabox_resource_test_{Guid.NewGuid():N}.db");
        _factory = new SqliteConnectionFactory(_testDbPath);

        var migrator = new DatabaseMigrator(_factory);
        migrator.Migrate();

        _workspaceRepo = new SqliteWorkspaceRepository(_factory);
        _resourceRepo = new SqliteResourceRepository(_factory);
    }

    public void Dispose()
    {
        if (File.Exists(_testDbPath))
        {
            try { File.Delete(_testDbPath); } catch { }
        }
    }

    [Fact]
    public async Task Add_And_Get_Resource_Persists_All_Properties()
    {
        var ws = new Workspace(Guid.NewGuid(), "WS Test", _testTime, _testTime, 0);
        await _workspaceRepo.AddAsync(ws);

        var res = new Resource(
            id: Guid.NewGuid(),
            workspaceId: ws.Id,
            name: "App Test",
            type: ResourceType.Application,
            target: @"C:\Program Files\App's Path (Test)\app.exe", // includes single quote
            sortOrder: 1,
            createdAt: _testTime,
            updatedAt: _testTime);

        await _resourceRepo.AddAsync(res);

        var retrieved = await _resourceRepo.GetAsync(res.Id);
        Assert.NotNull(retrieved);
        Assert.Equal(res.Id, retrieved.Id);
        Assert.Equal(ws.Id, retrieved.WorkspaceId);
        Assert.Equal("App Test", retrieved.Name);
        Assert.Equal(ResourceType.Application, retrieved.Type);
        Assert.Equal(@"C:\Program Files\App's Path (Test)\app.exe", retrieved.Target);
        Assert.Equal(1, retrieved.SortOrder);
        Assert.Equal(_testTime, retrieved.CreatedAt);
        Assert.Equal(_testTime, retrieved.UpdatedAt);
    }

    [Fact]
    public async Task Deleting_Workspace_Cascades_To_Resources()
    {
        var ws = new Workspace(Guid.NewGuid(), "WS Cascade", _testTime, _testTime, 0);
        await _workspaceRepo.AddAsync(ws);

        var res = new Resource(Guid.NewGuid(), ws.Id, "Doc", ResourceType.File, @"C:\doc.pdf", 0, _testTime, _testTime);
        await _resourceRepo.AddAsync(res);

        // Verify exists
        var found = await _resourceRepo.GetAsync(res.Id);
        Assert.NotNull(found);

        // Delete workspace
        var deletedWs = await _workspaceRepo.DeleteAsync(ws.Id);
        Assert.True(deletedWs);

        // Resource must be cascaded and deleted automatically
        var afterDelete = await _resourceRepo.GetAsync(res.Id);
        Assert.Null(afterDelete);
    }

    [Fact]
    public async Task AddRangeAsync_Is_Atomic_And_Rolls_Back_On_Failure()
    {
        var ws = new Workspace(Guid.NewGuid(), "WS Atomic", _testTime, _testTime, 0);
        await _workspaceRepo.AddAsync(ws);

        var validRes = new Resource(Guid.NewGuid(), ws.Id, "Valid", ResourceType.File, @"C:\valid.txt", 0, _testTime, _testTime);
        // Invalid res referencing non-existent foreign key workspace
        var invalidRes = new Resource(Guid.NewGuid(), Guid.NewGuid(), "Invalid", ResourceType.File, @"C:\inv.txt", 1, _testTime, _testTime);

        // Attempting to add range should throw due to foreign key violation on invalidRes
        await Assert.ThrowsAnyAsync<Exception>(() => _resourceRepo.AddRangeAsync(new[] { validRes, invalidRes }));

        // Due to transaction rollback, even validRes must NOT be persisted!
        var checkValid = await _resourceRepo.GetAsync(validRes.Id);
        Assert.Null(checkValid);
    }

    [Fact]
    public async Task ReorderAsync_Updates_SortOrder_In_Transaction()
    {
        var ws = new Workspace(Guid.NewGuid(), "WS Reorder", _testTime, _testTime, 0);
        await _workspaceRepo.AddAsync(ws);

        var r1 = new Resource(Guid.NewGuid(), ws.Id, "Item 1", ResourceType.Link, "https://1.com", 0, _testTime, _testTime);
        var r2 = new Resource(Guid.NewGuid(), ws.Id, "Item 2", ResourceType.Link, "https://2.com", 1, _testTime, _testTime);
        var r3 = new Resource(Guid.NewGuid(), ws.Id, "Item 3", ResourceType.Link, "https://3.com", 2, _testTime, _testTime);

        await _resourceRepo.AddRangeAsync(new[] { r1, r2, r3 });

        // Reverse order: r3, r2, r1
        await _resourceRepo.ReorderAsync(ws.Id, new[] { r3.Id, r2.Id, r1.Id });

        var list = await _resourceRepo.GetByWorkspaceAsync(ws.Id);
        Assert.Equal(3, list.Count);
        Assert.Equal(r3.Id, list[0].Id);
        Assert.Equal(r2.Id, list[1].Id);
        Assert.Equal(r1.Id, list[2].Id);
    }

    [Fact]
    public async Task Handles_Special_Characters_And_Non_Ascii_In_Target()
    {
        var ws = new Workspace(Guid.NewGuid(), "WS Unicode", _testTime, _testTime, 0);
        await _workspaceRepo.AddAsync(ws);

        var unicodeTarget = @"D:\Projek\プロジェクト\ファイル's `test`\dokument.pdf";
        var res = new Resource(Guid.NewGuid(), ws.Id, "日本語ドキュメント", ResourceType.File, unicodeTarget, 0, _testTime, _testTime);

        await _resourceRepo.AddAsync(res);

        var retrieved = await _resourceRepo.GetAsync(res.Id);
        Assert.NotNull(retrieved);
        Assert.Equal("日本語ドキュメント", retrieved.Name);
        Assert.Equal(unicodeTarget, retrieved.Target);
    }
}
