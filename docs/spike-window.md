# Spike Report: Top Bar Window (Spike A) & Hover Trigger (Spike B)

**Dokumen Terkait:** Modul M00, Task T4 & T5  
**Lokasi Kode:** `spikes/WindowSpike/`  
**Target:** Menentukan strategi implementasi Top Bar (`Overlay Topmost` vs `AppBar`) dan mekanisme hover trigger.

---

## 1. Perbandingan Varian Top Bar

| Kriteria / Pertanyaan | Varian 1: Overlay Topmost (`Topmost=True`, `ShowActivated=False`) | Varian 2: AppBar (`SHAppBarMessage`) |
|---|---|---|
| **Pencurian Fokus saat Muncul** | **Tidak mencuri fokus.** Dengan `ShowActivated="False"`, window muncul tanpa mengambil fokus keyboard dari aplikasi aktif pengguna. | **Tidak mencuri fokus**, tetapi Windows Shell menggeser seluruh layout jendela yang dimaksimalkan saat terdaftar. |
| **Menyisihkan Ruang Layar (Desktop Work Area)** | **Tidak menyisihkan.** Mengambang di atas konten. Jendela aplikasi lain yang di-maximize tetap berukuran penuh. | **Menyisihkan.** Mengurangi ukuran `WorkArea` Windows. Semua aplikasi yang di-maximize tergeser turun sebesar tinggi bar (36 DIP). |
| **Kemampuan Input Teks (TextBox)** | **Berfungsi sempurna.** Saat pengguna mengklik TextBox pada bar, fokus keyboard berpindah secara alami dan input teks diterima. | **Berfungsi.** |
| **Efek `WS_EX_NOACTIVATE`** | Jika gaya Win32 `WS_EX_NOACTIVATE` dipaksakan, window sama sekali tidak bisa menerima fokus keyboard. **Akibatnya, input TextBox rusak/tidak dapat diketik.** | Sama jika gaya diterapkan. |
| **Perilaku di Atas App Fullscreen / Video** | Windows memprioritaskan aplikasi Exclusive Fullscreen (game/DirectX) di atas jendela Topmost standar. Top bar tetap di atas jendela non-exclusive. | AppBar dipaksa bersembunyi oleh DirectX Exclusive Fullscreen. |
| **DPI Scaling (100%, 125%, 150%)** | Ditangani secara mulus oleh WPF dengan konfigurasi manifest `PerMonitorV2`. Ukuran dan ketajaman teks/border tetap proporsional di berbagai monitor DPI tinggi. | Sama, namun kalkulasi RECT Win32 harus dikonversi manual antara physical pixel dan DIP. |
| **Kompleksitas & Risiko Bug** | **Rendah & Bersih.** Tidak ada manipulasi global shell Windows, tidak ada risiko memodifikasi layout desktop jika aplikasi crash. | **Tinggi.** Jika aplikasi crash tanpa memanggil `ABM_REMOVE`, ruang layar Windows bisa tetap kosong/rusak sampai Explorer di-restart. |

---

## 2. Parameter Hover Trigger (Spike B)

Dari pengujian prototipe pada `TriggerStripWindow`:
- **Tinggi Strip Trigger:** `3.0 DIP` di tepi paling atas layar.
  * *Temuan:* Ketinggian 3 DIP memberikan area tangkap yang cukup tanpa mengganggu interaksi titlebar jendela normal (yang biasanya setinggi 30–32 DIP).
- **Background Strip:** `#01000000` (Alpha 1/255).
  * *Temuan:* Latar belakang 100% transparan (`#00000000`) diabaikan oleh subsistem hit-test WPF. Warna hampir transparan `#01000000` tidak terlihat secara kasat mata namun menerima event mouse secara konsisten.
- **Dwell Time:** `250 ms`.
  * *Temuan:* Menggerakkan mouse cepat melintasi tepi atas layar tidak memunculkan bar secara tidak sengaja. Bar hanya muncul jika kursor sengaja ditahan selama ≥250 ms.
- **Hide Delay:** `600 ms`.
  * *Temuan:* Waktu tenggang 600 ms setelah kursor meninggalkan bar/strip mencegah efek kedip (*flicker*) jika mouse pengguna sedikit keluar dari batas bar.
- Seluruh konstanta ini dikunci di `AdaraBox.Core.Common.UiConstants`.

---

## 3. Rekomendasi Teknis (Keputusan Final M00)

**Pilihan Final: Varian 1 (Overlay Topmost, ShowActivated=False).**
* *Alasan:* Sesuai filosofi Adara Box sebagai *quick-access top bar* yang ringan dan fleksibel. Tidak merusak layout workspace desktop pengguna, tidak memerlukan interop shell berbahaya, dan mendukung input teks dengan sempurna.

---

## 4. Panduan Verifikasi Manual [HUMAN CHECK]

Jalankan executable `spikes/WindowSpike/bin/Release/net8.0-windows/WindowSpike.exe` dan uji skenario berikut:

### Skenario 1: Overlay Topmost (Varian 1)
1. Buka aplikasi lain (misal Notepad atau Browser) dan ketik sesuatu.
2. Klik tombol **"1. Test Overlay Topmost"**.
3. *Verifikasi:* Bar muncul di tengah atas layar, tetapi kursor teks di Notepad/Browser tetap aktif (fokus tidak tercuri).
4. Klik kolom "Input Test" pada bar dan ketik teks.
5. *Verifikasi:* Teks dapat diketik normal.
6. Geser jendela lain di bawah top bar.
7. *Verifikasi:* Top bar tetap berada di atas jendela tersebut.

### Skenario 2: Keterbatasan `WS_EX_NOACTIVATE`
1. Klik tombol **"2. Test WS_EX_NOACTIVATE"**.
2. Klik kolom "Input Test" pada bar dan coba ketik.
3. *Verifikasi:* Karakter tidak masuk ke TextBox (membuktikan bahwa `WS_EX_NOACTIVATE` tidak boleh dipakai jika ada input teks).

### Skenario 3: AppBar (`SHAppBarMessage`)
1. Maksimalkan jendela browser atau file explorer di layar utama.
2. Klik tombol **"3. Test AppBar"**.
3. *Verifikasi:* Jendela yang dimaksimalkan tergeser turun sebesar 36 DIP karena ruang desktop disisihkan. Tutup bar untuk memulihkan ukuran semula.

### Skenario 4: Hover Trigger Strip (Spike B)
1. Klik tombol **"4. Test Hover Trigger Strip"**.
2. Bar akan tersembunyi, strip pemicu 3px aktif di tepi atas layar.
3. Ayunkan mouse dengan cepat melewati tepi atas layar.
   * *Verifikasi:* Bar **TIDAK** muncul (dwell threshold 250ms berhasil memblokir trigger tidak sengaja).
4. Tempatkan kursor di tepi atas tengah layar dan tahan selama >250ms.
   * *Verifikasi:* Bar muncul.
5. Geser kursor keluar dari bar.
   * *Verifikasi:* Bar tetap terlihat selama 600ms grace period, lalu tersembunyi kembali jika kursor tidak kembali.
