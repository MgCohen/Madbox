using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Madbox.Addressables;
using Madbox.Addressables.Contracts;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AddressableAssets;
#pragma warning disable SCA0003
#pragma warning disable SCA0005
#pragma warning disable SCA0006

namespace Madbox.Addressables.Tests
{
    public class AddressablesGatewayComponentExtensionsTests
    {
        [Test]
        public void LoadComponentAsync_WhenPrefabHasComponent_ReturnsComponentAndReleasesUnderlyingGameObject()
        {
            TestAddressableAssetClient client = CreateClient();
            AddressablesGateway gateway = CreateGateway(client);
            GameObject prefabRoot = new GameObject("BeePrefab");
            prefabRoot.AddComponent<TestMonoBehaviour>();
            client.ObjectAssets["enemy/bee"] = prefabRoot;

            AssetReference reference = new AssetReference("enemy/bee");
            IAssetHandle<TestMonoBehaviour> handle =
                gateway.LoadComponentAsync<TestMonoBehaviour>(reference, CancellationToken.None).GetAwaiter().GetResult();

            Assert.IsNotNull(handle.Asset);
            Assert.AreSame(prefabRoot.GetComponent<TestMonoBehaviour>(), handle.Asset);
            Assert.AreEqual(1, client.CountLoadCallsForType(typeof(GameObject)));

            handle.Release();
            Assert.AreEqual(1, client.ReleaseCalls.Count);
            Assert.Contains(prefabRoot.name, client.ReleaseCalls);

            UnityEngine.Object.DestroyImmediate(prefabRoot);
        }

        [Test]
        public void LoadComponentAsync_WhenComponentMissing_ReleasesHandleAndThrows()
        {
            TestAddressableAssetClient client = CreateClient();
            AddressablesGateway gateway = CreateGateway(client);
            GameObject prefabRoot = new GameObject("BarePrefab");
            client.ObjectAssets["enemy/bee"] = prefabRoot;

            AssetReference reference = new AssetReference("enemy/bee");
            Assert.Throws<InvalidOperationException>(() =>
                gateway.LoadComponentAsync<TestMonoBehaviour>(reference, CancellationToken.None).GetAwaiter().GetResult());

            Assert.AreEqual(1, client.ReleaseCalls.Count);
            UnityEngine.Object.DestroyImmediate(prefabRoot);
        }

        [Test]
        public void LoadComponentGroupAsync_MapsEachGameObjectToComponent()
        {
            TestAddressableAssetClient client = CreateClient();
            BuildEnemyCatalog(client);
            AddressablesGateway gateway = CreateGateway(client);

            GameObject goBee = new GameObject("bee");
            goBee.AddComponent<TestMonoBehaviour>();
            client.ObjectAssets["enemy/bee"] = goBee;
            GameObject goSlime = new GameObject("slime");
            goSlime.AddComponent<TestMonoBehaviour>();
            client.ObjectAssets["enemy/slime"] = goSlime;

            AssetLabelReference label = CreateEnemyLabelReference();
            IAssetGroupHandle<TestMonoBehaviour> group =
                gateway.LoadComponentGroupAsync<TestMonoBehaviour>(label, CancellationToken.None).GetAwaiter().GetResult();
            group.WhenReady.GetAwaiter().GetResult();

            Assert.AreEqual(2, group.Assets.Count);
            Assert.AreSame(goBee.GetComponent<TestMonoBehaviour>(), group.Assets[0]);
            Assert.AreSame(goSlime.GetComponent<TestMonoBehaviour>(), group.Assets[1]);

            group.Release();
            Assert.AreEqual(2, client.ReleaseCalls.Count);

            UnityEngine.Object.DestroyImmediate(goBee);
            UnityEngine.Object.DestroyImmediate(goSlime);
        }

        [Test]
        public void LoadComponentGroupAsync_WhenOnePrefabLacksComponent_ReleasesGroupAndThrows()
        {
            TestAddressableAssetClient client = CreateClient();
            BuildEnemyCatalog(client);
            AddressablesGateway gateway = CreateGateway(client);

            GameObject goBee = new GameObject("bee");
            goBee.AddComponent<TestMonoBehaviour>();
            client.ObjectAssets["enemy/bee"] = goBee;
            GameObject goSlime = new GameObject("slime");
            client.ObjectAssets["enemy/slime"] = goSlime;

            AssetLabelReference label = CreateEnemyLabelReference();
            Assert.Throws<InvalidOperationException>(() =>
                gateway.LoadComponentGroupAsync<TestMonoBehaviour>(label, CancellationToken.None).GetAwaiter().GetResult());

            Assert.AreEqual(2, client.ReleaseCalls.Count);

            UnityEngine.Object.DestroyImmediate(goBee);
            UnityEngine.Object.DestroyImmediate(goSlime);
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

        private static AssetLabelReference CreateEnemyLabelReference()
        {
            return new AssetLabelReference { labelString = "enemy" };
        }

        private static void BuildEnemyCatalog(TestAddressableAssetClient client)
        {
            client.CatalogToKeys["enemy"] = new[] { "enemy/bee", "enemy/slime" };
        }

        private sealed class TestMonoBehaviour : MonoBehaviour
        {
        }

        private sealed class TestAddressableAssetClient : IAddressablesAssetClient
        {
            public TaskCompletionSource<bool> LoadGate { get; set; }
            public TaskCompletionSource<bool> LabelLoadGate { get; set; }
            public readonly List<string> LoadCalls = new List<string>();
            public readonly List<string> LabelLoadCalls = new List<string>();
            public readonly List<string> ReleaseCalls = new List<string>();
            public readonly Dictionary<string, IReadOnlyList<string>> CatalogToKeys = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);
            public readonly Dictionary<string, UnityEngine.Object> ObjectAssets = new Dictionary<string, UnityEngine.Object>(StringComparer.Ordinal);
            private readonly Dictionary<string, TestAsset> cache = new Dictionary<string, TestAsset>(StringComparer.Ordinal);

            public Task SyncCatalogAndContentAsync(CancellationToken cancellationToken)
            {
                return Task.CompletedTask;
            }

            public async Task<T> LoadAssetAsync<T>(string key, CancellationToken cancellationToken) where T : UnityEngine.Object
            {
                cancellationToken.ThrowIfCancellationRequested();
                LoadCalls.Add($"{typeof(T).FullName}|{key}");
                if (LoadGate != null)
                {
                    await LoadGate.Task;
                }

                if (ObjectAssets.TryGetValue(key, out UnityEngine.Object existing))
                {
                    return (T)existing;
                }

                if (cache.TryGetValue(key, out TestAsset cachedAsset))
                {
                    return (T)(UnityEngine.Object)cachedAsset;
                }

                TestAsset created = ScriptableObject.CreateInstance<TestAsset>();
                created.AssetId = key;
                cache[key] = created;
                return (T)(UnityEngine.Object)created;
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

            public async Task<IReadOnlyList<T>> LoadAssetsByLabelAsync<T>(AssetLabelReference label, CancellationToken cancellationToken) where T : UnityEngine.Object
            {
                cancellationToken.ThrowIfCancellationRequested();
                LabelLoadCalls.Add($"{typeof(T).FullName}|{label?.labelString}");
                if (LabelLoadGate != null)
                {
                    await LabelLoadGate.Task;
                }

                if (label == null || !CatalogToKeys.TryGetValue(label.labelString, out IReadOnlyList<string> keys))
                {
                    return Array.Empty<T>();
                }

                List<T> assets = new List<T>(keys.Count);
                for (int i = 0; i < keys.Count; i++)
                {
                    string key = keys[i];
                    if (ObjectAssets.TryGetValue(key, out UnityEngine.Object existing))
                    {
                        assets.Add((T)existing);
                        continue;
                    }

                    if (!cache.TryGetValue(key, out TestAsset cachedAsset))
                    {
                        cachedAsset = ScriptableObject.CreateInstance<TestAsset>();
                        cachedAsset.AssetId = key;
                        cache[key] = cachedAsset;
                    }

                    assets.Add((T)(UnityEngine.Object)cachedAsset);
                }

                return assets;
            }

            public void Release(UnityEngine.Object asset)
            {
                if (asset is TestAsset testAsset)
                {
                    ReleaseCalls.Add(testAsset.AssetId);
                    return;
                }

                if (asset is GameObject gameObject)
                {
                    ReleaseCalls.Add(gameObject.name);
                }
            }

            public Task<IReadOnlyList<string>> ResolveLabelAsync<T>(AssetLabelReference label, CancellationToken cancellationToken) where T : UnityEngine.Object
            {
                if (CatalogToKeys.TryGetValue(label.labelString, out IReadOnlyList<string> keys))
                {
                    return Task.FromResult(keys);
                }

                return Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());
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
