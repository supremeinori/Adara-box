using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AdaraBox.Core.Media;

/// <summary>
/// Controls system media playback via native OS transport controls.
/// Supports multi-session management and platform filtering (Spotify, YouTube, YouTube Music).
/// </summary>
public interface IMediaPlaybackController : IDisposable
{
    MediaPlaybackState CurrentState { get; }
    MediaTrackInfo? CurrentTrack { get; }
    bool IsAvailable { get; }

    IReadOnlyList<MediaSessionInfo> ActiveSessions { get; }
    MediaSessionInfo? PrimarySession { get; }

    event EventHandler<MediaTrackInfo?>? TrackChanged;
    event EventHandler<MediaPlaybackState>? PlaybackStateChanged;
    event EventHandler<IReadOnlyList<MediaSessionInfo>>? SessionsChanged;

    Task<bool> PlayPauseAsync();
    Task<bool> NextAsync();
    Task<bool> PreviousAsync();

    Task<bool> PlayPauseSessionAsync(string sessionId);
    Task<bool> NextSessionAsync(string sessionId);
    Task<bool> PreviousSessionAsync(string sessionId);
    void SetPrimarySession(string sessionId);
    void DismissSession(string sessionId);
    void SetSessionPlatform(string sessionId, MediaPlatform platform);
}
