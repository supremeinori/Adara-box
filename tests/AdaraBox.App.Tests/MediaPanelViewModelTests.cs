using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AdaraBox.App.Services;
using AdaraBox.App.ViewModels;
using AdaraBox.Core.Common;
using AdaraBox.Core.Launcher;
using AdaraBox.Core.Media;
using AdaraBox.Core.Resources;
using NSubstitute;
using Xunit;

namespace AdaraBox.App.Tests;

public class MediaPanelViewModelTests
{
    private readonly IMediaLinkService _mediaService = Substitute.For<IMediaLinkService>();
    private readonly IResourceLauncher _launcher = Substitute.For<IResourceLauncher>();
    private readonly IDialogService _dialogService = Substitute.For<IDialogService>();
    private readonly DateTimeOffset _fixedTime = new(2026, 10, 31, 12, 0, 0, TimeSpan.Zero);

    private MediaPanelViewModel CreateVm() => new(_mediaService, _launcher, _dialogService);

    [Fact]
    public async Task InitialLoad_WithLinks_PopulatesList()
    {
        var link = new MediaLink(Guid.NewGuid(), "Lofi Girl", "https://youtube.com/live", 0, _fixedTime, _fixedTime);
        _mediaService.ListAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult<IReadOnlyList<MediaLink>>(new[] { link }));

        var vm = CreateVm();
        await vm.LoadLinksAsync();

        Assert.Single(vm.Links);
        Assert.Equal("Lofi Girl", vm.Links[0].Name);
        Assert.True(vm.HasLinks);
        Assert.False(vm.IsEmpty);
    }

    [Fact]
    public void BeginAdd_OpensForm_WithEmptyFields()
    {
        var vm = CreateVm();
        vm.BeginAdd();

        Assert.True(vm.IsFormOpen);
        Assert.Empty(vm.FormName);
        Assert.Empty(vm.FormUrl);
        Assert.Null(vm.FormError);
    }

    [Fact]
    public void CancelForm_ClosesForm()
    {
        var vm = CreateVm();
        vm.BeginAdd();
        vm.CancelForm();

        Assert.False(vm.IsFormOpen);
    }

    [Fact]
    public async Task SaveForm_Success_AddsItemAndClosesForm()
    {
        var created = new MediaLink(Guid.NewGuid(), "Spotify", "https://open.spotify.com", 0, _fixedTime, _fixedTime);
        _mediaService.CreateAsync("Spotify", "https://open.spotify.com", Arg.Any<CancellationToken>())
            .Returns(OperationResult<MediaLink>.Ok(created));

        var vm = CreateVm();
        vm.BeginAdd();
        vm.FormName = "Spotify";
        vm.FormUrl = "https://open.spotify.com";

        await vm.SaveFormAsync();

        Assert.False(vm.IsFormOpen);
        Assert.Single(vm.Links);
        Assert.Equal("Spotify", vm.Links[0].Name);
        Assert.Equal("open.spotify.com", vm.Links[0].Domain);
    }

    [Fact]
    public async Task SaveForm_FailsValidation_DisplaysError()
    {
        _mediaService.CreateAsync("", "", Arg.Any<CancellationToken>())
            .Returns(OperationResult<MediaLink>.Fail("media.name.empty", "Nama tidak boleh kosong"));

        var vm = CreateVm();
        vm.BeginAdd();

        await vm.SaveFormAsync();

        Assert.True(vm.IsFormOpen);
        Assert.True(vm.HasFormError);
        Assert.Equal("Nama tidak boleh kosong", vm.FormError);
    }

    [Fact]
    public async Task LaunchLink_CallsLauncher()
    {
        var linkId = Guid.NewGuid();
        _launcher.LaunchAsync(Arg.Any<Resource>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new LaunchResult(linkId, "Spotify", true)));

        var link = new MediaLink(linkId, "Spotify", "https://spotify.com", 0, _fixedTime, _fixedTime);
        var vm = CreateVm();
        var item = new MediaLinkItemViewModel(link, vm);

        await vm.LaunchLinkAsync(item);

        await _launcher.Received(1).LaunchAsync(Arg.Is<Resource>(r => r.Target == "https://spotify.com" && r.Type == ResourceType.Link), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteLink_Confirmed_DeletesLink()
    {
        var id = Guid.NewGuid();
        var link = new MediaLink(id, "Track", "https://track.com", 0, _fixedTime, _fixedTime);
        _mediaService.ListAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult<IReadOnlyList<MediaLink>>(new[] { link }));
        _dialogService.ConfirmDeleteResourceAsync("Track").Returns(true);
        _mediaService.DeleteAsync(id, Arg.Any<CancellationToken>()).Returns(OperationResult.Ok());

        var vm = CreateVm();
        await vm.LoadLinksAsync();
        var item = vm.Links[0];

        await vm.DeleteLinkAsync(item);

        Assert.Empty(vm.Links);
        await _mediaService.Received(1).DeleteAsync(id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public void MediaController_InitialTrackAndState_SetsProperties()
    {
        var controller = Substitute.For<IMediaPlaybackController>();
        controller.CurrentTrack.Returns(new MediaTrackInfo("Bohemian Rhapsody", "Queen", "A Night at the Opera", "Spotify"));
        controller.CurrentState.Returns(MediaPlaybackState.Playing);

        var vm = new MediaPanelViewModel(_mediaService, _launcher, _dialogService, controller);

        Assert.Equal("Bohemian Rhapsody", vm.TrackTitle);
        Assert.Equal("Queen", vm.TrackArtist);
        Assert.True(vm.HasActiveTrack);
        Assert.True(vm.IsPlaying);
        Assert.Equal("⏸ Pause", vm.PlayPauseButtonText);
    }

    [Fact]
    public void MediaController_TrackChangedEvent_UpdatesTrackProperties()
    {
        var controller = Substitute.For<IMediaPlaybackController>();
        var vm = new MediaPanelViewModel(_mediaService, _launcher, _dialogService, controller);

        controller.TrackChanged += Raise.Event<EventHandler<MediaTrackInfo?>>(controller, new MediaTrackInfo("Starboy", "The Weeknd", "Starboy", "Chrome"));

        Assert.Equal("Starboy", vm.TrackTitle);
        Assert.Equal("The Weeknd", vm.TrackArtist);
        Assert.True(vm.HasActiveTrack);

        controller.TrackChanged += Raise.Event<EventHandler<MediaTrackInfo?>>(controller, (MediaTrackInfo?)null);

        Assert.Equal("Tidak ada media aktif", vm.TrackTitle);
        Assert.False(vm.HasActiveTrack);
    }

    [Fact]
    public void MediaController_PlaybackStateChangedEvent_UpdatesPlayingState()
    {
        var controller = Substitute.For<IMediaPlaybackController>();
        var vm = new MediaPanelViewModel(_mediaService, _launcher, _dialogService, controller);

        controller.PlaybackStateChanged += Raise.Event<EventHandler<MediaPlaybackState>>(controller, MediaPlaybackState.Playing);
        Assert.True(vm.IsPlaying);
        Assert.Equal("⏸ Pause", vm.PlayPauseButtonText);

        controller.PlaybackStateChanged += Raise.Event<EventHandler<MediaPlaybackState>>(controller, MediaPlaybackState.Paused);
        Assert.False(vm.IsPlaying);
        Assert.Equal("▶ Play", vm.PlayPauseButtonText);
    }

    [Fact]
    public async Task MediaController_PlaybackCommands_InvokeController()
    {
        var controller = Substitute.For<IMediaPlaybackController>();
        var vm = new MediaPanelViewModel(_mediaService, _launcher, _dialogService, controller);

        await vm.PlayPauseCommand.ExecuteAsync(null);
        await controller.Received(1).PlayPauseAsync();

        await vm.NextTrackCommand.ExecuteAsync(null);
        await controller.Received(1).NextAsync();

        await vm.PreviousTrackCommand.ExecuteAsync(null);
        await controller.Received(1).PreviousAsync();
    }

    [Fact]
    public void MultiSession_UpdatesSessions_PopulatesPrimaryAndSecondary()
    {
        var controller = Substitute.For<IMediaPlaybackController>();
        var s1 = new MediaSessionInfo("s1", MediaPlatform.Spotify, "Song A", "Artist A", "Album A", "spotify.exe", MediaPlaybackState.Playing, true);
        var s2 = new MediaSessionInfo("s2", MediaPlatform.YouTube, "Video B", "Channel B", "", "chrome.exe", MediaPlaybackState.Paused, false);
        var s3 = new MediaSessionInfo("s3", MediaPlatform.YouTubeMusic, "Song C", "Artist C", "YT Music", "edge.exe", MediaPlaybackState.Paused, false);

        controller.PrimarySession.Returns(s1);
        controller.ActiveSessions.Returns(new[] { s1, s2, s3 });

        var vm = new MediaPanelViewModel(_mediaService, _launcher, _dialogService, controller);

        Assert.NotNull(vm.PrimarySession);
        Assert.Equal("s1", vm.PrimarySession.SessionId);
        Assert.Equal("Song A", vm.PrimarySession.Title);
        Assert.Equal("Artist A", vm.PrimarySession.Artist);
        Assert.Equal(MediaPlatform.Spotify, vm.PrimarySession.Platform);
        Assert.True(vm.PrimarySession.IsPlaying);
        Assert.True(vm.HasActiveSessions);

        Assert.Equal(2, vm.SecondarySessions.Count);
        Assert.Equal("s2", vm.SecondarySessions[0].SessionId);
        Assert.Equal("s3", vm.SecondarySessions[1].SessionId);
        Assert.True(vm.HasMultipleSessions);
        Assert.True(vm.IsCollapsed);
        Assert.Equal("Sesi Lainnya (2) ▼", vm.CollapseButtonText);
    }

    [Fact]
    public void MultiSession_ToggleCollapse_TogglesStateAndText()
    {
        var controller = Substitute.For<IMediaPlaybackController>();
        var s1 = new MediaSessionInfo("s1", MediaPlatform.Spotify, "Song A", "Artist A", "", "spotify", MediaPlaybackState.Playing, true);
        var s2 = new MediaSessionInfo("s2", MediaPlatform.YouTube, "Video B", "Channel B", "", "chrome", MediaPlaybackState.Paused, false);
        controller.PrimarySession.Returns(s1);
        controller.ActiveSessions.Returns(new[] { s1, s2 });

        var vm = new MediaPanelViewModel(_mediaService, _launcher, _dialogService, controller);
        Assert.True(vm.IsCollapsed);
        Assert.Equal("Sesi Lainnya (1) ▼", vm.CollapseButtonText);

        vm.ToggleCollapse();
        Assert.False(vm.IsCollapsed);
        Assert.Equal("Tampilkan Lebih Sedikit ▲", vm.CollapseButtonText);

        vm.ToggleCollapse();
        Assert.True(vm.IsCollapsed);
        Assert.Equal("Sesi Lainnya (1) ▼", vm.CollapseButtonText);
    }

    [Fact]
    public void MultiSession_MakeTopSession_InvokesControllerSetPrimarySession()
    {
        var controller = Substitute.For<IMediaPlaybackController>();
        var s1 = new MediaSessionInfo("s1", MediaPlatform.Spotify, "Song A", "Artist A", "", "spotify", MediaPlaybackState.Playing, true);
        var s2 = new MediaSessionInfo("s2", MediaPlatform.YouTube, "Video B", "Channel B", "", "chrome", MediaPlaybackState.Paused, false);
        controller.PrimarySession.Returns(s1);
        controller.ActiveSessions.Returns(new[] { s1, s2 });

        var vm = new MediaPanelViewModel(_mediaService, _launcher, _dialogService, controller);
        var secondaryItem = vm.SecondarySessions[0];

        secondaryItem.MakeTopCommand.Execute(null);

        controller.Received(1).SetPrimarySession("s2");
    }

    [Fact]
    public async Task MultiSession_TogglePlayPauseSession_InvokesController()
    {
        var controller = Substitute.For<IMediaPlaybackController>();
        var s1 = new MediaSessionInfo("s1", MediaPlatform.Spotify, "Song A", "Artist A", "", "spotify", MediaPlaybackState.Playing, true);
        var s2 = new MediaSessionInfo("s2", MediaPlatform.YouTube, "Video B", "Channel B", "", "chrome", MediaPlaybackState.Paused, false);
        controller.PrimarySession.Returns(s1);
        controller.ActiveSessions.Returns(new[] { s1, s2 });

        var vm = new MediaPanelViewModel(_mediaService, _launcher, _dialogService, controller);
        var secondaryItem = vm.SecondarySessions[0];

        await secondaryItem.TogglePlayPauseCommand.ExecuteAsync(null);

        await controller.Received(1).PlayPauseSessionAsync("s2");
    }

    [Fact]
    public void MultiSession_TabClosed_RemovesFromListAutomatically()
    {
        var controller = Substitute.For<IMediaPlaybackController>();
        var s1 = new MediaSessionInfo("s1", MediaPlatform.Spotify, "Song A", "Artist A", "", "spotify", MediaPlaybackState.Playing, true);
        var s2 = new MediaSessionInfo("s2", MediaPlatform.YouTube, "Video B", "Channel B", "", "chrome", MediaPlaybackState.Paused, false);
        controller.PrimarySession.Returns(s1);
        controller.ActiveSessions.Returns(new[] { s1, s2 });

        var vm = new MediaPanelViewModel(_mediaService, _launcher, _dialogService, controller);
        Assert.Single(vm.SecondarySessions);

        // Simulate browser tab s2 closed
        controller.ActiveSessions.Returns(new[] { s1 });
        vm.UpdateSessions(new[] { s1 });

        Assert.Empty(vm.SecondarySessions);
        Assert.False(vm.HasMultipleSessions);
        Assert.NotNull(vm.PrimarySession);
        Assert.Equal("s1", vm.PrimarySession.SessionId);
    }

    [Fact]
    public void MultiSession_AllClosed_ClearsSessionsAndResetsTrackDisplay()
    {
        var controller = Substitute.For<IMediaPlaybackController>();
        var s1 = new MediaSessionInfo("s1", MediaPlatform.Spotify, "Song A", "Artist A", "", "spotify", MediaPlaybackState.Playing, true);
        controller.PrimarySession.Returns(s1);
        controller.ActiveSessions.Returns(new[] { s1 });

        var vm = new MediaPanelViewModel(_mediaService, _launcher, _dialogService, controller);
        Assert.NotNull(vm.PrimarySession);

        // All tabs / apps closed
        vm.UpdateSessions(Array.Empty<MediaSessionInfo>());

        Assert.Null(vm.PrimarySession);
        Assert.Empty(vm.SecondarySessions);
        Assert.False(vm.HasActiveSessions);
        Assert.False(vm.HasMultipleSessions);
        Assert.Equal("Tidak ada media aktif", vm.TrackTitle);
        Assert.Equal("Buka Spotify, YouTube, atau YouTube Music", vm.TrackArtist);
    }

    [Fact]
    public void MultiSession_DismissSession_RemovesAndInvokesController()
    {
        var controller = Substitute.For<IMediaPlaybackController>();
        var s1 = new MediaSessionInfo("s1", MediaPlatform.Spotify, "Song A", "Artist A", "", "spotify", MediaPlaybackState.Playing, true);
        var s2 = new MediaSessionInfo("s2", MediaPlatform.YouTube, "Video B", "Channel B", "", "chrome", MediaPlaybackState.Paused, false);
        controller.PrimarySession.Returns(s1);
        controller.ActiveSessions.Returns(new[] { s1, s2 });

        var vm = new MediaPanelViewModel(_mediaService, _launcher, _dialogService, controller);
        Assert.Single(vm.SecondarySessions);

        var sec = vm.SecondarySessions[0];
        sec.DismissCommand.Execute(null);

        controller.Received(1).DismissSession("s2");
        Assert.Empty(vm.SecondarySessions);
        Assert.False(vm.HasMultipleSessions);
    }

    [Fact]
    public void MultiSession_CyclePlatform_SwitchesPlatformAndInvokesController()
    {
        var controller = Substitute.For<IMediaPlaybackController>();
        var s1 = new MediaSessionInfo("s1", MediaPlatform.YouTube, "Song A", "Artist A", "", "opera", MediaPlaybackState.Playing, true);
        controller.PrimarySession.Returns(s1);
        controller.ActiveSessions.Returns(new[] { s1 });

        var vm = new MediaPanelViewModel(_mediaService, _launcher, _dialogService, controller);
        Assert.Equal(MediaPlatform.YouTube, vm.PrimarySession!.Platform);

        // YouTube -> YouTubeMusic
        vm.CyclePrimaryPlatformCommand.Execute(null);
        controller.Received(1).SetSessionPlatform("s1", MediaPlatform.YouTubeMusic);
        Assert.Equal(MediaPlatform.YouTubeMusic, vm.PrimarySession.Platform);

        // YouTubeMusic -> Spotify
        vm.CyclePrimaryPlatformCommand.Execute(null);
        controller.Received(1).SetSessionPlatform("s1", MediaPlatform.Spotify);
        Assert.Equal(MediaPlatform.Spotify, vm.PrimarySession.Platform);
    }
}
