using System;

namespace AdaraBox.Core.Resources;

public sealed record Resource
{
    public Guid Id { get; init; }
    public Guid WorkspaceId { get; init; }
    public string Name { get; init; } = string.Empty;
    public ResourceType Type { get; init; }
    public string Target { get; init; } = string.Empty;
    public int SortOrder { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }

    public Resource() { }

    public Resource(
        Guid id,
        Guid workspaceId,
        string name,
        ResourceType type,
        string target,
        int sortOrder,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt)
    {
        Id = id;
        WorkspaceId = workspaceId;
        Name = name;
        Type = type;
        Target = target;
        SortOrder = sortOrder;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }
}
