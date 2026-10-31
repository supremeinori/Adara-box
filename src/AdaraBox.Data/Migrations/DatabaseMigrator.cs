using System;
using System.Collections.Generic;
using System.Linq;
using AdaraBox.Data.Infrastructure;
using Dapper;

namespace AdaraBox.Data.Migrations;

public sealed class DatabaseMigrator
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IReadOnlyList<IMigration> _migrations;
    private readonly IDatabaseBackupService? _backupService;
    private readonly string? _dbPath;
    private readonly string? _backupsDir;

    public DatabaseMigrator(
        IDbConnectionFactory connectionFactory,
        IEnumerable<IMigration>? migrations = null,
        IDatabaseBackupService? backupService = null,
        string? dbPath = null,
        string? backupsDir = null)
    {
        _connectionFactory = connectionFactory;
        _migrations = migrations?.OrderBy(m => m.Version).ToList()
                      ?? (IReadOnlyList<IMigration>)new IMigration[]
                      {
                          new Migration001_InitialSchema(),
                          new Migration002_MediaLinksSchema(),
                          new Migration003_WorkspaceIconSchema()
                      };
        _backupService = backupService;
        _dbPath = dbPath;
        _backupsDir = backupsDir;
    }

    public void Migrate()
    {
        using var connection = _connectionFactory.CreateConnection();

        var currentVersion = connection.ExecuteScalar<int>("PRAGMA user_version;");
        var maxKnownVersion = _migrations.Count > 0 ? _migrations.Max(m => m.Version) : 0;

        if (currentVersion > maxKnownVersion)
        {
            throw new InvalidOperationException(
                $"Database dibuat oleh versi aplikasi yang lebih baru (versi {currentVersion}, aplikasi mendukung hingga versi {maxKnownVersion}).");
        }

        var pendingMigrations = _migrations
            .Where(m => m.Version > currentVersion)
            .OrderBy(m => m.Version)
            .ToList();

        if (pendingMigrations.Count == 0)
        {
            return;
        }

        // Automatic backup before applying pending migrations if schema already exists (T1)
        if (currentVersion > 0 && _backupService != null && !string.IsNullOrEmpty(_dbPath) && !string.IsNullOrEmpty(_backupsDir))
        {
            _backupService.CreateBackup(_dbPath, _backupsDir, currentVersion);
        }

        foreach (var migration in pendingMigrations)
        {
            using var transaction = connection.BeginTransaction();
            try
            {
                connection.Execute(migration.Sql, transaction: transaction);
                connection.Execute($"PRAGMA user_version = {migration.Version};", transaction: transaction);
                transaction.Commit();
            }
            catch (Exception ex)
            {
                try
                {
                    transaction.Rollback();
                }
                catch
                {
                    // preserve original migration failure exception
                }

                throw new InvalidOperationException(
                    $"Migration {migration.Version} ('{migration.Description}') failed and was rolled back: {ex.Message}",
                    ex);
            }
        }
    }
}
