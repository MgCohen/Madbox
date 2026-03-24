using System.Collections;
using Madbox.App.Bootstrap;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Madbox.Bootstrap.Tests.PlayMode
{
    public sealed class BootstrapScenePlayModeTests
    {
        private const string BootstrapSceneName = "Bootstrap";
        private const float CompletionTimeoutSeconds = 120f;

        [UnityTest]
        public IEnumerator BootstrapScene_LoadsAndBootstrapScopeCompletes()
        {
            AsyncOperation loadOperation = SceneManager.LoadSceneAsync(BootstrapSceneName, LoadSceneMode.Single);
            Assert.IsNotNull(loadOperation, "Failed to start loading bootstrap scene (is it in Build Settings?).");
            yield return loadOperation;

            float deadline = Time.realtimeSinceStartup + CompletionTimeoutSeconds;
            BootstrapScope scope = null;

            while (Time.realtimeSinceStartup < deadline)
            {
                scope ??= Object.FindObjectOfType<BootstrapScope>();
                if (scope != null && scope.IsBootstrapCompleted)
                {
                    yield break;
                }

                yield return null;
            }

            Assert.IsNotNull(scope, "BootstrapScope was not found in the bootstrap scene.");
            Assert.IsTrue(
                scope.IsBootstrapCompleted,
                $"BootstrapScope did not report completion within {CompletionTimeoutSeconds} seconds.");
        }
    }
}
