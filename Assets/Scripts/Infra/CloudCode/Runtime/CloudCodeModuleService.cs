using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Unity.Services.CloudCode;

namespace Madbox.CloudCode
{
    public sealed class CloudCodeModuleService : ICloudCodeModuleService
    {
        public async Task<T> CallEndpointAsync<T>(string module, string endpoint, int maxRetries = 2, int retryCall = 2, Dictionary<string, object> payload = null)
        {
                Dictionary<string, object> finalPayload = payload ?? new Dictionary<string, object>();
                var response = await CloudCodeService.Instance.CallModuleEndpointAsync(module, endpoint, finalPayload);
                var settings = new JsonSerializerSettings()
                {
                    TypeNameHandling = TypeNameHandling.Auto
                };
                return JsonConvert.DeserializeObject<T>(response, settings);
        }
    }
}
