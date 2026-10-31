namespace AdaraBox.Core.Media;

/// <summary>
/// Information about an active media playback session from a supported platform.
/// </summary>
public sealed record MediaSessionInfo(
    string SessionId,
    MediaPlatform Platform,
    string Title,
    string Artist,
    string AlbumTitle,
    string SourceAppId,
    MediaPlaybackState State,
    bool IsPlaying)
{
    public string FormattedDisplayName =>
        !string.IsNullOrWhiteSpace(Artist)
            ? $"{Title} — {Artist}"
            : Title;

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
}
