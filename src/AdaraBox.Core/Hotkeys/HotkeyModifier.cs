using System;

namespace AdaraBox.Core.Hotkeys;

[Flags]
public enum HotkeyModifier
{
    None = 0,
    Alt = 1,
    Control = 2,
    Shift = 4,
    Windows = 8
}
