using System.Threading;
using System.Threading.Tasks;

namespace AdaraBox.Core.Settings;

public interface ISettingsStore
{
    Task<string?> GetAsync(string key, CancellationToken ct = default);
    Task SetAsync(string key, string value, CancellationToken ct = default);
}
