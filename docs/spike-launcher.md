# Spike Report: Resource Launcher Verification (Spike C)

**Dokumen Terkait:** Modul M00, Task T6  
**Lokasi Kode:** `spikes/LauncherSpike/` & `tests/AdaraBox.Platform.Tests/LauncherTests.cs`  
**Target:** Memverifikasi pembukaan resource dengan `Process.Start(UseShellExecute = true)` tanpa shell command string, serta memetakan Win32 Error Codes.

---

## 1. Hasil Pengujian Jenis Resource

Semua pengujian dilakukan murni melalui `ProcessStartInfo` dengan `UseShellExecute = true` (mematuhi **NFR-SC-04** dan **Aturan Emas #3: Tidak ada eksekusi shell**):

| Jenis Resource | Target Uji | Hasil Pengujian | Catatan Teknis |
|---|---|---|---|
| **File (Spasi & Unicode)** | `C:\Users\...\Temp\...\Dokumén ü.txt` | **Berhasil** | Windows Shell mendeteksi asosiasi file `.txt` dan membuka editor default tanpa issue encoding. |
| **Folder (Spasi)** | `C:\Users\...\Temp\...\Folder Uji Coba` | **Berhasil** | Folder terbuka di Windows Explorer. |
| **Tautan Web** | `https://example.com` | **Berhasil** | Terbuka di browser default pengguna. |
| **Aplikasi Executable** | `notepad.exe` | **Berhasil** | Terbuka langsung. |
| **Shortcut (.lnk)** | Shortcut Windows standar | **Berhasil** | Ditangani secara transparan oleh Windows Shell. |
| **Windows Store Apps** | `shell:AppsFolder\<PackageFamilyName>!...` | **Berhasil** | Protokol `shell:AppsFolder` dapat dieksekusi langsung oleh ShellExecute. |

---

## 2. Pemetaan Kode Kesalahan Win32 (`NativeErrorCode`)

Saat pembukaan gagal, `Process.Start` melempar `System.ComponentModel.Win32Exception`. Kode kesalahan numerik (`NativeErrorCode`) dipetakan ke `OperationError` untuk dilaporkan per item di M04:

| Win32 Error Code | Konstanta Win32 | Kode `OperationError` di Adara Box | Pesan UI (Bahasa Indonesia) |
|---|---|---|---|
| **2** | `ERROR_FILE_NOT_FOUND` | `ResourceNotFound` | "Target tidak ditemukan di path yang ditentukan." |
| **5** | `ERROR_ACCESS_DENIED` | `AccessDenied` | "Akses ke file/folder ditolak (memerlukan izin khusus)." |
| **1155** | `ERROR_NO_ASSOCIATION` | `NoAssociation` | "Tidak ada aplikasi default di Windows untuk membuka jenis file ini." |
| **1223** | `ERROR_CANCELLED` | `CancelledByUser` | "Tindakan dibatalkan oleh pengguna (prompt UAC ditolak)." |
| *Lainnya* | (Variatif) | `LaunchFailed` | "Gagal membuka resource (Win32 Error: {code})." |

---

## 3. Temuan `WorkingDirectory`

* **Masalah:** Banyak aplikasi desktop (terutama aplikasi pihak ketiga portabel, game, atau alat dev) mengasumsikan folder executable sebagai direktori kerja saat ini untuk membaca konfigurasi dan DLL lokal. Jika `WorkingDirectory` tidak diisi, proses akan menggunakan direktori Adara Box sebagai *working directory*, yang bisa menyebabkan aplikasi target gagal memuat dependensi lokalnya.
* **Solusi/Keputusan:** Untuk resource bertipe Executable (`.exe`), Adara Box **harus secara otomatis menyetel** `psi.WorkingDirectory = Path.GetDirectoryName(exePath)`.
