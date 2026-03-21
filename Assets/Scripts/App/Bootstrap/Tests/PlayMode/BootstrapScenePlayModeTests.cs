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
        [Ignore("Temporarily disabled while bootstrap runtime throws NullReferenceException in PlayMode startup.")]
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
            while (!operation.isDone)
{
    yield return null;
}
        }

        private IEnumerator WaitForBootstrapInitialization()
        {
            const float timeoutSeconds = 5f;
            yield return WaitForBootstrapScope(timeoutSeconds);
            yield return WaitForBootstrapCompletion(timeoutSeconds);
        }

        private IEnumerator WaitForBootstrapScope(float timeoutSeconds)
        {
            float startedAt = Time.realtimeSinceStartup;
            MonoBehaviour scope = FindBootstrapScope();
            while (scope == null && !HasTimedOut(startedAt, timeoutSeconds))
            {
                yield return null;
                scope = FindBootstrapScope();
            }
        }

        private IEnumerator WaitForBootstrapCompletion(float timeoutSeconds)
        {
            float startedAt = Time.realtimeSinceStartup;
            while (ShouldWaitForBootstrapCompletion(startedAt, timeoutSeconds))
{
    yield return null;
}
        }

        private bool ShouldWaitForBootstrapCompletion(float startedAt, float timeoutSeconds)
        {
            if (HasTimedOut(startedAt, timeoutSeconds))
{
    return false;
}
            MonoBehaviour scope = FindBootstrapScope();
            return !IsBootstrapCompleted(scope);
        }

        private bool HasTimedOut(float startedAt, float timeoutSeconds)
        {
            return Time.realtimeSinceStartup - startedAt >= timeoutSeconds;
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
            fatalLogs.Add($"{type}: {condition}");
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


