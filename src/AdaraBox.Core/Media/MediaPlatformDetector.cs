using System;
using System.Linq;

namespace AdaraBox.Core.Media;

/// <summary>
/// Detects and normalizes media platforms and track metadata.
/// Supports Spotify (Desktop & Web), YouTube Music, and YouTube (across all browsers including Opera, Chrome, Edge, Brave, Firefox).
/// </summary>
public static class MediaPlatformDetector
{
    private static readonly string[] BrowserKeywords = new[]
    {
        "opera", "chrome", "edge", "msedge", "brave", "firefox", "vivaldi", "arc", "browser", "waterfox", "librewolf", "floorp"
    };

    public static bool IsBrowser(string? sourceAppId)
    {
        if (string.IsNullOrWhiteSpace(sourceAppId)) return false;
        var app = sourceAppId.ToLowerInvariant();
        return BrowserKeywords.Any(k => app.Contains(k));
    }

    public static MediaPlatform Detect(string? sourceAppId, string? title, string? albumTitle, string? artist, bool isVideo = false)
    {
        var app = sourceAppId ?? string.Empty;
        var t = title ?? string.Empty;
        var a = albumTitle ?? string.Empty;
        var art = artist ?? string.Empty;

        // 1. Check Spotify (Desktop App, Web Player with explicit tag, or URL/AppId)
        if (app.IndexOf("spotify", StringComparison.OrdinalIgnoreCase) >= 0 ||
            t.IndexOf("spotify", StringComparison.OrdinalIgnoreCase) >= 0 ||
            a.IndexOf("spotify", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return MediaPlatform.Spotify;
        }

        // 2. Check YouTube Music (PWA, Web App, or explicit metadata)
        if (t.IndexOf("youtube music", StringComparison.OrdinalIgnoreCase) >= 0 ||
            a.IndexOf("youtube music", StringComparison.OrdinalIgnoreCase) >= 0 ||
            app.IndexOf("youtubemusic", StringComparison.OrdinalIgnoreCase) >= 0 ||
            app.IndexOf("music.youtube", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return MediaPlatform.YouTubeMusic;
        }

        // 3. Check YouTube (Explicit in title/album/source)
        if (t.IndexOf("youtube", StringComparison.OrdinalIgnoreCase) >= 0 ||
            a.IndexOf("youtube", StringComparison.OrdinalIgnoreCase) >= 0 ||
            app.IndexOf("youtube", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return MediaPlatform.YouTube;
        }

        // 4. Browser Playback Detection:
        // By default, general media playing in a browser (Opera, Chrome, Edge, Brave, etc.)
        // is YouTube / web video. Spotify is detected when using Spotify Desktop, Spotify PWA,
        // or when Spotify is explicitly present in title/album/app.
        if (IsBrowser(app))
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
