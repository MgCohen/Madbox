using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Madbox.Addressables.Contracts;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using VContainer;
using VContainer.Unity;

namespace Madbox.Addressables.Tests.PlayMode
{
    public sealed class AddressablesBootstrapPlayModeTests
    {
        private const string bootstrapScopeTypeName = "Madbox.App.Bootstrap.BootstrapScope";
        private const string completionPropertyName = "IsBootstrapCompleted";
        private List<string> fatalLogs;

        [SetUp]
        public void SetUp()
        {
            fatalLogs = new List<string>();
            Application.logMessageReceived += OnLogMessageReceived;
        }

        [TearDown]
        public void TearDown()
        {
            Application.logMessageReceived -= OnLogMessageReceived;
            fatalLogs = null;
        }

        [UnityTest]
        public IEnumerator BootstrapScene_ResolvesGateway_LoadsAndReleasesAddressable()
        {
            yield return LoadBootstrapScene();
            yield return WaitForBootstrapInitialization();
            IAddressablesGateway gateway = ResolveGateway();
            IAssetHandle<GameObject> handle = null;
            yield return LoadLongSword(gateway, value => handle = value);
            AssertHandleLifecycle(handle);
            AssertNoFatalLogs();
        }

        private IEnumerator LoadBootstrapScene()
        {
            AsyncOperation operation = SceneManager.LoadSceneAsync("Bootstrap", LoadSceneMode.Single);
            Assert.IsNotNull(operation);
            while (!operation.isDone) { yield return null; }
        }

        private IEnumerator WaitForBootstrapInitialization()
        {
            const int maxFrames = 240;
            yield return WaitForBootstrapScope(maxFrames);
            yield return WaitForBootstrapCompletion(maxFrames);
        }

        private IEnumerator WaitForBootstrapScope(int maxFrames)
        {
            int frame = 0;
            MonoBehaviour scope = FindBootstrapScope();
            while (scope == null && frame < maxFrames) { frame++; yield return null; scope = FindBootstrapScope(); }
        }

        private IEnumerator WaitForBootstrapCompletion(int maxFrames)
        {
            int frame = 0;
            while (ShouldWaitForBootstrapCompletion(frame, maxFrames)) { frame++; yield return null; }
        }

        private bool ShouldWaitForBootstrapCompletion(int frame, int maxFrames)
        {
            if (frame >= maxFrames) { return false; }
            MonoBehaviour scope = FindBootstrapScope();
            return !IsBootstrapCompleted(scope);
        }

        private MonoBehaviour FindBootstrapScope()
        {
            MonoBehaviour[] behaviours = Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < behaviours.Length; i++) { if (IsBootstrapScope(behaviours[i])) { return behaviours[i]; } }
            return null;
        }

        private bool IsBootstrapScope(MonoBehaviour behaviour)
        {
            if (behaviour == null) { return false; }
            return behaviour.GetType().FullName == bootstrapScopeTypeName;
        }

        private bool IsBootstrapCompleted(MonoBehaviour scope)
        {
            if (scope == null) { return false; }
            var property = scope.GetType().GetProperty(completionPropertyName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
            if (property == null) { return false; }
            object value = property.GetValue(scope, null);
            return value is bool completed && completed;
        }

        private IAddressablesGateway ResolveGateway()
        {
            LifetimeScope[] scopes = Object.FindObjectsByType<LifetimeScope>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < scopes.Length; i++) { if (TryResolveGateway(scopes[i], out IAddressablesGateway gateway)) { return gateway; } }
            Assert.Fail("Failed to resolve IAddressablesGateway from active LifetimeScopes.");
            return null;
        }

        private bool TryResolveGateway(LifetimeScope scope, out IAddressablesGateway gateway)
        {
            gateway = null;
            if (scope == null || scope.Container == null) { return false; }
            try { gateway = scope.Container.Resolve<IAddressablesGateway>(); return gateway != null; }
            catch (VContainerException) { return false; }
        }

        private IEnumerator LoadLongSword(IAddressablesGateway gateway, System.Action<IAssetHandle<GameObject>> onLoaded)
        {
            AssetKey key = new AssetKey("LongSword");
            var task = gateway.LoadAsync<GameObject>(key, CancellationToken.None);
            while (!task.IsCompleted) { yield return null; }
            Assert.IsFalse(task.IsFaulted, task.Exception?.ToString());
            onLoaded(task.Result);
        }

        private void AssertHandleLifecycle(IAssetHandle<GameObject> handle)
        {
            Assert.IsNotNull(handle);
            Assert.IsNotNull(handle.Asset);
            Assert.IsNotNull(handle.UntypedAsset);
            Assert.AreEqual(typeof(GameObject), handle.AssetType);
            Assert.IsFalse(handle.IsReleased);
            handle.Release();
            Assert.IsTrue(handle.IsReleased);
            handle.Release();
        }

        private void OnLogMessageReceived(string condition, string stackTrace, LogType type)
        {
            if (type != LogType.Assert && type != LogType.Error && type != LogType.Exception) { return; }
            fatalLogs.Add($"{type}: {condition}");
        }

        private void AssertNoFatalLogs()
        {
            if (fatalLogs == null || fatalLogs.Count == 0) { return; }
            string message = string.Join("\n", fatalLogs);
            Assert.Fail(message);
        }
    }
}
