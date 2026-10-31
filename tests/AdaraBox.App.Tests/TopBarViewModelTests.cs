using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using AdaraBox.App.Services;
using AdaraBox.App.ViewModels;
using AdaraBox.Core.Launcher;
using AdaraBox.Core.Media;
using AdaraBox.Core.Modules;
using AdaraBox.Core.Resources;
using AdaraBox.Core.Settings;
using AdaraBox.Core.Stats;
using AdaraBox.Core.Visibility;
using AdaraBox.Core.Workspaces;
using AdaraBox.Platform.Availability;
using AdaraBox.Platform.Hotkeys;
using AdaraBox.Platform.Icons;
using AdaraBox.Platform.Startup;
using NSubstitute;
using Xunit;

namespace AdaraBox.App.Tests;

public class TopBarViewModelTests
{
    private readonly ITopBarVisibilityService _visibilityService = Substitute.For<ITopBarVisibilityService>();
    private readonly IWorkspaceService _workspaceService = Substitute.For<IWorkspaceService>();
    private readonly IResourceService _resourceService = Substitute.For<IResourceService>();
    private readonly IResourceAvailabilityChecker _availabilityChecker = Substitute.For<IResourceAvailabilityChecker>();
    private readonly IIconProvider _iconProvider = Substitute.For<IIconProvider>();
    private readonly IFilePickerService _filePickerService = Substitute.For<IFilePickerService>();
    private readonly IDialogService _dialogService = Substitute.For<IDialogService>();
    private readonly ISettingsStore _settingsStore = Substitute.For<ISettingsStore>();
    private readonly IMultiLaunchService _multiLaunchService = Substitute.For<IMultiLaunchService>();
    private readonly IMediaLinkService _mediaLinkService = Substitute.For<IMediaLinkService>();
    private readonly IResourceLauncher _launcher = Substitute.For<IResourceLauncher>();
    private readonly ISystemStatsProvider _statsProvider = Substitute.For<ISystemStatsProvider>();
    private readonly IAutoStartService _autoStartService = Substitute.For<IAutoStartService>();
    private readonly IGlobalHotkeyService _hotkeyService = Substitute.For<IGlobalHotkeyService>();

    private WorkspacePanelViewModel CreateWorkspaceVm() =>
        new(_workspaceService, _resourceService, _availabilityChecker, _iconProvider, _filePickerService, _dialogService, _settingsStore, _multiLaunchService);

    private MediaPanelViewModel CreateMediaVm() =>
        new(_mediaLinkService, _launcher, _dialogService);

    private StatsPanelViewModel CreateStatsVm() =>
        new(_statsProvider);

    private SettingsPanelViewModel CreateSettingsVm() =>
        new(_visibilityService, _autoStartService, _hotkeyService, _settingsStore);

    [Fact]
    public void TopBar_WithoutMediaController_InitializesEmpty()
    {
        var vm = new TopBarViewModel(
            Array.Empty<IAppModule>(),
            Array.Empty<ITopBarAction>(),
            _visibilityService,
            CreateWorkspaceVm(),
            CreateMediaVm(),
            CreateStatsVm(),
            CreateSettingsVm());

        Assert.Empty(vm.NowPlayingText);
        Assert.False(vm.HasActiveMedia);
        Assert.False(vm.IsPlaying);
        Assert.Equal("▶", vm.PlayPauseIcon);
    }

    [Fact]
    public void TopBar_WithActiveMedia_InitializesNowPlaying()
    {
        var controller = Substitute.For<IMediaPlaybackController>();
        controller.CurrentTrack.Returns(new MediaTrackInfo("Song A", "Artist B", "Album C", "Spotify"));
        controller.CurrentState.Returns(MediaPlaybackState.Playing);

        var vm = new TopBarViewModel(
            Array.Empty<IAppModule>(),
            Array.Empty<ITopBarAction>(),
            _visibilityService,
            CreateWorkspaceVm(),
            CreateMediaVm(),
            CreateStatsVm(),
            CreateSettingsVm(),
            controller);

        Assert.Equal("Song A — Artist B", vm.NowPlayingText);
        Assert.True(vm.HasActiveMedia);
        Assert.True(vm.IsPlaying);
        Assert.Equal("⏸", vm.PlayPauseIcon);
    }

    [Fact]
    public void TopBar_TrackChangedEvent_UpdatesNowPlaying()
    {
        var controller = Substitute.For<IMediaPlaybackController>();
        var vm = new TopBarViewModel(
            Array.Empty<IAppModule>(),
            Array.Empty<ITopBarAction>(),
            _visibilityService,
            CreateWorkspaceVm(),
            CreateMediaVm(),
            CreateStatsVm(),
            CreateSettingsVm(),
            controller);

        controller.TrackChanged += Raise.Event<EventHandler<MediaTrackInfo?>>(controller, new MediaTrackInfo("Song X", "Artist Y", "Album Z", "Chrome"));

        Assert.Equal("Song X — Artist Y", vm.NowPlayingText);
        Assert.True(vm.HasActiveMedia);

        controller.TrackChanged += Raise.Event<EventHandler<MediaTrackInfo?>>(controller, (MediaTrackInfo?)null);

        Assert.Empty(vm.NowPlayingText);
        Assert.False(vm.HasActiveMedia);
    }

    [Fact]
    public void TopBar_PlaybackStateChangedEvent_UpdatesPlayPauseIcon()
    {
        var controller = Substitute.For<IMediaPlaybackController>();
        var vm = new TopBarViewModel(
            Array.Empty<IAppModule>(),
            Array.Empty<ITopBarAction>(),
            _visibilityService,
            CreateWorkspaceVm(),
            CreateMediaVm(),
            CreateStatsVm(),
            CreateSettingsVm(),
            controller);

        controller.PlaybackStateChanged += Raise.Event<EventHandler<MediaPlaybackState>>(controller, MediaPlaybackState.Playing);
        Assert.True(vm.IsPlaying);
        Assert.Equal("⏸", vm.PlayPauseIcon);

        controller.PlaybackStateChanged += Raise.Event<EventHandler<MediaPlaybackState>>(controller, MediaPlaybackState.Paused);
        Assert.False(vm.IsPlaying);
        Assert.Equal("▶", vm.PlayPauseIcon);
    }

    [Fact]
    public async Task TopBar_PlaybackCommands_InvokeController()
    {
        var controller = Substitute.For<IMediaPlaybackController>();
        var vm = new TopBarViewModel(
            Array.Empty<IAppModule>(),
            Array.Empty<ITopBarAction>(),
            _visibilityService,
            CreateWorkspaceVm(),
            CreateMediaVm(),
            CreateStatsVm(),
            CreateSettingsVm(),
            controller);

        await vm.TogglePlayPauseCommand.ExecuteAsync(null);
        await controller.Received(1).PlayPauseAsync();

        await vm.NextTrackCommand.ExecuteAsync(null);
        await controller.Received(1).NextAsync();

        await vm.PreviousTrackCommand.ExecuteAsync(null);
        await controller.Received(1).PreviousAsync();
    }
}
