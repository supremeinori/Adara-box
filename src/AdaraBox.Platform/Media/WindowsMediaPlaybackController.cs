using System;
using System.Threading.Tasks;
using Windows.Media.Control;
using AdaraBox.Core.Media;
using Serilog;

namespace AdaraBox.Platform.Media;

/// <summary>
/// Native Windows Global System Media Transport Controls (GSMTC) controller.
/// Detects and controls active media from Spotify Desktop, browsers (Chrome/Edge/Firefox), Apple Music, VLC, etc.
/// </summary>
public sealed class WindowsMediaPlaybackController : IMediaPlaybackController
{
    private readonly ILogger _logger = Log.ForContext<WindowsMediaPlaybackController>();
    private GlobalSystemMediaTransportControlsSessionManager? _sessionManager;
    private GlobalSystemMediaTransportControlsSession? _currentSession;
    private readonly object _lock = new();

    public MediaPlaybackState CurrentState { get; private set; } = MediaPlaybackState.None;
    public MediaTrackInfo? CurrentTrack { get; private set; }
    public bool IsAvailable => _sessionManager != null;

    public event EventHandler<MediaTrackInfo?>? TrackChanged;
    public event EventHandler<MediaPlaybackState>? PlaybackStateChanged;

    public WindowsMediaPlaybackController()
    {
        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        try
        {
            _sessionManager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
            if (_sessionManager != null)
            {
                _sessionManager.CurrentSessionChanged += OnCurrentSessionChanged;
                UpdateCurrentSession(_sessionManager.GetCurrentSession());
            }
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Failed to initialize Windows GSMTC SessionManager.");
        }
    }

    private void OnCurrentSessionChanged(GlobalSystemMediaTransportControlsSessionManager sender, CurrentSessionChangedEventArgs args)
    {
        try
        {
            UpdateCurrentSession(sender.GetCurrentSession());
        }
        catch (Exception ex)
        {
            _logger.Debug(ex, "Error handling GSMTC CurrentSessionChanged.");
        }
    }

    private void UpdateCurrentSession(GlobalSystemMediaTransportControlsSession? newSession)
    {
        lock (_lock)
        {
            if (_currentSession != null)
            {
                _currentSession.MediaPropertiesChanged -= OnMediaPropertiesChanged;
                _currentSession.PlaybackInfoChanged -= OnPlaybackInfoChanged;
            }

            _currentSession = newSession;

            if (_currentSession != null)
            {
                _currentSession.MediaPropertiesChanged += OnMediaPropertiesChanged;
                _currentSession.PlaybackInfoChanged += OnPlaybackInfoChanged;
            }
        }

        _ = RefreshSessionDetailsAsync(_currentSession);
    }

    private void OnMediaPropertiesChanged(GlobalSystemMediaTransportControlsSession sender, MediaPropertiesChangedEventArgs args)
    {
        _ = RefreshSessionDetailsAsync(sender);
    }

    private void OnPlaybackInfoChanged(GlobalSystemMediaTransportControlsSession sender, PlaybackInfoChangedEventArgs args)
    {
        _ = RefreshPlaybackStateAsync(sender);
    }

    private async Task RefreshSessionDetailsAsync(GlobalSystemMediaTransportControlsSession? session)
    {
        if (session == null)
        {
            SetTrack(null);
            SetPlaybackState(MediaPlaybackState.None);
            return;
        }

        try
        {
            var mediaProps = await session.TryGetMediaPropertiesAsync();
            if (mediaProps != null && !string.IsNullOrWhiteSpace(mediaProps.Title))
            {
                var track = new MediaTrackInfo(
                    mediaProps.Title,
                    mediaProps.Artist ?? string.Empty,
                    mediaProps.AlbumTitle ?? string.Empty,
                    session.SourceAppUserModelId ?? string.Empty);
                SetTrack(track);
            }
            else
            {
                SetTrack(null);
            }

            await RefreshPlaybackStateAsync(session);
        }
        catch (Exception ex)
        {
            _logger.Debug(ex, "Error reading GSMTC media properties.");
        }
    }

    private Task RefreshPlaybackStateAsync(GlobalSystemMediaTransportControlsSession session)
    {
        try
        {
            var info = session.GetPlaybackInfo();
            if (info != null)
            {
                var state = info.PlaybackStatus switch
                {
                    GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing => MediaPlaybackState.Playing,
                    GlobalSystemMediaTransportControlsSessionPlaybackStatus.Paused => MediaPlaybackState.Paused,
                    GlobalSystemMediaTransportControlsSessionPlaybackStatus.Stopped => MediaPlaybackState.Stopped,
                    _ => MediaPlaybackState.None
                };
                SetPlaybackState(state);
            }
            else
            {
                SetPlaybackState(MediaPlaybackState.None);
            }
        }
        catch (Exception ex)
        {
            _logger.Debug(ex, "Error reading GSMTC playback info.");
        }

        return Task.CompletedTask;
    }

    private void SetTrack(MediaTrackInfo? track)
    {
        if (CurrentTrack != track)
        {
            CurrentTrack = track;
            TrackChanged?.Invoke(this, track);
        }
    }

    private void SetPlaybackState(MediaPlaybackState state)
    {
        if (CurrentState != state)
        {
            CurrentState = state;
            PlaybackStateChanged?.Invoke(this, state);
        }
    }

    public async Task<bool> PlayPauseAsync()
    {
        try
        {
            GlobalSystemMediaTransportControlsSession? session;
            lock (_lock) session = _currentSession;

            if (session != null)
            {
                return await session.TryTogglePlayPauseAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Failed to toggle play/pause via GSMTC.");
        }

        return false;
    }

    public async Task<bool> NextAsync()
    {
        try
        {
            GlobalSystemMediaTransportControlsSession? session;
            lock (_lock) session = _currentSession;

            if (session != null)
            {
                return await session.TrySkipNextAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Failed to skip next via GSMTC.");
        }

        return false;
    }

    public async Task<bool> PreviousAsync()
    {
        try
        {
            GlobalSystemMediaTransportControlsSession? session;
            lock (_lock) session = _currentSession;

            if (session != null)
            {
                return await session.TrySkipPreviousAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Failed to skip previous via GSMTC.");
        }

        return false;
    }

    public void Dispose()
    {
        lock (_lock)
        {
            if (_currentSession != null)
            {
                _currentSession.MediaPropertiesChanged -= OnMediaPropertiesChanged;
                _currentSession.PlaybackInfoChanged -= OnPlaybackInfoChanged;
                _currentSession = null;
            }

            if (_sessionManager != null)
            {
                _sessionManager.CurrentSessionChanged -= OnCurrentSessionChanged;
                _sessionManager = null;
            }
        }
    }
}
