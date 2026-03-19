using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
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
        public IEnumerator WhiteBoxLoop_MainMenuToGameAndBack_Works()
        {
            yield return LoadBootstrapScene();
            yield return WaitForMainMenu();
            yield return ClickStartGame();
            yield return WaitForGameStateDone();
            yield return ClickComplete();
            yield return WaitForMainMenu();
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

        private IEnumerator WaitForMainMenu()
        {
            const float timeoutSeconds = 6f;
            float startedAt = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - startedAt < timeoutSeconds)
            {
                if (FindButton("StartGameButton") != null)
                {
                    yield break;
                }

                yield return null;
            }

            Assert.Fail("MainMenu StartGameButton not found.");
        }

        private IEnumerator ClickStartGame()
        {
            Button button = FindButton("StartGameButton");
            Assert.IsNotNull(button);
            button.onClick.Invoke();
            yield return null;
        }

        private IEnumerator WaitForGameStateDone()
        {
            const float timeoutSeconds = 8f;
            float startedAt = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - startedAt < timeoutSeconds)
            {
                Component stateText = FindTmpLabel("GameStateText");
                if (stateText != null)
                {
                    string text = ReadTmpText(stateText);
                    if (text == "GameState: Done")
                    {
                        yield break;
                    }
                }

                yield return null;
            }

            Assert.Fail("GameState did not reach Done within timeout.");
        }

        private IEnumerator ClickComplete()
        {
            const float timeoutSeconds = 4f;
            float startedAt = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - startedAt < timeoutSeconds)
            {
                Button button = FindButton("CompleteButton");
                if (button != null && button.gameObject.activeInHierarchy)
                {
                    button.onClick.Invoke();
                    yield return null;
                    yield break;
                }

                yield return null;
            }

            Assert.Fail("Complete button was not available.");
        }

        private Button FindButton(string name)
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

        private Component FindTmpLabel(string name)
        {
            Component[] components = Object.FindObjectsByType<Component>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < components.Length; i++)
            {
                Component component = components[i];
                if (component == null)
                {
                    continue;
                }

                if (component.gameObject.name != name)
                {
                    continue;
                }

                if (component.GetType().Name == "TextMeshProUGUI")
                {
                    return component;
                }
            }

            return null;
        }

        private string ReadTmpText(Component component)
        {
            if (component == null)
            {
                return string.Empty;
            }

            var property = component.GetType().GetProperty("text");
            if (property == null)
            {
                return string.Empty;
            }

            object value = property.GetValue(component);
            return value as string ?? string.Empty;
        }

        private void OnLogMessageReceived(string condition, string stackTrace, LogType type)
        {
            if (type == LogType.Assert || type == LogType.Error || type == LogType.Exception)
            {
                fatalLogs.Add($"{type}: {condition}");
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
