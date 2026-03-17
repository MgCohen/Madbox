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
            const int maxFrames = 180;
            yield return WaitForBootstrapScope(maxFrames);
            yield return WaitForBootstrapCompletion(maxFrames);
        }

        private IEnumerator WaitForBootstrapScope(int maxFrames)
        {
            int frame = 0;
            MonoBehaviour scope = FindBootstrapScope();
            while (scope == null && frame < maxFrames)
            {
                frame++;
                yield return null;
                scope = FindBootstrapScope();
            }
        }

        private IEnumerator WaitForBootstrapCompletion(int maxFrames)
        {
            int frame = 0;
            while (ShouldWaitForBootstrapCompletion(frame, maxFrames))
            {
                frame++;
                yield return null;
            }
        }

        private bool ShouldWaitForBootstrapCompletion(int frame, int maxFrames)
        {
            if (frame >= maxFrames) { return false; }
            MonoBehaviour scope = FindBootstrapScope();
            return !IsBootstrapCompleted(scope);
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
            MonoBehaviour[] behaviours = FindBehaviours();
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (IsBootstrapScope(behaviours[i])) { return behaviours[i]; }
            }

            return null;
        }

        private MonoBehaviour[] FindBehaviours()
        {
            return Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        }

        private bool IsBootstrapScope(MonoBehaviour behaviour)
        {
            if (behaviour == null) { return false; }
            return behaviour.GetType().FullName == bootstrapScopeTypeName;
        }

        private bool IsBootstrapCompleted(MonoBehaviour scope)
        {
            PropertyInfo property = GetCompletionProperty(scope);
            if (property == null) { return false; }
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
            if (scope == null) { return null; }
            return scope.GetType().GetProperty(completionPropertyName, BindingFlags.Instance | BindingFlags.Public);
        }

        private void OnLogMessageReceived(string condition, string stackTrace, LogType type)
        {
            if (!IsFatal(type)) { return; }
            fatalLogs.Add($"{type}: {condition}");
        }

        private bool IsFatal(LogType type)
        {
            return type == LogType.Assert || type == LogType.Error || type == LogType.Exception;
        }

        private void AssertNoFatalLogs()
        {
            if (fatalLogs == null || fatalLogs.Count == 0) { return; }
            string message = string.Join("\n", fatalLogs);
            Assert.Fail(message);
        }
    }
}
