using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Scaffold.Addressables.Contracts;
using UnityEngine;

namespace Scaffold.Addressables.Tests
{
    public sealed class AddressablesGatewayTests
    {
        [Test]
        public void LoadAsync_SameKeyTwice_LoadsOnceAndReleasesOnce()
        {
            TestAddressableAssetClient client = new TestAddressableAssetClient();
            AddressablesPreloadRegistry registry = new AddressablesPreloadRegistry();
            AddressablesGateway gateway = new AddressablesGateway(client, registry);

            IAssetHandle<TestAsset> first = gateway.LoadAsync<TestAsset>(new AssetKey("enemy/bee"), CancellationToken.None).GetAwaiter().GetResult();
            IAssetHandle<TestAsset> second = gateway.LoadAsync<TestAsset>(new AssetKey("enemy/bee"), CancellationToken.None).GetAwaiter().GetResult();

            Assert.AreEqual(1, client.LoadCalls.Count);
            first.Release();
            Assert.AreEqual(0, client.ReleaseCalls.Count);
            second.Release();
            Assert.AreEqual(1, client.ReleaseCalls.Count);
        }

        [Test]
        public void Release_CalledTwice_IsNoOpAfterFirstRelease()
        {
            TestAddressableAssetClient client = new TestAddressableAssetClient();
            AddressablesPreloadRegistry registry = new AddressablesPreloadRegistry();
            AddressablesGateway gateway = new AddressablesGateway(client, registry);
            IAssetHandle<TestAsset> handle = gateway.LoadAsync<TestAsset>(new AssetKey("enemy/bee"), CancellationToken.None).GetAwaiter().GetResult();

            handle.Release();
            handle.Release();

            Assert.AreEqual(1, client.ReleaseCalls.Count);
        }

        [Test]
        public void InitializeAsync_NormalPreload_FirstConsumerReceivesPreloadedOwner()
        {
            TestAddressableAssetClient client = new TestAddressableAssetClient();
            AddressablesPreloadRegistry registry = new AddressablesPreloadRegistry();
            registry.Register<TestAsset>(new AssetKey("enemy/bee"), PreloadMode.Normal);
            AddressablesGateway gateway = new AddressablesGateway(client, registry);

            gateway.InitializeAsync(CancellationToken.None).GetAwaiter().GetResult();
            Assert.AreEqual(1, client.SyncCalls);
            Assert.AreEqual(1, client.LoadCalls.Count);

            IAssetHandle<TestAsset> firstConsumerHandle = gateway.LoadAsync<TestAsset>(new AssetKey("enemy/bee"), CancellationToken.None).GetAwaiter().GetResult();
            Assert.AreEqual(1, client.LoadCalls.Count);
            firstConsumerHandle.Release();
            Assert.AreEqual(1, client.ReleaseCalls.Count);
        }

        [Test]
        public void InitializeAsync_NeverDiePreload_KeepsGatewayOwnedReference()
        {
            TestAddressableAssetClient client = new TestAddressableAssetClient();
            AddressablesPreloadRegistry registry = new AddressablesPreloadRegistry();
            registry.Register<TestAsset>(new AssetKey("enemy/bee"), PreloadMode.NeverDie);
            AddressablesGateway gateway = new AddressablesGateway(client, registry);

            gateway.InitializeAsync(CancellationToken.None).GetAwaiter().GetResult();
            IAssetHandle<TestAsset> consumer = gateway.LoadAsync<TestAsset>(new AssetKey("enemy/bee"), CancellationToken.None).GetAwaiter().GetResult();
            consumer.Release();

            Assert.AreEqual(0, client.ReleaseCalls.Count);
        }

        [Test]
        public void InitializeAsync_WhenSyncFails_ContinuesStartup()
        {
            TestAddressableAssetClient client = new TestAddressableAssetClient
            {
                ThrowOnSync = true
            };
            AddressablesPreloadRegistry registry = new AddressablesPreloadRegistry();
            registry.Register<TestAsset>(new AssetKey("enemy/bee"), PreloadMode.Normal);
            AddressablesGateway gateway = new AddressablesGateway(client, registry);

            gateway.InitializeAsync(CancellationToken.None).GetAwaiter().GetResult();

            Assert.AreEqual(1, client.SyncCalls);
            Assert.AreEqual(1, client.LoadCalls.Count);
        }

        [Test]
        public void LoadAsync_ByCatalog_ResolvesAndLoadsAll()
        {
            TestAddressableAssetClient client = new TestAddressableAssetClient();
            client.CatalogToKeys["enemy"] = new[] { "enemy/bee", "enemy/slime" };
            AddressablesPreloadRegistry registry = new AddressablesPreloadRegistry();
            AddressablesGateway gateway = new AddressablesGateway(client, registry);

            IReadOnlyList<IAssetHandle<TestAsset>> handles = gateway.LoadAsync<TestAsset>(new CatalogKey("enemy"), CancellationToken.None).GetAwaiter().GetResult();

            Assert.AreEqual(2, handles.Count);
            Assert.AreEqual(2, client.LoadCalls.Count);
        }

        [Test]
        public void LayerInitializer_InvokesGatewayInitialize()
        {
            RecordingGateway gateway = new RecordingGateway();
            AddressablesLayerInitializer initializer = new AddressablesLayerInitializer(gateway);

            initializer.InitializeAsync(CancellationToken.None).GetAwaiter().GetResult();

            Assert.AreEqual(1, gateway.InitializeCalls);
        }

        private sealed class TestAddressableAssetClient : IAddressablesAssetClient
        {
            public readonly List<string> LoadCalls = new List<string>();
            public readonly List<string> ReleaseCalls = new List<string>();
            public readonly Dictionary<string, IReadOnlyList<string>> CatalogToKeys = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);
            private readonly Dictionary<string, TestAsset> cache = new Dictionary<string, TestAsset>(StringComparer.Ordinal);
            public int SyncCalls { get; private set; }
            public bool ThrowOnSync { get; set; }

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

                if (!cache.TryGetValue(key.Value, out TestAsset asset))
                {
                    asset = ScriptableObject.CreateInstance<TestAsset>();
                    asset.AssetId = key.Value;
                    cache[key.Value] = asset;
                }

                return Task.FromResult(asset as T);
            }

            public Task<IReadOnlyList<AssetKey>> ResolveCatalogAsync(Type assetType, CatalogKey catalog, CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!CatalogToKeys.TryGetValue(catalog.Value, out IReadOnlyList<string> keys))
                {
                    return Task.FromResult<IReadOnlyList<AssetKey>>(Array.Empty<AssetKey>());
                }

                List<AssetKey> result = new List<AssetKey>(keys.Count);
                foreach (string key in keys)
                {
                    result.Add(new AssetKey(key));
                }

                return Task.FromResult<IReadOnlyList<AssetKey>>(result);
            }

            public void Release(UnityEngine.Object asset)
            {
                if (asset is TestAsset testAsset)
                {
                    ReleaseCalls.Add(testAsset.AssetId);
                }
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

            public Task<IReadOnlyList<IAssetHandle<T>>> LoadAsync<T>(CatalogKey catalog, CancellationToken cancellationToken = default) where T : UnityEngine.Object
            {
                throw new NotSupportedException();
            }

            public Task<IAssetHandle<T>> LoadAsync<T>(AssetReferenceKey reference, CancellationToken cancellationToken = default) where T : UnityEngine.Object
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
