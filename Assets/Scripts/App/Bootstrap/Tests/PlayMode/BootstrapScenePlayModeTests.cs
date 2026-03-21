using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using System.Collections.Generic;

namespace Madbox.Bootstrap.Tests.PlayMode
{
    public sealed class BootstrapScenePlayModeTests
    {
        private const string bootstrapScopeTypeName = "Madbox.App.Bootstrap.BootstrapScope";
        private const string completionPropertyName = "IsBootstrapCompleted";
        private const int sceneLoadTimeoutFrames = 600;
        private const int bootstrapScopeTimeoutFrames = 300;
        private const int bootstrapCompletionTimeoutFrames = 300;
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
            yield return WaitForBootstrapInitialization();
            AssertBootstrapCompleted();
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

        private void AssertBootstrapCompleted()
        {
            MonoBehaviour scope = FindBootstrapScope();
            bool completed = IsBootstrapCompleted(scope);
            string state = GetBootstrapStateName(scope);
            Assert.IsNotNull(scope, "Expected bootstrap scene to contain BootstrapScope.");
            Assert.IsTrue(completed, $"Expected bootstrap to complete initialization. Current state: {state}.");
        }

        private MonoBehaviour FindBootstrapScope()
        {
            Scene bootstrapScene = SceneManager.GetSceneByName("Bootstrap");
            MonoBehaviour[] behaviours = FindBehaviours();
            return FindBootstrapScopeInScene(bootstrapScene, behaviours);
        }

        private MonoBehaviour FindBootstrapScopeInScene(Scene bootstrapScene, MonoBehaviour[] behaviours)
        {
            for (int i = 0; i < behaviours.Length; i++)
            {
                MonoBehaviour behaviour = behaviours[i];
                if (IsBootstrapScope(behaviour) && IsScopeInBootstrapScene(bootstrapScene, behaviour))
{
    return behaviour;
}
            }

            return null;
        }

        private bool IsScopeInBootstrapScene(Scene bootstrapScene, MonoBehaviour behaviour)
        {
            return !bootstrapScene.IsValid() || behaviour.gameObject.scene == bootstrapScene;
        }

        private MonoBehaviour[] FindBehaviours()
        {
            return Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
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
            PropertyInfo property = GetCompletionProperty(scope);
            if (property == null)
            {
                return false;
            }
            object value = property.GetValue(scope, null);
            return value is bool completed && completed;
        }

        private string GetBootstrapStateName(MonoBehaviour scope)
        {
            bool completed = IsBootstrapCompleted(scope);
            return completed ? "Completed" : "NotCompleted";
        }

        private PropertyInfo GetCompletionProperty(MonoBehaviour scope)
        {
            if (scope == null)
            {
                return null;
            }
            return scope.GetType().GetProperty(completionPropertyName, BindingFlags.Instance | BindingFlags.Public);
        }

        private void OnLogMessageReceived(string condition, string stackTrace, LogType type)
        {
            if (!IsFatal(type))
            {
                return;
            }
            fatalLogs.Add($"{type}: {condition}\n{stackTrace}");
        }

        private bool IsFatal(LogType type)
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


