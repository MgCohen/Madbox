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

namespace Madbox.Addressables.Tests
{
    public sealed class AddressablesAssetPreloadHandlerTests
    {
        [Test]
        public void BuildAsync_AssetReferenceEntry_BuildsSingleRegistration()
        {
            TestAddressableAssetClient client = new TestAddressableAssetClient();
            IAssetPreloadHandler handler = new AddressablesAssetPreloadHandler(client);
            AddressablesPreloadConfig config = CreateConfig(new[] { CreateAssetEntry(typeof(TestAsset), "enemy/bee", PreloadMode.Normal) });
            IReadOnlyList<AddressablesPreloadRegistration> registrations = handler.BuildAsync(config, CancellationToken.None).GetAwaiter().GetResult();
            Assert.AreEqual(1, registrations.Count);
            Assert.AreEqual(typeof(TestAsset), registrations[0].AssetType);
            Assert.AreEqual("enemy/bee", registrations[0].Key);
            Assert.AreEqual(PreloadMode.Normal, registrations[0].Mode);
        }

        [Test]
        public void BuildAsync_LabelReferenceEntry_BuildsOneRegistrationPerResolvedKey()
        {
            IReadOnlyList<AddressablesPreloadRegistration> registrations = BuildByEnemyLabel();
            AssertLabelRegistrations(registrations);
        }

        [Test]
        public void BuildAsync_InvalidEntry_ThrowsInvalidOperationException()
        {
            TestAddressableAssetClient client = new TestAddressableAssetClient();
            IAssetPreloadHandler handler = new AddressablesAssetPreloadHandler(client);
            AddressablesPreloadConfig config = CreateConfig(new[] { CreateAssetEntry(typeof(string), "enemy/bee", PreloadMode.Normal) });
            Assert.Throws<InvalidOperationException>(() => handler.BuildAsync(config, CancellationToken.None).GetAwaiter().GetResult());
        }

        private AddressablesPreloadConfig CreateConfig(IReadOnlyList<AddressablesPreloadConfigEntry> entries)
        {
            AddressablesPreloadConfig config = ScriptableObject.CreateInstance<AddressablesPreloadConfig>();
            SetField(config, "entries", new List<AddressablesPreloadConfigEntry>(entries));
            return config;
        }

        private IReadOnlyList<AddressablesPreloadRegistration> BuildByEnemyLabel()
        {
            TestAddressableAssetClient client = new TestAddressableAssetClient();
            client.CatalogToKeys["enemy"] = new[] { "enemy/bee", "enemy/slime" };
            IAssetPreloadHandler handler = new AddressablesAssetPreloadHandler(client);
            AddressablesPreloadConfig config = CreateConfig(new[] { CreateLabelEntry(typeof(TestAsset), "enemy", PreloadMode.NeverDie) });
            return handler.BuildAsync(config, CancellationToken.None).GetAwaiter().GetResult();
        }

        private void AssertLabelRegistrations(IReadOnlyList<AddressablesPreloadRegistration> registrations)
        {
            Assert.AreEqual(2, registrations.Count);
            Assert.AreEqual("enemy/bee", registrations[0].Key);
            Assert.AreEqual("enemy/slime", registrations[1].Key);
            Assert.AreEqual(PreloadMode.NeverDie, registrations[0].Mode);
            Assert.AreEqual(PreloadMode.NeverDie, registrations[1].Mode);
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

        private void SetField(object target, string name, object value)
        {
            FieldInfo field = target.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
            if (field == null) { throw new InvalidOperationException($"Field '{name}' not found on '{target.GetType().FullName}'."); }
            field.SetValue(target, value);
        }

        private sealed class TestAddressableAssetClient : IAddressablesAssetClient
        {
            public readonly Dictionary<string, IReadOnlyList<string>> CatalogToKeys = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);

            public Task SyncCatalogAndContentAsync(CancellationToken cancellationToken)
            {
                return Task.CompletedTask;
            }

            public Task<UnityEngine.Object> LoadAssetAsync(string key, Type assetType, CancellationToken cancellationToken)
            {
                return Task.FromResult((UnityEngine.Object)null);
            }

            public Task<IReadOnlyList<string>> ResolveLabelAsync(Type assetType, AssetLabelReference label, CancellationToken cancellationToken)
            {
                if (CatalogToKeys.TryGetValue(label.labelString, out IReadOnlyList<string> keys)) { return Task.FromResult(keys); }
                return Task.FromResult((IReadOnlyList<string>)Array.Empty<string>());
            }

            public void Release(UnityEngine.Object asset)
            {
            }
        }

        private sealed class TestAsset : ScriptableObject
        {
        }
    }
}
#pragma warning restore SCA0003
