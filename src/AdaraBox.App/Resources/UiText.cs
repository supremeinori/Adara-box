namespace AdaraBox.App.Resources;

/// <summary>
/// Centralized user-facing Indonesian text strings for Adara Box.
/// Adheres to Master Guide §5 (UI texts in Indonesian, single source of truth).
/// </summary>
public static class UiText
{
    public const string AppName = "Adara Box";

    // Module Status Texts
    public const string StatusInDevelopment = "Masih dalam pengembangan";
    public const string StatusNotAvailable = "Belum tersedia";
    public const string StatusUnsupported = "Tidak didukung pada perangkat ini";
    public const string StatusError = "Terjadi kesalahan";

    // Panel & Component Texts
    public const string WorkspaceEmpty = "Belum ada workspace.";
    public const string CloseButtonTooltip = "Tutup panel (Esc)";

    // Settings Panel Texts
    public const string SettingsTitle = "Pengaturan";
    public const string SettingsHideTopBar = "Sembunyikan top bar otomatis";
    public const string SettingsHideTopBarDescription = "Top bar hanya muncul saat kursor diarahkan ke tepi atas layar.";
    public const string AppVersionLabel = "Versi: {0}";

    // Tray Menu
    public const string TrayShow = "Tampilkan Top Bar";
    public const string TrayHide = "Sembunyikan Top Bar";
    public const string TrayExit = "Keluar";

    // Error Dialogs
    public const string UnhandledExceptionTitle = "Kesalahan Tak Terduga";
    public const string UnhandledExceptionMessage = "Terjadi kesalahan pada aplikasi Adara Box. Rincian telah dicatat di file log.";
}
