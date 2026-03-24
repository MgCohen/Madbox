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
            IAssetHandle<TestAsset> first = BuildAcquireBee(handler);
            IAssetHandle<TestAsset> second = BuildAcquireBee(handler);
            BuildAssertReleaseLifecycle(client, first, second);
        }

        [Test]
        public void AcquireAsync_NeverDie_WhenReleased_DoesNotReleaseUnderlyingAsset()
        {
            TestAddressableAssetClient client = new TestAddressableAssetClient();
            IAssetReferenceHandler handler = new AddressablesAssetReferenceHandler(client);
            IAssetHandle<TestAsset> preloaded = handler.AcquireAsync<TestAsset>("enemy/bee", PreloadMode.NeverDie, true, CancellationToken.None).GetAwaiter().GetResult();
            IAssetHandle<TestAsset> consumer = BuildAcquireBee(handler);
            consumer.Release();
            preloaded.Release();
            Assert.AreEqual(0, client.ReleaseCalls.Count);
        }

        private static IAssetHandle<TestAsset> BuildAcquireBee(IAssetReferenceHandler handler)
        {
            return handler.AcquireAsync<TestAsset>("enemy/bee", CancellationToken.None).GetAwaiter().GetResult();
        }

        private static void BuildAssertReleaseLifecycle(TestAddressableAssetClient client, IAssetHandle<TestAsset> first, IAssetHandle<TestAsset> second)
        {
            first.Release();
            Assert.AreEqual(0, client.ReleaseCalls.Count);
            second.Release();
            Assert.AreEqual(1, client.ReleaseCalls.Count);
            int loadCount = BuildCountTestAssetLoads(client);
            Assert.AreEqual(1, loadCount);
        }

        private static int BuildCountTestAssetLoads(TestAddressableAssetClient client)
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

            public Task<T> LoadAssetAsync<T>(string key, CancellationToken cancellationToken) where T : UnityEngine.Object
            {
                cancellationToken.ThrowIfCancellationRequested();
                LoadCalls.Add($"{typeof(T).FullName}|{key}");
                if (cache.TryGetValue(key, out TestAsset existing))
                {
                    return Task.FromResult((T)(UnityEngine.Object)existing);
                }
                TestAsset created = ScriptableObject.CreateInstance<TestAsset>();
                created.AssetId = key;
                cache[key] = created;
                return Task.FromResult((T)(UnityEngine.Object)created);
            }

            public Task<IReadOnlyList<T>> LoadAssetsByLabelAsync<T>(UnityEngine.AddressableAssets.AssetLabelReference label, CancellationToken cancellationToken) where T : UnityEngine.Object
            {
                return Task.FromResult((IReadOnlyList<T>)Array.Empty<T>());
            }

            public Task<IReadOnlyList<string>> ResolveLabelAsync<T>(UnityEngine.AddressableAssets.AssetLabelReference label, CancellationToken cancellationToken) where T : UnityEngine.Object
            {
                return Task.FromResult((IReadOnlyList<string>)Array.Empty<string>());
            }

            public void Release(UnityEngine.Object asset)
            {
                if (asset is TestAsset testAsset)
                {
                    ReleaseCalls.Add(testAsset.AssetId);
                }
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
        }

        private sealed class TestAsset : ScriptableObject
        {
            public string AssetId;
        }
    }
}


