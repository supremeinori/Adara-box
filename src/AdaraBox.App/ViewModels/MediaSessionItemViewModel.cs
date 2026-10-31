using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AdaraBox.Core.Media;

namespace AdaraBox.App.ViewModels;

public sealed partial class MediaSessionItemViewModel : ObservableObject
{
    private readonly MediaPanelViewModel _parent;

    public string SessionId { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PlatformDisplayName))]
    [NotifyPropertyChangedFor(nameof(PlatformBadgeColor))]
    [NotifyPropertyChangedFor(nameof(PlatformIcon))]
    private MediaPlatform _platform;

    [ObservableProperty]
    private string _title;

    [ObservableProperty]
    private string _artist;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PlayPauseButtonText))]
    private bool _isPlaying;

    [ObservableProperty]
    private bool _isPrimary;

    public string PlatformDisplayName => Platform switch
    {
        MediaPlatform.Spotify => "Spotify",
        MediaPlatform.YouTube => "YouTube",
        MediaPlatform.YouTubeMusic => "YouTube Music",
        _ => "Media"
    };

    public string PlatformBadgeColor => Platform switch
    {
        MediaPlatform.Spotify => "#1DB954",
        MediaPlatform.YouTube => "#FF0000",
        MediaPlatform.YouTubeMusic => "#FF4E45",
        _ => "#A1A1AA"
    };

    public string PlatformIcon => Platform switch
    {
        MediaPlatform.Spotify => "🟢",
        MediaPlatform.YouTube => "🔴",
        MediaPlatform.YouTubeMusic => "🎵",
        _ => "▶"
    };

    public string PlayPauseButtonText => IsPlaying ? "⏸ Pause" : "▶ Play";
    public string PlayPauseIconText => IsPlaying ? "⏸" : "▶";

    public MediaSessionItemViewModel(MediaSessionInfo info, MediaPanelViewModel parent, bool isPrimary = false)
    {
        _parent = parent ?? throw new ArgumentNullException(nameof(parent));
        SessionId = info.SessionId;
        Platform = info.Platform;
        _title = info.Title;
        _artist = !string.IsNullOrWhiteSpace(info.Artist) ? info.Artist : "Artis tidak diketahui";
        _isPlaying = info.IsPlaying;
        _isPrimary = isPrimary;
    }

    public void Update(MediaSessionInfo info, bool isPrimary)
    {
        Platform = info.Platform;
        Title = info.Title;
        Artist = !string.IsNullOrWhiteSpace(info.Artist) ? info.Artist : "Artis tidak diketahui";
        IsPlaying = info.IsPlaying;
        IsPrimary = isPrimary;
    }

    [RelayCommand]
    public async Task TogglePlayPauseAsync()
    {
        await _parent.TogglePlayPauseSessionAsync(this);
    }

    [RelayCommand]
    public void MakeTop()
    {
        _parent.MakeTopSession(this);
    }

    [RelayCommand]
    public void Dismiss()
    {
        _parent.DismissSession(this);
    }

    [RelayCommand]
    public void CyclePlatform()
    {
        _parent.CyclePlatform(this);
    }
}
