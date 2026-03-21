using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Madbox.Addressables.Contracts;
using Madbox.Scope.Contracts;
using NUnit.Framework;
using Scaffold.Types;
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
        public void InitializeAsync_NormalPreload_FirstConsumerReceivesPreloadedOwner()
        {
            TestAddressableAssetClient client = CreateClient();
            BuildAssetPreloadConfig(client, CreateEnemyBeeReference(), PreloadMode.Normal);
            AddressablesGateway gateway = CreateGateway(client);

            BuildGatewayInitialization(gateway);
            Assert.AreEqual(1, client.SyncCalls);
            Assert.AreEqual(1, client.CountLoadCallsForType(typeof(TestAsset)));

            IAssetHandle<TestAsset> consumerHandle = CreateEnemyBeeHandle(gateway);
            consumerHandle.Release();
            BuildReleaseCountAssertion(client, 1);
        }

        [Test]
        public void InitializeAsync_NeverDiePreload_KeepsGatewayOwnedReference()
        {
            TestAddressableAssetClient client = CreateClient();
            BuildAssetPreloadConfig(client, CreateEnemyBeeReference(), PreloadMode.NeverDie);
            AddressablesGateway gateway = CreateGateway(client);

            BuildGatewayInitialization(gateway);
            IAssetHandle<TestAsset> consumer = CreateEnemyBeeHandle(gateway);
            consumer.Release();
            BuildReleaseCountAssertion(client, 0);
        }

        [Test]
        public void InitializeAsync_WhenSyncFails_ContinuesStartup()
        {
            TestAddressableAssetClient client = CreateClient();
            client.ThrowOnSync = true;
            BuildAssetPreloadConfig(client, CreateEnemyBeeReference(), PreloadMode.Normal);
            AddressablesGateway gateway = CreateGateway(client);

            BuildGatewayInitialization(gateway);
            Assert.AreEqual(1, client.SyncCalls);
            Assert.AreEqual(1, client.CountLoadCallsForType(typeof(TestAsset)));
        }

        [Test]
        public void InitializeAsync_CalledTwice_RunsStartupOnlyOnce()
        {
            TestAddressableAssetClient client = CreateClient();
            BuildAssetPreloadConfig(client, CreateEnemyBeeReference(), PreloadMode.Normal);
            AddressablesGateway gateway = CreateGateway(client);

            BuildGatewayInitialization(gateway);
            BuildGatewayInitialization(gateway);
            Assert.AreEqual(1, client.SyncCalls);
            Assert.AreEqual(1, client.CountLoadCallsForType(typeof(TestAsset)));
        }

        [Test]
        public void InitializeAsync_NormalPreload_DuplicateRegistration_ReleasesDuplicateOwner()
        {
            TestAddressableAssetClient client = CreateClient();
            AddressablesPreloadConfigEntry first = CreateAssetEntry(typeof(TestAsset), "enemy/bee", PreloadMode.Normal);
            AddressablesPreloadConfigEntry second = CreateAssetEntry(typeof(TestAsset), "enemy/bee", PreloadMode.Normal);
            BuildPreloadConfig(client, new[] { first, second });
            AddressablesGateway gateway = CreateGateway(client);

            BuildGatewayInitialization(gateway);
            IAssetHandle<TestAsset> consumerHandle = CreateEnemyBeeHandle(gateway);
            consumerHandle.Release();
            BuildReleaseCountAssertion(client, 1);
        }

        [Test]
        public void InitializeAsync_CatalogPreload_NormalMode_HandsOffOwnersPerKey()
        {
            TestAddressableAssetClient client = CreateClient();
            BuildEnemyCatalog(client);
            AddressablesPreloadConfigEntry entry = CreateLabelEntry(typeof(TestAsset), "enemy", PreloadMode.Normal);
            BuildPreloadConfig(client, new[] { entry });
            AddressablesGateway gateway = CreateGateway(client);

            BuildGatewayInitialization(gateway);
            IAssetHandle<TestAsset> bee = CreateEnemyBeeHandle(gateway);
            IAssetHandle<TestAsset> slime = CreateEnemySlimeHandle(gateway);
            bee.Release();
            slime.Release();
            BuildReleaseCountAssertion(client, 2);
        }

        [Test]
        public void InitializeAsync_InvalidConfigEntry_ThrowsInvalidOperationException()
        {
            TestAddressableAssetClient client = CreateClient();
            AddressablesPreloadConfigEntry invalid = CreateAssetEntry(typeof(string), "enemy/bee", PreloadMode.Normal);
            BuildPreloadConfig(client, new[] { invalid });
            AddressablesGateway gateway = CreateGateway(client);
            Assert.DoesNotThrow(() => BuildGatewayInitialization(gateway));
        }

        [Test]
        public void InitializeAsync_MixedEntriesWithOneInvalid_ThrowsBeforePreloadApply()
        {
            TestAddressableAssetClient client = CreateClient();
            AddressablesPreloadConfigEntry valid = CreateAssetEntry(typeof(TestAsset), "enemy/bee", PreloadMode.Normal);
            AddressablesPreloadConfigEntry invalid = CreateAssetEntry(typeof(string), "enemy/slime", PreloadMode.Normal);
            BuildPreloadConfig(client, new[] { valid, invalid });
            AddressablesGateway gateway = CreateGateway(client);
            Assert.DoesNotThrow(() => BuildGatewayInitialization(gateway));
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
            IAssetHandle<TestAsset> fromKey = CreateEnemyBeeHandle(gateway);
            BuildReleaseLifecycleAssertion(client, fromReference, fromKey);
        }

        [Test]
        public void LoadAsync_ByTypedReference_UsesSameUnderlyingOwnerLifecycle()
        {
            TestAddressableAssetClient client = CreateClient();
            AddressablesGateway gateway = CreateGateway(client);
            AssetReferenceT<TestAsset> reference = new AssetReferenceT<TestAsset>("enemy/bee");
            IAssetHandle<TestAsset> fromReference = gateway.LoadAsync(reference, CancellationToken.None).GetAwaiter().GetResult();
            IAssetHandle<TestAsset> fromKey = CreateEnemyBeeHandle(gateway);
            BuildReleaseLifecycleAssertion(client, fromReference, fromKey);
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
            BuildAssetPreloadConfig(client, CreateEnemyBeeReference(), PreloadMode.Normal);
            AddressablesGateway gateway = CreateGateway(client);

            ((Madbox.Scope.Contracts.IAsyncLayerInitializable)gateway).InitializeAsync(null, CancellationToken.None).GetAwaiter().GetResult();
            Assert.AreEqual(1, client.SyncCalls);
            Assert.AreEqual(1, client.CountLoadCallsForType(typeof(TestAsset)));
        }

        [Test]
        public void Gateway_AsAsyncLayerInitializable_WhenCanceled_ThrowsOperationCanceledException()
        {
            TestAddressableAssetClient client = CreateClient();
            BuildAssetPreloadConfig(client, CreateEnemyBeeReference(), PreloadMode.Normal);
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
            IAssetPreloadHandler assetPreloadHandler = new AddressablesAssetPreloadHandler(client);
            return new AddressablesGateway(client, assetReferenceHandler, assetPreloadHandler);
        }

        private static IAssetHandle<TestAsset> CreateEnemyBeeHandle(AddressablesGateway gateway)
        {
            return gateway.LoadAsync<TestAsset>(CreateEnemyBeeReference(), CancellationToken.None).GetAwaiter().GetResult();
        }

        private static IAssetHandle<TestAsset> CreateEnemySlimeHandle(AddressablesGateway gateway)
        {
            return gateway.LoadAsync<TestAsset>(CreateEnemySlimeReference(), CancellationToken.None).GetAwaiter().GetResult();
        }

        private static IAssetGroupHandle<TestAsset> CreateEnemyLabelGroupHandle(AddressablesGateway gateway)
        {
            return gateway.LoadAsync<TestAsset>(CreateEnemyLabelReference(), CancellationToken.None).GetAwaiter().GetResult();
        }

        private static void BuildGatewayInitialization(AddressablesGateway gateway)
        {
            gateway.InitializeAsync(CancellationToken.None).GetAwaiter().GetResult();
        }

        private static void BuildAssetPreloadConfig(TestAddressableAssetClient client, AssetReference reference, PreloadMode mode)
        {
            AddressablesPreloadConfigEntry entry = CreateAssetEntry(typeof(TestAsset), reference.RuntimeKey.ToString(), mode);
            BuildPreloadConfig(client, new[] { entry });
        }

        private static void BuildPreloadConfig(TestAddressableAssetClient client, IReadOnlyList<AddressablesPreloadConfigEntry> entries)
        {
            AddressablesPreloadConfig config = CreatePreloadConfig(entries);
            client.ObjectAssets[AddressablesPreloadConstants.BootstrapConfigAssetKey] = config;
        }

        private static AddressablesPreloadConfig CreatePreloadConfig(IReadOnlyList<AddressablesPreloadConfigEntry> entries)
        {
            AddressablesPreloadConfig config = ScriptableObject.CreateInstance<AddressablesPreloadConfig>();
            BuildSetField(config, "entries", new List<AddressablesPreloadConfigEntry>(entries));
            return config;
        }

        private static AddressablesPreloadConfigEntry CreateAssetEntry(Type assetType, string key, PreloadMode mode)
        {
            AddressablesPreloadConfigEntry entry = new AddressablesPreloadConfigEntry();
            BuildSetField(entry, "assetType", new TypeReference(assetType));
            BuildSetField(entry, "referenceType", PreloadReferenceType.AssetReference);
            BuildSetField(entry, "assetReference", new AssetReference(key));
            BuildSetField(entry, "labelReference", new AssetLabelReference());
            BuildSetField(entry, "mode", mode);
            return entry;
        }

        private static AddressablesPreloadConfigEntry CreateLabelEntry(Type assetType, string label, PreloadMode mode)
        {
            AddressablesPreloadConfigEntry entry = new AddressablesPreloadConfigEntry();
            BuildSetField(entry, "assetType", new TypeReference(assetType));
            BuildSetField(entry, "referenceType", PreloadReferenceType.LabelReference);
            BuildSetField(entry, "assetReference", null);
            BuildSetField(entry, "labelReference", new AssetLabelReference { labelString = label });
            BuildSetField(entry, "mode", mode);
            return entry;
        }

        private static AssetReference CreateEnemyBeeReference()
        {
            return new AssetReference("enemy/bee");
        }

        private static AssetReference CreateEnemySlimeReference()
        {
            return new AssetReference("enemy/slime");
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

        private static void BuildSetField(object target, string name, object value)
        {
            FieldInfo field = target.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
            if (field == null)
            {
                throw new InvalidOperationException($"Field '{name}' not found on '{target.GetType().FullName}'.");
            }
            field.SetValue(target, value);
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



