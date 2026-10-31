# PROGRESS — Adara Box

> File ini **dibaca dan diperbarui oleh agent** di setiap sesi. Manusia mengubah kolom "Persetujuan".
> Centang hanya setelah benar-benar terverifikasi (test hijau atau [HUMAN CHECK] selesai).

## Status Modul

| Modul | Nama | Status | Persetujuan manusia |
|---|---|---|---|
| M00 | Setup & Spikes | ✔ selesai | ☐ |
| M01 | App Shell | ✔ selesai | ☐ |
| M02 | Workspace Management | ✔ selesai | ☐ |
| M03 | Resource Management | ☐ belum | ☐ |
| M04 | Open the File | ☐ belum | ☐ |
| — | **MVP fungsional** (setelah M04) | ☐ | ☐ |
| M05 | Hardening | ☐ belum | ☐ |
| M06 | Packaging & Rilis (v1.0.0) | ☐ belum | ☐ |
| M07 | Stats (pasca-MVP) | ☐ belum | ☐ |
| M08 | Media (pasca-MVP) | ☐ belum | ☐ |

Status: ☐ belum · ◐ sedang dikerjakan · ✔ selesai · ⛔ terblokir

## Menunggu Verifikasi Manusia ([HUMAN CHECK])

| Modul | Deskripsi | Langkah Uji / Referensi |
|---|---|---|
| M00 | Verifikasi visual interaktif Top Bar (Overlay Topmost vs AppBar) & Hover Trigger Strip | Jalankan `spikes/WindowSpike/bin/Release/net8.0-windows/WindowSpike.exe`, ikuti 4 skenario di `docs/spike-window.md` (uji pencurian fokus, input textbox, space reservation AppBar, dan dwell timer hover). |
| M01 | Verifikasi visual Application Shell (Startup tanpa window biasa, Top Bar compact 36 DIP, ekspansi panel horizontal animasi, honest placeholder Media & Stats, Esc & Close button, toggle Hide via Settings & Tray, hover trigger strip, dan single instance) | Jalankan `src/AdaraBox.App/bin/Release/net8.0-windows/AdaraBox.App.exe`, ikuti 8 skenario di `docs/manual-tests/M01.md`. |
| M02 | Verifikasi visual dan fungsional Workspace Management & Persistensi (Inisialisasi SQLite WAL & migrasi awal, tombol data-driven `+ Workspace`, form inline buat/ganti nama dengan validasi, duplikat nama diizinkan, dialog konfirmasi hapus non-destructive, tampilan detail workspace FR-WS-05, serta persistensi preferensi Hide dan data workspace setelah restart) | Jalankan `src/AdaraBox.App/bin/Release/net8.0-windows/AdaraBox.App.exe`, ikuti 9 skenario di `docs/manual-tests/M02.md`. |

## OPEN QUESTIONS

| # | Pertanyaan | Modul | Jawaban manusia |
|---|---|---|---|
| Q1 | Bagaimana perilaku top bar pada setup multi-monitor (apakah muncul di primary monitor saja atau di monitor aktif tempat kursor berada)? | M01 / M05 | (Menunggu jawaban di M05) |
| Q2 | Apa shortcut keyboard default untuk toggle/fokus top bar (misal `Win+Alt+A` atau `Ctrl+Alt+Space`) agar tidak konflik dengan hotkey Windows? | M01 / M05 | (Menunggu jawaban di M05) |
| Q3 | Bagaimana preferensi startup awal (Run on Windows Startup, Start minimized to tray, atau langsung Always Visible)? | M01 / M05 | (Menunggu jawaban di M05) |
| Q4 | Apakah drag-and-drop dari File Explorer langsung ke Workspace didukung pada MVP atau pasca-MVP? | M03 | (Menunggu evaluasi di M03) |

## Acceptance Criteria MVP (SRS §11) — dibuktikan di M04/M05

- [ ] 1. Pengguna dapat membuat dan membuka beberapa workspace.
- [ ] 2. Workspace dapat menyimpan referensi aplikasi, file, folder, dan tautan.
- [ ] 3. Resource ditampilkan dalam grid maupun list.
- [ ] 4. Ganti tampilan tanpa kehilangan data.
- [ ] 5. Open the File memungkinkan pemilihan dan pembukaan beberapa resource.
- [ ] 6. Kegagalan satu resource tidak menghentikan resource lain.
- [ ] 7. Menghapus resource dari Adara Box tidak menghapus file/folder asli.
- [ ] 8. Data workspace tetap ada setelah aplikasi dimulai ulang.
- [ ] 9. Top bar dapat selalu terlihat atau disembunyikan sesuai pengaturan.
- [ ] 10. Modul yang belum diimplementasikan menampilkan status jelas dan tidak berpura-pura berfungsi.
- [ ] 11. Aplikasi tetap berguna untuk Workspace tanpa Spotify, browser mini, atau Stats.

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
