# Masalah yang Diketahui & Utang Teknis (Known Issues & Technical Debt)

**Aplikasi:** Adara Box MVP (v1.0.0)  
**Tanggal:** 31 Oktober 2026

Dokumen ini mencatat keterbatasan desain, masalah prioritas rendah (P2/P3), dan keputusan teknis yang ditunda untuk rilis pasca-MVP.

---

## 1. Keterbatasan Teknis Platform Windows

### KI-01: Tampilan di Atas Aplikasi DirectX Exclusive Fullscreen
- **Deskripsi:** Top bar Adara Box dibuat menggunakan window WPF dengan properti `Topmost="True"`. Pada game atau aplikasi grafis yang menggunakan mode *Exclusive Fullscreen* (mengambil alih display adapter secara langsung), jendela topmost Windows tidak dapat ditampilkan di atas lapisan swapchain eksklusif tersebut.
- **Dampak:** Rendah (P2). Pengguna game full-screen eksklusif tidak dapat melihat top bar tanpa beralih jendela (`Alt+Tab`).
- **Solusi Sementara (Workaround):** Gunakan mode *Borderless Windowed* atau *Windowed Fullscreen* pada pengaturan game.

### KI-02: Peluncuran Target yang Membutuhkan Hak Administrator (UAC Elevation)
- **Deskripsi:** Adara Box sengaja dirancang untuk berjalan dalam izin pengguna standar (`PrivilegesRequired=lowest`). Jika sebuah resource yang ditambahkan ke workspace adalah aplikasi yang secara eksplisit membutuhkan hak administrator (misalnya tool konfigurasi sistem), Windows Shell akan memunculkan prompt dialog persetujuan UAC.
- **Dampak:** Sesuai desain keamanan Windows.
- **Rencana Lanjutan:** Mengevaluasi opsi penanda visual (ikon perisai UAC) untuk aplikasi yang memerlukan elevasi pada rilis mendatang.

### KI-03: Peringatan Windows SmartScreen pada File Installer
- **Deskripsi:** Paket instalasi `AdaraBox-Setup-1.0.0.exe` yang dihasilkan belum ditandatangani secara digital menggunakan sertifikat EV Code Signing komersial berbayar. Windows Defender SmartScreen kemungkinan akan memunculkan dialog peringatan *"Windows protected your PC"* saat pengguna pertama kali menjalankan installer.
- **Dampak:** Pengguna perlu mengeklik *"More info"* lalu *"Run anyway"*.
- **Mitigasi Pasca-MVP:** Menyiapkan proses automated code-signing pada pipeline CI/CD rilis publik.

---

## 2. Fitur yang Ditunda ke Pasca-MVP

| Item | Deskripsi | Rencana Target |
|---|---|---|
| **Drag-and-Drop File Explorer** | Menambahkan file atau folder ke workspace dengan menariknya (drag) langsung dari Windows File Explorer ke panel Adara Box. | Pasca-MVP (v1.1) |
| **Modul Stats (FR-ST-03..06)** | Pemantauan metrik hardware real-time (utilisasi CPU, RAM, Disk, dan GPU) menggunakan WMI/PerformanceCounter tanpa membebani thread UI. | Modul M07 |
| **Modul Media (FR-MD-03..05)** | Kontrol pemutaran media lokal atau integrasi mini-player web service. | Modul M08 |
| **Kategori / Tagging Resource** | Pengelompokan resource di dalam satu workspace berdasarkan tag atau folder virtual. | Pasca-MVP (v1.2) |
| **Opsi Auto-Update** | Pemeriksaan versi baru di GitHub Releases dan pembaruan otomatis tanpa perlu mengunduh installer ulang secara manual. | Pasca-MVP (v1.2) |

---

## 3. Catatan Utang Teknis Internal

1. **Ukuran Paket Self-Contained:** Paket publish mandiri win-x64 berukuran ~163 MB karena mencakup runtime .NET 8 dan native WPF DirectX composition engine. Jika di kemudian hari dibutuhkan installer yang lebih kecil (< 15 MB), dapat disediakan opsi installer alternatif berbasis *Framework-Dependent* yang mengasumsikan .NET 8 Desktop Runtime telah terpasang di komputer pengguna.
2. **Kustomisasi Shortcut Global:** Pengaturan shortcut global saat ini mendukung kombinasi modifier (`Ctrl`, `Alt`, `Shift`, `Win`) dengan tombol alfabet dan tombol fungsi (`F1`–`F12`). Tombol mouse tambahan (misalnya mouse button 4/5) belum didukung untuk aktivasi hotkey.
