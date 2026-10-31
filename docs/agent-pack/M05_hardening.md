# M05 — MVP Hardening

**Tujuan:** membuat MVP stabil, tahan error, dan nyaman dipakai harian. Tidak ada fitur besar baru; fokus pada ketahanan, kompatibilitas, dan keputusan yang tertunda.
**Prasyarat:** M04 disetujui. **Referensi SRS:** §5.1 (FR-TB-06), §5.9 (FR-CF-03/04), §6, §10, §11, §13, §16.

## Scope
**In:** penanganan DB rusak & backup, ketahanan runtime, multi-monitor, DPI, shortcut global, startup bersama Windows, navigasi keyboard & aksesibilitas dasar, performa, audit keamanan, pengujian menyeluruh, perbaikan bug.
**Out:** fitur baru (Stats/Media/drag-drop/kategori), sinkronisasi apa pun.

## Task

### T1 — Database rusak, backup, dan pemulihan (FR-CF-03, NFR-RL)
1. **Backup otomatis:** sebelum menjalankan migrasi yang akan mengubah skema, salin DB ke `%LOCALAPPDATA%\AdaraBox\backups\adarabox-v{user_version}-{yyyyMMdd-HHmmss}.db` (pakai SQLite `VACUUM INTO` atau `Microsoft.Data.Sqlite` backup API; jangan menyalin file mentah saat WAL aktif). Simpan 5 terbaru, hapus yang lebih lama (hanya di folder backups).
2. **Deteksi:** saat startup jalankan `PRAGMA quick_check`. Hasil bukan `ok`, atau koneksi/migrasi gagal → **jangan lanjut diam-diam**.
3. **Dialog pemulihan** dengan pilihan jelas:
   - "Buka folder data" (Explorer ke folder DB),
   - "Pulihkan dari backup terbaru" (daftar backup; salinan DB rusak diberi nama `adarabox.db.corrupt-{timestamp}` lebih dulu, **tidak dihapus**),
   - "Mulai dengan database baru" (DB lama tetap diarsipkan sebagai `.corrupt-…`),
   - "Keluar".
4. **Test (Data.Tests):** file DB berisi byte acak, file 0 byte, file read-only, direktori tidak bisa ditulis, DB versi lebih baru dari aplikasi (`user_version` > yang dikenal → tolak dengan pesan "dibuat oleh versi lebih baru", jangan diubah).
5. Operasi tulis di-guard: gagal menulis (disk penuh/terkunci) → pesan jelas, data di memori tidak dianggap tersimpan, UI tidak crash (NFR-RL-03).

### T2 — Ketahanan runtime
1. Audit semua `async void` dan fire-and-forget; hanya boleh untuk event handler dan harus ber-try/catch.
2. Pastikan handler global dari M01 mencatat log + menampilkan pesan, dan aplikasi bisa lanjut bila memungkinkan.
3. Semua operasi I/O punya `CancellationToken` yang dihormati saat aplikasi ditutup.
4. Penutupan aplikasi saat operasi berjalan: tidak ada data parsial (verifikasi dengan test transaksi/kill proses di tes integrasi).
5. Log: pastikan rotasi (14 hari), tidak ada path lengkap di level Information, tidak ada data pengguna sensitif.

### T3 — Multi-monitor & DPI (NFR-CP-03; SRS §16 no. 2)
Default keputusan (ubah hanya jika manusia menyatakan lain): **top bar muncul di monitor tempat kursor berada saat dimunculkan; saat tidak ada informasi, di monitor primer.**
1. Hitung posisi dari `WorkArea` + DPI monitor yang bersangkutan (PerMonitorV2), bukan `SystemParameters.PrimaryScreen`.
2. Tangani `SystemEvents.DisplaySettingsChanged` (monitor dicabut/diganti resolusi) → reposisi, bar tidak boleh berada di luar layar.
3. Trigger strip mengikuti monitor yang sama.
4. **[HUMAN CHECK]** uji skala 100/125/150/200% dan konfigurasi 2 monitor dengan skala berbeda.

### T4 — Shortcut global & startup (FR-TB-06, FR-CF-04; SRS §16 no. 3)
1. **Hotkey global** via `RegisterHotKey` (Platform, `IGlobalHotkeyService`): default `Ctrl+Alt+Space` untuk menampilkan/memfokuskan top bar (toggle panel Workspace). Dapat diubah di Settings; simpan di `settings` (`hotkey.topbar`).
2. Jika registrasi gagal (sudah dipakai app lain) → pesan jelas di Settings, aplikasi tetap jalan, **jangan crash**. Tolak kombinasi tanpa modifier dan kombinasi sistem umum (`Ctrl+Alt+Del`, `Win+L`, dll.).
3. **Startup bersama Windows:** opsi di Settings (default **mati**), tulis/hapus nilai di `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`; mulai dalam keadaan tray/top bar sesuai preferensi Hide. Hanya menyentuh key milik Adara Box.
4. Test: parser/validator kombinasi tombol (Core), service dengan fake (Platform).

### T5 — Keyboard & aksesibilitas (NFR-UX-03)
1. Seluruh alur inti bisa dilakukan hanya dengan keyboard: pindah modul, buka workspace, tambah/edit/hapus, toggle grid/list, mode Open, pilih, Open.
2. Urutan Tab logis; fokus terlihat jelas; `Esc` selalu mundur satu level (dialog → mode Open → panel).
3. `AutomationProperties.Name` pada kontrol ikon-saja; kontras teks memadai (≥ 4.5:1 untuk teks normal). 
4. Pintasan: `F2` ganti nama, `Delete` hapus (dengan konfirmasi) — hanya di mode Kelola.

### T6 — Performa (§10.4, NFR-PF)
Buat dataset uji (workspace dengan 50, 200, 500 resource; generator di `tests/` atau script) dan ukur lewat `Stopwatch` + log:
| Metrik | Target awal (bisa dikoreksi manusia) |
|---|---|
| Buka workspace 200 resource sampai daftar tampil | < 300 ms (tanpa availability check) |
| Scroll grid/list 500 item | tanpa jank terlihat **[HUMAN CHECK]** |
| CPU idle (bar terlihat, tidak ada interaksi) | ≈ 0 % rata-rata 60 detik |
| RAM idle | < 150 MB (catat angka aktual) |
| Memory leak: buka-tutup panel 100× | RAM stabil (tidak naik monoton) |
Catat hasil di `docs/perf-report.md`. Jika target gagal → perbaiki (virtualisasi, cache ikon, hindari binding berlebih) atau minta keputusan manusia.

### T7 — Audit keamanan & privasi (§6.5)
Lakukan audit dengan `grep`/review dan tulis hasilnya di `docs/security-review.md`:
- [ ] Tidak ada `File.Delete`, `File.Move`, `File.Copy`, `Directory.Delete/Move` terhadap path resource pengguna (kecuali backup/log milik app sendiri).
- [ ] Tidak ada `cmd`, `powershell`, `Process.Start` dengan `UseShellExecute=false` + shell, atau string command gabungan.
- [ ] Semua SQL berparameter; tidak ada interpolasi string ke SQL.
- [ ] Tidak ada panggilan jaringan (`HttpClient`, `WebClient`, `Socket`) di MVP.
- [ ] URL hanya `http/https`; path divalidasi sebelum dibuka.
- [ ] Log tidak memuat data sensitif pada level Information.
- [ ] Dependensi NuGet dicek (`dotnet list package --vulnerable --include-transitive`) tanpa temuan High/Critical.

### T8 — Uji menyeluruh & pembersihan
1. Jalankan seluruh strategi uji SRS §10.1-10.3; untuk tiap butir tulis: otomatis (nama test) atau manual (langkah) di `docs/test-matrix.md` dengan hasil.
2. Skenario ketahanan wajib: target dihapus/dipindah setelah ditambahkan; target tanpa izin; satu dari lima resource gagal; DB tidak terbaca; nama dengan karakter khusus/emoji/panjang maksimum; aplikasi di-kill saat menulis.
3. Buat **traceability matrix** `docs/traceability.md`: setiap FR-ID P0/P1 → modul + test/manual-test yang membuktikan.
4. Perbaiki semua bug P0/P1 yang ditemukan; catat yang ditunda beserta alasan.
5. Review konsistensi UI: seluruh status "belum tersedia" memakai komponen status yang sama; tidak ada teks hard-code tersebar.
6. Pastikan seluruh **11 acceptance criteria MVP (SRS §11)** terpenuhi dan dibuktikan satu per satu di `PROGRESS.md`.

## Acceptance Criteria
- [ ] DB rusak/0 byte/versi lebih baru ditangani dengan dialog jelas; tidak ada data tertimpa diam-diam; backup otomatis jalan.
- [ ] Hotkey global dan opsi startup bekerja, dapat dikonfigurasi, dan gagal dengan anggun bila konflik.
- [ ] Multi-monitor dan DPI berperilaku sesuai keputusan; bar tidak pernah di luar layar. **[HUMAN CHECK]**
- [ ] Semua alur inti dapat dilakukan lewat keyboard.
- [ ] `docs/perf-report.md`, `docs/security-review.md`, `docs/test-matrix.md`, `docs/traceability.md` terisi.
- [ ] Seluruh 11 acceptance criteria MVP terbukti.
- [ ] Tidak ada bug P0/P1 terbuka; `dotnet build`/`test` hijau; CI hijau.

## Jangan
- Jangan menambah fitur baru untuk "sekalian".
- Jangan menghilangkan/skip test yang gagal; perbaiki penyebabnya.
- Jangan mengubah migrasi lama; perubahan skema = migrasi baru + test upgrade.
- Jangan menghapus file selain backup/log milik aplikasi sendiri.
