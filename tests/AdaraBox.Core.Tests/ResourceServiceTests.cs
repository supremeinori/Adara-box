using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AdaraBox.Core.Common;
using AdaraBox.Core.Resources;
using AdaraBox.Core.Workspaces;
using NSubstitute;
using Xunit;

namespace AdaraBox.Core.Tests;

public class ResourceServiceTests
{
    private readonly IResourceRepository _resourceRepository = Substitute.For<IResourceRepository>();
    private readonly IWorkspaceRepository _workspaceRepository = Substitute.For<IWorkspaceRepository>();
    private readonly IClock _clock = Substitute.For<IClock>();
    private readonly DateTimeOffset _fixedTime = new(2026, 10, 31, 10, 0, 0, TimeSpan.Zero);

    public ResourceServiceTests()
    {
        _clock.UtcNow.Returns(_fixedTime);
    }

    #region Validator Tests

    [Theory]
    [InlineData("example.com", "https://example.com/")]
    [InlineData("https://github.com/supremeinori/Adara-box", "https://github.com/supremeinori/Adara-box")]
    [InlineData("http://localhost:8080/dashboard", "http://localhost:8080/dashboard")]
    public void Validator_Accepts_And_Normalizes_Valid_Links(string input, string expectedUrl)
    {
        var result = ResourceTargetValidator.Validate(ResourceType.Link, input, null);
        Assert.True(result.Success);
        Assert.Equal(expectedUrl, result.Value.NormalizedTarget);
    }

    [Theory]
    [InlineData("file://C:/Windows/System32/calc.exe")]
    [InlineData("javascript:alert(1)")]
    [InlineData("data:text/html,<html></html>")]
    [InlineData("ms-settings:privacy")]
    [InlineData("ftp://files.example.com")]
    public void Validator_Rejects_Dangerous_Link_Schemes(string dangerousUrl)
    {
        var result = ResourceTargetValidator.Validate(ResourceType.Link, dangerousUrl, null);
        Assert.False(result.Success);
        Assert.Equal("resource.target.invalid", result.Error?.Code);
    }

    [Fact]
    public void Validator_Rejects_Link_Exceeding_2048_Characters()
    {
        var longUrl = "https://example.com/" + new string('a', 2050);
        var result = ResourceTargetValidator.Validate(ResourceType.Link, longUrl, null);
        Assert.False(result.Success);
        Assert.Equal("resource.target.invalid", result.Error?.Code);
    }

    [Theory]
    [InlineData(@"C:\Tools\app.exe", "app")]
    [InlineData(@"D:\Shortcuts\Browser.lnk", "Browser")]
    [InlineData(@"shell:AppsFolder\Microsoft.WindowsTerminal_8wekyb3d8bbwe!App", "Microsoft.WindowsTerminal_8wekyb3d8bbwe!App")]
    public void Validator_Accepts_Valid_Applications(string target, string expectedName)
    {
        var result = ResourceTargetValidator.Validate(ResourceType.Application, target, null);
        Assert.True(result.Success);
        Assert.Equal(expectedName, result.Value.DisplayName);
    }

    [Theory]
    [InlineData(@"C:\Script\run.bat")]
    [InlineData(@"C:\Script\test.cmd")]
    [InlineData(@"C:\Script\file.txt")]
    public void Validator_Rejects_Application_With_Invalid_Extension(string nonAppTarget)
    {
        var result = ResourceTargetValidator.Validate(ResourceType.Application, nonAppTarget, null);
        Assert.False(result.Success);
        Assert.Equal("resource.type.mismatch", result.Error?.Code);
    }

    [Theory]
    [InlineData("relative/path/document.pdf")]
    [InlineData("document.pdf")]
    [InlineData("..\\secret.txt")]
    public void Validator_Rejects_Relative_Path_For_File_And_Folder(string relativePath)
    {
        var fileResult = ResourceTargetValidator.Validate(ResourceType.File, relativePath, null);
        Assert.False(fileResult.Success);
        Assert.Equal("resource.target.invalid", fileResult.Error?.Code);

        var folderResult = ResourceTargetValidator.Validate(ResourceType.Folder, relativePath, null);
        Assert.False(folderResult.Success);
        Assert.Equal("resource.target.invalid", folderResult.Error?.Code);
    }

    [Fact]
    public void Validator_Rejects_Display_Name_Longer_Than_80_Characters()
    {
        var longName = new string('A', 81);
        var result = ResourceTargetValidator.Validate(ResourceType.File, @"C:\Docs\file.txt", longName);
        Assert.False(result.Success);
        Assert.Equal("resource.name.too_long", result.Error?.Code);
    }

    [Fact]
    public void Validator_Rejects_Control_Characters_In_Display_Name()
    {
        var invalidName = "Laporan\u0000Tahunan";
        var result = ResourceTargetValidator.Validate(ResourceType.File, @"C:\Docs\file.txt", invalidName);
        Assert.False(result.Success);
        Assert.Equal("resource.name.invalid", result.Error?.Code);
    }

    #endregion

    #region Service Tests

    [Fact]
    public async Task AddAsync_Returns_WorkspaceNotFound_If_Workspace_Missing()
    {
        var workspaceId = Guid.NewGuid();
        _workspaceRepository.GetAsync(workspaceId, Arg.Any<CancellationToken>()).Returns((Workspace?)null);

        var service = new ResourceService(_resourceRepository, _workspaceRepository, _clock);
        var result = await service.AddAsync(workspaceId, ResourceType.File, @"C:\Docs\readme.txt", null);

        Assert.False(result.Success);
        Assert.Equal("workspace.not_found", result.Error?.Code);
    }

    [Fact]
    public async Task AddAsync_Adds_Resource_And_Allows_Duplicates()
    {
        var workspaceId = Guid.NewGuid();
        var ws = new Workspace(workspaceId, "Dev", _fixedTime, _fixedTime, 0);
        _workspaceRepository.GetAsync(workspaceId, Arg.Any<CancellationToken>()).Returns(ws);
        _resourceRepository.GetNextSortOrderAsync(workspaceId, Arg.Any<CancellationToken>()).Returns(5);

        var service = new ResourceService(_resourceRepository, _workspaceRepository, _clock);

        // First add
        var res1 = await service.AddAsync(workspaceId, ResourceType.File, @"C:\Docs\readme.txt", "Readme File");
        Assert.True(res1.Success);
        Assert.Equal("Readme File", res1.Value!.Name);
        Assert.Equal(5, res1.Value.SortOrder);

        // Duplicate target allowed (FR-RS-09)
        var res2 = await service.AddAsync(workspaceId, ResourceType.File, @"C:\Docs\readme.txt", "Readme Copy");
        Assert.True(res2.Success);
        Assert.NotEqual(res1.Value.Id, res2.Value!.Id);
        Assert.Equal("Readme Copy", res2.Value.Name);

        await _resourceRepository.Received(2).AddAsync(Arg.Any<Resource>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddManyAsync_Atomically_Adds_Multiple_Files()
    {
        var workspaceId = Guid.NewGuid();
        var ws = new Workspace(workspaceId, "College", _fixedTime, _fixedTime, 0);
        _workspaceRepository.GetAsync(workspaceId, Arg.Any<CancellationToken>()).Returns(ws);
        _resourceRepository.GetNextSortOrderAsync(workspaceId, Arg.Any<CancellationToken>()).Returns(10);

        var targets = new[] { @"C:\Papers\p1.pdf", @"C:\Papers\p2.pdf", @"C:\Papers\p3.pdf" };

        var service = new ResourceService(_resourceRepository, _workspaceRepository, _clock);
        var result = await service.AddManyAsync(workspaceId, ResourceType.File, targets);

        Assert.True(result.Success);
        Assert.Equal(3, result.Value!.Count);
        Assert.Equal(10, result.Value[0].SortOrder);
        Assert.Equal(11, result.Value[1].SortOrder);
        Assert.Equal(12, result.Value[2].SortOrder);

        await _resourceRepository.Received(1).AddRangeAsync(Arg.Is<IEnumerable<Resource>>(r => r.AsList().Count == 3), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_Updates_Resource_Metadata()
    {
        var id = Guid.NewGuid();
        var wsId = Guid.NewGuid();
        var existing = new Resource(id, wsId, "Old Name", ResourceType.Link, "https://old.com", 0, _fixedTime, _fixedTime);
        _resourceRepository.GetAsync(id, Arg.Any<CancellationToken>()).Returns(existing);

        var service = new ResourceService(_resourceRepository, _workspaceRepository, _clock);
        var result = await service.UpdateAsync(id, "New Name", "https://new.com");

        Assert.True(result.Success);
        await _resourceRepository.Received(1).UpdateAsync(id, "New Name", "https://new.com/", _fixedTime, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RemoveAsync_Deletes_From_Repository_Without_Touching_Disk()
    {
        var id = Guid.NewGuid();
        _resourceRepository.DeleteAsync(id, Arg.Any<CancellationToken>()).Returns(true);

        var service = new ResourceService(_resourceRepository, _workspaceRepository, _clock);
        var result = await service.RemoveAsync(id);

        Assert.True(result.Success);
        await _resourceRepository.Received(1).DeleteAsync(id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MoveAsync_Reorders_Items_Correctly()
    {
        var wsId = Guid.NewGuid();
        var r1 = new Resource(Guid.NewGuid(), wsId, "R1", ResourceType.File, @"C:\r1.txt", 0, _fixedTime, _fixedTime);
        var r2 = new Resource(Guid.NewGuid(), wsId, "R2", ResourceType.File, @"C:\r2.txt", 1, _fixedTime, _fixedTime);
        var r3 = new Resource(Guid.NewGuid(), wsId, "R3", ResourceType.File, @"C:\r3.txt", 2, _fixedTime, _fixedTime);

        _resourceRepository.GetAsync(r2.Id, Arg.Any<CancellationToken>()).Returns(r2);
        _resourceRepository.GetByWorkspaceAsync(wsId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<Resource>>(new[] { r1, r2, r3 }));

        var service = new ResourceService(_resourceRepository, _workspaceRepository, _clock);

        // Move r2 up (-1)
        var result = await service.MoveAsync(r2.Id, -1);
        Assert.True(result.Success);

        // Expected new order: r2, r1, r3
        await _resourceRepository.Received(1).ReorderAsync(
            wsId,
            Arg.Is<IReadOnlyList<Guid>>(ids => ids[0] == r2.Id && ids[1] == r1.Id && ids[2] == r3.Id),
            Arg.Any<CancellationToken>());
    }

    #endregion
}

internal static class EnumerableExtensions
{
    public static List<T> AsList<T>(this IEnumerable<T> source) => source as List<T> ?? new List<T>(source);
}
