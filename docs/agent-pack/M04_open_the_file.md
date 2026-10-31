# M04 — Open the File (Pembukaan Multi-Resource)

**Tujuan:** mode pemilihan beberapa resource dan membukanya sekaligus, dengan pelaporan sukses/gagal per item.
Setelah modul ini disetujui, **MVP fungsional tercapai**.
**Prasyarat:** M03 disetujui. **Referensi SRS:** §5.5, §7.3, §9.2; FR-OF-01…10, NFR-PF-03, NFR-SC-04, NFR-CP-01/02.

## Scope
**In:** mode Open the File (toggle, checkbox, counter, select all/none), `ResourceLauncher`, laporan hasil, pemisahan mode Kelola vs Buka.
**Out:** argumen command-line, urutan/penundaan peluncuran yang bisa diatur pengguna, "jalankan sebagai admin", launch profile, menutup aplikasi yang sudah dibuka.

## Model & Interface

```csharp
// AdaraBox.Core
public enum LaunchFailureReason { NotFound, AccessDenied, InvalidTarget, NoHandler, Cancelled, Unknown }

public sealed record LaunchResult(Guid ResourceId, string ResourceName, bool Success,
                                  LaunchFailureReason? Reason = null, string? Message = null);

public sealed record LaunchSummary(IReadOnlyList<LaunchResult> Results)
{
    public int SuccessCount => Results.Count(r => r.Success);
    public int FailureCount => Results.Count(r => !r.Success);
}

public interface IResourceLauncher
{
    Task<LaunchResult> LaunchAsync(Resource resource, CancellationToken ct = default);
}

public interface IMultiLaunchService
{
    // Membuka semua resource; satu kegagalan TIDAK menghentikan yang lain.
    Task<LaunchSummary> LaunchAllAsync(IReadOnlyList<Resource> resources,
                                       IProgress<LaunchResult>? progress = null,
                                       CancellationToken ct = default);
}

public interface IProcessStarter   // abstraksi agar launcher bisa dites tanpa membuka program sungguhan
{
    void Start(string fileName, string? workingDirectory);   // implementasi: Process.Start dgn UseShellExecute=true
}
```

## Aturan Launcher (`ResourceLauncher`, di Platform)

1. **Revalidasi** target dengan `ResourceTargetValidator` (M03) sebelum membuka; gagal → `InvalidTarget`.
2. Per jenis:
   - `Link` → `IProcessStarter.Start(uri.AbsoluteUri, null)` (browser default).
   - `File`/`Folder` → cek `File.Exists`/`Directory.Exists` dulu; tidak ada → `NotFound` (tanpa exception). Lalu `Start(path, null)`.
   - `Application` → cek exists (kecuali `shell:AppsFolder\…`); `Start(path, Path.GetDirectoryName(path))`.
3. **Dilarang:** `cmd /c`, `powershell`, `start` via shell, atau menggabungkan input pengguna ke dalam command string (Aturan Emas #3).
   `ProcessStartInfo.FileName` = target saja; `Arguments` kosong.
4. Tangkap `Win32Exception` dan petakan `NativeErrorCode` (hasil spike M00):
   `2`/`3` → `NotFound`; `5` → `AccessDenied`; `1155` → `NoHandler`; `1223` → `Cancelled` (pengguna menolak UAC); lainnya → `Unknown`.
   Tangkap juga `UnauthorizedAccessException`, `FileNotFoundException`, `InvalidOperationException`, `IOException`. **Jangan** menangkap `OutOfMemoryException`/exception fatal.
5. Pesan kegagalan (`LaunchResult.Message`) ramah dan dalam Bahasa Indonesia (`UiText`), tanpa stack trace; detail teknis hanya ke log.
6. `MultiLaunchService`: eksekusi **berurutan** di background (`Task.Run`) dengan jeda kecil antar item (default 150 ms, konstanta `UiConstants`/`LaunchOptions`)
   agar sistem tidak kewalahan; lapor progres per item lewat `IProgress<>`; dukung `CancellationToken`; satu item melempar exception → tetap lanjut item berikutnya (FR-OF-06).
7. Log (level Information) tiap peluncuran: jenis + hasil. **Jangan log path lengkap** di level Information (privasi); path boleh di level Debug.

## Task

### T1 — Core & Platform
Implementasi `MultiLaunchService` (Core, memakai `IResourceLauncher`) dan `ResourceLauncher` + `ShellProcessStarter` (Platform). Daftarkan di DI.

### T2 — State & ViewModel
1. `WorkspaceDetailViewModel` mendapat `Mode` ∈ {`Manage`, `Open`} (default `Manage`).
2. `ResourceItemViewModel.IsSelected` — **state UI sementara**, tidak disimpan ke database.
3. Properti turunan: `SelectedCount`, `CanOpen` (= mode Open && SelectedCount > 0 && tidak sedang meluncurkan), `IsLaunching`.
4. Perintah: `ToggleOpenModeCommand`, `OpenSelectedCommand`, `SelectAllCommand`, `ClearSelectionCommand`.
5. **FR-OF-08:** pilihan dibersihkan saat mode Open dimatikan, saat keluar dari workspace (Kembali), dan saat berganti workspace.
6. **FR-OF-05 (kritis):** saat mode `Open` aktif, perintah tambah/edit/hapus/urut **tidak tersedia** (disembunyikan atau `CanExecute=false`, dan tombolnya tidak dirender). Klik pada item di mode Open = toggle pilihan, bukan membuka editor.
7. Selama `IsLaunching`: tombol Open disabled, ada indikator progres, mode tidak bisa diganti.

### T3 — UI
1. Tombol **Open the File** (toggle) di header detail workspace — label persis "Open the File" (FR-OF-01).
2. Mode aktif: checkbox muncul di **grid dan list** (FR-OF-02, FR-VW-05); header berubah visual (aksen/warna) agar jelas sedang di mode Open (NFR-UX-02).
3. Bar aksi: teks "N dipilih", tombol **Open** (disabled bila 0, FR-OF-09), "Pilih semua", "Bersihkan".
4. Item `Missing`/`AccessDenied` tetap bisa dipilih tetapi diberi penanda peringatan; kegagalannya nanti dilaporkan di ringkasan.
5. **Ringkasan hasil** (panel atau dialog non-blocking) setelah selesai: "X berhasil, Y gagal"; daftar item gagal + alasan; item berhasil bisa dilipat. Tombol "Coba lagi yang gagal" (opsional, P1) hanya meluncurkan ulang item gagal.
6. Setelah ringkasan ditutup, mode Open tetap aktif sampai pengguna keluar sendiri; pilihan tetap sampai mode dimatikan (sesuai FR-OF-08).
7. Peringatan ringan (bukan blokir) bila memilih > 10 item: "Membuka banyak resource sekaligus dapat membebani komputer." (FR-OF-10).
8. Keyboard: `Space` toggle pilihan pada item terfokus, `Ctrl+A` pilih semua (di mode Open), `Enter` = Open bila `CanOpen`, `Esc` = keluar mode Open.

### T4 — Test
**Core.Tests (`MultiLaunchService` dengan `IResourceLauncher` fake):** semua sukses; satu gagal di tengah → sisanya tetap dicoba dan hasil memuat tepat 1 gagal; launcher melempar exception → dibungkus sebagai gagal `Unknown`, tidak bocor; urutan hasil = urutan input; cancel di tengah → item belum jalan tidak dieksekusi; progress dilaporkan tiap item.
**Platform.Tests (`ResourceLauncher` dengan `IProcessStarter` fake):** tiap jenis memanggil `Start` dengan argumen benar (path spasi/non-ASCII utuh, **tanpa** modifikasi atau quoting manual); `Win32Exception` kode 2/5/1155/1223 → alasan benar; target tidak ada → `NotFound` tanpa memanggil `Start`; URL skema `javascript:` → `InvalidTarget`; verifikasi tidak ada pemanggilan shell.
**ViewModel tests:** mode Open → aksi kelola tidak tersedia; keluar mode → selection kosong; `CanOpen` false saat 0 dipilih; tidak bisa Open dua kali bersamaan.
Tulis `docs/manual-tests/M04.md`: skenario nyata memakai file/folder/URL/aplikasi sungguhan **[HUMAN CHECK]**.

## Acceptance Criteria
- [ ] Mode Open the File bisa diaktifkan/dinonaktifkan; checkbox hanya muncul saat aktif, di grid maupun list.
- [ ] Memilih ≥ 2 resource lalu Open membuka semuanya lewat mekanisme Windows (file → app default, folder → Explorer, link → browser default, aplikasi → berjalan). **[HUMAN CHECK]**
- [ ] Satu resource gagal tidak menghentikan yang lain; ringkasan menampilkan item gagal + alasan yang dapat dipahami (FR-OF-06).
- [ ] Open dinonaktifkan saat tidak ada pilihan; counter benar (FR-OF-07/09).
- [ ] Di mode Open tidak ada cara menghapus/mengganti nama/mengedit tanpa sengaja (FR-OF-05).
- [ ] Pilihan terhapus saat mode dimatikan atau keluar workspace (FR-OF-08).
- [ ] UI tidak freeze saat membuka banyak item; aplikasi tidak crash karena satu target bermasalah (NFR-PF-03).
- [ ] Tidak ada pemanggilan shell/`cmd`/`powershell` di seluruh kodebase (grep sebagai bagian dari review).
- [ ] Acceptance criteria SRS §11 nomor 1-8 dan 11 dapat didemokan. Tandai di `PROGRESS.md`.

## Jangan
- Jangan menambahkan argumen, working directory khusus pengguna, atau "run as admin".
- Jangan menyimpan seleksi ke DB/settings.
- Jangan membuka resource otomatis saat workspace dibuka.
- Jangan mem-parse hasil atau memantau proses yang diluncurkan (di luar scope).
