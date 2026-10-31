namespace AdaraBox.Core.Hotkeys;

public sealed record HotkeyDefinition(HotkeyModifier Modifiers, int KeyCode, string DisplayText);
