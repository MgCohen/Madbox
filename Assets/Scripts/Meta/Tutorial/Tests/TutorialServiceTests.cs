using System.Threading;
using System.Threading.Tasks;
using GameModuleDTO.GameModule;
using GameModuleDTO.ModuleRequests;
using GameModuleDTO.Modules.Tutorial;
using Madbox.LiveOps;
using Madbox.Scope.Contracts;
using NUnit.Framework;
using VContainer;

namespace Madbox.Tutorial.Tests
{
    public sealed class TutorialServiceTests
    {
        [Test]
        public void InitializeAsync_LoadsTutorialGameDataFromLiveOps()
        {
            TutorialGameData expected = new TutorialGameData(new TutorialPersistence(), new TutorialConfig());
            ContainerBuilder builder = new ContainerBuilder();
            builder.RegisterInstance(new StubLiveOps(expected)).As<ILiveOpsService>();
            builder.Register<TestTutorialService>(Lifetime.Scoped).AsSelf().As<IAsyncLayerInitializable>();

            using (IObjectResolver container = builder.Build())
            {
                TestTutorialService sut = container.Resolve<TestTutorialService>();
                ((IAsyncLayerInitializable)sut).InitializeAsync(container, CancellationToken.None).GetAwaiter().GetResult();

                Assert.That(sut.ExposedData, Is.SameAs(expected));
            }
        }

        private sealed class TestTutorialService : TutorialService
        {
            public TutorialGameData ExposedData => data;
        }

        private sealed class StubLiveOps : ILiveOpsService
        {
            private readonly TutorialGameData data;

            public StubLiveOps(TutorialGameData data)
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
