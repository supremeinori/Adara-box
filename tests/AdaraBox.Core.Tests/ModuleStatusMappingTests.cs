using AdaraBox.Core.Common;
using Xunit;

namespace AdaraBox.Core.Tests;

public class ModuleStatusMappingTests
{
    [Theory]
    [InlineData(ModuleStatus.InDevelopment, "Masih dalam pengembangan")]
    [InlineData(ModuleStatus.NotAvailable, "Belum tersedia")]
    [InlineData(ModuleStatus.Unsupported, "Tidak didukung pada perangkat ini")]
    [InlineData(ModuleStatus.Error, "Terjadi kesalahan")]
    [InlineData(ModuleStatus.Available, "Aktif")]
    public void Status_Maps_To_Correct_Indonesian_Display_Text(ModuleStatus status, string expectedText)
    {
        var text = status.ToDisplayText();
        Assert.Equal(expectedText, text);
    }

    [Fact]
    public void Error_Status_Is_Distinctly_Different_From_InDevelopment()
    {
        // Enforcing FR-PL-03: Status pengembangan ≠ error
        var inDevText = ModuleStatus.InDevelopment.ToDisplayText();
        var errorText = ModuleStatus.Error.ToDisplayText();

        Assert.NotEqual(inDevText, errorText);
    }

    [Fact]
    public void Custom_Message_Overrides_Default_Status_Text()
    {
        var custom = "Gagal membaca berkas sistem";
        var text = ModuleStatus.Error.ToDisplayText(custom);
        Assert.Equal(custom, text);
    }
}
