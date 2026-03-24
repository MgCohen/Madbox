using System.Threading;
using System.Threading.Tasks;
using GameModuleDTO.GameModule;
using GameModuleDTO.ModuleRequests;
using GameModuleDTO.Modules.Gold;
using Madbox.LiveOps;
using Madbox.Scope.Contracts;
using NUnit.Framework;
using Newtonsoft.Json;
using VContainer;

namespace Madbox.Gold.Tests
{
    public class GoldServiceTests
    {
        [Test]
        public void GetWallet_WhenCalled_ReturnsStableModelInstance()
        {
            GoldService service = new GoldService(new StubLiveOps(null));
            GoldWallet first = service.GetWallet();
            GoldWallet second = service.GetWallet();
            Assert.AreSame(first, second);
        }

        [Test]
        public void Handle_GoldChangedResponse_AddsDiffToWallet()
        {
            GoldService service = new GoldService(new StubLiveOps(null));
            service.Handle(new GoldChangedResponse(10, 10));
            Assert.AreEqual(10, service.GetWallet().CurrentGold);
        }

        [Test]
        public void Handle_GoldChangedResponse_WhenDiffZero_DoesNotChangeWallet()
        {
            GoldService service = new GoldService(new StubLiveOps(null));
            service.Handle(new GoldChangedResponse(5, 5));
            service.Handle(new GoldChangedResponse(5, 0));
            Assert.AreEqual(5, service.GetWallet().CurrentGold);
        }

        [Test]
        public void InitializeAsync_WhenLiveOpsProvidesCurrentGold_SetsWalletCurrentGold()
        {
            const string json = "{\"_current\":42,\"_min\":0,\"_max\":999999,\"_defaultRewardAmount\":50}";
            GoldGameData data = JsonConvert.DeserializeObject<GoldGameData>(json);

            ContainerBuilder builder = new ContainerBuilder();
            builder.RegisterInstance<ILiveOpsService>(new StubLiveOps(data));
            builder.Register<GoldService>(Lifetime.Singleton).AsSelf().As<IAsyncLayerInitializable>();

            using (IObjectResolver container = builder.Build())
            {
                GoldService service = container.Resolve<GoldService>();
                ((IAsyncLayerInitializable)service).InitializeAsync(container, CancellationToken.None).GetAwaiter().GetResult();
                Assert.AreEqual(42, service.GetWallet().CurrentGold);
            }
        }

        [Test]
        public void InitializeAsync_WhenCalled_UsesLiveOpsCurrentGold()
        {
            const string json = "{\"_current\":9,\"_min\":0,\"_max\":999999,\"_defaultRewardAmount\":50}";
            GoldGameData data = JsonConvert.DeserializeObject<GoldGameData>(json);

            ContainerBuilder builder = new ContainerBuilder();
            builder.RegisterInstance<ILiveOpsService>(new StubLiveOps(data));
            builder.Register<GoldService>(Lifetime.Singleton).AsSelf().As<IAsyncLayerInitializable>();

            using (IObjectResolver container = builder.Build())
            {
                GoldService service = container.Resolve<GoldService>();
                ((IAsyncLayerInitializable)service).InitializeAsync(container, CancellationToken.None).GetAwaiter().GetResult();
                Assert.AreEqual(9, service.GetWallet().CurrentGold);
            }
        }

        private sealed class StubLiveOps : ILiveOpsService
        {
            public StubLiveOps(GoldGameData data)
            {
                this.data = data;
            }

            private readonly GoldGameData data;
            private readonly System.Collections.Generic.Queue<ModuleResponse> queuedResponses = new System.Collections.Generic.Queue<ModuleResponse>();
            public int CallCount { get; private set; }

            public void EnqueueResponse(ModuleResponse response)
            {
                queuedResponses.Enqueue(response);
            }

            public T GetModuleData<T>() where T : class, IGameModuleData
            {
                return data as T;
            }

            public Task<TResponse> CallAsync<TResponse>(GameModuleDTO.ModuleRequests.ModuleRequest<TResponse> request, CancellationToken cancellationToken = default) where TResponse : GameModuleDTO.ModuleRequests.ModuleResponse
            {
                CallCount++;
                if (queuedResponses.Count > 0)
                {
                    return Task.FromResult((TResponse)queuedResponses.Dequeue());
                }

                return Task.FromResult(default(TResponse));
            }
        }
    }
}


