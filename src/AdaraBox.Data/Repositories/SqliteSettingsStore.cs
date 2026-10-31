using System.Threading;
using System.Threading.Tasks;
using AdaraBox.Core.Settings;
using AdaraBox.Data.Infrastructure;
using Dapper;

namespace AdaraBox.Data.Repositories;

public sealed class SqliteSettingsStore : ISettingsStore
{
    private readonly IDbConnectionFactory _connectionFactory;

    public SqliteSettingsStore(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<string?> GetAsync(string key, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<string>(
            "SELECT value FROM settings WHERE key = @Key;",
            new { Key = key });
    }

    public async Task SetAsync(string key, string value, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            INSERT INTO settings (key, value)
            VALUES (@Key, @Value)
            ON CONFLICT(key) DO UPDATE SET value = excluded.value;
        ";
        await connection.ExecuteAsync(sql, new { Key = key, Value = value });
    }
}
