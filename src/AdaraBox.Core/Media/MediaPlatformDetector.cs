using System;

namespace AdaraBox.Core.Media;

/// <summary>
/// Detects and normalizes media platforms and track metadata.
/// Strictly filters to Spotify, YouTube, and YouTube Music.
/// </summary>
public static class MediaPlatformDetector
{
    public static MediaPlatform Detect(string? sourceAppId, string? title, string? albumTitle, string? artist)
    {
        var app = sourceAppId ?? string.Empty;
        var t = title ?? string.Empty;
        var a = albumTitle ?? string.Empty;
        var art = artist ?? string.Empty;

        // 1. Check Spotify (Desktop app or browser web player)
        if (app.IndexOf("spotify", StringComparison.OrdinalIgnoreCase) >= 0 ||
            t.IndexOf("spotify", StringComparison.OrdinalIgnoreCase) >= 0 ||
            a.IndexOf("spotify", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return MediaPlatform.Spotify;
        }

        // 2. Check YouTube Music (Must precede generic YouTube check)
        if (t.IndexOf("youtube music", StringComparison.OrdinalIgnoreCase) >= 0 ||
            a.IndexOf("youtube music", StringComparison.OrdinalIgnoreCase) >= 0 ||
            app.IndexOf("youtubemusic", StringComparison.OrdinalIgnoreCase) >= 0 ||
            app.IndexOf("music.youtube", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return MediaPlatform.YouTubeMusic;
        }

        // 3. Check YouTube
        if (t.IndexOf("youtube", StringComparison.OrdinalIgnoreCase) >= 0 ||
            a.IndexOf("youtube", StringComparison.OrdinalIgnoreCase) >= 0 ||
            app.IndexOf("youtube", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return MediaPlatform.YouTube;
        }

        return MediaPlatform.Unknown;
    }

    public static string CleanTitle(string? rawTitle, MediaPlatform platform)
    {
        if (string.IsNullOrWhiteSpace(rawTitle)) return string.Empty;
        var t = rawTitle.Trim();

        if (platform == MediaPlatform.YouTube)
        {
            if (t.EndsWith(" - YouTube", StringComparison.OrdinalIgnoreCase))
                return t[..^10].Trim();
            if (t.EndsWith("— YouTube", StringComparison.OrdinalIgnoreCase))
                return t[..^9].Trim();
            if (t.EndsWith("- YouTube", StringComparison.OrdinalIgnoreCase))
                return t[..^9].Trim();
        }
        else if (platform == MediaPlatform.YouTubeMusic)
        {
            if (t.EndsWith(" - YouTube Music", StringComparison.OrdinalIgnoreCase))
                return t[..^16].Trim();
            if (t.EndsWith("— YouTube Music", StringComparison.OrdinalIgnoreCase))
                return t[..^15].Trim();
        }
        else if (platform == MediaPlatform.Spotify)
        {
            if (t.EndsWith(" - Spotify", StringComparison.OrdinalIgnoreCase))
                return t[..^10].Trim();
            if (t.EndsWith(" · Spotify", StringComparison.OrdinalIgnoreCase))
                return t[..^10].Trim();
        }

        return t;
    }
}
