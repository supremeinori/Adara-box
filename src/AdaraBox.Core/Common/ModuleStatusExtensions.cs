namespace AdaraBox.Core.Common;

public static class ModuleStatusExtensions
{
    public static string ToDisplayText(this ModuleStatus status, string? customMessage = null)
    {
        return status switch
        {
            ModuleStatus.InDevelopment => customMessage ?? "Masih dalam pengembangan",
            ModuleStatus.NotAvailable => customMessage ?? "Belum tersedia",
            ModuleStatus.Unsupported => customMessage ?? "Tidak didukung pada perangkat ini",
            ModuleStatus.Error => customMessage ?? "Terjadi kesalahan",
            _ => "Aktif"
        };
    }
}
