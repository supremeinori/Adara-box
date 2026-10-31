using System.ComponentModel;
using System.Diagnostics;
using Xunit;

namespace AdaraBox.Platform.Tests;

public class LauncherTests
{
    [Fact]
    public void Launching_NonExistentFile_Returns_NativeErrorCode_2()
    {
        var nonExistentPath = Path.Combine(Path.GetTempPath(), "NonExistent_" + Guid.NewGuid().ToString("N") + ".xyz");
        var psi = new ProcessStartInfo(nonExistentPath)
        {
            UseShellExecute = true
        };

        var ex = Assert.Throws<Win32Exception>(() => Process.Start(psi));
        Assert.Equal(2, ex.NativeErrorCode); // ERROR_FILE_NOT_FOUND
    }

    [Theory]
    [InlineData(2, "ResourceNotFound", "Target tidak ditemukan")]
    [InlineData(5, "AccessDenied", "Akses ke resource ditolak")]
    [InlineData(1155, "NoAssociation", "Tidak ada aplikasi default yang terhubung")]
    [InlineData(1223, "CancelledByUser", "Tindakan dibatalkan oleh pengguna")]
    public void Win32ErrorMapper_Maps_Standard_Codes_Accurately(int errorCode, string expectedCode, string expectedPrefix)
    {
        // Testing error map logic that will be centralized in Platform/Core for M04
        var (code, message) = MapWin32Error(errorCode);
        Assert.Equal(expectedCode, code);
        Assert.StartsWith(expectedPrefix, message);
    }

    private static (string Code, string Message) MapWin32Error(int nativeErrorCode) => nativeErrorCode switch
    {
        2 => ("ResourceNotFound", "Target tidak ditemukan"),
        5 => ("AccessDenied", "Akses ke resource ditolak (Permission Denied)"),
        1155 => ("NoAssociation", "Tidak ada aplikasi default yang terhubung dengan file ini"),
        1223 => ("CancelledByUser", "Tindakan dibatalkan oleh pengguna (UAC/Prompt)"),
        _ => ("LaunchFailed", $"Gagal membuka resource (Win32 Error: {nativeErrorCode})")
    };
}
