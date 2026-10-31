# M07 — Modul Stats (Pasca-MVP)

**Tujuan:** mengganti placeholder Stats dengan metrik sistem real-time (tanpa histori): CPU, RAM, storage, dan GPU bila tersedia.
**Prasyarat:** **MVP v1.0 sudah dirilis/disetujui** (M06 selesai). Jangan mulai sebelum itu. **Referensi SRS:** §3.5, §5.7, §6.1 (NFR-PF-04), §9.4; FR-ST-01…06.

## Scope
**In:** provider metrik, polling ringan hanya saat panel terlihat, UI panel Stats, penanganan metrik tidak tersedia.
**Out:** histori/grafik historis, notifikasi ambang, per-proses, suhu/fan, ekspor data, penyimpanan metrik ke DB.

## Model & Interface

```csharp
// AdaraBox.Core/Stats
public sealed record DriveUsage(string Name, long TotalBytes, long FreeBytes);   // Name mis. "C:\"

public sealed record StatsSnapshot(
    DateTimeOffset Timestamp,
    double? CpuPercent,              // null = tidak tersedia
    long? MemoryUsedBytes,
    long? MemoryTotalBytes,
    IReadOnlyList<DriveUsage> Drives,
    double? GpuPercent);             // null = tidak tersedia / tidak andal

public interface ISystemStatsProvider
{
    Task<StatsSnapshot> ReadAsync(CancellationToken ct = default);
}
```
**Aturan inti (FR-ST-06, SRS §9.4):** metrik yang tidak bisa dibaca = `null` dan UI menampilkan "Tidak tersedia". **Dilarang** mengarang, menebak, atau memakai angka dummy/perkiraan sebagai data aktual.

## Task

### T1 — Provider (AdaraBox.Platform)
1. **CPU:** hitung dari selisih dua pembacaan `GetSystemTimes` (P/Invoke): `usage = 1 - (Δidle / (Δkernel + Δuser))` (kernel sudah termasuk idle). Pembacaan pertama tidak bermakna → simpan baseline, hasil `null` sampai ada dua sampel.
   Hindari `PerformanceCounter` untuk CPU (rentan counter korup/lokalisasi).
2. **RAM:** `GlobalMemoryStatusEx` → used = total − available.
3. **Storage:** `DriveInfo.GetDrives()` hanya `DriveType.Fixed` dan `IsReady`; bungkus try/catch per drive (drive bermasalah dilewati).
4. **GPU:** coba `PerformanceCounterCategory("GPU Engine")`: jumlahkan `Utilization Percentage` untuk instance bertipe `engtype_3D` (ambil max antar GPU bila ada beberapa).
   Jika kategori tidak ada, exception, atau hasil tidak masuk akal → `GpuPercent = null`. Catat alasan ke log (Debug), jangan spam.
5. Semua pembacaan dibungkus agar **tidak pernah melempar** keluar dari `ReadAsync`; kegagalan sebagian = `null` pada metrik itu.

### T2 — Polling
- `StatsPollingService`: mulai saat panel Stats **terlihat**, berhenti saat panel ditutup/top bar Hidden/aplikasi minimize ke tray. Interval default 1000 ms (`UiConstants`).
- Tidak ada polling saat panel tidak terlihat (NFR-PF-04). Pembacaan di background thread; update UI lewat Dispatcher; tidak menumpuk (lewati tick jika pembacaan sebelumnya belum selesai).
- Hormati `CancellationToken` saat berhenti.

### T3 — UI
- Ubah status `StatsModule` dari `InDevelopment` → `Available`. Hapus teks "Masih dalam pengembangan" **hanya** untuk Stats; Media tetap placeholder.
- Panel: kartu/bar untuk CPU %, RAM (terpakai/total + %), tiap drive (terpakai/total), GPU %.
- Nilai `null` → teks "Tidak tersedia" dengan tooltip singkat penyebab umum ("Data GPU bergantung pada perangkat keras dan driver"). Visual berbeda dari error.
- Tidak ada grafik histori. Boleh bar/ring indikator saat ini saja.
- Angka diformat dengan `CultureInfo` aktif, satuan GB/MB sesuai ukuran.

### T4 — Test
**Core.Tests:** logika perhitungan CPU dari sampel (fungsi murni `CpuUsageCalculator`: kasus normal, Δ=0, counter wrap/mundur → `null`); formatter ukuran; pemetaan `null` → teks "Tidak tersedia".
**Platform.Tests:** provider tidak melempar bila sebagian sumber gagal (inject fake); `ReadAsync` mengembalikan snapshot dengan drive Fixed saja.
**Perf [HUMAN CHECK]:** panel Stats terbuka 5 menit → CPU aplikasi rata-rata < 2%, RAM stabil; panel tertutup → tidak ada aktivitas polling (verifikasi lewat log/counter).

## Acceptance Criteria
- [ ] Panel Stats menampilkan CPU, RAM, storage; GPU bila tersedia, otherwise "Tidak tersedia" (FR-ST-03/04/06).
- [ ] Tidak ada angka dummy/karangan di kode maupun UI.
- [ ] Polling hanya berjalan saat panel terlihat; UI tidak freeze (FR-ST-05, NFR-PF-04).
- [ ] Tidak ada histori/grafik riwayat, tidak ada penyimpanan metrik.
- [ ] Media masih placeholder jujur; fitur Workspace tidak terpengaruh (regresi: seluruh test M02-M05 tetap hijau).
- [ ] Tes dan build hijau; `CHANGELOG.md` diperbarui (versi minor, mis. 1.1.0).

## Jangan
- Jangan menambah package berat (mis. library monitoring besar) tanpa catatan alasan di `DECISIONS.md`.
- Jangan polling saat panel tersembunyi.
- Jangan menampilkan GPU 0% ketika sebenarnya tidak bisa dibaca.
