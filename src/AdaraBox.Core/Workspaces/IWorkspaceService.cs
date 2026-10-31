using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AdaraBox.Core.Common;

namespace AdaraBox.Core.Workspaces;

public interface IWorkspaceService
{
    Task<IReadOnlyList<Workspace>> ListAsync(CancellationToken ct = default);
    Task<OperationResult<Workspace>> CreateAsync(string name, CancellationToken ct = default);
    Task<OperationResult> RenameAsync(Guid id, string newName, CancellationToken ct = default);
    Task<OperationResult> DeleteAsync(Guid id, CancellationToken ct = default);
}
