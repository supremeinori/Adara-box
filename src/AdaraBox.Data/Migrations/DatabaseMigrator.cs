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

    public DatabaseMigrator(IDbConnectionFactory connectionFactory, IEnumerable<IMigration>? migrations = null)
    {
        _connectionFactory = connectionFactory;
        _migrations = migrations?.OrderBy(m => m.Version).ToList()
                      ?? (IReadOnlyList<IMigration>)new IMigration[] { new Migration001_InitialSchema() };
    }

    public void Migrate()
    {
        using var connection = _connectionFactory.CreateConnection();

        var currentVersion = connection.ExecuteScalar<int>("PRAGMA user_version;");

        var pendingMigrations = _migrations
            .Where(m => m.Version > currentVersion)
            .OrderBy(m => m.Version)
            .ToList();

        if (pendingMigrations.Count == 0)
        {
            return;
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
