using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Madbox.Addressables.Contracts;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Madbox.Addressables.Tests
{
    public sealed class AddressablesGatewayTests
    {
        [Test]
        public void LoadAsync_SameKeyTwice_LoadsOnceAndReleasesOnce()
        {
            TestAddressableAssetClient client = CreateClient();
            AddressablesGateway gateway = CreateGateway(client);
            IAssetHandle<TestAsset> first = LoadEnemyBee(gateway);
            IAssetHandle<TestAsset> second = LoadEnemyBee(gateway);
            AssertReleaseLifecycle(client, first, second);
        }

        [Test]
        public void Release_CalledTwice_IsNoOpAfterFirstRelease()
        {
            TestAddressableAssetClient client = CreateClient();
            AddressablesGateway gateway = CreateGateway(client);
            IAssetHandle<TestAsset> handle = LoadEnemyBee(gateway);
            ReleaseTwice(handle);
            AssertReleaseCount(client, 1);
        }

        [Test]
        public void InitializeAsync_NormalPreload_FirstConsumerReceivesPreloadedOwner()
        {
            TestAddressableAssetClient client = CreateClient();
            AddressablesGateway gateway = CreatePreloadedGateway(client, PreloadMode.Normal);
            InitializeGateway(gateway);
            AssertSyncAndLoad(client, 1, 1);
            IAssetHandle<TestAsset> consumerHandle = LoadEnemyBee(gateway);
            ReleaseHandle(consumerHandle);
            AssertReleaseCount(client, 1);
        }

        [Test]
        public void InitializeAsync_NeverDiePreload_KeepsGatewayOwnedReference()
        {
            TestAddressableAssetClient client = CreateClient();
            AddressablesGateway gateway = CreatePreloadedGateway(client, PreloadMode.NeverDie);
            InitializeGateway(gateway);
            IAssetHandle<TestAsset> consumer = LoadEnemyBee(gateway);
            ReleaseHandle(consumer);
            AssertReleaseCount(client, 0);
        }

        [Test]
        public void InitializeAsync_WhenSyncFails_ContinuesStartup()
        {
            TestAddressableAssetClient client = CreateClientWithFailingSync();
            AddressablesGateway gateway = CreatePreloadedGateway(client, PreloadMode.Normal);
            InitializeGateway(gateway);
            AssertSyncAndLoad(client, 1, 1);
        }

        [Test]
        public void LoadAsync_ByLabel_ResolvesAndLoadsAll()
        {
            TestAddressableAssetClient client = CreateClient();
            ConfigureEnemyCatalog(client);
            AddressablesGateway gateway = CreateGateway(client);
            IAssetGroupHandle<TestAsset> group = LoadEnemyLabelGroup(gateway);
            AssertCatalogLoad(client, group.TypedHandles.Count);
        }

        [Test]
        public void LoadAsync_ByLabelGroup_ReleasesAllChildrenAtOnce()
        {
            TestAddressableAssetClient client = CreateClient();
            ConfigureEnemyCatalog(client);
            AddressablesGateway gateway = CreateGateway(client);
            IAssetGroupHandle<TestAsset> group = LoadEnemyLabelGroup(gateway);
            AssertGroupLoaded(group, 2);
            AssertReleaseCount(client, 0);
            group.Release();
            AssertReleaseCount(client, 2);
        }

        [Test]
        public void LoadAsync_ByLabelGroup_ReleaseIsIdempotent()
        {
            TestAddressableAssetClient client = CreateClient();
            ConfigureEnemyCatalog(client);
            AddressablesGateway gateway = CreateGateway(client);
            IAssetGroupHandle<TestAsset> group = LoadEnemyLabelGroup(gateway);
            group.Release();
            group.Release();
            AssertReleaseCount(client, 2);
        }

        [Test]
        public void InitializeAsync_CalledTwice_RunsStartupOnlyOnce()
        {
            TestAddressableAssetClient client = CreateClient();
            AddressablesGateway gateway = CreatePreloadedGateway(client, PreloadMode.Normal);
            InitializeGateway(gateway);
            InitializeGateway(gateway);
            AssertSyncAndLoad(client, 1, 1);
        }

        [Test]
        public void InitializeAsync_NormalPreload_DuplicateRegistration_ReleasesDuplicateOwner()
        {
            TestAddressableAssetClient client = CreateClient();
            AddressablesGateway gateway = CreateGatewayWithDuplicateNormalPreload(client);
            InitializeGateway(gateway);
            AssertSyncAndLoad(client, 1, 1);
            IAssetHandle<TestAsset> consumerHandle = LoadEnemyBee(gateway);
            ReleaseHandle(consumerHandle);
            AssertReleaseCount(client, 1);
        }

        [Test]
        public void InitializeAsync_CatalogPreload_NormalMode_HandsOffOwnersPerKey()
        {
            TestAddressableAssetClient client = CreateClient();
            AddressablesGateway gateway = CreateCatalogPreloadedGatewayWithCatalog(client);
            AssertCatalogPreloadLifecycle(client, gateway);
        }

        [Test]
        public void LoadAsync_ByReference_UsesSameUnderlyingOwnerLifecycle()
        {
            TestAddressableAssetClient client = CreateClient();
            AddressablesGateway gateway = CreateGateway(client);
            AssetReference reference = CreateEnemyBeeReference();
            IAssetHandle<TestAsset> fromReference = gateway.LoadAsync<TestAsset>(reference, CancellationToken.None).GetAwaiter().GetResult();
            IAssetHandle<TestAsset> fromKey = LoadEnemyBee(gateway);
            AssertReleaseLifecycle(client, fromReference, fromKey);
        }

        [Test]
        public void LoadAsync_ByTypedReference_UsesSameUnderlyingOwnerLifecycle()
        {
            TestAddressableAssetClient client = CreateClient();
            AddressablesGateway gateway = CreateGateway(client);
            AssetReferenceT<TestAsset> reference = CreateEnemyBeeTypedReference();
            IAssetHandle<TestAsset> fromReference = gateway.LoadAsync(reference, CancellationToken.None).GetAwaiter().GetResult();
            IAssetHandle<TestAsset> fromKey = LoadEnemyBee(gateway);
            AssertReleaseLifecycle(client, fromReference, fromKey);
        }

        [Test]
        public void LayerInitializer_InvokesGatewayInitialize()
        {
            RecordingGateway gateway = new RecordingGateway();
            AddressablesLayerInitializer initializer = new AddressablesLayerInitializer(gateway);

            initializer.InitializeAsync(CancellationToken.None).GetAwaiter().GetResult();

            Assert.AreEqual(1, gateway.InitializeCalls);
        }

        [Test]
        public void PreloadRegistry_RegisterUntypedKey_ThrowsNotSupported()
        {
            AddressablesPreloadRegistry registry = new AddressablesPreloadRegistry();
            Assert.Throws<NotSupportedException>(() => RegisterUntypedKey(registry));
        }

        [Test]
        public void PreloadRegistry_RegisterUntypedReference_ThrowsNotSupported()
        {
            AddressablesPreloadRegistry registry = new AddressablesPreloadRegistry();
            AssetReference reference = CreateEnemyBeeReference();
            Assert.Throws<NotSupportedException>(() => RegisterUntypedReference(registry, reference));
        }

        [Test]
        public void PreloadRegistry_RegisterUntypedLabel_ThrowsNotSupported()
        {
            AddressablesPreloadRegistry registry = new AddressablesPreloadRegistry();
            AssetLabelReference label = CreateEnemyLabelReference();
            Assert.Throws<NotSupportedException>(() => RegisterUntypedLabel(registry, label));
        }

        private void RegisterUntypedKey(AddressablesPreloadRegistry registry)
        {
#pragma warning disable CS0618
            AssetKey key = EnemyBeeKey();
            registry.Register(key, PreloadMode.Normal);
#pragma warning restore CS0618
        }

        private void RegisterUntypedReference(AddressablesPreloadRegistry registry, AssetReference reference)
        {
#pragma warning disable CS0618
            registry.Register(reference, PreloadMode.Normal);
#pragma warning restore CS0618
        }

        private void RegisterUntypedLabel(AddressablesPreloadRegistry registry, AssetLabelReference label)
        {
#pragma warning disable CS0618
            registry.Register(label, PreloadMode.Normal);
#pragma warning restore CS0618
        }

        private TestAddressableAssetClient CreateClient()
        {
            return new TestAddressableAssetClient();
        }

        private TestAddressableAssetClient CreateClientWithFailingSync()
        {
            TestAddressableAssetClient client = new TestAddressableAssetClient();
            client.ThrowOnSync = true;
            return client;
        }

        private AddressablesGateway CreateGateway(TestAddressableAssetClient client)
        {
            AddressablesPreloadRegistry registry = new AddressablesPreloadRegistry();
            return new AddressablesGateway(client, registry);
        }

        private AddressablesGateway CreatePreloadedGateway(TestAddressableAssetClient client, PreloadMode mode)
        {
            AddressablesPreloadRegistry registry = new AddressablesPreloadRegistry();
            AssetKey key = EnemyBeeKey();
            registry.Register<TestAsset>(key, mode);
            return new AddressablesGateway(client, registry);
        }

        private AddressablesGateway CreateGatewayWithDuplicateNormalPreload(TestAddressableAssetClient client)
        {
            AddressablesPreloadRegistry registry = new AddressablesPreloadRegistry();
            AssetKey key = EnemyBeeKey();
            registry.Register<TestAsset>(key, PreloadMode.Normal);
            registry.Register<TestAsset>(key, PreloadMode.Normal);
            return new AddressablesGateway(client, registry);
        }

        private AssetKey EnemyBeeKey()
        {
            return new AssetKey("enemy/bee");
        }

        private IAssetHandle<TestAsset> LoadEnemyBee(AddressablesGateway gateway)
        {
            AssetKey key = EnemyBeeKey();
            return gateway.LoadAsync<TestAsset>(key, CancellationToken.None).GetAwaiter().GetResult();
        }

        private AddressablesGateway CreateCatalogPreloadedGatewayWithCatalog(TestAddressableAssetClient client)
        {
            ConfigureEnemyCatalog(client);
            return CreateCatalogPreloadedGateway(client, PreloadMode.Normal);
        }

        private void AssertCatalogPreloadLifecycle(TestAddressableAssetClient client, AddressablesGateway gateway)
        {
            InitializeGateway(gateway);
            AssertSyncAndLoad(client, 1, 2);
            IAssetHandle<TestAsset> bee = LoadEnemyBee(gateway);
            IAssetHandle<TestAsset> slime = LoadEnemySlime(gateway);
            ReleaseHandle(bee);
            ReleaseHandle(slime);
            AssertReleaseCount(client, 2);
        }

        private IAssetHandle<TestAsset> LoadEnemySlime(AddressablesGateway gateway)
        {
            AssetKey key = EnemySlimeKey();
            return gateway.LoadAsync<TestAsset>(key, CancellationToken.None).GetAwaiter().GetResult();
        }

        private AssetKey EnemySlimeKey()
        {
            return new AssetKey("enemy/slime");
        }

        private AddressablesGateway CreateCatalogPreloadedGateway(TestAddressableAssetClient client, PreloadMode mode)
        {
            AddressablesPreloadRegistry registry = new AddressablesPreloadRegistry();
            AssetLabelReference label = CreateEnemyLabelReference();
            registry.Register<TestAsset>(label, mode);
            return new AddressablesGateway(client, registry);
        }

        private void ReleaseTwice(IAssetHandle<TestAsset> handle)
        {
            handle.Release();
            handle.Release();
        }

        private void ReleaseHandle(IAssetHandle<TestAsset> handle)
        {
            handle.Release();
        }

        private void InitializeGateway(AddressablesGateway gateway)
        {
            gateway.InitializeAsync(CancellationToken.None).GetAwaiter().GetResult();
        }

        private IAssetGroupHandle<TestAsset> LoadEnemyLabelGroup(AddressablesGateway gateway)
        {
            AssetLabelReference label = CreateEnemyLabelReference();
            return gateway.LoadAsync<TestAsset>(label, CancellationToken.None).GetAwaiter().GetResult();
        }

        private AssetReference CreateEnemyBeeReference()
        {
            return new AssetReference("enemy/bee");
        }

        private AssetReferenceT<TestAsset> CreateEnemyBeeTypedReference()
        {
            return new AssetReferenceT<TestAsset>("enemy/bee");
        }

        private AssetLabelReference CreateEnemyLabelReference()
        {
            AssetLabelReference label = new AssetLabelReference();
            label.labelString = "enemy";
            return label;
        }

        private void ConfigureEnemyCatalog(TestAddressableAssetClient client)
        {
            client.CatalogToKeys["enemy"] = new[] { "enemy/bee", "enemy/slime" };
        }

        private void AssertCatalogLoad(TestAddressableAssetClient client, int handleCount)
        {
            Assert.AreEqual(2, handleCount);
            Assert.AreEqual(2, client.LoadCalls.Count);
        }

        private void AssertGroupLoaded(IAssetGroupHandle<TestAsset> group, int expectedCount)
        {
            Assert.IsNotNull(group);
            Assert.AreEqual(expectedCount, group.TypedHandles.Count);
            Assert.IsFalse(group.IsReleased);
        }

        private void AssertSyncAndLoad(TestAddressableAssetClient client, int syncCount, int loadCount)
        {
            Assert.AreEqual(syncCount, client.SyncCalls);
            Assert.AreEqual(loadCount, client.LoadCalls.Count);
        }

        private void AssertReleaseCount(TestAddressableAssetClient client, int count)
        {
            Assert.AreEqual(count, client.ReleaseCalls.Count);
        }

        private void AssertReleaseLifecycle(TestAddressableAssetClient client, IAssetHandle<TestAsset> first, IAssetHandle<TestAsset> second)
        {
            Assert.AreEqual(1, client.LoadCalls.Count);
            first.Release();
            AssertReleaseCount(client, 0);
            second.Release();
            AssertReleaseCount(client, 1);
        }

        private sealed class TestAddressableAssetClient : IAddressablesAssetClient
        {
            public int SyncCalls { get; private set; }
            public bool ThrowOnSync { get; set; }
            public readonly List<string> LoadCalls = new List<string>();
            public readonly List<string> ReleaseCalls = new List<string>();
            public readonly Dictionary<string, IReadOnlyList<string>> CatalogToKeys = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);
            private readonly Dictionary<string, TestAsset> cache = new Dictionary<string, TestAsset>(StringComparer.Ordinal);

            public Task SyncCatalogAndContentAsync(CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                SyncCalls++;
                if (ThrowOnSync)
                {
                    throw new InvalidOperationException("sync failed");
                }

                return Task.CompletedTask;
            }

            public Task<T> LoadAssetAsync<T>(AssetKey key, CancellationToken cancellationToken) where T : UnityEngine.Object
            {
                cancellationToken.ThrowIfCancellationRequested();
                LoadCalls.Add($"{typeof(T).FullName}|{key.Value}");
                TestAsset asset = GetOrCreateAsset(key.Value);
                T typed = asset as T;
                return Task.FromResult(typed);
            }

            public Task<IReadOnlyList<AssetKey>> ResolveLabelAsync(Type assetType, AssetLabelReference label, CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                IReadOnlyList<string> keys = ResolveLabelValues(label);
                IReadOnlyList<AssetKey> result = ConvertToAssetKeys(keys);
                return Task.FromResult(result);
            }

            public void Release(UnityEngine.Object asset)
            {
                if (asset is TestAsset testAsset)
                {
                    ReleaseCalls.Add(testAsset.AssetId);
                }
            }

            private TestAsset GetOrCreateAsset(string key)
            {
                if (cache.TryGetValue(key, out TestAsset existing)) { return existing; }
                TestAsset created = CreateAsset(key);
                cache[key] = created;
                return created;
            }

            private TestAsset CreateAsset(string key)
            {
                TestAsset asset = ScriptableObject.CreateInstance<TestAsset>();
                asset.AssetId = key;
                return asset;
            }

            private IReadOnlyList<string> ResolveLabelValues(AssetLabelReference label)
            {
                if (CatalogToKeys.TryGetValue(label.labelString, out IReadOnlyList<string> keys)) { return keys; }
                return Array.Empty<string>();
            }

            private IReadOnlyList<AssetKey> ConvertToAssetKeys(IReadOnlyList<string> keys)
            {
                List<AssetKey> result = new List<AssetKey>(keys.Count);
                foreach (string key in keys)
                {
                    AssetKey assetKey = new AssetKey(key);
                    result.Add(assetKey);
                }

                return result;
            }
        }

        private sealed class RecordingGateway : IAddressablesGateway
        {
            public int InitializeCalls { get; private set; }

            public Task InitializeAsync(CancellationToken cancellationToken = default)
            {
                InitializeCalls++;
                return Task.CompletedTask;
            }

            public Task<IAssetHandle<T>> LoadAsync<T>(AssetKey key, CancellationToken cancellationToken = default) where T : UnityEngine.Object
            {
                throw new NotSupportedException();
            }

            public Task<IAssetGroupHandle<T>> LoadAsync<T>(AssetLabelReference label, CancellationToken cancellationToken = default) where T : UnityEngine.Object
            {
                throw new NotSupportedException();
            }

            public Task<IAssetHandle<T>> LoadAsync<T>(AssetReference reference, CancellationToken cancellationToken = default) where T : UnityEngine.Object
            {
                throw new NotSupportedException();
            }

            public Task<IAssetHandle<T>> LoadAsync<T>(AssetReferenceT<T> reference, CancellationToken cancellationToken = default) where T : UnityEngine.Object
            {
                throw new NotSupportedException();
            }
        }

        private sealed class TestAsset : ScriptableObject
        {
            public string AssetId;
        }
    }
}
