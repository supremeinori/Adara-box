using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AdaraBox.Core.Common;
using AdaraBox.Core.Workspaces;
using NSubstitute;
using Xunit;

namespace AdaraBox.Core.Tests;

public class WorkspaceServiceTests
{
    private readonly IWorkspaceRepository _repository = Substitute.For<IWorkspaceRepository>();
    private readonly IClock _clock = Substitute.For<IClock>();
    private readonly DateTimeOffset _fixedTime = new(2026, 10, 31, 10, 0, 0, TimeSpan.Zero);

    public WorkspaceServiceTests()
    {
        _clock.UtcNow.Returns(_fixedTime);
    }

    [Theory]
    [InlineData(null, "workspace.name.empty")]
    [InlineData("", "workspace.name.empty")]
    [InlineData("   ", "workspace.name.empty")]
    [InlineData("Nama dengan karakter kontrol \u0000 di tengah", "workspace.name.invalid")]
    public void Validator_Rejects_Invalid_Names(string? name, string expectedErrorCode)
    {
        var result = WorkspaceNameValidator.Validate(name);
        Assert.False(result.Success);
        Assert.Equal(expectedErrorCode, result.Error?.Code);
    }

    [Fact]
    public void Validator_Rejects_Names_Longer_Than_60_Characters()
    {
        var longName = new string('A', 61);
        var result = WorkspaceNameValidator.Validate(longName);
        Assert.False(result.Success);
        Assert.Equal("workspace.name.too_long", result.Error?.Code);
    }

    [Fact]
    public void Validator_Trims_And_Accepts_Valid_Names()
    {
        var result = WorkspaceNameValidator.Validate("  Proyek Keren  ");
        Assert.True(result.Success);
        Assert.Equal("Proyek Keren", result.Value);
    }

    [Fact]
    public async Task CreateAsync_Creates_Workspace_With_Next_SortOrder()
    {
        _repository.GetNextSortOrderAsync(Arg.Any<CancellationToken>()).Returns(3);
        var service = new WorkspaceService(_repository, _clock);

        var result = await service.CreateAsync("Workspace Baru");

        Assert.True(result.Success);
        Assert.NotNull(result.Value);
        Assert.Equal("Workspace Baru", result.Value.Name);
        Assert.Equal(3, result.Value.SortOrder);
        Assert.Equal(_fixedTime, result.Value.CreatedAt);
        Assert.Equal(_fixedTime, result.Value.UpdatedAt);

        await _repository.Received(1).AddAsync(Arg.Is<Workspace>(w => w.Name == "Workspace Baru" && w.SortOrder == 3), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RenameAsync_Returns_NotFound_If_Workspace_Missing()
    {
        var missingId = Guid.NewGuid();
        _repository.GetAsync(missingId, Arg.Any<CancellationToken>()).Returns((Workspace?)null);
        var service = new WorkspaceService(_repository, _clock);

        var result = await service.RenameAsync(missingId, "Nama Baru");

        Assert.False(result.Success);
        Assert.Equal("workspace.not_found", result.Error?.Code);
    }

    [Fact]
    public async Task RenameAsync_Updates_Name_When_Found()
    {
        var existingId = Guid.NewGuid();
        var existing = new Workspace(existingId, "Lama", _fixedTime, _fixedTime, 0);
        _repository.GetAsync(existingId, Arg.Any<CancellationToken>()).Returns(existing);

        var service = new WorkspaceService(_repository, _clock);
        var result = await service.RenameAsync(existingId, "Baru");

        Assert.True(result.Success);
        await _repository.Received(1).UpdateNameAsync(existingId, "Baru", _fixedTime, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_Allows_Duplicate_Names()
    {
        _repository.GetNextSortOrderAsync(Arg.Any<CancellationToken>()).Returns(0);
        var service = new WorkspaceService(_repository, _clock);

        var res1 = await service.CreateAsync("Work");
        var res2 = await service.CreateAsync("Work");

        Assert.True(res1.Success);
        Assert.True(res2.Success);
        Assert.NotEqual(res1.Value!.Id, res2.Value!.Id);
        Assert.Equal("Work", res1.Value!.Name);
        Assert.Equal("Work", res2.Value!.Name);
    }

    [Fact]
    public async Task DeleteAsync_Calls_Repository_Without_Touching_Disk()
    {
        var existingId = Guid.NewGuid();
        var existing = new Workspace(existingId, "Carry On", _fixedTime, _fixedTime, 0);
        _repository.GetAsync(existingId, Arg.Any<CancellationToken>()).Returns(existing);

        var service = new WorkspaceService(_repository, _clock);
        var result = await service.DeleteAsync(existingId);

        Assert.True(result.Success);
        await _repository.Received(1).DeleteAsync(existingId, Arg.Any<CancellationToken>());
    }
}
