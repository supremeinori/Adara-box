# Decisions — Adara Box

Dokumen ini mencatat Architectural Decision Records (ADR) dan Open Questions resmi untuk proyek Adara Box.

---

## ADR-001: Framework Desktop Final — .NET 8 & WPF
* **Tanggal:** 2026-10-31  
* **Status:** ACCEPTED  
* **Konteks:** SRS §9.1 dan §16 butir 9 merekomendasikan evaluasi C# / WPF vs web-based (Tauri/Electron). Adara Box membutuhkan integrasi native Windows yang mendalam (top bar topmost, Win32 interop, ShellExecute, DPI PerMonitorV2, dan performa startup cepat tanpa overhead browser engine ganda).  
* **Keputusan:** Mengunci framework desktop utama menggunakan **C# 12 / .NET 8 (LTS) dan WPF**, dengan pola arsitektur **MVVM** dibantu `CommunityToolkit.Mvvm`.  
* **Konsekuensi:**  
  * Mendukung penuh integrasi Windows API dan per-monitor DPI scaling secara native.  
  * Runtime dependensi mengikuti standar .NET 8 desktop runtime di Windows.  
  * Arsitektur modular terpisah (`Core`, `Data`, `Platform`, `App`).

---

## ADR-002: Perilaku Top Bar — Overlay Topmost vs AppBar
* **Tanggal:** 2026-10-31  
* **Status:** ACCEPTED  
* **Konteks:** SRS §3.1 dan Modul M00 Task T4 menguji dua pendekatan penempatan top bar: Varian 1 (Overlay Topmost) dan Varian 2 (Win32 AppBar via `SHAppBarMessage`).  
* **Temuan Spike A (`docs/spike-window.md`):**  
  1. *AppBar* menyisihkan desktop work area secara permanen, memaksa semua aplikasi lain yang dimaksimalkan tergeser turun. Jika aplikasi crash/unclean exit, desktop layout pengguna tertinggal dalam keadaan rusak hingga Explorer di-restart.  
  2. *Overlay Topmost* (`Topmost=True`, `ShowActivated=False`, `AllowsTransparency=True`) mengambang bersih di atas konten tanpa mencuri fokus aktif keyboard dari aplikasi lain saat muncul.  
  3. Gaya `WS_EX_NOACTIVATE` terbukti merusak kapabilitas input teks (TextBox tidak bisa diketik); oleh karena itu `WS_EX_NOACTIVATE` **tidak boleh digunakan**. Fokus keyboard harus ditangani secara alami melalui event fokus WPF.  
* **Keputusan:** Memilih **Varian 1: Overlay Topmost** (`ShowActivated=False`, tidak menyisihkan ruang layar).  
* **Konsekuensi:** Top bar ringan, tidak mengubah tata letak workspace pengguna, dan ramah terhadap multi-tasking.

---

## ADR-003: Parameter Hover Trigger Strip (Top Bar Tersembunyi)
* **Tanggal:** 2026-10-31  
* **Status:** ACCEPTED  
* **Konteks:** SRS §3.1 (FR-TB-04) dan M00 Task T5 mewajibkan mekanisme pemunculan top bar saat opsi `Hide` aktif tanpa terjadi pemicuan tidak sengaja saat pengguna menyeret jendela lain ke atas.  
* **Temuan Spike B (`docs/spike-window.md`):**  
  * Window transparan 100% (`#00000000`) diabaikan oleh hit-test WPF. Diperlukan brush transparan minimal `#01000000` (alpha=1/255).  
  * Dwell time <200ms terlalu sensitif; dwell time >400ms terasa lambat. Angka optimal adalah 250 ms.  
  * Hide delay 600 ms memberikan toleransi gerakan kursor tanpa efek flicker.  
* **Keputusan:** Mengunci parameter hover di `AdaraBox.Core.Common.UiConstants`:  
  * `HoverStripHeightDip = 3.0`  
  * `HoverDwellTimeMs = 250`  
  * `HoverHideDelayMs = 600`  
  * `HitTestableTransparentColorHex = "#01000000"`  
* **Konsekuensi:** Pengalaman pengguna saat memunculkan top bar terasa responsif dan bebas dari pemicuan palsu (*accidental triggers*).

---

## ADR-004: Mekanisme Membuka Resource & Pemetaan Error Win32
* **Tanggal:** 2026-10-31  
* **Status:** ACCEPTED  
* **Konteks:** Master Guide Aturan Emas #3, SRS §9.2, dan NFR-SC-04 melarang penggunaan command string shell (`cmd /c` atau `powershell`). Pembukaan resource harus aman dan menangani error secara per-item (FR-OF-06).  
* **Temuan Spike C (`docs/spike-launcher.md`):**  
  * `Process.Start(new ProcessStartInfo(target) { UseShellExecute = true })` berhasil membuka seluruh jenis resource: file (termasuk path spasi & Unicode), folder, URL, executable, shortcut `.lnk`, dan Windows Store apps (`shell:AppsFolder\...`).  
  * Untuk target `.exe`, `WorkingDirectory` wajib disetel ke direktori file executable agar aplikasi target dapat memuat dependensi lokalnya.  
  * Exception `Win32Exception` melempar `NativeErrorCode` yang dapat dipetakan secara presisi.  
* **Keputusan:**  
  * Seluruh pembukaan resource menggunakan `ProcessStartInfo` + `UseShellExecute = true`.  
  * Pemetaan kode error:  
    * `2 (ERROR_FILE_NOT_FOUND)` → `ResourceNotFound` ("Target tidak ditemukan di path yang ditentukan.")  
    * `5 (ERROR_ACCESS_DENIED)` → `AccessDenied` ("Akses ke file/folder ditolak.")  
    * `1155 (ERROR_NO_ASSOCIATION)` → `NoAssociation` ("Tidak ada aplikasi default untuk jenis file ini.")  
    * `1223 (ERROR_CANCELLED)` → `CancelledByUser` ("Tindakan dibatalkan oleh pengguna.")  
    * Kode lain → `LaunchFailed` ("Gagal membuka resource: {error}").  
* **Konsekuensi:** Keamanan terjamin (path adalah data, bukan kode yang dieksekusi) dan pelaporan error per-item di mode *Open the File* (M04) menjadi akurat.

---

## ADR-005: Strategi Penyimpanan Lokal dengan SQLite & Dapper
* **Tanggal:** 2026-10-31  
* **Status:** ACCEPTED  
* **Konteks:** SRS §8, §9.1, NFR-RL-01..04, dan M00 Task T7 membutuhkan persistensi lokal yang andal, tahan korupsi, dan mendukung konkurensi (UI thread dan background tasks).  
* **Temuan Spike D (`docs/spike-sqlite.md`):**  
  * Mode WAL (`PRAGMA journal_mode = WAL;`) memungkinkan pembacaan bersamaan tanpa memblokir thread UI.  
  * `PRAGMA foreign_keys = ON;` wajib dieksekusi setiap pembukaan koneksi.  
  * `PRAGMA user_version` dapat digunakan untuk pelacakan migrasi skema database yang ringan dan deterministik.  
  * Dapper memetakan `Guid` dan `DateTimeOffset` ke kolom `TEXT` secara konsisten jika TypeHandler mendaftarkan string uppercase untuk Guid (sesuai format internal `Microsoft.Data.Sqlite`) dan ISO-8601 untuk `DateTimeOffset`.  
* **Keputusan:**  
  * Database disimpan di `%LOCALAPPDATA%\AdaraBox\adarabox.db`.  
  * Koneksi SQLite selalu mengaktifkan `foreign_keys = ON` dan `journal_mode = WAL`.  
  * Pelacakan skema menggunakan `PRAGMA user_version`.  
  * Dapper TypeHandler (`SqliteTypeHandlers.Register()`) menjadi standar pemetaan tipe data.  
* **Konsekuensi:** Data lokal aman dari korupsi parsial (**NFR-RL-04**) dan bebas dari dependensi server eksternal.

---

## ADR-006: Versi Minimum Sistem Operasi yang Didukung
* **Tanggal:** 2026-10-31  
* **Status:** ACCEPTED  
* **Konteks:** SRS §2.4, §16 butir 7, dan NFR-CP-04 meminta penentuan versi minimum Windows sebelum MVP dirilis.  
* **Keputusan:** Menetapkan batas minimum dukungan resmi pada **Windows 10 versi 21H2 (Build 19044) atau lebih tinggi**, serta **Windows 11** (semua edisi).  
* **Alasan:**  
  1. Build 19044+ memiliki implementasi API DPI awareness `PerMonitorV2` yang matang dan stabil.  
  2. Versi ini didukung penuh oleh runtime .NET 8 LTS.  
  3. Versi Windows 10 sebelum 21H2 sudah mencapai *End of Servicing* (EoS) resmi Microsoft.  
* **Konsekuensi:** Installer (M06) akan memvalidasi minimum OS build ≥ 19044.

---

## ADR-007: Lingkup Preferensi Tampilan Grid/List
* **Tanggal:** 2026-10-31  
* **Status:** ACCEPTED  
* **Konteks:** SRS §16 butir 4 mempertanyakan apakah preferensi tampilan Grid vs List disimpan per-workspace atau secara global.  
* **Keputusan:** Preferensi mode tampilan disimpan secara **global** di konfigurasi aplikasi (setting key: `view.mode`, nilai: `Grid` atau `List`).  
* **Alasan:** Pengguna umumnya memiliki preferensi kebiasaan visual yang konsisten saat bernavigasi antar-workspace; penyimpanan global menyederhanakan antarmuka dan konsistensi UX pada MVP.  
* **Konsekuensi:** Mengubah mode tampilan di satu workspace langsung berlaku secara global ke seluruh workspace.

---

## ADR-008: Strategi Placeholder untuk Modul Media & Stats
* **Tanggal:** 2026-10-31  
* **Status:** ACCEPTED  
* **Konteks:** SRS §3.4, §3.5, §16 butir 8, dan Aturan Emas #2 mengatur status modul Media dan Stats pada tahap MVP.  
* **Keputusan:** Panel **Media** dan **Stats** ditampilkan sejak MVP sebagai **Honest Placeholders** (status: `ModuleStatus.InDevelopment` / "Masih dalam pengembangan").  
* **Aturan Implementasi:**  
  1. Seluruh tombol atau kontrol interaktif palsu di-disable / ditiadakan.  
  2. Teks label menyatakan secara eksplisit: "Masih dalam pengembangan".  
  3. Status pengembangan tidak boleh dilaporkan sebagai kesalahan (*Error*).  
  4. Tidak ada integrasi Spotify API atau polling sistem berat di MVP.  
* **Konsekuensi:** Transparansi penuh kepada pengguna sesuai filosofi Adara Box tanpa membebani jadwal MVP.

---

## OPEN QUESTIONS

| # | Pertanyaan | Modul Terkait | Target Keputusan | Catatan |
|---|---|---|---|---|
| Q1 | Bagaimana perilaku top bar pada setup multi-monitor (apakah muncul di primary monitor saja atau di monitor tempat kursor berada)? | M01 / M05 | M05 (Hardening) | Default awal MVP: Primary monitor. Multi-monitor tracking dievaluasi di M05. |
| Q2 | Apa shortcut keyboard default untuk toggle/fokus top bar (misal `Win+Alt+A` atau `Ctrl+Alt+Space`) agar tidak konflik dengan hotkey Windows? | M01 / M05 | M05 (Hardening) | Akan diuji konflik hotkey di M05. |
| Q3 | Bagaimana preferensi startup awal (Run on Windows Startup, Start minimized to tray, atau langsung Always Visible)? | M01 / M05 | M05 (Hardening) | Integrasi registry Run key diuji di M05. |
| Q4 | Apakah drag-and-drop dari File Explorer langsung ke Workspace didukung pada MVP atau pasca-MVP? | M03 | M03 (Resource) | Prioritas picker standar dahulu; DnD dievaluasi sebagai enhancement P1 di M03. |
