using System.Globalization;
using AdaraBox.Core.Stats;
using Xunit;

namespace AdaraBox.Core.Tests.Stats;

public class SizeFormatterTests
{
    private readonly CultureInfo _inv = CultureInfo.InvariantCulture;

    [Theory]
    [InlineData(0, "0 B")]
    [InlineData(-10, "0 B")]
    [InlineData(500, "500 B")]
    [InlineData(1024, "1 KB")]
    [InlineData(1536, "2 KB")]
    [InlineData(1048576, "1.0 MB")]
    [InlineData(1572864, "1.5 MB")]
    [InlineData(1073741824, "1.0 GB")]
    [InlineData(1610612736, "1.5 GB")]
    public void FormatBytes_ProducesExpectedString(long bytes, string expected)
    {
        var result = SizeFormatter.FormatBytes(bytes, _inv);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void FormatPercent_NullValue_ReturnsTidakTersedia()
    {
        var result = SizeFormatter.FormatPercent(null, _inv);
        Assert.Equal("Tidak tersedia", result);
    }

    [Theory]
    [InlineData(0.0, "0%")]
    [InlineData(50.4, "50%")]
    [InlineData(99.6, "100%")]
    public void FormatPercent_ValidValue_ReturnsFormattedPercent(double percent, string expected)
    {
        var result = SizeFormatter.FormatPercent(percent, _inv);
        Assert.Equal(expected, result);
    }
}
