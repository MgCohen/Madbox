using System.Collections.Generic;
using System.Threading.Tasks;
using Madbox.LiveOps.CloudCode.FetchData;
using Madbox.LiveOps.CloudCode.Signal;
using Madbox.LiveOps.DTO.ModuleRequests;
using Unity.Services.CloudCode.Core;

namespace Madbox.LiveOps.CloudCode.Response
{
    public sealed class ModuleRequestHandler
    {
        private readonly SignalModule _signalModule;
        private readonly IPlayerData _playerData;

        public ModuleRequestHandler(SignalModule signalModule, IPlayerData playerData)
        {
            _signalModule = signalModule;
            _playerData = playerData;
        }

        public ModuleRequest Request { get; private set; }
        public List<ModuleResponse> Responses { get; private set; } = new List<ModuleResponse>();

        public void SetCurrentRequest(ModuleRequest request)
        {
            Request = request;
        }

        public void NotifyRequestResolve(ModuleRequest request)
        {
            if (request == null)
            {
                return;
            }
            _signalModule.Push(request);
        }

        public async Task<T> ResolveResponse<T>(IExecutionContext context, ModuleRequest<T> request, T response, IPlayerData playerData = null) where T : ModuleResponse
        {
            if (request == null || context == null)
            {
                return null;
            }

            NotifyRequestResolve(request);
            if (playerData != null)
            {
                await playerData.SaveCache(context);
            }
            else
            {
                await _playerData.SaveCache(context);
            }

            return response;
        }

        public void AddResponse(ModuleResponse response)
        {
            if (response == null)
            {
                return;
            }
            Responses.Add(response);
        }
    }
}
