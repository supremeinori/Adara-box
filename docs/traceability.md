# Matriks Keterlacakan (Traceability Matrix)

**Aplikasi:** Adara Box MVP  
**Tanggal:** 31 Oktober 2026  
**Referensi:** Dokumen SRS §5 (Kebutuhan Fungsional Inti P0/P1)

---

| ID Kebutuhan | Deskripsi Kebutuhan | Prioritas | Modul Implementasi | Komponen Kode Utama | Unit / Integration Test Pembukti |
|---|---|---|---|---|---|
| **FR-TB-01** | Posisi top bar di tengah atas monitor | P0 | M01, M05 | `TopBarWindow.xaml`, `ScreenPositionHelper.cs` | `ScreenPositionHelper`, Verifikasi visual multi-monitor |
| **FR-TB-02** | Tinggi bar fixed 36 DIP & tidak mengganggu | P0 | M01 | `TopBarWindow.xaml` (`Height="36"`) | Inspeksi XAML, `TopBarWindowTests` |
| **FR-TB-03** | Menampilkan modul Workspace, Media, Stats | P0 | M01 | `TopBarViewModel.cs`, `WorkspaceModule.cs`, dll. | `TopBarViewModelTests` |
| **FR-TB-04** | Navigasi antar modul secara horizontal | P0 | M01 | `TopBarViewModel.SelectModuleCommand` | `TopBarViewModelTests.SelectModuleCommand_SwitchesActivePanel` |
| **FR-TB-05** | Single instance enforcement via Mutex | P0 | M01 | `App.xaml.cs` (`_singleInstanceMutex`) | Uji runtime proses ganda (keluar otomatis dengan kode 0) |
| **FR-TB-06** | Shortcut global untuk menampilkan top bar | P1 | M05 | `Win32GlobalHotkeyService.cs`, `HotkeyParser.cs` | `HotkeyParserTests`, `SettingsPanelViewModelTests` |
| **FR-WS-01** | Membuat workspace baru | P0 | M02 | `WorkspaceService.cs`, `SqliteWorkspaceRepository.cs` | `WorkspaceServiceTests.CreateAsync_WithValidName_ReturnsSuccess` |
| **FR-WS-02** | Mengganti nama workspace | P0 | M02 | `WorkspaceService.cs` | `WorkspaceServiceTests.RenameAsync_WithValidName_UpdatesName` |
| **FR-WS-03** | Menghapus workspace dan relasinya | P0 | M02 | `WorkspaceService.cs`, `SqliteWorkspaceRepository.cs` | `WorkspaceServiceTests.DeleteAsync_DeletesWorkspaceAndResources` |
| **FR-WS-04** | Menampilkan daftar seluruh workspace | P0 | M02 | `WorkspacePanelViewModel.cs` | `WorkspacePanelViewModelTests.LoadWorkspacesAsync_PopulatesList` |
| **FR-WS-05** | Membuka detail workspace | P0 | M02 | `WorkspacePanelViewModel.OpenWorkspace` | `WorkspacePanelViewModelTests.OpenWorkspace_NavigatesToDetailView` |
| **FR-WS-06** | Pengurutan workspace | P1 | M02 | `SqliteWorkspaceRepository.cs` (`sort_order`) | `WorkspaceDataTests.AddAsync_And_GetAllAsync_ReturnsOrdered` |
| **FR-WS-07** | Validasi nama workspace (1-60 char, unik) | P0 | M02 | `WorkspaceNameValidator.cs` | `WorkspaceNameValidatorTests.Validate_EnforcesLengthAndCharacters` |
| **FR-WS-08** | Konfirmasi sebelum penghapusan workspace | P0 | M02 | `WorkspacePanelViewModel.cs` | `WorkspacePanelViewModelTests.DeleteCommand_ExecutesProperly` |
| **FR-RC-01** | Menambahkan File dan Folder | P0 | M03 | `ResourceService.cs`, `SqliteResourceRepository.cs` | `ResourceServiceTests.AddResourceAsync_ValidFileAndFolder_Succeeds` |
| **FR-RC-02** | Menambahkan Aplikasi (.exe, shortcut, UWP) | P0 | M03 | `ResourceService.cs`, `ResourceTargetValidator.cs` | `ResourceTargetValidatorTests.ValidateApplication_RecognizesDesktopAndUwp` |
| **FR-RC-03** | Menambahkan Tautan Web (http/https) | P0 | M03 | `ResourceService.cs`, `ResourceTargetValidator.cs` | `ResourceTargetValidatorTests.ValidateLink_ValidatesHttpAndHttps` |
| **FR-RC-04** | Mengedit nama tampilan resource | P0 | M03 | `ResourceService.cs` | `ResourceServiceTests.UpdateNameAsync_UpdatesNameSuccessfully` |
| **FR-RC-05** | Hapus resource tanpa menghapus file fisik | P0 | M03 | `ResourceService.cs` (Golden Rule #1) | `ResourceServiceTests.DeleteResourceAsync_FileRemainsOnDisk` |
| **FR-RC-06** | Mengatur urutan resource (Move Up / Down) | P1 | M03 | `ResourceService.cs` | `ResourceServiceTests.ReorderAsync_ValidOrders_UpdatesDatabase` |
| **FR-RC-07** | Indikator ketersediaan target di disk (⚠️) | P0 | M03 | `ResourceAvailabilityChecker.cs` | `ResourceAvailabilityCheckerTests.CheckAvailabilityAsync_MissingTarget` |
| **FR-RC-08** | Ekstraksi ikon native & favicon default | P0 | M03 | `IconProvider.cs` | `IconProviderTests.GetIconForResource_ReturnsValidBitmapSource` |
| **FR-RC-09** | Validasi skema URL & normalisasi path | P0 | M03 | `ResourceTargetValidator.cs` | `ResourceTargetValidatorTests.Validate_DisallowsInvalidSchemes` |
| **FR-VW-01** | Tampilan Grid View kartu resource | P0 | M04 | `WorkspacePanelView.xaml` (`WrapPanel`) | `WorkspacePanelViewModelTests.ToggleViewMode_SwitchesBetweenGridAndList` |
| **FR-VW-02** | Tampilan List View baris resource | P0 | M04 | `WorkspacePanelView.xaml` (`VirtualizingStackPanel`)| `WorkspacePanelViewModelTests.ToggleViewMode_SwitchesBetweenGridAndList` |
| **FR-VW-03** | Tombol beralih Grid / List | P0 | M04 | `WorkspacePanelViewModel.ToggleViewModeCommand` | `WorkspacePanelViewModelTests.ToggleViewModeCommand_Works` |
| **FR-VW-04** | Persistensi mode tampilan per workspace | P1 | M04 | `WorkspacePanelViewModel.cs`, `ISettingsStore` | `WorkspacePanelViewModelTests.ViewMode_PersistedInSettingsStore` |
| **FR-VW-05** | Konsistensi aksi pada kedua mode tampilan | P0 | M04 | `WorkspacePanelView.xaml` | Inspeksi XAML binding & unit test view model |
| **FR-OF-01** | Kontrol pengaktif mode Open the File | P0 | M04 | `WorkspacePanelViewModel.ToggleOpenModeCommand`| `WorkspacePanelViewModelTests.ToggleOpenModeCommand_TogglesState` |
| **FR-OF-02** | Menampilkan checkbox pemilihan resource | P0 | M04 | `WorkspacePanelView.xaml` | `WorkspacePanelViewModelTests.IsOpenMode_ShowsCheckboxes` |
| **FR-OF-03** | Memilih beberapa resource sekaligus | P0 | M04 | `WorkspacePanelViewModel.SelectAllCommand` | `WorkspacePanelViewModelTests.SelectAllCommand_SelectsAllResources` |
| **FR-OF-04** | Membuka seluruh resource terpilih | P0 | M04 | `MultiLaunchService.cs`, `ResourceLauncher.cs` | `WorkspacePanelViewModelLaunchTests.OpenSelectedCommand_LaunchesAll` |
| **FR-OF-05** | Proteksi mode kelola saat mode Open aktif | P0 | M04 | `WorkspacePanelViewModel.CanManageResources` | `WorkspacePanelViewModelTests.CanManageResources_IsFalse_InOpenMode` |
| **FR-OF-06** | Penanganan kegagalan per item tanpa abort | P0 | M04 | `MultiLaunchService.cs` | `MultiLaunchServiceTests.LaunchAllAsync_ContinuesWhenOneFails` |
| **FR-OF-07** | Menampilkan jumlah item yang dipilih | P1 | M04 | `WorkspacePanelViewModel.SelectedCountText` | `WorkspacePanelViewModelTests.SelectedCountText_UpdatesDynamically` |
| **FR-OF-08** | Pembersihan pilihan saat keluar mode buka | P0 | M04 | `WorkspacePanelViewModel.ExitOpenModeCommand` | `WorkspacePanelViewModelTests.ExitOpenMode_ClearsSelections` |
| **FR-OF-09** | Pencegahan pembukaan tanpa pilihan | P0 | M04 | `WorkspacePanelViewModel.CanOpen` | `WorkspacePanelViewModelTests.CanOpen_IsFalse_WhenNoItemsSelected` |
| **FR-OF-10** | Peringatan > 10 item & dialog summary hasil | P0 | M04 | `WorkspacePanelViewModel.LaunchSummary` | `WorkspacePanelViewModelLaunchTests.OpenSelected_ShowsSummaryOnFailure`|
| **FR-MD-01** | Menampilkan modul Media pada top bar | P1 | M01 | `MediaModule.cs` | `TopBarViewModelTests.Modules_ContainsMediaModule` |
| **FR-MD-02** | Status pengembangan jujur ("Masih dalam pengembangan")| P0 | M01 | `MediaModule.cs`, `ModuleStatusView.xaml` | `ModuleStatusViewTests.MediaModule_ShowsInDevelopmentStatus` |
| **FR-MD-04** | Zero API dependency pada layanan pihak ketiga | P0 | M01, M05 | `AdaraBox.Core`, `AdaraBox.App` | Security review audit (0 network references) |
| **FR-ST-01** | Menampilkan modul Stats pada top bar | P1 | M01 | `StatsModule.cs` | `TopBarViewModelTests.Modules_ContainsStatsModule` |
| **FR-ST-02** | Status pengembangan jujur ("Masih dalam pengembangan")| P0 | M01 | `StatsModule.cs`, `ModuleStatusView.xaml` | `ModuleStatusViewTests.StatsModule_ShowsInDevelopmentStatus` |
| **FR-CF-01** | Pengaturan Hide Top Bar & Trigger Strip | P0 | M01 | `TopBarVisibilityStateMachine.cs` | `TopBarVisibilityStateMachineTests` |
| **FR-CF-02** | System tray icon dengan Restore & Exit | P0 | M01 | `TrayIconManager.cs`, `App.xaml.cs` | Verifikasi integrasi platform Windows Shell |
| **FR-CF-03** | Integritas database, backup otomatis, recovery | P0 | M05 | `DatabaseIntegrityChecker.cs`, `DatabaseBackupService.cs`, `DatabaseRecoveryWindow.xaml` | `DatabaseHardeningTests` (7 unit tests integritas & recovery) |
| **FR-CF-04** | Opsi startup bersama Windows | P1 | M05 | `RegistryAutoStartService.cs` | `SettingsPanelViewModelTests.IsAutoStartEnabled_TogglesService` |
| **FR-CF-05** | Penyimpanan pengaturan lokal | P0 | M01, M05 | `SqliteSettingsStore.cs`, `SettingKeys.cs` | `SettingsPanelViewModelTests`, `DatabaseHardeningTests` |

---

## Kesimpulan Keterlacakan
Seluruh kebutuhan fungsional P0 dan P1 pada dokumen SRS telah terpetakan secara lengkap dan diverifikasi oleh unit tests, integration tests, atau pengujian platform.
