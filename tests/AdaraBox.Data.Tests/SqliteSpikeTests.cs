using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using AdaraBox.Data.TypeHandlers;
using Dapper;
using Microsoft.Data.Sqlite;
using Xunit;

namespace AdaraBox.Data.Tests;

public class SqliteSpikeTests : IDisposable
{
    private readonly string _tempFolder;

    public SqliteSpikeTests()
    {
        SqliteTypeHandlers.Register();
        _tempFolder = Path.Combine(Path.GetTempPath(), "AdaraBox_DataSpike_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempFolder);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempFolder))
            {
                Directory.Delete(_tempFolder, true);
            }
        }
        catch
        {
            // ignore cleanup failure on locked files
        }
    }

    [Fact]
    public void ForeignKeys_And_UserVersion_Work_Properly()
    {
        var dbPath = Path.Combine(_tempFolder, "pragma_test.db");
        using var connection = new SqliteConnection($"Data Source={dbPath}");
        connection.Open();

        // 1. Enable foreign keys
        connection.Execute("PRAGMA foreign_keys = ON;");
        var fkStatus = connection.ExecuteScalar<int>("PRAGMA foreign_keys;");
        Assert.Equal(1, fkStatus);

        // 2. Set and get user_version
        connection.Execute("PRAGMA user_version = 42;");
        var version = connection.ExecuteScalar<int>("PRAGMA user_version;");
        Assert.Equal(42, version);
    }

    [Fact]
    public void Transaction_Rollback_Preserves_Data_Integrity()
    {
        var dbPath = Path.Combine(_tempFolder, "tx_test.db");
        using var connection = new SqliteConnection($"Data Source={dbPath}");
        connection.Open();

        connection.Execute("CREATE TABLE Items (Id TEXT PRIMARY KEY, Name TEXT NOT NULL);");

        using (var tx = connection.BeginTransaction())
        {
            connection.Execute("INSERT INTO Items (Id, Name) VALUES (@Id, @Name)", new { Id = "1", Name = "Committed" }, tx);
            tx.Commit();
        }

        using (var tx = connection.BeginTransaction())
        {
            connection.Execute("INSERT INTO Items (Id, Name) VALUES (@Id, @Name)", new { Id = "2", Name = "RolledBack" }, tx);
            tx.Rollback();
        }

        var count = connection.ExecuteScalar<int>("SELECT COUNT(*) FROM Items;");
        Assert.Equal(1, count);

        var name = connection.ExecuteScalar<string>("SELECT Name FROM Items WHERE Id = '1';");
        Assert.Equal("Committed", name);
    }

    [Fact]
    public void DatabasePath_With_Spaces_And_Unicode_Works_Flawlessly()
    {
        var unicodeDir = Path.Combine(_tempFolder, "Uji Coba Spasi & Dokumén ü");
        Directory.CreateDirectory(unicodeDir);
        var dbPath = Path.Combine(unicodeDir, "adarabox_test.db");

        using var connection = new SqliteConnection($"Data Source={dbPath}");
        connection.Open();

        connection.Execute("CREATE TABLE Test (Id INTEGER PRIMARY KEY, Val TEXT);");
        connection.Execute("INSERT INTO Test (Val) VALUES ('Karakter non-ASCII: üöä');");

        var readVal = connection.ExecuteScalar<string>("SELECT Val FROM Test;");
        Assert.Equal("Karakter non-ASCII: üöä", readVal);
    }

    [Fact]
    public async Task Wal_Mode_Enables_Concurrent_Read_And_Write()
    {
        var dbPath = Path.Combine(_tempFolder, "wal_test.db");

        // Connection 1: UI / Main Connection
        using (var conn1 = new SqliteConnection($"Data Source={dbPath}"))
        {
            conn1.Open();
            var journalMode = conn1.ExecuteScalar<string>("PRAGMA journal_mode = WAL;");
            Assert.NotNull(journalMode);
            Assert.Equal("wal", journalMode.ToLowerInvariant());

            conn1.Execute("CREATE TABLE Logs (Id INTEGER PRIMARY KEY AUTOINCREMENT, Message TEXT);");
        }

        // Run concurrent tasks simulating UI reading while Background writes
        var writerTask = Task.Run(async () =>
        {
            for (int i = 0; i < 20; i++)
            {
                using var connWriter = new SqliteConnection($"Data Source={dbPath}");
                connWriter.Open();
                connWriter.Execute("INSERT INTO Logs (Message) VALUES (@Msg)", new { Msg = $"Log entry {i}" });
                await Task.Delay(5);
            }
        });

        var readerTask = Task.Run(async () =>
        {
            int maxSeen = 0;
            for (int i = 0; i < 20; i++)
            {
                using var connReader = new SqliteConnection($"Data Source={dbPath}");
                connReader.Open();
                var count = connReader.ExecuteScalar<int>("SELECT COUNT(*) FROM Logs;");
                if (count > maxSeen) maxSeen = count;
                await Task.Delay(5);
            }
            return maxSeen;
        });

        await Task.WhenAll(writerTask, readerTask);

        using var finalConn = new SqliteConnection($"Data Source={dbPath}");
        finalConn.Open();
        var total = finalConn.ExecuteScalar<int>("SELECT COUNT(*) FROM Logs;");
        Assert.Equal(20, total);
    }

    private sealed record TestEntity(Guid Id, string Name, DateTimeOffset CreatedAt);

    [Fact]
    public void Dapper_Maps_Guid_And_DateTimeOffset_As_Text()
    {
        var dbPath = Path.Combine(_tempFolder, "dapper_mapping.db");
        using var connection = new SqliteConnection($"Data Source={dbPath}");
        connection.Open();

        connection.Execute(@"
            CREATE TABLE Entities (
                Id TEXT PRIMARY KEY,
                Name TEXT NOT NULL,
                CreatedAt TEXT NOT NULL
            );
        ");

        var testId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var entity = new TestEntity(testId, "Workspace Test", now);

        var rowsInserted = connection.Execute("INSERT INTO Entities (Id, Name, CreatedAt) VALUES (@Id, @Name, @CreatedAt)", entity);
        Assert.Equal(1, rowsInserted);

        // Verify stored representation in SQLite raw text
        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = "SELECT Id, CreatedAt FROM Entities WHERE Id = @Id";
            cmd.Parameters.AddWithValue("@Id", testId.ToString("D").ToUpperInvariant());
            using var reader = cmd.ExecuteReader();
            Assert.True(reader.Read());

            var rawId = reader.GetString(0);
            var rawDate = reader.GetString(1);

            Assert.Equal(testId.ToString("D").ToUpperInvariant(), rawId);
            Assert.Equal(testId, Guid.Parse(rawId));
            Assert.Equal(now, DateTimeOffset.Parse(rawDate, CultureInfo.InvariantCulture));
        }

        // Verify Dapper query mapping back to strongly typed record
        var queried = connection.QuerySingle<TestEntity>("SELECT * FROM Entities WHERE Id = @Id", new { Id = testId });
        Assert.Equal(testId, queried.Id);
        Assert.Equal("Workspace Test", queried.Name);
        Assert.Equal(now, queried.CreatedAt);
    }
}
