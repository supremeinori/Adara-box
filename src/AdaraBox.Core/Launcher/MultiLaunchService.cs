using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AdaraBox.Core.Resources;

namespace AdaraBox.Core.Launcher;

/// <summary>
/// Executes launch of multiple resources sequentially in a background task.
/// </summary>
public sealed class MultiLaunchService : IMultiLaunchService
{
    private readonly IResourceLauncher _launcher;
    private readonly TimeSpan _itemDelay;

    public MultiLaunchService(IResourceLauncher launcher, TimeSpan? itemDelay = null)
    {
        _launcher = launcher ?? throw new ArgumentNullException(nameof(launcher));
        _itemDelay = itemDelay ?? TimeSpan.FromMilliseconds(150);
    }

    public async Task<LaunchSummary> LaunchAllAsync(
        IReadOnlyList<Resource> resources,
        IProgress<LaunchResult>? progress = null,
        CancellationToken ct = default)
    {
        if (resources == null || resources.Count == 0)
        {
            return new LaunchSummary(Array.Empty<LaunchResult>());
        }

        return await Task.Run(async () =>
        {
            var results = new List<LaunchResult>(resources.Count);

            for (int i = 0; i < resources.Count; i++)
            {
                if (ct.IsCancellationRequested)
                {
                    break;
                }

                var res = resources[i];
                LaunchResult result;

                try
                {
                    result = await _launcher.LaunchAsync(res, ct).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception)
                {
                    result = new LaunchResult(
                        res.Id,
                        res.Name,
                        Success: false,
                        Reason: LaunchFailureReason.Unknown,
                        Message: "Terjadi kesalahan tidak terduga saat membuka resource.");
                }

                results.Add(result);
                progress?.Report(result);

                if (i < resources.Count - 1 && _itemDelay > TimeSpan.Zero)
                {
                    try
                    {
                        await Task.Delay(_itemDelay, ct).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException) when (ct.IsCancellationRequested)
                    {
                        break;
                    }
                }
            }

            return new LaunchSummary(results);
        }, ct).ConfigureAwait(false);
    }
}
