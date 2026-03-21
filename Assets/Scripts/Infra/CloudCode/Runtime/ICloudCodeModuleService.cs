using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Madbox.CloudCode
{
    public interface ICloudCodeModuleService
    {
        Task<string> CallModuleEndpointJsonAsync(string moduleName, string functionName, Dictionary<string, object> payload, CancellationToken cancellationToken = default);
    }
}
