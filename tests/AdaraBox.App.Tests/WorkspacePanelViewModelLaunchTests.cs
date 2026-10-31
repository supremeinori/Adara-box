using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using AdaraBox.App.Services;
using AdaraBox.App.ViewModels;
using AdaraBox.Core.Launcher;
using AdaraBox.Core.Resources;
using AdaraBox.Core.Settings;
using AdaraBox.Core.Workspaces;
using AdaraBox.Platform.Availability;
using AdaraBox.Platform.Icons;
using NSubstitute;
using Xunit;

namespace AdaraBox.App.Tests;

public class WorkspacePanelViewModelLaunchTests
{
    private readonly IWorkspaceService _workspaceService = Substitute.For<IWorkspaceService>();
    private readonly IResourceService _resourceService = Substitute.For<IResourceService>();
    private readonly IResourceAvailabilityChecker _availabilityChecker = Substitute.For<IResourceAvailabilityChecker>();
    private readonly IIconProvider _iconProvider = Substitute.For<IIconProvider>();
    private readonly IFilePickerService _filePickerService = Substitute.For<IFilePickerService>();
    private readonly IDialogService _dialogService = Substitute.For<IDialogService>();
    private readonly ISettingsStore _settingsStore = Substitute.For<ISettingsStore>();
    private readonly IMultiLaunchService _multiLaunchService = Substitute.For<IMultiLaunchService>();
    private readonly DateTimeOffset _fixedTime = new(2026, 10, 31, 10, 0, 0, TimeSpan.Zero);

    public WorkspacePanelViewModelLaunchTests()
    {
        var bmp = BitmapSource.Create(1, 1, 96, 96, PixelFormats.Bgra32, null, new byte[4], 4);
        bmp.Freeze();
        _iconProvider.GetIcon(Arg.Any<ResourceType>(), Arg.Any<string>()).Returns(bmp);
    }

    private WorkspacePanelViewModel CreateVm()
    {
        return new WorkspacePanelViewModel(
            _workspaceService,
            _resourceService,
            _availabilityChecker,
            _iconProvider,
            _filePickerService,
            _dialogService,
            _settingsStore,
            _multiLaunchService);
    }

    private static Resource CreateResource(Guid workspaceId, string name, ResourceType type = ResourceType.File)
    {
        return new Resource(
            Guid.NewGuid(),
            workspaceId,
            name,
            type,
            $"C:\\test\\{name}",
            0,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task OpenMode_Toggle_SwitchesStateAndClearsSelectionOnExit()
    {
        // Arrange
        var ws = new Workspace(Guid.NewGuid(), "Test WS", _fixedTime, _fixedTime, 0);
        var r1 = CreateResource(ws.Id, "Item 1");
        _resourceService.ListAsync(ws.Id).Returns(Task.FromResult<IReadOnlyList<Resource>>(new[] { r1 }));

        var vm = CreateVm();
        vm.OpenWorkspace(new WorkspaceItemViewModel(ws, vm));
        await vm.LoadResourcesAsync(ws.Id);

        // Act 1: Toggle Open Mode ON
        vm.ToggleOpenModeCommand.Execute(null);
        Assert.True(vm.IsOpenMode);
        Assert.False(vm.CanManageResources);

        // Select item
        vm.Resources[0].IsSelected = true;
        Assert.Equal(1, vm.SelectedCount);
        Assert.True(vm.CanOpen);

        // Act 2: Toggle Open Mode OFF (FR-OF-08)
        vm.ToggleOpenModeCommand.Execute(null);
        Assert.False(vm.IsOpenMode);
        Assert.True(vm.CanManageResources);
        Assert.Equal(0, vm.SelectedCount);
        Assert.False(vm.Resources[0].IsSelected);
        Assert.False(vm.CanOpen);
    }

    [Fact]
    public async Task OpenMode_CanOpen_IsFalseWhenZeroSelected_IsTrueWhenSelected()
    {
        // Arrange (FR-OF-09)
        var ws = new Workspace(Guid.NewGuid(), "Test WS", _fixedTime, _fixedTime, 0);
        var r1 = CreateResource(ws.Id, "Doc1.txt");
        var r2 = CreateResource(ws.Id, "Doc2.txt");
        _resourceService.ListAsync(ws.Id).Returns(Task.FromResult<IReadOnlyList<Resource>>(new[] { r1, r2 }));

        var vm = CreateVm();
        vm.OpenWorkspace(new WorkspaceItemViewModel(ws, vm));
        await vm.LoadResourcesAsync(ws.Id);

        vm.ToggleOpenModeCommand.Execute(null);

        // 0 selected
        Assert.Equal(0, vm.SelectedCount);
        Assert.False(vm.CanOpen);

        // Select 1 item
        vm.Resources[0].IsSelected = true;
        Assert.Equal(1, vm.SelectedCount);
        Assert.True(vm.CanOpen);

        // Unselect
        vm.Resources[0].IsSelected = false;
        Assert.Equal(0, vm.SelectedCount);
        Assert.False(vm.CanOpen);
    }

    [Fact]
    public async Task OpenMode_SelectAll_And_ClearSelection_WorkCorrectly()
    {
        // Arrange
        var ws = new Workspace(Guid.NewGuid(), "Test WS", _fixedTime, _fixedTime, 0);
        var resources = Enumerable.Range(1, 5)
            .Select(i => CreateResource(ws.Id, $"File{i}.txt"))
            .ToList();
        _resourceService.ListAsync(ws.Id).Returns(Task.FromResult<IReadOnlyList<Resource>>(resources));

        var vm = CreateVm();
        vm.OpenWorkspace(new WorkspaceItemViewModel(ws, vm));
        await vm.LoadResourcesAsync(ws.Id);

        vm.ToggleOpenModeCommand.Execute(null);

        // Select All
        vm.SelectAllCommand.Execute(null);
        Assert.Equal(5, vm.SelectedCount);
        Assert.All(vm.Resources, r => Assert.True(r.IsSelected));

        // Clear Selection
        vm.ClearSelectionCommand.Execute(null);
        Assert.Equal(0, vm.SelectedCount);
        Assert.All(vm.Resources, r => Assert.False(r.IsSelected));
    }

    [Fact]
    public async Task OpenMode_ManageActions_AreBlocked( )
    {
        // Arrange (FR-OF-05)
        var ws = new Workspace(Guid.NewGuid(), "Test WS", _fixedTime, _fixedTime, 0);
        var r1 = CreateResource(ws.Id, "File.txt");
        _resourceService.ListAsync(ws.Id).Returns(Task.FromResult<IReadOnlyList<Resource>>(new[] { r1 }));

        var vm = CreateVm();
        vm.OpenWorkspace(new WorkspaceItemViewModel(ws, vm));
        await vm.LoadResourcesAsync(ws.Id);

        vm.ToggleOpenModeCommand.Execute(null);
        Assert.True(vm.IsOpenMode);
        Assert.False(vm.CanManageResources);

        // Verify resource item manage commands are disabled
        var item = vm.Resources[0];
        Assert.False(item.CanManage);
        Assert.False(item.EditCommand.CanExecute(null));
        Assert.False(item.DeleteCommand.CanExecute(null));
        Assert.False(item.MoveUpCommand.CanExecute(null));
        Assert.False(item.MoveDownCommand.CanExecute(null));

        // Calling add actions while in Open mode does nothing
        await vm.AddFilesAsync();
        await _filePickerService.DidNotReceive().PickFilesAsync();

        await vm.AddFolderAsync();
        await _filePickerService.DidNotReceive().PickFolderAsync();

        await vm.AddApplicationAsync();
        await _filePickerService.DidNotReceive().PickApplicationAsync();

        vm.BeginAddLink();
        Assert.False(vm.IsResourceFormOpen);
    }

    [Fact]
    public async Task OpenMode_BackToList_ClearsSelectionAndExitsOpenMode()
    {
        // Arrange (FR-OF-08)
        var ws = new Workspace(Guid.NewGuid(), "Test WS", _fixedTime, _fixedTime, 0);
        var r1 = CreateResource(ws.Id, "File.txt");
        _resourceService.ListAsync(ws.Id).Returns(Task.FromResult<IReadOnlyList<Resource>>(new[] { r1 }));

        var vm = CreateVm();
        vm.OpenWorkspace(new WorkspaceItemViewModel(ws, vm));
        await vm.LoadResourcesAsync(ws.Id);

        vm.ToggleOpenModeCommand.Execute(null);
        vm.Resources[0].IsSelected = true;
        Assert.True(vm.IsOpenMode);
        Assert.Equal(1, vm.SelectedCount);

        // Act: Back to list
        vm.BackToListCommand.Execute(null);

        // Assert
        Assert.False(vm.IsOpenMode);
        Assert.Equal(0, vm.SelectedCount);
        Assert.Null(vm.SelectedWorkspace);
    }

    [Fact]
    public async Task OpenSelected_LaunchesOnlySelectedResources_AndShowsSummary()
    {
        // Arrange
        var ws = new Workspace(Guid.NewGuid(), "Test WS", _fixedTime, _fixedTime, 0);
        var r1 = CreateResource(ws.Id, "Selected1.txt");
        var r2 = CreateResource(ws.Id, "Unselected.txt");
        var r3 = CreateResource(ws.Id, "Selected2.txt");
        _resourceService.ListAsync(ws.Id).Returns(Task.FromResult<IReadOnlyList<Resource>>(new[] { r1, r2, r3 }));

        _multiLaunchService.LaunchAllAsync(
            Arg.Any<IReadOnlyList<Resource>>(),
            Arg.Any<IProgress<LaunchResult>>(),
            Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var input = callInfo.Arg<IReadOnlyList<Resource>>();
                var results = input.Select(r => new LaunchResult(r.Id, r.Name, true)).ToList();
                return Task.FromResult(new LaunchSummary(results));
            });

        var vm = CreateVm();
        vm.OpenWorkspace(new WorkspaceItemViewModel(ws, vm));
        await vm.LoadResourcesAsync(ws.Id);

        vm.ToggleOpenModeCommand.Execute(null);
        vm.Resources[0].IsSelected = true; // Selected1
        vm.Resources[2].IsSelected = true; // Selected2

        // Act
        await vm.OpenSelectedAsync();

        // Assert
        await _multiLaunchService.Received(1).LaunchAllAsync(
            Arg.Is<IReadOnlyList<Resource>>(list => list.Count == 2 && list[0].Id == r1.Id && list[1].Id == r3.Id),
            Arg.Any<IProgress<LaunchResult>>(),
            Arg.Any<CancellationToken>());

        Assert.True(vm.IsLaunchSummaryOpen);
        Assert.Equal(2, vm.SuccessfulResults.Count);
        Assert.Empty(vm.FailedResults);
    }

    [Fact]
    public async Task OpenSelected_WhenSomeFail_PopulatesFailedResults_AndCanRetry()
    {
        // Arrange (FR-OF-06)
        var ws = new Workspace(Guid.NewGuid(), "Test WS", _fixedTime, _fixedTime, 0);
        var r1 = CreateResource(ws.Id, "Good.txt");
        var r2 = CreateResource(ws.Id, "Missing.txt");
        _resourceService.ListAsync(ws.Id).Returns(Task.FromResult<IReadOnlyList<Resource>>(new[] { r1, r2 }));

        var firstResults = new List<LaunchResult>
        {
            new(r1.Id, r1.Name, true),
            new(r2.Id, r2.Name, false, LaunchFailureReason.NotFound, "File tidak ada")
        };

        var retryResults = new List<LaunchResult>
        {
            new(r2.Id, r2.Name, true)
        };

        int callCount = 0;
        _multiLaunchService.LaunchAllAsync(
            Arg.Any<IReadOnlyList<Resource>>(),
            Arg.Any<IProgress<LaunchResult>>(),
            Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                callCount++;
                return Task.FromResult(new LaunchSummary(callCount == 1 ? firstResults : retryResults));
            });

        var vm = CreateVm();
        vm.OpenWorkspace(new WorkspaceItemViewModel(ws, vm));
        await vm.LoadResourcesAsync(ws.Id);

        vm.ToggleOpenModeCommand.Execute(null);
        vm.SelectAllCommand.Execute(null);

        // Act 1: Initial launch
        await vm.OpenSelectedAsync();

        Assert.True(vm.IsLaunchSummaryOpen);
        Assert.Single(vm.SuccessfulResults);
        Assert.Single(vm.FailedResults);
        Assert.Equal(r2.Id, vm.FailedResults[0].ResourceId);

        // Act 2: Retry failed
        await vm.RetryFailedAsync();

        // Should have called LaunchAllAsync with only r2
        Assert.Equal(2, callCount);
        Assert.True(vm.IsLaunchSummaryOpen);
        Assert.Single(vm.SuccessfulResults);
        Assert.Empty(vm.FailedResults);
    }

    [Fact]
    public async Task HasManySelected_ReturnsTrue_WhenMoreThan10Selected()
    {
        // Arrange (FR-OF-10)
        var ws = new Workspace(Guid.NewGuid(), "Test WS", _fixedTime, _fixedTime, 0);
        var resources = Enumerable.Range(1, 12)
            .Select(i => CreateResource(ws.Id, $"Item{i}"))
            .ToList();
        _resourceService.ListAsync(ws.Id).Returns(Task.FromResult<IReadOnlyList<Resource>>(resources));

        var vm = CreateVm();
        vm.OpenWorkspace(new WorkspaceItemViewModel(ws, vm));
        await vm.LoadResourcesAsync(ws.Id);

        vm.ToggleOpenModeCommand.Execute(null);

        // Select 10
        for (int i = 0; i < 10; i++) vm.Resources[i].IsSelected = true;
        Assert.False(vm.HasManySelected);

        // Select 11th
        vm.Resources[10].IsSelected = true;
        Assert.True(vm.HasManySelected);
        Assert.Equal(11, vm.SelectedCount);
    }
}
