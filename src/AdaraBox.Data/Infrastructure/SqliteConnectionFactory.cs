using System.Data.Common;
using AdaraBox.Data.TypeHandlers;
using Dapper;
using Microsoft.Data.Sqlite;

namespace AdaraBox.Data.Infrastructure;

public interface IDbConnectionFactory
{
    DbConnection CreateConnection();
}

public sealed class SqliteConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;

    public SqliteConnectionFactory(string dbPath)
    {
        SqliteTypeHandlers.Register();
        _connectionString = $"Data Source={dbPath}";
    }

    public DbConnection CreateConnection()
    {
        var connection = new SqliteConnection(_connectionString);
        connection.Open();

        // Enforce foreign keys and WAL mode on each connection
        connection.Execute("PRAGMA foreign_keys = ON; PRAGMA journal_mode = WAL;");
        return connection;
    }
}
