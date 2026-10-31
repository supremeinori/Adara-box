using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AdaraBox.Core.Workspaces;
using AdaraBox.Data.Infrastructure;
using Dapper;

namespace AdaraBox.Data.Repositories;

public sealed class SqliteWorkspaceRepository : IWorkspaceRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public SqliteWorkspaceRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<Workspace>> GetAllAsync(CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT id AS Id, name AS Name, icon AS Icon, created_at AS CreatedAt, updated_at AS UpdatedAt, sort_order AS SortOrder
            FROM workspace
            ORDER BY sort_order ASC, created_at ASC;
        ";
        var results = await connection.QueryAsync<Workspace>(sql);
        return results.ToList();
    }

    public async Task<Workspace?> GetAsync(Guid id, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT id AS Id, name AS Name, icon AS Icon, created_at AS CreatedAt, updated_at AS UpdatedAt, sort_order AS SortOrder
            FROM workspace
            WHERE id = @Id;
        ";
        return await connection.QuerySingleOrDefaultAsync<Workspace>(sql, new { Id = id });
    }

    public async Task AddAsync(Workspace workspace, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            INSERT INTO workspace (id, name, icon, created_at, updated_at, sort_order)
            VALUES (@Id, @Name, @Icon, @CreatedAt, @UpdatedAt, @SortOrder);
        ";
        await connection.ExecuteAsync(sql, workspace);
    }

    public async Task UpdateNameAsync(Guid id, string name, DateTimeOffset updatedAt, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            UPDATE workspace
            SET name = @Name, updated_at = @UpdatedAt
            WHERE id = @Id;
        ";
        await connection.ExecuteAsync(sql, new { Id = id, Name = name, UpdatedAt = updatedAt });
    }

    public async Task UpdateWorkspaceAsync(Guid id, string name, string icon, DateTimeOffset updatedAt, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            UPDATE workspace
            SET name = @Name, icon = @Icon, updated_at = @UpdatedAt
            WHERE id = @Id;
        ";
        await connection.ExecuteAsync(sql, new { Id = id, Name = name, Icon = icon, UpdatedAt = updatedAt });
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = "DELETE FROM workspace WHERE id = @Id;";
        var rows = await connection.ExecuteAsync(sql, new { Id = id });
        return rows > 0;
    }

    public async Task<int> GetNextSortOrderAsync(CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = "SELECT COALESCE(MAX(sort_order), -1) + 1 FROM workspace;";
        return await connection.ExecuteScalarAsync<int>(sql);
    }
}
