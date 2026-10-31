using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AdaraBox.Core.Common;
using AdaraBox.Core.Resources;

namespace AdaraBox.Core.Media;

public sealed class MediaLinkService : IMediaLinkService
{
    private readonly IMediaLinkRepository _repository;
    private readonly IClock _clock;

    public const int MaxNameLength = 80;

    public MediaLinkService(IMediaLinkRepository repository, IClock clock)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    public async Task<OperationResult<MediaLink>> CreateAsync(string name, string url, CancellationToken ct = default)
    {
        var validation = Validate(name, url);
        if (!validation.Success)
        {
            return OperationResult<MediaLink>.Fail(validation.Error!.Code, validation.Error.Message);
        }

        var existing = await _repository.ListAsync(ct).ConfigureAwait(false);
        int nextOrder = existing.Count > 0 ? existing.Max(x => x.SortOrder) + 1 : 0;
        var now = _clock.UtcNow;

        var entity = new MediaLink(
            Guid.NewGuid(),
            name.Trim(),
            url.Trim(),
            nextOrder,
            now,
            now);

        await _repository.InsertAsync(entity, ct).ConfigureAwait(false);
        return OperationResult<MediaLink>.Ok(entity);
    }

    public async Task<OperationResult> UpdateAsync(Guid id, string name, string url, CancellationToken ct = default)
    {
        var existing = await _repository.GetByIdAsync(id, ct).ConfigureAwait(false);
        if (existing == null)
        {
            return OperationResult.Fail("media.not_found", "Tautan media tidak ditemukan.");
        }

        var validation = Validate(name, url);
        if (!validation.Success)
        {
            return validation;
        }

        var updated = existing with
        {
            Name = name.Trim(),
            Url = url.Trim(),
            UpdatedAt = _clock.UtcNow
        };

        await _repository.UpdateAsync(updated, ct).ConfigureAwait(false);
        return OperationResult.Ok();
    }

    public async Task<OperationResult> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var existing = await _repository.GetByIdAsync(id, ct).ConfigureAwait(false);
        if (existing == null)
        {
            return OperationResult.Fail("media.not_found", "Tautan media tidak ditemukan.");
        }

        await _repository.DeleteAsync(id, ct).ConfigureAwait(false);
        return OperationResult.Ok();
    }

    public Task<IReadOnlyList<MediaLink>> ListAsync(CancellationToken ct = default)
    {
        return _repository.ListAsync(ct);
    }

    public async Task<OperationResult> ReorderAsync(IReadOnlyList<Guid> orderedIds, CancellationToken ct = default)
    {
        if (orderedIds == null)
        {
            return OperationResult.Fail("media.order.invalid", "Daftar urutan tidak valid.");
        }

        await _repository.UpdateOrderAsync(orderedIds, ct).ConfigureAwait(false);
        return OperationResult.Ok();
    }

    private static OperationResult Validate(string name, string url)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return OperationResult.Fail("media.name.empty", "Nama tautan media tidak boleh kosong.");
        }

        if (name.Trim().Length > MaxNameLength)
        {
            return OperationResult.Fail("media.name.too_long", $"Nama tautan maksimal {MaxNameLength} karakter.");
        }

        if (string.IsNullOrWhiteSpace(url))
        {
            return OperationResult.Fail("media.url.empty", "URL media tidak boleh kosong.");
        }

        // Validate URL using ResourceTargetValidator for Link (strictly http/https, rejects javascript:, file:, etc.)
        var urlValidation = ResourceTargetValidator.Validate(ResourceType.Link, url.Trim(), name.Trim());
        if (!urlValidation.Success)
        {
            return OperationResult.Fail("media.url.invalid", "Format URL tidak valid (harus diawali http:// atau https://).");
        }

        return OperationResult.Ok();
    }
}
