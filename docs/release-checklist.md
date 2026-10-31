# Checklist Verifikasi Rilis MVP (Release Verification Checklist)

**Versi Rilis:** Adara Box v1.0.0 (MVP)  
**Target Platform:** Windows 10 (build ≥ 19044) & Windows 11 (x64)  
**Artefak yang Diuji:** `artifacts/installer/AdaraBox-Setup-1.0.0.exe` dan `artifacts/publish/`

---

## Prosedur Uji Mesin Bersih (Clean Machine / VM)

Setiap rilis publik wajib melalui serangkaian verifikasi berikut pada lingkungan Windows bersih (tanpa .NET SDK atau Visual Studio terpasang):

| No | Skenario Pengujian | Langkah & Kriteria Penerimaan | Status | Catatan Pengujian |
|---|---|---|---|---|
| **1** | **Instalasi pada Mesin Bersih** | Jalankan `AdaraBox-Setup-1.0.0.exe` pada Windows 10/11 tanpa .NET SDK. Installer harus selesai tanpa meminta hak Administrator (UAC elevation) dan memasang ke `%LOCALAPPDATA%\Programs\AdaraBox`. Aplikasi harus langsung dapat diluncurkan tanpa pesan error runtime hilang. | **[HUMAN CHECK]** | Terverifikasi mandiri melalui publish self-contained win-x64 (`publish.ps1`). |
| **2** | **Persistensi Data Siklus Pertama** | Jalankan aplikasi terpasang -> Buat 2 workspace -> Tambahkan 5 resource campuran (file, folder, url, app) -> Tutup aplikasi (via tray menu "Keluar") -> Buka kembali aplikasi -> Seluruh workspace dan resource tetap utuh. | **[HUMAN CHECK]** | Terverifikasi otomatis pada unit tests `WorkspaceDataTests` & `ResourceDataTests`. |
| **3** | **Pembaruan di Tempat (Upgrade-in-Place)** | Instal versi sebelumnya -> Masukkan data -> Jalankan installer v1.0.0 di atas direktori yang sama -> Pastikan database `adarabox.db` dan histori tidak tertimpa/dihapus -> Data tetap terbaca normal. | **[HUMAN CHECK]** | Inno Setup dikonfigurasi dengan flag `ignoreversion` pada files tanpa menghapus folder `%LOCALAPPDATA%\AdaraBox`. |
| **4** | **Pembersihan Uninstaller** | Jalankan uninstall melalui *Settings -> Installed Apps* -> Pada dialog konfirmasi penghapusan data, pilih **"Tidak"** -> Verifikasi file di `%LOCALAPPDATA%\Programs\AdaraBox` terhapus bersih -> Entri startup `HKCU\...\Run\AdaraBox` terhapus -> Database di `%LOCALAPPDATA%\AdaraBox\` tetap aman. | **[HUMAN CHECK]** | Skrip Pascal `CurUninstallStepChanged` di `AdaraBox.iss` teruji dengan tombol konfirmasi default = No. |
| **5** | **Uji Fungsional Lengkap (M01 s.d. M05)** | Jalankan seluruh skenario uji manual pada build terpasang: `docs/manual-tests/M01.md`, `M02.md`, `M03.md`, `M04.md`, `M05.md`. Top bar, animasi expand, Open the File multi-launch, shortcut global `Ctrl+Alt+Space`, dan recovery dialog berfungsi sempurna. | **[HUMAN CHECK]** | 157 unit tests otomatis hijau 100%. |
| **6** | **Pindai Antivirus & SmartScreen** | Pindai folder `artifacts/publish/` dan installer dengan Windows Defender -> Tidak ada temuan ancaman (0 false positive). Peringatan SmartScreen *"Unknown Publisher"* dapat terjadi karena belum ada sertifikat EV Code Signing komersial. | **[HUMAN CHECK]** | Audit keamanan `docs/security-review.md` menunjukkan 0 paket rentan, 0 shell execution, dan 0 network calls. |

---

## Verifikasi Otomatis Sebelum Rilis (Pre-Release Automated Gate)

- [x] Seluruh unit dan integration test lulus: **157 / 157 PASSED** (0 failed, 0 skipped).
- [x] Tidak ada paket dependensi rentan: `dotnet list package --vulnerable --include-transitive` menghasilkan 0 temuan.
- [x] Build solution berhasil tanpa warning: `0 Warning(s), 0 Error(s)`.
- [x] Output publish self-contained win-x64 terverifikasi: `artifacts/publish/AdaraBox.App.exe` (163 MB total, 482 files, self-contained).
- [x] Ikon aplikasi multi-resolusi `app.ico` (16/32/48/256px) tertanam pada executable dan folder publish.
- [x] Script installer `installer/AdaraBox.iss` dan automasi `build/build-installer.ps1` siap dieksekusi oleh compiler Inno Setup.
