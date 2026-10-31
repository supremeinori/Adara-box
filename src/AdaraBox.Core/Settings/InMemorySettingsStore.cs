using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace AdaraBox.Core.Settings;

/// <summary>
/// In-memory implementation of ISettingsStore used during M01 (Application Shell).
/// Replaced by persistent SQLite settings store in Module M02.
/// </summary>
public sealed class InMemorySettingsStore : ISettingsStore
{
    private readonly ConcurrentDictionary<string, string> _store = new();

    public Task<string?> GetAsync(string key, CancellationToken ct = default)
    {
        _store.TryGetValue(key, out var value);
        return Task.FromResult(value);
    }

    public Task SetAsync(string key, string value, CancellationToken ct = default)
    {
        _store[key] = value;
        return Task.CompletedTask;
    }
}
