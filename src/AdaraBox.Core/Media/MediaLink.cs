using System;

namespace AdaraBox.Core.Media;

/// <summary>
/// Represents a user-curated media service or playlist quick link (M08 Tahap A).
/// </summary>
public sealed record MediaLink(
    Guid Id,
    string Name,
    string Url,
    int SortOrder,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
