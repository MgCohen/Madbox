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
        public void BuildLayerTree_Throws_WhenNavigationSettingsMissing()
        {
            using ScopeHarness harness = CreateScopeHarness();
            ConfigureScope(harness.Scope, includeNavigationSettings: false, includeViewHolder: true);
            Exception exception = CaptureBuildLayerTreeException(harness.Scope);
            Assert.IsNotNull(exception);
            Assert.IsInstanceOf<ArgumentNullException>(exception);
            Assert.AreEqual("navigationSettings", ((ArgumentNullException)exception).ParamName);
        }

        [Test]
        public void BuildLayerTree_Throws_WhenViewHolderMissing()
        {
            using ScopeHarness harness = CreateScopeHarness();
            ConfigureScope(harness.Scope, includeNavigationSettings: true, includeViewHolder: false);
            Exception exception = CaptureBuildLayerTreeException(harness.Scope);
            Assert.IsNotNull(exception);
            Assert.IsInstanceOf<ArgumentNullException>(exception);
            Assert.AreEqual("viewHolder", ((ArgumentNullException)exception).ParamName);
        }

        [Test]
        public void BuildLayerTree_ReturnsAssetRootWithInfraChild_WhenSerializedFieldsPresent()
        {
            using ScopeHarness harness = CreateScopeHarness();
            ConfigureScope(harness.Scope, includeNavigationSettings: true, includeViewHolder: true);
            LayerInstallerBase root = InvokeBuildLayerTree(harness.Scope);
            Assert.IsNotNull(root);
            Assert.AreEqual(1, root.Children.Count);
            Assert.AreEqual("BootstrapInfraInstaller", root.Children[0].GetType().Name);
        }

        private static ScopeHarness CreateScopeHarness()
        {
            GameObject root = new GameObject(nameof(BuildLayerTree_Throws_WhenNavigationSettingsMissing));
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

        private static Exception CaptureBuildLayerTreeException(Component scope)
        {
            try
            {
                InvokeBuildLayerTree(scope);
                return null;
            }
            catch (TargetInvocationException exception)
            {
                return exception.InnerException;
            }
        }

        private static LayerInstallerBase InvokeBuildLayerTree(Component scope)
        {
            BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            MethodInfo method = scope.GetType().GetMethod("BuildLayerTree", flags);
            Assert.IsNotNull(method);
            object result = method.Invoke(scope, null);
            return result as LayerInstallerBase;
        }

        private static void ConfigureScope(Component scope, bool includeNavigationSettings, bool includeViewHolder)
        {
            SetPrivateField(scope, "navigationSettings", includeNavigationSettings ? CreateNavigationSettingsInstance() : null);
            SetPrivateField(scope, "viewHolder", includeViewHolder ? CreateViewHolderTransform(scope) : null);
        }

        private static ScriptableObject CreateNavigationSettingsInstance()
        {
            Type type = Type.GetType("Scaffold.Navigation.NavigationSettings, Scaffold.Navigation");
            Assert.IsNotNull(type);
            ScriptableObject instance = ScriptableObject.CreateInstance(type);
            Assert.IsNotNull(instance);
            return instance;
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
