# Panduan Pengguna Adara Box (User Guide)

Selamat datang di **Adara Box**! Panduan ini menjelaskan cara menggunakan fitur-fitur utama aplikasi Adara Box untuk meningkatkan alur kerja harian Anda.

---

## 1. Membuka & Menavigasi Top Bar

Top bar Adara Box melayang di bagian tengah atas layar monitor Anda dengan ketebalan hanya 36 DIP.

### A. Membuka dan Menutup Panel
- **Membuka Panel Modul:** Klik tombol modul pada bilah atas (**Workspace**, **Media**, atau **Stats**) atau gunakan tombol `Tab` lalu `Enter`.
- **Menutup Panel:** Klik tombol **✕** di sudut kanan bilah atas atau tekan tombol `Esc` pada keyboard Anda.

### B. Mode Sembunyikan (Hide Top Bar)
Jika Anda ingin layar sepenuhnya bersih tanpa bilah yang terlihat permanen:
1. Buka modul **Pengaturan (Settings)** pada top bar.
2. Aktifkan sakelar **Sembunyikan top bar secara otomatis**.
3. Bilah atas akan menghilang dan digantikan oleh garis pemicu tipis (Trigger Strip) setebal 2 pixel di tepi atas layar.
4. **Cara memunculkan kembali:**
   - Arahkan kursor mouse ke area tengah atas layar monitor selama sepersekian detik.
   - Atau gunakan shortcut global keyboard `Ctrl+Alt+Space`.

---

## 2. Manajemen Workspace

Workspace berfungsi mengelompokkan berbagai aplikasi, file dokumen, folder, dan link web berdasarkan tugas atau proyek tertentu.

### A. Membuat Workspace
1. Buka modul **Workspace**.
2. Klik tombol **+ Buat Workspace** (atau tekan tombol `Tab` menuju tombol tersebut).
3. Ketikkan nama workspace Anda (1–60 karakter, contoh: `Proyek Skripsi`, `Desain Web`, `Frontend Dev`).
4. Tekan tombol **Enter** atau klik **Simpan**.

### B. Mengganti Nama Workspace
1. Arahkan mouse ke kartu workspace yang ingin diubah namanya, lalu klik ikon pensil (**✏**), atau pilih kartu tersebut dan tekan tombol `F2`.
2. Ubah nama pada kotak input yang muncul.
3. Tekan **Enter** untuk menyimpan, atau tekan **Esc** untuk membatalkan.

### C. Menghapus Workspace
1. Klik ikon tempat sampah (**🗑**) pada kartu workspace, atau pilih kartu lalu tekan tombol `Delete`.
2. Sebuah jendela konfirmasi akan muncul memberitahukan nama workspace dan jumlah resource di dalamnya.
3. Klik **Hapus** untuk mengonfirmasi.
> [!NOTE]
> Menghapus workspace hanya menghapus pengelompokan di dalam Adara Box. File asli di komputer Anda sama sekali tidak akan terhapus.

---

## 3. Menambahkan Resource ke Workspace

Setelah membuat workspace, klik kartu workspace untuk masuk ke tampilan detail.

Adara Box mendukung 4 jenis resource:
1. **+ File:** Membuka dialog pemilih file Windows. Anda dapat memilih beberapa file sekaligus (multiselect).
2. **+ Folder:** Membuka pemilih folder untuk menambahkan direktori kerja.
3. **+ Aplikasi:** Menambahkan file eksekutabel (`.exe`), pintasan (`.lnk`), atau protokol aplikasi Windows Store / UWP.
4. **+ Tautan:** Menambahkan URL situs web (harus diawali dengan `http://` atau `https://`).

### Mengubah Tampilan Resource (Grid vs List)
Di sudut kanan atas detail workspace, terdapat tombol pemilih tampilan:
- **⊞ Grid:** Tampilan kartu visual horizontal dengan ikon besar dan nama yang rapi.
- **☰ List:** Tampilan baris padat dengan informasi path, tag tipe, dan tombol urut naik/turun.
Pilihan tampilan ini disimpan secara otomatis untuk setiap workspace.

### Mengurutkan & Mengedit Resource
- Pada mode List, gunakan tombol **▲** (Naik) dan **▼** (Turun) untuk mengatur urutan item sesuai kebiasaan kerja Anda.
- Klik ikon pensil (**✏**) untuk mengubah nama tampilan resource jika Anda ingin nama yang lebih ringkas.
- Jika target file telah dipindahkan atau dihapus dari disk, Adara Box akan menampilkan ikon peringatan kuning (**⚠️**).

---

## 4. Mode "Open the File" (Membuka Banyak Sekaligus)

Fitur unggulan Adara Box adalah kemampuan membuka banyak resource dalam satu langkah:

1. Di dalam detail workspace, klik tombol **Open the File** (tombol akan menyala hijau zamrud).
2. Kotak centang (checkbox) akan muncul pada setiap item resource.
3. Centang item yang ingin Anda buka, atau klik **Pilih Semua** (`Ctrl+A`).
4. Klik tombol **Open (N)** di bilah tindakan atas.
5. Adara Box akan meluncurkan seluruh file, folder, web browser, dan aplikasi tersebut secara berurutan dengan jeda halus (150ms) agar sistem operasi Anda tetap responsif.
6. **Ringkasan Hasil (Launch Summary):** Jika ada file yang gagal dibuka (misalnya target tidak ditemukan), jendela ringkasan akan memberitahukan item mana yang bermasalah beserta tombol **Coba Lagi yang Gagal**.
7. Klik **Selesai** atau tekan `Esc` untuk keluar dari mode Open the File.

---

## 5. Pintasan Keyboard (Shortcuts)

| Tombol | Fungsi |
|---|---|
| `Ctrl+Alt+Space` | Shortcut global memanggil dan memfokuskan Top Bar dari aplikasi apa pun |
| `Tab` / `Shift+Tab` | Berpindah fokus antar modul dan kartu |
| `Enter` | Membuka workspace yang dipilih / Menjalankan aksi tombol |
| `F2` | Mengganti nama workspace atau mengedit resource |
| `Delete` | Menghapus workspace atau resource (dengan konfirmasi) |
| `Space` | Mencentang / membatalkan centang resource di mode Open the File |
| `Ctrl+A` | Memilih semua resource di mode Open the File |
| `Esc` | Membatalkan form / Keluar dari mode Open the File / Menutup panel |

---

## 6. Lokasi Data & Cadangan (Backup) Manual

Semua data Adara Box disimpan di komputer lokal Anda:
- **Folder Data Utama:** `%LOCALAPPDATA%\AdaraBox\` (misal: `C:\Users\<NamaAnda>\AppData\Local\AdaraBox\`)
- **File Database:** `adarabox.db`
- **Folder Auto-Backup:** `%LOCALAPPDATA%\AdaraBox\backups\` (aplikasi secara otomatis mencadangkan database sebelum pembaruan skema dan menyimpan 5 cadangan terbaru).

### Cara Backup Manual
Jika Anda ingin memindahkan data ke komputer baru:
1. Tutup aplikasi Adara Box (klik kanan ikon Adara Box di System Tray dekat jam Windows -> klik **Keluar**).
2. Tekan tombol `Win + R`, ketik `%LOCALAPPDATA%\AdaraBox` lalu tekan Enter.
3. Salin file `adarabox.db` ke media penyimpanan Anda.
4. Untuk memulihkan, letakkan file `adarabox.db` tersebut di folder `%LOCALAPPDATA%\AdaraBox` pada komputer baru.

---

## 7. Status Modul Media & Stats

Pada versi MVP saat ini, modul **Media** dan **Stats** menampilkan status **"Masih dalam pengembangan"**:
- Adara Box memegang teguh prinsip transparansi dan tidak menampilkan metrik atau tombol palsu (mock/dummy).
- Integrasi audio player/media serta pemantau utilisasi hardware sistem (CPU/RAM/GPU) direncanakan untuk pembaruan pasca-MVP setelah fondasi inti stabil.
