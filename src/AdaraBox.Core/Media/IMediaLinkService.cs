using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AdaraBox.Core.Common;

namespace AdaraBox.Core.Media;

public interface IMediaLinkService
{
    Task<OperationResult<MediaLink>> CreateAsync(string name, string url, CancellationToken ct = default);
    Task<OperationResult> UpdateAsync(Guid id, string name, string url, CancellationToken ct = default);
    Task<OperationResult> DeleteAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<MediaLink>> ListAsync(CancellationToken ct = default);
    Task<OperationResult> ReorderAsync(IReadOnlyList<Guid> orderedIds, CancellationToken ct = default);
}
