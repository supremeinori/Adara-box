using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AdaraBox.App.Resources;
using AdaraBox.App.Services;
using AdaraBox.Core.Launcher;
using AdaraBox.Core.Media;
using AdaraBox.Core.Resources;

namespace AdaraBox.App.ViewModels;

public sealed partial class MediaPanelViewModel : ObservableObject
{
    private readonly IMediaLinkService _mediaLinkService;
    private readonly IResourceLauncher _launcher;
    private readonly IDialogService _dialogService;
    private readonly IMediaPlaybackController? _mediaController;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isFormOpen;

    [ObservableProperty]
    private string _formTitle = UiText.AddMediaLinkTitle;

    [ObservableProperty]
    private string _formName = string.Empty;

    [ObservableProperty]
    private string _formUrl = string.Empty;

    [ObservableProperty]
    private string? _formError;

    // Multi-Session Media Support
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasActiveSessions))]
    private MediaSessionItemViewModel? _primarySession;

    public ObservableCollection<MediaSessionItemViewModel> SecondarySessions { get; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CollapseButtonText))]
    private bool _isCollapsed = true;

    public bool HasMultipleSessions => SecondarySessions.Count > 0;
    public bool HasActiveSessions => PrimarySession != null;

    public string CollapseButtonText => IsCollapsed
        ? $"Sesi Lainnya ({SecondarySessions.Count}) ▼"
        : "Tampilkan Lebih Sedikit ▲";

    // Legacy/TopBar compatibility bindings
    [ObservableProperty]
    private string _trackTitle = "Tidak ada media aktif";

    [ObservableProperty]
    private string _trackArtist = "Buka Spotify, YouTube, atau YouTube Music";

    [ObservableProperty]
    private bool _hasActiveTrack;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PlayPauseButtonText))]
    private bool _isPlaying;

    public string PlayPauseButtonText => IsPlaying ? "⏸ Pause" : "▶ Play";

    public ObservableCollection<MediaLinkItemViewModel> Links { get; } = new();

    public bool HasLinks => Links.Count > 0;
    public bool IsEmpty => !IsLoading && Links.Count == 0;
    public bool HasFormError => !string.IsNullOrEmpty(FormError);

    public string BrowserNoticeText => UiText.MediaBrowserNotice;
    public string ControlsNoticeText => UiText.MediaControlsNotice;
    public string EmptyTitle => UiText.MediaEmpty;
    public string EmptyDescription => UiText.MediaEmptyDescription;

    public MediaPanelViewModel(
        IMediaLinkService mediaLinkService,
        IResourceLauncher launcher,
        IDialogService dialogService,
        IMediaPlaybackController? mediaController = null)
    {
        _mediaLinkService = mediaLinkService ?? throw new ArgumentNullException(nameof(mediaLinkService));
        _launcher = launcher ?? throw new ArgumentNullException(nameof(launcher));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        _mediaController = mediaController;

        if (_mediaController != null)
        {
            _mediaController.SessionsChanged += (s, sessions) =>
            {
                RunOnDispatcher(() => UpdateSessions(sessions));
            };

            _mediaController.TrackChanged += (s, track) =>
            {
                RunOnDispatcher(() => UpdateTrackDisplay(track));
            };

            _mediaController.PlaybackStateChanged += (s, state) =>
            {
                RunOnDispatcher(() =>
                {
                    IsPlaying = state == MediaPlaybackState.Playing;
                    if (PrimarySession != null)
                    {
                        PrimarySession.IsPlaying = IsPlaying;
                    }
                });
            };

            if (_mediaController.ActiveSessions.Count > 0)
            {
                UpdateSessions(_mediaController.ActiveSessions);
            }
            else
            {
                UpdateTrackDisplay(_mediaController.CurrentTrack);
                IsPlaying = _mediaController.CurrentState == MediaPlaybackState.Playing;
            }
        }

        _ = LoadLinksAsync();
    }

    private void RunOnDispatcher(Action action)
    {
        var app = Application.Current;
        if (app != null && !app.Dispatcher.HasShutdownStarted)
        {
            app.Dispatcher.InvokeAsync(action);
        }
        else
        {
            action();
        }
    }

    public void UpdateSessions(IReadOnlyList<MediaSessionInfo> sessions)
    {
        if (sessions == null || sessions.Count == 0)
        {
            PrimarySession = null;
            SecondarySessions.Clear();
            UpdateTrackDisplay(null);
            IsPlaying = false;
            OnPropertyChanged(nameof(HasMultipleSessions));
            OnPropertyChanged(nameof(HasActiveSessions));
            OnPropertyChanged(nameof(CollapseButtonText));
            return;
        }

        var primaryInfo = _mediaController?.PrimarySession ?? sessions[0];
        if (PrimarySession == null || PrimarySession.SessionId != primaryInfo.SessionId)
        {
            PrimarySession = new MediaSessionItemViewModel(primaryInfo, this, true);
        }
        else
        {
            PrimarySession.Update(primaryInfo, true);
        }

        // Secondary sessions (all other sessions except primary)
        var secondaryInfos = sessions.Where(s => s.SessionId != primaryInfo.SessionId).ToList();

        // Reconcile secondary list
        for (var i = SecondarySessions.Count - 1; i >= 0; i--)
        {
            var item = SecondarySessions[i];
            var stillExists = secondaryInfos.Any(s => s.SessionId == item.SessionId);
            if (!stillExists)
            {
                SecondarySessions.RemoveAt(i);
            }
        }

        foreach (var secInfo in secondaryInfos)
        {
            var existing = SecondarySessions.FirstOrDefault(s => s.SessionId == secInfo.SessionId);
            if (existing != null)
            {
                existing.Update(secInfo, false);
            }
            else
            {
                SecondarySessions.Add(new MediaSessionItemViewModel(secInfo, this, false));
            }
        }

        TrackTitle = PrimarySession.Title;
        TrackArtist = PrimarySession.Artist;
        HasActiveTrack = true;
        IsPlaying = PrimarySession.IsPlaying;

        OnPropertyChanged(nameof(HasMultipleSessions));
        OnPropertyChanged(nameof(HasActiveSessions));
        OnPropertyChanged(nameof(CollapseButtonText));
    }

    private void UpdateTrackDisplay(MediaTrackInfo? track)
    {
        if (track != null)
        {
            TrackTitle = track.Title;
            TrackArtist = !string.IsNullOrWhiteSpace(track.Artist) ? track.Artist : "Artis tidak diketahui";
            HasActiveTrack = true;
        }
        else
        {
            TrackTitle = "Tidak ada media aktif";
            TrackArtist = "Buka Spotify, YouTube, atau YouTube Music";
            HasActiveTrack = false;
        }
    }

    [RelayCommand]
    public void ToggleCollapse()
    {
        IsCollapsed = !IsCollapsed;
        OnPropertyChanged(nameof(CollapseButtonText));
    }

    public async Task TogglePlayPauseSessionAsync(MediaSessionItemViewModel session)
    {
        if (_mediaController != null && session != null)
        {
            await _mediaController.PlayPauseSessionAsync(session.SessionId);
        }
    }

    public void MakeTopSession(MediaSessionItemViewModel session)
    {
        if (_mediaController != null && session != null)
        {
            _mediaController.SetPrimarySession(session.SessionId);
        }
    }

    [RelayCommand]
    public async Task PlayPauseAsync()
    {
        if (_mediaController != null)
        {
            await _mediaController.PlayPauseAsync();
        }
    }

    [RelayCommand]
    public async Task NextTrackAsync()
    {
        if (_mediaController != null)
        {
            await _mediaController.NextAsync();
        }
    }

    [RelayCommand]
    public async Task PreviousTrackAsync()
    {
        if (_mediaController != null)
        {
            await _mediaController.PreviousAsync();
        }
    }

    [RelayCommand]
    public async Task LoadLinksAsync()
    {
        try
        {
            IsLoading = true;
            NotifyListState();

            Links.Clear();
            var list = await _mediaLinkService.ListAsync();
            foreach (var link in list)
            {
                Links.Add(new MediaLinkItemViewModel(link, this));
            }
        }
        finally
        {
            IsLoading = false;
            NotifyListState();
        }
    }

    [RelayCommand]
    public void BeginAdd()
    {
        FormTitle = UiText.AddMediaLinkTitle;
        FormName = string.Empty;
        FormUrl = string.Empty;
        FormError = null;
        IsFormOpen = true;
    }

    [RelayCommand]
    public void CancelForm()
    {
        IsFormOpen = false;
        FormName = string.Empty;
        FormUrl = string.Empty;
        FormError = null;
    }

    [RelayCommand]
    public async Task SaveFormAsync()
    {
        FormError = null;

        var result = await _mediaLinkService.CreateAsync(FormName, FormUrl);
        if (!result.Success)
        {
            FormError = result.Error?.Message ?? UiText.ErrorUnknown;
            return;
        }

        Links.Add(new MediaLinkItemViewModel(result.Value!, this));
        IsFormOpen = false;
        FormName = string.Empty;
        FormUrl = string.Empty;
        NotifyListState();
    }

    public async Task LaunchLinkAsync(MediaLinkItemViewModel item)
    {
        if (item == null) return;

        var dummyResource = new Resource(
            item.Id,
            Guid.Empty,
            item.Name,
            ResourceType.Link,
            item.Url,
            item.SortOrder,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow);

        var result = await _launcher.LaunchAsync(dummyResource);
        if (!result.Success)
        {
            await _dialogService.ShowErrorAsync(UiText.StatusError, result.Message ?? "Gagal membuka tautan di browser.");
        }
    }

    public async Task DeleteLinkAsync(MediaLinkItemViewModel item)
    {
        if (item == null) return;

        var confirmed = await _dialogService.ConfirmDeleteResourceAsync(item.Name);
        if (!confirmed) return;

        var result = await _mediaLinkService.DeleteAsync(item.Id);
        if (!result.Success)
        {
            await _dialogService.ShowErrorAsync(UiText.StatusError, result.Error?.Message ?? UiText.ErrorUnknown);
            return;
        }

        Links.Remove(item);
        NotifyListState();
    }

    partial void OnFormErrorChanged(string? value)
    {
        OnPropertyChanged(nameof(HasFormError));
    }

    private void NotifyListState()
    {
        OnPropertyChanged(nameof(HasLinks));
        OnPropertyChanged(nameof(IsEmpty));
    }
}
