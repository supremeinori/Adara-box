# PROGRESS — Adara Box

> File ini **dibaca dan diperbarui oleh agent** di setiap sesi. Manusia mengubah kolom "Persetujuan".
> Centang hanya setelah benar-benar terverifikasi (test hijau atau [HUMAN CHECK] selesai).

## Status Modul

| Modul | Nama | Status | Persetujuan manusia |
|---|---|---|---|
| M00 | Setup & Spikes | ☐ belum | ☐ |
| M01 | App Shell | ☐ belum | ☐ |
| M02 | Workspace Management | ☐ belum | ☐ |
| M03 | Resource Management | ☐ belum | ☐ |
| M04 | Open the File | ☐ belum | ☐ |
| — | **MVP fungsional** (setelah M04) | ☐ | ☐ |
| M05 | Hardening | ☐ belum | ☐ |
| M06 | Packaging & Rilis (v1.0.0) | ☐ belum | ☐ |
| M07 | Stats (pasca-MVP) | ☐ belum | ☐ |
| M08 | Media (pasca-MVP) | ☐ belum | ☐ |

Status: ☐ belum · ◐ sedang dikerjakan · ✔ selesai · ⛔ terblokir

## Menunggu Verifikasi Manusia ([HUMAN CHECK])
_(agent menambah baris di sini: modul, deskripsi, langkah uji)_

## OPEN QUESTIONS
_(agent menambah; manusia menjawab di kolom kanan)_

| # | Pertanyaan | Modul | Jawaban manusia |
|---|---|---|---|

## Acceptance Criteria MVP (SRS §11) — dibuktikan di M04/M05

- [ ] 1. Pengguna dapat membuat dan membuka beberapa workspace.
- [ ] 2. Workspace dapat menyimpan referensi aplikasi, file, folder, dan tautan.
- [ ] 3. Resource ditampilkan dalam grid maupun list.
- [ ] 4. Ganti tampilan tanpa kehilangan data.
- [ ] 5. Open the File memungkinkan pemilihan dan pembukaan beberapa resource.
- [ ] 6. Kegagalan satu resource tidak menghentikan resource lain.
- [ ] 7. Menghapus resource dari Adara Box tidak menghapus file/folder asli.
- [ ] 8. Data workspace tetap ada setelah aplikasi dimulai ulang.
- [ ] 9. Top bar dapat selalu terlihat atau disembunyikan sesuai pengaturan.
- [ ] 10. Modul yang belum diimplementasikan menampilkan status jelas dan tidak berpura-pura berfungsi.
- [ ] 11. Aplikasi tetap berguna untuk Workspace tanpa Spotify, browser mini, atau Stats.

## Peta Requirement → Modul (acuan traceability)

| Requirement | Modul |
|---|---|
| FR-TB-01…04 (top bar, modul, Hide, hover) | M01 |
| FR-TB-05 (persist preferensi) | M02 |
| FR-TB-06 (shortcut) | M05 |
| FR-WS-01…07 | M02 |
| FR-RS-01…09 | M03 |
| FR-VW-01…05 | M03 |
| FR-OF-01…10 | M04 |
| FR-PL-01…04, FR-MD-02, FR-ST-02 (placeholder) | M01 |
| FR-CF-01/02/05 | M02 |
| FR-CF-03/04 | M05 |
| FR-ST-03…06 | M07 |
| FR-MD-01/03/04/05 | M08 |
| NFR-MN-01…03 (arsitektur) | M00-M01 (test arsitektur) |
| NFR-RL, NFR-CP, NFR-UX, NFR-PF, NFR-SC | M02-M05 (diaudit di M05) |
| NFR-CP-04 (Windows minimum) | M00 (keputusan), M06 (installer) |

## Log Laporan Modul
_(agent menempelkan Laporan Modul di bawah ini, terbaru di atas; format ada di Master Guide §6)_
