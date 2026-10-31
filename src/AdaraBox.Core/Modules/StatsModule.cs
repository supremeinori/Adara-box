using AdaraBox.Core.Common;

namespace AdaraBox.Core.Modules;

public sealed class StatsModule : IAppModule
{
    public string Id => "stats";
    public string DisplayName => "Stats";
    public ModuleStatus Status => ModuleStatus.InDevelopment;
    public string? StatusMessage => "Masih dalam pengembangan";
    public int Order => 3;
}
