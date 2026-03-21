using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Madbox.Addressables.Contracts;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AddressableAssets;
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
        private const int sceneLoadTimeoutFrames = 600;
        private const int gatewayLoadTimeoutFrames = 600;
        private const int bootstrapScopeTimeoutFrames = 240;
        private const int bootstrapCompletionTimeoutFrames = 240;
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
            int frame = 0;
            while (!operation.isDone)
            {
                if (frame++ >= sceneLoadTimeoutFrames)
                {
                    Assert.Fail($"Bootstrap scene load did not complete within {sceneLoadTimeoutFrames} frames.");
                }
                yield return null;
            }
        }

        private IEnumerator WaitForBootstrapInitialization()
        {
            yield return WaitForBootstrapScope(bootstrapScopeTimeoutFrames);
            yield return WaitForBootstrapCompletion(bootstrapCompletionTimeoutFrames);
        }

        private IEnumerator WaitForBootstrapScope(int maxFrames)
        {
            int frame = 0;
            MonoBehaviour scope = FindBootstrapScope();
            while (scope == null)
            {
                if (frame++ >= maxFrames)
                {
                    Assert.Fail($"Bootstrap scope '{bootstrapScopeTypeName}' not found within {maxFrames} frames.");
                }

                yield return null;
                scope = FindBootstrapScope();
            }
        }

        private IEnumerator WaitForBootstrapCompletion(int maxFrames)
        {
            int frame = 0;
            while (true)
            {
                MonoBehaviour scope = FindBootstrapScope();
                if (IsBootstrapCompleted(scope))
                {
                    yield break;
                }

                if (frame++ >= maxFrames)
                {
                    Assert.Fail($"Bootstrap completion flag '{completionPropertyName}' did not become true within {maxFrames} frames.");
                }

                yield return null;
            }
        }

        private MonoBehaviour FindBootstrapScope()
        {
            MonoBehaviour[] behaviours = Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (IsBootstrapScope(behaviours[i]))
                {
                    return behaviours[i];
                }
            }
            return null;
        }

        private bool IsBootstrapScope(MonoBehaviour behaviour)
        {
            if (behaviour == null)
            {
                return false;
            }
            return behaviour.GetType().FullName == bootstrapScopeTypeName;
        }

        private bool IsBootstrapCompleted(MonoBehaviour scope)
        {
            if (scope == null)
            {
                return false;
            }
            var property = scope.GetType().GetProperty(completionPropertyName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
            if (property == null)
            {
                return false;
            }
            object value = property.GetValue(scope, null);
            return value is bool completed && completed;
        }

        private IAddressablesGateway ResolveGateway()
        {
            LifetimeScope[] scopes = Object.FindObjectsByType<LifetimeScope>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < scopes.Length; i++)
            {
                if (TryResolveGateway(scopes[i], out IAddressablesGateway gateway))
                {
                    return gateway;
                }
            }
            Assert.Fail("Failed to resolve IAddressablesGateway from active LifetimeScopes.");
            return null;
        }

        private bool TryResolveGateway(LifetimeScope scope, out IAddressablesGateway gateway)
        {
            gateway = null;
            if (scope == null || scope.Container == null)
            {
                return false;
            }

            try
            {
                gateway = scope.Container.Resolve<IAddressablesGateway>();
                return gateway != null;
            }
            catch (VContainerException)
            {
                return false;
            }
        }

        private IEnumerator LoadLongSword(IAddressablesGateway gateway, System.Action<IAssetHandle<GameObject>> onLoaded)
        {
            AssetReference reference = new AssetReference("LongSword");
            var task = gateway.LoadAsync<GameObject>(reference, CancellationToken.None);
            int frame = 0;
            while (!task.IsCompleted)
            {
                if (frame++ >= gatewayLoadTimeoutFrames)
                {
                    Assert.Fail($"Addressable load for '{reference.AssetGUID}' did not complete within {gatewayLoadTimeoutFrames} frames.");
                }
                yield return null;
            }
            Assert.IsFalse(task.IsFaulted, task.Exception?.ToString());
            Assert.IsFalse(task.IsCanceled, "Addressable load task was canceled unexpectedly.");
            onLoaded(task.GetAwaiter().GetResult());
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
            if (type != LogType.Assert && type != LogType.Error && type != LogType.Exception)
            {
                return;
            }

            fatalLogs.Add($"{type}: {condition}\n{stackTrace}");
        }

        private void AssertNoFatalLogs()
        {
            if (fatalLogs == null || fatalLogs.Count == 0)
            {
                return;
            }
            string message = string.Join("\n", fatalLogs);
            Assert.Fail(message);
        }
    }
}


