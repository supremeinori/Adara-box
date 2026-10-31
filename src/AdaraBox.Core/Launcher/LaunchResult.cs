using System;

namespace AdaraBox.Core.Launcher;

/// <summary>
/// Individual result of launching a single resource.
/// </summary>
public sealed record LaunchResult(
    Guid ResourceId,
    string ResourceName,
    bool Success,
    LaunchFailureReason? Reason = null,
    string? Message = null);
