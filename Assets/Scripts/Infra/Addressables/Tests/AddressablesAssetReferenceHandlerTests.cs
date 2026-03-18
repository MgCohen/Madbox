using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Madbox.Addressables.Contracts;
using NUnit.Framework;
using UnityEngine;

namespace Madbox.Addressables.Tests
{
    public sealed class AddressablesAssetReferenceHandlerTests
    {
        [Test]
        public void AcquireAsync_SameKeyTwice_LoadsOnceAndReleasesOnce()
        {
            TestAddressableAssetClient client = new TestAddressableAssetClient();
            IAssetReferenceHandler handler = new AddressablesAssetReferenceHandler(client);
            IAssetHandle<TestAsset> first = AcquireBee(handler);
            IAssetHandle<TestAsset> second = AcquireBee(handler);
            AssertReleaseLifecycle(client, first, second);
        }

        [Test]
        public void AcquireByTypeAsync_NeverDie_WhenReleased_DoesNotReleaseUnderlyingAsset()
        {
            TestAddressableAssetClient client = new TestAddressableAssetClient();
            IAssetReferenceHandler handler = new AddressablesAssetReferenceHandler(client);
            Type assetType = typeof(TestAsset);
            IAssetHandle preloaded = handler.AcquireByTypeAsync(assetType, "enemy/bee", PreloadMode.NeverDie, true, CancellationToken.None).GetAwaiter().GetResult();
            IAssetHandle<TestAsset> consumer = AcquireBee(handler);
            consumer.Release();
            preloaded.Release();
            Assert.AreEqual(0, client.ReleaseCalls.Count);
        }

        private IAssetHandle<TestAsset> AcquireBee(IAssetReferenceHandler handler)
        {
            return handler.AcquireAsync<TestAsset>("enemy/bee", CancellationToken.None).GetAwaiter().GetResult();
        }

        private void AssertReleaseLifecycle(TestAddressableAssetClient client, IAssetHandle<TestAsset> first, IAssetHandle<TestAsset> second)
        {
            first.Release();
            Assert.AreEqual(0, client.ReleaseCalls.Count);
            second.Release();
            Assert.AreEqual(1, client.ReleaseCalls.Count);
            int loadCount = CountTestAssetLoads(client);
            Assert.AreEqual(1, loadCount);
        }

        private int CountTestAssetLoads(TestAddressableAssetClient client)
        {
            Type assetType = typeof(TestAsset);
            return client.CountLoadCallsForType(assetType);
        }

        private sealed class TestAddressableAssetClient : IAddressablesAssetClient
        {
            public readonly List<string> LoadCalls = new List<string>();
            public readonly List<string> ReleaseCalls = new List<string>();
            private readonly Dictionary<string, TestAsset> cache = new Dictionary<string, TestAsset>(StringComparer.Ordinal);

            public Task SyncCatalogAndContentAsync(CancellationToken cancellationToken)
            {
                return Task.CompletedTask;
            }

            public Task<UnityEngine.Object> LoadAssetAsync(string key, Type assetType, CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                LoadCalls.Add($"{assetType.FullName}|{key}");
                if (cache.TryGetValue(key, out TestAsset existing)) { return Task.FromResult((UnityEngine.Object)existing); }
                TestAsset created = ScriptableObject.CreateInstance<TestAsset>();
                created.AssetId = key;
                cache[key] = created;
                return Task.FromResult((UnityEngine.Object)created);
            }

            public Task<IReadOnlyList<string>> ResolveLabelAsync(Type assetType, UnityEngine.AddressableAssets.AssetLabelReference label, CancellationToken cancellationToken)
            {
                return Task.FromResult((IReadOnlyList<string>)Array.Empty<string>());
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
        }

        private sealed class TestAsset : ScriptableObject
        {
            public string AssetId;
        }
    }
}
