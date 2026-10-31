using AdaraBox.Core.Common;

namespace AdaraBox.Core.Modules;

public sealed class WorkspaceModule : IAppModule
{
    public string Id => "workspace";
    public string DisplayName => "Workspace";
    public ModuleStatus Status => ModuleStatus.Available;
    public string? StatusMessage => null;
    public int Order => 1;
}
