using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AdaraBox.Core.Media;

public interface IMediaLinkRepository
{
    Task<IReadOnlyList<MediaLink>> ListAsync(CancellationToken ct = default);
    Task<MediaLink?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task InsertAsync(MediaLink link, CancellationToken ct = default);
    Task UpdateAsync(MediaLink link, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
    Task UpdateOrderAsync(IReadOnlyList<Guid> orderedIds, CancellationToken ct = default);
}
