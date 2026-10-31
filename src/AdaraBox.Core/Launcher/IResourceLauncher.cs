using System.Threading;
using System.Threading.Tasks;
using AdaraBox.Core.Resources;

namespace AdaraBox.Core.Launcher;

/// <summary>
/// Abstraction for launching a single resource (File, Folder, Application, Link).
/// </summary>
public interface IResourceLauncher
{
    Task<LaunchResult> LaunchAsync(Resource resource, CancellationToken ct = default);
}
