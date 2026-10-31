namespace AdaraBox.Core.Launcher;

/// <summary>
/// Categorized reason why a resource launch failed.
/// </summary>
public enum LaunchFailureReason
{
    NotFound,
    AccessDenied,
    InvalidTarget,
    NoHandler,
    Cancelled,
    Unknown
}
