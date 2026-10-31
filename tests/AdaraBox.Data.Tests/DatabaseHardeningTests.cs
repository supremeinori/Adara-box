using System;
using System.IO;
using System.Linq;
using AdaraBox.Data.Infrastructure;
using AdaraBox.Data.Migrations;
using Dapper;
using Microsoft.Data.Sqlite;
using Xunit;

namespace AdaraBox.Data.Tests;

public class DatabaseHardeningTests
{
    private static string GetTempDbPath() =>
        Path.Combine(Path.GetTempPath(), $"AdaraBox_Hardening_{Guid.NewGuid():N}.db");

    private static string GetTempDir() =>
        Path.Combine(Path.GetTempPath(), $"AdaraBox_Backups_{Guid.NewGuid():N}");

    [Fact]
    public void IntegrityCheck_ReturnsCorrupt_WhenFileContainsRandomBytes()
    {
        var dbPath = GetTempDbPath();
        try
        {
            File.WriteAllBytes(dbPath, new byte[] { 0xDE, 0xAD, 0xBE, 0xEF, 0x01, 0x02, 0x03, 0x04 });

            var result = DatabaseIntegrityChecker.Check(dbPath, maxKnownVersion: 1);

            Assert.Equal(DatabaseIntegrityStatus.Corrupt, result.Status);
            Assert.NotNull(result.Message);
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (File.Exists(dbPath)) File.Delete(dbPath);
        }
    }

    [Fact]
    public void IntegrityCheck_ReturnsEmptyFile_WhenFileIsZeroBytes()
    {
        var dbPath = GetTempDbPath();
        try
        {
            File.WriteAllBytes(dbPath, Array.Empty<byte>());

            var result = DatabaseIntegrityChecker.Check(dbPath, maxKnownVersion: 1);

            Assert.Equal(DatabaseIntegrityStatus.EmptyFile, result.Status);
            Assert.NotNull(result.Message);
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (File.Exists(dbPath)) File.Delete(dbPath);
        }
    }

    [Fact]
    public void IntegrityCheck_ReturnsNewerVersion_WhenUserVersionExceedsMaxKnown()
    {
        var dbPath = GetTempDbPath();
        try
        {
            using (var connection = new SqliteConnection($"Data Source={dbPath}"))
            {
                connection.Open();
                connection.Execute("PRAGMA user_version = 99;");
            }

            var result = DatabaseIntegrityChecker.Check(dbPath, maxKnownVersion: 1);

            Assert.Equal(DatabaseIntegrityStatus.NewerVersion, result.Status);
            Assert.Equal(99, result.CurrentVersion);
            Assert.Contains("lebih baru", result.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (File.Exists(dbPath)) File.Delete(dbPath);
        }
    }

    [Fact]
    public void Migrator_ThrowsException_WhenDatabaseIsFromNewerVersion()
    {
        var dbPath = GetTempDbPath();
        try
        {
            using (var connection = new SqliteConnection($"Data Source={dbPath}"))
            {
                connection.Open();
                connection.Execute("PRAGMA user_version = 5;");
            }

            var factory = new SqliteConnectionFactory(dbPath);
            var migrator = new DatabaseMigrator(factory, new[] { new Migration001_InitialSchema() }); // max known = 1

            var ex = Assert.Throws<InvalidOperationException>(() => migrator.Migrate());
            Assert.Contains("lebih baru", ex.Message);
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (File.Exists(dbPath)) File.Delete(dbPath);
        }
    }

    [Fact]
    public void BackupService_CreatesVacuumBackup_AndPrunesOldBackups()
    {
        var dbPath = GetTempDbPath();
        var backupsDir = GetTempDir();

        try
        {
            // Setup DB
            var factory = new SqliteConnectionFactory(dbPath);
            var migrator = new DatabaseMigrator(factory);
            migrator.Migrate();

            var backupService = new DatabaseBackupService();

            // Create 7 backups
            for (int i = 1; i <= 7; i++)
            {
                var created = backupService.CreateBackup(dbPath, backupsDir, userVersion: 1);
                Assert.NotNull(created);
                Assert.True(File.Exists(created));
                // Slight delay to ensure distinct timestamps
                System.Threading.Thread.Sleep(1050);
            }

            // Pruning must retain exactly 5 newest backups
            var retained = backupService.GetAvailableBackups(backupsDir);
            Assert.Equal(DatabaseBackupService.MaxRetainedBackups, retained.Count);
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (File.Exists(dbPath)) File.Delete(dbPath);
            if (Directory.Exists(backupsDir)) Directory.Delete(backupsDir, true);
        }
    }

    [Fact]
    public void RecoveryService_ArchiveAndStartFresh_RenamesCorruptDatabase()
    {
        var dbPath = GetTempDbPath();
        try
        {
            File.WriteAllText(dbPath, "corrupt content");

            var backupService = new DatabaseBackupService();
            var success = backupService.ArchiveAndStartFresh(dbPath);

            Assert.True(success);
            Assert.False(File.Exists(dbPath));

            // Verify .corrupt file was created
            var dir = Path.GetDirectoryName(dbPath)!;
            var fileName = Path.GetFileName(dbPath);
            var corruptFiles = Directory.GetFiles(dir, $"{fileName}.corrupt-*");
            Assert.NotEmpty(corruptFiles);

            foreach (var f in corruptFiles) File.Delete(f);
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (File.Exists(dbPath)) File.Delete(dbPath);
        }
    }

    [Fact]
    public void RecoveryService_RestoreBackup_RestoresDataAccurately()
    {
        var dbPath = GetTempDbPath();
        var backupsDir = GetTempDir();

        try
        {
            // 1. Setup healthy DB with 1 workspace
            var factory = new SqliteConnectionFactory(dbPath);
            var migrator = new DatabaseMigrator(factory);
            migrator.Migrate();

            using (var conn = factory.CreateConnection())
            {
                conn.Execute("INSERT INTO workspace (id, name, created_at, updated_at, sort_order) VALUES ('ws-1', 'BackupWS', '2026-10-31', '2026-10-31', 0);");
            }

            // 2. Create backup
            var backupService = new DatabaseBackupService();
            var backupFile = backupService.CreateBackup(dbPath, backupsDir, userVersion: 1);
            Assert.NotNull(backupFile);

            // 3. Corrupt primary DB
            SqliteConnection.ClearAllPools();
            File.WriteAllBytes(dbPath, new byte[] { 0x00, 0x00, 0x00 });

            // 4. Restore from backup
            var restored = backupService.RestoreBackup(dbPath, backupFile);
            Assert.True(restored);

            // 5. Verify restored DB is readable and contains workspace
            using (var conn = factory.CreateConnection())
            {
                var count = conn.ExecuteScalar<int>("SELECT COUNT(*) FROM workspace;");
                Assert.Equal(1, count);
            }
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (File.Exists(dbPath)) File.Delete(dbPath);
            if (Directory.Exists(backupsDir)) Directory.Delete(backupsDir, true);
        }
    }
}
