# M00 — Setup Proyek & Spike Teknis

**Tujuan:** menyiapkan fondasi repo dan membuktikan bagian paling berisiko *sebelum* menulis kode produksi.
**Prasyarat:** tidak ada. **Referensi SRS:** §3.1, §9, §16 (keputusan terbuka), NFR-MN-01.
**Dikunci di modul ini:** framework final, perilaku dasar top bar (overlay vs AppBar), strategi membuka resource.

## Scope

**In:** struktur solution, konfigurasi build, CI dasar, 4 spike (window, hover trigger, launcher, SQLite), dokumen keputusan.
**Out:** fitur produksi apa pun, UI final, logika workspace/resource.

## Task

### T1 — Inisialisasi repo
1. `git init`, buat `.gitignore` (template Visual Studio/.NET), `.editorconfig`.
2. Buat `docs/` dan salin SRS ke `docs/SRS.md`; salin file-file agent pack ke `docs/agent-pack/`.
3. Buat `docs/DECISIONS.md` dengan format:
   ```
   # Decisions
   ## ADR-001 <judul> — <tanggal> — STATUS: ACCEPTED|PROPOSED
   Konteks / Keputusan / Konsekuensi
   ## OPEN QUESTIONS
   - Q1 ...
   ```
4. Buat `Directory.Build.props`:
   ```xml
   <Project>
     <PropertyGroup>
       <LangVersion>12</LangVersion>
       <Nullable>enable</Nullable>
       <ImplicitUsings>enable</ImplicitUsings>
       <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
       <EnableWindowsTargeting>true</EnableWindowsTargeting>
       <Version>0.1.0</Version>
     </PropertyGroup>
   </Project>
   ```

### T2 — Solution & proyek
Buat sesuai struktur di Master Guide §4:
- `AdaraBox.Core` (classlib, `net8.0`), `AdaraBox.Data` (classlib, `net8.0`),
  `AdaraBox.Platform` (classlib, `net8.0-windows`), `AdaraBox.App` (WPF, `net8.0-windows`).
- Tiga proyek test xUnit. Test project Platform/App-related target `net8.0-windows`.
- Referensi proyek sesuai aturan dependensi. **Tambahkan test arsitektur** (`AdaraBox.Core.Tests/ArchitectureTests.cs`)
  yang gagal bila `AdaraBox.Core` mereferensikan assembly `PresentationFramework`, `Microsoft.Data.Sqlite`, atau `AdaraBox.Platform`/`Data`
  (cek via `typeof(...).Assembly.GetReferencedAssemblies()`).
- Package: `CommunityToolkit.Mvvm`, `Microsoft.Extensions.DependencyInjection` (App); `Microsoft.Data.Sqlite`, `Dapper` (Data); `Serilog`, `Serilog.Sinks.File` (App); `xunit`, `NSubstitute` (tests).
- Taruh tipe dasar `OperationResult`, `OperationResult<T>`, `ModuleStatus` (lihat Master Guide §5) di `AdaraBox.Core/Common/`.

### T3 — CI
Buat `.github/workflows/ci.yml` (runner `windows-latest`): restore → build Release → test. Cache NuGet.

### T4 — Spike A: Window top bar
Lokasi: `spikes/WindowSpike/` (WPF kecil, tidak masuk solution utama atau masuk folder `spikes` di solution).
Buat **dua varian**, lalu catat perbandingannya:
- **Varian 1 — Overlay topmost:** `WindowStyle=None`, `AllowsTransparency=True`, `Topmost=True`, `ShowInTaskbar=False`,
  `ResizeMode=NoResize`, `ShowActivated=False`, posisi di tengah atas layar, tinggi ±36 DIP.
- **Varian 2 — AppBar:** daftarkan lewat `SHAppBarMessage` (`ABM_NEW`, `ABM_QUERYPOS`, `ABM_SETPOS`) agar ruang layar disisihkan.

Uji dan catat (tulis tabel di `docs/DECISIONS.md`):
- Apakah mencuri fokus dari aplikasi lain saat muncul?
- Apakah tetap di atas app fullscreen/video? (catat sebagai keterbatasan, jangan dipaksa.)
- Apakah text input (TextBox) tetap bisa diketik saat window aktif? Jika `WS_EX_NOACTIVATE` dipakai, input teks rusak — tulis temuannya.
- Perilaku pada DPI 100/125/150% (`app.manifest` dengan `PerMonitorV2`).
- **[HUMAN CHECK]** semua pengamatan visual; agent menyiapkan skenario uji dan form catatan di `docs/spike-window.md`.

**Default yang disarankan** bila tidak ada bukti sebaliknya: **Varian 1 (overlay topmost, tidak menyisihkan ruang layar)**.

### T5 — Spike B: Hover trigger (top bar tersembunyi)
- Buat window "trigger strip": lebar = lebar bar, tinggi 2-4 px, di tepi atas layar, `Topmost`, background `#01000000`
  (hampir transparan tapi tetap bisa menerima mouse; window 100% transparan tidak menerima hit-test).
- Mouse masuk strip + dwell ≥ 250 ms → munculkan top bar. Mouse keluar dari bar > 600 ms → sembunyikan lagi.
- Catat parameter (dwell, delay sembunyi, tinggi strip) sebagai konstanta di satu file `UiConstants`.
- **[HUMAN CHECK]** uji agar tidak muncul tidak sengaja saat menyeret window/menu ke atas layar.

### T6 — Spike C: Membuka resource
Lokasi: `spikes/LauncherSpike/` (console atau tes manual). Verifikasi `Process.Start(new ProcessStartInfo(target) { UseShellExecute = true })` untuk:
| Jenis | Contoh |
|---|---|
| File | `.txt`, `.pdf`, file dengan spasi dan karakter non-ASCII (`C:\Uji Coba\Dokumén ü.txt`) |
| Folder | folder biasa, folder dengan spasi |
| Link | `https://example.com` |
| Aplikasi | `notepad.exe`, shortcut `.lnk`, app Store via `shell:AppsFolder\<AUMID>` |
Catat: exception apa yang muncul (`Win32Exception.NativeErrorCode`) untuk 2 = tidak ditemukan, 5 = akses ditolak,
1155 = tidak ada handler/asosiasi, 1223 = dibatalkan pengguna (UAC). Peta kode ini akan dipakai di M04.
Catat juga: apakah `WorkingDirectory` perlu diset ke folder executable.

### T7 — Spike D: SQLite
- Buka koneksi `Microsoft.Data.Sqlite`, `PRAGMA foreign_keys=ON`, buat tabel contoh, tes transaksi rollback, tes `PRAGMA user_version`.
- Tes path DB mengandung spasi/non-ASCII. Tes dua koneksi bersamaan (UI + background) dengan `PRAGMA journal_mode=WAL`.
- Konfirmasi: Dapper bisa memetakan `Guid` ↔ `TEXT` (daftarkan `TypeHandler`) dan `DateTimeOffset` ↔ ISO-8601 `TEXT`.

### T8 — Kunci keputusan
Perbarui `docs/DECISIONS.md` dengan ADR untuk:
1. Framework final (default: WPF/.NET 8).
2. Top bar: overlay vs AppBar (+ alasan dan temuan spike).
3. Trigger hover: parameter final.
4. Cara membuka resource + peta kode error.
5. Strategi data (SQLite+Dapper, lokasi file, WAL, `user_version` untuk migrasi).
6. Versi Windows minimum (default: Windows 10 21H2 / build 19044).
7. Keputusan SRS §16 nomor 4 dan 8: **preferensi grid/list = global (setting `view.mode`)**; **Media & Stats tampil sebagai placeholder sejak MVP**.
8. Daftar OPEN QUESTION yang tersisa (multi-monitor, shortcut default, startup behavior → diputuskan di M05).

## Acceptance Criteria
- [ ] `dotnet build` dan `dotnet test` hijau dari repo bersih; CI hijau.
- [ ] Test arsitektur lulus (Core tidak bergantung ke WPF/SQLite/Win32).
- [ ] Setiap spike punya hasil tertulis (bisa/tidak + catatan) di `docs/DECISIONS.md` atau `docs/spike-*.md`.
- [ ] Semua ADR di T8 berstatus ACCEPTED, atau PROPOSED dengan pertanyaan jelas untuk manusia.
- [ ] Item **[HUMAN CHECK]** terdaftar di `PROGRESS.md`.

## Jangan
- Jangan menulis UI produksi atau repository produksi di modul ini.
- Jangan memakai AppBar hanya karena "lebih keren"; pilih berdasarkan temuan.
- Jangan menjalankan perintah shell untuk membuka resource di spike (`cmd /c start` dilarang).

## Output
Repo siap pakai + `docs/DECISIONS.md` terisi + laporan modul. **Berhenti** menunggu persetujuan keputusan teknis.
