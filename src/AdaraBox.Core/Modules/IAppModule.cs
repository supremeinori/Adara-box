using AdaraBox.Core.Common;

namespace AdaraBox.Core.Modules;

/// <summary>
/// Contract for UI modules accessible from the Top Bar.
/// </summary>
public interface IAppModule
{
    string Id { get; }
    string DisplayName { get; }
    ModuleStatus Status { get; }
    string? StatusMessage { get; }
    int Order { get; }
}
