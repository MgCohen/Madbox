using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using global::Madbox.LiveOps.Ugs;
using Unity.Services.CloudCode;

namespace Madbox.LiveOps.CloudCode
{
    public sealed class CloudCodeModuleService : ICloudCodeModuleService
    {
        public CloudCodeModuleService(IUgsInitializationService ugsInitializationService)
        {
            if (ugsInitializationService == null)
            {
                throw new ArgumentNullException(nameof(ugsInitializationService));
            }

            Ugs = ugsInitializationService;
        }

        private IUgsInitializationService Ugs { get; }

        public async Task<string> CallModuleEndpointJsonAsync(string moduleName, string functionName, Dictionary<string, object> payload, CancellationToken cancellationToken = default)
        {
            GuardCall(moduleName, functionName, payload);
            await Ugs.EnsureInitializedAsync(cancellationToken);
            return await CloudCodeService.Instance.CallModuleEndpointAsync(moduleName, functionName, payload);
        }

        private void GuardCall(string moduleName, string functionName, Dictionary<string, object> payload)
        {
            if (string.IsNullOrEmpty(moduleName))
            {
                throw new ArgumentException("Module name is required.", nameof(moduleName));
            }

            if (string.IsNullOrEmpty(functionName))
            {
                throw new ArgumentException("Function name is required.", nameof(functionName));
            }

            if (payload == null)
            {
                throw new ArgumentNullException(nameof(payload));
            }
        }
    }
}
