using System.Collections;
using System.Collections.Generic;
using Madbox.Scope;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Madbox.Bootstrap.Tests.PlayMode
{
    public sealed class BootstrapScenePlayModeTests
    {
        private const int sceneLoadTimeoutFrames = 900;
        private const int bootstrapCompleteTimeoutFrames = 1200;
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
        public IEnumerator BootstrapScene_Loads_AndCompletesBootstrapInitialization()
        {
            yield return LoadBootstrapScene();

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

        private static LayeredScope FindLayeredScopeInBootstrapScene()
        {
            Scene bootstrapScene = SceneManager.GetSceneByName("Bootstrap");
            if (!bootstrapScene.IsValid() || !bootstrapScene.isLoaded)
            {
                return null;
            }

            foreach (GameObject root in bootstrapScene.GetRootGameObjects())
            {
                LayeredScope scope = root.GetComponentInChildren<LayeredScope>(true);
                if (scope != null)
                {
                    return scope;
                }
            }

            return null;
        }

        private void OnLogMessageReceived(string condition, string stackTrace, LogType type)
        {
            if (!IsFatal(type))
            {
                return;
            }
            fatalLogs.Add($"{type}: {condition}\n{stackTrace}");
        }

        private static bool IsFatal(LogType type)
        {
            return type == LogType.Assert || type == LogType.Error || type == LogType.Exception;
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
