# Laporan Evaluasi: Integrasi Global System Media Transport Controls (GSMTC)

**Dokumen:** `docs/media-controls-evaluation.md`  
**Status:** Studi Kelayakan Teknis (Eksplorasi M08)  
**Referensi:** SRS §9.3; Windows SDK UWP/WinRT APIs  

---

## 1. Ringkasan Evaluasi
Untuk mengontrol pemutaran media sistem (Play/Pause, Next, Previous) dan menampilkan judul/artis lagu yang sedang diputar di aplikasi lain (misal Spotify Desktop, browser, VLC), Windows menyediakan API:
`Windows.Media.Control.GlobalSystemMediaTransportControlsSessionManager` (GSMTC).

---

## 2. Analisis Teknis & Dampak Arsitektur

### 2.1 Target Framework Moniker (TFM)
- **Kebutuhan:** Untuk memanggil WinRT/GSMTC APIs secara modern di .NET 8, proyek harus menargetkan:
  `<TargetFramework>net8.0-windows10.0.19041.0</TargetFramework>`
  bukan sekadar `net8.0-windows`.
- **Dampak Build:**
  - Build time meningkat sekitar 15%–25% karena resolusi metadata Windows SDK Projections (`Microsoft.Windows.SDK.NET.Ref`).
  - Tidak kompatibel dengan OS lama sebelum Windows 10 build 19041 (Mei 2020 Update).

### 2.2 Kemampuan API
- **Pembacaan Metadata:**
  - `GetCurrentSession()` mengembalikan sesi media aktif.
  - `TryGetMediaPropertiesAsync()` menyediakan `Title`, `Artist`, `AlbumTitle`, dan thumbnail stream.
- **Kontrol Aksi:**
  - `TryPlayAsync()`, `TryPauseAsync()`, `TryTogglePlayPauseAsync()`
  - `TrySkipNextAsync()`, `TrySkipPreviousAsync()`
- **Event-Driven:**
  - `CurrentSessionChanged`
  - `MediaPropertiesChanged`
  - `PlaybackInfoChanged`

### 2.3 Keterbatasan & Masalah di Dunia Nyata
1. **Dukungan Aplikasi Tidak Merata:**
   - Spotify Desktop dan Google Chrome mendukung GSMTC dengan baik.
   - Beberapa game, media player portable, atau pemutar video pihak ketiga tidak mendaftarkan sesi GSMTC mereka, sehingga API mengembalikan `null` (seolah tidak ada musik yang diputar).
2. **Race Condition & Multi-Session:**
   - Jika pengguna memutar video di YouTube pada satu tab, dan musik di Spotify pada background, GSMTC sering berganti-ganti fokus sesi secara tidak menentu saat pengguna menjeda salah satu media.
3. **Threading & STA/MTA Synchronization:**
   - Callback WinRT berjalan di MTA thread pool; harus dipindahkan secara hati-hati ke WPF STA Dispatcher untuk menghindari UI lock.

---

## 3. Rekomendasi
- **Status Saat Ini:** Tetap tampilkan banner jujur di UI: `"Kontrol pemutaran: masih dalam pengembangan"`.
- **Rencana Rilis Mendatang:** Fitur kontrol media sistem GSMTC dapat dijadwalkan sebagai eksperimen di v1.2 melalui modul sidecar terisolasi tanpa membebani build core aplikasi saat ini.
