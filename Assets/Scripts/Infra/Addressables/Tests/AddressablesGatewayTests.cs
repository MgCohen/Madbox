using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Madbox.Addressables.Contracts;
using NUnit.Framework;
using Scaffold.Types;
using UnityEngine;
using UnityEngine.AddressableAssets;
#pragma warning disable SCA0003
#pragma warning disable SCA0006

namespace Madbox.Addressables.Tests
{
    public class AddressablesGatewayTests
    {
        private const string preloadConfigLabel = "addressables-preload-config";

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
            handle.Release();
            handle.Release();
            AssertReleaseCount(client, 1);
        }

        [Test]
        public void InitializeAsync_NormalPreload_FirstConsumerReceivesPreloadedOwner()
        {
            TestAddressableAssetClient client = CreateClient();
            ConfigureAssetPreloadWrapper(client, EnemyBeeKey(), PreloadMode.Normal);
            AddressablesGateway gateway = CreateGateway(client);

            InitializeGateway(gateway);
            Assert.AreEqual(1, client.SyncCalls);
            Assert.AreEqual(1, client.CountLoadCallsForType(typeof(TestAsset)));

            IAssetHandle<TestAsset> consumerHandle = LoadEnemyBee(gateway);
            consumerHandle.Release();
            AssertReleaseCount(client, 1);
        }

        [Test]
        public void InitializeAsync_NeverDiePreload_KeepsGatewayOwnedReference()
        {
            TestAddressableAssetClient client = CreateClient();
            ConfigureAssetPreloadWrapper(client, EnemyBeeKey(), PreloadMode.NeverDie);
            AddressablesGateway gateway = CreateGateway(client);

            InitializeGateway(gateway);
            IAssetHandle<TestAsset> consumer = LoadEnemyBee(gateway);
            consumer.Release();
            AssertReleaseCount(client, 0);
        }

        [Test]
        public void InitializeAsync_WhenSyncFails_ContinuesStartup()
        {
            TestAddressableAssetClient client = CreateClient();
            client.ThrowOnSync = true;
            ConfigureAssetPreloadWrapper(client, EnemyBeeKey(), PreloadMode.Normal);
            AddressablesGateway gateway = CreateGateway(client);

            InitializeGateway(gateway);
            Assert.AreEqual(1, client.SyncCalls);
            Assert.AreEqual(1, client.CountLoadCallsForType(typeof(TestAsset)));
        }

        [Test]
        public void InitializeAsync_CalledTwice_RunsStartupOnlyOnce()
        {
            TestAddressableAssetClient client = CreateClient();
            ConfigureAssetPreloadWrapper(client, EnemyBeeKey(), PreloadMode.Normal);
            AddressablesGateway gateway = CreateGateway(client);

            InitializeGateway(gateway);
            InitializeGateway(gateway);
            Assert.AreEqual(1, client.SyncCalls);
            Assert.AreEqual(1, client.CountLoadCallsForType(typeof(TestAsset)));
        }

        [Test]
        public void InitializeAsync_NormalPreload_DuplicateRegistration_ReleasesDuplicateOwner()
        {
            TestAddressableAssetClient client = CreateClient();
            AddressablesPreloadConfigEntry first = CreateAssetEntry(typeof(TestAsset), "enemy/bee", PreloadMode.Normal);
            AddressablesPreloadConfigEntry second = CreateAssetEntry(typeof(TestAsset), "enemy/bee", PreloadMode.Normal);
            ConfigurePreloadWrapper(client, new[] { first, second });
            AddressablesGateway gateway = CreateGateway(client);

            InitializeGateway(gateway);
            IAssetHandle<TestAsset> consumerHandle = LoadEnemyBee(gateway);
            consumerHandle.Release();
            AssertReleaseCount(client, 1);
        }

        [Test]
        public void InitializeAsync_CatalogPreload_NormalMode_HandsOffOwnersPerKey()
        {
            TestAddressableAssetClient client = CreateClient();
            ConfigureEnemyCatalog(client);
            AddressablesPreloadConfigEntry entry = CreateLabelEntry(typeof(TestAsset), "enemy", PreloadMode.Normal);
            ConfigurePreloadWrapper(client, new[] { entry });
            AddressablesGateway gateway = CreateGateway(client);

            InitializeGateway(gateway);
            IAssetHandle<TestAsset> bee = LoadEnemyBee(gateway);
            IAssetHandle<TestAsset> slime = LoadEnemySlime(gateway);
            bee.Release();
            slime.Release();
            AssertReleaseCount(client, 2);
        }

        [Test]
        public void InitializeAsync_InvalidConfigEntry_ThrowsInvalidOperationException()
        {
            TestAddressableAssetClient client = CreateClient();
            AddressablesPreloadConfigEntry invalid = CreateAssetEntry(typeof(string), "enemy/bee", PreloadMode.Normal);
            ConfigurePreloadWrapper(client, new[] { invalid });
            AddressablesGateway gateway = CreateGateway(client);

            Assert.Throws<InvalidOperationException>(() => InitializeGateway(gateway));
        }

        [Test]
        public void LoadAsync_ByLabel_ResolvesAndLoadsAll()
        {
            TestAddressableAssetClient client = CreateClient();
            ConfigureEnemyCatalog(client);
            AddressablesGateway gateway = CreateGateway(client);

            IAssetGroupHandle<TestAsset> group = LoadEnemyLabelGroup(gateway);
            Assert.AreEqual(2, group.TypedHandles.Count);
            Assert.AreEqual(2, client.CountLoadCallsForType(typeof(TestAsset)));
        }

        [Test]
        public void LoadAsync_ByLabelGroup_ReleasesAllChildrenAtOnce()
        {
            TestAddressableAssetClient client = CreateClient();
            ConfigureEnemyCatalog(client);
            AddressablesGateway gateway = CreateGateway(client);

            IAssetGroupHandle<TestAsset> group = LoadEnemyLabelGroup(gateway);
            Assert.AreEqual(2, group.TypedHandles.Count);
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
        public void LoadAsync_ByReference_UsesSameUnderlyingOwnerLifecycle()
        {
            TestAddressableAssetClient client = CreateClient();
            AddressablesGateway gateway = CreateGateway(client);
            AssetReference reference = new AssetReference("enemy/bee");
            IAssetHandle<TestAsset> fromReference = gateway.LoadAsync<TestAsset>(reference, CancellationToken.None).GetAwaiter().GetResult();
            IAssetHandle<TestAsset> fromKey = LoadEnemyBee(gateway);
            AssertReleaseLifecycle(client, fromReference, fromKey);
        }

        [Test]
        public void LoadAsync_ByTypedReference_UsesSameUnderlyingOwnerLifecycle()
        {
            TestAddressableAssetClient client = CreateClient();
            AddressablesGateway gateway = CreateGateway(client);
            AssetReferenceT<TestAsset> reference = new AssetReferenceT<TestAsset>("enemy/bee");
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

        private TestAddressableAssetClient CreateClient()
        {
            return new TestAddressableAssetClient();
        }

        private AddressablesGateway CreateGateway(TestAddressableAssetClient client)
        {
            return new AddressablesGateway(client);
        }

        private IAssetHandle<TestAsset> LoadEnemyBee(AddressablesGateway gateway)
        {
            return gateway.LoadAsync<TestAsset>(EnemyBeeKey(), CancellationToken.None).GetAwaiter().GetResult();
        }

        private IAssetHandle<TestAsset> LoadEnemySlime(AddressablesGateway gateway)
        {
            return gateway.LoadAsync<TestAsset>(EnemySlimeKey(), CancellationToken.None).GetAwaiter().GetResult();
        }

        private IAssetGroupHandle<TestAsset> LoadEnemyLabelGroup(AddressablesGateway gateway)
        {
            return gateway.LoadAsync<TestAsset>(CreateEnemyLabelReference(), CancellationToken.None).GetAwaiter().GetResult();
        }

        private void InitializeGateway(AddressablesGateway gateway)
        {
            gateway.InitializeAsync(CancellationToken.None).GetAwaiter().GetResult();
        }

        private void ConfigureAssetPreloadWrapper(TestAddressableAssetClient client, AssetKey key, PreloadMode mode)
        {
            AddressablesPreloadConfigEntry entry = CreateAssetEntry(typeof(TestAsset), key.Value, mode);
            ConfigurePreloadWrapper(client, new[] { entry });
        }

        private void ConfigurePreloadWrapper(TestAddressableAssetClient client, IReadOnlyList<AddressablesPreloadConfigEntry> entries)
        {
            AddressablesPreloadConfigWrapper wrapper = ScriptableObject.CreateInstance<AddressablesPreloadConfigWrapper>();
            SetField(wrapper, "entries", new List<AddressablesPreloadConfigEntry>(entries));
            string wrapperKey = "config/preload/default";
            client.CatalogToKeys[preloadConfigLabel] = new[] { wrapperKey };
            client.ObjectAssets[wrapperKey] = wrapper;
        }

        private AddressablesPreloadConfigEntry CreateAssetEntry(Type assetType, string key, PreloadMode mode)
        {
            AddressablesPreloadConfigEntry entry = new AddressablesPreloadConfigEntry();
            SetField(entry, "assetType", new TypeReference(assetType));
            SetField(entry, "referenceType", PreloadReferenceType.AssetReference);
            SetField(entry, "assetReference", new AssetReference(key));
            SetField(entry, "labelReference", new AssetLabelReference());
            SetField(entry, "mode", mode);
            return entry;
        }

        private AddressablesPreloadConfigEntry CreateLabelEntry(Type assetType, string label, PreloadMode mode)
        {
            AddressablesPreloadConfigEntry entry = new AddressablesPreloadConfigEntry();
            SetField(entry, "assetType", new TypeReference(assetType));
            SetField(entry, "referenceType", PreloadReferenceType.LabelReference);
            SetField(entry, "assetReference", null);
            SetField(entry, "labelReference", new AssetLabelReference { labelString = label });
            SetField(entry, "mode", mode);
            return entry;
        }

        private AssetKey EnemyBeeKey()
        {
            return new AssetKey("enemy/bee");
        }

        private AssetKey EnemySlimeKey()
        {
            return new AssetKey("enemy/slime");
        }

        private AssetLabelReference CreateEnemyLabelReference()
        {
            return new AssetLabelReference { labelString = "enemy" };
        }

        private void ConfigureEnemyCatalog(TestAddressableAssetClient client)
        {
            client.CatalogToKeys["enemy"] = new[] { "enemy/bee", "enemy/slime" };
        }

        private void AssertReleaseCount(TestAddressableAssetClient client, int count)
        {
            Assert.AreEqual(count, client.ReleaseCalls.Count);
        }

        private void AssertReleaseLifecycle(TestAddressableAssetClient client, IAssetHandle<TestAsset> first, IAssetHandle<TestAsset> second)
        {
            Assert.AreEqual(1, client.CountLoadCallsForType(typeof(TestAsset)));
            first.Release();
            AssertReleaseCount(client, 0);
            second.Release();
            AssertReleaseCount(client, 1);
        }

        private void SetField(object target, string name, object value)
        {
            FieldInfo field = target.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
            if (field == null) { throw new InvalidOperationException($"Field '{name}' not found on '{target.GetType().FullName}'."); }
            field.SetValue(target, value);
        }

        private class TestAddressableAssetClient : IAddressablesAssetClient
        {
            public int SyncCalls { get; private set; }
            public bool ThrowOnSync { get; set; }
            public readonly List<string> LoadCalls = new List<string>();
            public readonly List<string> ReleaseCalls = new List<string>();
            public readonly Dictionary<string, IReadOnlyList<string>> CatalogToKeys = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);
            public readonly Dictionary<string, UnityEngine.Object> ObjectAssets = new Dictionary<string, UnityEngine.Object>(StringComparer.Ordinal);
            private readonly Dictionary<string, TestAsset> cache = new Dictionary<string, TestAsset>(StringComparer.Ordinal);

            public Task SyncCatalogAndContentAsync(CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                SyncCalls++;
                if (ThrowOnSync) { throw new InvalidOperationException("sync failed"); }
                return Task.CompletedTask;
            }

            public Task<T> LoadAssetAsync<T>(AssetKey key, CancellationToken cancellationToken) where T : UnityEngine.Object
            {
                cancellationToken.ThrowIfCancellationRequested();
                LoadCalls.Add($"{typeof(T).FullName}|{key.Value}");
                if (ObjectAssets.TryGetValue(key.Value, out UnityEngine.Object existing)) { return Task.FromResult(existing as T); }
                TestAsset asset = GetOrCreateAsset(key.Value);
                return Task.FromResult(asset as T);
            }

            public Task<IReadOnlyList<AssetKey>> ResolveLabelAsync(Type assetType, AssetLabelReference label, CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (CatalogToKeys.TryGetValue(label.labelString, out IReadOnlyList<string> keys)) { return Task.FromResult(ConvertToAssetKeys(keys)); }
                return Task.FromResult((IReadOnlyList<AssetKey>)Array.Empty<AssetKey>());
            }

            public void Release(UnityEngine.Object asset)
            {
                if (asset is TestAsset testAsset) { ReleaseCalls.Add(testAsset.AssetId); }
            }

            public int CountLoadCallsForType(Type type)
            {
                string prefix = $"{type.FullName}|";
                int count = 0;
                for (int i = 0; i < LoadCalls.Count; i++)
                {
                    if (LoadCalls[i].StartsWith(prefix, StringComparison.Ordinal)) { count++; }
                }
                return count;
            }

            private TestAsset GetOrCreateAsset(string key)
            {
                if (cache.TryGetValue(key, out TestAsset existing)) { return existing; }
                TestAsset created = ScriptableObject.CreateInstance<TestAsset>();
                created.AssetId = key;
                cache[key] = created;
                return created;
            }

            private IReadOnlyList<AssetKey> ConvertToAssetKeys(IReadOnlyList<string> keys)
            {
                List<AssetKey> result = new List<AssetKey>(keys.Count);
                for (int i = 0; i < keys.Count; i++) { result.Add(new AssetKey(keys[i])); }
                return result;
            }
        }

        private class RecordingGateway : IAddressablesGateway
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

        private class TestAsset : ScriptableObject
        {
            public string AssetId;
        }
    }
}
#pragma warning restore SCA0006
#pragma warning restore SCA0003
