using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
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

        [UnityTest]
        public IEnumerator BootstrapScene_OpensMainMenu_AndAddGoldUpdatesTmpText()
        {
            yield return LoadBootstrapScene();
            yield return WaitForBootstrapInitialization();
            Component mainMenuView = null;
            yield return WaitForMainMenuView(240, value => mainMenuView = value);
            yield return AssertMainMenuGoldFlow(mainMenuView);
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

        private IEnumerator WaitForMainMenuView(int maxFrames, System.Action<Component> onComplete)
        {
            Component view = null;
            for (int frame = 0; frame < maxFrames && view == null; frame++)
            {
                view = FindMainMenuView();
                if (view == null) { yield return null; }
            }
            onComplete?.Invoke(view);
        }

        private Component FindMainMenuView()
        {
            MonoBehaviour[] behaviours = FindBehaviours();
            return FindMainMenuView(behaviours);
        }

        private Component FindMainMenuView(MonoBehaviour[] behaviours)
        {
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (IsMainMenuView(behaviours[i])) { return behaviours[i]; }
            }
            return null;
        }

        private bool IsMainMenuView(MonoBehaviour behaviour)
        {
            return behaviour != null && behaviour.GetType().FullName == "Madbox.App.MainMenu.MainMenuView";
        }

        private IEnumerator AssertMainMenuGoldFlow(Component mainMenuView)
        {
            Assert.IsNotNull(mainMenuView, "Expected Main Menu view to be opened after bootstrap completion.");
            Component goldText = ResolveGoldLabel(mainMenuView.gameObject);
            Assert.IsNotNull(goldText, "Expected main menu to contain a TextMeshProUGUI gold label.");
            string before = ReadTextValue(goldText);
            InvokeAddGold(mainMenuView.gameObject);
            yield return null;
            string after = ReadTextValue(goldText);
            Assert.AreNotEqual(before, after, "Expected TMP gold text to change after add-gold click.");
        }

        private void InvokeAddGold(GameObject root)
        {
            Button button = ResolveAddGoldButton(root);
            Assert.IsNotNull(button, "Expected main menu to contain an add-gold button.");
            button.onClick.Invoke();
        }

        private Button ResolveAddGoldButton(GameObject root)
        {
            Button[] buttons = root.GetComponentsInChildren<Button>(true);
            return buttons.Length > 0 ? buttons[0] : null;
        }

        private Component ResolveGoldLabel(GameObject root)
        {
            Component[] labels = root.GetComponentsInChildren<Component>(true);
            return FindGoldLabel(labels);
        }

        private Component FindGoldLabel(Component[] labels)
        {
            for (int i = 0; i < labels.Length; i++)
            {
                if (IsGoldLabel(labels[i])) { return labels[i]; }
            }
            return null;
        }

        private bool IsGoldLabel(Component label)
        {
            if (!IsTmpLabel(label)) { return false; }
            string value = ReadTextValue(label);
            return value.StartsWith("Gold:");
        }

        private bool IsTmpLabel(Component label)
        {
            return label != null && label.GetType().Name == "TextMeshProUGUI";
        }

        private string ReadTextValue(Component component)
        {
            if (component == null) { return string.Empty; }
            PropertyInfo property = component.GetType().GetProperty("text");
            if (property == null) { return string.Empty; }
            object value = property.GetValue(component);
            return value as string ?? string.Empty;
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
