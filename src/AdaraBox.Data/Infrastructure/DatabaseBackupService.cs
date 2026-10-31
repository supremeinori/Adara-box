using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Dapper;
using Microsoft.Data.Sqlite;

namespace AdaraBox.Data.Infrastructure;

public interface IDatabaseBackupService
{
    string? CreateBackup(string dbPath, string backupsDir, int userVersion);
    bool RestoreBackup(string dbPath, string backupPath);
    bool ArchiveAndStartFresh(string dbPath);
    IReadOnlyList<string> GetAvailableBackups(string backupsDir);
}

public sealed class DatabaseBackupService : IDatabaseBackupService
{
    public const int MaxRetainedBackups = 5;

    public string? CreateBackup(string dbPath, string backupsDir, int userVersion)
    {
        if (!File.Exists(dbPath)) return null;

        Directory.CreateDirectory(backupsDir);
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
        var backupFileName = $"adarabox-v{userVersion}-{timestamp}.db";
        var destinationPath = Path.Combine(backupsDir, backupFileName);

        using (var connection = new SqliteConnection($"Data Source={dbPath}"))
        {
            connection.Open();
            var escapedDest = destinationPath.Replace("'", "''");
            connection.Execute($"VACUUM INTO '{escapedDest}';");
        }

        PruneOldBackups(backupsDir);
        return destinationPath;
    }

    public bool RestoreBackup(string dbPath, string backupPath)
    {
        if (!File.Exists(backupPath)) return false;

        SqliteConnection.ClearAllPools();
        ArchiveCorruptDatabase(dbPath);

        File.Copy(backupPath, dbPath, overwrite: true);
        return true;
    }

    public bool ArchiveAndStartFresh(string dbPath)
    {
        SqliteConnection.ClearAllPools();
        ArchiveCorruptDatabase(dbPath);
        return true;
    }

    public IReadOnlyList<string> GetAvailableBackups(string backupsDir)
    {
        if (!Directory.Exists(backupsDir)) return Array.Empty<string>();

        var dirInfo = new DirectoryInfo(backupsDir);
        return dirInfo.GetFiles("adarabox-v*.db")
            .OrderByDescending(f => f.CreationTimeUtc)
            .Select(f => f.FullName)
            .ToList();
    }

    private static void ArchiveCorruptDatabase(string dbPath)
    {
        if (!File.Exists(dbPath)) return;

        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
        var corruptArchive = $"{dbPath}.corrupt-{timestamp}";

        try
        {
            File.Move(dbPath, corruptArchive, overwrite: true);
        }
        catch
        {
            try { File.Copy(dbPath, corruptArchive, overwrite: true); } catch { }
        }

        // Also archive WAL and SHM if present
        var wal = $"{dbPath}-wal";
        if (File.Exists(wal))
        {
            try { File.Move(wal, $"{wal}.corrupt-{timestamp}", overwrite: true); } catch { }
        }

        var shm = $"{dbPath}-shm";
        if (File.Exists(shm))
        {
            try { File.Move(shm, $"{shm}.corrupt-{timestamp}", overwrite: true); } catch { }
        }
    }

    private static void PruneOldBackups(string backupsDir)
    {
        try
        {
            var dirInfo = new DirectoryInfo(backupsDir);
            var backups = dirInfo.GetFiles("adarabox-v*.db")
                .OrderByDescending(f => f.CreationTimeUtc)
                .ToList();

            if (backups.Count > MaxRetainedBackups)
            {
                var toDelete = backups.Skip(MaxRetainedBackups);
                foreach (var file in toDelete)
                {
                    try { file.Delete(); } catch { }
                }
            }
        }
        catch
        {
            // Pruning failure shouldn't abort operations
        }
    }
}
