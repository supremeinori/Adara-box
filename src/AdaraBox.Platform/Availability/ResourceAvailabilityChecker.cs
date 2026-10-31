using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AdaraBox.Core.Resources;

namespace AdaraBox.Platform.Availability;

public sealed class ResourceAvailabilityChecker : IResourceAvailabilityChecker
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(2);
    private readonly SemaphoreSlim _throttler;
    private readonly TimeSpan _timeout;

    public ResourceAvailabilityChecker(int maxConcurrency = 8, TimeSpan? timeout = null)
    {
        _throttler = new SemaphoreSlim(maxConcurrency, maxConcurrency);
        _timeout = timeout ?? DefaultTimeout;
    }

    public async Task<ResourceAvailability> CheckAsync(Resource resource, CancellationToken ct = default)
    {
        if (resource == null) return ResourceAvailability.Invalid;

        if (ct.IsCancellationRequested)
        {
            return ResourceAvailability.Unknown;
        }

        if (resource.Type == ResourceType.Link)
        {
            // Golden Rule #5: Never make network requests to check link availability
            return ResourceAvailability.Unknown;
        }

        if (resource.Type == ResourceType.Application &&
            resource.Target.StartsWith(@"shell:AppsFolder\", StringComparison.OrdinalIgnoreCase))
        {
            return ResourceAvailability.Unknown;
        }

        await _throttler.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeoutCts.CancelAfter(_timeout);

            return await Task.Run(() =>
            {
                if (timeoutCts.Token.IsCancellationRequested)
                {
                    return ResourceAvailability.Unknown;
                }

                try
                {
                    switch (resource.Type)
                    {
                        case ResourceType.File:
                        case ResourceType.Application:
                            return File.Exists(resource.Target)
                                ? ResourceAvailability.Available
                                : ResourceAvailability.Missing;

                        case ResourceType.Folder:
                            return Directory.Exists(resource.Target)
                                ? ResourceAvailability.Available
                                : ResourceAvailability.Missing;

                        default:
                            return ResourceAvailability.Unknown;
                    }
                }
                catch (UnauthorizedAccessException)
                {
                    return ResourceAvailability.AccessDenied;
                }
                catch (ArgumentException)
                {
                    return ResourceAvailability.Invalid;
                }
                catch (NotSupportedException)
                {
                    return ResourceAvailability.Invalid;
                }
                catch (PathTooLongException)
                {
                    return ResourceAvailability.Invalid;
                }
                catch
                {
                    return ResourceAvailability.Unknown;
                }
            }, timeoutCts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Timeout elapsed or cancellation requested
            return ResourceAvailability.Unknown;
        }
        finally
        {
            _throttler.Release();
        }
    }
}
