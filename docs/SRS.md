# Adara Box

## Software Requirements Specification (SRS)

**Versi:** 0.2 --- Draft\
**Status:** Draft untuk validasi desain dan MVP\
**Platform target:** Windows desktop\
**Dokumen:** Software Requirements Specification

------------------------------------------------------------------------

## 1. Pendahuluan

### 1.1 Tujuan Dokumen

Dokumen ini mendefinisikan kebutuhan fungsional, kebutuhan
nonfungsional, batasan, rancangan konseptual, dan kriteria penerimaan
untuk **Adara Box**, aplikasi desktop Windows yang menyediakan top bar
ringkas sebagai titik akses cepat ke workspace, resource proyek, media,
dan informasi sistem.

Dokumen ini menjadi acuan untuk: - merancang antarmuka dan alur
interaksi; - menentukan ruang lingkup Minimum Viable Product (MVP); -
mengimplementasikan dan menguji fitur; - membedakan fitur yang sudah
tersedia, belum diimplementasikan, atau masih dalam pengembangan.

Dokumen ini merupakan rancangan awal. Detail yang belum diputuskan harus
divalidasi sebelum implementasi terkait dimulai.

### 1.2 Tujuan Produk

Adara Box bertujuan menyediakan akses cepat dan terorganisasi ke
kumpulan resource yang digunakan dalam aktivitas berbeda, tanpa
mengharuskan pengguna membuka dan mencari setiap aplikasi, folder, file,
atau tautan secara manual.

Produk berpusat pada top bar yang selalu mudah dijangkau. Fitur utama
MVP adalah **Workspace**, tempat pengguna membuat kelompok resource
untuk suatu proyek atau aktivitas, lalu membuka beberapa resource yang
dipilih sekaligus.

### 1.3 Ruang Lingkup

Adara Box direncanakan sebagai aplikasi desktop Windows dengan modul
yang dapat dikembangkan secara bertahap.

Ruang lingkup yang direncanakan: - top bar yang ringkas dan dapat
disembunyikan; - pengelolaan workspace; - pengelolaan resource di dalam
workspace; - tampilan resource dalam grid atau list; - mode pemilihan
resource untuk membuka beberapa resource sekaligus; - panel Media dan
Stats yang dapat dikembangkan setelah fitur inti; - indikator visual
untuk fitur yang belum tersedia atau masih dalam pengembangan.

**Prioritas MVP adalah Workspace dan alur pembukaan resource.** Media
dan Stats dapat ditampilkan sebagai modul yang belum tersedia atau masih
dalam pengembangan apabila belum diimplementasikan.

### 1.4 Definisi dan Istilah

  -----------------------------------------------------------------------
  Istilah                             Definisi
  ----------------------------------- -----------------------------------
  Top bar                             Bilah antarmuka kecil di bagian
                                      atas layar sebagai akses utama ke
                                      modul Adara Box.

  Panel                               Area antarmuka yang melebar secara
                                      horizontal dari top bar saat sebuah
                                      modul dibuka.

  Workspace                           Kumpulan resource yang
                                      dikelompokkan berdasarkan proyek
                                      atau aktivitas.

  Resource                            Referensi ke aplikasi, file,
                                      folder, atau tautan yang dapat
                                      dibuka dari Adara Box.

  Open the File                       Mode untuk memilih dan membuka satu
                                      atau beberapa resource di dalam
                                      workspace.

  WebView                             Komponen browser yang ditanam di
                                      dalam aplikasi desktop.

  MVP                                 Versi awal dengan fitur inti yang
                                      cukup untuk digunakan dan
                                      dievaluasi.

  Placeholder                         Tampilan sementara yang menunjukkan
                                      fitur belum tersedia atau masih
                                      dikembangkan.
  -----------------------------------------------------------------------

------------------------------------------------------------------------

## 2. Gambaran Umum Produk

### 2.1 Perspektif Produk

Adara Box merupakan aplikasi desktop mandiri. Adara Box menyimpan
konfigurasi workspace dan referensi resource secara lokal. File dan
aplikasi yang ditambahkan tidak disalin ke dalam penyimpanan Adara Box;
aplikasi menyimpan referensi atau lokasi resource tersebut.

Adara Box bukan pengganti file manager, launcher aplikasi, atau browser.
Aplikasi bertindak sebagai lapisan akses cepat yang mengelompokkan
resource dan membantu pengguna membukanya sesuai konteks aktivitas.

### 2.2 Pengguna Sasaran

Pengguna sasaran adalah pengguna Windows yang bekerja dengan banyak
aplikasi, file, folder, dan tautan untuk beberapa proyek atau aktivitas,
misalnya: - pekerjaan kreatif dan produksi konten; - pengembangan
perangkat lunak; - kegiatan kuliah; - pengelolaan referensi dan
membaca; - proyek personal.

### 2.3 Prinsip Produk

1.  **Workspace-first:** Workspace merupakan fitur utama dan prioritas
    implementasi pertama.
2.  **Quick access:** Resource harus mudah ditemukan dan dibuka.
3.  **Reference, not copy:** Resource yang ditambahkan tetap berada di
    lokasi aslinya.
4.  **Separate management from opening:** Pengelolaan resource tidak
    bercampur dengan pemilihan resource untuk dibuka.
5.  **Progress transparency:** Fitur yang belum tersedia harus ditandai
    secara jelas, bukan dibuat seolah-olah sudah berfungsi.
6.  **Modular development:** Media dan Stats dapat dikembangkan terpisah
    dari fitur Workspace.

### 2.4 Lingkungan Operasi

-   Sistem operasi target: Windows 10 atau Windows 11, dengan versi
    minimum final ditentukan saat implementasi.
-   Penggunaan utama: desktop atau laptop Windows.
-   Penyimpanan konfigurasi: lokal pada perangkat.
-   Koneksi internet: tidak wajib untuk pengelolaan Workspace lokal;
    diperlukan oleh resource daring yang dibuka pengguna.
-   Integrasi pihak ketiga: bukan prasyarat MVP.

### 2.5 Batasan dan Ketergantungan

-   Resource hanya dapat dibuka jika target masih tersedia dan pengguna
    memiliki izin akses.
-   Aplikasi atau jenis file tertentu bergantung pada aplikasi default
    dan asosiasi file di Windows.
-   Membuka beberapa resource sekaligus dapat memerlukan waktu dan
    sumber daya sistem sesuai jenis resource.
-   Browser mini dan integrasi layanan pihak ketiga belum menjadi bagian
    wajib MVP.
-   Pengukuran GPU dan beberapa metrik sistem dapat bergantung pada
    perangkat keras, driver, dan API yang tersedia.

------------------------------------------------------------------------

## 3. Arsitektur dan Struktur Antarmuka Konseptual

### 3.1 Top Bar

Top bar merupakan titik akses utama Adara Box. Top bar dirancang kecil,
berada di bagian atas layar, dan tetap tersedia saat aplikasi berjalan.

Top bar memiliki dua perilaku visibilitas: - **Always visible:** top bar
tetap terlihat dalam keadaan ringkas. - **Hide:** top bar disembunyikan;
pengguna dapat memunculkannya kembali dengan mengarahkan kursor ke area
pemicu di bagian atas layar.

Status visibilitas harus dapat dikendalikan pengguna melalui pengaturan.
Perilaku pemicu, fokus keyboard, dan penempatan top bar harus diuji agar
tidak menghalangi penggunaan aplikasi lain.

Modul yang direncanakan untuk diakses melalui top bar: - Workspace; -
Media; - Stats; - Add Workspace; - Settings atau menu pengaturan.

Susunan dan ikon final ditentukan pada tahap desain UI.

### 3.2 Panel Modul

Saat modul dipilih, panel modul muncul dari top bar dan melebar secara
horizontal. Panel tidak harus membuka jendela aplikasi terpisah untuk
setiap modul.

Panel harus: - menunjukkan modul yang sedang aktif; - memiliki cara yang
jelas untuk ditutup atau kembali ke top bar; - menjaga agar interaksi
panel tidak mengubah data workspace secara tidak sengaja; - menyesuaikan
isi dengan ukuran jendela dan resolusi layar yang tersedia.

Detail arah pelebaran, ukuran panel, perilaku ketika kehilangan fokus,
dan responsivitas akan diputuskan melalui prototipe UI.

### 3.3 Modul Workspace

Workspace adalah modul inti. Panel Workspace menampilkan workspace yang
telah dibuat dan memungkinkan pengguna membuka, mengelola, atau membuat
workspace baru.

Contoh workspace: - Carry On; - ARUS; - College; - Reading.

Nama tersebut merupakan contoh penggunaan, bukan workspace bawaan yang
diwajibkan.

### 3.4 Modul Media

Media direncanakan sebagai tempat akses media atau layanan media.
Integrasi layanan spesifik belum menjadi ketergantungan MVP.

Eksplorasi berikut dapat dilakukan setelah fitur Workspace stabil: -
membuka layanan media eksternal; - menampilkan layanan web di dalam
panel melalui WebView; - menyediakan kontrol media sistem jika didukung.

Jika fitur belum diimplementasikan, panel Media harus menampilkan status
**"Masih dalam pengembangan"** atau status lain yang setara. Placeholder
tidak boleh menampilkan kontrol interaktif palsu.

### 3.5 Modul Stats

Stats direncanakan menampilkan kondisi sistem secara real-time, tanpa
histori atau grafik historis pada ruang lingkup saat ini.

Metrik yang dipertimbangkan: - penggunaan CPU; - penggunaan RAM; -
penggunaan storage; - penggunaan GPU jika tersedia dan dapat diambil
secara andal.

Jika belum diimplementasikan, panel Stats harus menunjukkan status
pengembangan dengan jelas.

------------------------------------------------------------------------

## 4. Ruang Lingkup MVP

### 4.1 Fitur Wajib MVP

1.  Menampilkan top bar.
2.  Menyediakan perilaku visibilitas top bar.
3.  Membuat, melihat, mengubah nama, dan menghapus workspace.
4.  Menambahkan, melihat, mengubah detail, dan menghapus resource di
    workspace.
5.  Mendukung resource berupa aplikasi, file, folder, dan tautan.
6.  Menyediakan tampilan grid dan list.
7.  Menyediakan mode Open the File untuk memilih beberapa resource.
8.  Membuka resource terpilih menggunakan mekanisme yang sesuai pada
    Windows.
9.  Menangani resource yang tidak ditemukan atau gagal dibuka.
10. Menyimpan data workspace secara lokal agar tersedia setelah aplikasi
    ditutup dan dibuka kembali.
11. Menampilkan status pengembangan secara eksplisit untuk fitur non-MVP
    yang belum tersedia.

### 4.2 Fitur yang Boleh Ditampilkan sebagai Placeholder

-   Media;
-   Stats;
-   integrasi layanan media;
-   browser mini/WebView;
-   kontrol media sistem;
-   metrik GPU jika belum didukung.

Placeholder harus membedakan dengan jelas antara: - belum
diimplementasikan; - sedang dalam pengembangan; - gagal dimuat atau
mengalami kesalahan.

### 4.3 Di Luar Ruang Lingkup MVP

-   Sinkronisasi cloud dan akun pengguna;
-   berbagi workspace antarpengguna;
-   integrasi Spotify API sebagai kebutuhan inti;
-   otomatisasi dan orkestrasi workflow kompleks;
-   plugin pihak ketiga;
-   AI assistant;
-   histori metrik sistem;
-   penghapusan atau pemindahan file asli dari dalam Adara Box;
-   penyimpanan salinan resource yang ditambahkan.

------------------------------------------------------------------------

## 5. Kebutuhan Fungsional

Prioritas: - **P0:** wajib untuk MVP; - **P1:** penting, dapat menyusul
setelah fungsi inti; - **P2:** eksplorasi atau pengembangan lanjutan.

### 5.1 Top Bar

**FR-TB-01 --- Menampilkan top bar (P0)**\
Sistem harus menampilkan top bar ringkas saat aplikasi aktif, kecuali
pengguna mengaktifkan opsi Hide.

**FR-TB-02 --- Menyediakan akses modul (P0)**\
Top bar harus menyediakan akses ke Workspace dan akses visual ke modul
lain yang direncanakan, termasuk Media dan Stats.

**FR-TB-03 --- Mengatur visibilitas (P0)**\
Sistem harus menyediakan pengaturan untuk menyembunyikan atau
menampilkan top bar.

**FR-TB-04 --- Memunculkan top bar saat tersembunyi (P0)**\
Saat Hide aktif, pengguna harus dapat memunculkan top bar melalui
interaksi pointer pada area pemicu di bagian atas layar. Mekanisme final
harus diuji untuk menghindari pemunculan yang tidak disengaja.

**FR-TB-05 --- Mempertahankan preferensi (P1)**\
Preferensi visibilitas top bar harus disimpan dan diterapkan kembali
saat aplikasi dijalankan berikutnya.

**FR-TB-06 --- Menyediakan akses keyboard (P1)**\
Sistem sebaiknya menyediakan shortcut keyboard untuk menampilkan atau
memfokuskan top bar. Shortcut harus dapat dikonfigurasi atau menghindari
konflik umum dengan shortcut sistem.

### 5.2 Workspace

**FR-WS-01 --- Menampilkan daftar workspace (P0)**\
Sistem harus menampilkan daftar workspace yang tersimpan.

**FR-WS-02 --- Membuat workspace (P0)**\
Pengguna harus dapat membuat workspace dengan nama yang tidak kosong.

**FR-WS-03 --- Mengubah nama workspace (P0)**\
Pengguna harus dapat mengubah nama workspace yang telah dibuat.

**FR-WS-04 --- Menghapus workspace (P0)**\
Pengguna harus dapat menghapus workspace. Sistem harus meminta
konfirmasi jika tindakan tersebut berpotensi menghilangkan konfigurasi
yang tersimpan.

Penghapusan workspace hanya menghapus data workspace dan referensi
resource dari Adara Box, bukan file, folder, atau aplikasi aslinya.

**FR-WS-05 --- Membuka workspace (P0)**\
Pengguna harus dapat memilih workspace untuk melihat resource di
dalamnya.

**FR-WS-06 --- Mempertahankan workspace (P0)**\
Workspace harus tetap tersedia setelah aplikasi ditutup dan dijalankan
kembali.

**FR-WS-07 --- Menangani nama duplikat (P1)**\
Sistem harus mengizinkan atau menolak nama workspace duplikat
berdasarkan aturan yang ditetapkan. Untuk MVP, nama duplikat sebaiknya
diizinkan selama setiap workspace memiliki identitas internal unik.

### 5.3 Pengelolaan Resource

**FR-RS-01 --- Jenis resource (P0)**\
Sistem harus mendukung resource dengan jenis: - aplikasi atau
executable; - file; - folder; - tautan web.

**FR-RS-02 --- Menambahkan resource (P0)**\
Pengguna harus dapat menambahkan resource ke workspace melalui pemilih
file/folder, input lokasi, atau input URL sesuai jenis resource.

**FR-RS-03 --- Menyimpan referensi resource (P0)**\
Sistem harus menyimpan lokasi atau URI resource, bukan membuat salinan
file asli.

**FR-RS-04 --- Menampilkan resource (P0)**\
Sistem harus menampilkan nama resource dan informasi yang cukup untuk
membedakan item sejenis.

**FR-RS-05 --- Mengubah detail resource (P0)**\
Pengguna harus dapat mengubah nama tampilan atau target resource sesuai
aturan jenisnya.

**FR-RS-06 --- Menghapus resource dari workspace (P0)**\
Pengguna harus dapat menghapus resource dari workspace. Tindakan ini
hanya menghapus referensinya dari Adara Box dan tidak boleh menghapus
target asli.

**FR-RS-07 --- Menangani target yang tidak tersedia (P0)**\
Jika target resource tidak ditemukan atau tidak dapat diakses, sistem
harus memberi indikator dan pesan yang menjelaskan masalahnya. Sistem
tidak boleh diam-diam menghapus resource tersebut.

**FR-RS-08 --- Menambah banyak resource (P1)**\
Sistem sebaiknya mendukung penambahan resource berulang dengan alur yang
efisien. Dukungan drag-and-drop dapat dipertimbangkan setelah alur dasar
stabil.

**FR-RS-09 --- Mengatur urutan resource (P1)**\
Pengguna sebaiknya dapat mengatur urutan resource secara manual atau
berdasarkan kriteria yang tersedia.

### 5.4 Tampilan Grid dan List

**FR-VW-01 --- Tampilan grid (P0)**\
Sistem harus menyediakan tampilan grid untuk menampilkan resource.

**FR-VW-02 --- Tampilan list (P0)**\
Sistem harus menyediakan tampilan list untuk menampilkan resource.

**FR-VW-03 --- Beralih tampilan (P0)**\
Pengguna harus dapat beralih antara grid dan list tanpa kehilangan atau
mengubah resource.

**FR-VW-04 --- Menyimpan preferensi tampilan (P1)**\
Sistem sebaiknya menyimpan preferensi tampilan. Jika preferensi disimpan
per workspace, pengguna dapat menggunakan tampilan berbeda pada
workspace berbeda.

**FR-VW-05 --- Menjaga fungsi konsisten (P0)**\
Operasi pengelolaan dan pemilihan resource harus tetap tersedia pada
kedua tampilan, kecuali terdapat keterbatasan UI yang dijelaskan secara
eksplisit.

### 5.5 Open the File

**FR-OF-01 --- Mengaktifkan mode Open the File (P0)**\
Sistem harus menyediakan kontrol bernama **Open the File** untuk
mengaktifkan atau menonaktifkan mode pemilihan resource.

**FR-OF-02 --- Menampilkan checkbox (P0)**\
Saat mode aktif, sistem harus menampilkan checkbox atau kontrol
pemilihan yang jelas pada setiap resource yang dapat dipilih.

**FR-OF-03 --- Memilih beberapa resource (P0)**\
Pengguna harus dapat memilih lebih dari satu resource dalam workspace
yang sedang dibuka.

**FR-OF-04 --- Membuka resource terpilih (P0)**\
Sistem harus menyediakan tindakan Open untuk membuka semua resource
terpilih melalui mekanisme Windows yang sesuai dengan jenis resource.

**FR-OF-05 --- Memisahkan mode kelola dan mode buka (P0)**\
Mode Open the File tidak boleh membuat pengguna secara tidak sengaja
menghapus, mengganti nama, atau mengedit resource ketika sedang memilih
item untuk dibuka.

**FR-OF-06 --- Menangani kegagalan per item (P0)**\
Jika satu resource gagal dibuka, sistem harus tetap mencoba membuka
resource lain yang terpilih dan melaporkan item yang gagal.

**FR-OF-07 --- Menampilkan jumlah pilihan (P1)**\
Sistem sebaiknya menampilkan jumlah resource yang dipilih.

**FR-OF-08 --- Mengatur status pilihan (P0)**\
Pilihan resource bersifat sementara. Saat pengguna keluar dari workspace
atau menonaktifkan mode Open the File, pilihan harus dibersihkan,
kecuali keputusan desain selanjutnya menetapkan perilaku lain.

**FR-OF-09 --- Tidak membuka item tanpa pilihan (P0)**\
Jika pengguna menekan Open tanpa memilih resource, sistem harus
menonaktifkan tindakan tersebut atau menampilkan petunjuk yang sesuai.

**FR-OF-10 --- Menangani pembukaan banyak item (P0)**\
Sistem harus menghindari pemanggilan pembukaan resource yang tidak valid
dan memberi umpan balik ketika item gagal dibuka. Pengguna bertanggung
jawab memastikan resource yang dibuka sesuai dengan kebutuhan sistemnya.

### 5.6 Media

**FR-MD-01 --- Menyediakan modul Media (P1)**\
Sistem dapat menampilkan modul Media pada top bar sejak tahap awal,
walaupun implementasinya belum tersedia.

**FR-MD-02 --- Menampilkan status pengembangan (P0 jika placeholder
ditampilkan)**\
Jika Media belum berfungsi, sistem harus menampilkan status seperti
**"Masih dalam pengembangan"** dan tidak menampilkan kontrol yang
seolah-olah berfungsi.

**FR-MD-03 --- Mengeksplorasi pembukaan layanan web (P2)**\
Implementasi lanjutan dapat mengevaluasi pembukaan layanan media di
browser eksternal atau melalui WebView di dalam Adara Box.

**FR-MD-04 --- Tidak mewajibkan integrasi API (P0)**\
MVP tidak boleh bergantung pada API Spotify atau layanan media pihak
ketiga.

**FR-MD-05 --- Menampilkan kegagalan layanan (P2)**\
Jika layanan web atau WebView diimplementasikan dan gagal dimuat, sistem
harus menampilkan pesan kegagalan yang sesuai.

### 5.7 Stats

**FR-ST-01 --- Menyediakan modul Stats (P1)**\
Sistem dapat menampilkan modul Stats pada top bar sejak tahap awal.

**FR-ST-02 --- Menampilkan status pengembangan (P0 jika placeholder
ditampilkan)**\
Jika Stats belum diimplementasikan, panel harus menampilkan status
pengembangan yang jelas.

**FR-ST-03 --- Menampilkan metrik real-time (P2)**\
Jika modul Stats diimplementasikan, sistem harus menampilkan metrik
sistem terkini, bukan histori.

**FR-ST-04 --- Metrik sistem (P2)**\
Metrik yang dipertimbangkan adalah CPU, RAM, storage, dan GPU jika data
tersedia secara andal.

**FR-ST-05 --- Memperbarui data (P2)**\
Data harus diperbarui secara berkala tanpa membuat UI tidak responsif.
Interval dan metode pengambilan metrik ditentukan pada tahap
implementasi.

**FR-ST-06 --- Menangani metrik yang tidak tersedia (P2)**\
Jika metrik tertentu tidak dapat diambil, sistem harus menunjukkan bahwa
data tidak tersedia, bukan menampilkan angka yang dikarang.

### 5.8 Status Implementasi dan Placeholder

**FR-PL-01 --- Menandai fitur belum tersedia (P0)**\
Fitur yang ditampilkan sebelum implementasinya selesai harus memiliki
status yang jelas, misalnya: - "Belum tersedia"; - "Masih dalam
pengembangan"; - "Tidak didukung pada perangkat ini".

**FR-PL-02 --- Tidak menampilkan interaksi palsu (P0)**\
Kontrol yang belum berfungsi harus dinonaktifkan atau diberi penjelasan
yang terlihat. Klik pada placeholder tidak boleh menimbulkan kesan bahwa
suatu tindakan berhasil.

**FR-PL-03 --- Memisahkan status pengembangan dan kesalahan (P0)**\
Status fitur yang belum selesai harus dibedakan dari kesalahan runtime
atau kegagalan jaringan.

**FR-PL-04 --- Menampilkan ruang lingkup yang belum selesai (P1)**\
Untuk fitur yang masih dikembangkan, sistem dapat menampilkan keterangan
singkat tentang kemampuan yang belum tersedia.

### 5.9 Pengaturan dan Penyimpanan

**FR-CF-01 --- Menyimpan konfigurasi lokal (P0)**\
Sistem harus menyimpan workspace, resource, preferensi tampilan, dan
pengaturan aplikasi yang sudah diimplementasikan secara lokal.

**FR-CF-02 --- Memulihkan data (P0)**\
Sistem harus memuat kembali data yang tersimpan ketika aplikasi
dijalankan.

**FR-CF-03 --- Menangani data rusak (P1)**\
Jika penyimpanan tidak dapat dibaca, sistem harus menampilkan kesalahan
yang jelas dan tidak menimpa data secara diam-diam.

**FR-CF-04 --- Pengaturan dasar (P1)**\
Pengaturan dapat mencakup visibilitas top bar, shortcut, perilaku
startup, dan preferensi tampilan.

**FR-CF-05 --- Tidak memerlukan akun (P0)**\
MVP tidak memerlukan login atau akun cloud.

------------------------------------------------------------------------

## 6. Kebutuhan Nonfungsional

### 6.1 Performa

**NFR-PF-01** --- Top bar dan panel utama harus terasa responsif untuk
interaksi umum.\
**NFR-PF-02** --- Membuka workspace tidak boleh memblokir UI dalam waktu
lama.\
**NFR-PF-03** --- Pembukaan beberapa resource harus dijalankan dengan
penanganan kegagalan yang baik dan tidak membuat aplikasi utama crash
akibat satu target bermasalah.\
**NFR-PF-04** --- Pembaruan Stats, jika tersedia, tidak boleh
menghabiskan sumber daya secara berlebihan.

### 6.2 Keandalan dan Pemulihan

**NFR-RL-01** --- Data workspace harus tetap tersimpan setelah aplikasi
ditutup secara normal.\
**NFR-RL-02** --- Kegagalan membuka resource tidak boleh merusak data
workspace.\
**NFR-RL-03** --- Aplikasi harus memberikan pesan yang dapat dipahami
ketika terjadi kesalahan yang diketahui.\
**NFR-RL-04** --- Penulisan data lokal sebaiknya menggunakan transaksi
atau mekanisme setara untuk mengurangi risiko data parsial.

### 6.3 Usability dan Aksesibilitas

**NFR-UX-01** --- Top bar harus ringkas dan tidak mengambil ruang layar
secara berlebihan.\
**NFR-UX-02** --- Status modul aktif, mode Open the File, dan resource
terpilih harus terlihat jelas.\
**NFR-UX-03** --- Fungsi inti harus dapat digunakan dengan mouse;
navigasi keyboard dapat ditambahkan untuk meningkatkan aksesibilitas.\
**NFR-UX-04** --- Grid dan list harus memiliki hierarki visual dan
keterbacaan yang memadai.\
**NFR-UX-05** --- Status fitur yang belum tersedia harus mudah dibedakan
dari fitur aktif.

### 6.4 Kompatibilitas

**NFR-CP-01** --- Aplikasi harus menggunakan mekanisme pembukaan
resource yang kompatibel dengan Windows.\
**NFR-CP-02** --- Sistem harus menangani path yang berisi spasi dan
karakter non-ASCII.\
**NFR-CP-03** --- Aplikasi harus mempertimbangkan variasi resolusi dan
scaling layar Windows.\
**NFR-CP-04** --- Dukungan versi minimum Windows harus ditetapkan
sebelum rilis MVP.

### 6.5 Privasi dan Keamanan

**NFR-SC-01** --- MVP harus menyimpan konfigurasi workspace secara
lokal.\
**NFR-SC-02** --- Aplikasi tidak boleh menghapus atau memodifikasi file
asli hanya karena resource dihapus dari workspace.\
**NFR-SC-03** --- Aplikasi tidak boleh mengirim daftar resource atau
data sistem ke layanan eksternal tanpa persetujuan dan kebutuhan yang
jelas.\
**NFR-SC-04** --- URL dan path harus diperlakukan sebagai data, bukan
sebagai instruksi untuk mengeksekusi perintah shell yang tidak
tervalidasi.\
**NFR-SC-05** --- Jika WebView ditambahkan, pengelolaan cookie, sesi
login, penyimpanan kredensial, dan batas keamanan harus ditinjau sebelum
rilis.

### 6.6 Maintainability

**NFR-MN-01** --- Kode harus memisahkan UI, logika bisnis, penyimpanan
data, dan integrasi Windows.\
**NFR-MN-02** --- Modul Workspace, Media, dan Stats harus dapat
dikembangkan secara relatif independen.\
**NFR-MN-03** --- Fitur yang belum tersedia harus dapat ditampilkan
melalui status modul yang konsisten tanpa membuat implementasi
placeholder bercampur dengan logika fitur aktif.

------------------------------------------------------------------------

## 7. Alur Pengguna Utama

### 7.1 Membuat Workspace

1.  Pengguna membuka top bar.
2.  Pengguna memilih Add Workspace.
3.  Sistem menampilkan form pembuatan workspace.
4.  Pengguna memasukkan nama.
5.  Pengguna mengonfirmasi.
6.  Sistem menyimpan workspace dan menampilkannya pada daftar.

### 7.2 Menambahkan Resource

1.  Pengguna membuka Workspace.
2.  Pengguna memilih workspace yang diinginkan.
3.  Pengguna memilih Add Resource.
4.  Pengguna menentukan jenis resource.
5.  Pengguna memilih target atau memasukkan URL.
6.  Sistem menyimpan referensi dan menampilkan resource di workspace.
7.  Sistem tidak membuat salinan target asli.

### 7.3 Membuka Beberapa Resource

1.  Pengguna membuka workspace.
2.  Pengguna mengaktifkan Open the File.
3.  Sistem menampilkan checkbox pada resource.
4.  Pengguna memilih beberapa resource.
5.  Pengguna menekan Open.
6.  Sistem membuka setiap resource menggunakan handler yang sesuai.
7.  Jika ada kegagalan, sistem menampilkan ringkasan item yang berhasil
    dibuka dan item yang gagal.
8.  Pengguna keluar dari mode Open the File; pilihan sementara
    dibersihkan.

### 7.4 Menghapus Resource

1.  Pengguna masuk ke mode pengelolaan resource.
2.  Pengguna memilih tindakan hapus pada suatu resource.
3.  Sistem meminta konfirmasi bila diperlukan.
4.  Sistem menghapus referensi dari workspace.
5.  Target asli tetap berada di lokasi asal.

### 7.5 Membuka Modul yang Belum Tersedia

1.  Pengguna memilih Media atau Stats.
2.  Sistem membuka panel modul.
3.  Jika modul belum diimplementasikan, sistem menampilkan status
    pengembangan.
4.  Sistem tidak menjalankan tindakan palsu atau mengklaim bahwa fitur
    aktif.

### 7.6 Menyembunyikan dan Memunculkan Top Bar

1.  Pengguna mengaktifkan Hide melalui pengaturan atau kontrol yang
    disediakan.
2.  Top bar menghilang.
3.  Pengguna mengarahkan kursor ke area pemicu di bagian atas layar.
4.  Top bar muncul kembali untuk sementara.
5.  Jika Hide dinonaktifkan, top bar kembali selalu terlihat.

------------------------------------------------------------------------

## 8. Model Data Konseptual

Implementasi awal dapat menggunakan SQLite dengan struktur konseptual
berikut.

### 8.1 Workspace

  Field        Tipe konseptual     Keterangan
  ------------ ------------------- ----------------------------------
  id           UUID atau integer   Identitas unik workspace
  name         string              Nama workspace
  created_at   datetime            Waktu pembuatan
  updated_at   datetime            Waktu perubahan terakhir
  sort_order   integer             Urutan workspace, jika digunakan

### 8.2 Resource

  Field          Tipe konseptual     Keterangan
  -------------- ------------------- -----------------------------------------
  id             UUID atau integer   Identitas unik resource
  workspace_id   foreign key         Workspace pemilik referensi
  name           string              Nama tampilan
  type           enum/string         application, file, folder, link
  target         string              Path, URI, atau target yang akan dibuka
  sort_order     integer             Urutan resource
  created_at     datetime            Waktu penambahan
  updated_at     datetime            Waktu perubahan

### 8.3 Settings

  Field   Tipe konseptual   Keterangan
  ------- ----------------- ------------------
  key     string            Nama pengaturan
  value   string/JSON       Nilai pengaturan

Status pemilihan resource untuk Open the File sebaiknya disimpan sebagai
state UI sementara dan tidak wajib masuk ke database.

Skema final, aturan penghapusan, migrasi, serta format penyimpanan
ditetapkan pada tahap desain teknis.

------------------------------------------------------------------------

## 9. Rekomendasi Implementasi

Bagian ini merupakan rekomendasi teknis awal, bukan keputusan teknologi
yang tidak dapat diubah.

### 9.1 Pendekatan Aplikasi

Untuk aplikasi yang berfokus pada Windows, pilihan awal yang layak
dievaluasi adalah: - **C# dan WPF** untuk UI desktop Windows; - pola
**MVVM** untuk memisahkan UI dan logika aplikasi; - **SQLite** untuk
penyimpanan lokal; - API Windows yang relevan untuk perilaku top bar,
hotkey, dan pembukaan resource.

Alternatif seperti Tauri dapat dievaluasi jika kebutuhan UI web-based
lebih penting daripada penggunaan framework desktop Windows-native.

### 9.2 Pembukaan Resource

-   File dan folder dibuka menggunakan mekanisme Windows yang sesuai.
-   URL dibuka menggunakan browser default pada MVP.
-   Aplikasi atau executable dibuka melalui target yang telah
    divalidasi.
-   Kesalahan dilaporkan per resource agar kegagalan satu item tidak
    menghentikan seluruh proses.

### 9.3 WebView dan Media

Jika WebView dieksplorasi, implementasi harus mengevaluasi: - dukungan
autentikasi layanan; - penyimpanan sesi dan cookie; - batasan penggunaan
layanan pihak ketiga; - penggunaan RAM dan CPU; - perilaku playback dan
media key; - keamanan konten web yang dimuat.

WebView tidak boleh diasumsikan dapat menggunakan sesi login Chrome
secara otomatis. Integrasi media bukan syarat untuk menyelesaikan MVP
Workspace.

### 9.4 Pengukuran Stats

Metrik sistem harus berasal dari sumber yang sesuai dan tidak boleh
ditampilkan sebagai data aktual jika hanya perkiraan. GPU usage dapat
bergantung pada hardware, driver, dan API yang tersedia.

------------------------------------------------------------------------

## 10. Strategi Pengujian

### 10.1 Pengujian Fungsional

-   Membuat, mengganti nama, dan menghapus workspace.
-   Menambahkan seluruh jenis resource yang didukung.
-   Mengubah nama tampilan dan menghapus referensi resource.
-   Memastikan target asli tidak terhapus ketika referensi dihapus.
-   Beralih antara grid dan list.
-   Mengaktifkan dan menonaktifkan Open the File.
-   Memilih dan membuka beberapa resource.
-   Menangani path yang tidak ditemukan dan URL yang tidak valid.
-   Memastikan data tetap ada setelah aplikasi dimulai ulang.
-   Menguji status placeholder pada Media dan Stats.

### 10.2 Pengujian Antarmuka

-   Top bar selalu terlihat ketika Hide tidak aktif.
-   Top bar tersembunyi ketika Hide aktif.
-   Top bar dapat dimunculkan melalui area pemicu.
-   Panel modul melebar secara horizontal sesuai desain final.
-   Panel tidak menutupi interaksi penting secara tidak terduga.
-   Checkbox hanya muncul saat mode Open the File aktif.
-   Grid dan list tetap mudah digunakan pada ukuran layar berbeda.

### 10.3 Pengujian Ketahanan

-   Resource target dihapus atau dipindahkan setelah ditambahkan.
-   Resource tidak memiliki izin akses.
-   Satu dari beberapa resource gagal dibuka.
-   Database tidak dapat dibaca atau ditulis.
-   Nama workspace atau resource mengandung karakter khusus.
-   Aplikasi ditutup saat operasi sedang berlangsung.

### 10.4 Pengujian Performa

-   Waktu membuka workspace dengan jumlah resource yang representatif.
-   Responsivitas top bar ketika aplikasi lain sedang berjalan.
-   Penggunaan CPU dan RAM saat idle.
-   Pengaruh polling Stats jika modul tersebut sudah diterapkan.

------------------------------------------------------------------------

## 11. Kriteria Penerimaan MVP

MVP dianggap memenuhi kebutuhan inti apabila:

1.  Pengguna dapat membuat dan membuka beberapa workspace.
2.  Setiap workspace dapat menyimpan referensi aplikasi, file, folder,
    dan tautan.
3.  Resource dapat ditampilkan dalam grid maupun list.
4.  Pengguna dapat mengganti tampilan tanpa kehilangan data.
5.  Open the File memungkinkan pemilihan dan pembukaan beberapa
    resource.
6.  Kegagalan membuka satu resource tidak menghentikan percobaan
    pembukaan resource lain.
7.  Menghapus resource dari Adara Box tidak menghapus file atau folder
    asli.
8.  Data workspace tetap tersedia setelah aplikasi dimulai ulang.
9.  Top bar dapat selalu terlihat atau disembunyikan sesuai pengaturan.
10. Modul yang belum diimplementasikan menampilkan status yang jelas dan
    tidak berpura-pura berfungsi.
11. Aplikasi tetap dapat digunakan untuk fungsi Workspace tanpa
    integrasi Spotify, browser mini, atau Stats.

------------------------------------------------------------------------

## 12. Status Implementasi yang Ditampilkan di UI

Untuk menjaga transparansi selama pengembangan, status fitur yang
ditampilkan kepada pengguna harus konsisten.

  -----------------------------------------------------------------------
  Status                  Makna                   Perilaku UI
  ----------------------- ----------------------- -----------------------
  Available               Fitur sudah tersedia    Kontrol aktif

  In Development          Fitur sedang            Tampilkan label "Masih
                          dikembangkan            dalam pengembangan";
                                                  tindakan belum tersedia
                                                  dinonaktifkan

  Not Available           Fitur belum tersedia    Tampilkan keterangan
                                                  singkat dan jangan
                                                  mengklaim fungsi aktif

  Unsupported             Fitur tidak didukung    Tampilkan alasan jika
                          pada lingkungan ini     diketahui

  Error                   Fitur seharusnya        Tampilkan pesan
                          tersedia tetapi         kesalahan yang relevan
                          mengalami kegagalan     
  -----------------------------------------------------------------------

Status pengembangan tidak boleh disamakan dengan error. Contohnya, panel
Media yang belum dibuat harus menunjukkan "Masih dalam pengembangan",
bukan "Gagal memuat".

------------------------------------------------------------------------

## 13. Risiko dan Mitigasi

  -----------------------------------------------------------------------
  Risiko                  Dampak                  Mitigasi
  ----------------------- ----------------------- -----------------------
  Top bar mengganggu      Mengurangi kenyamanan   Uji posisi, area
  aplikasi lain                                   pemicu, fokus, dan
                                                  pengaturan Hide

  Resource dipindah atau  Resource tidak dapat    Tampilkan indikator
  dihapus dari lokasi     dibuka                  target tidak tersedia
  asli                                            dan sediakan tindakan
                                                  edit/remove

  Pembukaan banyak        Performa menurun        Laporkan hasil per item
  resource membebani                              dan uji jumlah resource
  sistem                                          yang realistis

  WebView tidak           Media tidak berfungsi   Jadikan eksplorasi
  kompatibel dengan login sesuai harapan          pasca-MVP dan uji
  atau playback                                   secara terpisah

  Metrik GPU tidak        Data Stats tidak        Tampilkan status tidak
  tersedia                lengkap                 tersedia; jangan
                                                  mengarang angka

  Fitur placeholder       Ekspektasi pengguna     Gunakan status
  disalahartikan sebagai  keliru                  implementasi yang
  fitur aktif                                     konsisten

  Perubahan skema merusak Workspace hilang atau   Gunakan migrasi
  data lokal              rusak                   database dan mekanisme
                                                  pemulihan
  -----------------------------------------------------------------------

------------------------------------------------------------------------

## 14. Roadmap Implementasi

### Tahap 1 --- Application Shell

-   Struktur aplikasi desktop.
-   Top bar ringkas.
-   Navigasi modul.
-   Perilaku visibilitas dasar.
-   Panel modul dengan ukuran dan perilaku awal.

### Tahap 2 --- Workspace Management

-   Membuat, melihat, mengganti nama, dan menghapus workspace.
-   Menyimpan data lokal.
-   Membuka workspace dan melihat isinya.

### Tahap 3 --- Resource Management

-   Menambah dan menghapus referensi resource.
-   Mendukung aplikasi, file, folder, dan tautan.
-   Mengubah nama tampilan.
-   Grid dan list.

### Tahap 4 --- Open the File

-   Mode pemilihan.
-   Checkbox resource.
-   Pemilihan beberapa item.
-   Pembukaan resource melalui Windows.
-   Pelaporan kegagalan per item.

### Tahap 5 --- MVP Hardening

-   Penanganan error.
-   Validasi data.
-   Pengujian persistensi.
-   Perbaikan responsivitas dan penggunaan keyboard.
-   Status implementasi untuk modul yang belum tersedia.

### Tahap 6 --- Eksplorasi Pasca-MVP

-   Stats real-time.
-   Media.
-   Evaluasi WebView.
-   Evaluasi kontrol media sistem.
-   Penyempurnaan shortcut dan pengaturan.

Urutan ini menempatkan Workspace sebagai prioritas utama dan mencegah
eksplorasi integrasi eksternal menghambat penyelesaian MVP.

------------------------------------------------------------------------

## 15. Keputusan Desain yang Sudah Ditetapkan

  -----------------------------------------------------------------------
  Area                                Keputusan saat ini
  ----------------------------------- -----------------------------------
  Nama produk                         Adara Box

  Platform awal                       Windows desktop

  Antarmuka utama                     Top bar kecil di bagian atas layar

  Visibilitas                         Selalu terlihat secara default;
                                      dapat disembunyikan dan dimunculkan
                                      lewat hover pada area pemicu

  Panel modul                         Melebar secara horizontal dari top
                                      bar

  Modul utama MVP                     Workspace

  Resource                            Aplikasi, file, folder, dan tautan

  Tampilan resource                   Grid dan list

  Pembukaan resource                  Mode Open the File dengan pemilihan
                                      beberapa item

  Penyimpanan                         Lokal

  Stats                               Real-time tanpa histori; pasca-MVP
                                      jika belum siap

  Media                               Eksplorasi pasca-MVP; integrasi
                                      layanan bukan syarat MVP

  Spotify                             Tidak memerlukan integrasi API
                                      untuk MVP; WebView dapat dievaluasi
                                      kemudian

  Status fitur                        Fitur yang belum tersedia harus
                                      ditandai dengan jelas
  -----------------------------------------------------------------------

------------------------------------------------------------------------

## 16. Hal yang Masih Perlu Diputuskan

Hal berikut tidak menghalangi perumusan kebutuhan inti, tetapi perlu
dipastikan sebelum implementasi terkait: 1. Detail ukuran, posisi, dan
animasi panel horizontal. 2. Perilaku top bar saat aplikasi berjalan
pada banyak monitor. 3. Shortcut keyboard dan perilaku startup. 4.
Apakah preferensi grid/list disimpan per workspace atau secara global.
5. Dukungan drag-and-drop untuk menambahkan resource. 6. Apakah resource
dapat dikelompokkan ke dalam kategori atau folder virtual di dalam
workspace. 7. Versi minimum Windows yang akan didukung. 8. Apakah Stats
dan Media ditampilkan sejak MVP sebagai panel placeholder atau baru
ditambahkan setelah MVP. 9. Pilihan final framework desktop.

------------------------------------------------------------------------

## 17. Ringkasan

Adara Box adalah aplikasi desktop Windows yang menyediakan top bar
ringkas sebagai akses utama ke berbagai modul. Fokus MVP adalah
Workspace: pengguna dapat mengelompokkan referensi aplikasi, file,
folder, dan tautan ke dalam workspace, mengelolanya dalam grid atau
list, lalu memilih dan membuka beberapa resource sekaligus melalui Open
the File.

Top bar dapat selalu terlihat atau disembunyikan dengan mekanisme
pemunculan melalui hover. Panel modul direncanakan melebar secara
horizontal. Media dan Stats tetap menjadi arah pengembangan, tetapi
tidak boleh menghambat penyelesaian Workspace. Setiap fitur yang belum
tersedia harus ditandai secara transparan sebagai bagian dari proses
pengembangan.

Dokumen ini menjadi baseline kebutuhan versi 0.2 dan dapat diperbarui
setelah prototipe UI dan hasil implementasi awal dievaluasi.
