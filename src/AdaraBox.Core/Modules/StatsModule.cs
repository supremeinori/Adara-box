using AdaraBox.Core.Common;

namespace AdaraBox.Core.Modules;

public sealed class StatsModule : IAppModule
{
    public string Id => "stats";
    public string DisplayName => "Stats";
    public ModuleStatus Status => ModuleStatus.Available;
    public string? StatusMessage => null;
    public int Order => 3;
}
