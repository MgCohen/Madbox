using System.Threading;
using System.Threading.Tasks;
using GameModuleDTO.GameModule;
using GameModuleDTO.ModuleRequests;
using Madbox.Scope.Contracts;
using NUnit.Framework;
using VContainer;

namespace Madbox.LiveOps.Tests
{
    public sealed class GameClientModuleBaseTests
    {
        [Test]
        public void InitializeAsync_LoadsDataFromLiveOpsAfterInitialGameData()
        {
            DummyModuleData moduleData = new DummyModuleData();
            StubLiveOps liveOps = new StubLiveOps(moduleData);

            ContainerBuilder builder = new ContainerBuilder();
            builder.RegisterInstance(liveOps).As<ILiveOpsService>();
            builder.Register<DummyClientModule>(Lifetime.Singleton).AsSelf().As<IAsyncLayerInitializable>();

            using (IObjectResolver container = builder.Build())
            {
                DummyClientModule module = container.Resolve<DummyClientModule>();
                ((IAsyncLayerInitializable)module).InitializeAsync(container, CancellationToken.None).GetAwaiter().GetResult();

                Assert.That(module.ExposedData, Is.SameAs(moduleData));
            }
        }

        private sealed class DummyModuleData : IGameModuleData
        {
            public string Key => nameof(DummyModuleData);
        }

        private sealed class DummyClientModule : GameClientModuleBase<DummyModuleData>
        {
            public DummyModuleData ExposedData => data;
        }

        private sealed class StubLiveOps : ILiveOpsService
        {
            private readonly DummyModuleData data;

            public StubLiveOps(DummyModuleData data)
            {
                this.data = data;
            }

            public T GetModuleData<T>() where T : class, IGameModuleData
            {
                return data as T;
            }

            public Task<TResponse> CallAsync<TResponse>(ModuleRequest<TResponse> request, CancellationToken cancellationToken = default) where TResponse : ModuleResponse
            {
                return Task.FromResult(default(TResponse));
            }
        }
    }
}
