using System;

namespace AdaraBox.Core.Visibility;

public interface ITopBarVisibilityService
{
    TopBarVisibilityMode CurrentMode { get; }
    TopBarDisplayState CurrentDisplayState { get; }
    bool IsTopBarVisible { get; }
    bool IsTriggerStripActive { get; }

    event EventHandler<TopBarVisibilityMode>? ModeChanged;
    event EventHandler<TopBarDisplayState>? DisplayStateChanged;

    void SetMode(TopBarVisibilityMode mode);
    void ToggleMode();
    void SetPanelOpen(bool isOpen);

    void NotifyPointerEnteredTrigger();
    void NotifyPointerLeftTrigger();
    void NotifyPointerEnteredTopBar();
    void NotifyPointerLeftTopBar();
}
