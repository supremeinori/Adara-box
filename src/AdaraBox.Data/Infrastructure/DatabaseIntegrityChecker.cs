using System;
using System.IO;
using Dapper;
using Microsoft.Data.Sqlite;

namespace AdaraBox.Data.Infrastructure;

public enum DatabaseIntegrityStatus
{
    Ok,
    EmptyFile,
    Corrupt,
    NewerVersion,
    Inaccessible
}

public sealed record DatabaseIntegrityResult(
    DatabaseIntegrityStatus Status,
    string? Message = null,
    int CurrentVersion = 0);

/// <summary>
/// Verifies SQLite database file integrity before startup or migrations.
/// </summary>
public static class DatabaseIntegrityChecker
{
    public static DatabaseIntegrityResult Check(string dbPath, int maxKnownVersion)
    {
        if (!File.Exists(dbPath))
        {
            return new DatabaseIntegrityResult(DatabaseIntegrityStatus.Ok);
        }

        var fileInfo = new FileInfo(dbPath);
        if (fileInfo.Length == 0)
        {
            return new DatabaseIntegrityResult(DatabaseIntegrityStatus.EmptyFile, "File database kosong (0 byte).");
        }

        try
        {
            using var connection = new SqliteConnection($"Data Source={dbPath};Mode=ReadOnly;Pooling=False");
            connection.Open();

            var checkResult = connection.ExecuteScalar<string>("PRAGMA quick_check;");
            if (!string.Equals(checkResult, "ok", StringComparison.OrdinalIgnoreCase))
            {
                return new DatabaseIntegrityResult(DatabaseIntegrityStatus.Corrupt, $"Pemeriksaan integritas gagal: {checkResult}");
            }

            var userVersion = connection.ExecuteScalar<int>("PRAGMA user_version;");
            if (userVersion > maxKnownVersion)
            {
                return new DatabaseIntegrityResult(
                    DatabaseIntegrityStatus.NewerVersion,
                    $"Database dibuat oleh versi Adara Box yang lebih baru (versi skema {userVersion}, didukung {maxKnownVersion}).",
                    userVersion);
            }

            return new DatabaseIntegrityResult(DatabaseIntegrityStatus.Ok, null, userVersion);
        }
        catch (SqliteException ex)
        {
            return new DatabaseIntegrityResult(DatabaseIntegrityStatus.Corrupt, $"Kesalahan database: {ex.Message}");
        }
        catch (UnauthorizedAccessException ex)
        {
            return new DatabaseIntegrityResult(DatabaseIntegrityStatus.Inaccessible, $"Akses ditolak: {ex.Message}");
        }
        catch (IOException ex)
        {
            return new DatabaseIntegrityResult(DatabaseIntegrityStatus.Inaccessible, $"Kesalahan I/O: {ex.Message}");
        }
        catch (Exception ex)
        {
            return new DatabaseIntegrityResult(DatabaseIntegrityStatus.Corrupt, $"Kesalahan tidak terduga: {ex.Message}");
        }
    }
}
