using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AdaraBox.Core.Common;
using AdaraBox.Core.Media;
using NSubstitute;
using Xunit;

namespace AdaraBox.Core.Tests;

public class MediaLinkServiceTests
{
    private readonly IMediaLinkRepository _repo = Substitute.For<IMediaLinkRepository>();
    private readonly IClock _clock = Substitute.For<IClock>();
    private readonly DateTimeOffset _fixedTime = new(2026, 10, 31, 12, 0, 0, TimeSpan.Zero);

    public MediaLinkServiceTests()
    {
        _clock.UtcNow.Returns(_fixedTime);
    }

    private MediaLinkService CreateService() => new(_repo, _clock);

    [Fact]
    public async Task CreateAsync_ValidInput_InsertsAndReturnsOk()
    {
        _repo.ListAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult<IReadOnlyList<MediaLink>>(Array.Empty<MediaLink>()));
        var svc = CreateService();

        var result = await svc.CreateAsync("Spotify Web", "https://open.spotify.com");

        Assert.True(result.Success);
        Assert.NotNull(result.Value);
        Assert.Equal("Spotify Web", result.Value.Name);
        Assert.Equal("https://open.spotify.com", result.Value.Url);
        Assert.Equal(0, result.Value.SortOrder);
        await _repo.Received(1).InsertAsync(Arg.Is<MediaLink>(m => m.Name == "Spotify Web"), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task CreateAsync_EmptyName_Fails(string? name)
    {
        var svc = CreateService();
        var result = await svc.CreateAsync(name!, "https://open.spotify.com");

        Assert.False(result.Success);
        Assert.Equal("media.name.empty", result.Error?.Code);
    }

    [Fact]
    public async Task CreateAsync_NameTooLong_Fails()
    {
        var longName = new string('A', 81);
        var svc = CreateService();
        var result = await svc.CreateAsync(longName, "https://open.spotify.com");

        Assert.False(result.Success);
        Assert.Equal("media.name.too_long", result.Error?.Code);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task CreateAsync_EmptyUrl_Fails(string? url)
    {
        var svc = CreateService();
        var result = await svc.CreateAsync("Valid Name", url!);

        Assert.False(result.Success);
        Assert.Equal("media.url.empty", result.Error?.Code);
    }

    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("file:///C:/windows/notepad.exe")]
    [InlineData("data:text/html,<h1>hi</h1>")]
    [InlineData("ftp://files.example.com")]
    [InlineData("not a url")]
    public async Task CreateAsync_InvalidOrDangerousUrl_Fails(string dangerousUrl)
    {
        var svc = CreateService();
        var result = await svc.CreateAsync("Test", dangerousUrl);

        Assert.False(result.Success);
        Assert.Equal("media.url.invalid", result.Error?.Code);
    }

    [Fact]
    public async Task UpdateAsync_Existing_UpdatesAndReturnsOk()
    {
        var id = Guid.NewGuid();
        var existing = new MediaLink(id, "Old", "https://old.com", 0, _fixedTime, _fixedTime);
        _repo.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(Task.FromResult<MediaLink?>(existing));

        var svc = CreateService();
        var result = await svc.UpdateAsync(id, "New Name", "https://new.com");

        Assert.True(result.Success);
        await _repo.Received(1).UpdateAsync(Arg.Is<MediaLink>(m => m.Id == id && m.Name == "New Name" && m.Url == "https://new.com"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_NotFound_Fails()
    {
        _repo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult<MediaLink?>(null));

        var svc = CreateService();
        var result = await svc.UpdateAsync(Guid.NewGuid(), "New", "https://new.com");

        Assert.False(result.Success);
        Assert.Equal("media.not_found", result.Error?.Code);
    }

    [Fact]
    public async Task DeleteAsync_Existing_DeletesAndReturnsOk()
    {
        var id = Guid.NewGuid();
        var existing = new MediaLink(id, "Track", "https://track.com", 0, _fixedTime, _fixedTime);
        _repo.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(Task.FromResult<MediaLink?>(existing));

        var svc = CreateService();
        var result = await svc.DeleteAsync(id);

        Assert.True(result.Success);
        await _repo.Received(1).DeleteAsync(id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_NotFound_Fails()
    {
        _repo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult<MediaLink?>(null));

        var svc = CreateService();
        var result = await svc.DeleteAsync(Guid.NewGuid());

        Assert.False(result.Success);
        Assert.Equal("media.not_found", result.Error?.Code);
    }
}
