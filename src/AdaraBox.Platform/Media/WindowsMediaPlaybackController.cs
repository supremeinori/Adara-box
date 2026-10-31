using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Media.Control;
using AdaraBox.Core.Media;
using Serilog;

namespace AdaraBox.Platform.Media;

/// <summary>
/// Native Windows Global System Media Transport Controls (GSMTC) controller.
/// Detects and manages multiple active media sessions, strictly filtered to
/// Spotify, YouTube, and YouTube Music.
/// </summary>
public sealed class WindowsMediaPlaybackController : IMediaPlaybackController
{
    private readonly ILogger _logger = Log.ForContext<WindowsMediaPlaybackController>();
    private GlobalSystemMediaTransportControlsSessionManager? _sessionManager;
    private readonly List<TrackedSession> _trackedSessions = new();
    private readonly object _stateLock = new();
    private readonly SemaphoreSlim _refreshLock = new(1, 1);
    private System.Threading.Timer? _syncTimer;
    private string? _primarySessionId;

    public MediaPlaybackState CurrentState { get; private set; } = MediaPlaybackState.None;
    public MediaTrackInfo? CurrentTrack { get; private set; }
    public bool IsAvailable => _sessionManager != null;

    public IReadOnlyList<MediaSessionInfo> ActiveSessions { get; private set; } = Array.Empty<MediaSessionInfo>();
    public MediaSessionInfo? PrimarySession { get; private set; }

    public event EventHandler<MediaTrackInfo?>? TrackChanged;
    public event EventHandler<MediaPlaybackState>? PlaybackStateChanged;
    public event EventHandler<IReadOnlyList<MediaSessionInfo>>? SessionsChanged;

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
                _sessionManager.SessionsChanged += OnSessionsChanged;
                _sessionManager.CurrentSessionChanged += OnCurrentSessionChanged;

                // 2.5 second polling sync to immediately clean up closed browser tabs and apps
                _syncTimer = new System.Threading.Timer(OnSyncTimerTick, null, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(2.5));

                await RefreshAllSessionsAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Failed to initialize Windows GSMTC SessionManager.");
        }
    }

    private void OnSessionsChanged(GlobalSystemMediaTransportControlsSessionManager sender, SessionsChangedEventArgs args)
    {
        _ = RefreshAllSessionsAsync();
    }

    private void OnCurrentSessionChanged(GlobalSystemMediaTransportControlsSessionManager sender, CurrentSessionChangedEventArgs args)
    {
        _ = RefreshAllSessionsAsync();
    }

    private void OnSyncTimerTick(object? state)
    {
        _ = RefreshAllSessionsAsync();
    }

    private async Task RefreshAllSessionsAsync()
    {
        if (_sessionManager == null) return;
        if (!await _refreshLock.WaitAsync(100)) return;

        try
        {
            IReadOnlyList<GlobalSystemMediaTransportControlsSession> rawSessions;
            try
            {
                rawSessions = _sessionManager.GetSessions();
            }
            catch (Exception ex)
            {
                _logger.Debug(ex, "Failed to fetch GSMTC sessions.");
                return;
            }

            // 1. Reconcile tracked sessions with raw sessions from Windows
            lock (_stateLock)
            {
                // Remove sessions that are no longer returned by GSMTC
                for (var i = _trackedSessions.Count - 1; i >= 0; i--)
                {
                    var tracked = _trackedSessions[i];
                    var stillExists = rawSessions.Any(r => SessionMatches(tracked.Session, r));
                    if (!stillExists)
                    {
                        tracked.Dispose();
                        _trackedSessions.RemoveAt(i);
                    }
                }

                // Add newly discovered sessions
                foreach (var raw in rawSessions)
                {
                    var existing = _trackedSessions.FirstOrDefault(t => SessionMatches(t.Session, raw));
                    if (existing == null)
                    {
                        var newTracker = new TrackedSession(Guid.NewGuid().ToString("N"), raw);
                        newTracker.MediaPropertiesChanged += OnTrackedSessionMediaPropertiesChanged;
                        newTracker.PlaybackInfoChanged += OnTrackedSessionPlaybackInfoChanged;
                        _trackedSessions.Add(newTracker);
                    }
                }
            }

            // 2. Query properties & filter to Spotify, YouTube, YouTube Music only
            var updatedList = new List<MediaSessionInfo>();
            List<TrackedSession> currentTrackers;
            lock (_stateLock)
            {
                currentTrackers = _trackedSessions.ToList();
            }

            foreach (var tracker in currentTrackers)
            {
                try
                {
                    var playbackInfo = tracker.Session.GetPlaybackInfo();
                    if (playbackInfo == null ||
                        playbackInfo.PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Closed)
                    {
                        continue;
                    }

                    var mediaProps = await tracker.Session.TryGetMediaPropertiesAsync();
                    if (mediaProps == null || string.IsNullOrWhiteSpace(mediaProps.Title))
                    {
                        continue;
                    }

                    var sourceAppId = tracker.Session.SourceAppUserModelId ?? string.Empty;
                    var platform = MediaPlatformDetector.Detect(sourceAppId, mediaProps.Title, mediaProps.AlbumTitle, mediaProps.Artist);

                    // Strictly filter out non-supported platforms
                    if (platform == MediaPlatform.Unknown)
                    {
                        continue;
                    }

                    var cleanTitle = MediaPlatformDetector.CleanTitle(mediaProps.Title, platform);
                    var state = playbackInfo.PlaybackStatus switch
                    {
                        GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing => MediaPlaybackState.Playing,
                        GlobalSystemMediaTransportControlsSessionPlaybackStatus.Paused => MediaPlaybackState.Paused,
                        GlobalSystemMediaTransportControlsSessionPlaybackStatus.Stopped => MediaPlaybackState.Stopped,
                        _ => MediaPlaybackState.None
                    };

                    var info = new MediaSessionInfo(
                        tracker.Id,
                        platform,
                        cleanTitle,
                        mediaProps.Artist ?? string.Empty,
                        mediaProps.AlbumTitle ?? string.Empty,
                        sourceAppId,
                        state,
                        state == MediaPlaybackState.Playing);

                    tracker.CurrentInfo = info;
                    updatedList.Add(info);
                }
                catch (Exception ex)
                {
                    _logger.Debug(ex, "Failed to read properties for session {SessionId}.", tracker.Id);
                }
            }

            // 3. Determine Primary (On-Top) session
            MediaSessionInfo? newPrimary = null;
            if (updatedList.Count > 0)
            {
                if (!string.IsNullOrEmpty(_primarySessionId))
                {
                    newPrimary = updatedList.FirstOrDefault(s => s.SessionId == _primarySessionId);
                }

                if (newPrimary == null)
                {
                    newPrimary = updatedList[0];
                    _primarySessionId = newPrimary.SessionId;
                }
            }
            else
            {
                _primarySessionId = null;
            }

            // 4. Update state & notify
            bool sessionsChanged;
            bool primaryChanged;
            MediaPlaybackState oldState;
            MediaPlaybackState newState;

            lock (_stateLock)
            {
                sessionsChanged = !AreSessionsEquivalent(ActiveSessions, updatedList);
                primaryChanged = !Equals(PrimarySession, newPrimary);
                oldState = CurrentState;

                ActiveSessions = updatedList.AsReadOnly();
                PrimarySession = newPrimary;
                newState = PrimarySession?.State ?? MediaPlaybackState.None;
                CurrentState = newState;
                CurrentTrack = PrimarySession != null
                    ? new MediaTrackInfo(PrimarySession.Title, PrimarySession.Artist, PrimarySession.AlbumTitle, PrimarySession.SourceAppId)
                    : null;
            }

            if (sessionsChanged)
            {
                SessionsChanged?.Invoke(this, ActiveSessions);
            }

            if (primaryChanged)
            {
                TrackChanged?.Invoke(this, CurrentTrack);
            }

            if (oldState != newState || primaryChanged)
            {
                PlaybackStateChanged?.Invoke(this, CurrentState);
            }
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    private void OnTrackedSessionMediaPropertiesChanged(object? sender, EventArgs e)
    {
        _ = RefreshAllSessionsAsync();
    }

    private void OnTrackedSessionPlaybackInfoChanged(object? sender, EventArgs e)
    {
        _ = RefreshAllSessionsAsync();
    }

    private static bool SessionMatches(GlobalSystemMediaTransportControlsSession a, GlobalSystemMediaTransportControlsSession b)
    {
        if (ReferenceEquals(a, b)) return true;
        try
        {
            return a.Equals(b);
        }
        catch
        {
            return string.Equals(a.SourceAppUserModelId, b.SourceAppUserModelId, StringComparison.OrdinalIgnoreCase);
        }
    }

    private static bool AreSessionsEquivalent(IReadOnlyList<MediaSessionInfo> listA, IReadOnlyList<MediaSessionInfo> listB)
    {
        if (listA.Count != listB.Count) return false;
        for (var i = 0; i < listA.Count; i++)
        {
            var a = listA[i];
            var b = listB[i];
            if (a.SessionId != b.SessionId ||
                a.Title != b.Title ||
                a.Artist != b.Artist ||
                a.State != b.State ||
                a.Platform != b.Platform)
            {
                return false;
            }
        }
        return true;
    }

    public void SetPrimarySession(string sessionId)
    {
        lock (_stateLock)
        {
            _primarySessionId = sessionId;
        }
        _ = RefreshAllSessionsAsync();
    }

    public async Task<bool> PlayPauseSessionAsync(string sessionId)
    {
        TrackedSession? tracker;
        lock (_stateLock)
        {
            tracker = _trackedSessions.FirstOrDefault(t => t.Id == sessionId);
        }

        if (tracker != null)
        {
            try
            {
                var result = await tracker.Session.TryTogglePlayPauseAsync();
                _ = RefreshAllSessionsAsync();
                return result;
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, "Failed to toggle play/pause for session {SessionId}.", sessionId);
            }
        }

        return false;
    }

    public async Task<bool> NextSessionAsync(string sessionId)
    {
        TrackedSession? tracker;
        lock (_stateLock)
        {
            tracker = _trackedSessions.FirstOrDefault(t => t.Id == sessionId);
        }

        if (tracker != null)
        {
            try
            {
                var result = await tracker.Session.TrySkipNextAsync();
                _ = RefreshAllSessionsAsync();
                return result;
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, "Failed to skip next for session {SessionId}.", sessionId);
            }
        }

        return false;
    }

    public async Task<bool> PreviousSessionAsync(string sessionId)
    {
        TrackedSession? tracker;
        lock (_stateLock)
        {
            tracker = _trackedSessions.FirstOrDefault(t => t.Id == sessionId);
        }

        if (tracker != null)
        {
            try
            {
                var result = await tracker.Session.TrySkipPreviousAsync();
                _ = RefreshAllSessionsAsync();
                return result;
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, "Failed to skip previous for session {SessionId}.", sessionId);
            }
        }

        return false;
    }

    public async Task<bool> PlayPauseAsync()
    {
        var primaryId = PrimarySession?.SessionId;
        if (!string.IsNullOrEmpty(primaryId))
        {
            return await PlayPauseSessionAsync(primaryId);
        }

        return false;
    }

    public async Task<bool> NextAsync()
    {
        var primaryId = PrimarySession?.SessionId;
        if (!string.IsNullOrEmpty(primaryId))
        {
            return await NextSessionAsync(primaryId);
        }

        return false;
    }

    public async Task<bool> PreviousAsync()
    {
        var primaryId = PrimarySession?.SessionId;
        if (!string.IsNullOrEmpty(primaryId))
        {
            return await PreviousSessionAsync(primaryId);
        }

        return false;
    }

    public void Dispose()
    {
        _syncTimer?.Dispose();
        _syncTimer = null;

        lock (_stateLock)
        {
            foreach (var tracker in _trackedSessions)
            {
                tracker.Dispose();
            }
            _trackedSessions.Clear();

            if (_sessionManager != null)
            {
                _sessionManager.SessionsChanged -= OnSessionsChanged;
                _sessionManager.CurrentSessionChanged -= OnCurrentSessionChanged;
                _sessionManager = null;
            }
        }

        _refreshLock.Dispose();
    }

    private sealed class TrackedSession : IDisposable
    {
        public string Id { get; }
        public GlobalSystemMediaTransportControlsSession Session { get; }
        public MediaSessionInfo? CurrentInfo { get; set; }

        public event EventHandler? MediaPropertiesChanged;
        public event EventHandler? PlaybackInfoChanged;

        public TrackedSession(string id, GlobalSystemMediaTransportControlsSession session)
        {
            Id = id;
            Session = session;
            Session.MediaPropertiesChanged += OnMediaPropertiesChanged;
            Session.PlaybackInfoChanged += OnPlaybackInfoChanged;
        }

        private void OnMediaPropertiesChanged(GlobalSystemMediaTransportControlsSession sender, MediaPropertiesChangedEventArgs args)
        {
            MediaPropertiesChanged?.Invoke(this, EventArgs.Empty);
        }

        private void OnPlaybackInfoChanged(GlobalSystemMediaTransportControlsSession sender, PlaybackInfoChangedEventArgs args)
        {
            PlaybackInfoChanged?.Invoke(this, EventArgs.Empty);
        }

        public void Dispose()
        {
            try
            {
                Session.MediaPropertiesChanged -= OnMediaPropertiesChanged;
                Session.PlaybackInfoChanged -= OnPlaybackInfoChanged;
            }
            catch
            {
                // Ignore any teardown exceptions on COM objects
            }
        }
    }
}
