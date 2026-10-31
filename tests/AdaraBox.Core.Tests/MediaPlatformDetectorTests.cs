using AdaraBox.Core.Media;
using Xunit;

namespace AdaraBox.Core.Tests;

public class MediaPlatformDetectorTests
{
    [Theory]
    [InlineData("Spotify.exe", "Track", "Album", "Artist", MediaPlatform.Spotify)]
    [InlineData("SpotifyAB.SpotifyMusic_z6rc5r9iwwwb8", "Track", "Album", "Artist", MediaPlatform.Spotify)]
    [InlineData("opera.exe", "Rick Astley - Never Gonna Give You Up - YouTube", "", "", MediaPlatform.YouTube)]
    [InlineData("opera_gx.exe", "Gaming Beats", "", "", MediaPlatform.YouTube)] // Opera GX browser fallback
    [InlineData("chrome.exe", "Rick Astley - Never Gonna Give You Up - YouTube", "", "", MediaPlatform.YouTube)]
    [InlineData("chrome.exe", "Song Without YouTube in Title", "", "", MediaPlatform.YouTube)] // Chrome browser fallback
    [InlineData("msedge.exe", "Chill Beats - YouTube", "", "", MediaPlatform.YouTube)]
    [InlineData("msedge.exe", "Lofi Music", "", "", MediaPlatform.YouTube)] // Edge browser fallback
    [InlineData("chrome.exe", "Starboy", "YouTube Music", "The Weeknd", MediaPlatform.YouTubeMusic)]
    [InlineData("music.youtube.pwa", "Song", "Album", "Artist", MediaPlatform.YouTubeMusic)]
    [InlineData("opera.exe", "Song Name - Spotify", "", "", MediaPlatform.Spotify)]
    [InlineData("chrome.exe", "Song Name - Spotify", "", "", MediaPlatform.Spotify)]
    [InlineData("spotify_pwa.exe", "33x", "", "Perunggu", MediaPlatform.Spotify)] // Spotify PWA or desktop
    [InlineData("opera.exe", "HEAD TO HEAD GURU GEMBUL", "", "GEMBULIKUM", MediaPlatform.YouTube)] // YouTube with channel name in artist
    [InlineData("vlc.exe", "MyMovie.mkv", "", "", MediaPlatform.Unknown)]
    [InlineData("potplayer.exe", "Local Video", "", "", MediaPlatform.Unknown)]
    [InlineData("discord.exe", "Voice Call", "", "", MediaPlatform.Unknown)]
    public void Detect_ReturnsExpectedPlatform(string app, string title, string album, string artist, MediaPlatform expected)
    {
        var platform = MediaPlatformDetector.Detect(app, title, album, artist);
        Assert.Equal(expected, platform);
    }

    [Fact]
    public void Detect_BrowserVideo_ReturnsYouTube()
    {
        var platform = MediaPlatformDetector.Detect("opera.exe", "HEAD TO HEAD GURU GEMBUL", "", "GEMBULIKUM", isVideo: true);
        Assert.Equal(MediaPlatform.YouTube, platform);
    }

    [Theory]
    [InlineData("Lofi Girl - Synthwave Radio - YouTube", MediaPlatform.YouTube, "Lofi Girl - Synthwave Radio")]
    [InlineData("Lofi Girl - Synthwave Radio — YouTube", MediaPlatform.YouTube, "Lofi Girl - Synthwave Radio")]
    [InlineData("Song Name - YouTube Music", MediaPlatform.YouTubeMusic, "Song Name")]
    [InlineData("Song Name - Spotify", MediaPlatform.Spotify, "Song Name")]
    [InlineData("Song Name · Spotify", MediaPlatform.Spotify, "Song Name")]
    [InlineData("Plain Song Title", MediaPlatform.Spotify, "Plain Song Title")]
    public void CleanTitle_RemovesPlatformSuffix(string raw, MediaPlatform platform, string expected)
    {
        var cleaned = MediaPlatformDetector.CleanTitle(raw, platform);
        Assert.Equal(expected, cleaned);
    }
}
