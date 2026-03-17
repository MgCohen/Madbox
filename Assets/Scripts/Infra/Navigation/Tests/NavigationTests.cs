using Scaffold.Types;
using Scaffold.Events;
using UnityEngine;
using System.Reflection;
using System.Collections.Generic;
using System;
using NUnit.Framework;
using Scaffold.Navigation.Contracts;
namespace Scaffold.Navigation.Tests
{
    public class NavigationTests
    {
        [Test]
        public void NavigationOptions_DefaultState_AllFieldsAreNull()
        {
            var options = new NavigationOptions();
            Assert.IsNull(options.RenderOverride);
            Assert.IsNull(options.CloseAllViews);
        }

        [Test]
        public void NavigationPoint_Dispose_SetsDisposedToTrue()
        {
            using var fixture = CreateNavigationFixture();
            fixture.Navigation.Open(fixture.Controller);
            NavigationPoint point = fixture.Navigation.CurrentPoint;
            Assert.IsFalse(point.Disposed);
            point.Dispose();
            Assert.IsTrue(point.Disposed);
        }

        [Test]
        public void NavigationPoint_Constructor_StoresIsSceneView()
        {
            using var fixture = CreateNavigationFixture();
            fixture.Navigation.Open(fixture.Controller);
            NavigationPoint point = fixture.Navigation.CurrentPoint;
            Assert.IsTrue(point.IsSceneView);
        }

        [Test]
        public void NavigationController_Constructor_WithNullEvents_Throws()
        {
            NavigationSettings settings = BuildSettings(out ViewConfig config);
            Transform holder = BuildHolder().transform;
            INavigationMiddleware[] middlewares = Array.Empty<INavigationMiddleware>();
            Assert.Throws<ArgumentNullException>(() => new NavigationController(null, settings, holder, middlewares));
            UnityEngine.Object.DestroyImmediate(config);
            UnityEngine.Object.DestroyImmediate(settings);
            UnityEngine.Object.DestroyImmediate(holder.gameObject);
        }

        [Test]
        public void NavigationController_Constructor_WithNullSettings_Throws()
        {
            EventController events = new EventController();
            Transform holder = BuildHolder().transform;
            INavigationMiddleware[] middlewares = Array.Empty<INavigationMiddleware>();
            Assert.Throws<ArgumentNullException>(() => new NavigationController(events, null, holder, middlewares));
            UnityEngine.Object.DestroyImmediate(holder.gameObject);
        }

        [Test]
        public void Return_WhenOnlyCurrentPointExists_ReturnsNull()
        {
            using var fixture = CreateNavigationFixture();
            fixture.Navigation.Open(fixture.Controller);
            IViewController returned = fixture.Navigation.Return();
            Assert.IsNull(returned);
            Assert.AreSame(fixture.Controller, fixture.Navigation.CurrentController);
        }

        [Test]
        public void Return_WhenTwoPointsExist_ReturnsPreviousController()
        {
            using var fixture = CreateDualNavigationFixture();
            TestController secondController = new TestController();
            fixture.Navigation.Open(fixture.Controller);
            fixture.Navigation.Open(secondController);
            IViewController returned = fixture.Navigation.Return();
            Assert.AreSame(fixture.Controller, returned);
            Assert.AreSame(fixture.Controller, fixture.Navigation.CurrentController);
        }

        [Test]
        public void Close_WhenClosingNonCurrentPoint_RemovesPointFromStack()
        {
            using var fixture = CreateDualNavigationFixture();
            TestController secondController = new TestController();
            fixture.Navigation.Open(fixture.Controller);
            fixture.Navigation.Open(secondController);
            fixture.Navigation.Close(fixture.Controller);
            IViewController returned = fixture.Navigation.Return();
            AssertCloseAfterNonCurrentClose(fixture, secondController, returned);
        }

        [Test]
        public void NavigationPoint_SetDepth_WithRenderOverride_AppliesCanvasRenderMode()
        {
            using var fixture = CreateNavigationFixture();
            ApplyRenderOverrideAndAssert(fixture);
        }

        [Test]
        public void NavigationSchemaEnums_DefineExpectedValues()
        {
            Assert.AreEqual(0, (int)ViewFilter.Any);
            Assert.AreEqual(1, (int)ViewFilter.SpecificViews);
            Assert.AreEqual(2, (int)AnimationType.Opening);
            Assert.AreEqual(4, (int)TransitionDirection.ToThisView);
        }

        [Test]
        public void NavigationMiddleware_WithOpenHandler_InvokesHandler()
        {
            OpenHandlerProbe probe = new OpenHandlerProbe();
            NavigationMiddleware middleware = new NavigationMiddleware(new INavigationMiddleware[] { probe });
            TestController controller = new TestController();
            middleware.OnOpen(controller);
            Assert.AreEqual(1, probe.OpenCalls);
        }

        [Test]
        public void NavigationAnimationAndTransitionHandlers_AreImplementableContracts()
        {
            IViewAnimationHandler animationHandler = new ViewAnimationHandlerProbe();
            IViewTransitionHandler transitionHandler = new ViewTransitionHandlerProbe();
            Assert.IsNotNull(animationHandler);
            Assert.IsNotNull(transitionHandler);
        }

        private NavigationFixture CreateNavigationFixture()
        {
            var holder = BuildHolder();
            var viewObject = BuildViewObject<TestView>(holder, "NavigationTestView");
            var settings = BuildSettings(out var config);
            var navigation = BuildNavigation(settings, holder.transform);
            var controller = new TestController();
            var testView = viewObject.GetComponent<TestView>();
            return new NavigationFixture(holder, settings, navigation, controller, viewObject, config, testView);
        }

        private NavigationFixture CreateDualNavigationFixture()
        {
            return CreateNavigationFixture();
        }

        private GameObject BuildHolder()
        {
            return new GameObject("NavigationTestHolder");
        }

        private GameObject BuildViewObject<TView>(GameObject holder, string name) where TView : MonoBehaviour
        {
            var viewObject = new GameObject(name);
            viewObject.transform.SetParent(holder.transform);
            viewObject.AddComponent<Canvas>();
            viewObject.AddComponent<TView>();
            return viewObject;
        }

        private NavigationSettings BuildSettings(out ViewConfig config)
        {
            var settings = ScriptableObject.CreateInstance<NavigationSettings>();
            config = BuildViewConfig();
            List<ViewConfig> screens = new List<ViewConfig> { config };
            ApplyFieldValue(settings, "screens", screens);
            return settings;
        }

        private ViewConfig BuildViewConfig()
        {
            var config = ScriptableObject.CreateInstance<ViewConfig>();
            var viewTypeReference = new TypeReference(typeof(TestView));
            var controllerTypeReference = new TypeReference(typeof(TestController));
            ApplyFieldValue(config, "viewType", viewTypeReference);
            ApplyFieldValue(config, "controllerType", controllerTypeReference);
            return config;
        }

        private NavigationController BuildNavigation(NavigationSettings settings, Transform holderTransform)
        {
            var events = new EventController();
            var middlewares = Array.Empty<INavigationMiddleware>();
            return new NavigationController(events, settings, holderTransform, middlewares);
        }

        private void AssertCloseAfterNonCurrentClose(NavigationFixture fixture, TestController secondController, IViewController returned)
        {
            Assert.IsNull(returned);
            Assert.AreSame(secondController, fixture.Navigation.CurrentController);
            Assert.AreEqual(1, fixture.TestView.CloseCalls);
        }

        private void ApplyRenderOverrideAndAssert(NavigationFixture fixture)
        {
            fixture.Navigation.Open(fixture.Controller);
            NavigationPoint point = fixture.Navigation.CurrentPoint;
            var options = new NavigationOptions { RenderOverride = RenderMode.ScreenSpaceOverlay };
            point.SetDepth(30, options);
            Canvas canvas = fixture.ViewObject.GetComponent<Canvas>();
            Assert.AreEqual(RenderMode.ScreenSpaceOverlay, canvas.renderMode);
        }

        private void ApplyFieldValue(object target, string fieldName, object value)
        {
            var targetType = target.GetType();
            var flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
            var field = targetType.GetField(fieldName, flags);
            if (field == null) { throw new MissingFieldException(targetType.Name, fieldName); }
            field.SetValue(target, value);
        }

        private sealed class NavigationFixture : IDisposable
        {
            public NavigationFixture(GameObject holder, NavigationSettings settings, NavigationController navigation, TestController controller, GameObject viewObject, ViewConfig config, TestView testView)
            {
                Holder = holder;
                Settings = settings;
                Navigation = navigation;
                Controller = controller;
                ViewObject = viewObject;
                Config = config;
                TestView = testView;
            }

            public GameObject Holder { get; }
            public NavigationSettings Settings { get; }
            public NavigationController Navigation { get; }
            public TestController Controller { get; }
            public GameObject ViewObject { get; }
            public ViewConfig Config { get; }
            public TestView TestView { get; }

            public void Dispose()
            {
                UnityEngine.Object.DestroyImmediate(ViewObject);
                UnityEngine.Object.DestroyImmediate(Holder);
                UnityEngine.Object.DestroyImmediate(Config);
                UnityEngine.Object.DestroyImmediate(Settings);
            }
        }

        private sealed class TestView : MonoBehaviour, IView
        {
            public int CloseCalls { get; private set; }
            public ViewState State => ViewState.Closed;
            public ViewType Type => ViewType.Screen;
            public void Bind(IViewController controller) { }
            public void Close() { CloseCalls++; }
            public void Focus() { }
            public void Hide() { }
            public void Open() { }
            public void Order(int depth) { }
        }

        private sealed class TestController : IViewController
        {
            public void Bind(INavigation navigation) { }
            public void Close() { }
        }

        private sealed class OpenHandlerProbe : INavigationOpenHandler
        {
            public int OpenCalls { get; private set; }

            public void OnOpen(IViewController viewModel)
            {
                OpenCalls++;
            }
        }

        private sealed class ViewAnimationHandlerProbe : IViewAnimationHandler
        {
            public System.Threading.Tasks.Task AnimateView(AnimationType direction)
            {
                return System.Threading.Tasks.Task.CompletedTask;
            }
        }

        private sealed class ViewTransitionHandlerProbe : IViewTransitionHandler
        {
            public System.Threading.Tasks.Task DoTransition(object transitionData, TransitionDirection direction)
            {
                return System.Threading.Tasks.Task.CompletedTask;
            }
        }
    }
}


