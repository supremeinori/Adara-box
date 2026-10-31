# Adara Box

> **Adara Box** adalah aplikasi produktivitas desktop Windows minimalis yang menyediakan akses cepat dan terorganisir ke workspace harian Anda melalui overlay top bar yang elegan dan hemat ruang.

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![Platform: Windows](https://img.shields.io/badge/Platform-Windows%2010%20%7C%2011-0078D6.svg)](https://microsoft.com/windows)
[![.NET 8.0](https://img.shields.io/badge/.NET-8.0-512BD4.svg)](https://dotnet.microsoft.com/)

---

## Fitur Utama (MVP v1.0.0)

- ✦ **Top Bar Minimalis & Non-Intrusif:** Selalu melayang tipis (36 DIP) di tengah atas layar tanpa mencuri fokus kerja atau menutupi bilah judul aplikasi lain.
- 📁 **Manajemen Workspace Berbasis Konteks:** Kelompokkan file, folder proyek, aplikasi desktop/UWP, dan tautan web ke dalam workspace terpisah (misalnya: *Kerja*, *Desain*, *Gaming*, *Riset*).
- 🚀 **Open the File (Multi-Launch Engine):** Buka seluruh atau sebagian resource pilihan Anda secara bersamaan dengan satu klik melalui background pacing yang aman (tanpa membuat Windows lag atau freeze).
- ⊞ **Tampilan Fleksibel (Grid & List):** Pilih tata letak kartu grid visual atau daftar list padat sesuai preferensi Anda per workspace.
- ⚠️ **Deteksi Ketersediaan Real-Time:** Memberikan indikator peringatan visual jika file/folder target telah dipindahkan, diganti nama, atau dihapus dari disk.
- 🛡️ **Aturan Emas Referensi (Non-Destructive):** Menghapus resource dari Adara Box **hanya** menghapus referensi di database aplikasi; file fisik asli Anda di disk tidak pernah disentuh atau dihapus.
- 🔒 **100% Offline & Mengutamakan Privasi:** Tanpa login, tanpa analitik/telemetri, tanpa koneksi jaringan. Data Anda sepenuhnya tersimpan secara lokal di mesin Anda.
- 💾 **Hardening & Pemulihan Database:** Dilengkapi auto-backup sebelum migrasi skema dan dialog pemulihan interaktif jika file database rusak saat startup.
- 🖥️ **Dukungan Multi-Monitor & DPI:** Top bar otomatis muncul di monitor tempat kursor berada (PerMonitorV2 DPI aware) dan reposisi dinamis jika susunan monitor berubah.
- ⌨️ **Navigasi Keyboard & Shortcut Global:** Buka/fokus top bar dari aplikasi mana pun dengan shortcut global (`Ctrl+Alt+Space`), navigasi penuh dengan tombol panah, `Enter`, `F2` (rename), `Delete` (hapus), `Space` (pilih), dan `Esc`.

---

## Cara Memasang (Instalasi)

### Opsi 1: Menggunakan Installer Resmi (Rekomendasi)
1. Unduh `AdaraBox-Setup-1.0.0.exe` dari [Halaman Rilis GitHub](https://github.com/supremeinori/Adara-box/releases).
2. Jalankan installer. Pemasangan bersifat *per-user* ke `%LOCALAPPDATA%\Programs\AdaraBox` (tidak memerlukan hak administrator).
3. Anda dapat memilih untuk membuat shortcut Desktop dan mengaktifkan startup bersama Windows.

### Opsi 2: Versi Portable (Self-Contained)
1. Unduh arsip zip rilis dan ekstrak ke direktori pilihan Anda.
2. Jalankan `AdaraBox.App.exe`. Tidak diperlukan instalasi .NET Runtime terpisah (seluruh runtime dan dependensi telah dibundel).

---

## Panduan Penggunaan Singkat

- **Menampilkan / Menyembunyikan Top Bar:**
  - Tekan shortcut global: `Ctrl+Alt+Space`.
  - Jika opsi *Hide Top Bar* aktif di Pengaturan: geser kursor mouse ke tepi tengah paling atas layar monitor untuk memunculkan bar sementara.
- **Membuat Workspace Baru:**
  - Klik tombol `+ Buat Workspace` atau tekan `Tab` lalu `Enter`.
  - Masukkan nama workspace (maksimal 60 karakter) lalu tekan `Enter` untuk menyimpan.
- **Menambahkan Resource:**
  - Masuk ke detail workspace, klik salah satu tombol: `+ File`, `+ Folder`, `+ Aplikasi`, atau `+ Tautan`.
- **Mode Buka Banyak (Open the File):**
  - Klik tombol **Open the File** di header workspace.
  - Centang item yang ingin dibuka (atau tekan `Ctrl+A` untuk memilih semua).
  - Klik tombol **Open (N)** untuk meluncurkan resource secara bersamaan.
  - Jika ada target yang gagal dibuka, dialog ringkasan akan memberitahukan alasan kegagalan dan menyediakan tombol untuk mencoba lagi item yang gagal.

Panduan pengguna selengkapnya tersedia di [docs/USER_GUIDE.md](docs/USER_GUIDE.md).

---

## Cara Build dari Source Code

### Prasyarat
- Sistem Operasi: Windows 10 (versi 21H2 build 19044 atau lebih baru) / Windows 11.
- SDK: [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (x64).
- PowerShell 7 atau Windows PowerShell 5.1.
- Inno Setup 6 (opsional, hanya jika ingin mengompilasi file installer `.exe`).

### Langkah Kompilasi & Pengujian

```powershell
# 1. Clone repository
git clone https://github.com/supremeinori/Adara-box.git
cd Adara-box

# 2. Jalankan automated unit & integration tests
dotnet test AdaraBox.sln -c Release

# 3. Build aplikasi
dotnet build AdaraBox.sln -c Release

# 4. Jalankan script publish self-contained
powershell -ExecutionPolicy Bypass -File build/publish.ps1

# 5. (Opsional) Buat paket installer Inno Setup
powershell -ExecutionPolicy Bypass -File build/build-installer.ps1
```

---

## Struktur Direktori Repository

```text
Adara-box/
├── src/
│   ├── AdaraBox.Core/          # Domain model, antarmuka, validasi target, state machine
│   ├── AdaraBox.Data/          # SQLite repository, migrasi skema, backup & integrity
│   ├── AdaraBox.Platform/      # Win32 shell integration, shortcut global, launcher, icon provider
│   └── AdaraBox.App/           # UI desktop WPF (Views, ViewModels, resources, DI startup)
├── tests/
│   ├── AdaraBox.Core.Tests/     # Unit tests domain logic & validation (73 tests)
│   ├── AdaraBox.Data.Tests/     # SQLite database, backup, & benchmark tests (29 tests)
│   ├── AdaraBox.Platform.Tests/ # Windows platform launcher & icon provider tests (31 tests)
│   └── AdaraBox.App.Tests/      # ViewModel & UI logic tests (24 tests)
├── build/                       # Script automasi publish dan packaging installer
├── installer/                   # Script Inno Setup (.iss)
├── assets/                      # Ikon aplikasi (app.ico multi-resolusi)
└── docs/                        # Dokumentasi arsitektur, panduan, audit & pengujian
```

---

## Lokasi Data & Log Aplikasi

Seluruh data lokal disimpan di dalam direktori profil pengguna:
- **Database SQLite:** `%LOCALAPPDATA%\AdaraBox\adarabox.db`
- **Cadangan Database (Auto-Backup):** `%LOCALAPPDATA%\AdaraBox\backups\` (menyimpan 5 cadangan terbaru)
- **Log Serilog:** `%LOCALAPPDATA%\AdaraBox\logs\adarabox-.log` (rotasi otomatis 14 hari)

---

## Keterbatasan yang Diketahui (Known Limitations)

1. **Aplikasi Game Fullscreen Eksklusif:** Top bar Adara Box melayang sebagai topmost window standar Windows. Pada game atau aplikasi yang berjalan dalam mode *Exclusive Fullscreen DirectX*, top bar tidak akan terlihat di atas layar game tersebut (gunakan mode *Borderless Windowed*).
2. **Aplikasi yang Memerlukan Hak Administrator (UAC):** Jika Adara Box dijalankan dalam hak akses pengguna standar (tanpa admin), meluncurkan aplikasi yang secara eksplisit meminta elevasi administrator akan memicu prompt UAC Windows standar.
3. **SmartScreen Warning:** Setup executable yang dibuild dari source belum ditandatangani dengan sertifikat digital komersial (Code Signing Certificate), sehingga Windows SmartScreen dapat menampilkan peringatan keamanan saat pertama kali dipasang.

---

## Lisensi

Proyek ini dilisensikan di bawah [MIT License](LICENSE).
