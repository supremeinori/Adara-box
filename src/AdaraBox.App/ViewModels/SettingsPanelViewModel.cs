using CommunityToolkit.Mvvm.ComponentModel;
using AdaraBox.App.Resources;
using AdaraBox.Core.Visibility;

namespace AdaraBox.App.ViewModels;

public sealed class SettingsPanelViewModel : ObservableObject
{
    private readonly ITopBarVisibilityService _visibilityService;

    public SettingsPanelViewModel(ITopBarVisibilityService visibilityService)
    {
        _visibilityService = visibilityService;
        _visibilityService.ModeChanged += (s, mode) =>
        {
            OnPropertyChanged(nameof(IsHideTopBarEnabled));
        };
    }

    public string Title => UiText.SettingsTitle;
    public string HideTopBarLabel => UiText.SettingsHideTopBar;
    public string HideTopBarDescription => UiText.SettingsHideTopBarDescription;
    public string VersionLabel => string.Format(UiText.AppVersionLabel, "0.1.0 (MVP Shell)");

    public bool IsHideTopBarEnabled
    {
        get => _visibilityService.CurrentMode == TopBarVisibilityMode.Hidden;
        set
        {
            var targetMode = value ? TopBarVisibilityMode.Hidden : TopBarVisibilityMode.AlwaysVisible;
            if (_visibilityService.CurrentMode != targetMode)
            {
                _visibilityService.SetMode(targetMode);
                OnPropertyChanged();
            }
        }
    }
}
