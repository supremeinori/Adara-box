using System;
using System.Collections.Generic;
using AdaraBox.Core.Common;

namespace AdaraBox.Core.Hotkeys;

public static class HotkeyParser
{
    public static readonly HotkeyDefinition DefaultTopbarHotkey =
        new(HotkeyModifier.Control | HotkeyModifier.Alt, 0x20, "Ctrl+Alt+Space");

    private static readonly HashSet<string> ForbiddenCombinations = new(StringComparer.OrdinalIgnoreCase)
    {
        "Ctrl+Alt+Delete",
        "Control+Alt+Delete",
        "Ctrl+Shift+Escape",
        "Control+Shift+Escape",
        "Win+L",
        "Windows+L",
        "Win+D",
        "Windows+D",
        "Alt+Tab",
        "Alt+F4"
    };

    public static OperationResult<HotkeyDefinition> Parse(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return OperationResult<HotkeyDefinition>.Fail("hotkey.empty", "Kombinasi hotkey tidak boleh kosong.");
        }

        var normalized = input.Trim();

        if (ForbiddenCombinations.Contains(normalized))
        {
            return OperationResult<HotkeyDefinition>.Fail("hotkey.reserved", "Kombinasi ini merupakan shortcut sistem Windows dan tidak boleh digunakan.");
        }

        var parts = normalized.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length < 2)
        {
            return OperationResult<HotkeyDefinition>.Fail("hotkey.no_modifier", "Hotkey harus memiliki minimal satu modifier (Ctrl, Alt, Shift, atau Win).");
        }

        var modifiers = HotkeyModifier.None;
        int? keyCode = null;
        string? keyName = null;

        for (int i = 0; i < parts.Length; i++)
        {
            var part = parts[i];

            if (part.Equals("Ctrl", StringComparison.OrdinalIgnoreCase) || part.Equals("Control", StringComparison.OrdinalIgnoreCase))
            {
                modifiers |= HotkeyModifier.Control;
            }
            else if (part.Equals("Alt", StringComparison.OrdinalIgnoreCase))
            {
                modifiers |= HotkeyModifier.Alt;
            }
            else if (part.Equals("Shift", StringComparison.OrdinalIgnoreCase))
            {
                modifiers |= HotkeyModifier.Shift;
            }
            else if (part.Equals("Win", StringComparison.OrdinalIgnoreCase) || part.Equals("Windows", StringComparison.OrdinalIgnoreCase))
            {
                modifiers |= HotkeyModifier.Windows;
            }
            else
            {
                // Key part
                if (keyCode != null)
                {
                    return OperationResult<HotkeyDefinition>.Fail("hotkey.multiple_keys", "Hanya satu tombol utama yang diizinkan dalam kombinasi.");
                }

                var parsedKey = ParseVirtualKeyCode(part);
                if (parsedKey == null)
                {
                    return OperationResult<HotkeyDefinition>.Fail("hotkey.invalid_key", $"Tombol '{part}' tidak didukung sebagai hotkey.");
                }

                keyCode = parsedKey.Value.Code;
                keyName = parsedKey.Value.Name;
            }
        }

        if (modifiers == HotkeyModifier.None)
        {
            return OperationResult<HotkeyDefinition>.Fail("hotkey.no_modifier", "Hotkey harus memiliki minimal satu modifier (Ctrl, Alt, Shift, atau Win).");
        }

        if (keyCode == null)
        {
            return OperationResult<HotkeyDefinition>.Fail("hotkey.missing_key", "Hotkey harus memiliki tombol utama selain modifier.");
        }

        var displayText = FormatDisplayText(modifiers, keyName!);
        return OperationResult<HotkeyDefinition>.Ok(new HotkeyDefinition(modifiers, keyCode.Value, displayText));
    }

    private static (int Code, string Name)? ParseVirtualKeyCode(string key)
    {
        if (key.Length == 1)
        {
            char c = char.ToUpperInvariant(key[0]);
            if ((c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9'))
            {
                return (c, c.ToString());
            }
        }

        if (key.StartsWith("F", StringComparison.OrdinalIgnoreCase) &&
            int.TryParse(key.Substring(1), out int fNum) && fNum >= 1 && fNum <= 12)
        {
            return (0x70 + (fNum - 1), $"F{fNum}");
        }

        if (key.Equals("Space", StringComparison.OrdinalIgnoreCase)) return (0x20, "Space");
        if (key.Equals("Tab", StringComparison.OrdinalIgnoreCase)) return (0x09, "Tab");
        if (key.Equals("Enter", StringComparison.OrdinalIgnoreCase) || key.Equals("Return", StringComparison.OrdinalIgnoreCase)) return (0x0D, "Enter");
        if (key.Equals("Esc", StringComparison.OrdinalIgnoreCase) || key.Equals("Escape", StringComparison.OrdinalIgnoreCase)) return (0x1B, "Esc");

        return null;
    }

    private static string FormatDisplayText(HotkeyModifier modifiers, string keyName)
    {
        var parts = new List<string>();
        if (modifiers.HasFlag(HotkeyModifier.Control)) parts.Add("Ctrl");
        if (modifiers.HasFlag(HotkeyModifier.Alt)) parts.Add("Alt");
        if (modifiers.HasFlag(HotkeyModifier.Shift)) parts.Add("Shift");
        if (modifiers.HasFlag(HotkeyModifier.Windows)) parts.Add("Win");
        parts.Add(keyName);
        return string.Join("+", parts);
    }
}
