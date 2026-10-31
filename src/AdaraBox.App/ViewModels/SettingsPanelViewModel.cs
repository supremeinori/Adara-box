using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AdaraBox.App.Resources;
using AdaraBox.Core.Hotkeys;
using AdaraBox.Core.Settings;
using AdaraBox.Core.Visibility;
using AdaraBox.Platform.Hotkeys;
using AdaraBox.Platform.Startup;

namespace AdaraBox.App.ViewModels;

public sealed partial class SettingsPanelViewModel : ObservableObject
{
    private readonly ITopBarVisibilityService _visibilityService;
    private readonly IAutoStartService _autoStartService;
    private readonly IGlobalHotkeyService _hotkeyService;
    private readonly ISettingsStore _settingsStore;

    [ObservableProperty]
    private string _hotkeyText = HotkeyParser.DefaultTopbarHotkey.DisplayText;

    [ObservableProperty]
    private string? _hotkeyError;

    public bool HasHotkeyError => !string.IsNullOrEmpty(HotkeyError);

    public SettingsPanelViewModel(
        ITopBarVisibilityService visibilityService,
        IAutoStartService autoStartService,
        IGlobalHotkeyService hotkeyService,
        ISettingsStore settingsStore)
    {
        _visibilityService = visibilityService;
        _autoStartService = autoStartService;
        _hotkeyService = hotkeyService;
        _settingsStore = settingsStore;

        _visibilityService.ModeChanged += (s, mode) =>
        {
            OnPropertyChanged(nameof(IsHideTopBarEnabled));
        };

        _ = LoadSettingsAsync();
    }

    private async Task LoadSettingsAsync()
    {
        var savedHotkey = await _settingsStore.GetAsync(SettingKeys.GlobalHotkey);
        if (!string.IsNullOrEmpty(savedHotkey))
        {
            HotkeyText = savedHotkey;
        }
    }

    public string Title => UiText.SettingsTitle;
    public string HideTopBarLabel => UiText.SettingsHideTopBar;
    public string HideTopBarDescription => UiText.SettingsHideTopBarDescription;
    public string AutoStartLabel => UiText.SettingsAutoStart;
    public string AutoStartDescription => UiText.SettingsAutoStartDescription;
    public string GlobalHotkeyLabel => UiText.SettingsGlobalHotkey;
    public string GlobalHotkeyDescription => UiText.SettingsGlobalHotkeyDescription;
    public string ApplyHotkeyButtonText => UiText.SaveHotkeyButton;
    public string VersionLabel => string.Format(UiText.AppVersionLabel, "1.0.0-rc (MVP Complete)");

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

    public bool IsAutoStartEnabled
    {
        get => _autoStartService.IsAutoStartEnabled();
        set
        {
            if (_autoStartService.IsAutoStartEnabled() != value)
            {
                _autoStartService.SetAutoStartEnabled(value);
                _ = _settingsStore.SetAsync(SettingKeys.RunOnStartup, value ? "True" : "False");
                OnPropertyChanged();
            }
        }
    }

    [RelayCommand]
    public async Task ApplyHotkeyAsync()
    {
        HotkeyError = null;
        var parseResult = HotkeyParser.Parse(HotkeyText);
        if (!parseResult.Success)
        {
            HotkeyError = parseResult.Error?.Message ?? "Format hotkey tidak valid.";
            OnPropertyChanged(nameof(HasHotkeyError));
            return;
        }

        var hotkey = parseResult.Value!;
        var registered = _hotkeyService.Register(hotkey);
        if (!registered)
        {
            HotkeyError = _hotkeyService.LastRegistrationError ?? "Gagal mendaftarkan hotkey.";
            OnPropertyChanged(nameof(HasHotkeyError));
            return;
        }

        HotkeyText = hotkey.DisplayText;
        await _settingsStore.SetAsync(SettingKeys.GlobalHotkey, hotkey.DisplayText);
        OnPropertyChanged(nameof(HasHotkeyError));
    }
}
