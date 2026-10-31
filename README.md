# Adara Box

> **Adara Box** adalah aplikasi produktivitas desktop Windows bertema **Apple Dark Neomorphism** yang sangat ringan, hemat memori, dan non-intrusif. Dirancang dengan overlay top bar (ala macOS Menu Bar) yang elegan untuk menyediakan akses instan 1-klik ke workspace harian Anda, pengontrol musik native Windows (GSMTC), dan pemantau performa hardware PC secara real-time.

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![Platform: Windows](https://img.shields.io/badge/Platform-Windows%2010%20%7C%2011-0078D6.svg)](https://microsoft.com/windows)
[![.NET 8.0](https://img.shields.io/badge/.NET-8.0-512BD4.svg)](https://dotnet.microsoft.com/)
[![Tests: 229 Passed](https://img.shields.io/badge/Tests-229%20Passed-success.svg)](tests/)

---

## 🚀 Download & Instalasi (Tinggal Download 1 File)

Untuk pengguna umum yang ingin langsung memakai Adara Box, Anda hanya perlu mengunduh **satu file installer**:

### 📥 [Unduh AdaraBox-Setup-1.0.0.exe](https://github.com/supremeinori/Adara-box/releases)

1. **Jalankan Installer**: Buka `AdaraBox-Setup-1.0.0.exe`.
2. **Pemasangan Instan (Per-User)**: Otomatis terpasang di `%LOCALAPPDATA%\Programs\AdaraBox` tanpa memerlukan hak Administrator (UAC).
3. **Pilihan Otomatis**: Buat ikon desktop dan opsi menjalankan otomatis saat Windows dinyalakan.
4. **Langsung Pakai**: Tekan shortcut global `Ctrl+Alt+Space` kapan saja untuk memanggil Adara Box!

*(Tersedia juga versi portable self-contained `AdaraBox-Portable-win-x64.zip` bagi yang tidak ingin melakukan instalasi).*

---

## ✨ Fitur Unggulan

### 1. ✦ Apple Menu Bar & Dark Neomorphism
- **Desain Ultra-Kompak (30 DIP):** Melayang tipis di tengah atas layar ala macOS Menu Bar tanpa menutupi bilah judul aplikasi lain atau mencuri fokus kerja.
- **Dark Neomorphism Fisik:** Sentuhan tekstur matte charcoal (`#16161B`), hairline top highlight halus (`#25FFFFFF`), soft ambient drop shadow, dan efek kompresi taktil instan (`scale 0.97`) saat tombol atau kartu ditekan.
- **Animasi Pop-Down & Pop-Up:** Panel modul meluncur turun dengan halus (*critically damped spring ease*) saat dibuka, dan meluncur kembali ke atas saat ditutup.

### 2. 🎵 Native Windows GSMTC Media Controller
- **Kontrol Musik Langsung:** Terintegrasi langsung dengan Windows WinRT Native Media API (*GlobalSystemMediaTransportControls*).
- **Mendukung Segala Player Musik:** Mengontrol lagu yang diputar di Spotify Desktop, Apple Music, VLC, maupun tab browser (YouTube, SoundCloud di Chrome, Edge, Firefox).
- **Fitur Lengkap:** Play/Pause, Next Track, Previous Track, dan tampilan judul lagu + artis aktif langsung di Top Bar dan panel Media dengan memori hemat (<0.5% CPU, tanpa Chromium/WebView2).

### 3. 📁 Workspace Grid View & Kustomisasi Logo (Gambar & Emoji)
- **Tampilan Grid Responsif:** Kartu workspace tertata rapi dalam format grid yang modern dan intuitif.
- **Logo Gambar Kustom:** Pasang gambar logo kustom dari komputer Anda (`.png`, `.jpg`, `.ico`, `.webp`). Gambar otomatis dicache dengan aman di folder data aplikasi sehingga tidak akan hilang.
- **Preset Emoji & Custom:** Pilihan preset emoji populer (📁, 💼, 🚀, 💻, 🎨, 📚, 🎮, 🎧, ⚡, 🌐, ☕, 🛠️, 📊, 🎯, 🎬, 💡) jika tidak menggunakan gambar.
- **Detail Header:** Logo kustom Anda tampil elegan di kartu grid dan header detail workspace.

### 4. ⚡ 1-Click Instant Shortcut Launch
- **Buka Langsung:** Cukup 1 klik pada file, folder, aplikasi, atau tautan apa saja untuk langsung meluncurkannya tanpa langkah berbelit.
- **Tombol "🚀 Buka Semua":** Luncurkan seluruh file dan aplikasi di dalam workspace sekaligus dengan pacing aman di background.
- **Deteksi Ketersediaan Real-Time:** Peringatan visual otomatis jika berkas target telah dipindah atau dihapus dari disk.

### 5. 📊 Real-Time Hardware Performance Monitor
- Pemantau beban CPU, GPU (3D Engine counter), RAM, dan kapasitas penyimpanan drive lokal secara langsung dengan tampilan progress bar monokrom yang elegan.

### 6. 🛡️ Keamanan & Privasi 100% Offline
- **Golden Rule Non-Destructive:** Menghapus workspace atau resource dari Adara Box **hanya** menghapus referensinya di database; berkas fisik asli Anda di komputer tidak akan pernah disentuh atau dihapus.
- **100% Offline:** Tanpa registrasi, tanpa analitik/telemetri, tanpa koneksi cloud. Seluruh database tersimpan di mesin lokal Anda.

---

## ⌨️ Pintasan Keyboard (Shortcuts)

| Tombol Pintasan | Fungsi |
| :--- | :--- |
| `Ctrl + Alt + Space` | Buka / Sembunyikan Top Bar dari aplikasi mana pun |
| `Esc` | Tutup panel aktif atau batalkan form input |
| `Enter` / `Space` | Luncurkan resource terpilih atau simpan form |
| `F2` | Ganti nama & logo workspace atau resource |
| `Delete` | Hapus workspace atau resource |
| `Ctrl + A` | Pilih semua resource dalam mode seleksi |

---

## 🛠️ Cara Build dari Source Code

### Prasyarat
- Sistem Operasi: Windows 10 (versi 21H2 build 19044 atau lebih baru) / Windows 11.
- SDK: [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (x64).
- Inno Setup 6 (untuk membuat file installer `.exe`).

### Langkah Build & Testing

```powershell
# 1. Clone repository
git clone https://github.com/supremeinori/Adara-box.git
cd Adara-box

# 2. Jalankan seluruh automated test suite (229 tests)
dotnet test AdaraBox.sln -c Release

# 3. Publish rilis mandiri (Self-Contained win-x64)
powershell -ExecutionPolicy Bypass -File build/publish.ps1

# 4. Bangun paket installer tunggal (AdaraBox-Setup-1.0.0.exe)
powershell -ExecutionPolicy Bypass -File build/build-installer.ps1 -SkipPublish
```

Hasil installer setup akan tersedia di `artifacts/installer/AdaraBox-Setup-1.0.0.exe` (~52 MB).

---

## 📂 Lokasi Data & Log Aplikasi

Semua data lokal disimpan rapi di dalam direktori profil pengguna:
- **Database SQLite:** `%LOCALAPPDATA%\AdaraBox\adarabox.db`
- **Logo & Gambar Workspace:** `%LOCALAPPDATA%\AdaraBox\workspace-icons\`
- **Cadangan Database (Auto-Backup):** `%LOCALAPPDATA%\AdaraBox\backups\`
- **Log Serilog:** `%LOCALAPPDATA%\AdaraBox\logs\adarabox-.log` (rotasi otomatis 14 hari)

---

## 📄 Lisensi

Proyek ini dilisensikan di bawah [MIT License](LICENSE).
