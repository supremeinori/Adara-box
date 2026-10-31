# M06 — Packaging & Rilis MVP (v1.0.0)

**Tujuan:** menghasilkan installer yang bisa dipasang di komputer bersih, plus dokumentasi minimal. Modul ini tidak ada di SRS tetapi dibutuhkan agar MVP benar-benar bisa dipakai.
**Prasyarat:** M05 disetujui (semua acceptance criteria MVP terpenuhi). **Referensi SRS:** NFR-CP-04 (versi Windows minimum), §6.4.

## Scope
**In:** versioning, publish script, installer, ikon aplikasi, uninstaller yang bersih, README, CHANGELOG, panduan pengguna singkat, tag rilis.
**Out:** auto-update, code signing (catat sebagai rekomendasi), distribusi Microsoft Store, telemetry.

## Task

### T1 — Versioning
1. SemVer di `Directory.Build.props` (`<Version>1.0.0</Version>`); tampilkan versi di panel Settings (dari `AssemblyInformationalVersion`).
2. `CHANGELOG.md` (format Keep a Changelog): ringkas semua fitur MVP per modul.

### T2 — Konfigurasi publish
Buat `build/publish.ps1`:
```powershell
dotnet publish src/AdaraBox.App -c Release -r win-x64 `
  --self-contained true -p:PublishSingleFile=false -p:IncludeNativeLibrariesForSelfExtract=true `
  -o artifacts/publish
```
- Default: **self-contained, win-x64** (pengguna tidak perlu memasang .NET). Catat ukuran hasil; jika terlalu besar, usulkan opsi framework-dependent ke manusia di `DECISIONS.md`.
- Jangan aktifkan trimming agresif untuk WPF (berisiko merusak binding/reflection); jika dicoba, hasilnya harus diuji penuh.
- Pastikan `app.manifest` (PerMonitorV2) dan ikon aplikasi (`.ico`, multi-ukuran 16/32/48/256) ikut terbawa.
- Skrip gagal (exit code ≠ 0) bila build/test gagal: jalankan `dotnet test` dulu.

### T3 — Installer (Inno Setup)
Buat `installer/AdaraBox.iss`:
- Instal per-pengguna ke `{localappdata}\Programs\AdaraBox` (tanpa hak admin; `PrivilegesRequired=lowest`).
- Pintasan Start Menu; pintasan Desktop opsional (default mati); opsi "Jalankan Adara Box saat Windows dimulai" (default mati, sinkron dengan fitur M05 — gunakan nilai Registry yang sama).
- Persyaratan OS: Windows 10 build ≥ 19044 (sesuai ADR M00); tolak di bawah itu dengan pesan jelas.
- Cek instance berjalan saat upgrade/uninstall (`CloseApplications=yes` atau mutex `Global\AdaraBox.SingleInstance`).
- **Uninstaller:** hapus file aplikasi dan entri Run milik Adara Box. **Data pengguna (`%LOCALAPPDATA%\AdaraBox\`) TIDAK dihapus secara default**; tanyakan "Hapus juga data (workspace & pengaturan)?" dengan default **Tidak**.
- Upgrade di atas versi lama tidak boleh menghapus database.
- Output: `artifacts/installer/AdaraBox-Setup-1.0.0.exe`.
- Jika Inno Setup tidak terpasang di lingkungan agent: tulis `.iss` + `build/build-installer.ps1` yang memanggil `ISCC.exe`, tandai langkah kompilasi **[HUMAN CHECK]**.

### T4 — Dokumentasi
- `README.md`: apa itu Adara Box, fitur MVP, cara instal, cara build dari sumber (`dotnet build`, prasyarat SDK 8), struktur repo, lokasi data/log, keterbatasan yang diketahui (mis. top bar tidak tampil di atas aplikasi fullscreen eksklusif; aplikasi yang butuh admin).
- `docs/USER_GUIDE.md` (Bahasa Indonesia, singkat): membuat workspace, menambah resource, grid/list, Open the File, Hide/Show top bar, shortcut, lokasi data & cara backup manual, status "Masih dalam pengembangan" untuk Media/Stats.
- `docs/KNOWN_ISSUES.md`: utang teknis dan bug P2/P3 yang ditunda.

### T5 — Verifikasi rilis (sebagian **[HUMAN CHECK]**)
Checklist di `docs/release-checklist.md`:
1. Instal di mesin/VM **bersih** (tanpa .NET SDK) → aplikasi berjalan.
2. Buat workspace + resource → tutup → buka lagi → data ada.
3. Upgrade-in-place dari build sebelumnya → data aman.
4. Uninstall → file aplikasi hilang; data tetap (default); entri startup hilang.
5. Jalankan seluruh checklist manual M01 dan M04 pada build terpasang.
6. Pindai installer dengan Windows Defender (tidak ada false positive); catat bahwa tanpa code signing SmartScreen bisa menampilkan peringatan.

### T6 — Tag rilis
Setelah manusia menyetujui: `git tag v1.0.0`, catat commit & artefak di `PROGRESS.md`. Agent **tidak** mem-push tag tanpa perintah manusia.

## Acceptance Criteria
- [ ] `build/publish.ps1` menghasilkan build Release yang bisa dijalankan tanpa .NET terpasang.
- [ ] Installer terpasang tanpa hak admin; uninstall bersih; data pengguna aman secara default.
- [ ] Upgrade tidak merusak data.
- [ ] README, USER_GUIDE, CHANGELOG, KNOWN_ISSUES, release-checklist lengkap.
- [ ] Versi tampil di Settings dan sama dengan versi installer.
- [ ] Checklist rilis dijalankan di mesin bersih. **[HUMAN CHECK]**

## Jangan
- Jangan menambahkan auto-update, telemetry, atau koneksi jaringan.
- Jangan menghapus data pengguna saat uninstall tanpa persetujuan eksplisit di dialog.
- Jangan commit artefak biner (`artifacts/`) ke git; masukkan ke `.gitignore`.
- Jangan commit sertifikat atau secret apa pun.
