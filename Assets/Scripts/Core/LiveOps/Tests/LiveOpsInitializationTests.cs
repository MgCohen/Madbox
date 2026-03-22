using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GameModuleDTO.GameModule;
using GameModuleDTO.ModuleRequests;
using GameModuleDTO.Modules.Ads;
using Madbox.CloudCode;
using Madbox.Scope.Contracts;
using NUnit.Framework;
using VContainer;

namespace Madbox.LiveOps.Tests
{
    public sealed class LiveOpsInitializationTests
    {
        [Test]
        public void InitializeAsync_FetchesAndStoresGameData()
        {
            ContainerBuilder builder = new ContainerBuilder();
            builder.RegisterInstance(new GameDataResponseCloudStub()).As<ICloudCodeModuleService>();
            builder.Register<LiveOpsService>(Lifetime.Scoped).As<ILiveOpsService>().As<IAsyncLayerInitializable>();

            using (IObjectResolver container = builder.Build())
            {
                IAsyncLayerInitializable init = container.Resolve<IAsyncLayerInitializable>();
                init.InitializeAsync(container, CancellationToken.None).GetAwaiter().GetResult();
                ILiveOpsService liveOps = container.Resolve<ILiveOpsService>();
                Assert.That(liveOps.GetModuleData<AdsGameData>(), Is.Not.Null);
            }
        }

        private sealed class GameDataResponseCloudStub : ICloudCodeModuleService
        {
            public Task<T> CallEndpointAsync<T>(string module, string endpoint, int maxRetries = 2, int retryCall = 2, Dictionary<string, object> payload = null, CancellationToken cancellationToken = default)
            {
                if (typeof(T) == typeof(GameDataResponse))
                {
                    GameData gameData = new GameData();
                    gameData.AddModuleData(AdsGameData.From(new AdsPersistence(), new AdsConfig()));
                    GameDataResponse response = new GameDataResponse(gameData);
                    return Task.FromResult((T)(object)response);
                }

                return Task.FromResult(default(T));
            }
        }
    }
}
