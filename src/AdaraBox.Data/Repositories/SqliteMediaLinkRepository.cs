using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AdaraBox.Core.Media;
using AdaraBox.Data.Infrastructure;
using Dapper;

namespace AdaraBox.Data.Repositories;

public sealed class SqliteMediaLinkRepository : IMediaLinkRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    private sealed class MediaLinkRow
    {
        public string id { get; set; } = string.Empty;
        public string name { get; set; } = string.Empty;
        public string url { get; set; } = string.Empty;
        public int sort_order { get; set; }
        public string created_at { get; set; } = string.Empty;
        public string updated_at { get; set; } = string.Empty;

        public MediaLink ToDomain() => new(
            Guid.Parse(id),
            name,
            url,
            sort_order,
            DateTimeOffset.Parse(created_at),
            DateTimeOffset.Parse(updated_at));
    }

    public SqliteMediaLinkRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
    }

    public async Task<IReadOnlyList<MediaLink>> ListAsync(CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = "SELECT id, name, url, sort_order, created_at, updated_at FROM media_link ORDER BY sort_order ASC, created_at ASC;";
        var rows = await connection.QueryAsync<MediaLinkRow>(sql);
        return rows.Select(r => r.ToDomain()).ToList();
    }

    public async Task<MediaLink?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = "SELECT id, name, url, sort_order, created_at, updated_at FROM media_link WHERE id = @Id LIMIT 1;";
        var row = await connection.QuerySingleOrDefaultAsync<MediaLinkRow>(sql, new { Id = id.ToString() });
        return row?.ToDomain();
    }

    public async Task InsertAsync(MediaLink link, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            INSERT INTO media_link (id, name, url, sort_order, created_at, updated_at)
            VALUES (@Id, @Name, @Url, @SortOrder, @CreatedAt, @UpdatedAt);";

        await connection.ExecuteAsync(sql, new
        {
            Id = link.Id.ToString(),
            Name = link.Name,
            Url = link.Url,
            SortOrder = link.SortOrder,
            CreatedAt = link.CreatedAt.ToString("o"),
            UpdatedAt = link.UpdatedAt.ToString("o")
        });
    }

    public async Task UpdateAsync(MediaLink link, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            UPDATE media_link
            SET name = @Name,
                url = @Url,
                sort_order = @SortOrder,
                updated_at = @UpdatedAt
            WHERE id = @Id;";

        await connection.ExecuteAsync(sql, new
        {
            Id = link.Id.ToString(),
            Name = link.Name,
            Url = link.Url,
            SortOrder = link.SortOrder,
            UpdatedAt = link.UpdatedAt.ToString("o")
        });
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = "DELETE FROM media_link WHERE id = @Id;";
        await connection.ExecuteAsync(sql, new { Id = id.ToString() });
    }

    public async Task UpdateOrderAsync(IReadOnlyList<Guid> orderedIds, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        connection.Open();
        using var tx = connection.BeginTransaction();
        const string sql = "UPDATE media_link SET sort_order = @SortOrder WHERE id = @Id;";

        for (int i = 0; i < orderedIds.Count; i++)
        {
            await connection.ExecuteAsync(sql, new { Id = orderedIds[i].ToString(), SortOrder = i }, tx);
        }

        tx.Commit();
    }
}
