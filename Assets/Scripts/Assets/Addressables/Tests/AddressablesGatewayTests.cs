using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Madbox.Addressables.Contracts;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AddressableAssets;
#pragma warning disable SCA0003
#pragma warning disable SCA0005
#pragma warning disable SCA0006

namespace Madbox.Addressables.Tests
{
    public class AddressablesGatewayTests
    {
        [Test]
        public void LoadAsync_SameKeyTwice_LoadsOnceAndReleasesOnce()
        {
            TestAddressableAssetClient client = CreateClient();
            AddressablesGateway gateway = CreateGateway(client);
            IAssetHandle<TestAsset> first = CreateEnemyBeeHandle(gateway);
            IAssetHandle<TestAsset> second = CreateEnemyBeeHandle(gateway);
            BuildReleaseLifecycleAssertion(client, first, second);
        }

        [Test]
        public void Release_CalledTwice_IsNoOpAfterFirstRelease()
        {
            TestAddressableAssetClient client = CreateClient();
            AddressablesGateway gateway = CreateGateway(client);
            IAssetHandle<TestAsset> handle = CreateEnemyBeeHandle(gateway);
            handle.Release();
            handle.Release();
            BuildReleaseCountAssertion(client, 1);
        }

        [Test]
        public void InitializeAsync_WhenSyncFails_ContinuesStartup()
        {
            TestAddressableAssetClient client = CreateClient();
            client.ThrowOnSync = true;
            AddressablesGateway gateway = CreateGateway(client);
            Assert.DoesNotThrow(() => BuildGatewayInitialization(gateway));
            Assert.AreEqual(1, client.SyncCalls);
        }

        [Test]
        public void InitializeAsync_CalledTwice_RunsStartupOnlyOnce()
        {
            TestAddressableAssetClient client = CreateClient();
            AddressablesGateway gateway = CreateGateway(client);
            BuildGatewayInitialization(gateway);
            BuildGatewayInitialization(gateway);
            Assert.AreEqual(1, client.SyncCalls);
        }

        [Test]
        public void LoadAsync_ByLabel_ResolvesAndLoadsAll()
        {
            TestAddressableAssetClient client = CreateClient();
            BuildEnemyCatalog(client);
            AddressablesGateway gateway = CreateGateway(client);

            IAssetGroupHandle<TestAsset> group = CreateEnemyLabelGroupHandle(gateway);
            Assert.AreEqual(2, group.TypedHandles.Count);
            Assert.AreEqual(2, client.CountLoadCallsForType(typeof(TestAsset)));
        }

        [Test]
        public void LoadAsync_ByLabelGroup_ReleasesAllChildrenAtOnce()
        {
            TestAddressableAssetClient client = CreateClient();
            BuildEnemyCatalog(client);
            AddressablesGateway gateway = CreateGateway(client);

            IAssetGroupHandle<TestAsset> group = CreateEnemyLabelGroupHandle(gateway);
            Assert.AreEqual(2, group.TypedHandles.Count);
            group.Release();
            BuildReleaseCountAssertion(client, 2);
        }

        [Test]
        public void LoadAsync_ByLabelGroup_ReleaseIsIdempotent()
        {
            TestAddressableAssetClient client = CreateClient();
            BuildEnemyCatalog(client);
            AddressablesGateway gateway = CreateGateway(client);

            IAssetGroupHandle<TestAsset> group = CreateEnemyLabelGroupHandle(gateway);
            group.Release();
            group.Release();
            BuildReleaseCountAssertion(client, 2);
        }

        [Test]
        public void LoadAsync_ByReference_UsesSameUnderlyingOwnerLifecycle()
        {
            TestAddressableAssetClient client = CreateClient();
            AddressablesGateway gateway = CreateGateway(client);
            AssetReference reference = new AssetReference("enemy/bee");
            IAssetHandle<TestAsset> fromReference = gateway.LoadAsync<TestAsset>(reference, CancellationToken.None).GetAwaiter().GetResult();
            IAssetHandle<TestAsset> fromAgain = CreateEnemyBeeHandle(gateway);
            BuildReleaseLifecycleAssertion(client, fromReference, fromAgain);
        }

        [Test]
        public void LoadAsync_ByTypedReference_UsesSameUnderlyingOwnerLifecycle()
        {
            TestAddressableAssetClient client = CreateClient();
            AddressablesGateway gateway = CreateGateway(client);
            AssetReferenceT<TestAsset> reference = new AssetReferenceT<TestAsset>("enemy/bee");
            IAssetHandle<TestAsset> fromReference = gateway.LoadAsync(reference, CancellationToken.None).GetAwaiter().GetResult();
            IAssetHandle<TestAsset> fromAgain = CreateEnemyBeeHandle(gateway);
            BuildReleaseLifecycleAssertion(client, fromReference, fromAgain);
        }

        [Test]
        public void Load_ByReference_TransitionsToReadyAndReleasesOnce()
        {
            TestAddressableAssetClient client = CreateClient();
            client.LoadGate = new TaskCompletionSource<bool>();
            AddressablesGateway gateway = CreateGateway(client);

            IAssetHandle<TestAsset> handle = gateway.Load<TestAsset>(CreateEnemyBeeReference(), CancellationToken.None);
            Assert.AreEqual(AssetHandleState.Loading, handle.State);
            Assert.IsFalse(handle.IsReady);

            client.LoadGate.SetResult(true);
            handle.WhenReady.GetAwaiter().GetResult();
            Assert.AreEqual(AssetHandleState.Ready, handle.State);
            Assert.IsTrue(handle.IsReady);
            Assert.IsNotNull(handle.Asset);

            handle.Release();
            Assert.AreEqual(1, client.ReleaseCalls.Count);
        }

        [Test]
        public void Load_ReleaseBeforeReady_ReleasesUnderlyingHandleAfterCompletion()
        {
            TestAddressableAssetClient client = CreateClient();
            client.LoadGate = new TaskCompletionSource<bool>();
            AddressablesGateway gateway = CreateGateway(client);

            IAssetHandle<TestAsset> handle = gateway.Load<TestAsset>(CreateEnemyBeeReference(), CancellationToken.None);
            handle.Release();
            Assert.AreEqual(AssetHandleState.Loading, handle.State);

            client.LoadGate.SetResult(true);
            handle.WhenReady.GetAwaiter().GetResult();
            Assert.AreEqual(AssetHandleState.Released, handle.State);
            Assert.AreEqual(1, client.ReleaseCalls.Count);
        }

        [Test]
        public void Gateway_AsAsyncLayerInitializable_InitializesThroughInterface()
        {
            TestAddressableAssetClient client = CreateClient();
            AddressablesGateway gateway = CreateGateway(client);

            ((Madbox.Scope.Contracts.IAsyncLayerInitializable)gateway).InitializeAsync(null, CancellationToken.None).GetAwaiter().GetResult();
            Assert.AreEqual(1, client.SyncCalls);
        }

        [Test]
        public void Gateway_AsAsyncLayerInitializable_WhenCanceled_ThrowsOperationCanceledException()
        {
            TestAddressableAssetClient client = CreateClient();
            AddressablesGateway gateway = CreateGateway(client);
            using CancellationTokenSource cancellationSource = new CancellationTokenSource();
            cancellationSource.Cancel();

            Assert.Throws<OperationCanceledException>(() =>
                ((Madbox.Scope.Contracts.IAsyncLayerInitializable)gateway).InitializeAsync(null, cancellationSource.Token).GetAwaiter().GetResult());
        }

        private static TestAddressableAssetClient CreateClient()
        {
            return new TestAddressableAssetClient();
        }

        private static AddressablesGateway CreateGateway(TestAddressableAssetClient client)
        {
            IAssetReferenceHandler assetReferenceHandler = new AddressablesAssetReferenceHandler(client);
            return new AddressablesGateway(client, assetReferenceHandler);
        }

        private static IAssetHandle<TestAsset> CreateEnemyBeeHandle(AddressablesGateway gateway)
        {
            return gateway.LoadAsync<TestAsset>(CreateEnemyBeeReference(), CancellationToken.None).GetAwaiter().GetResult();
        }

        private static IAssetGroupHandle<TestAsset> CreateEnemyLabelGroupHandle(AddressablesGateway gateway)
        {
            return gateway.LoadAsync<TestAsset>(CreateEnemyLabelReference(), CancellationToken.None).GetAwaiter().GetResult();
        }

        private static void BuildGatewayInitialization(AddressablesGateway gateway)
        {
            gateway.InitializeAsync(CancellationToken.None).GetAwaiter().GetResult();
        }

        private static AssetReference CreateEnemyBeeReference()
        {
            return new AssetReference("enemy/bee");
        }

        private static AssetLabelReference CreateEnemyLabelReference()
        {
            return new AssetLabelReference { labelString = "enemy" };
        }

        private static void BuildEnemyCatalog(TestAddressableAssetClient client)
        {
            client.CatalogToKeys["enemy"] = new[] { "enemy/bee", "enemy/slime" };
        }

        private static void BuildReleaseCountAssertion(TestAddressableAssetClient client, int count)
        {
            Assert.AreEqual(count, client.ReleaseCalls.Count);
        }

        private static void BuildReleaseLifecycleAssertion(TestAddressableAssetClient client, IAssetHandle<TestAsset> first, IAssetHandle<TestAsset> second)
        {
            Assert.AreEqual(1, client.CountLoadCallsForType(typeof(TestAsset)));
            first.Release();
            BuildReleaseCountAssertion(client, 0);
            second.Release();
            BuildReleaseCountAssertion(client, 1);
        }

        private class TestAddressableAssetClient : IAddressablesAssetClient
        {
            public int SyncCalls { get; private set; }
            public bool ThrowOnSync { get; set; }
            public TaskCompletionSource<bool> LoadGate { get; set; }
            public readonly List<string> LoadCalls = new List<string>();
            public readonly List<string> ReleaseCalls = new List<string>();
            public readonly Dictionary<string, IReadOnlyList<string>> CatalogToKeys = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);
            public readonly Dictionary<string, UnityEngine.Object> ObjectAssets = new Dictionary<string, UnityEngine.Object>(StringComparer.Ordinal);
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

            public async Task<UnityEngine.Object> LoadAssetAsync(string key, Type assetType, CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                LoadCalls.Add($"{assetType.FullName}|{key}");
                if (LoadGate != null)
                {
                    await LoadGate.Task;
                }

                if (ObjectAssets.TryGetValue(key, out UnityEngine.Object existing))
                {
                    return existing;
                }

                if (cache.TryGetValue(key, out TestAsset cachedAsset))
                {
                    return cachedAsset;
                }

                TestAsset created = ScriptableObject.CreateInstance<TestAsset>();
                created.AssetId = key;
                cache[key] = created;
                return created;
            }

            public int CountLoadCallsForType(Type type)
            {
                string prefix = $"{type.FullName}|";
                int count = 0;
                for (int i = 0; i < LoadCalls.Count; i++)
                {
                    if (LoadCalls[i].StartsWith(prefix, StringComparison.Ordinal))
                    {
                        count++;
                    }
                }

                return count;
            }

            public void Release(UnityEngine.Object asset)
            {
                if (asset is TestAsset testAsset)
                {
                    ReleaseCalls.Add(testAsset.AssetId);
                }
            }

            public Task<IReadOnlyList<string>> ResolveLabelAsync(Type assetType, AssetLabelReference label, CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (CatalogToKeys.TryGetValue(label.labelString, out IReadOnlyList<string> keys))
                {
                    return Task.FromResult(keys);
                }

                return Task.FromResult((IReadOnlyList<string>)Array.Empty<string>());
            }
        }

        private class TestAsset : ScriptableObject
        {
            public string AssetId;
        }
    }
}
#pragma warning restore SCA0006
#pragma warning restore SCA0005
#pragma warning restore SCA0003
