using System;
using System.IO;
using System.Threading.Tasks;
using AdaraBox.Core.Settings;
using AdaraBox.Core.Workspaces;
using AdaraBox.Data.Infrastructure;
using AdaraBox.Data.Migrations;
using AdaraBox.Data.Repositories;
using Dapper;
using Xunit;

namespace AdaraBox.Data.Tests;

public class WorkspaceDataTests : IDisposable
{
    private readonly string _testDir;
    private readonly string _dbPath;
    private readonly IDbConnectionFactory _factory;

    public WorkspaceDataTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), "AdaraBox_M02Tests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testDir);
        _dbPath = Path.Combine(_testDir, "adarabox_test.db");
        _factory = new SqliteConnectionFactory(_dbPath);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_testDir))
            {
                Directory.Delete(_testDir, true);
            }
        }
        catch
        {
            // ignore temp cleanup failure
        }
    }

    [Fact]
    public void Migrator_Applies_Initial_Schema_And_Sets_UserVersion()
    {
        var migrator = new DatabaseMigrator(_factory);
        migrator.Migrate();

        using var connection = _factory.CreateConnection();
        var version = connection.ExecuteScalar<int>("PRAGMA user_version;");
        Assert.Equal(2, version);

        // Verify tables exist
        var workspaceTableCount = connection.ExecuteScalar<int>("SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='workspace';");
        var resourceTableCount = connection.ExecuteScalar<int>("SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='resource';");
        var settingsTableCount = connection.ExecuteScalar<int>("SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='settings';");
        var mediaLinkTableCount = connection.ExecuteScalar<int>("SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='media_link';");

        Assert.Equal(1, workspaceTableCount);
        Assert.Equal(1, resourceTableCount);
        Assert.Equal(1, settingsTableCount);
        Assert.Equal(1, mediaLinkTableCount);
    }

    [Fact]
    public void Migrator_Is_Idempotent()
    {
        var migrator = new DatabaseMigrator(_factory);

        // Run twice
        migrator.Migrate();
        migrator.Migrate();

        using var connection = _factory.CreateConnection();
        var version = connection.ExecuteScalar<int>("PRAGMA user_version;");
        Assert.Equal(2, version);
    }

    [Fact]
    public void Migrator_Rolls_Back_On_Failure_Without_Corrupting_Database()
    {
        var migrator = new DatabaseMigrator(_factory);
        migrator.Migrate();

        var faultyMigration = new FaultyTestMigration();
        var migratorWithFault = new DatabaseMigrator(_factory, new IMigration[] { new Migration001_InitialSchema(), new Migration002_MediaLinksSchema(), faultyMigration });

        var ex = Assert.Throws<InvalidOperationException>(() => migratorWithFault.Migrate());
        Assert.Contains("failed and was rolled back", ex.Message);

        using var connection = _factory.CreateConnection();
        var version = connection.ExecuteScalar<int>("PRAGMA user_version;");
        // user_version must remain at 2 because migration 3 was rolled back
        Assert.Equal(2, version);
    }

    private sealed class FaultyTestMigration : IMigration
    {
        public int Version => 3;
        public string Description => "Broken migration for rollback test";
        public string Sql => "CREATE TABLE valid_part (id TEXT); SYNTAX ERROR INTENTIONAL;";
    }

    [Fact]
    public async Task ForeignKey_Cascade_Deletes_Resources_When_Workspace_Deleted()
    {
        var migrator = new DatabaseMigrator(_factory);
        migrator.Migrate();

        var repo = new SqliteWorkspaceRepository(_factory);
        var workspaceId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var workspace = new Workspace(workspaceId, "Proyek Utama", now, now, 0);

        await repo.AddAsync(workspace);

        using (var conn = _factory.CreateConnection())
        {
            // Insert resource referencing this workspace
            var resourceSql = @"
                INSERT INTO resource (id, workspace_id, name, type, target, sort_order, created_at, updated_at)
                VALUES (@Id, @WorkspaceId, @Name, @Type, @Target, 0, @CreatedAt, @UpdatedAt);
            ";
            await conn.ExecuteAsync(resourceSql, new
            {
                Id = Guid.NewGuid(),
                WorkspaceId = workspaceId,
                Name = "Dokumen Desain",
                Type = "file",
                Target = "C:\\docs\\design.pdf",
                CreatedAt = now,
                UpdatedAt = now
            });

            var resCountBefore = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM resource WHERE workspace_id = @WId;", new { WId = workspaceId });
            Assert.Equal(1, resCountBefore);
        }

        // Delete workspace
        var deleted = await repo.DeleteAsync(workspaceId);
        Assert.True(deleted);

        using (var conn = _factory.CreateConnection())
        {
            var wsCount = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM workspace WHERE id = @WId;", new { WId = workspaceId });
            var resCountAfter = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM resource WHERE workspace_id = @WId;", new { WId = workspaceId });

            Assert.Equal(0, wsCount);
            // CASCADE delete confirmed!
            Assert.Equal(0, resCountAfter);
        }
    }

    [Fact]
    public async Task Workspace_Persists_Across_Connection_Restarts()
    {
        var migrator = new DatabaseMigrator(_factory);
        migrator.Migrate();

        var repo1 = new SqliteWorkspaceRepository(_factory);
        var id = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var ws = new Workspace(id, "Persistence Test Workspace", now, now, 1);
        await repo1.AddAsync(ws);

        // Simulate application restart by creating completely new factory & repository
        var restartFactory = new SqliteConnectionFactory(_dbPath);
        var repo2 = new SqliteWorkspaceRepository(restartFactory);

        var retrieved = await repo2.GetAsync(id);
        Assert.NotNull(retrieved);
        Assert.Equal(id, retrieved.Id);
        Assert.Equal("Persistence Test Workspace", retrieved.Name);
        Assert.Equal(1, retrieved.SortOrder);
    }

    [Fact]
    public async Task SettingsStore_Upsert_Works_As_Expected()
    {
        var migrator = new DatabaseMigrator(_factory);
        migrator.Migrate();

        var store = new SqliteSettingsStore(_factory);

        // Initially null
        var val1 = await store.GetAsync(SettingKeys.TopBarVisibility);
        Assert.Null(val1);

        // Insert
        await store.SetAsync(SettingKeys.TopBarVisibility, "Hidden");
        var val2 = await store.GetAsync(SettingKeys.TopBarVisibility);
        Assert.Equal("Hidden", val2);

        // Update (upsert)
        await store.SetAsync(SettingKeys.TopBarVisibility, "AlwaysVisible");
        var val3 = await store.GetAsync(SettingKeys.TopBarVisibility);
        Assert.Equal("AlwaysVisible", val3);
    }

    [Fact]
    public async Task Unicode_And_Spaces_In_Database_Path_Work_Flawlessly()
    {
        var unicodeDir = Path.Combine(_testDir, "Folder Dokumén ü & Spasi");
        Directory.CreateDirectory(unicodeDir);
        var dbPath = Path.Combine(unicodeDir, "adarabox_unicode.db");

        var unicodeFactory = new SqliteConnectionFactory(dbPath);
        var migrator = new DatabaseMigrator(unicodeFactory);
        migrator.Migrate();

        var repo = new SqliteWorkspaceRepository(unicodeFactory);
        var id = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var ws = new Workspace(id, "Workspace Unicode üöä", now, now, 0);

        await repo.AddAsync(ws);
        var loaded = await repo.GetAsync(id);

        Assert.NotNull(loaded);
        Assert.Equal("Workspace Unicode üöä", loaded.Name);
    }
}
