using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Madbox.Addressables.Contracts;
using NUnit.Framework;
using Scaffold.Events;
using Scaffold.Navigation.Contracts;
using Scaffold.Types;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Scaffold.Navigation.Tests
{
    public class NavigationTests
    {
        [Test]
        public void NavigationOptions_DefaultState_AllFieldsAreNull()
        {
            NavigationOptions options = new NavigationOptions();
            Assert.IsNull(options.RenderOverride);
            Assert.IsNull(options.CloseAllViews);
        }

        [Test]
        public void NavigationPoint_Dispose_SetsDisposedToTrue()
        {
            using NavigationFixture fixture = CreateNavigationFixture();
            fixture.Navigation.Open(fixture.Controller);
            NavigationPoint point = fixture.Navigation.CurrentPoint;
            Assert.IsFalse(point.Disposed);
            point.Dispose();
            Assert.IsTrue(point.Disposed);
        }

        [Test]
        public void NavigationPoint_Constructor_StoresIsSceneView()
        {
            using NavigationFixture fixture = CreateNavigationFixture();
            fixture.Navigation.Open(fixture.Controller);
            NavigationPoint point = fixture.Navigation.CurrentPoint;
            Assert.IsTrue(point.IsSceneView);
        }

        [Test]
        public void NavigationController_Constructor_WithNullEvents_Throws()
        {
            AssertConstructorThrowsForNullEvents();
        }

        [Test]
        public void NavigationController_Constructor_WithNullSettings_Throws()
        {
            EventController events = new EventController();
            Transform holder = BuildHolder().transform;
            INavigationMiddleware[] middlewares = Array.Empty<INavigationMiddleware>();
            FakeAddressablesGateway gateway = new FakeAddressablesGateway();
            Assert.Throws<ArgumentNullException>(() => new NavigationController(events, null, holder, middlewares, gateway));
            UnityEngine.Object.DestroyImmediate(holder.gameObject);
        }

        [Test]
        public void NavigationController_Constructor_WithNullGateway_Throws()
        {
            AssertConstructorThrowsForNullGateway();
        }

        [Test]
        public void Return_WhenOnlyCurrentPointExists_ReturnsNull()
        {
            using NavigationFixture fixture = CreateNavigationFixture();
            fixture.Navigation.Open(fixture.Controller);
            IViewController returned = fixture.Navigation.Return();
            Assert.IsNull(returned);
            Assert.AreSame(fixture.Controller, fixture.Navigation.CurrentController);
        }

        [Test]
        public void Return_WhenTwoPointsExist_ReturnsPreviousController()
        {
            using NavigationFixture fixture = CreateDualNavigationFixture();
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
            using NavigationFixture fixture = CreateDualNavigationFixture();
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
            using NavigationFixture fixture = CreateNavigationFixture();
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

        [Test]
        public void Open_WhenContextViewExists_DoesNotLoadAddressables()
        {
            using NavigationFixture fixture = CreateNavigationFixture();
            fixture.Navigation.Open(fixture.Controller);
            Assert.AreEqual(0, fixture.Gateway.AssetReferenceLoadCalls);
            Assert.IsTrue(fixture.Navigation.CurrentPoint.IsSceneView);
        }

        [Test]
        public void Close_WhenClosingNonCurrentAddressablePoint_ReleasesHandle()
        {
            using AssetNavigationFixture fixture = CreateAssetNavigationFixture();
            fixture.Navigation.Open(fixture.AssetController);
            fixture.Navigation.Open(fixture.ContextController);
            fixture.Navigation.Close(fixture.AssetController);
            Assert.AreEqual(1, fixture.Gateway.LastIssuedHandle.ReleaseCalls);
        }

        private NavigationFixture CreateNavigationFixture()
        {
            GameObject holder = BuildHolder();
            GameObject viewObject = BuildViewObject<TestView>(holder, "NavigationTestView");
            NavigationSettings settings = BuildDefaultNavigationSettings(out ViewConfig config);
            FakeAddressablesGateway gateway = new FakeAddressablesGateway();
            NavigationController navigation = BuildNavigation(settings, holder.transform, gateway);
            TestController controller = new TestController();
            TestView testView = viewObject.GetComponent<TestView>();
            return new NavigationFixture(holder, settings, navigation, controller, viewObject, config, testView, gateway);
        }

        private NavigationFixture CreateDualNavigationFixture()
        {
            return CreateNavigationFixture();
        }

        private AssetNavigationFixture CreateAssetNavigationFixture()
        {
            GameObject assetPrefab = BuildAssetPrefab();
            AssetReference assetReference = new AssetReference("00000000000000000000000000000000");
            AssetFixtureState state = BuildAssetFixtureState(assetReference);
            NavigationSettings settings = BuildAssetNavigationSettings(assetReference, out ViewConfig assetConfig, out ViewConfig contextConfig);
            state.Gateway.RegisterPrefab(assetReference, assetPrefab);
            NavigationController navigation = BuildNavigation(settings, state.Holder.transform, state.Gateway);
            return new AssetNavigationFixture(state.Holder, settings, navigation, assetConfig, contextConfig, state.ContextObject, assetPrefab, state.Gateway);
        }

        private GameObject BuildAssetPrefab()
        {
            GameObject prefabRoot = new GameObject("PrefabRoot");
            return BuildViewObject<AssetBackedTestView>(prefabRoot, "AddressableViewPrefab");
        }

        private NavigationSettings BuildDefaultNavigationSettings(out ViewConfig config)
        {
            config = BuildViewConfig(typeof(TestView), typeof(TestController));
            return BuildSettings(config);
        }

        private NavigationSettings BuildAssetNavigationSettings(AssetReference assetReference, out ViewConfig assetConfig, out ViewConfig contextConfig)
        {
            assetConfig = BuildViewConfig(typeof(AssetBackedTestView), typeof(AssetBackedController), assetReference);
            contextConfig = BuildViewConfig(typeof(TestView), typeof(TestController));
            return BuildSettings(assetConfig, contextConfig);
        }

        private void AssertConstructorThrowsForNullEvents()
        {
            ConstructorFixture fixture = BuildConstructorFixture();
            INavigationMiddleware[] middlewares = Array.Empty<INavigationMiddleware>();
            FakeAddressablesGateway gateway = new FakeAddressablesGateway();
            TestDelegate create = () => new NavigationController(null, fixture.Settings, fixture.Holder.transform, middlewares, gateway);
            Assert.Throws<ArgumentNullException>(create);
            fixture.Dispose();
        }

        private void AssertConstructorThrowsForNullGateway()
        {
            EventController events = new EventController();
            ConstructorFixture fixture = BuildConstructorFixture();
            INavigationMiddleware[] middlewares = Array.Empty<INavigationMiddleware>();
            TestDelegate create = () => new NavigationController(events, fixture.Settings, fixture.Holder.transform, middlewares, null);
            Assert.Throws<ArgumentNullException>(create);
            fixture.Dispose();
        }

        private ConstructorFixture BuildConstructorFixture()
        {
            ViewConfig config = BuildViewConfig(typeof(TestView), typeof(TestController));
            NavigationSettings settings = BuildSettings(config);
            GameObject holder = BuildHolder();
            return new ConstructorFixture(config, settings, holder);
        }

        private AssetFixtureState BuildAssetFixtureState(AssetReference assetReference)
        {
            GameObject holder = BuildHolder();
            GameObject contextObject = BuildViewObject<TestView>(holder, "ContextView");
            FakeAddressablesGateway gateway = new FakeAddressablesGateway();
            return new AssetFixtureState(holder, contextObject, gateway);
        }

        private NavigationController BuildNavigation(NavigationSettings settings, Transform holderTransform, FakeAddressablesGateway gateway)
        {
            EventController events = new EventController();
            INavigationMiddleware[] middlewares = Array.Empty<INavigationMiddleware>();
            return new NavigationController(events, settings, holderTransform, middlewares, gateway);
        }

        private NavigationSettings BuildSettings(params ViewConfig[] configs)
        {
            NavigationSettings settings = ScriptableObject.CreateInstance<NavigationSettings>();
            List<ViewConfig> screens = new List<ViewConfig>(configs);
            ApplyFieldValue(settings, "screens", screens);
            return settings;
        }

        private ViewConfig BuildViewConfig(Type viewType, Type controllerType, AssetReference asset = null)
        {
            ViewConfig config = ScriptableObject.CreateInstance<ViewConfig>();
            TypeReference viewTypeReference = new TypeReference(viewType);
            TypeReference controllerTypeReference = new TypeReference(controllerType);
            ApplyFieldValue(config, "viewType", viewTypeReference);
            ApplyFieldValue(config, "controllerType", controllerTypeReference);
            ApplyFieldValue(config, "asset", asset);
            return config;
        }

        private GameObject BuildHolder()
        {
            return new GameObject("NavigationTestHolder");
        }

        private GameObject BuildViewObject<TView>(GameObject holder, string name) where TView : MonoBehaviour
        {
            GameObject viewObject = new GameObject(name);
            viewObject.transform.SetParent(holder.transform);
            viewObject.AddComponent<Canvas>();
            viewObject.AddComponent<TView>();
            return viewObject;
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
            NavigationOptions options = new NavigationOptions { RenderOverride = RenderMode.ScreenSpaceOverlay };
            point.SetDepth(30, options);
            Canvas canvas = fixture.ViewObject.GetComponent<Canvas>();
            Assert.AreEqual(RenderMode.ScreenSpaceOverlay, canvas.renderMode);
        }

        private void ApplyFieldValue(object target, string fieldName, object value)
        {
            Type targetType = target.GetType();
            BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
            FieldInfo field = targetType.GetField(fieldName, flags);
            if (field == null) { throw new MissingFieldException(targetType.Name, fieldName); }
            field.SetValue(target, value);
        }

        private sealed class NavigationFixture : IDisposable
        {
            public NavigationFixture(GameObject holder, NavigationSettings settings, NavigationController navigation, TestController controller, GameObject viewObject, ViewConfig config, TestView testView, FakeAddressablesGateway gateway)
            {
                Holder = holder;
                Settings = settings;
                Navigation = navigation;
                Controller = controller;
                ViewObject = viewObject;
                Config = config;
                TestView = testView;
                Gateway = gateway;
            }

            public GameObject Holder { get; }
            public NavigationSettings Settings { get; }
            public NavigationController Navigation { get; }
            public TestController Controller { get; }
            public GameObject ViewObject { get; }
            public ViewConfig Config { get; }
            public TestView TestView { get; }
            public FakeAddressablesGateway Gateway { get; }

            public void Dispose()
            {
                UnityEngine.Object.DestroyImmediate(ViewObject);
                UnityEngine.Object.DestroyImmediate(Holder);
                UnityEngine.Object.DestroyImmediate(Config);
                UnityEngine.Object.DestroyImmediate(Settings);
            }
        }

        private sealed class AssetNavigationFixture : IDisposable
        {
            public AssetNavigationFixture(GameObject holder, NavigationSettings settings, NavigationController navigation, ViewConfig assetConfig, ViewConfig contextConfig, GameObject contextObject, GameObject assetPrefab, FakeAddressablesGateway gateway)
            {
                Holder = holder;
                Settings = settings;
                Navigation = navigation;
                AssetConfig = assetConfig;
                ContextConfig = contextConfig;
                ContextObject = contextObject;
                AssetPrefab = assetPrefab;
                Gateway = gateway;
                AssetController = new AssetBackedController();
                ContextController = new TestController();
            }

            public GameObject Holder { get; }
            public NavigationSettings Settings { get; }
            public NavigationController Navigation { get; }
            public ViewConfig AssetConfig { get; }
            public ViewConfig ContextConfig { get; }
            public GameObject ContextObject { get; }
            public GameObject AssetPrefab { get; }
            public FakeAddressablesGateway Gateway { get; }
            public AssetBackedController AssetController { get; }
            public TestController ContextController { get; }

            public void Dispose()
            {
                UnityEngine.Object.DestroyImmediate(ContextObject);
                UnityEngine.Object.DestroyImmediate(Holder);
                UnityEngine.Object.DestroyImmediate(AssetConfig);
                UnityEngine.Object.DestroyImmediate(ContextConfig);
                UnityEngine.Object.DestroyImmediate(Settings);
                UnityEngine.Object.DestroyImmediate(AssetPrefab.transform.parent.gameObject);
            }
        }

        private sealed class ConstructorFixture : IDisposable
        {
            public ConstructorFixture(ViewConfig config, NavigationSettings settings, GameObject holder)
            {
                Config = config;
                Settings = settings;
                Holder = holder;
            }

            public ViewConfig Config { get; }
            public NavigationSettings Settings { get; }
            public GameObject Holder { get; }

            public void Dispose()
            {
                UnityEngine.Object.DestroyImmediate(Config);
                UnityEngine.Object.DestroyImmediate(Settings);
                UnityEngine.Object.DestroyImmediate(Holder);
            }
        }

        private sealed class AssetFixtureState
        {
            public AssetFixtureState(GameObject holder, GameObject contextObject, FakeAddressablesGateway gateway)
            {
                Holder = holder;
                ContextObject = contextObject;
                Gateway = gateway;
            }

            public GameObject Holder { get; }
            public GameObject ContextObject { get; }
            public FakeAddressablesGateway Gateway { get; }
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

        private sealed class AssetBackedTestView : MonoBehaviour, IView
        {
            public ViewState State => ViewState.Closed;
            public ViewType Type => ViewType.Screen;
            public void Bind(IViewController controller) { }
            public void Close() { }
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

        private sealed class AssetBackedController : IViewController
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
            public Task AnimateView(AnimationType direction)
            {
                return Task.CompletedTask;
            }
        }

        private sealed class ViewTransitionHandlerProbe : IViewTransitionHandler
        {
            public Task DoTransition(object transitionData, TransitionDirection direction)
            {
                return Task.CompletedTask;
            }
        }

        private sealed class FakeAddressablesGateway : IAddressablesGateway
        {
            public int AssetReferenceLoadCalls { get; private set; }
            public FakeAssetHandle LastIssuedHandle { get; private set; }
            private readonly Dictionary<string, GameObject> prefabs = new Dictionary<string, GameObject>();

            public Task InitializeAsync(CancellationToken cancellationToken = default)
            {
                return Task.CompletedTask;
            }

            public Task<IAssetHandle<T>> LoadAsync<T>(AssetKey key, CancellationToken cancellationToken = default) where T : UnityEngine.Object
            {
                throw new NotSupportedException();
            }

            public Task<IAssetGroupHandle<T>> LoadAsync<T>(AssetLabelReference label, CancellationToken cancellationToken = default) where T : UnityEngine.Object
            {
                throw new NotSupportedException();
            }

            public Task<IAssetHandle<T>> LoadAsync<T>(AssetReference reference, CancellationToken cancellationToken = default) where T : UnityEngine.Object
            {
                AssetReferenceLoadCalls++;
                GameObject prefab = ResolvePrefab(reference);
                FakeAssetHandle handle = new FakeAssetHandle(prefab);
                LastIssuedHandle = handle;
                IAssetHandle<T> typed = CastHandle<T>(handle);
                return Task.FromResult(typed);
            }

            public Task<IAssetHandle<T>> LoadAsync<T>(AssetReferenceT<T> reference, CancellationToken cancellationToken = default) where T : UnityEngine.Object
            {
                return LoadAsync<T>((AssetReference)reference, cancellationToken);
            }

            public void RegisterPrefab(AssetReference reference, GameObject prefab)
            {
                string key = reference.RuntimeKey.ToString();
                prefabs[key] = prefab;
            }

            private GameObject ResolvePrefab(AssetReference reference)
            {
                string key = reference.RuntimeKey.ToString();
                if (prefabs.TryGetValue(key, out GameObject prefab)) { return prefab; }
                throw new InvalidOperationException($"Missing fake prefab for key '{key}'.");
            }

            private IAssetHandle<T> CastHandle<T>(FakeAssetHandle handle) where T : UnityEngine.Object
            {
                if (typeof(T) != typeof(GameObject))
                {
                    throw new InvalidOperationException("Fake gateway supports GameObject loads only.");
                }
                return handle as IAssetHandle<T>;
            }
        }

        private sealed class FakeAssetHandle : IAssetHandle<GameObject>
        {
            public FakeAssetHandle(GameObject asset)
            {
                Id = Guid.NewGuid().ToString("N");
                Asset = asset;
            }

            public string Id { get; }
            public Type AssetType => typeof(GameObject);
            public UnityEngine.Object UntypedAsset => Asset;
            public bool IsReleased => ReleaseCalls > 0;
            public GameObject Asset { get; }
            public int ReleaseCalls { get; private set; }

            public void Release()
            {
                ReleaseCalls++;
            }
        }
    }
}
