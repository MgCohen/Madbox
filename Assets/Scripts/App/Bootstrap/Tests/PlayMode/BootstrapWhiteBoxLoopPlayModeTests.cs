using System.Collections;
using System.Collections.Generic;
using Madbox.Scope;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
#pragma warning disable SCA0003
#pragma warning disable SCA0006

namespace Madbox.Bootstrap.Tests.PlayMode
{
    public sealed class BootstrapWhiteBoxLoopPlayModeTests
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
        public IEnumerator WhiteBoxLoop_MainMenu_AddGold_UpdatesGoldLabel()
        {
            yield return LoadBootstrapScene();
            yield return WaitForBootstrapCompleted();
            yield return WaitForMainMenuAddGoldButton();
            string before = FindGoldDisplayText();
            Assert.IsFalse(string.IsNullOrEmpty(before), "Expected main menu gold label text.");
            Button addGold = FindButton("AddGoldButton");
            Assert.IsNotNull(addGold);
            addGold.onClick.Invoke();
            yield return null;
            yield return null;
            string after = FindGoldDisplayText();
            Assert.AreNotEqual(before, after, "Gold label should change after Add Gold.");
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

        private IEnumerator WaitForMainMenuAddGoldButton()
        {
            const float timeoutSeconds = 6f;
            float startedAt = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - startedAt < timeoutSeconds)
            {
                if (FindButton("AddGoldButton") != null)
                {
                    yield break;
                }

                yield return null;
            }

            Assert.Fail("Main menu AddGoldButton not found.");
        }

        private static string FindGoldDisplayText()
        {
            TextMeshProUGUI[] labels = Object.FindObjectsByType<TextMeshProUGUI>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            for (int i = 0; i < labels.Length; i++)
            {
                TextMeshProUGUI label = labels[i];
                if (label != null && label.text != null && label.text.StartsWith("Gold:", System.StringComparison.Ordinal))
                {
                    return label.text;
                }
            }

            return null;
        }

        private static Button FindButton(string name)
        {
            Button[] buttons = Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] != null && buttons[i].gameObject.name == name)
                {
                    return buttons[i];
                }
            }

            return null;
        }

        private void OnLogMessageReceived(string condition, string stackTrace, LogType type)
        {
            if (type == LogType.Assert || type == LogType.Error || type == LogType.Exception)
            {
                fatalLogs.Add($"{type}: {condition}\n{stackTrace}");
            }
        }

        private void AssertNoFatalLogs()
        {
            if (fatalLogs == null || fatalLogs.Count == 0)
            {
                return;
            }

            Assert.Fail(string.Join("\n", fatalLogs));
        }
    }
}
