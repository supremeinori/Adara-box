using System;

namespace AdaraBox.Core.Visibility;

/// <summary>
/// Pure domain state machine governing Top Bar visibility, hover triggers, and panel interactions.
/// Independent of WPF and Win32, completely unit-testable.
/// </summary>
public sealed class TopBarVisibilityStateMachine
{
    private TopBarVisibilityMode _mode;
    private TopBarDisplayState _displayState;
    private bool _isPanelOpen;
    private bool _isPointerOverTopBar;
    private bool _isPointerOverTrigger;

    public event EventHandler<TopBarDisplayState>? DisplayStateChanged;
    public event EventHandler<TopBarVisibilityMode>? ModeChanged;
    public event EventHandler? StartDwellTimerRequested;
    public event EventHandler? CancelDwellTimerRequested;
    public event EventHandler? StartHideTimerRequested;
    public event EventHandler? CancelHideTimerRequested;

    public TopBarVisibilityStateMachine(TopBarVisibilityMode initialMode = TopBarVisibilityMode.AlwaysVisible)
    {
        _mode = initialMode;
        _displayState = initialMode == TopBarVisibilityMode.AlwaysVisible
            ? TopBarDisplayState.Shown
            : TopBarDisplayState.Hidden;
    }

    public TopBarVisibilityMode Mode => _mode;
    public TopBarDisplayState DisplayState => _displayState;
    public bool IsPanelOpen => _isPanelOpen;
    public bool IsPointerOverTopBar => _isPointerOverTopBar;
    public bool IsPointerOverTrigger => _isPointerOverTrigger;

    public bool IsTopBarVisible => _displayState == TopBarDisplayState.Shown || _displayState == TopBarDisplayState.TemporarilyShown;
    public bool IsTriggerStripActive => _mode == TopBarVisibilityMode.Hidden;

    public void SetMode(TopBarVisibilityMode newMode)
    {
        if (_mode == newMode) return;

        _mode = newMode;
        ModeChanged?.Invoke(this, _mode);

        if (_mode == TopBarVisibilityMode.AlwaysVisible)
        {
            CancelDwellTimerRequested?.Invoke(this, EventArgs.Empty);
            CancelHideTimerRequested?.Invoke(this, EventArgs.Empty);
            SetDisplayState(TopBarDisplayState.Shown);
        }
        else
        {
            // Switched to Hidden
            if (_isPanelOpen || _isPointerOverTopBar)
            {
                SetDisplayState(TopBarDisplayState.TemporarilyShown);
            }
            else
            {
                SetDisplayState(TopBarDisplayState.Hidden);
            }
        }
    }

    public void SetPanelOpen(bool isOpen)
    {
        _isPanelOpen = isOpen;

        if (_isPanelOpen)
        {
            CancelHideTimerRequested?.Invoke(this, EventArgs.Empty);
            if (!IsTopBarVisible)
            {
                SetDisplayState(_mode == TopBarVisibilityMode.AlwaysVisible ? TopBarDisplayState.Shown : TopBarDisplayState.TemporarilyShown);
            }
        }
        else if (_mode == TopBarVisibilityMode.Hidden && !_isPointerOverTopBar && !_isPointerOverTrigger)
        {
            StartHideTimerRequested?.Invoke(this, EventArgs.Empty);
        }
    }

    public void OnPointerEnteredTrigger()
    {
        _isPointerOverTrigger = true;
        if (_mode == TopBarVisibilityMode.Hidden && _displayState == TopBarDisplayState.Hidden)
        {
            CancelHideTimerRequested?.Invoke(this, EventArgs.Empty);
            StartDwellTimerRequested?.Invoke(this, EventArgs.Empty);
        }
    }

    public void OnPointerLeftTrigger()
    {
        _isPointerOverTrigger = false;
        CancelDwellTimerRequested?.Invoke(this, EventArgs.Empty);

        if (_mode == TopBarVisibilityMode.Hidden && _displayState == TopBarDisplayState.TemporarilyShown && !_isPointerOverTopBar && !_isPanelOpen)
        {
            StartHideTimerRequested?.Invoke(this, EventArgs.Empty);
        }
    }

    public void OnDwellTimerElapsed()
    {
        if (_mode == TopBarVisibilityMode.Hidden && _isPointerOverTrigger)
        {
            SetDisplayState(TopBarDisplayState.TemporarilyShown);
        }
    }

    public void OnPointerEnteredTopBar()
    {
        _isPointerOverTopBar = true;
        CancelHideTimerRequested?.Invoke(this, EventArgs.Empty);
    }

    public void OnPointerLeftTopBar()
    {
        _isPointerOverTopBar = false;

        if (_mode == TopBarVisibilityMode.Hidden && !_isPanelOpen)
        {
            StartHideTimerRequested?.Invoke(this, EventArgs.Empty);
        }
    }

    public void OnHideTimerElapsed()
    {
        if (_mode == TopBarVisibilityMode.Hidden && !_isPanelOpen && !_isPointerOverTopBar && !_isPointerOverTrigger)
        {
            SetDisplayState(TopBarDisplayState.Hidden);
        }
    }

    private void SetDisplayState(TopBarDisplayState newState)
    {
        if (_displayState != newState)
        {
            _displayState = newState;
            DisplayStateChanged?.Invoke(this, _displayState);
        }
    }
}
