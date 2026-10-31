# M08 — Modul Media (Pasca-MVP, Eksplorasi Bertahap)

**Tujuan:** menyediakan akses cepat ke layanan media tanpa membuat MVP bergantung pada integrasi pihak ketiga.
Dikerjakan dalam **dua tahap terpisah**; tahap B hanya jika manusia menyetujui hasil evaluasi.
**Prasyarat:** M06 selesai (MVP dirilis). M07 sebaiknya selesai, tetapi tidak wajib. **Referensi SRS:** §3.4, §5.6, §6.5 (NFR-SC-05), §9.3, §13; FR-MD-01…05.

## Scope
**In:** Tahap A (media quick links lewat browser default), Tahap B (evaluasi WebView2 → laporan, bukan fitur produksi), evaluasi kontrol media sistem (laporan).
**Out:** integrasi API Spotify/layanan lain (dilarang, FR-MD-04), menyimpan kredensial/cookie sendiri, scraping, mengunduh/merekam media, plugin pihak ketiga.

## Tahap A — Media Quick Links (fitur nyata, risiko rendah)

Pengguna menyimpan daftar tautan layanan media (mis. playlist, situs musik/video) dan membukanya di **browser default**. Memakai ulang infrastruktur resource & launcher yang sudah ada.

### A1 — Data
- Migrasi baru (#N, jangan edit migrasi lama): tabel `media_link(id TEXT PK, name TEXT NOT NULL, url TEXT NOT NULL, sort_order INTEGER NOT NULL DEFAULT 0, created_at TEXT NOT NULL, updated_at TEXT NOT NULL)`.
- `IMediaLinkRepository` + `IMediaLinkService` (validasi URL memakai `ResourceTargetValidator` jenis `Link`: hanya `http/https`).
- Test: CRUD, validasi, migrasi upgrade dari DB v1.x tanpa kehilangan data.

### A2 — UI
- Ubah status `MediaModule` ke `Available` **hanya jika** Tahap A selesai; panel menampilkan daftar tautan (kartu/list), tombol tambah/edit/hapus, klik = buka di browser default lewat `IResourceLauncher` (hasil gagal dilaporkan jelas).
- Teks tegas di panel: layanan dibuka di browser; Adara Box tidak mengontrol pemutaran.
- **Jangan** menampilkan kontrol play/pause/next (tidak berfungsi di tahap ini). Status `InDevelopment` untuk kontrol media sistem tetap ditampilkan jujur ("Kontrol pemutaran: masih dalam pengembangan") atau disembunyikan sama sekali.
- Tidak ada request jaringan oleh Adara Box (Aturan Emas #5).

### A3 — Test & acceptance
- [ ] CRUD tautan media bekerja dan bertahan setelah restart.
- [ ] Tautan dibuka di browser default; kegagalan dilaporkan.
- [ ] Skema `javascript:`, `file:` ditolak.
- [ ] Workspace/Stats tidak berubah perilakunya (regresi hijau).

## Tahap B — Evaluasi WebView2 (HANYA laporan keputusan)

Tujuan: menjawab apakah menanam layanan web di dalam panel layak. **Jangan mengirim fitur produksi di tahap ini.**
Buat proyek eksperimen `spikes/WebViewSpike/` dan tulis `docs/webview-evaluation.md` yang menjawab, dengan bukti/pengukuran:
1. **Autentikasi:** apakah login layanan target bisa dilakukan di WebView2? (Catat: sesi login Chrome **tidak** otomatis tersedia; WebView2 memakai profil/userDataFolder sendiri — SRS §9.3.)
2. **Penyimpanan sesi/cookie:** di mana disimpan (`%LOCALAPPDATA%\AdaraBox\webview2`), bagaimana dihapus, dampak privasi (NFR-SC-05).
3. **Kebijakan layanan:** apakah ketentuan layanan target membatasi penyematan/pemutaran? (Tulis "perlu ditinjau manusia" jika tidak yakin; jangan menyimpulkan legal.)
4. **Sumber daya:** RAM/CPU idle dan saat memutar; dampak ke top bar yang harus selalu ringan.
5. **Playback & media key:** apakah media key sistem ikut bekerja; perilaku saat panel ditutup (audio lanjut/berhenti).
6. **Keamanan:** navigasi dibatasi ke domain allowlist; blokir pembukaan window baru/unduhan; nonaktifkan fitur yang tidak perlu (dev tools, context menu) di build rilis; runtime WebView2 harus tersedia (cek & pesan jelas bila belum terpasang).
7. **Distribusi:** dampak pada installer (Evergreen runtime bootstrapper) dan ukuran.
Akhiri laporan dengan **rekomendasi: Lanjut / Tunda / Batal** dan daftar risiko. **Berhenti dan tunggu keputusan manusia.**

## Evaluasi Kontrol Media Sistem (laporan singkat, opsional)
Teliti `Windows.Media.Control.GlobalSystemMediaTransportControlsSessionManager` (butuh TFM `net8.0-windows10.0.19041.0`):
kemampuan membaca judul/artis, play/pause/next, dukungan lintas aplikasi, dan dampaknya ke dependensi build. Tulis di `docs/media-controls-evaluation.md`. **Tidak ada implementasi produksi tanpa persetujuan.**

## Acceptance Criteria Modul
- [ ] Tahap A selesai dan teruji; Media tidak lagi placeholder **hanya** untuk fungsi yang benar-benar ada.
- [ ] `docs/webview-evaluation.md` lengkap dengan rekomendasi dan bukti.
- [ ] Tidak ada API pihak ketiga, tidak ada penyimpanan kredensial, tidak ada koneksi jaringan oleh aplikasi sendiri di Tahap A.
- [ ] Semua test lama tetap hijau; `CHANGELOG.md` diperbarui.

## Jangan
- Jangan mengimplementasikan integrasi Spotify API atau OAuth apa pun.
- Jangan menjadikan WebView2 dependensi wajib aplikasi inti.
- Jangan menampilkan kontrol media yang tidak berfungsi.
- Jangan menyimpan password/token pengguna dengan cara apa pun.
