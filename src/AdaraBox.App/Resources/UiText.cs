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
    public const string WorkspaceEmptyDescription = "Buat workspace pertamamu untuk mulai mengelompokkan file, aplikasi, dan tautan.";
    public const string CreateFirstWorkspace = "Buat Workspace";
    public const string CloseButtonTooltip = "Tutup panel (Esc)";

    // Workspace Management Texts
    public const string WorkspaceModuleDisplayName = "Workspace";
    public const string AddWorkspaceButton = "+ Workspace";
    public const string WorkspaceListTitle = "Daftar Workspace";
    public const string CreateWorkspaceTitle = "Buat Workspace Baru";
    public const string RenameWorkspaceTitle = "Ganti Nama Workspace";
    public const string WorkspaceNamePlaceholder = "Nama workspace...";
    public const string WorkspaceNameLabel = "Nama Workspace";
    public const string Save = "Simpan";
    public const string Cancel = "Batal";
    public const string Back = "Kembali";
    public const string Rename = "Ganti Nama";
    public const string Delete = "Hapus";
    public const string WorkspaceDetailResourcesPlaceholder = "Resource untuk workspace ini belum ada.";
    public const string WorkspaceDetailResourcesSubtext = "Pengelolaan file, folder, aplikasi, dan tautan akan hadir di Modul M03.";

    // Workspace Validation & Error Messages
    public const string ErrorWorkspaceNameEmpty = "Nama workspace tidak boleh kosong.";
    public const string ErrorWorkspaceNameTooLong = "Nama workspace maksimal 60 karakter.";
    public const string ErrorWorkspaceNameInvalid = "Nama workspace mengandung karakter kontrol yang tidak diizinkan.";
    public const string ErrorWorkspaceNotFound = "Workspace tidak ditemukan.";
    public const string ErrorUnknown = "Terjadi kesalahan yang tidak terduga.";

    // Confirmation Dialogs
    public const string DeleteWorkspaceTitle = "Hapus Workspace";
    public const string DeleteWorkspaceConfirm = "Apakah Anda yakin ingin menghapus workspace \"{0}\"?\nJumlah resource: {1}.\n\nHanya menghapus workspace dari Adara Box. File, folder, aplikasi, dan tautan aslinya tidak terpengaruh.";

    // Resource Management Texts
    public const string ResourceEmpty = "Workspace ini masih kosong.";
    public const string ResourceEmptyDescription = "Tambahkan file, folder, aplikasi, atau tautan pertama Anda ke dalam workspace ini.";
    public const string AddResourceButton = "+ Tambah Resource";
    public const string AddFile = "Tambah File...";
    public const string AddFolder = "Tambah Folder...";
    public const string AddApplication = "Tambah Aplikasi...";
    public const string AddLink = "Tambah Tautan...";
    public const string AddLinkTitle = "Tambah Tautan Baru";
    public const string EditResourceTitle = "Edit Resource";
    public const string ResourceNameLabel = "Nama Tampilan";
    public const string ResourceTargetLabel = "Target";
    public const string ResourceUrlPlaceholder = "https://example.com";
    public const string RecheckAvailability = "Cek Ulang Ketersediaan";
    public const string ViewModeGrid = "Grid";
    public const string ViewModeList = "List";

    // Resource Types
    public const string ResourceTypeApplication = "Aplikasi";
    public const string ResourceTypeFile = "File";
    public const string ResourceTypeFolder = "Folder";
    public const string ResourceTypeLink = "Tautan";

    // Availability Warnings
    public const string AvailabilityMissing = "Target tidak ditemukan di lokasi ini.";
    public const string AvailabilityAccessDenied = "Akses ke target ini ditolak.";
    public const string AvailabilityUnknown = "Status ketersediaan tidak diketahui.";

    // Delete Resource Confirm
    public const string DeleteResourceTitle = "Hapus Resource";
    public const string DeleteResourceConfirm = "Hapus \"{0}\" dari workspace?\n\nFile, folder, atau aplikasi aslinya tidak akan dihapus.";

    // Open the File (M04) Texts
    public const string OpenTheFileToggle = "Open the File";
    public const string ExitOpenMode = "Selesai";
    public const string OpenButton = "Open";
    public const string OpeningWithProgress = "Membuka ({0}/{1})...";
    public const string SelectedCountFormat = "{0} dipilih";
    public const string SelectAll = "Pilih Semua";
    public const string ClearSelection = "Bersihkan";
    public const string ManyItemsWarning = "Membuka banyak resource sekaligus dapat membebani komputer.";
    public const string LaunchSummaryTitle = "Ringkasan Hasil Pembukaan";
    public const string LaunchSummaryAllSuccessFormat = "Semua resource ({0} item) berhasil dibuka.";
    public const string LaunchSummaryPartialFormat = "{0} berhasil dibuka, {1} gagal.";
    public const string RetryFailedButton = "Coba Lagi yang Gagal";
    public const string CloseSummaryButton = "Tutup";

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
