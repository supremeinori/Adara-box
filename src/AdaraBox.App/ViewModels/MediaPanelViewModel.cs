using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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

    // Apple Music Style Now Playing Card
    [ObservableProperty]
    private string _trackTitle = "Tidak ada media aktif";

    [ObservableProperty]
    private string _trackArtist = "Buka Spotify, YouTube, atau player musik";

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
            _mediaController.TrackChanged += (s, track) =>
            {
                var app = System.Windows.Application.Current;
                if (app != null && !app.Dispatcher.HasShutdownStarted)
                {
                    app.Dispatcher.InvokeAsync(() => UpdateTrackDisplay(track));
                }
                else
                {
                    UpdateTrackDisplay(track);
                }
            };

            _mediaController.PlaybackStateChanged += (s, state) =>
            {
                var app = System.Windows.Application.Current;
                if (app != null && !app.Dispatcher.HasShutdownStarted)
                {
                    app.Dispatcher.InvokeAsync(() =>
                    {
                        IsPlaying = state == MediaPlaybackState.Playing;
                    });
                }
                else
                {
                    IsPlaying = state == MediaPlaybackState.Playing;
                }
            };

            UpdateTrackDisplay(_mediaController.CurrentTrack);
            IsPlaying = _mediaController.CurrentState == MediaPlaybackState.Playing;
        }

        _ = LoadLinksAsync();
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
            TrackArtist = "Buka Spotify, YouTube, atau player musik";
            HasActiveTrack = false;
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
