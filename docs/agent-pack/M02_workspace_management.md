# M02 — Workspace Management & Persistensi

**Tujuan:** fondasi data (SQLite + migrasi + settings) dan fitur CRUD workspace yang bertahan setelah restart.
**Prasyarat:** M01 disetujui. **Referensi SRS:** §3.3, §5.2, §5.9, §6.2, §8; FR-WS-01…07, FR-CF-01/02/05, FR-TB-05, NFR-RL-01/04.

## Scope
**In:** database + migrasi, `ISettingsStore` versi SQLite, model `Workspace`, repository, service, UI daftar workspace, form buat/ganti nama, hapus dengan konfirmasi, persistensi preferensi Hide.
**Out:** resource (M03), tampilan grid/list (M03), Open the File (M04), penanganan DB rusak lengkap (M05; di sini cukup gagal dengan pesan jelas).

## Model & Interface (AdaraBox.Core)

```csharp
public sealed record Workspace(Guid Id, string Name, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt, int SortOrder);

public interface IWorkspaceRepository
{
    Task<IReadOnlyList<Workspace>> GetAllAsync(CancellationToken ct = default);   // urut SortOrder, lalu CreatedAt
    Task<Workspace?> GetAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(Workspace workspace, CancellationToken ct = default);
    Task UpdateNameAsync(Guid id, string name, DateTimeOffset updatedAt, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);              // cascade ke resource (M03)
    Task<int> GetNextSortOrderAsync(CancellationToken ct = default);
}

public interface IWorkspaceService
{
    Task<IReadOnlyList<Workspace>> ListAsync(CancellationToken ct = default);
    Task<OperationResult<Workspace>> CreateAsync(string name, CancellationToken ct = default);
    Task<OperationResult> RenameAsync(Guid id, string newName, CancellationToken ct = default);
    Task<OperationResult> DeleteAsync(Guid id, CancellationToken ct = default);
}

public interface IClock { DateTimeOffset UtcNow { get; } }   // supaya waktu bisa di-fake di test
```

**Aturan validasi nama** (di Core, `WorkspaceNameValidator`): trim; tidak boleh kosong/whitespace; maksimum 60 karakter;
karakter kontrol ditolak; **duplikat diizinkan** (FR-WS-07). Kode error: `workspace.name.empty`, `workspace.name.too_long`, `workspace.name.invalid`, `workspace.not_found`.

## Skema Database (migrasi #1)

Lokasi file: `%LOCALAPPDATA%\AdaraBox\adarabox.db`. Aktifkan `PRAGMA foreign_keys=ON` di **setiap** koneksi dan `PRAGMA journal_mode=WAL` saat pertama dibuka.
Versi skema pakai `PRAGMA user_version`.

```sql
CREATE TABLE workspace (
  id          TEXT PRIMARY KEY,
  name        TEXT NOT NULL,
  created_at  TEXT NOT NULL,           -- ISO-8601 UTC
  updated_at  TEXT NOT NULL,
  sort_order  INTEGER NOT NULL DEFAULT 0
);
CREATE TABLE resource (                -- dibuat sekarang agar cascade sudah benar; dipakai di M03
  id            TEXT PRIMARY KEY,
  workspace_id  TEXT NOT NULL REFERENCES workspace(id) ON DELETE CASCADE,
  name          TEXT NOT NULL,
  type          TEXT NOT NULL CHECK (type IN ('application','file','folder','link')),
  target        TEXT NOT NULL,
  sort_order    INTEGER NOT NULL DEFAULT 0,
  created_at    TEXT NOT NULL,
  updated_at    TEXT NOT NULL
);
CREATE INDEX ix_resource_workspace ON resource(workspace_id, sort_order);
CREATE TABLE settings (
  key   TEXT PRIMARY KEY,
  value TEXT NOT NULL
);
```

## Task

### T1 — Infrastruktur Data (AdaraBox.Data)
1. `IDbConnectionFactory` + `SqliteConnectionFactory(string dbPath)`: membuka koneksi baru per operasi, menjalankan PRAGMA yang perlu.
2. Dapper `TypeHandler` untuk `Guid` (TEXT) dan `DateTimeOffset` (ISO-8601 round-trip `"O"`). Daftarkan sekali di startup.
3. **Migrator:** daftar migrasi berurutan (`IMigration { int Version; string Sql; }`); jalankan yang `Version > user_version`,
   **tiap migrasi dalam satu transaksi**, naikkan `user_version` di transaksi yang sama. Migrasi tidak boleh diedit setelah dirilis; perubahan = migrasi baru.
4. Jalankan migrator saat startup sebelum UI muncul; jika gagal → dialog error jelas + log, aplikasi berhenti (jangan lanjut dengan DB setengah jadi).
5. Implementasi `SqliteWorkspaceRepository` dan `SqliteSettingsStore` (upsert: `INSERT … ON CONFLICT(key) DO UPDATE`).

### T2 — Service (AdaraBox.Core)
Implementasi `WorkspaceService`: validasi → `Guid.NewGuid()` + `IClock` → repository. `DeleteAsync` hanya menghapus baris DB (tidak menyentuh disk — Aturan Emas #1).
`CreateAsync` memberi `SortOrder = GetNextSortOrderAsync`.

### T3 — Persistensi preferensi top bar
Hubungkan `TopBarVisibilityService` (M01) ke `ISettingsStore` (`SettingKeys.TopBarVisibility`): baca saat startup, tulis saat berubah.
Hapus implementasi sementara/in-memory dari runtime (boleh tetap dipakai di test).

### T4 — UI Workspace
1. Tombol **Add Workspace** muncul di top bar sekarang (data-driven lewat modul/aksi, bukan hard-code).
2. `WorkspacePanelViewModel`: daftar workspace (`ObservableCollection<WorkspaceItemViewModel>`), state kosong ("Belum ada workspace. Buat yang pertama."), state loading.
3. Buat workspace: dialog/inline form dengan satu TextBox nama + tombol Simpan/Batal. Enter = simpan, Esc = batal. Error validasi tampil di bawah field (bukan MessageBox).
4. Ganti nama: aksi pada item (menu konteks atau tombol ⋯ + F2). Memakai form yang sama dengan nama terisi.
5. Hapus: dialog konfirmasi yang menyebut nama workspace dan jumlah resource (0 untuk sekarang) dan teks tegas:
   "Hanya menghapus workspace dari Adara Box. File, folder, aplikasi, dan tautan aslinya tidak terpengaruh."
6. Klik workspace → membuka **tampilan detail workspace** (kerangka kosong dengan judul + tombol Kembali; isi resource di M03). Ini memenuhi FR-WS-05.
7. Abstraksi dialog lewat `IDialogService` (App) supaya ViewModel bisa dites tanpa WPF.

### T5 — Test
**Core.Tests:** validator nama (kosong, spasi, 61 karakter, karakter kontrol, nama duplikat OK); `WorkspaceService` dengan repository fake (create/rename/delete/not found).
**Data.Tests** (file DB sementara di temp, dibersihkan): migrasi dari DB kosong ke versi terbaru; migrasi idempoten; foreign key aktif (hapus workspace → resource ikut terhapus);
create → restart (koneksi baru) → data masih ada; upsert settings; transaksi rollback bila migrasi gagal; path dengan spasi/non-ASCII.
**App.Tests (opsional):** ViewModel dengan `IDialogService` fake.

## Acceptance Criteria
- [ ] Pengguna bisa membuat, melihat, mengganti nama, dan menghapus workspace (FR-WS-01…05).
- [ ] Nama kosong ditolak dengan pesan jelas; nama duplikat diizinkan.
- [ ] Workspace dan preferensi Hide tetap ada setelah aplikasi ditutup dan dibuka lagi (FR-WS-06, FR-TB-05).
- [ ] Hapus workspace meminta konfirmasi dan tidak menyentuh file asli (cek: tidak ada panggilan `File.Delete`/`Directory.Delete` di seluruh kode).
- [ ] Migrasi berjalan di DB baru; upgrade ke versi berikutnya tidak menghapus data (test).
- [ ] Penulisan data memakai transaksi bila lebih dari satu statement (NFR-RL-04).
- [ ] Tidak ada operasi DB di UI thread yang memblokir.
- [ ] Test Core + Data hijau; test arsitektur masih hijau.

## Jangan
- Jangan menaruh SQL di ViewModel atau Core.
- Jangan membuat resource CRUD di modul ini walaupun tabelnya sudah ada.
- Jangan menyimpan state UI (seleksi, panel terbuka) di database.
- Jangan mengedit migrasi #1 setelah modul disetujui.
