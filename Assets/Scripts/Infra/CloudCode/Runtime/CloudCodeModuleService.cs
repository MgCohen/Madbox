using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Madbox.Ugs;
using Unity.Services.CloudCode;

namespace Madbox.CloudCode
{
    public sealed class CloudCodeModuleService : ICloudCodeModuleService
    {
        private readonly IUgsInitializationService ugsInitializationService;

        public CloudCodeModuleService(IUgsInitializationService ugsInitializationService)
        {
            if (ugsInitializationService == null)
            {
                throw new ArgumentNullException(nameof(ugsInitializationService));
            }

            this.ugsInitializationService = ugsInitializationService;
        }

        public async Task<string> CallModuleEndpointJsonAsync(string moduleName, string functionName, Dictionary<string, object> payload, CancellationToken cancellationToken = default)
        {
            await ugsInitializationService.EnsureInitializedAsync(cancellationToken);
            return await CloudCodeService.Instance.CallModuleEndpointAsync(moduleName, functionName, payload);
        }
    }
}
