using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Madbox.Addressables.Contracts;
using Madbox.Scope;
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
        private const string longSwordPrefabGuid = "bac79aa2a0057e5429a664d3f336da3d";
        private const int sceneLoadTimeoutFrames = 900;
        private const int gatewayLoadTimeoutFrames = 1800;
        private const int bootstrapCompleteTimeoutFrames = 2400;
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
            yield return WaitForBootstrapCompleted();
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

            for (int frame = 0; frame < sceneLoadTimeoutFrames && !operation.isDone; frame++)
            {
                yield return null;
            }

            Assert.IsTrue(operation.isDone, $"Bootstrap scene load did not complete within {sceneLoadTimeoutFrames} frames.");
        }

        private IEnumerator WaitForBootstrapCompleted()
        {
            LayeredScope scope = null;
            for (int frame = 0; frame < bootstrapCompleteTimeoutFrames; frame++)
            {
                scope = FindLayeredScopeInBootstrapScene();
                if (scope != null && scope.IsBootstrapCompleted)
                {
                    break;
                }

                yield return null;
            }

            Assert.IsNotNull(scope, "Expected Bootstrap scene to contain a LayeredScope.");
            Assert.IsTrue(scope.IsBootstrapCompleted, "Expected bootstrap initialization to complete.");
        }

        private static LayeredScope FindLayeredScopeInBootstrapScene()
        {
            Scene bootstrapScene = SceneManager.GetSceneByName("Bootstrap");
            if (!bootstrapScene.IsValid() || !bootstrapScene.isLoaded)
            {
                return null;
            }

            foreach (GameObject root in bootstrapScene.GetRootGameObjects())
            {
                LayeredScope layered = root.GetComponentInChildren<LayeredScope>(true);
                if (layered != null)
                {
                    return layered;
                }
            }

            return null;
        }

        private IAddressablesGateway ResolveGateway()
        {
            LayeredScope layeredScope = FindLayeredScopeInBootstrapScene();
            Assert.IsNotNull(layeredScope, "Expected Bootstrap scene LayeredScope before gateway resolution.");

            if (TryResolveGatewayFromScopes(layeredScope.GetComponentsInChildren<LifetimeScope>(true), out IAddressablesGateway fromLayered))
            {
                return fromLayered;
            }

            LifetimeScope[] allScopes = Object.FindObjectsByType<LifetimeScope>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (TryResolveGatewayFromScopes(allScopes, out IAddressablesGateway fromScene))
            {
                return fromScene;
            }

            Assert.Fail("Failed to resolve IAddressablesGateway from active LifetimeScopes.");
            return null;
        }

        private static bool TryResolveGatewayFromScopes(LifetimeScope[] scopes, out IAddressablesGateway gateway)
        {
            gateway = null;
            if (scopes == null || scopes.Length == 0)
            {
                return false;
            }

            SortLifetimeScopesByDepthDescending(scopes);
            for (int i = 0; i < scopes.Length; i++)
            {
                if (TryResolveGateway(scopes[i], out gateway))
                {
                    return true;
                }
            }

            return false;
        }

        private static void SortLifetimeScopesByDepthDescending(LifetimeScope[] scopes)
        {
            System.Array.Sort(scopes, (a, b) => GetTransformDepth(b.transform).CompareTo(GetTransformDepth(a.transform)));
        }

        private static int GetTransformDepth(Transform transform)
        {
            int depth = 0;
            Transform current = transform;
            while (current != null)
            {
                depth++;
                current = current.parent;
            }

            return depth;
        }

        private static bool TryResolveGateway(LifetimeScope scope, out IAddressablesGateway gateway)
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
            AssetReference reference = new AssetReference(longSwordPrefabGuid);
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


