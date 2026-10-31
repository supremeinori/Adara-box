using System.Threading.Tasks;
using AdaraBox.App.ViewModels;
using AdaraBox.Core.Hotkeys;
using AdaraBox.Core.Settings;
using AdaraBox.Core.Visibility;
using AdaraBox.Platform.Hotkeys;
using AdaraBox.Platform.Startup;
using NSubstitute;
using Xunit;

namespace AdaraBox.App.Tests;

public class SettingsPanelViewModelTests
{
    private readonly ITopBarVisibilityService _visibilityService = Substitute.For<ITopBarVisibilityService>();
    private readonly IAutoStartService _autoStartService = Substitute.For<IAutoStartService>();
    private readonly IGlobalHotkeyService _hotkeyService = Substitute.For<IGlobalHotkeyService>();
    private readonly ISettingsStore _settingsStore = Substitute.For<ISettingsStore>();

    private SettingsPanelViewModel CreateViewModel()
    {
        return new SettingsPanelViewModel(
            _visibilityService,
            _autoStartService,
            _hotkeyService,
            _settingsStore);
    }

    [Fact]
    public void IsAutoStartEnabled_TogglesServiceAndSavesSetting()
    {
        _autoStartService.IsAutoStartEnabled().Returns(false);
        var vm = CreateViewModel();

        vm.IsAutoStartEnabled = true;

        _autoStartService.Received(1).SetAutoStartEnabled(true);
        _settingsStore.Received(1).SetAsync(SettingKeys.RunOnStartup, "True");
    }

    [Fact]
    public async Task ApplyHotkeyAsync_WithInvalidHotkey_SetsErrorAndDoesNotRegister()
    {
        var vm = CreateViewModel();
        vm.HotkeyText = "Space"; // Invalid: no modifier

        await vm.ApplyHotkeyAsync();

        Assert.True(vm.HasHotkeyError);
        Assert.NotNull(vm.HotkeyError);
        _hotkeyService.DidNotReceive().Register(Arg.Any<HotkeyDefinition>());
        await _settingsStore.DidNotReceive().SetAsync(SettingKeys.GlobalHotkey, Arg.Any<string>());
    }

    [Fact]
    public async Task ApplyHotkeyAsync_WhenRegistrationFails_SetsErrorMessage()
    {
        var vm = CreateViewModel();
        vm.HotkeyText = "Ctrl+Alt+P";
        _hotkeyService.Register(Arg.Any<HotkeyDefinition>()).Returns(false);
        _hotkeyService.LastRegistrationError.Returns("Hotkey sudah dipakai aplikasi lain.");

        await vm.ApplyHotkeyAsync();

        Assert.True(vm.HasHotkeyError);
        Assert.Equal("Hotkey sudah dipakai aplikasi lain.", vm.HotkeyError);
        await _settingsStore.DidNotReceive().SetAsync(SettingKeys.GlobalHotkey, Arg.Any<string>());
    }

    [Fact]
    public async Task ApplyHotkeyAsync_WhenSuccessful_RegistersAndPersists()
    {
        var vm = CreateViewModel();
        vm.HotkeyText = "Ctrl+Shift+F12";
        _hotkeyService.Register(Arg.Any<HotkeyDefinition>()).Returns(true);

        await vm.ApplyHotkeyAsync();

        Assert.False(vm.HasHotkeyError);
        Assert.Null(vm.HotkeyError);
        _hotkeyService.Received(1).Register(Arg.Is<HotkeyDefinition>(h => h.DisplayText == "Ctrl+Shift+F12" && (h.Modifiers & HotkeyModifier.Control) != 0 && (h.Modifiers & HotkeyModifier.Shift) != 0));
        await _settingsStore.Received(1).SetAsync(SettingKeys.GlobalHotkey, "Ctrl+Shift+F12");
    }
}
