using System;
using System.Reflection;
using Madbox.Scope;
using NUnit.Framework;
using UnityEngine;

namespace Madbox.App.Bootstrap.Tests
{
    public sealed class BootstrapScopeValidationTests
    {

        [Test]
        public void BuildLayerTree_ReturnsLayerTree_WhenViewHolderMissing()
        {
            using ScopeHarness harness = CreateScopeHarness();
            ConfigureScope(harness.Scope, includeViewHolder: false);
            LayerInstallerBase root = InvokeBuildLayerTree(harness.Scope);
            Assert.IsNotNull(root);
            Assert.AreEqual(1, root.Children.Count);
            Assert.AreEqual("BootstrapInfraInstaller", root.Children[0].GetType().Name);
        }

        [Test]
        public void BuildLayerTree_ReturnsAssetRootWithInfraChild_WhenSerializedFieldsPresent()
        {
            using ScopeHarness harness = CreateScopeHarness();
            ConfigureScope(harness.Scope, includeViewHolder: true);
            LayerInstallerBase root = InvokeBuildLayerTree(harness.Scope);
            Assert.IsNotNull(root);
            Assert.AreEqual(1, root.Children.Count);
            LayerInstallerBase infra = root.Children[0];
            Assert.AreEqual("BootstrapInfraInstaller", infra.GetType().Name);
            Assert.AreEqual(1, infra.Children.Count);
            LayerInstallerBase core = infra.Children[0];
            Assert.AreEqual("BootstrapCoreInstaller", core.GetType().Name);
            Assert.AreEqual(1, core.Children.Count);
            LayerInstallerBase meta = core.Children[0];
            Assert.AreEqual("BootstrapMetaInstaller", meta.GetType().Name);
        }

        private static ScopeHarness CreateScopeHarness()
        {
            GameObject root = new GameObject("BootstrapScopeValidationViewHolder");
            Component scope = AddBootstrapScope(root);
            return new ScopeHarness(root, scope);
        }

        private static Component AddBootstrapScope(GameObject root)
        {
            Type type = ResolveBootstrapScopeType();
            Component scope = root.AddComponent(type);
            Assert.IsNotNull(scope);
            return scope;
        }

        private static Type ResolveBootstrapScopeType()
        {
            Type type = Type.GetType("Madbox.App.Bootstrap.BootstrapScope, Madbox.Bootstrap.Runtime");
            Assert.IsNotNull(type);
            return type;
        }

        private static LayerInstallerBase InvokeBuildLayerTree(Component scope)
        {
            BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            MethodInfo method = scope.GetType().GetMethod("BuildLayerTree", flags);
            Assert.IsNotNull(method);
            object result = method.Invoke(scope, null);
            return result as LayerInstallerBase;
        }

        private static void ConfigureScope(Component scope, bool includeViewHolder)
        {
            SetPrivateField(scope, "viewHolder", includeViewHolder ? CreateViewHolderTransform(scope) : null);
        }

        private static Transform CreateViewHolderTransform(Component scope)
        {
            GameObject holder = new GameObject("BootstrapScopeValidationViewHolder");
            holder.transform.SetParent(scope.transform);
            return holder.transform;
        }

        private static void SetPrivateField(Component scope, string fieldName, object value)
        {
            BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            FieldInfo field = scope.GetType().GetField(fieldName, flags);
            Assert.IsNotNull(field, $"Expected private field '{fieldName}' on {scope.GetType().Name}.");
            field.SetValue(scope, value);
        }

        private sealed class ScopeHarness : IDisposable
        {
            public ScopeHarness(GameObject root, Component scope)
            {
                this.root = root;
                Scope = scope;
            }

            public Component Scope { get; }

            private readonly GameObject root;

            public void Dispose()
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }
    }
}
