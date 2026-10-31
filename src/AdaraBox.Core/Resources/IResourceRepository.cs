using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AdaraBox.Core.Resources;

public interface IResourceRepository
{
    Task<IReadOnlyList<Resource>> GetByWorkspaceAsync(Guid workspaceId, CancellationToken ct = default);
    Task<Resource?> GetAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(Resource resource, CancellationToken ct = default);
    Task AddRangeAsync(IEnumerable<Resource> resources, CancellationToken ct = default);
    Task UpdateAsync(Guid id, string name, string target, DateTimeOffset updatedAt, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
    Task ReorderAsync(Guid workspaceId, IReadOnlyList<Guid> orderedIds, CancellationToken ct = default);
    Task<int> GetNextSortOrderAsync(Guid workspaceId, CancellationToken ct = default);
    Task<int> GetCountByWorkspaceAsync(Guid workspaceId, CancellationToken ct = default);
}
