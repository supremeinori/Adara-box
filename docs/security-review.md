# Laporan Audit Keamanan & Privasi (Security & Privacy Review)

**Aplikasi:** Adara Box MVP  
**Tanggal Audit:** 31 Oktober 2026  
**Referensi:** SRS §6.5, SRS §10, SRS §13 (Golden Rules #1, #3, #5)

---

## 1. Ringkasan Checklist Keamanan

| Kriteria Audit | Status | Verifikasi & Bukti |
|---|---|---|
| **Tidak ada manipulasi file pengguna** | **LULUS** | 0 panggilan `File.Delete`, `File.Move`, `File.Copy` pada path resource pengguna. Operasi file hanya dilakukan pada `%LOCALAPPDATA%\AdaraBox` untuk database backup, log rotation, dan corrupt archive. |
| **Tidak ada eksekusi shell command string** | **LULUS** | 0 eksekusi `cmd.exe`, `powershell.exe`. Peluncuran resource menggunakan `ShellProcessStarter` langsung via `ProcessStartInfo { UseShellExecute = true }` tanpa penggabungan argumen string (Golden Rule #3). |
| **100% Parameterized SQL** | **LULUS** | Seluruh query SQLite (Dapper & ADO.NET) menggunakan parameter terikat (`@param`). Tidak ada interpolasi string (`$"..."`) pada perintah SQL. |
| **Zero Network Calls** | **LULUS** | 0 instansiasi `HttpClient`, `WebClient`, atau raw TCP/UDP socket pada kode produksi (Golden Rule #5). Aplikasi bekerja 100% lokal dan offline. |
| **Validasi Skema URL & Path** | **LULUS** | `ResourceTargetValidator` hanya mengizinkan skema `http://` dan `https://` untuk tautan. Skema berisiko seperti `file://`, `javascript:`, atau UNC path yang tidak sah ditolak. Path lokal dinormalisasi dan divalidasi sebelum dibuka. |
| **Privasi Log Pengguna** | **LULUS** | Log Serilog dikonfigurasi dengan retensi 14 hari. Pada level `Information`, path lengkap file dan URL pengguna tidak dicatat (hanya dicatat pada level `Debug` untuk diagnosis lokal pengembang). |
| **Audit Kerentanan Dependensi NuGet** | **LULUS** | Hasil `dotnet list package --vulnerable --include-transitive`: **0 vulnerable packages** ditemukan (High/Critical: 0) setelah memperbarui `Microsoft.Data.Sqlite` ke versi `8.0.33` dan memitigasi paket transitif via `Directory.Build.props`. |

---

## 2. Rincian Temuan & Langkah Mitigasi

### A. Golden Rule #1: Referensi, Bukan Duplikasi atau Manipulasi
- **Pengecekan:** Pemeriksaan seluruh kode pada folder `src/` menunjukkan bahwa menghapus resource dari workspace hanya menghapus baris relasi di database (`DELETE FROM resources WHERE id = @Id`). File target fisik pengguna di disk tidak pernah disentuh atau dihapus.

### B. Golden Rule #3: Shell Execution Safety
- Peluncuran file, direktori, aplikasi Desktop, aplikasi UWP (via `shell:AppsFolder\...`), dan browser dijalankan langsung melalui API Windows Shell tanpa melalui command prompt atau PowerShell wrapper.

### C. Golden Rule #5: Zero Network Communication
- Tidak ada telemetry, update checker daring, atau background analytics. Seluruh dependensi bekerja mandiri di lingkungan sandbox offline.

### D. Audit Dependensi Paket
- `dotnet list package --vulnerable --include-transitive` dijalankan terhadap seluruh solusi (`AdaraBox.sln`).
- Hasil audit: Seluruh proyek (`AdaraBox.App`, `AdaraBox.Core`, `AdaraBox.Data`, `AdaraBox.Platform`, dan proyek uji) berstatus:
  ```text
  The given project has no vulnerable packages given the current sources.
  ```

---

## 3. Kesimpulan

Adara Box MVP memenuhi seluruh standar keamanan, integritas data, dan privasi yang disyaratkan oleh dokumen spesifikasi (SRS) dan Master Guide.
