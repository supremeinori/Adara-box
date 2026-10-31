using System;
using System.Threading.Tasks;

namespace AdaraBox.Core.Media;

/// <summary>
/// Controls system media playback via native OS transport controls.
/// </summary>
public interface IMediaPlaybackController : IDisposable
{
    MediaPlaybackState CurrentState { get; }
    MediaTrackInfo? CurrentTrack { get; }
    bool IsAvailable { get; }

    event EventHandler<MediaTrackInfo?>? TrackChanged;
    event EventHandler<MediaPlaybackState>? PlaybackStateChanged;

    Task<bool> PlayPauseAsync();
    Task<bool> NextAsync();
    Task<bool> PreviousAsync();
}
