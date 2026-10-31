using System;
using System.IO;
using AdaraBox.Data.TypeHandlers;
using Dapper;
using Microsoft.Data.Sqlite;

namespace AdaraBox.SqliteSpike;

public static class Program
{
    private sealed record WorkspaceItem(Guid Id, string Name, DateTimeOffset CreatedAt);

    public static int Main()
    {
        Console.WriteLine("==================================================");
        Console.WriteLine("Adara Box — Spike D: SQLite & Dapper Verification");
        Console.WriteLine("==================================================");

        SqliteTypeHandlers.Register();

        var tempDir = Path.Combine(Path.GetTempPath(), "AdaraBox_SqliteSpike_" + Guid.NewGuid().ToString("N"));
        var nonAsciiDir = Path.Combine(tempDir, "Folder Dokumén ü & Spasi");
        Directory.CreateDirectory(nonAsciiDir);

        var dbPath = Path.Combine(nonAsciiDir, "adarabox_spike.db");
        Console.WriteLine($"Database Path: {dbPath}");

        using (var conn = new SqliteConnection($"Data Source={dbPath}"))
        {
            conn.Open();

            // 1. Foreign keys and WAL mode
            conn.Execute("PRAGMA foreign_keys = ON;");
            var fk = conn.ExecuteScalar<int>("PRAGMA foreign_keys;");
            var journal = conn.ExecuteScalar<string>("PRAGMA journal_mode = WAL;");
            Console.WriteLine($"[1] PRAGMA foreign_keys = {fk} (Expected: 1)");
            Console.WriteLine($"[2] PRAGMA journal_mode = {journal} (Expected: wal)");

            // 2. PRAGMA user_version
            conn.Execute("PRAGMA user_version = 1;");
            var version = conn.ExecuteScalar<int>("PRAGMA user_version;");
            Console.WriteLine($"[3] PRAGMA user_version = {version} (Expected: 1)");

            // 3. Table creation
            conn.Execute(@"
                CREATE TABLE Workspaces (
                    Id TEXT PRIMARY KEY,
                    Name TEXT NOT NULL,
                    CreatedAt TEXT NOT NULL
                );
            ");

            // 4. Dapper Guid and DateTimeOffset mapping
            var item = new WorkspaceItem(Guid.NewGuid(), "College & Research üöä", DateTimeOffset.UtcNow);
            conn.Execute("INSERT INTO Workspaces (Id, Name, CreatedAt) VALUES (@Id, @Name, @CreatedAt)", item);

            var loaded = conn.QuerySingle<WorkspaceItem>("SELECT * FROM Workspaces WHERE Id = @Id", new { Id = item.Id });
            Console.WriteLine($"[4] Stored & Loaded via Dapper:");
            Console.WriteLine($"    Id: {loaded.Id} (matches: {loaded.Id == item.Id})");
            Console.WriteLine($"    Name: {loaded.Name} (matches: {loaded.Name == item.Name})");
            Console.WriteLine($"    CreatedAt: {loaded.CreatedAt:O} (matches: {loaded.CreatedAt == item.CreatedAt})");
        }

        Console.WriteLine("\nSpike D verification complete.");

        try { Directory.Delete(tempDir, true); } catch { }
        return 0;
    }
}
