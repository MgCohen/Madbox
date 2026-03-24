using System;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;

namespace Madbox.App.Gameplay.Tests
{
    public class GameViewTests
    {
        [Test]
        public void HandleSessionCompleted_WhenWin_ShowsPopupAndWinText()
        {
            using ViewFixture fixture = new ViewFixture();

            InvokeHandleSessionCompleted(fixture.View, "Win");

            Assert.IsTrue(fixture.PopupRoot.activeSelf);
            Assert.AreEqual("You Win", fixture.Label.text);
        }

        [Test]
        public void HandleSessionCompleted_WhenLose_ShowsPopupAndLoseText()
        {
            using ViewFixture fixture = new ViewFixture();

            InvokeHandleSessionCompleted(fixture.View, "Lose");

            Assert.IsTrue(fixture.PopupRoot.activeSelf);
            Assert.AreEqual("You Lose", fixture.Label.text);
        }

        [Test]
        public void HandleSessionReady_HidesLoadingCover()
        {
            using ViewFixture fixture = new ViewFixture();
            fixture.LoadingRoot.SetActive(true);

            InvokeNoArgPrivateMethod(fixture.View, "HandleSessionReady");

            Assert.IsFalse(fixture.LoadingRoot.activeSelf);
        }

        private static void InvokeHandleSessionCompleted(object view, string reasonName)
        {
            MethodInfo method = view.GetType().GetMethod("HandleSessionCompleted", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(method);
            ParameterInfo[] parameters = method.GetParameters();
            Assert.AreEqual(1, parameters.Length);
            object reason = Enum.Parse(parameters[0].ParameterType, reasonName);
            method.Invoke(view, new object[] { reason });
        }

        private static void InvokeNoArgPrivateMethod(object target, string methodName)
        {
            MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(method);
            method.Invoke(target, Array.Empty<object>());
        }

        private sealed class ViewFixture : IDisposable
        {
            public ViewFixture()
            {
                Root = new GameObject("GameViewRoot", typeof(RectTransform));
                Type gameViewType = ResolveGameViewType();
                View = Root.AddComponent(gameViewType);

                PopupRoot = new GameObject("EndPopup", typeof(RectTransform));
                PopupRoot.transform.SetParent(Root.transform, false);
                PopupRoot.SetActive(false);
                LoadingRoot = new GameObject("LoadingCover", typeof(RectTransform));
                LoadingRoot.transform.SetParent(Root.transform, false);
                LoadingRoot.SetActive(true);

                GameObject labelGo = new GameObject("EndLabel", typeof(RectTransform));
                labelGo.transform.SetParent(PopupRoot.transform, false);
                Label = labelGo.AddComponent<TextMeshProUGUI>();

                SetPrivateField(View, "endStatePopupRoot", PopupRoot);
                SetPrivateField(View, "endStateLabel", Label);
                SetPrivateField(View, "loadingViewRoot", LoadingRoot);
            }

            public GameObject Root { get; }
            public Component View { get; }
            public GameObject PopupRoot { get; }
            public GameObject LoadingRoot { get; }
            public TextMeshProUGUI Label { get; }

            public void Dispose()
            {
                if (Root != null)
                {
                    UnityEngine.Object.DestroyImmediate(Root);
                }
            }
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field);
            field.SetValue(target, value);
        }

        private static Type ResolveGameViewType()
        {
            Type type = Type.GetType("Madbox.App.Gameplay.GameView, Madbox.Gameplay");
            Assert.IsNotNull(type);
            return type;
        }
    }
}
