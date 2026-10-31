using System.Threading;
using System.Threading.Tasks;

namespace AdaraBox.Core.Resources;

public interface IResourceAvailabilityChecker
{
    Task<ResourceAvailability> CheckAsync(Resource resource, CancellationToken ct = default);
}
