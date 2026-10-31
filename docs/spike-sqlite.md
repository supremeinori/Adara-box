# Spike Report: SQLite & Dapper Persistence (Spike D)

**Dokumen Terkait:** Modul M00, Task T7  
**Lokasi Kode:** `spikes/SqliteSpike/` & `tests/AdaraBox.Data.Tests/SqliteSpikeTests.cs`  
**Target:** Memvalidasi driver `Microsoft.Data.Sqlite`, Dapper TypeHandler, WAL mode, integritas path non-ASCII, dan strategi migrasi dengan `PRAGMA user_version`.

---

## 1. Temuan Konfigurasi SQLite

| Aspek | Konfigurasi / Pengujian | Hasil Pengujian | Catatan Teknis |
|---|---|---|---|
| **Foreign Keys** | `PRAGMA foreign_keys = ON;` | **Aktif** | Wajib dipanggil pada setiap pembukaan koneksi SQLite karena default SQLite adalah `OFF`. |
| **Journal Mode (Konkurensi)** | `PRAGMA journal_mode = WAL;` | **Mode WAL aktif** | Memungkinkan pembacaan konkuren (misal background watcher atau beberapa thread) tanpa memblokir thread UI. File `-wal` dan `-shm` dikelola otomatis oleh SQLite. |
| **Migrasi Skema** | `PRAGMA user_version` | **Valid** | Nilai integer tersimpan di header database. Digunakan sebagai tracking versi migrasi tanpa perlu tabel tambahan yang rumit di MVP. |
| **Transaksi & Rollback** | `SqliteTransaction` (`Commit` & `Rollback`) | **Aman & Konsisten** | Rollback membersihkan data yang gagal tanpa meninggalkan state parsial (**NFR-RL-04**). |
| **Path Spasi & Unicode** | Path mengandung spasi dan karakter non-ASCII (`üöä`) | **Berhasil** | Driver `Microsoft.Data.Sqlite` menangani path UTF-8 Windows dengan andal. |

---

## 2. Pemetaan Tipe Dapper (`Guid` & `DateTimeOffset`)

SQLite tidak memiliki tipe bawaan terpisah untuk `UUID` atau `TIMESTAMP WITH TIME ZONE`. Untuk menjamin format seragam dan portabilitas data:
1. **`Guid` ↔ `TEXT`:** Disimpan sebagai string UUID berformat `"D"` (`00000000-0000-0000-0000-000000000000`).
2. **`DateTimeOffset` ↔ `TEXT`:** Disimpan sebagai string ISO-8601 berformat `"O"` (`yyyy-MM-ddTHH:mm:ss.fffffffzzz`).
3. Dapper `SqlMapper.TypeHandler` didaftarkan secara terpusat melalui `AdaraBox.Data.TypeHandlers.SqliteTypeHandlers.Register()`.
4. Terbukti berhasil memetakan objek secara dua arah (C# record ↔ SQLite column) tanpa kehilangan presisi waktu.

---

## 3. Lokasi Penyimpanan Produksi

Sesuai Master Guide §8:
* Database: `%LOCALAPPDATA%\AdaraBox\adarabox.db`
* Direktori Backup: `%LOCALAPPDATA%\AdaraBox\backups\`
* Test harness menggunakan temporary directory (`Path.GetTempPath()`) terisolasi dan dibersihkan setelah pengujian selesai.
