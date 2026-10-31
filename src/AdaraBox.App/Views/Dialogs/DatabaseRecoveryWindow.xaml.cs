using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using AdaraBox.Data.Infrastructure;

namespace AdaraBox.App.Views.Dialogs;

public partial class DatabaseRecoveryWindow : Window
{
    private readonly string _dbPath;
    private readonly string _backupsDir;
    private readonly IDatabaseBackupService _backupService;

    public bool RecoveredSuccessfully { get; private set; }

    public DatabaseRecoveryWindow(
        string dbPath,
        string backupsDir,
        DatabaseIntegrityResult integrityResult,
        IDatabaseBackupService backupService)
    {
        InitializeComponent();

        _dbPath = dbPath;
        _backupsDir = backupsDir;
        _backupService = backupService;

        ErrorMessageTextBlock.Text = integrityResult.Message ?? "Integritas database tidak valid.";

        // Populate backups
        var backups = _backupService.GetAvailableBackups(_backupsDir);
        if (backups.Count > 0)
        {
            foreach (var b in backups)
            {
                BackupsListBox.Items.Add(Path.GetFileName(b));
            }
            BackupsListBox.SelectedIndex = 0;
            RestoreBackupButton.IsEnabled = true;
            NoBackupWarningTextBlock.Visibility = Visibility.Collapsed;
        }
        else
        {
            RestoreBackupButton.IsEnabled = false;
            NoBackupWarningTextBlock.Visibility = Visibility.Visible;
        }
    }

    private void OpenFolderButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var dir = Path.GetDirectoryName(_dbPath);
            if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = dir,
                    UseShellExecute = true
                });
            }
        }
        catch { }
    }

    private void RestoreBackupButton_Click(object sender, RoutedEventArgs e)
    {
        var selectedItem = BackupsListBox.SelectedItem as string;
        if (string.IsNullOrEmpty(selectedItem)) return;

        var backupPath = Path.Combine(_backupsDir, selectedItem);
        if (_backupService.RestoreBackup(_dbPath, backupPath))
        {
            RecoveredSuccessfully = true;
            DialogResult = true;
            Close();
        }
        else
        {
            MessageBox.Show("Gagal memulihkan database dari backup yang dipilih.", "Pemulihan Gagal", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void StartFreshButton_Click(object sender, RoutedEventArgs e)
    {
        var confirmed = MessageBox.Show(
            "Apakah Anda yakin ingin memulai dengan database baru?\n\nDatabase lama akan diarsipkan sebagai salinan (.corrupt-timestamp) dan tidak akan dihapus.",
            "Konfirmasi Mulai Baru",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (confirmed == MessageBoxResult.Yes)
        {
            if (_backupService.ArchiveAndStartFresh(_dbPath))
            {
                RecoveredSuccessfully = true;
                DialogResult = true;
                Close();
            }
        }
    }

    private void ExitButton_Click(object sender, RoutedEventArgs e)
    {
        RecoveredSuccessfully = false;
        DialogResult = false;
        Close();
    }
}
