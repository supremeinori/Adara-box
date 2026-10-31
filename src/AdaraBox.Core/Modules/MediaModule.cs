using AdaraBox.Core.Common;

namespace AdaraBox.Core.Modules;

public sealed class MediaModule : IAppModule
{
    public string Id => "media";
    public string DisplayName => "Media";
    public ModuleStatus Status => ModuleStatus.InDevelopment;
    public string? StatusMessage => "Masih dalam pengembangan";
    public int Order => 2;
}
