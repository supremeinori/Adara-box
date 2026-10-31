using System;
using System.IO;
using System.Threading.Tasks;
using AdaraBox.Core.Media;
using AdaraBox.Data.Infrastructure;
using AdaraBox.Data.Migrations;
using AdaraBox.Data.Repositories;
using Xunit;

namespace AdaraBox.Data.Tests;

public class MediaLinkDataTests : IDisposable
{
    private readonly string _dbPath;
    private readonly SqliteConnectionFactory _factory;
    private readonly DateTimeOffset _fixedTime = new(2026, 10, 31, 12, 0, 0, TimeSpan.Zero);

    public MediaLinkDataTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"adarabox_test_media_{Guid.NewGuid():N}.db");
        _factory = new SqliteConnectionFactory(_dbPath);

        var migrator = new DatabaseMigrator(_factory);
        migrator.Migrate();
    }

    public void Dispose()
    {
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        if (File.Exists(_dbPath))
        {
            try { File.Delete(_dbPath); } catch { }
        }
    }

    [Fact]
    public async Task Insert_And_List_PersistsAndOrdersCorrectly()
    {
        var repo = new SqliteMediaLinkRepository(_factory);

        var link1 = new MediaLink(Guid.NewGuid(), "YouTube Music", "https://music.youtube.com", 0, _fixedTime, _fixedTime);
        var link2 = new MediaLink(Guid.NewGuid(), "Spotify", "https://open.spotify.com", 1, _fixedTime, _fixedTime);

        await repo.InsertAsync(link1);
        await repo.InsertAsync(link2);

        var list = await repo.ListAsync();

        Assert.Equal(2, list.Count);
        Assert.Equal("YouTube Music", list[0].Name);
        Assert.Equal("Spotify", list[1].Name);
    }

    [Fact]
    public async Task GetById_ReturnsLink_OrNull()
    {
        var repo = new SqliteMediaLinkRepository(_factory);
        var id = Guid.NewGuid();
        var link = new MediaLink(id, "SoundCloud", "https://soundcloud.com", 0, _fixedTime, _fixedTime);

        await repo.InsertAsync(link);

        var fetched = await repo.GetByIdAsync(id);
        Assert.NotNull(fetched);
        Assert.Equal("SoundCloud", fetched.Name);

        var missing = await repo.GetByIdAsync(Guid.NewGuid());
        Assert.Null(missing);
    }

    [Fact]
    public async Task Update_ModifiesExistingRecord()
    {
        var repo = new SqliteMediaLinkRepository(_factory);
        var id = Guid.NewGuid();
        var link = new MediaLink(id, "Radio", "https://radio.com", 0, _fixedTime, _fixedTime);

        await repo.InsertAsync(link);

        var updated = link with { Name = "Radio Stream", Url = "https://stream.radio.com" };
        await repo.UpdateAsync(updated);

        var fetched = await repo.GetByIdAsync(id);
        Assert.NotNull(fetched);
        Assert.Equal("Radio Stream", fetched.Name);
        Assert.Equal("https://stream.radio.com", fetched.Url);
    }

    [Fact]
    public async Task Delete_RemovesRecord()
    {
        var repo = new SqliteMediaLinkRepository(_factory);
        var id = Guid.NewGuid();
        var link = new MediaLink(id, "Podcast", "https://podcast.com", 0, _fixedTime, _fixedTime);

        await repo.InsertAsync(link);
        await repo.DeleteAsync(id);

        var fetched = await repo.GetByIdAsync(id);
        Assert.Null(fetched);
    }

    [Fact]
    public async Task UpdateOrder_ReordersElements()
    {
        var repo = new SqliteMediaLinkRepository(_factory);
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();

        await repo.InsertAsync(new MediaLink(id1, "Item 1", "https://1.com", 0, _fixedTime, _fixedTime));
        await repo.InsertAsync(new MediaLink(id2, "Item 2", "https://2.com", 1, _fixedTime, _fixedTime));

        // Reorder item 2 before item 1
        await repo.UpdateOrderAsync(new[] { id2, id1 });

        var list = await repo.ListAsync();
        Assert.Equal(2, list.Count);
        Assert.Equal(id2, list[0].Id);
        Assert.Equal(id1, list[1].Id);
    }
}
