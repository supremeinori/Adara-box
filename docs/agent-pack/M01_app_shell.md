# M01 — Application Shell

**Tujuan:** kerangka aplikasi yang bisa dijalankan: top bar, navigasi modul, panel horizontal, mekanisme Hide/Show,
infrastruktur DI/logging/settings, dan sistem status modul (placeholder jujur untuk Media & Stats).
**Prasyarat:** M00 selesai dan disetujui. **Referensi SRS:** §3.1-3.2, §5.1, §5.8, §6.6, §12; FR-TB-01…05, FR-PL-01…03, FR-MD-02, FR-ST-02, NFR-MN-01…03.

## Scope
**In:** startup app, DI, logging, settings store dasar, TopBarWindow, trigger strip, panel host, komponen status modul, modul Workspace (kosong dulu), Media & Stats placeholder, tombol Settings (panel pengaturan minimal), single-instance.
**Out:** isi Workspace (M02), resource (M03), Open the File (M04), shortcut global & autostart (M05).

## Arsitektur modul UI

```csharp
// AdaraBox.Core/Modules
public interface IAppModule
{
    string Id { get; }                  // "workspace", "media", "stats", "settings"
    string DisplayName { get; }         // teks tombol
    ModuleStatus Status { get; }
    string? StatusMessage { get; }      // mis. "Masih dalam pengembangan"
    int Order { get; }
}

// AdaraBox.Core/Settings
public interface ISettingsStore
{
    Task<string?> GetAsync(string key, CancellationToken ct = default);
    Task SetAsync(string key, string value, CancellationToken ct = default);
}
public static class SettingKeys
{
    public const string TopBarVisibility = "topbar.visibility"; // "AlwaysVisible" | "Hidden"
    public const string ViewMode = "view.mode";                 // "Grid" | "List"
}
```
**Keputusan:** di M01 `ISettingsStore` memakai `InMemorySettingsStore` (preferensi Hide belum bertahan setelah restart; ini disengaja).
Implementasi SQLite dan persistensi dibuat di M02 (FR-TB-05). Catat keterbatasan ini di Laporan M01.

## Task

### T1 — Bootstrap aplikasi
1. `App.xaml.cs`: bangun `ServiceProvider` (DI), konfigurasi Serilog (path log di Master Guide §8), pasang handler global:
   `DispatcherUnhandledException`, `AppDomain.CurrentDomain.UnhandledException`, `TaskScheduler.UnobservedTaskException` → log + dialog ramah (jangan crash senyap).
2. **Single-instance** pakai named `Mutex` (`Global\AdaraBox.SingleInstance`). Instance kedua: keluar diam-diam (opsional: sinyalkan instance pertama untuk menampilkan top bar).
3. `app.manifest`: `PerMonitorV2` DPI awareness.
4. Tidak ada window utama biasa; `ShutdownMode=OnExplicitShutdown`, dan sediakan ikon tray minimal (NotifyIcon) dengan menu **Tampilkan / Keluar** agar aplikasi bisa ditutup. *(Boleh pakai `System.Windows.Forms.NotifyIcon` via `UseWindowsForms` atau library ringan; catat pilihan di DECISIONS.)*

### T2 — Sistem status modul
1. Implementasi `IAppModule` untuk: `WorkspaceModule` (Available), `MediaModule` (InDevelopment), `StatsModule` (InDevelopment), `SettingsModule` (Available).
2. Buat kontrol `ModuleStatusView` (UserControl): menerima `ModuleStatus` + pesan, menampilkan badge + teks. Pemetaan teks (di `UiText`):
   | Status | Teks |
   |---|---|
   | InDevelopment | "Masih dalam pengembangan" |
   | NotAvailable | "Belum tersedia" |
   | Unsupported | "Tidak didukung pada perangkat ini" |
   | Error | pesan error relevan |
3. Aturan UI: status selain `Available` → semua kontrol aksi **disabled/tidak ada**; tidak ada tombol yang pura-pura berfungsi. Gaya visual `Error` **berbeda** dari `InDevelopment` (warna/ikon), sesuai FR-PL-03.
4. Test: tiap `ModuleStatus` ter-map ke teks yang benar; `Error` ≠ `InDevelopment`.

### T3 — TopBarWindow
Ikuti keputusan ADR dari M00 (default: overlay topmost, tidak menyisihkan ruang layar).
- Window borderless, transparan, `Topmost`, tidak muncul di taskbar, tidak mencuri fokus saat muncul.
- State **Compact**: hanya bar (tinggi ±36 DIP) berisi tombol modul (ikon + label pendek) dan tombol Settings; posisi tengah atas monitor aktif.
- Tombol dibuat dari daftar `IEnumerable<IAppModule>` (data-driven, urut `Order`), bukan hard-code di XAML.
- Modul aktif ditandai jelas (NFR-UX-02).
- Ukuran & jarak dari satu file `UiConstants` (nilai dari spike M00).

### T4 — Panel horizontal
- Klik modul → bar **melebar horizontal** (animasi `Width`, durasi ≈ 150-200 ms, easing halus) dan menampilkan area konten di bawah bar.
- Klik modul aktif lagi, tombol tutup (✕), atau tombol `Esc` → panel menutup dan kembali Compact (FR/§3.2 "cara jelas untuk menutup").
- Panel menampilkan konten sesuai modul lewat `ContentControl` + `DataTemplate` per ViewModel modul.
- Panel menyesuaikan ukuran dengan area kerja layar (`SystemParameters.WorkArea`/info monitor); lebar maksimum = 90% lebar monitor, tinggi maksimum = 70%.
- Saat panel terbuka dan window kehilangan fokus: **tetap terbuka** (default; ADR bisa mengubah). Konstanta `ClosePanelOnFocusLost = false` di `UiConstants`.
- Interaksi di panel tidak boleh mengubah data secara tidak sengaja (belum relevan sekarang; jaga pola: aksi destruktif selalu lewat konfirmasi).

### T5 — Hide / Show + trigger strip
1. `TopBarVisibilityService` (Core interface + implementasi App): state `AlwaysVisible | Hidden`, event perubahan.
2. Mode `Hidden`: TopBarWindow disembunyikan; `TriggerStripWindow` (dari spike M00) aktif; hover dwell → bar muncul sementara; keluar → hilang setelah delay.
3. Mode `AlwaysVisible`: bar selalu terlihat, trigger strip dimatikan.
4. Kontrol toggle Hide ada di panel Settings dan di menu tray.
5. Jangan bikin bar hilang selagi panel terbuka atau mouse masih di atas bar.
6. Test logika: state machine (visible/hidden/temporarily-shown) diuji sebagai kelas murni di Core, dengan timer diabstraksi (`IClock`/`ITimer` fake).

### T6 — Panel Workspace & Settings (kerangka)
- `WorkspacePanelViewModel`: hanya menampilkan teks kosong "Belum ada workspace." Jangan tampilkan tombol yang belum berfungsi.
- `SettingsPanelViewModel`: toggle "Sembunyikan top bar" (terhubung T5) dan info versi aplikasi.
- Tombol **Add Workspace** di top bar: **jangan dirender di M01**; baru ditambahkan di M02.

### T7 — Test & dokumentasi
- Unit test: state machine visibilitas, pemetaan status modul, urutan modul, `OperationResult`.
- Smoke test manual (checklist di bawah) → tulis di `docs/manual-tests/M01.md`.

## Acceptance Criteria
- [ ] Aplikasi start tanpa window biasa; top bar muncul di tengah atas layar; ikon tray ada; Keluar menutup proses sepenuhnya.
- [ ] Hanya satu instance berjalan.
- [ ] Tombol Workspace/Media/Stats/Settings tampil dari daftar modul; modul aktif jelas.
- [ ] Panel melebar horizontal dengan animasi dan bisa ditutup dengan ✕ dan Esc.
- [ ] Media & Stats menampilkan "Masih dalam pengembangan" tanpa kontrol palsu; tampilannya berbeda dari status Error.
- [ ] Mode Hide: bar hilang, hover strip memunculkannya, keluar menyembunyikannya lagi; toggle di Settings dan tray bekerja.
- [ ] Top bar tidak mencuri fokus saat muncul. **[HUMAN CHECK]**
- [ ] Layout benar pada DPI 100/125/150%. **[HUMAN CHECK]**
- [ ] Layer Core tetap bebas WPF/Win32 (test arsitektur hijau).
- [ ] Unhandled exception tercatat di log dan tidak menutup aplikasi tanpa pesan.

## Checklist Manual (isi di docs/manual-tests/M01.md)
1. Jalankan app → bar muncul. 2. Klik Workspace → panel melebar. 3. Klik Media → status pengembangan. 4. Esc → tutup.
5. Settings → Hide → bar hilang. 6. Hover tepi atas → bar muncul → jauhkan mouse → hilang. 7. Jalankan kedua kali → tidak ada instance kedua.
8. Tray → Keluar → proses mati (cek Task Manager).

## Jangan
- Jangan menambah logika workspace/resource/database di modul ini.
- Jangan hard-code daftar tombol di XAML.
- Jangan memakai `Thread.Sleep` untuk delay UI; pakai `DispatcherTimer`/async.
- Jangan membuat placeholder yang bisa diklik dan memberi kesan sukses.
