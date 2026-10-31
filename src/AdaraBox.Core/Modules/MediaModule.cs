using AdaraBox.Core.Common;

namespace AdaraBox.Core.Modules;

public sealed class MediaModule : IAppModule
{
    public string Id => "media";
    public string DisplayName => "Media";
    public ModuleStatus Status => ModuleStatus.Available;
    public string? StatusMessage => null;
    public int Order => 2;
}
