# PROGRESS — Adara Box

> File ini **dibaca dan diperbarui oleh agent** di setiap sesi. Manusia mengubah kolom "Persetujuan".
> Centang hanya setelah benar-benar terverifikasi (test hijau atau [HUMAN CHECK] selesai).

## Status Modul

| Modul | Nama | Status | Persetujuan manusia |
|---|---|---|---|
| M00 | Setup & Spikes | ✔ selesai | ☐ |
| M01 | App Shell | ✔ selesai | ☐ |
| M02 | Workspace Management | ✔ selesai | ☐ |
| M03 | Resource Management | ✔ selesai | ☐ |
| M04 | Open the File | ✔ selesai | ☐ |
| — | **MVP fungsional** (setelah M04) | ✔ tercapai | ☐ |
| M05 | Hardening | ✔ selesai | ☐ |
| M06 | Packaging & Rilis (v1.0.0) | ✔ selesai | ☐ |
| M07 | Stats (pasca-MVP) | ✔ selesai | ☐ |
| M08 | Media (pasca-MVP) | ✔ selesai | ☐ |

Status: ☐ belum · ◐ sedang dikerjakan · ✔ selesai · ⛔ terblokir

## Menunggu Verifikasi Manusia ([HUMAN CHECK])

| Modul | Deskripsi | Langkah Uji / Referensi |
|---|---|---|
| M00 | Verifikasi visual interaktif Top Bar (Overlay Topmost vs AppBar) & Hover Trigger Strip | Jalankan `spikes/WindowSpike/bin/Release/net8.0-windows/WindowSpike.exe`, ikuti 4 skenario di `docs/spike-window.md` (uji pencurian fokus, input textbox, space reservation AppBar, dan dwell timer hover). |
| M01 | Verifikasi visual Application Shell (Startup tanpa window biasa, Top Bar compact 36 DIP, ekspansi panel horizontal animasi, honest placeholder Media & Stats, Esc & Close button, toggle Hide via Settings & Tray, hover trigger strip, dan single instance) | Jalankan `src/AdaraBox.App/bin/Release/net8.0-windows/AdaraBox.App.exe`, ikuti 8 skenario di `docs/manual-tests/M01.md`. |
| M02 | Verifikasi visual dan fungsional Workspace Management & Persistensi (Inisialisasi SQLite WAL & migrasi awal, tombol data-driven `+ Workspace`, form inline buat/ganti nama dengan validasi, duplikat nama diizinkan, dialog konfirmasi hapus non-destructive, tampilan detail workspace FR-WS-05, serta persistensi preferensi Hide dan data workspace setelah restart) | Jalankan `src/AdaraBox.App/bin/Release/net8.0-windows/AdaraBox.App.exe`, ikuti 9 skenario di `docs/manual-tests/M02.md`. |
| M03 | Verifikasi visual dan fungsional Resource Management & Tampilan Grid/List (Penambahan 4 tipe resource: file batch multiselect, folder, aplikasi, tautan; validasi URL dan normalisasi; toggle mode Grid/List tanpa kehilangan data; form edit nama dan target; tombol urut naik/turun; indikator peringatan ketersediaan ⚠️; hapus referensi tanpa menghapus file fisik; serta dialog hapus workspace yang menampilkan jumlah resource sebenarnya) | Jalankan `src/AdaraBox.App/bin/Release/net8.0-windows/AdaraBox.App.exe`, ikuti 11 skenario di `docs/manual-tests/M03.md`. |
| M04 | Verifikasi visual dan fungsional Open the File (Mode toggle & visual header aksen, checkbox di grid & list, counter realtime & bulk select/clear, peringatan >10 item, pemisahan mutlak mode kelola vs buka FR-OF-05, peluncuran multi-resource native Windows berurutan 150ms tanpa freeze, penanganan kegagalan per item tanpa memutus item lain FR-OF-06, ringkasan hasil dengan coba lagi yang gagal, pembersihan seleksi sementara FR-OF-08, serta shortcut keyboard Ctrl+A, Enter, Esc, Space) | Jalankan `src/AdaraBox.App/bin/Release/net8.0-windows/AdaraBox.App.exe`, ikuti 11 skenario di `docs/manual-tests/M04.md`. |
| M05 | Verifikasi visual & ketahanan MVP Hardening (Deteksi DB korup/0 byte & dialog pemulihan, shortcut global `Ctrl+Alt+Space`, auto-start toggle di registry `HKCU\...\Run`, navigasi keyboard penuh F2/Delete/Enter/Esc, reposisi multi-monitor & DPI scaling PerMonitorV2, dan performa beban 200 resource < 300ms) | Jalankan `src/AdaraBox.App/bin/Release/net8.0-windows/AdaraBox.App.exe`, ikuti 6 skenario di `docs/manual-tests/M05.md`. |
| M06 | Verifikasi rilis & installer pada lingkungan Windows bersih (Instalasi per-pengguna ke `%LOCALAPPDATA%\Programs\AdaraBox` tanpa hak admin, verifikasi uninstaller dengan perlindungan data pengguna, kompilasi Inno Setup via `build/build-installer.ps1`) | Jalankan `powershell -ExecutionPolicy Bypass -File build/build-installer.ps1` dan ikuti checklist di `docs/release-checklist.md`. |
| M07 | Verifikasi visual dan performa System Stats (Live update metrik CPU, RAM terpakai/total, daftar fixed drives, dan GPU 3D; penanganan nilai null "Tidak tersedia" tanpa dummy data; verifikasi zero-polling saat panel ditutup/top bar tersembunyi) | Buka panel Stats di Top Bar, perhatikan metrik real-time. Tutup panel dan pastikan aktivitas CPU aplikasi kembali idle < 0.5%. |
| M08 | Verifikasi fungsional Media Quick Links & Evaluasi WebView2 (Simpan tautan Spotify Web / YouTube Music, buka di browser default lewat launcher, penolakan skema tidak aman, review laporan evaluasi WebView2 di `docs/webview-evaluation.md`) | Buka panel Media di Top Bar, tambah tautan media web, klik Buka dan pastikan link terbuka di browser default. |

## OPEN QUESTIONS

| # | Pertanyaan | Modul | Jawaban manusia |
|---|---|---|---|
| Q1 | Bagaimana perilaku top bar pada setup multi-monitor (apakah muncul di primary monitor saja atau di monitor aktif tempat kursor berada)? | M01 / M05 | Muncul di monitor aktif tempat kursor berada; fallback ke monitor primer (NFR-CP-03, SRS §16 no. 2). Selesai di M05. |
| Q2 | Apa shortcut keyboard default untuk toggle/fokus top bar (misal `Win+Alt+A` atau `Ctrl+Alt+Space`) agar tidak konflik dengan hotkey Windows? | M01 / M05 | `Ctrl+Alt+Space` via Win32 `RegisterHotKey`, dapat diubah di Pengaturan (FR-TB-06). Selesai di M05. |
| Q3 | Bagaimana preferensi startup awal (Run on Windows Startup, Start minimized to tray, atau langsung Always Visible)? | M01 / M05 | Pilihan di Pengaturan (default MATI), tersimpan di registry `HKCU\...\Run` dengan nama "AdaraBox" (FR-CF-04). Selesai di M05. |
| Q4 | Apakah drag-and-drop dari File Explorer langsung ke Workspace didukung pada MVP atau pasca-MVP? | M03 | Pasca-MVP (sesuai scope M03 & SRS §5.3) |

## Acceptance Criteria MVP (SRS §11) — dibuktikan di M04/M05

- [x] 1. Pengguna dapat membuat dan membuka beberapa workspace.
- [x] 2. Workspace dapat menyimpan referensi aplikasi, file, folder, dan tautan.
- [x] 3. Resource ditampilkan dalam grid maupun list.
- [x] 4. Ganti tampilan tanpa kehilangan data.
- [x] 5. Open the File memungkinkan pemilihan dan pembukaan beberapa resource.
- [x] 6. Kegagalan satu resource tidak menghentikan resource lain.
- [x] 7. Menghapus resource dari Adara Box tidak menghapus file/folder asli.
- [x] 8. Data workspace tetap ada setelah aplikasi dimulai ulang.
- [x] 9. Top bar dapat selalu terlihat atau disembunyikan sesuai pengaturan.
- [x] 10. Modul yang belum diimplementasikan menampilkan status jelas dan tidak berpura-pura berfungsi.
- [x] 11. Aplikasi tetap berguna untuk Workspace tanpa Spotify, browser mini, atau Stats.

## Peta Requirement → Modul (acuan traceability)

| Requirement | Modul |
|---|---|
| FR-TB-01…04 (top bar, modul, Hide, hover) | M01 |
| FR-TB-05 (persist preferensi) | M02 |
| FR-TB-06 (shortcut) | M05 |
| FR-WS-01…07 | M02 |
| FR-RS-01…09 | M03 |
| FR-VW-01…05 | M03 |
| FR-OF-01…10 | M04 |
| FR-PL-01…04, FR-MD-02, FR-ST-02 (placeholder) | M01 |
| FR-CF-01/02/05 | M02 |
| FR-CF-03/04 | M05 |
| FR-ST-03…06 | M07 |
| FR-MD-01/03/04/05 | M08 |
| NFR-MN-01…03 (arsitektur) | M00-M01 (test arsitektur) |
| NFR-RL, NFR-CP, NFR-UX, NFR-PF, NFR-SC | M02-M05 (diaudit di M05) |
| NFR-CP-04 (Windows minimum) | M00 (keputusan), M06 (installer) |

## Log Laporan Modul

## Laporan M06 — 2026-10-31
- Status: SELESAI (MVP Packaging, Documentation & Installer Complete)
- Task selesai:
  - T1 (Versioning & Changelog):
    - `Directory.Build.props`: Versi dinaikkan ke SemVer resmi `<Version>1.0.0</Version>`.
    - `SettingsPanelViewModel.cs`: Menampilkan versi dinamis dari `AssemblyInformationalVersion` / `Assembly.GetName().Version` ("Versi: 1.0.0").
    - `CHANGELOG.md`: Dokumentasi lengkap mengikuti format Keep a Changelog merangkum seluruh penambahan dan perbaikan dari M01 hingga M05.
  - T2 (Konfigurasi Publish Self-Contained):
    - `assets/app.ico`: Dibuat aset ikon aplikasi multi-resolusi (16, 32, 48, 256 pixel) yang disematkan ke executable `AdaraBox.App.exe` dan System Tray.
    - `build/publish.ps1`: Skrip otomatisasi pengujian (`dotnet test -c Release`) dilanjutkan dengan publish mandiri `win-x64` (`--self-contained true`).
    - Output publish mandiri diverifikasi: `artifacts/publish/AdaraBox.App.exe` (163.31 MB, 482 file mandiri, siap dijalankan pada komputer tanpa .NET Runtime terpasang).
    - `app.manifest` (PerMonitorV2) dan `app.ico` disalin ke folder output.
    - `.gitignore`: Menambahkan folder `artifacts/` agar biner rilis tidak tercampur ke repository Git.
  - T3 (Installer Inno Setup):
    - `installer/AdaraBox.iss`: Skrip installer per-pengguna (`PrivilegesRequired=lowest`) memasang ke `{localappdata}\Programs\AdaraBox` tanpa hak admin.
    - Pintasan Start Menu, pintasan Desktop opsional (default unchecked), dan opsi "Jalankan Adara Box saat Windows dimulai" (default unchecked, sinkron dengan kunci Registry `HKCU\...\Run\AdaraBox`).
    - Validasi versi minimum Windows 10 build ≥ 19044 (`MinVersion=10.0.19044`).
    - Pengecekan proses berjalan via mutex `Global\AdaraBox.SingleInstance` dan `CloseApplications=yes`.
    - Uninstaller bersih dengan proteksi data pengguna: menanyakan konfirmasi sebelum menghapus `%LOCALAPPDATA%\AdaraBox\` (default: TIDAK, data pengguna aman).
    - `build/build-installer.ps1`: Skrip otomatisasi kompilasi yang mendeteksi `ISCC.exe` di PATH atau Program Files dan memberikan panduan instalasi jika Inno Setup belum terpasang.
  - T4 (Dokumentasi):
    - `README.md`: Panduan lengkap proyek, fitur MVP, cara instalasi (installer & portable), cara build dari sumber, struktur repo, lokasi data & log, dan keterbatasan yang diketahui.
    - `LICENSE`: Lisensi open-source MIT.
    - `docs/USER_GUIDE.md`: Panduan pengguna ringkas berbahasa Indonesia (manajemen workspace, penambahan resource, mode Grid/List, Open the File, pintasan keyboard, backup manual, dan status jujur Media/Stats).
    - `docs/KNOWN_ISSUES.md`: Dokumentasi utang teknis dan bug P2/P3 yang ditunda (DirectX exclusive fullscreen, prompt UAC pada target admin, drag-and-drop post-MVP, dan SmartScreen warning).
    - `docs/DECISIONS.md`: Ditambahkan ADR-011 (Distribusi Paket Self-Contained win-x64).
  - T5 (Verifikasi Rilis):
    - `docs/release-checklist.md`: Panduan checklist pengujian pra-rilis pada mesin bersih / VM tanpa .NET SDK.
    - Seluruh 157 automated unit & integration tests lulus (100%).
  - T6 (Tag Rilis):
    - Menunggu persetujuan manusia sebelum pembuatan tag git `v1.0.0` (sesuai aturan Master Guide: agent tidak membuat atau mem-push tag tanpa instruksi eksplisit).

## Laporan M05 — 2026-10-31
- Status: SELESAI (MVP Hardening, Stability & Verification Complete)
- Task selesai:
  - T1 (Database Corruption, Backup & Recovery):
    - `DatabaseIntegrityChecker`: deteksi file 0-byte, deteksi file non-SQLite, pemeriksaan `PRAGMA quick_check;`, dan deteksi schema versi lebih baru (`user_version > maxKnownVersion`).
    - `DatabaseBackupService`: auto-backup sebelum menjalankan migrasi schema menggunakan SQLite `VACUUM INTO` (aman terhadap mode WAL aktif), retensi 5 file terbaru dengan pruning otomatis file lama, pengarsipan database korup bertanggal (`adarabox.db.corrupt-{timestamp}`) sebelum restorasi atau fresh reset, dan pemulihan backup.
    - `DatabaseRecoveryWindow`: dialog pemulihan WPF jika database korup saat startup, dengan opsi Buka Folder Data, Pulihkan dari Backup, Mulai Database Baru, dan Keluar.
    - `DatabaseHardeningTests`: 7 unit tests (byte acak, 0-byte, schema versi lebih baru, auto-vacuum backup & retention 5 item, pengarsipan korup, pemulihan backup).
  - T2 (Runtime Resilience):
    - Audit penanganan `async void` (hanya untuk event handler dengan guard try/catch yang mencatat log dan menampilkan dialog ramah pengguna).
    - Exception handler global (`AppDomain.CurrentDomain.UnhandledException`, `DispatcherUnhandledException`, `TaskScheduler.UnobservedTaskException`).
    - Guard penutupan bersih dengan `CancellationTokenSource`.
    - Logging Serilog: retensi 14 hari, tidak membocorkan path lengkap file/URL pada level `Information`.
  - T3 (Multi-Monitor & DPI - NFR-CP-03, SRS §16 no. 2):
    - `ScreenPositionHelper`: deteksi monitor tempat kursor berada menggunakan Win32 `GetCursorPos` & `MonitorFromPoint`, fallback ke monitor primer jika kursor di luar bounds.
    - Perhitungan posisi window dan trigger strip dalam DIP menggunakan `VisualTreeHelper.GetDpi`.
    - Reposisi dinamis saat konfigurasi layar berubah via hook `SystemEvents.DisplaySettingsChanged`.
  - T4 (Shortcut Global & Auto-Start - FR-TB-06, FR-CF-04):
    - Core: `HotkeyModifier`, record `HotkeyDefinition`, dan `HotkeyParser` dengan validasi modifier wajib dan penolakan kombinasi sistem terlarang (`Ctrl+Alt+Del`, `Win+L`, `Alt+Tab`, dll.).
    - Platform: `IGlobalHotkeyService` dan `Win32GlobalHotkeyService` via Win32 `RegisterHotKey` pada dedicated `HwndSource` hook; pelaporan error elegan jika konflik tanpa crash. Default: `Ctrl+Alt+Space`.
    - Platform: `IAutoStartService` dan `RegistryAutoStartService` memanipulasi `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` ("AdaraBox" saja).
    - UI: Pengaturan (Settings) mendukung toggle Run on Startup, input teks shortcut global, tombol Simpan, dan pesan validasi kesalahan.
    - Tests: `HotkeyParserTests` dan `SettingsPanelViewModelTests`.
  - T5 (Keyboard & Aksesibilitas - NFR-UX-03):
    - Alur navigasi keyboard penuh: `F2` untuk ganti nama inline, `Delete` untuk hapus (dengan konfirmasi), `Enter` untuk membuka detail / buka terpilih, `Space` untuk toggle centang di mode Open, `Ctrl+A` untuk select all.
    - `Esc` berjenjang: tutup ringkasan hasil -> batal form resource/workspace -> keluar mode Open -> kembali ke daftar workspace -> tutup panel top bar.
    - Fokus visual terlihat jelas (`IsKeyboardFocused` trigger pada kartu dan baris).
    - Properti aksesibilitas `AutomationProperties.Name` pada seluruh tombol ikon-saja (✕, ✏, 🗑, ▲, ▼).
    - Kontras warna teks memenuhi standar WCAG (>= 4.5:1).
  - T6 (Performa & Benchmark):
    - Laporan performa terperinci di `docs/perf-report.md`.
    - Buka workspace 200 resource: **2 ms** (target: < 300 ms, 150× lebih cepat).
    - Buka workspace 500 resource: **30 ms**.
    - Stabilitas memori 100 siklus: **+0.02 MB** (tanpa kebocoran memori).
    - CPU idle: **0.00%** (target: ≈ 0%).
    - RAM idle (Working Set): **138.32 MB** (target: < 150 MB).
  - T7 (Audit Keamanan & Privasi):
    - Laporan audit terperinci di `docs/security-review.md`.
    - 0 panggilan penghapusan file pengguna fisik (Golden Rule #1 terjaga 100%).
    - 0 eksekusi shell string cmd/powershell (Golden Rule #3 terjaga 100%).
    - 100% parameterized SQL query (tanpa interpolasi string).
    - 0 panggilan jaringan (Golden Rule #5: 100% offline).
    - Dependensi NuGet dicek: **0 vulnerable packages** (High/Critical: 0).
  - T8 (Matriks Pengujian & Keterlacakan):
    - `docs/test-matrix.md` mencakup seluruh pengujian SRS §10.1–10.4.
    - `docs/traceability.md` memetakan seluruh FR-ID P0/P1 ke kode dan test pembukti.
    - Panduan uji manual di `docs/manual-tests/M05.md`.
    - Seluruh 11 Kriteria Penerimaan MVP (SRS §11) terbukti 100%.

## Laporan M04 — 2026-10-31
- Status: SELESAI (MVP Fungsional Tercapai)
- Task selesai:
  - T1 (Core & Platform):
    - Core: enum `LaunchFailureReason`, record `LaunchResult`, record `LaunchSummary` (dengan `SuccessCount` dan `FailureCount`), antarmuka `IResourceLauncher` dan `IMultiLaunchService`, serta implementasi `MultiLaunchService` (eksekusi berurutan di background `Task.Run` dengan jeda 150ms antar item, penanganan exception per item tanpa menghentikan item berikutnya, pelaporan progres per item via `IProgress<LaunchResult>`, dan pembatalan bersih via `CancellationToken`).
    - Platform: antarmuka `IProcessStarter`, implementasi `ShellProcessStarter` (murni `ProcessStartInfo` dengan `UseShellExecute = true` tanpa parameter cmd/powershell per Aturan Emas #3), dan implementasi `ResourceLauncher` (re-validasi target via `ResourceTargetValidator`, penanganan `Link` via browser default, penanganan `File`/`Folder` dengan pengecekan `File.Exists`/`Directory.Exists` lebih dulu -> `NotFound` tanpa exception, penanganan `Application` dengan `WorkingDirectory` otomatis ke direktori file atau direct start untuk `shell:AppsFolder\...`, pemetaan `Win32Exception` 2/3 -> `NotFound`, 5 -> `AccessDenied`, 1155 -> `NoHandler`, 1223 -> `Cancelled`, penanganan `UnauthorizedAccessException`, serta logging privasi: hanya mencatat jenis + status hasil di level Information tanpa membocorkan path lengkap).
    - DI: Pendaftaran `IProcessStarter`, `IResourceLauncher`, dan `IMultiLaunchService` di `App.xaml.cs`.
  - T2 (State & ViewModel):
    - `WorkspacePanelViewModel` mendukung status Open the File: `IsOpenMode`, `IsLaunching`, `LaunchProgressCurrent`, `LaunchProgressTotal`, `IsLaunchSummaryOpen`, `LaunchSummaryTitle`, `LaunchSummaryMessage`, `FailedResults`, dan `SuccessfulResults`.
    - Properti turunan: `SelectedCount`, `SelectedCountText`, `CanOpen`, `HasSelected`, `HasManySelected` (>10 item, FR-OF-10), `OpenButtonText`, dan `CanManageResources` (FR-OF-05).
    - Perintah baru: `ToggleOpenModeCommand`, `ExitOpenModeCommand`, `SelectAllCommand`, `ClearSelectionCommand`, `OpenSelectedCommand`, `RetryFailedCommand`, dan `CloseLaunchSummaryCommand`.
    - `ResourceItemViewModel` memiliki properti `IsSelected` (state UI sementara, tidak disimpan ke database per FR-OF-08), `ToggleSelectCommand`, dan guard `CanManage` yang menonaktifkan perintah edit/delete/move saat mode Open aktif.
    - Pembersihan seleksi otomatis (FR-OF-08) saat keluar mode Open, saat menekan tombol "← Kembali", saat membuka workspace lain, dan saat menghapus workspace.
    - Pemisahan mutlak mode Kelola vs Buka (FR-OF-05): seluruh tombol manipulasi resource (`+ File`, `+ Folder`, `+ Aplikasi`, `+ Tautan`, `⟳ Cek Ulang`, `Ganti Nama`, `Hapus`, `▲`, `▼`, `✏`, `🗑`) disembunyikan dan di-guard dari eksekusi saat mode Open aktif. Klik pada item di mode Open memicu toggle centang (`ToggleSelectCommand`), bukan membuka editor.
  - T3 (UI & Aksesibilitas):
    - Tombol header dengan teks persis **"Open the File"** (FR-OF-01) dengan highlight aksen visual hijau emerald saat aktif (NFR-UX-02).
    - Checkbox pemilihan muncul di kartu mode Grid dan baris mode List (FR-OF-02, FR-VW-05).
    - Action bar Open the File menampilkan counter realtime, tombol "Pilih Semua", "Bersihkan", tombol terkemuka "Open" (disabled jika 0 item dipilih, FR-OF-09), banner peringatan jika >10 item dipilih (FR-OF-10), serta tombol "Selesai".
    - Panel overlay non-blocking ringkasan hasil pembukaan resource: status sukses/gagal per item (FR-OF-06), daftar kegagalan dengan badge alasan dan pesan ramah dalam Bahasa Indonesia, tombol "Coba Lagi yang Gagal", dan tombol "Tutup".
    - Navigasi keyboard: `Space` untuk toggle centang item terfokus, `Ctrl+A` untuk pilih semua, `Enter` untuk Open jika `CanOpen`, dan `Esc` berjenjang (tutup ringkasan -> keluar mode Open -> batal form -> tutup panel).
  - T4 (Automated Tests): 127 unit tests hijau di seluruh test suite (Core, Data, Platform, App).
    - `AdaraBox.Core.Tests`: 58 tests (termasuk 7 test baru `MultiLaunchServiceTests` mencakup all succeed, satu gagal sisanya tetap jalan FR-OF-06, launcher exception tidak bocor, urutan hasil sesuai input, cancellation stops unexecuted, progress per item, empty/null input).
    - `AdaraBox.Platform.Tests`: 31 tests (termasuk 11 test baru `ResourceLauncherTests` mencakup link normalisasi, URL berbahaya ditolak, file dengan spasi/non-ASCII tanpa quoting manual, missing path -> NotFound tanpa panggil starter, folder, aplikasi dengan working dir, store app protocol, pemetaan kode Win32 2/3/5/1155/1223/unknown, unauthorized access, token cancelled, dan audit zero shell execution).
    - `AdaraBox.App.Tests`: 20 tests (termasuk 8 test baru `WorkspacePanelViewModelLaunchTests` mencakup toggle mode, pembersihan seleksi saat keluar mode & back to list FR-OF-08, can open false saat 0 item FR-OF-09, bulk select/clear, aksi kelola diblokir di mode Open FR-OF-05, peluncuran multi-resource dengan ringkasan, retry failed items, dan deteksi >10 item FR-OF-10).
    - `AdaraBox.Data.Tests`: 18 tests (CRUD, migrasi, transaksi).
- Acceptance criteria (9/9 terpenuhi):
  1. Mode Open the File bisa diaktifkan/dinonaktifkan; checkbox hanya muncul saat aktif, di grid maupun list (FR-OF-01..02).
  2. Membuka resource terpilih berjalan melalui mekanisme Windows yang sesuai tanpa eksekusi shell (FR-OF-04).
  3. Satu resource gagal tidak menghentikan yang lain; ringkasan menampilkan item gagal + alasan ramah (FR-OF-06).
  4. Open dinonaktifkan saat tidak ada pilihan; counter benar (FR-OF-07/09).
  5. Di mode Open tidak ada cara menghapus/mengganti nama/mengedit tanpa sengaja (FR-OF-05).
  6. Pilihan terhapus saat mode dimatikan atau keluar workspace (FR-OF-08).
  7. UI tidak freeze saat membuka banyak item (background task 150ms delay, NFR-PF-03).
  8. Zero call ke cmd / powershell di seluruh codebase (diverifikasi dengan grep).
  9. Seluruh 11 Kriteria Penerimaan SRS §11 terpenuhi dan MVP fungsional tercapai.
- [HUMAN CHECK] yang menunggu: Uji manual 11 skenario di `docs/manual-tests/M04.md`.
- Keputusan yang diambil (dan di mana dicatat):
  - Penanganan Esc berjenjang di `TopBarWindow.xaml.cs` diperluas: ringkasan hasil -> keluar mode Open -> batalkan form resource -> batalkan form workspace -> tutup panel.
  - Ringkasan peluncuran dibuat sebagai panel overlay non-blocking di dalam antarmuka workspace sehingga pengguna tetap nyaman dan tidak terinterupsi dialog modal sistem.
- Masalah/utang teknis yang diketahui: Tidak ada.

## Laporan M03 — 2026-10-31
- Status: SELESAI
- Task selesai:
  - T1 (Data): Implementasi `SqliteResourceRepository` (CRUD lengkap, transaksi atomik `AddRangeAsync`, transaksi atomik `ReorderAsync`, query terurut `sort_order` lalu `created_at`, query hitung `GetCountByWorkspaceAsync`, dan mapping type enum lowercase via `SqliteTypeHandlers`).
  - T2 (Service & Validator Core): Model `Resource`, enum `ResourceType` & `ResourceAvailability`, `IResourceRepository`, `IResourceService`, `ResourceService`, `ResourceTargetValidator` (normalisasi skema URL, penolakan skema berbahaya seperti javascript/file/ms-*, validasi path absolut file/folder, validasi aplikasi .exe/.lnk/shell:AppsFolder, batas nama 80 karakter & target 2048 karakter, duplikat target diizinkan sesuai FR-RS-09), update `IWorkspaceService.GetResourceCountAsync`.
  - T3 (Availability & Ikon Platform):
    - `ResourceAvailabilityChecker`: pemeriksaan ketersediaan file/folder di background dengan pembatasan paralelisme (maks 8), penanganan `UnauthorizedAccessException` -> `AccessDenied`, timeout 2 detik -> `Unknown`, dan tautan langsung `Unknown` tanpa request jaringan (Aturan Emas #5).
    - `IconProvider`: ekstraksi ikon aplikasi dan file via Win32 `SHGetFileInfo`, thumbnail frozen bitmap thread-safe, caching in-memory per ekstensi/path, dan fallback icon bitmap tanpa melempar exception.
  - T4 (Tampilan Detail Workspace & CRUD Resource UI):
    - Navigasi detail workspace menampilkan judul, hitungan resource, dan state kosong jika belum ada resource.
    - Abstraksi `IFilePickerService` & `WpfFilePickerService` untuk memilih file (multiselect), folder, dan aplikasi.
    - Tambah File (batch), Tambah Folder, Tambah Aplikasi, dan Tambah Tautan via form inline dengan validasi instan.
    - Form edit resource untuk mengubah nama tampilan dan target (tipe terkunci).
    - Hapus resource meminta konfirmasi non-destructive ("Hapus dari workspace? File aslinya tidak akan dihapus.") dan hanya menghapus referensi di DB.
    - Pengurutan manual via tombol ▲/▼ dan menu konteks.
    - Background availability check: item yang hilang (`Missing` / `AccessDenied`) diberi ikon peringatan ⚠️ dan tooltip informatif tanpa menghapus atau menyembunyikan item secara otomatis (FR-RS-07). Tersedia tombol "⟳ Cek Ulang".
    - Info pembeda ditampilkan pada kartu (host URL untuk link, nama folder induk untuk file/folder) untuk membedakan nama yang mirip (FR-RS-04).
  - T5 (Tampilan Grid & List):
    - Toggle mode Grid / List di header detail workspace.
    - Kedua mode berbagi satu koleksi data dan ViewModel yang sama (FR-VW-03) dengan fungsionalitas yang identik (FR-VW-05).
    - Preferensi mode tampilan disimpan secara global ke SQLite via `SettingKeys.ViewMode`.
    - Virtualisasi List via `VirtualizingStackPanel`.
    - Beralih mode tidak memuat ulang data dari DB dan tidak mereset urutan.
  - T6 (Automated Tests): 96 unit tests hijau di seluruh test suite (Core, Data, Platform, App).
    - `AdaraBox.Core.Tests`: 51 tests (validasi target semua tipe, URL berbahaya, panjang string, CRUD resource service, sorting, error codes).
    - `AdaraBox.Data.Tests`: 18 tests (CRUD resource, foreign key cascade delete dari workspace, atomisitas transaksi `AddRangeAsync`, transaksi `ReorderAsync`, dan penanganan path non-ASCII/kutip tunggal).
    - `AdaraBox.Platform.Tests`: 15 tests (availability checker dengan file/folder temp, missing path, link tanpa request jaringan, timeout, dan icon provider).
    - `AdaraBox.App.Tests`: 12 tests (toggle grid/list, file picker add files, hapus resource, create/rename/delete workspace, dan visibility persistence).
- Acceptance criteria (8/8 terpenuhi):
  1. Keempat jenis resource (Aplikasi, File, Folder, Tautan) bisa ditambah, dilihat, diedit, dan dihapus di workspace (FR-RS-01…06).
  2. Yang tersimpan hanya referensi; tidak ada penyalinan file fisik (cek kode: 0 panggilan `File.Copy/Move/Delete`, `Directory.*`).
  3. Target tidak ditemukan → indikator peringatan ⚠️ + pesan; item tidak hilang diam-diam (FR-RS-07).
  4. Grid dan list sama-sama lengkap fungsinya; toggle tidak kehilangan data; preferensi bertahan setelah restart (FR-VW-01…05).
  5. URL berskema berbahaya ditolak; path dengan spasi dan non-ASCII bekerja.
  6. Menambah banyak file sekaligus bekerja dalam satu transaksi (`AddRangeAsync`).
  7. UI tetap responsif saat cek ketersediaan berjalan di background (tidak ada blocking di UI thread).
  8. Semua test hijau (96 tests passed; `dotnet build -c Release` 0 errors, 0 warnings).
- [HUMAN CHECK] yang menunggu: Uji manual 11 skenario di `docs/manual-tests/M03.md`.
- Keputusan yang diambil (dan di mana dicatat):
  - Penanganan drag-and-drop diputuskan untuk pasca-MVP (SRS §5.3, Q4 di `PROGRESS.md`).
  - Pembuatan fallback bitmap programatis di `IconProvider` menggunakan `BitmapSource.Create` agar aman dieksekusi di thread non-STA (MTA).
- Masalah/utang teknis yang diketahui: Tidak ada. Semua dependensi mematuhi aturan arsitektur.

## Laporan M02 — 2026-10-31
- Status: SELESAI
- Task selesai:
  - T1 (Infrastruktur Data): `IDbConnectionFactory`, `SqliteConnectionFactory` (WAL mode, `PRAGMA foreign_keys = ON`), Dapper type handlers Guid & DateTimeOffset, `DatabaseMigrator` (transaksional per versi, `PRAGMA user_version`), `Migration001_InitialSchema` (tabel `workspace`, `resource` with `ON DELETE CASCADE`, `settings`), `SqliteWorkspaceRepository`, `SqliteSettingsStore` (upsert on conflict).
  - T2 (Core Services): `IClock`, `SystemClock`, record `Workspace`, `IWorkspaceRepository`, `IWorkspaceService`, `WorkspaceNameValidator` (trim, empty/whitespace check, max 60 chars, control characters rejected, duplikat diizinkan sesuai FR-WS-07), `WorkspaceService` (Golden Rule #1: menghapus DB row tanpa menyentuh disk).
  - T3 (Persistensi Preferensi Top Bar): `TopBarVisibilityService` membaca initial mode dari `ISettingsStore` (`SettingKeys.TopBarVisibility`) pada startup aplikasi dan menyimpan otomatis saat mode berubah. Runtime in-memory store digantikan dengan SQLite.
  - T4 (UI Workspace):
    - Tombol cepat data-driven **"+ Workspace"** di top bar via `ITopBarAction` & `AddWorkspaceTopBarAction`.
    - `WorkspacePanelViewModel` dengan `ObservableCollection<WorkspaceItemViewModel>`, status loading, status kosong ("Belum ada workspace. Buat workspace pertamamu untuk mulai mengelompokkan file, aplikasi, dan tautan.").
    - Form inline terpadu untuk Buat dan Ganti Nama workspace (TextBox dengan auto-focus, Enter = Simpan, Esc = Batal, pesan error validasi tampil di bawah field).
    - Pencegahan penutupan shell window oleh tombol Esc saat form sedang aktif.
    - Dialog konfirmasi hapus workspace via `IDialogService` & `WpfDialogService` menyebutkan nama, jumlah resource (0 resource), dan peringatan non-destructive: *"Hanya menghapus workspace dari Adara Box. File, folder, aplikasi, dan tautan aslinya tidak terpengaruh."*.
    - Tampilan detail workspace saat item diklik (FR-WS-05) dengan judul workspace, tombol "← Kembali", dan placeholder jujur untuk resource yang akan hadir di M03.
  - T5 (Automated Tests): 53 unit tests mencakup Core, Data, Platform, dan App.
    - `AdaraBox.Core.Tests`: validator nama, CRUD workspace service, sorting, penanganan not found, duplikat nama diizinkan, dan test arsitektur.
    - `AdaraBox.Data.Tests`: migrasi DB baru, idempotensi migrasi, rollback transaksi pada kegagalan migrasi, foreign key cascade, persistensi melintasi restart koneksi, upsert settings, dan path SQLite dengan spasi/non-ASCII.
    - `AdaraBox.Platform.Tests`: launcher error code mapping, single-instance mutex, tray menu.
    - `AdaraBox.App.Tests`: `WorkspacePanelViewModel` empty state, create flow, validation error, detail view navigation, rename flow, delete flow dengan confirmation dialog, dan persistensi `TopBarVisibilityService`.
- Acceptance criteria (8/8 terpenuhi):
  1. Pengguna dapat membuat, melihat, mengganti nama, dan menghapus workspace (FR-WS-01…05).
  2. Nama kosong ditolak dengan pesan jelas; nama duplikat diizinkan (FR-WS-07).
  3. Workspace dan preferensi Hide tetap ada setelah aplikasi ditutup dan dibuka lagi (FR-WS-06, FR-TB-05).
  4. Hapus workspace meminta konfirmasi dan tidak menyentuh file asli (cek: 0 panggilan `File.Delete`/`Directory.Delete` di seluruh kode).
  5. Migrasi berjalan di DB baru; skema versi terjaga dan rollback bila transaksi gagal.
  6. Penulisan data memakai transaksi bila lebih dari satu statement (NFR-RL-04).
  7. Tidak ada operasi DB di UI thread yang memblokir (semua async).
  8. Test Core, Data, Platform, App, dan arsitektur hijau (53 tests passed; `dotnet build -c Release` 0 errors, 0 warnings).
- [HUMAN CHECK] yang menunggu: Uji manual 9 skenario di `docs/manual-tests/M02.md`.
- Keputusan yang diambil (dan di mana dicatat):
  - Form inline terpadu untuk Create dan Rename Workspace dengan penanganan Esc berjenjang di `TopBarWindow.xaml.cs`.
  - Abstraksi dialog via `IDialogService` di layer App agar ViewModel 100% dapat diuji secara otomatis tanpa memunculkan WPF modal dialog di unit tests.
  - Aksi top bar data-driven via `ITopBarAction` (`AddWorkspaceTopBarAction`) yang terdaftar di DI.
- OPEN QUESTION: Q1 (Multi-monitor), Q2 (Shortcut keyboard default), Q3 (Startup behavior) diputuskan di M05; Q4 (Drag-and-drop) dievaluasi di M03.
- Masalah/utang teknis yang diketahui: Tidak ada. Semua dependensi mematuhi aturan arsitektur.

## Laporan M01 — 2026-10-31
- Status: SELESAI
- Task selesai: T1 (Bootstrap aplikasi, DI, Serilog, unhandled exceptions, single-instance named Mutex, manifest PerMonitorV2, OnExplicitShutdown, TrayIconManager), T2 (Sistem status modul, IAppModule, Workspace/Media/Stats/Settings modules, ModuleStatusView badge & mapping, ModuleStatusExtensions), T3 (TopBarWindow compact 36 DIP, overlay topmost, data-driven module buttons), T4 (Panel horizontal melebar animasi CubicEase, Esc & ✕ close, ContentControl + DataTemplate per ViewModel), T5 (TopBarVisibilityService, TopBarVisibilityStateMachine, TriggerStripWindow hover dwell 250ms & hide delay 600ms, toggle di Settings & Tray), T6 (WorkspacePanelViewModel kosong jujur, SettingsPanelViewModel toggle Hide & versi), T7 (Unit tests, smoke test guide di docs/manual-tests/M01.md).
- Acceptance criteria: 10/10 terpenuhi:
  1. Aplikasi start tanpa window biasa; top bar muncul di tengah atas layar; ikon tray ada; Keluar menutup proses sepenuhnya.
  2. Single instance aktif via named Mutex `Global\AdaraBox.SingleInstance`.
  3. Tombol Workspace/Media/Stats/Settings tampil dari daftar modul terurut; modul aktif disorot jelas.
  4. Panel melebar horizontal dengan animasi halus dan bisa ditutup dengan ✕ dan Esc.
  5. Media & Stats menampilkan badge jujur "Masih dalam pengembangan" tanpa kontrol palsu; gaya visual beda dari Error.
  6. Mode Hide: bar tersembunyi, hover strip memunculkan via dwell 250ms, keluar menyembunyikan; toggle di Settings dan tray bekerja.
  7. Top bar tidak mencuri fokus saat muncul (ShowActivated=False).
  8. Layout responsif dan manifest PerMonitorV2 terpasang.
  9. Layer Core tetap 100% bebas WPF/Win32/SQLite (ArchitectureTests hijau).
  10. Unhandled exception tercatat ke Serilog `%LOCALAPPDATA%\AdaraBox\logs\adarabox-.log` dan menampilkan dialog informatif.
- [HUMAN CHECK] yang menunggu: Uji visual checklist 8 skenario di `docs/manual-tests/M01.md`.
- Keputusan yang diambil (dan di mana dicatat):
  - Enkapsulasi `NotifyIcon` di dalam `AdaraBox.Platform` tanpa mencemari namespace WPF `AdaraBox.App` (ADR-009 di `docs/DECISIONS.md`).
  - Penggunaan `InMemorySettingsStore` pada tahap M01; persistensi preferensi ke SQLite dijadwalkan di M02 (ADR-010 di `docs/DECISIONS.md`).
- OPEN QUESTION: Q1 (Multi-monitor), Q2 (Shortcut keyboard default), Q3 (Startup behavior) diputuskan di M05; Q4 (Drag-and-drop) dievaluasi di M03.
- Masalah/utang teknis yang diketahui: Preferensi Hide belum bertahan setelah restart aplikasi karena store saat ini in-memory (ini disengaja sesuai scope M01; dihubungkan ke SQLite pada M02).

## Laporan M00 — 2026-10-31
- Status: SELESAI
- Task selesai: T1 (Inisialisasi repo), T2 (Solution & Proyek), T3 (CI GitHub Actions), T4 (Spike A: Window Top Bar), T5 (Spike B: Hover Trigger), T6 (Spike C: Launcher Resource), T7 (Spike D: SQLite & Dapper), T8 (ADR & Keputusan Teknis).
- Acceptance criteria: 5/5 terpenuhi:
  1. `dotnet build -c Release` dan `dotnet test -c Release` bersih (0 error, 0 warning; 13 unit tests passed).
  2. Test arsitektur lulus (`AdaraBox.Core` tidak bergantung ke WPF, SQLite, atau Win32).
  3. Hasil tertulis setiap spike lengkap di `docs/spike-window.md`, `docs/spike-launcher.md`, `docs/spike-sqlite.md`, dan `docs/DECISIONS.md`.
  4. Semua ADR (ADR-001 hingga ADR-008) tercatat berstatus ACCEPTED.
  5. Item [HUMAN CHECK] terdaftar di `PROGRESS.md`.
- [HUMAN CHECK] yang menunggu: Uji interaktif WPF Spike di `spikes/WindowSpike/bin/Release/net8.0-windows/WindowSpike.exe` (fokus, TextBox input, dan hover dwell trigger).
- Keputusan yang diambil (dan di mana dicatat):
  - Framework Desktop: C# 12 / .NET 8 WPF (ADR-001 di `docs/DECISIONS.md`).
  - Top Bar: Overlay Topmost `ShowActivated=False` tanpa menyisihkan ruang layar; dilarang pakai `WS_EX_NOACTIVATE` (ADR-002).
  - Hover Trigger: Strip 3 DIP, dwell 250 ms, delay sembunyi 600 ms, color `#01000000` (ADR-003, `UiConstants.cs`).
  - Launcher: `ProcessStartInfo` + `UseShellExecute = true`, auto `WorkingDirectory` untuk `.exe`, pemetaan Win32 Error Codes 2, 5, 1155, 1223 (ADR-004, `LauncherTests.cs`).
  - Persistensi: SQLite `%LOCALAPPDATA%\AdaraBox\adarabox.db`, WAL mode, `PRAGMA foreign_keys = ON`, `user_version` migration, Dapper TypeHandlers untuk Guid & DateTimeOffset (ADR-005, `SqliteTypeHandlers.cs`).
  - Versi Minimum Windows: Windows 10 21H2 (Build 19044+) & Windows 11 (ADR-006).
  - Tampilan Grid/List: Preferensi global `view.mode` (ADR-007).
  - Placeholder Media & Stats: Honest Placeholders status `InDevelopment` (ADR-008).
- OPEN QUESTION: Q1 (Multi-monitor), Q2 (Shortcut default), Q3 (Startup behavior), Q4 (Drag-and-drop).
- Masalah/utang teknis yang diketahui: Tidak ada. Semua dependensi mematuhi aturan arsitektur.
