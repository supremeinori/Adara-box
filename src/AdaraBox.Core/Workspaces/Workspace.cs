using System;

namespace AdaraBox.Core.Workspaces;

/// <summary>
/// Domain model for an Adara Box Workspace.
/// Groups references to applications, files, folders, and links.
/// </summary>
public sealed record Workspace
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
    public int SortOrder { get; init; }

    public Workspace()
    {
    }

    public Workspace(Guid id, string name, DateTimeOffset createdAt, DateTimeOffset updatedAt, int sortOrder)
    {
        Id = id;
        Name = name;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
        SortOrder = sortOrder;
    }
}
