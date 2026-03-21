using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace Madbox.App.Bootstrap.Tests
{
    public sealed class BootstrapScopeValidationTests
    {
        [Test]
        public void BuildLayerInstallers_Throws_WhenNavigationSettingsMissing()
        {
            using ScopeHarness harness = CreateScopeHarness();
            BuildConfigureScope(harness.Scope, includeNavigationSettings: false, includeViewHolder: true);
            Exception exception = BuildCaptureBuildLayerInstallersException(harness.Scope);
            Assert.IsNotNull(exception);
            Assert.IsInstanceOf<ArgumentNullException>(exception);
            Assert.AreEqual("navigationSettings", ((ArgumentNullException)exception).ParamName);
        }

        [Test]
        public void BuildLayerInstallers_Throws_WhenViewHolderMissing()
        {
            using ScopeHarness harness = CreateScopeHarness();
            BuildConfigureScope(harness.Scope, includeNavigationSettings: true, includeViewHolder: false);
            Exception exception = BuildCaptureBuildLayerInstallersException(harness.Scope);
            Assert.IsNotNull(exception);
            Assert.IsInstanceOf<ArgumentNullException>(exception);
            Assert.AreEqual("viewHolder", ((ArgumentNullException)exception).ParamName);
        }

        [Test]
        public void BuildLayerInstallers_ReturnsAssetAndInfraInstallers_WhenSerializedFieldsPresent()
        {
            using ScopeHarness harness = CreateScopeHarness();
            BuildConfigureScope(harness.Scope, includeNavigationSettings: true, includeViewHolder: true);
            object result = BuildInvokeBuildLayerInstallers(harness.Scope);
            Assert.IsNotNull(result);
            var installers = result as System.Collections.ICollection;
            Assert.IsNotNull(installers);
            Assert.AreEqual(2, installers.Count);
        }

        private static ScopeHarness CreateScopeHarness()
        {
            GameObject root = new GameObject(nameof(BuildLayerInstallers_Throws_WhenNavigationSettingsMissing));
            Component scope = BuildAddBootstrapScope(root);
            return new ScopeHarness(root, scope);
        }

        private static Component BuildAddBootstrapScope(GameObject root)
        {
            Type type = BuildResolveBootstrapScopeType();
            Component scope = root.AddComponent(type);
            Assert.IsNotNull(scope);
            return scope;
        }

        private static Type BuildResolveBootstrapScopeType()
        {
            Type type = Type.GetType("Madbox.App.Bootstrap.BootstrapScope, Madbox.Bootstrap.Runtime");
            Assert.IsNotNull(type);
            return type;
        }

        private static Exception BuildCaptureBuildLayerInstallersException(Component scope)
        {
            return BuildInvokeBuildLayerInstallersSafely(scope);
        }

        private static Exception BuildInvokeBuildLayerInstallersSafely(Component scope)
        {
            try { BuildInvokeBuildLayerInstallers(scope); return null; }
            catch (TargetInvocationException exception) { return exception.InnerException; }
        }

        private static object BuildInvokeBuildLayerInstallers(Component scope)
        {
            BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            MethodInfo method = scope.GetType().GetMethod("BuildLayerInstallers", flags);
            Assert.IsNotNull(method);
            return method.Invoke(scope, null);
        }

        private static void BuildConfigureScope(Component scope, bool includeNavigationSettings, bool includeViewHolder)
        {
            BuildSetPrivateField(scope, "navigationSettings", includeNavigationSettings ? CreateNavigationSettingsInstance() : null);
            BuildSetPrivateField(scope, "viewHolder", includeViewHolder ? CreateViewHolderTransform(scope) : null);
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

        private static void BuildSetPrivateField(Component scope, string fieldName, object value)
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


