using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using GameModuleDTO.GameModule;
using GameModuleDTO.ModuleRequests;
using GameModuleDTO.Modules.Level;
using Madbox.Addressables.Contracts;
using Madbox.Level;
using Madbox.LiveOps;
using Madbox.Levels;
using Madbox.Scope.Contracts;
using NUnit.Framework;
using UnityEngine;
using VContainer;

namespace Madbox.Level.Tests
{
    public sealed class LevelServiceTests
    {
        [Test]
        public void CompleteLevelAsync_DelegatesToLiveOps()
        {
            CompleteLevelResponse expected = new CompleteLevelResponse(true);
            StubLiveOpsForComplete stub = new StubLiveOpsForComplete(expected);
            EmptyGroupProvider provider = new EmptyGroupProvider();
            LevelService sut = new LevelService(stub, provider);

            CompleteLevelResponse result = sut.CompleteLevelAsync(3, CancellationToken.None).GetAwaiter().GetResult();

            Assert.That(result, Is.SameAs(expected));
            Assert.That(stub.LastLevelId, Is.EqualTo(3));
        }

        [Test]
        public void InitializeAsync_LoadsLevelGameDataFromLiveOps()
        {
            LevelGameData expected = new LevelGameData(new LevelPersistence(), CreateConfig(1, 2, 3, 4));
            ContainerBuilder builder = new ContainerBuilder();
            builder.RegisterInstance(new StubLiveOps(expected)).As<ILiveOpsService>();
            builder.RegisterInstance<IAssetGroupProvider<LevelDefinition>>(new EmptyGroupProvider());
            builder.Register<TestLevelService>(Lifetime.Scoped).AsSelf().As<IAsyncLayerInitializable>();

            using (IObjectResolver container = builder.Build())
            {
                TestLevelService sut = container.Resolve<TestLevelService>();
                ((IAsyncLayerInitializable)sut).InitializeAsync(container, CancellationToken.None).GetAwaiter().GetResult();

                Assert.That(sut.ExposedData, Is.SameAs(expected));
            }
        }

        [Test]
        public void InitializeAsync_MapsDefinitionsToGameDataStates_InOrder()
        {
            LevelDefinition d1 = CreateLevelDefinition(1);
            LevelDefinition d2 = CreateLevelDefinition(2);
            FakeGroupProvider provider = new FakeGroupProvider(new[] { d2, d1 });
            LevelGameData data = new LevelGameData(new LevelPersistence(), CreateConfig(1, 2, 3, 4));
            ContainerBuilder builder = new ContainerBuilder();
            builder.RegisterInstance(new StubLiveOps(data)).As<ILiveOpsService>();
            builder.RegisterInstance<IAssetGroupProvider<LevelDefinition>>(provider);
            builder.Register<LevelService>(Lifetime.Scoped).AsSelf().As<IAsyncLayerInitializable>();

            using (IObjectResolver container = builder.Build())
            {
                LevelService sut = container.Resolve<LevelService>();
                ((IAsyncLayerInitializable)sut).InitializeAsync(container, CancellationToken.None).GetAwaiter().GetResult();

                IReadOnlyList<AvailableLevel> levels = sut.GetAvailableLevels();
                Assert.That(levels.Count, Is.EqualTo(2));
                Assert.That(levels[0].Definition.LevelId, Is.EqualTo(1));
                Assert.That(levels[1].Definition.LevelId, Is.EqualTo(2));
                Assert.That(levels[0].AvailabilityState, Is.EqualTo(data.States[0].State));
                Assert.That(levels[1].AvailabilityState, Is.EqualTo(data.States[1].State));
            }
        }

        private static LevelConfig CreateConfig(params int[] ids)
        {
            LevelConfig config = new LevelConfig();
            FieldInfo field = typeof(LevelConfig).GetField("_levels", BindingFlags.NonPublic | BindingFlags.Instance);
            field.SetValue(config, new List<int>(ids));
            return config;
        }

        private static LevelDefinition CreateLevelDefinition(int levelId)
        {
            LevelDefinition def = ScriptableObject.CreateInstance<LevelDefinition>();
            FieldInfo field = typeof(LevelDefinition).GetField("levelId", BindingFlags.NonPublic | BindingFlags.Instance);
            field.SetValue(def, levelId);
            return def;
        }

        private sealed class TestLevelService : LevelService
        {
            public TestLevelService(ILiveOpsService liveOps, IAssetGroupProvider<LevelDefinition> provider)
                : base(liveOps, provider)
            {
            }

            public LevelGameData ExposedData => data;
        }

        private sealed class EmptyGroupProvider : IAssetGroupProvider<LevelDefinition>
        {
            public bool TryGet(out IReadOnlyList<LevelDefinition> assets)
            {
                assets = System.Array.Empty<LevelDefinition>();
                return false;
            }
        }

        private sealed class FakeGroupProvider : IAssetGroupProvider<LevelDefinition>
        {
            public FakeGroupProvider(IReadOnlyList<LevelDefinition> assets)
            {
                this.assets = assets;
            }

            private readonly IReadOnlyList<LevelDefinition> assets;

            public bool TryGet(out IReadOnlyList<LevelDefinition> outAssets)
            {
                outAssets = assets;
                return assets != null && assets.Count > 0;
            }
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
            private readonly CompleteLevelResponse response;

            public StubLiveOpsForComplete(CompleteLevelResponse response)
            {
                this.response = response;
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

                return Task.FromResult((TResponse)(object)response);
            }
        }
    }
}
