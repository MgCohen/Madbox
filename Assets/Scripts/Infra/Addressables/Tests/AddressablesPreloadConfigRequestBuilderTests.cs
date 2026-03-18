using System;
using System.Collections.Generic;
using System.Reflection;
using Madbox.Addressables.Contracts;
using NUnit.Framework;
using Scaffold.Types;
using UnityEngine;
using UnityEngine.AddressableAssets;
#pragma warning disable SCA0003
#pragma warning disable SCA0006

namespace Madbox.Addressables.Tests
{
    public class AddressablesPreloadConfigRequestBuilderTests
    {
        [Test]
        public void AppendRequests_WithAssetReference_BuildsKeyRequest()
        {
            AddressablesPreloadConfigEntry entry = CreateAssetEntry(typeof(TestAsset), "enemy/bee", PreloadMode.Normal);
            AddressablesPreloadConfig config = CreateConfig(entry);
            AssetKey configKey = new AssetKey("addressables/preload/config");
            List<AddressablesPreloadRequest> requests = new List<AddressablesPreloadRequest>();

            AddressablesPreloadConfigRequestBuilder builder = new AddressablesPreloadConfigRequestBuilder();
            builder.AppendRequests(config, configKey, requests);

            Assert.AreEqual(1, requests.Count);
            Assert.AreEqual(typeof(TestAsset), requests[0].AssetType);
            Assert.AreEqual("enemy/bee", requests[0].Key.Value);
            Assert.AreEqual(PreloadMode.Normal, requests[0].Mode);
            Assert.IsFalse(requests[0].IsCatalog);
        }

        [Test]
        public void AppendRequests_WithLabelReference_BuildsCatalogRequest()
        {
            AddressablesPreloadConfigEntry entry = CreateLabelEntry(typeof(TestAsset), "enemy", PreloadMode.NeverDie);
            AddressablesPreloadConfig config = CreateConfig(entry);
            AssetKey configKey = new AssetKey("addressables/preload/config");
            List<AddressablesPreloadRequest> requests = new List<AddressablesPreloadRequest>();

            AddressablesPreloadConfigRequestBuilder builder = new AddressablesPreloadConfigRequestBuilder();
            builder.AppendRequests(config, configKey, requests);

            Assert.AreEqual(1, requests.Count);
            Assert.AreEqual(typeof(TestAsset), requests[0].AssetType);
            Assert.AreEqual("enemy", requests[0].Label.labelString);
            Assert.AreEqual(PreloadMode.NeverDie, requests[0].Mode);
            Assert.IsTrue(requests[0].IsCatalog);
        }

        [Test]
        public void AppendRequests_WhenAssetTypeIsInvalid_ThrowsInvalidOperationException()
        {
            AddressablesPreloadConfigEntry entry = CreateAssetEntry(typeof(string), "enemy/bee", PreloadMode.Normal);
            AddressablesPreloadConfig config = CreateConfig(entry);
            AssetKey configKey = new AssetKey("addressables/preload/config");
            List<AddressablesPreloadRequest> requests = new List<AddressablesPreloadRequest>();

            AddressablesPreloadConfigRequestBuilder builder = new AddressablesPreloadConfigRequestBuilder();

            Assert.Throws<InvalidOperationException>(() => builder.AppendRequests(config, configKey, requests));
        }

        [Test]
        public void AppendRequests_WhenSelectedReferenceIsMissing_ThrowsInvalidOperationException()
        {
            AddressablesPreloadConfigEntry entry = new AddressablesPreloadConfigEntry();
            SetField(entry, "assetType", new TypeReference(typeof(TestAsset)));
            SetField(entry, "referenceType", PreloadReferenceType.AssetReference);
            SetField(entry, "assetReference", null);
            SetField(entry, "labelReference", new AssetLabelReference { labelString = "enemy" });
            SetField(entry, "mode", PreloadMode.Normal);
            AddressablesPreloadConfig config = CreateConfig(entry);
            AssetKey configKey = new AssetKey("addressables/preload/config");
            List<AddressablesPreloadRequest> requests = new List<AddressablesPreloadRequest>();

            AddressablesPreloadConfigRequestBuilder builder = new AddressablesPreloadConfigRequestBuilder();

            Assert.Throws<InvalidOperationException>(() => builder.AppendRequests(config, configKey, requests));
        }

        private AddressablesPreloadConfig CreateConfig(AddressablesPreloadConfigEntry entry)
        {
            AddressablesPreloadConfig config = ScriptableObject.CreateInstance<AddressablesPreloadConfig>();
            SetField(config, "entries", new List<AddressablesPreloadConfigEntry> { entry });
            return config;
        }

        private AddressablesPreloadConfigEntry CreateAssetEntry(Type type, string key, PreloadMode mode)
        {
            AddressablesPreloadConfigEntry entry = new AddressablesPreloadConfigEntry();
            SetField(entry, "assetType", new TypeReference(type));
            SetField(entry, "referenceType", PreloadReferenceType.AssetReference);
            SetField(entry, "assetReference", new AssetReference(key));
            SetField(entry, "labelReference", new AssetLabelReference());
            SetField(entry, "mode", mode);
            return entry;
        }

        private AddressablesPreloadConfigEntry CreateLabelEntry(Type type, string label, PreloadMode mode)
        {
            AddressablesPreloadConfigEntry entry = new AddressablesPreloadConfigEntry();
            SetField(entry, "assetType", new TypeReference(type));
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

        private class TestAsset : ScriptableObject
        {
        }
    }
}
#pragma warning restore SCA0006
#pragma warning restore SCA0003
