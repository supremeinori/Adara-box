using System;
using AdaraBox.Core.Visibility;
using Xunit;

namespace AdaraBox.Core.Tests;

public class VisibilityStateMachineTests
{
    [Fact]
    public void InitialState_AlwaysVisible_IsShown_And_TriggerStripInactive()
    {
        var sm = new TopBarVisibilityStateMachine(TopBarVisibilityMode.AlwaysVisible);
        Assert.Equal(TopBarVisibilityMode.AlwaysVisible, sm.Mode);
        Assert.Equal(TopBarDisplayState.Shown, sm.DisplayState);
        Assert.True(sm.IsTopBarVisible);
        Assert.False(sm.IsTriggerStripActive);
    }

    [Fact]
    public void InitialState_Hidden_IsHidden_And_TriggerStripActive()
    {
        var sm = new TopBarVisibilityStateMachine(TopBarVisibilityMode.Hidden);
        Assert.Equal(TopBarVisibilityMode.Hidden, sm.Mode);
        Assert.Equal(TopBarDisplayState.Hidden, sm.DisplayState);
        Assert.False(sm.IsTopBarVisible);
        Assert.True(sm.IsTriggerStripActive);
    }

    [Fact]
    public void HoverFlow_Dwell_ShowsBar_And_HideTimer_HidesBar()
    {
        var sm = new TopBarVisibilityStateMachine(TopBarVisibilityMode.Hidden);

        bool dwellRequested = false;
        sm.StartDwellTimerRequested += (s, e) => dwellRequested = true;

        // 1. Pointer enters trigger
        sm.OnPointerEnteredTrigger();
        Assert.True(dwellRequested);
        Assert.Equal(TopBarDisplayState.Hidden, sm.DisplayState);

        // 2. Dwell elapsed
        sm.OnDwellTimerElapsed();
        Assert.Equal(TopBarDisplayState.TemporarilyShown, sm.DisplayState);
        Assert.True(sm.IsTopBarVisible);

        // 3. Pointer leaves trigger and moves to Top Bar
        sm.OnPointerLeftTrigger();
        sm.OnPointerEnteredTopBar();
        Assert.Equal(TopBarDisplayState.TemporarilyShown, sm.DisplayState);

        // 4. Pointer leaves Top Bar
        bool hideRequested = false;
        sm.StartHideTimerRequested += (s, e) => hideRequested = true;
        sm.OnPointerLeftTopBar();
        Assert.True(hideRequested);

        // 5. Hide elapsed
        sm.OnHideTimerElapsed();
        Assert.Equal(TopBarDisplayState.Hidden, sm.DisplayState);
        Assert.False(sm.IsTopBarVisible);
    }

    [Fact]
    public void RapidSwipe_Cancels_Dwell_Before_Showing()
    {
        var sm = new TopBarVisibilityStateMachine(TopBarVisibilityMode.Hidden);

        bool cancelDwellCalled = false;
        sm.CancelDwellTimerRequested += (s, e) => cancelDwellCalled = true;

        sm.OnPointerEnteredTrigger();
        sm.OnPointerLeftTrigger(); // quick swipe

        Assert.True(cancelDwellCalled);
        Assert.Equal(TopBarDisplayState.Hidden, sm.DisplayState);
    }

    [Fact]
    public void OpenPanel_Prevents_TopBar_From_Hiding()
    {
        var sm = new TopBarVisibilityStateMachine(TopBarVisibilityMode.Hidden);

        sm.OnPointerEnteredTrigger();
        sm.OnDwellTimerElapsed();
        sm.OnPointerLeftTrigger();
        Assert.True(sm.IsTopBarVisible);

        // User opens a panel
        sm.SetPanelOpen(true);
        Assert.True(sm.IsPanelOpen);

        // Pointer enters and then leaves bar
        sm.OnPointerEnteredTopBar();
        sm.OnPointerLeftTopBar();

        // Even if hide timer tick arrives, bar must not hide while panel is open
        sm.OnHideTimerElapsed();
        Assert.True(sm.IsTopBarVisible);
        Assert.Equal(TopBarDisplayState.TemporarilyShown, sm.DisplayState);

        // Closing panel allows hide timer to work
        sm.SetPanelOpen(false);
        sm.OnHideTimerElapsed();
        Assert.Equal(TopBarDisplayState.Hidden, sm.DisplayState);
    }
}
