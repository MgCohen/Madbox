using System.Threading;
using System.Threading.Tasks;
using GameModuleDTO.GameModule;
using GameModuleDTO.ModuleRequests;
using GameModuleDTO.Modules.Level;
using Madbox.Level;
using Madbox.LiveOps;
using Madbox.Scope.Contracts;
using NUnit.Framework;
using VContainer;

namespace Madbox.Level.Tests
{
    public sealed class LevelServiceTests
    {
        [Test]
        public async Task CompleteLevelAsync_DelegatesToLiveOps()
        {
            CompleteLevelResponse expected = new CompleteLevelResponse(true);
            StubLiveOpsForComplete stub = new StubLiveOpsForComplete(expected);
            LevelService sut = new LevelService(stub);

            CompleteLevelResponse result = await sut.CompleteLevelAsync(3, CancellationToken.None);

            Assert.That(result, Is.SameAs(expected));
            Assert.That(stub.LastLevelId, Is.EqualTo(3));
        }

        [Test]
        public void InitializeAsync_LoadsLevelGameDataFromLiveOps()
        {
            LevelGameData expected = new LevelGameData(new LevelPersistence(), new LevelConfig());
            ContainerBuilder builder = new ContainerBuilder();
            builder.RegisterInstance(new StubLiveOps(expected)).As<ILiveOpsService>();
            builder.Register<TestLevelService>(Lifetime.Scoped).AsSelf().As<IAsyncLayerInitializable>();

            using (IObjectResolver container = builder.Build())
            {
                TestLevelService sut = container.Resolve<TestLevelService>();
                ((IAsyncLayerInitializable)sut).InitializeAsync(container, CancellationToken.None).GetAwaiter().GetResult();

                Assert.That(sut.ExposedData, Is.SameAs(expected));
            }
        }

        private sealed class TestLevelService : LevelService
        {
            public TestLevelService(ILiveOpsService liveOps)
                : base(liveOps)
            {
            }

            public LevelGameData ExposedData => data;
        }

        private sealed class StubLiveOps : ILiveOpsService
        {
            private readonly LevelGameData data;

            public StubLiveOps(LevelGameData data)
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

        private sealed class StubLiveOpsForComplete : ILiveOpsService
        {
            private readonly CompleteLevelResponse _response;

            public StubLiveOpsForComplete(CompleteLevelResponse response)
            {
                _response = response;
            }

            public int? LastLevelId { get; private set; }

            public T GetModuleData<T>() where T : class, IGameModuleData
            {
                return null;
            }

            public Task<TResponse> CallAsync<TResponse>(ModuleRequest<TResponse> request, CancellationToken cancellationToken = default) where TResponse : ModuleResponse
            {
                if (request is CompleteLevelRequest clr)
                {
                    LastLevelId = clr.LevelId;
                }

                return Task.FromResult((TResponse)(object)_response);
            }
        }
    }
}
