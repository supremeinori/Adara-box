using AdaraBox.Core.Media;
using Xunit;

namespace AdaraBox.Core.Tests;

public class MediaPlatformDetectorTests
{
    [Theory]
    [InlineData("Spotify.exe", "Track", "Album", "Artist", MediaPlatform.Spotify)]
    [InlineData("SpotifyAB.SpotifyMusic_z6rc5r9iwwwb8", "Track", "Album", "Artist", MediaPlatform.Spotify)]
    [InlineData("chrome.exe", "Rick Astley - Never Gonna Give You Up - YouTube", "", "", MediaPlatform.YouTube)]
    [InlineData("msedge.exe", "Chill Beats - YouTube", "", "", MediaPlatform.YouTube)]
    [InlineData("chrome.exe", "Starboy", "YouTube Music", "The Weeknd", MediaPlatform.YouTubeMusic)]
    [InlineData("music.youtube.pwa", "Song", "Album", "Artist", MediaPlatform.YouTubeMusic)]
    [InlineData("chrome.exe", "Song Name - Spotify", "", "", MediaPlatform.Spotify)]
    [InlineData("vlc.exe", "MyMovie.mkv", "", "", MediaPlatform.Unknown)]
    [InlineData("chrome.exe", "Twitter / X Video Player", "", "", MediaPlatform.Unknown)]
    [InlineData("brave.exe", "Random Sound Clip", "Reddit", "", MediaPlatform.Unknown)]
    public void Detect_ReturnsExpectedPlatform(string app, string title, string album, string artist, MediaPlatform expected)
    {
        var platform = MediaPlatformDetector.Detect(app, title, album, artist);
        Assert.Equal(expected, platform);
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
