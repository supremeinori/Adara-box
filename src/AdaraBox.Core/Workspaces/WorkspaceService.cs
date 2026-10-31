using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AdaraBox.Core.Common;
using AdaraBox.Core.Resources;

namespace AdaraBox.Core.Workspaces;

public sealed class WorkspaceService : IWorkspaceService
{
    private readonly IWorkspaceRepository _repository;
    private readonly IClock _clock;
    private readonly IResourceRepository? _resourceRepository;

    public WorkspaceService(
        IWorkspaceRepository repository,
        IClock clock,
        IResourceRepository? resourceRepository = null)
    {
        _repository = repository;
        _clock = clock;
        _resourceRepository = resourceRepository;
    }

    public async Task<IReadOnlyList<Workspace>> ListAsync(CancellationToken ct = default)
    {
        return await _repository.GetAllAsync(ct);
    }

    public async Task<OperationResult<Workspace>> CreateAsync(string name, CancellationToken ct = default)
    {
        var validation = WorkspaceNameValidator.Validate(name);
        if (!validation.Success || validation.Value == null)
        {
            return OperationResult<Workspace>.Fail(validation.Error!.Code, validation.Error.Message);
        }

        var validName = validation.Value;
        var sortOrder = await _repository.GetNextSortOrderAsync(ct);
        var now = _clock.UtcNow;

        var workspace = new Workspace(
            id: Guid.NewGuid(),
            name: validName,
            createdAt: now,
            updatedAt: now,
            sortOrder: sortOrder);

        await _repository.AddAsync(workspace, ct);
        return OperationResult<Workspace>.Ok(workspace);
    }

    public async Task<OperationResult> RenameAsync(Guid id, string newName, CancellationToken ct = default)
    {
        var validation = WorkspaceNameValidator.Validate(newName);
        if (!validation.Success || validation.Value == null)
        {
            return OperationResult.Fail(validation.Error!.Code, validation.Error.Message);
        }

        var existing = await _repository.GetAsync(id, ct);
        if (existing == null)
        {
            return OperationResult.Fail("workspace.not_found", "Workspace tidak ditemukan.");
        }

        await _repository.UpdateNameAsync(id, validation.Value, _clock.UtcNow, ct);
        return OperationResult.Ok();
    }

    public async Task<OperationResult> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var existing = await _repository.GetAsync(id, ct);
        if (existing == null)
        {
            return OperationResult.Fail("workspace.not_found", "Workspace tidak ditemukan.");
        }

        // Golden Rule #1: Delete database reference only, never touch user's physical files
        await _repository.DeleteAsync(id, ct);
        return OperationResult.Ok();
    }

    public async Task<int> GetResourceCountAsync(Guid id, CancellationToken ct = default)
    {
        if (_resourceRepository == null) return 0;
        return await _resourceRepository.GetCountByWorkspaceAsync(id, ct);
    }
}
