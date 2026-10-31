using System;
using AdaraBox.Core.Hotkeys;

namespace AdaraBox.Platform.Hotkeys;

public interface IGlobalHotkeyService : IDisposable
{
    bool IsRegistered { get; }
    string? LastRegistrationError { get; }
    HotkeyDefinition? CurrentHotkey { get; }

    event EventHandler? HotkeyPressed;

    bool Register(HotkeyDefinition hotkey);
    void Unregister();
}
