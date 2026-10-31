using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using AdaraBox.App.Resources;
using AdaraBox.App.Services;
using AdaraBox.App.ViewModels;
using AdaraBox.Core.Common;
using AdaraBox.Core.Resources;
using AdaraBox.Core.Settings;
using AdaraBox.Core.Visibility;
using AdaraBox.Core.Workspaces;
using AdaraBox.Platform.Availability;
using AdaraBox.Platform.Icons;
using NSubstitute;
using Xunit;

namespace AdaraBox.App.Tests;

public class WorkspacePanelViewModelTests
{
    private readonly IWorkspaceService _workspaceService = Substitute.For<IWorkspaceService>();
    private readonly IResourceService _resourceService = Substitute.For<IResourceService>();
    private readonly IResourceAvailabilityChecker _availabilityChecker = Substitute.For<IResourceAvailabilityChecker>();
    private readonly IIconProvider _iconProvider = Substitute.For<IIconProvider>();
    private readonly IFilePickerService _filePickerService = Substitute.For<IFilePickerService>();
    private readonly IDialogService _dialogService = Substitute.For<IDialogService>();
    private readonly ISettingsStore _settingsStore = Substitute.For<ISettingsStore>();
    private readonly DateTimeOffset _fixedTime = new(2026, 10, 31, 10, 0, 0, TimeSpan.Zero);

    public WorkspacePanelViewModelTests()
    {
        // Return dummy frozen 1x1 bitmap for icon provider
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
            _settingsStore);
    }

    [Fact]
    public async Task InitialLoad_WithWorkspaces_PopulatesList()
    {
        var ws1 = new Workspace(Guid.NewGuid(), "Carry On", _fixedTime, _fixedTime, 0);
        var ws2 = new Workspace(Guid.NewGuid(), "ARUS", _fixedTime, _fixedTime, 1);
        _workspaceService.ListAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<Workspace>>(new[] { ws1, ws2 }));

        var vm = CreateVm();
        await vm.LoadWorkspacesAsync();

        Assert.Equal(2, vm.Workspaces.Count);
        Assert.True(vm.HasWorkspaces);
        Assert.False(vm.IsEmpty);
        Assert.Equal("Carry On", vm.Workspaces[0].Name);
        Assert.Equal("ARUS", vm.Workspaces[1].Name);
    }

    [Fact]
    public async Task InitialLoad_Empty_SetsEmptyState()
    {
        _workspaceService.ListAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<Workspace>>(Array.Empty<Workspace>()));

        var vm = CreateVm();
        await vm.LoadWorkspacesAsync();

        Assert.Empty(vm.Workspaces);
        Assert.False(vm.HasWorkspaces);
        Assert.True(vm.IsEmpty);
    }

    [Fact]
    public void BeginCreate_OpensForm_WithEmptyFields()
    {
        var vm = CreateVm();

        vm.BeginCreate();

        Assert.True(vm.IsFormOpen);
        Assert.Null(vm.EditingWorkspaceId);
        Assert.Equal(UiText.CreateWorkspaceTitle, vm.FormTitle);
        Assert.Empty(vm.FormName);
        Assert.Null(vm.FormError);
        Assert.False(vm.HasFormError);
    }

    [Fact]
    public async Task SaveForm_Create_FailsValidation_ShowsIndonesianErrorMessage()
    {
        _workspaceService.CreateAsync("", Arg.Any<CancellationToken>())
            .Returns(OperationResult<Workspace>.Fail("workspace.name.empty", "Name cannot be empty"));

        var vm = CreateVm();
        vm.BeginCreate();
        vm.FormName = "";

        await vm.SaveFormAsync();

        Assert.True(vm.IsFormOpen);
        Assert.True(vm.HasFormError);
        Assert.Equal(UiText.ErrorWorkspaceNameEmpty, vm.FormError);
    }

    [Fact]
    public async Task SaveForm_Create_Success_AddsItem_AndOpensDetail()
    {
        var created = new Workspace(Guid.NewGuid(), "College", _fixedTime, _fixedTime, 0);
        _workspaceService.CreateAsync("College", Arg.Any<CancellationToken>())
            .Returns(OperationResult<Workspace>.Ok(created));

        var vm = CreateVm();
        vm.BeginCreate();
        vm.FormName = "College";

        await vm.SaveFormAsync();

        Assert.False(vm.IsFormOpen);
        Assert.Single(vm.Workspaces);
        Assert.Equal("College", vm.Workspaces[0].Name);
        Assert.True(vm.IsDetailViewActive);
        Assert.Equal("College", vm.DetailTitle);
    }

    [Fact]
    public async Task SaveForm_Rename_UpdatesItemName()
    {
        var id = Guid.NewGuid();
        var ws = new Workspace(id, "Old Name", _fixedTime, _fixedTime, 0);
        _workspaceService.ListAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<Workspace>>(new[] { ws }));

        _workspaceService.RenameAsync(id, "New Name", Arg.Any<CancellationToken>())
            .Returns(OperationResult.Ok());

        var vm = CreateVm();
        await vm.LoadWorkspacesAsync();

        var item = vm.Workspaces[0];
        vm.BeginRename(item);
        vm.FormName = "New Name";

        await vm.SaveFormAsync();

        Assert.False(vm.IsFormOpen);
        Assert.Equal("New Name", item.Name);
    }

    [Fact]
    public async Task DeleteWorkspace_Cancelled_DoesNotDelete()
    {
        var id = Guid.NewGuid();
        var ws = new Workspace(id, "Reading", _fixedTime, _fixedTime, 0);
        _workspaceService.ListAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<Workspace>>(new[] { ws }));
        _workspaceService.GetResourceCountAsync(id, Arg.Any<CancellationToken>()).Returns(0);

        _dialogService.ConfirmDeleteWorkspaceAsync("Reading", 0).Returns(false);

        var vm = CreateVm();
        await vm.LoadWorkspacesAsync();

        var item = vm.Workspaces[0];
        await vm.DeleteWorkspaceAsync(item);

        Assert.Single(vm.Workspaces);
        await _workspaceService.DidNotReceive().DeleteAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteWorkspace_Confirmed_DeletesFromDatabaseAndList()
    {
        var id = Guid.NewGuid();
        var ws = new Workspace(id, "Reading", _fixedTime, _fixedTime, 0);
        _workspaceService.ListAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<Workspace>>(new[] { ws }));
        _workspaceService.GetResourceCountAsync(id, Arg.Any<CancellationToken>()).Returns(2);

        _dialogService.ConfirmDeleteWorkspaceAsync("Reading", 2).Returns(true);
        _workspaceService.DeleteAsync(id, Arg.Any<CancellationToken>()).Returns(OperationResult.Ok());

        var vm = CreateVm();
        await vm.LoadWorkspacesAsync();

        var item = vm.Workspaces[0];
        vm.OpenWorkspace(item);
        Assert.True(vm.IsDetailViewActive);

        await vm.DeleteWorkspaceAsync(item);

        Assert.Empty(vm.Workspaces);
        Assert.False(vm.IsDetailViewActive);
        Assert.True(vm.IsEmpty);
        await _workspaceService.Received(1).DeleteAsync(id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ToggleViewMode_Toggles_And_Persists_To_SettingsStore()
    {
        var vm = CreateVm();
        Assert.True(vm.IsGridView);

        await vm.ToggleViewModeAsync("List");

        Assert.True(vm.IsListView);
        Assert.False(vm.IsGridView);
        await _settingsStore.Received(1).SetAsync(SettingKeys.ViewMode, "List", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddFilesAsync_PicksFiles_And_AddsResources()
    {
        var wsId = Guid.NewGuid();
        var ws = new Workspace(wsId, "Projects", _fixedTime, _fixedTime, 0);
        var vm = CreateVm();
        var wsItem = new WorkspaceItemViewModel(ws, vm);
        vm.OpenWorkspace(wsItem);

        var files = new[] { @"C:\Docs\a.pdf", @"C:\Docs\b.pdf" };
        _filePickerService.PickFilesAsync().Returns(files);

        var r1 = new Resource(Guid.NewGuid(), wsId, "a", ResourceType.File, files[0], 0, _fixedTime, _fixedTime);
        var r2 = new Resource(Guid.NewGuid(), wsId, "b", ResourceType.File, files[1], 1, _fixedTime, _fixedTime);
        _resourceService.AddManyAsync(wsId, ResourceType.File, files, Arg.Any<CancellationToken>())
            .Returns(OperationResult<IReadOnlyList<Resource>>.Ok(new[] { r1, r2 }));

        await vm.AddFilesAsync();

        Assert.Equal(2, vm.Resources.Count);
        Assert.Equal("a", vm.Resources[0].Name);
        Assert.Equal("b", vm.Resources[1].Name);
        Assert.Equal(2, wsItem.ResourceCount);
    }

    [Fact]
    public async Task DeleteResource_Confirmed_Removes_From_Collection()
    {
        var wsId = Guid.NewGuid();
        var ws = new Workspace(wsId, "Projects", _fixedTime, _fixedTime, 0);
        var vm = CreateVm();
        var wsItem = new WorkspaceItemViewModel(ws, vm);
        vm.OpenWorkspace(wsItem);

        var r1 = new Resource(Guid.NewGuid(), wsId, "Delete Me", ResourceType.Link, "https://delete.com", 0, _fixedTime, _fixedTime);
        var itemVm = new ResourceItemViewModel(r1, vm, _iconProvider);
        vm.Resources.Add(itemVm);
        wsItem.ResourceCount = 1;

        _dialogService.ConfirmDeleteResourceAsync("Delete Me").Returns(true);
        _resourceService.RemoveAsync(r1.Id, Arg.Any<CancellationToken>()).Returns(OperationResult.Ok());

        await vm.DeleteResourceAsync(itemVm);

        Assert.Empty(vm.Resources);
        Assert.Equal(0, wsItem.ResourceCount);
        await _resourceService.Received(1).RemoveAsync(r1.Id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TopBarVisibilityService_PersistsModeToSettingsStore()
    {
        var settingsStore = Substitute.For<ISettingsStore>();
        var visibilityService = new TopBarVisibilityService(settingsStore, TopBarVisibilityMode.AlwaysVisible);

        visibilityService.SetMode(TopBarVisibilityMode.Hidden);

        await settingsStore.Received(1).SetAsync(SettingKeys.TopBarVisibility, "Hidden", Arg.Any<CancellationToken>());
    }
}
