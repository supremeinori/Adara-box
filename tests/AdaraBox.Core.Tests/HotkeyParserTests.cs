using AdaraBox.Core.Hotkeys;
using Xunit;

namespace AdaraBox.Core.Tests;

public class HotkeyParserTests
{
    [Theory]
    [InlineData("Ctrl+Alt+Space", HotkeyModifier.Control | HotkeyModifier.Alt, 0x20)]
    [InlineData("Ctrl+Shift+A", HotkeyModifier.Control | HotkeyModifier.Shift, (int)'A')]
    [InlineData("Alt+F4", HotkeyModifier.Alt, 0x73, true)] // Reserved system shortcut
    [InlineData("Win+Alt+F1", HotkeyModifier.Windows | HotkeyModifier.Alt, 0x70)]
    public void Parse_ValidAndReserved_ReturnsExpectedResult(string input, HotkeyModifier expectedMod, int expectedKey, bool shouldFailReserved = false)
    {
        var result = HotkeyParser.Parse(input);

        if (shouldFailReserved)
        {
            Assert.False(result.Success);
            Assert.Equal("hotkey.reserved", result.Error?.Code);
        }
        else
        {
            Assert.True(result.Success);
            Assert.NotNull(result.Value);
            Assert.Equal(expectedMod, result.Value.Modifiers);
            Assert.Equal(expectedKey, result.Value.KeyCode);
        }
    }

    [Theory]
    [InlineData("Space")]
    [InlineData("A")]
    [InlineData("F5")]
    public void Parse_MissingModifier_ReturnsError(string input)
    {
        var result = HotkeyParser.Parse(input);

        Assert.False(result.Success);
        Assert.Equal("hotkey.no_modifier", result.Error?.Code);
    }

    [Theory]
    [InlineData("Ctrl+Alt+Delete")]
    [InlineData("Win+L")]
    [InlineData("Win+D")]
    [InlineData("Alt+Tab")]
    [InlineData("Ctrl+Shift+Escape")]
    public void Parse_ForbiddenSystemShortcut_ReturnsError(string input)
    {
        var result = HotkeyParser.Parse(input);

        Assert.False(result.Success);
        Assert.Equal("hotkey.reserved", result.Error?.Code);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Parse_EmptyInput_ReturnsError(string? input)
    {
        var result = HotkeyParser.Parse(input);

        Assert.False(result.Success);
        Assert.Equal("hotkey.empty", result.Error?.Code);
    }
}
