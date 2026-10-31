# Adara Box — Master Guide untuk AI Agent

> **BACA FILE INI PERTAMA, setiap kali memulai sesi baru.**
> Lalu baca file modul yang ditugaskan (misal `M02_workspace_management.md`) dan `PROGRESS.md`.

## 1. Apa yang sedang dibangun

**Adara Box** = aplikasi desktop Windows dengan *top bar* kecil di atas layar sebagai akses cepat ke **Workspace**
(kumpulan resource: aplikasi, file, folder, tautan). Pengguna bisa memilih beberapa resource lalu membukanya sekaligus
lewat mode **Open the File**. Modul **Media** dan **Stats** adalah pasca-MVP (awalnya hanya placeholder jujur).

Sumber kebenaran kebutuhan: `docs/SRS.md` (= `Adara_Box_SRS_v0_2.md`). Jika file modul ini dan SRS bertentangan,
**berhenti dan tanyakan ke manusia** — jangan memilih sendiri.

## 2. Aturan Emas (tidak boleh dilanggar)

1. **Reference, not copy.** Jangan pernah menyalin/memindah/menghapus file asli milik pengguna. Hapus resource = hapus *baris referensi* di database saja.
2. **Jangan berpura-pura.** Fitur belum jadi harus tampil dengan status jelas ("Masih dalam pengembangan"), tombolnya disabled, dan tidak ada aksi palsu. Status pengembangan ≠ error.
3. **Tidak ada eksekusi shell.** Jangan pernah membangun command string untuk `cmd`/`powershell`. Buka resource lewat `Process.Start` dengan `UseShellExecute = true` dan `FileName` berisi target tervalidasi. Path/URL = data, bukan instruksi (NFR-SC-04).
4. **Satu kegagalan tidak boleh menjatuhkan yang lain.** Pembukaan banyak resource: tiap item punya try/catch sendiri, hasil dilaporkan per item.
5. **Tidak ada data keluar.** Aplikasi tidak melakukan request jaringan sendiri di MVP. Tidak ada telemetry.
6. **Jangan menimpa data diam-diam.** Database rusak → tampilkan error + backup, bukan buat ulang tanpa kabar.
7. **Jangan lompat modul.** Kerjakan hanya modul yang ditugaskan. Jangan menambah fitur di luar scope modul (Aturan "Out of scope" di tiap modul).
8. **Jangan menebak keputusan produk.** Jika ada hal yang belum diputuskan dan tidak tercantum di file modul → tulis di `docs/DECISIONS.md` sebagai "OPEN QUESTION" dan tanya manusia.

## 3. Tech Stack (default; dikunci di Modul M00)

| Aspek | Pilihan |
|---|---|
| Bahasa/Runtime | C# 12, .NET 8 (LTS) |
| UI | WPF, pola MVVM |
| MVVM helper | `CommunityToolkit.Mvvm` |
| DI | `Microsoft.Extensions.DependencyInjection` (+ `Hosting` bila perlu) |
| Database | SQLite via `Microsoft.Data.Sqlite` + `Dapper` |
| Logging | `Serilog` + `Serilog.Sinks.File` |
| Test | xUnit + NSubstitute (tanpa FluentAssertions; pakai `Assert` xUnit) |
| Target OS | Windows 10 21H2+ dan Windows 11 |
| Installer | Inno Setup (M06) |

Jika M00 membuktikan WPF tidak cocok, M00 harus menulis keputusan pengganti di `docs/DECISIONS.md`
dan **berhenti menunggu persetujuan manusia** sebelum lanjut.

## 4. Struktur Repo (target)

```
AdaraBox/
├─ AdaraBox.sln
├─ Directory.Build.props        # versi, nullable, analyzers, warnings-as-errors
├─ .editorconfig
├─ docs/
│  ├─ SRS.md                    # salinan SRS v0.2 (jangan diedit tanpa izin)
│  ├─ DECISIONS.md              # ADR ringkas + OPEN QUESTION
│  └─ agent-pack/               # file-file panduan ini
├─ src/
│  ├─ AdaraBox.Core/            # net8.0  — model, interface, service, validasi. TANPA WPF/SQLite/Win32
│  ├─ AdaraBox.Data/            # net8.0  — SQLite, migrasi, repository
│  ├─ AdaraBox.Platform/        # net8.0-windows — Win32 interop, launcher, ikon, hotkey, autostart
│  └─ AdaraBox.App/             # net8.0-windows (WPF) — View, ViewModel, tema, startup
├─ tests/
│  ├─ AdaraBox.Core.Tests/
│  ├─ AdaraBox.Data.Tests/
│  └─ AdaraBox.Platform.Tests/
├─ spikes/                      # kode eksperimen M00 (dibuang/diarsipkan setelah selesai)
├─ build/                       # script publish (M06)
└─ installer/                   # Inno Setup (M06)
```

**Aturan dependensi (wajib):**
`App → Core, Data, Platform` · `Data → Core` · `Platform → Core` · `Core → (tidak ke mana-mana)`.
`Core` dilarang mereferensikan WPF, SQLite, atau Win32. Pelanggaran = bug arsitektur.

## 5. Konvensi Kode

- `Nullable` enable, `TreatWarningsAsErrors` true.
- Nama: PascalCase untuk tipe/method, `_camelCase` untuk field privat, interface diawali `I`.
- Satu tipe publik per file. Namespace mengikuti folder.
- ViewModel tidak boleh mereferensikan tipe `System.Windows.*` selain lewat abstraksi (mis. `IDialogService`, `IFilePickerService`).
- **Code-behind** hanya untuk logika murni view (animasi, fokus). Tidak ada logika bisnis di code-behind.
- Operasi I/O (DB, disk, proses) **async**, tidak memblokir UI thread.
- Error yang *diharapkan* (nama kosong, file tidak ada) dikembalikan lewat `OperationResult` / `OperationResult<T>`, bukan exception.
- Teks UI Indonesia, disimpan terpusat di `AdaraBox.App/Resources/UiText.cs` (satu tempat, bukan hard-code tersebar).
- Komentar kode dalam Bahasa Inggris; identifier dalam Bahasa Inggris.
- Jangan tambah NuGet package di luar daftar tanpa mencatat alasannya di `docs/DECISIONS.md`.

### Tipe dasar bersama (dibuat di M00/M01, dipakai semua modul)

```csharp
// AdaraBox.Core
public sealed record OperationError(string Code, string Message);
public sealed record OperationResult(bool Success, OperationError? Error = null)
{
    public static OperationResult Ok() => new(true);
    public static OperationResult Fail(string code, string message) => new(false, new(code, message));
}
public sealed record OperationResult<T>(bool Success, T? Value = default, OperationError? Error = null)
{
    public static OperationResult<T> Ok(T value) => new(true, value);
    public static OperationResult<T> Fail(string code, string message) => new(false, default, new(code, message));
}

public enum ModuleStatus { Available, InDevelopment, NotAvailable, Unsupported, Error }
```

## 6. Alur Kerja Agent per Modul

1. Baca `00_MASTER_GUIDE.md` → file modul → `PROGRESS.md` → `docs/DECISIONS.md`.
2. Verifikasi lingkungan: `dotnet --info` (butuh SDK 8.x). Proyek WPF hanya bisa **dijalankan** di Windows; di non-Windows
   hanya bisa dibuild jika `EnableWindowsTargeting=true`. Jika tidak bisa menjalankan UI, kerjakan semua yang bisa diuji lewat unit test
   dan tandai item berlabel **[HUMAN CHECK]** sebagai "menunggu verifikasi manusia".
3. Kerjakan task **berurutan** sesuai nomor. Setelah tiap task: `dotnet build` dan `dotnet test` harus hijau.
4. Tulis test yang diminta modul. Jangan menghapus/men-skip test agar build hijau.
5. Cek **Acceptance Criteria** satu per satu. Centang di `PROGRESS.md` hanya yang benar-benar terverifikasi.
6. Commit per task logis, format: `feat(M02): create workspace service [FR-WS-02]`
   (tipe: `feat`, `fix`, `test`, `refactor`, `docs`, `chore`).
7. Di akhir modul: tulis **Laporan Modul** (format di bawah) ke `PROGRESS.md`, lalu **BERHENTI** dan tunggu persetujuan manusia.

### Format Laporan Modul (tempel di `PROGRESS.md`)

```
## Laporan M0X — <tanggal>
- Status: SELESAI | SELESAI DENGAN CATATAN | TERBLOKIR
- Task selesai: ...
- Acceptance criteria: x/y terpenuhi (sebutkan yang belum)
- [HUMAN CHECK] yang menunggu: ...
- Keputusan yang diambil (dan di mana dicatat): ...
- OPEN QUESTION: ...
- Masalah/utang teknis yang diketahui: ...
```

## 7. Definition of Done (berlaku untuk SETIAP modul)

- [ ] `dotnet build -c Release` bebas error dan warning.
- [ ] `dotnet test` hijau; ada test baru untuk logika baru.
- [ ] Tidak ada TODO tanpa nomor issue/penjelasan; tidak ada kode mati/komentar kode lama.
- [ ] Tidak ada hard-coded path pengguna, secret, atau string UI tersebar.
- [ ] Aturan Emas (bagian 2) tidak dilanggar.
- [ ] `PROGRESS.md` dan `docs/DECISIONS.md` diperbarui.
- [ ] Tidak ada fitur di luar scope modul.

## 8. Lokasi Data Aplikasi

| Apa | Path |
|---|---|
| Database | `%LOCALAPPDATA%\AdaraBox\adarabox.db` |
| Backup DB | `%LOCALAPPDATA%\AdaraBox\backups\` |
| Log | `%LOCALAPPDATA%\AdaraBox\logs\adarabox-.log` (rolling harian, simpan 14 hari) |

Tes tidak boleh menyentuh folder ini; pakai path temp (`Path.GetTempPath()`) atau SQLite in-memory/file sementara.

## 9. Urutan Modul

```
M00 Setup & Spikes ──► M01 App Shell ──► M02 Workspace ──► M03 Resource ──► M04 Open the File
                                                                                  │
                                       MVP FUNGSIONAL ◄───────────────────────────┘
                                             │
                          M05 Hardening ──► M06 Packaging & Release  (=MVP v1.0)
                                             │
                          (hanya setelah MVP lulus)  M07 Stats ──► M08 Media
```

Modul tidak boleh dikerjakan paralel atau dilompati, kecuali manusia menyatakan sebaliknya.

## 10. Template Prompt untuk Manusia (salin-tempel ke agent)

```
Kamu adalah AI agent yang membangun aplikasi Adara Box.
Baca docs/agent-pack/00_MASTER_GUIDE.md, lalu docs/agent-pack/PROGRESS.md,
lalu kerjakan modul: <M0X_nama_modul.md>.
Ikuti semua aturan di master guide. Jangan keluar dari scope modul.
Jika ada konflik atau keputusan yang belum jelas, berhenti dan tanyakan.
Setelah selesai, tulis Laporan Modul di PROGRESS.md dan berhenti.
```
