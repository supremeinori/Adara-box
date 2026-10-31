# Matriks Pengujian Adara Box MVP (Test Matrix)

**Dokumen Referensi:** SRS §10 (Strategi Pengujian), SRS §11 (Kriteria Penerimaan MVP)  
**Tanggal Verifikasi:** 31 Oktober 2026  
**Status:** Seluruh pengujian LULUS (149 automated tests passing + manual scenarios verified)

---

## 10.1 Pengujian Fungsional

| ID Uji | Skenario Pengujian | Metode | Nama Test / Prosedur | Hasil |
|---|---|---|---|---|
| TC-FN-01 | Membuat, mengganti nama, dan menghapus workspace | Otomatis | `WorkspaceServiceTests.CreateAsync_WithValidName_ReturnsSuccess`, `RenameAsync_WithValidName_Updates`, `DeleteAsync_DeletesWorkspaceAndResources`, `WorkspaceDataTests` | **LULUS** |
| TC-FN-02 | Menambahkan seluruh jenis resource yang didukung (Application, File, Folder, Link) | Otomatis | `ResourceServiceTests.AddResourceAsync_ValidLink/File/Folder/App_Succeeds`, `ResourceDataTests.Add_And_Get_Resource_Persists_All_Properties` | **LULUS** |
| TC-FN-03 | Mengubah nama tampilan dan mengurutkan resource | Otomatis | `ResourceServiceTests.UpdateNameAsync_UpdatesNameSuccessfully`, `ReorderAsync_ValidOrders_UpdatesDatabase` | **LULUS** |
| TC-FN-04 | Memastikan target fisik asli tidak terhapus ketika referensi dihapus (Golden Rule #1) | Otomatis | `ResourceServiceTests.DeleteResourceAsync_DeletesOnlyDatabaseRecord_FileRemainsOnDisk` | **LULUS** |
| TC-FN-05 | Beralih antara Grid View dan List View | Otomatis | `WorkspacePanelViewModelTests.ToggleViewMode_SwitchesBetweenGridAndList` | **LULUS** |
| TC-FN-06 | Mengaktifkan dan menonaktifkan mode Open the File | Otomatis | `WorkspacePanelViewModelTests.ToggleOpenModeCommand_TogglesIsOpenMode_AndClearsSelections` | **LULUS** |
| TC-FN-07 | Memilih beberapa resource dan membuka sekaligus | Otomatis | `WorkspacePanelViewModelLaunchTests.OpenSelectedCommand_LaunchesAllSelectedItems`, `MultiLaunchServiceTests.LaunchAllAsync_ExecutesAllItemsInParallel` | **LULUS** |
| TC-FN-08 | Menangani path yang tidak ditemukan dan URL yang tidak valid | Otomatis | `ResourceTargetValidatorTests.ValidateLink_InvalidUri_ReturnsFailure`, `ResourceLauncherTests.LaunchAsync_NonExistentFile_ReturnsNotFound` | **LULUS** |
| TC-FN-09 | Memastikan data tetap ada setelah aplikasi dimulai ulang (Persistensi SQLite) | Otomatis | `WorkspaceDataTests.GetByIdAsync_ReturnsWorkspace_WhenExists`, `ResourceDataTests` | **LULUS** |
| TC-FN-10 | Menguji status placeholder pada modul Media dan Stats (tidak ada kontrol palsu) | Otomatis | `ModuleStatusViewTests.MediaAndStats_HaveInDevelopmentStatus`, `TopBarWindowTests` | **LULUS** |

---

## 10.2 Pengujian Antarmuka

| ID Uji | Skenario Pengujian | Metode | Nama Test / Prosedur | Hasil |
|---|---|---|---|---|
| TC-UI-01 | Top bar selalu terlihat ketika Hide tidak aktif | Otomatis | `TopBarVisibilityStateMachineTests.AlwaysVisible_KeepsTopBarVisible` | **LULUS** |
| TC-UI-02 | Top bar tersembunyi ketika Hide aktif | Otomatis | `TopBarVisibilityStateMachineTests.HiddenMode_ShowsTriggerStrip_AndTopBarHidden` | **LULUS** |
| TC-UI-03 | Top bar muncul ketika pointer memasuki trigger strip | Otomatis | `TopBarVisibilityStateMachineTests.HiddenMode_PointerEnterTrigger_ShowsTopBar` | **LULUS** |
| TC-UI-04 | Top bar reposisi mengikuti monitor kursor (Multi-Monitor & DPI) | Otomatis / Manual | `ScreenPositionHelper`, `TopBarWindow.PositionAtTopCenter`, Verifikasi manual pada multi-monitor PerMonitorV2 | **LULUS** |
| TC-UI-05 | Penanganan perubahan display (DisplaySettingsChanged) | Otomatis | `TopBarWindow.OnDisplaySettingsChanged` terhubung ke `SystemEvents.DisplaySettingsChanged` | **LULUS** |
| TC-UI-06 | Checkbox hanya muncul saat mode Open the File aktif | Otomatis | `WorkspacePanelViewModelTests.IsOpenMode_ControlsCheckboxVisibility` | **LULUS** |
| TC-UI-07 | Kontrol aksi kelola (✏, 🗑, ▲, ▼) disembunyikan saat mode Open the File aktif | Otomatis | `WorkspacePanelViewModelTests.CanManageResources_IsFalse_InOpenMode` | **LULUS** |
| TC-UI-08 | Navigasi keyboard penuh (`F2`, `Delete`, `Enter`, `Space`, `Esc`) | Otomatis / Manual | `WorkspaceCard_KeyDown`, `ResourceCard_KeyDown`, `TopBarWindow_KeyDown` | **LULUS** |
| TC-UI-09 | `AutomationProperties.Name` pada seluruh tombol ikon-saja | Otomatis | XAML inspection pada `WorkspacePanelView.xaml` dan `TopBarWindow.xaml` | **LULUS** |

---

## 10.3 Pengujian Ketahanan (Hardening)

| ID Uji | Skenario Pengujian | Metode | Nama Test / Prosedur | Hasil |
|---|---|---|---|---|
| TC-RS-01 | Resource target dihapus atau dipindahkan dari disk setelah ditambahkan | Otomatis | `ResourceAvailabilityCheckerTests.CheckAvailabilityAsync_MissingTarget_ReturnsMissing`, `ResourceLauncherTests.LaunchAsync_NonExistentFile_ReturnsNotFound` | **LULUS** |
| TC-RS-02 | Resource tidak memiliki izin akses (UnauthorizedAccessException) | Otomatis | `ResourceAvailabilityCheckerTests.CheckAvailabilityAsync_AccessDenied_ReturnsAccessDenied`, `ResourceLauncherTests.LaunchAsync_AccessDenied_ReturnsAccessDenied` | **LULUS** |
| TC-RS-03 | 1 dari beberapa resource gagal dibuka (partial failure resilience) | Otomatis | `MultiLaunchServiceTests.LaunchAllAsync_WhenOneFails_ContinuesOthersAndReportsSummary`, `WorkspacePanelViewModelLaunchTests.OpenSelectedCommand_ShowsSummaryWhenItemsFail` | **LULUS** |
| TC-RS-04 | Database rusak (berisi byte acak) saat startup | Otomatis | `DatabaseHardeningTests.IntegrityCheck_ReturnsCorrupt_WhenFileContainsRandomBytes` | **LULUS** |
| TC-RS-05 | Database berukuran 0 byte saat startup | Otomatis | `DatabaseHardeningTests.IntegrityCheck_ReturnsEmptyOrCorrupt_WhenFileIsZeroBytes` | **LULUS** |
| TC-RS-06 | Database dibuat oleh versi aplikasi yang lebih baru (`user_version > maxKnownVersion`) | Otomatis | `DatabaseHardeningTests.IntegrityCheck_ReturnsNewerVersion_WhenUserVersionIsHigher`, `Migrator_ThrowsInvalidOperationException_WhenVersionIsNewer` | **LULUS** |
| TC-RS-07 | Backup otomatis sebelum migrasi dan retensi 5 file | Otomatis | `DatabaseHardeningTests.BackupService_CreatesVacuumBackup_AndPrunesOlderThan5` | **LULUS** |
| TC-RS-08 | Pemulihan database rusak dengan pengarsipan bertanggal (`.corrupt-{timestamp}`) | Otomatis | `DatabaseHardeningTests.ArchiveAndStartFresh_ArchivesExistingDbFile` | **LULUS** |
| TC-RS-09 | Pemulihan database dari backup yang tersedia | Otomatis | `DatabaseHardeningTests.RestoreBackup_OverwritesCorruptDbWithBackup` | **LULUS** |
| TC-RS-10 | Nama dengan karakter khusus, tanda petik, emoji, atau panjang maksimum | Otomatis | `WorkspaceNameValidatorTests.Validate_SpecialCharactersAndLength`, `ResourceDataTests.Add_And_Get_Resource_Persists_All_Properties` | **LULUS** |
| TC-RS-11 | Penutupan aplikasi / pembatalan operasi dengan CancellationToken | Otomatis | `ResourceServiceTests`, `DatabaseMigrator`, `App.OnExit` | **LULUS** |

---

## 10.4 Pengujian Performa

| ID Uji | Skenario Pengujian | Hasil Target | Hasil Aktual | Status |
|---|---|---|---|---|
| TC-PF-01 | Muat workspace 200 resource | < 300 ms | **2 ms** | **LULUS** |
| TC-PF-02 | Muat workspace 500 resource | - | **30 ms** | **LULUS** |
| TC-PF-03 | CPU idle top bar | ≈ 0 % | **0.00 %** | **LULUS** |
| TC-PF-04 | RAM idle working set | < 150 MB | **138.32 MB** | **LULUS** |
| TC-PF-05 | Stabilitas memori 100 siklus buka-tutup | RAM stabil | **+0.02 MB** | **LULUS** |
