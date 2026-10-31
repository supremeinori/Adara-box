using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AdaraBox.Core.Resources;
using AdaraBox.Data.Infrastructure;
using Dapper;

namespace AdaraBox.Data.Repositories;

public sealed class SqliteResourceRepository : IResourceRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public SqliteResourceRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<Resource>> GetByWorkspaceAsync(Guid workspaceId, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT id AS Id, workspace_id AS WorkspaceId, name AS Name, type AS Type,
                   target AS Target, sort_order AS SortOrder, created_at AS CreatedAt, updated_at AS UpdatedAt
            FROM resource
            WHERE workspace_id = @WorkspaceId
            ORDER BY sort_order ASC, created_at ASC;
        ";

        var list = await connection.QueryAsync<Resource>(sql, new { WorkspaceId = workspaceId });
        return list.ToList();
    }

    public async Task<Resource?> GetAsync(Guid id, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT id AS Id, workspace_id AS WorkspaceId, name AS Name, type AS Type,
                   target AS Target, sort_order AS SortOrder, created_at AS CreatedAt, updated_at AS UpdatedAt
            FROM resource
            WHERE id = @Id;
        ";

        return await connection.QuerySingleOrDefaultAsync<Resource>(sql, new { Id = id });
    }

    public async Task AddAsync(Resource resource, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            INSERT INTO resource (id, workspace_id, name, type, target, sort_order, created_at, updated_at)
            VALUES (@Id, @WorkspaceId, @Name, @Type, @Target, @SortOrder, @CreatedAt, @UpdatedAt);
        ";

        await connection.ExecuteAsync(sql, new
        {
            resource.Id,
            resource.WorkspaceId,
            resource.Name,
            Type = ToDbString(resource.Type),
            resource.Target,
            resource.SortOrder,
            resource.CreatedAt,
            resource.UpdatedAt
        });
    }

    public async Task AddRangeAsync(IEnumerable<Resource> resources, CancellationToken ct = default)
    {
        var list = resources.ToList();
        if (list.Count == 0) return;

        using var connection = _connectionFactory.CreateConnection();
        using var transaction = connection.BeginTransaction();
        try
        {
            const string sql = @"
                INSERT INTO resource (id, workspace_id, name, type, target, sort_order, created_at, updated_at)
                VALUES (@Id, @WorkspaceId, @Name, @Type, @Target, @SortOrder, @CreatedAt, @UpdatedAt);
            ";

            foreach (var res in list)
            {
                await connection.ExecuteAsync(sql, new
                {
                    res.Id,
                    res.WorkspaceId,
                    res.Name,
                    Type = ToDbString(res.Type),
                    res.Target,
                    res.SortOrder,
                    res.CreatedAt,
                    res.UpdatedAt
                }, transaction);
            }

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task UpdateAsync(Guid id, string name, string target, DateTimeOffset updatedAt, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            UPDATE resource
            SET name = @Name, target = @Target, updated_at = @UpdatedAt
            WHERE id = @Id;
        ";

        await connection.ExecuteAsync(sql, new
        {
            Id = id,
            Name = name,
            Target = target,
            UpdatedAt = updatedAt
        });
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = "DELETE FROM resource WHERE id = @Id;";
        var rows = await connection.ExecuteAsync(sql, new { Id = id });
        return rows > 0;
    }

    public async Task ReorderAsync(Guid workspaceId, IReadOnlyList<Guid> orderedIds, CancellationToken ct = default)
    {
        if (orderedIds.Count == 0) return;

        using var connection = _connectionFactory.CreateConnection();
        using var transaction = connection.BeginTransaction();
        try
        {
            const string sql = @"
                UPDATE resource
                SET sort_order = @SortOrder
                WHERE id = @Id AND workspace_id = @WorkspaceId;
            ";

            for (int i = 0; i < orderedIds.Count; i++)
            {
                await connection.ExecuteAsync(sql, new
                {
                    SortOrder = i,
                    Id = orderedIds[i],
                    WorkspaceId = workspaceId
                }, transaction);
            }

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task<int> GetNextSortOrderAsync(Guid workspaceId, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = "SELECT COALESCE(MAX(sort_order), -1) + 1 FROM resource WHERE workspace_id = @WorkspaceId;";
        return await connection.ExecuteScalarAsync<int>(sql, new { WorkspaceId = workspaceId });
    }

    public async Task<int> GetCountByWorkspaceAsync(Guid workspaceId, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = "SELECT COUNT(*) FROM resource WHERE workspace_id = @WorkspaceId;";
        return await connection.ExecuteScalarAsync<int>(sql, new { WorkspaceId = workspaceId });
    }

    private static string ToDbString(ResourceType type) => type switch
    {
        ResourceType.Application => "application",
        ResourceType.File => "file",
        ResourceType.Folder => "folder",
        ResourceType.Link => "link",
        _ => "file"
    };
}
