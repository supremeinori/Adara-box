using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AdaraBox.Core.Common;
using AdaraBox.Core.Workspaces;

namespace AdaraBox.Core.Resources;

public sealed class ResourceService : IResourceService
{
    private readonly IResourceRepository _resourceRepository;
    private readonly IWorkspaceRepository _workspaceRepository;
    private readonly IClock _clock;

    public ResourceService(
        IResourceRepository resourceRepository,
        IWorkspaceRepository workspaceRepository,
        IClock clock)
    {
        _resourceRepository = resourceRepository;
        _workspaceRepository = workspaceRepository;
        _clock = clock;
    }

    public async Task<IReadOnlyList<Resource>> ListAsync(Guid workspaceId, CancellationToken ct = default)
    {
        var ws = await _workspaceRepository.GetAsync(workspaceId, ct);
        if (ws == null)
        {
            return Array.Empty<Resource>();
        }

        return await _resourceRepository.GetByWorkspaceAsync(workspaceId, ct);
    }

    public async Task<OperationResult<Resource>> AddAsync(
        Guid workspaceId,
        ResourceType type,
        string target,
        string? displayName,
        CancellationToken ct = default)
    {
        var ws = await _workspaceRepository.GetAsync(workspaceId, ct);
        if (ws == null)
        {
            return OperationResult<Resource>.Fail("workspace.not_found", "Workspace tidak ditemukan.");
        }

        var validation = ResourceTargetValidator.Validate(type, target, displayName);
        if (!validation.Success)
        {
            return OperationResult<Resource>.Fail(validation.Error!.Code, validation.Error!.Message);
        }

        var sortOrder = await _resourceRepository.GetNextSortOrderAsync(workspaceId, ct);
        var now = _clock.UtcNow;

        var resource = new Resource(
            id: Guid.NewGuid(),
            workspaceId: workspaceId,
            name: validation.Value.DisplayName,
            type: type,
            target: validation.Value.NormalizedTarget,
            sortOrder: sortOrder,
            createdAt: now,
            updatedAt: now);

        await _resourceRepository.AddAsync(resource, ct);
        return OperationResult<Resource>.Ok(resource);
    }

    public async Task<OperationResult<IReadOnlyList<Resource>>> AddManyAsync(
        Guid workspaceId,
        ResourceType type,
        IEnumerable<string> targets,
        CancellationToken ct = default)
    {
        var ws = await _workspaceRepository.GetAsync(workspaceId, ct);
        if (ws == null)
        {
            return OperationResult<IReadOnlyList<Resource>>.Fail("workspace.not_found", "Workspace tidak ditemukan.");
        }

        var targetList = targets.ToList();
        if (targetList.Count == 0)
        {
            return OperationResult<IReadOnlyList<Resource>>.Ok(Array.Empty<Resource>());
        }

        var resources = new List<Resource>();
        var baseSortOrder = await _resourceRepository.GetNextSortOrderAsync(workspaceId, ct);
        var now = _clock.UtcNow;

        for (int i = 0; i < targetList.Count; i++)
        {
            var target = targetList[i];
            var validation = ResourceTargetValidator.Validate(type, target, null);
            if (!validation.Success)
            {
                return OperationResult<IReadOnlyList<Resource>>.Fail(validation.Error!.Code, validation.Error!.Message);
            }

            resources.Add(new Resource(
                id: Guid.NewGuid(),
                workspaceId: workspaceId,
                name: validation.Value.DisplayName,
                type: type,
                target: validation.Value.NormalizedTarget,
                sortOrder: baseSortOrder + i,
                createdAt: now,
                updatedAt: now));
        }

        await _resourceRepository.AddRangeAsync(resources, ct);
        return OperationResult<IReadOnlyList<Resource>>.Ok(resources);
    }

    public async Task<OperationResult> UpdateAsync(Guid id, string name, string target, CancellationToken ct = default)
    {
        var existing = await _resourceRepository.GetAsync(id, ct);
        if (existing == null)
        {
            return OperationResult.Fail("resource.not_found", "Resource tidak ditemukan.");
        }

        var validation = ResourceTargetValidator.Validate(existing.Type, target, name);
        if (!validation.Success)
        {
            return OperationResult.Fail(validation.Error!.Code, validation.Error!.Message);
        }

        var now = _clock.UtcNow;
        await _resourceRepository.UpdateAsync(id, validation.Value.DisplayName, validation.Value.NormalizedTarget, now, ct);
        return OperationResult.Ok();
    }

    public async Task<OperationResult> RemoveAsync(Guid id, CancellationToken ct = default)
    {
        // Golden Rule #1: ONLY deletes repository reference, NEVER deletes files on disk.
        var deleted = await _resourceRepository.DeleteAsync(id, ct);
        if (!deleted)
        {
            return OperationResult.Fail("resource.not_found", "Resource tidak ditemukan.");
        }

        return OperationResult.Ok();
    }

    public async Task<OperationResult> MoveAsync(Guid id, int direction, CancellationToken ct = default)
    {
        if (direction == 0) return OperationResult.Ok();

        var existing = await _resourceRepository.GetAsync(id, ct);
        if (existing == null)
        {
            return OperationResult.Fail("resource.not_found", "Resource tidak ditemukan.");
        }

        var items = (await _resourceRepository.GetByWorkspaceAsync(existing.WorkspaceId, ct)).ToList();
        var currentIndex = items.FindIndex(r => r.Id == id);
        if (currentIndex < 0)
        {
            return OperationResult.Fail("resource.not_found", "Resource tidak ditemukan.");
        }

        var newIndex = currentIndex + direction;
        if (newIndex < 0 || newIndex >= items.Count)
        {
            // Already at boundary
            return OperationResult.Ok();
        }

        var item = items[currentIndex];
        items.RemoveAt(currentIndex);
        items.Insert(newIndex, item);

        await _resourceRepository.ReorderAsync(existing.WorkspaceId, items.Select(r => r.Id).ToList(), ct);
        return OperationResult.Ok();
    }

    public async Task<int> GetCountAsync(Guid workspaceId, CancellationToken ct = default)
    {
        return await _resourceRepository.GetCountByWorkspaceAsync(workspaceId, ct);
    }
}
