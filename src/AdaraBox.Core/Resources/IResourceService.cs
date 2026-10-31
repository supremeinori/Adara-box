using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AdaraBox.Core.Common;

namespace AdaraBox.Core.Resources;

public interface IResourceService
{
    Task<IReadOnlyList<Resource>> ListAsync(Guid workspaceId, CancellationToken ct = default);
    Task<OperationResult<Resource>> AddAsync(Guid workspaceId, ResourceType type, string target, string? displayName, CancellationToken ct = default);
    Task<OperationResult<IReadOnlyList<Resource>>> AddManyAsync(Guid workspaceId, ResourceType type, IEnumerable<string> targets, CancellationToken ct = default);
    Task<OperationResult> UpdateAsync(Guid id, string name, string target, CancellationToken ct = default);
    Task<OperationResult> RemoveAsync(Guid id, CancellationToken ct = default);
    Task<OperationResult> MoveAsync(Guid id, int direction, CancellationToken ct = default);
    Task<int> GetCountAsync(Guid workspaceId, CancellationToken ct = default);
}
