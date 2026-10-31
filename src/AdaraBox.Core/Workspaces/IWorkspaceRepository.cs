using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AdaraBox.Core.Workspaces;

public interface IWorkspaceRepository
{
    Task<IReadOnlyList<Workspace>> GetAllAsync(CancellationToken ct = default);
    Task<Workspace?> GetAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(Workspace workspace, CancellationToken ct = default);
    Task UpdateNameAsync(Guid id, string name, DateTimeOffset updatedAt, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
    Task<int> GetNextSortOrderAsync(CancellationToken ct = default);
}
