using AdaraBox.Core.Common;

namespace AdaraBox.Core.Modules;

public sealed class SettingsModule : IAppModule
{
    public string Id => "settings";
    public string DisplayName => "Settings";
    public ModuleStatus Status => ModuleStatus.Available;
    public string? StatusMessage => null;
    public int Order => 4;
}
