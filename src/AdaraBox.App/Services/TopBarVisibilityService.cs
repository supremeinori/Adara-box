using System;
using System.Windows.Threading;
using AdaraBox.Core.Common;
using AdaraBox.Core.Settings;
using AdaraBox.Core.Visibility;

namespace AdaraBox.App.Services;

public sealed class TopBarVisibilityService : ITopBarVisibilityService
{
    private readonly TopBarVisibilityStateMachine _stateMachine;
    private readonly ISettingsStore _settingsStore;
    private readonly DispatcherTimer _dwellTimer;
    private readonly DispatcherTimer _hideTimer;

    public event EventHandler<TopBarVisibilityMode>? ModeChanged;
    public event EventHandler<TopBarDisplayState>? DisplayStateChanged;

    public TopBarVisibilityService(ISettingsStore settingsStore, TopBarVisibilityMode initialMode = TopBarVisibilityMode.AlwaysVisible)
    {
        _settingsStore = settingsStore;
        _stateMachine = new TopBarVisibilityStateMachine(initialMode);

        _dwellTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(UiConstants.HoverDwellTimeMs) };
        _dwellTimer.Tick += (s, e) =>
        {
            _dwellTimer.Stop();
            _stateMachine.OnDwellTimerElapsed();
        };

        _hideTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(UiConstants.HoverHideDelayMs) };
        _hideTimer.Tick += (s, e) =>
        {
            _hideTimer.Stop();
            _stateMachine.OnHideTimerElapsed();
        };

        _stateMachine.StartDwellTimerRequested += (s, e) =>
        {
            _hideTimer.Stop();
            _dwellTimer.Start();
        };

        _stateMachine.CancelDwellTimerRequested += (s, e) =>
        {
            _dwellTimer.Stop();
        };

        _stateMachine.StartHideTimerRequested += (s, e) =>
        {
            _dwellTimer.Stop();
            _hideTimer.Start();
        };

        _stateMachine.CancelHideTimerRequested += (s, e) =>
        {
            _hideTimer.Stop();
        };

        _stateMachine.ModeChanged += (s, mode) =>
        {
            ModeChanged?.Invoke(this, mode);
            _ = _settingsStore.SetAsync(SettingKeys.TopBarVisibility, mode.ToString());
        };

        _stateMachine.DisplayStateChanged += (s, state) =>
        {
            DisplayStateChanged?.Invoke(this, state);
        };
    }

    public TopBarVisibilityMode CurrentMode => _stateMachine.Mode;
    public TopBarDisplayState CurrentDisplayState => _stateMachine.DisplayState;
    public bool IsTopBarVisible => _stateMachine.IsTopBarVisible;
    public bool IsTriggerStripActive => _stateMachine.IsTriggerStripActive;

    public void SetMode(TopBarVisibilityMode mode) => _stateMachine.SetMode(mode);

    public void ToggleMode()
    {
        var newMode = CurrentMode == TopBarVisibilityMode.AlwaysVisible
            ? TopBarVisibilityMode.Hidden
            : TopBarVisibilityMode.AlwaysVisible;
        SetMode(newMode);
    }

    public void SetPanelOpen(bool isOpen) => _stateMachine.SetPanelOpen(isOpen);

    public void NotifyPointerEnteredTrigger() => _stateMachine.OnPointerEnteredTrigger();

    public void NotifyPointerLeftTrigger() => _stateMachine.OnPointerLeftTrigger();

    public void NotifyPointerEnteredTopBar() => _stateMachine.OnPointerEnteredTopBar();

    public void NotifyPointerLeftTopBar() => _stateMachine.OnPointerLeftTopBar();
}
