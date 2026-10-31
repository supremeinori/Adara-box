# M03 — Resource Management & Tampilan Grid/List

**Tujuan:** mengelola resource (aplikasi, file, folder, tautan) di dalam workspace dan menampilkannya dalam grid atau list.
**Prasyarat:** M02 disetujui. **Referensi SRS:** §5.3, §5.4, §7.2, §7.4, §8.2; FR-RS-01…09, FR-VW-01…05, NFR-CP-02, NFR-SC-02/04.

## Scope
**In:** model/repository/service resource, validasi target, dialog tambah/edit, hapus referensi, indikator ketersediaan, ikon, grid & list, preferensi tampilan global, pengurutan manual sederhana.
**Out:** membuka resource & checkbox (M04), drag-and-drop, kategori/folder virtual (pasca-MVP), argumen command-line untuk aplikasi.

## Model & Interface (AdaraBox.Core)

```csharp
public enum ResourceType { Application, File, Folder, Link }
public enum ResourceAvailability { Unknown, Available, Missing, AccessDenied, Invalid }

public sealed record Resource(Guid Id, Guid WorkspaceId, string Name, ResourceType Type,
                              string Target, int SortOrder, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);

public interface IResourceRepository
{
    Task<IReadOnlyList<Resource>> GetByWorkspaceAsync(Guid workspaceId, CancellationToken ct = default);
    Task<Resource?> GetAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(Resource resource, CancellationToken ct = default);
    Task AddRangeAsync(IEnumerable<Resource> resources, CancellationToken ct = default);   // satu transaksi
    Task UpdateAsync(Guid id, string name, string target, DateTimeOffset updatedAt, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
    Task ReorderAsync(Guid workspaceId, IReadOnlyList<Guid> orderedIds, CancellationToken ct = default); // satu transaksi
    Task<int> GetNextSortOrderAsync(Guid workspaceId, CancellationToken ct = default);
}

public interface IResourceService
{
    Task<IReadOnlyList<Resource>> ListAsync(Guid workspaceId, CancellationToken ct = default);
    Task<OperationResult<Resource>> AddAsync(Guid workspaceId, ResourceType type, string target, string? displayName, CancellationToken ct = default);
    Task<OperationResult<IReadOnlyList<Resource>>> AddManyAsync(Guid workspaceId, ResourceType type, IEnumerable<string> targets, CancellationToken ct = default);
    Task<OperationResult> UpdateAsync(Guid id, string name, string target, CancellationToken ct = default);
    Task<OperationResult> RemoveAsync(Guid id, CancellationToken ct = default);   // HANYA hapus referensi
    Task<OperationResult> MoveAsync(Guid id, int direction, CancellationToken ct = default); // -1 naik, +1 turun
}

public interface IResourceAvailabilityChecker   // implementasi di Platform
{
    Task<ResourceAvailability> CheckAsync(Resource resource, CancellationToken ct = default);
}
```

## Aturan Validasi Target (`ResourceTargetValidator`, di Core, murni & teruji)

| Type | Aturan |
|---|---|
| `Link` | Trim. Jika tanpa skema (mis. `example.com`) → normalisasi ke `https://example.com`. Harus `Uri.TryCreate(..., UriKind.Absolute)` dan skema **hanya `http`/`https`**. Tolak `file:`, `javascript:`, `ms-…:`, dll. Panjang maks 2048. |
| `File` / `Folder` | Tidak kosong; path absolut (`Path.IsPathFullyQualified`); tidak mengandung karakter ilegal (`Path.GetInvalidPathChars`). **Tidak** mensyaratkan file benar-benar ada saat disimpan (bisa di drive eksternal); keberadaan dicek terpisah sebagai *availability*. |
| `Application` | Seperti File, plus ekstensi ∈ {`.exe`, `.lnk`} **atau** diawali `shell:AppsFolder\` (app Store). Tanpa argumen command-line di MVP. |

Nama tampilan: trim, wajib tidak kosong, maks 80 karakter. Default jika kosong: nama file tanpa ekstensi / nama folder / host URL.
Kode error: `resource.target.empty`, `resource.target.invalid`, `resource.type.mismatch`, `resource.name.empty`, `resource.name.too_long`, `resource.not_found`, `workspace.not_found`.

Duplikat target dalam workspace yang sama **diizinkan** (pengguna bisa sengaja), tetapi UI memberi petunjuk halus "sudah ada di workspace ini" saat menambah.

## Task

### T1 — Data
1. Implementasi `SqliteResourceRepository` (tabel sudah ada dari M02; **tidak perlu migrasi baru** kecuali menemukan kekurangan — jika perlu, buat migrasi #2, jangan edit #1).
2. `AddRangeAsync` dan `ReorderAsync` dalam satu transaksi.
3. Query `GetByWorkspaceAsync` urut `sort_order`, lalu `created_at`.

### T2 — Service & validator (Core)
Implementasi `ResourceService` + `ResourceTargetValidator` sesuai tabel di atas. `RemoveAsync` **hanya** memanggil repository (Aturan Emas #1).
Update `WorkspaceService.DeleteAsync` agar dialog konfirmasi bisa menampilkan jumlah resource (tambahkan `GetResourceCountAsync` atau sejenisnya).

### T3 — Availability & ikon (AdaraBox.Platform)
1. `ResourceAvailabilityChecker`: `File` → `File.Exists`; `Folder` → `Directory.Exists`; `Application` → exists (untuk `shell:AppsFolder\…` → `Unknown`);
   `Link` → `Unknown` (tidak ada request jaringan, Aturan Emas #5). Tangkap `UnauthorizedAccessException` → `AccessDenied`.
   **Beri timeout 2 detik** per cek (path jaringan/drive terputus bisa menggantung) → hasil `Unknown`. Cek dijalankan di background, paralel terbatas (maks 8).
2. `IIconProvider`: ambil ikon via `SHGetFileInfo` atau `Icon.ExtractAssociatedIcon` → `BitmapSource` (frozen). Cache per path/ekstensi. Ikon generik untuk folder, tautan (globe), dan fallback saat gagal. Jangan memblokir UI thread.

### T4 — Tampilan detail workspace
Lanjutkan kerangka M02:
1. `WorkspaceDetailViewModel`: judul, daftar `ResourceItemViewModel` (Name, Type, Target, Icon, Availability), state kosong ("Workspace ini masih kosong. Tambahkan resource pertama."), tombol Kembali.
2. **Add Resource:** tombol "Tambah". Alur: pilih jenis → input sesuai jenis:
   - File: `OpenFileDialog` (`Multiselect=true` → `AddManyAsync`, FR-RS-08).
   - Folder: `OpenFolderDialog` (.NET 8).
   - Aplikasi: `OpenFileDialog` filter `*.exe;*.lnk`.
   - Tautan: TextBox URL (+ nama opsional).
   Semua lewat abstraksi `IFilePickerService` / `IDialogService` agar bisa dites.
3. **Edit:** ubah nama tampilan dan target (validasi sesuai jenis; jenis tidak bisa diubah).
4. **Hapus:** konfirmasi singkat: "Hapus dari workspace? File aslinya tidak akan dihapus."
5. **Urutkan:** tombol naik/turun pada item (mode kelola). Drag-and-drop **tidak** dikerjakan.
6. **Availability:** setelah daftar tampil, jalankan cek async; item `Missing`/`AccessDenied` diberi ikon peringatan + tooltip jelas ("Target tidak ditemukan di lokasi ini"). **Jangan** menghapus/menyembunyikan item otomatis (FR-RS-07). Sediakan aksi "Cek ulang".
7. Tiap item menampilkan nama + info pembeda (path singkat / host URL) agar item sejenis bisa dibedakan (FR-RS-04); tooltip = target lengkap.

### T5 — Grid & List
1. Toggle Grid/List di header detail workspace. Preferensi global disimpan di `SettingKeys.ViewMode`.
2. **Grid:** `ItemsControl` + `WrapPanel` (kartu ikon besar + nama, 2 baris maksimum dengan ellipsis).
   **List:** satu baris per item (ikon kecil, nama, jenis, target ringkas).
3. Keduanya memakai **koleksi & ViewModel yang sama** (FR-VW-03) dan **fungsi yang sama** (FR-VW-05): tambah/edit/hapus/urut tersedia di kedua mode.
4. Virtualisasi: untuk daftar besar gunakan `VirtualizingStackPanel` di List; untuk Grid pastikan tetap lancar dengan 500 item (ukur; catat hasilnya).
5. Beralih mode tidak memuat ulang data dari DB dan tidak mereset seleksi/urutan.

### T6 — Test
**Core.Tests:** `ResourceTargetValidator` (semua baris tabel: skema berbahaya, URL tanpa skema, path relatif, ekstensi app tidak valid, spasi & non-ASCII, panjang maksimum); `ResourceService` dengan repo fake (add/many/update/remove/move, error codes).
**Data.Tests:** CRUD resource, cascade saat workspace dihapus, `AddRange` atomik (gagal tengah jalan → tidak ada yang tersimpan), `Reorder`, urutan hasil query, karakter non-ASCII/kutip tunggal di path (parameterisasi — **tidak boleh ada string concat SQL**).
**Platform.Tests:** availability checker memakai file/folder temp; timeout dengan fake; ikon provider tidak melempar untuk path tidak ada.

## Acceptance Criteria
- [ ] Keempat jenis resource bisa ditambah, dilihat, diedit, dihapus di workspace (FR-RS-01…06).
- [ ] Yang tersimpan hanya referensi; tidak ada penyalinan file (cek kode: tidak ada `File.Copy/Move/Delete`, `Directory.*Delete/Move`).
- [ ] Target tidak ditemukan → indikator + pesan; item tidak hilang diam-diam (FR-RS-07).
- [ ] Grid dan list sama-sama lengkap fungsinya; toggle tidak kehilangan data; preferensi bertahan setelah restart.
- [ ] URL berskema berbahaya ditolak; path dengan spasi dan non-ASCII bekerja.
- [ ] Menambah banyak file sekaligus bekerja dalam satu transaksi.
- [ ] UI tetap responsif saat cek ketersediaan (tidak ada blocking di UI thread).
- [ ] Semua test hijau; test arsitektur hijau.

## Jangan
- Jangan menambah checkbox, tombol Open, atau logika membuka resource (itu M04).
- Jangan melakukan request jaringan untuk memeriksa tautan.
- Jangan menyimpan ikon atau thumbnail ke database.
- Jangan menjalankan atau memverifikasi aplikasi dengan benar-benar mengeksekusinya.
