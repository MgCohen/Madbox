using Scaffold.Navigation.Contracts;
using Scaffold.MVVM.Binding;
using System.Collections.Generic;
using System;
using NUnit.Framework;
using Scaffold.MVVM.Contracts;
namespace Scaffold.MVVM.Tests
{
    public class ViewModelTests
    {
        [Test]
        public void ViewModel_Bind_CallsInitialize()
        {
            TestViewModel viewModel = new TestViewModel();
            viewModel.Bind(null);
            Assert.IsTrue(viewModel.InitializeCalled);
        }

        [Test]
        public void ViewModel_Close_DelegatesToNavigation()
        {
            SpyNavigation navigation = new SpyNavigation();
            ClosableViewModel viewModel = new ClosableViewModel();
            viewModel.Bind(navigation);
            ExecuteCloseAndAssert(viewModel, navigation);
        }

        [Test]
        public void ViewModel_Close_WithoutNavigation_DoesNotInvokeOnClosed()
        {
            ClosableViewModel viewModel = new ClosableViewModel();
            viewModel.Close();
            Assert.AreEqual(0, viewModel.OnClosedCalls);
        }

        [Test]
        public void ViewModel_Bind_CalledTwice_InvokesInitializeTwice()
        {
            CountingInitializeViewModel viewModel = new CountingInitializeViewModel();
            viewModel.Bind(null);
            viewModel.Bind(null);
            Assert.AreEqual(2, viewModel.InitializeCalls);
        }

        [Test]
        public void BindContext_Bind_WithStrictOptions_UpdatesImmediatelyAndOnUpdate()
        {
            StrictBindScenario scenario = CreateStrictBindScenario(3);
            scenario.Source = 8;
            scenario.Context.Update();
            AssertStrictBindUpdates(scenario.Bind);
        }

        [Test]
        public void BindContext_Update_WithLazyBindAndNullReference_DoesNotThrow()
        {
            LazyNullScenario scenario = CreateLazyNullScenario();
            scenario.Model.Value = null;
            Assert.DoesNotThrow(() => scenario.Context.Update());
            Assert.AreEqual(0, scenario.Bind.Values.Count);
        }

        [Test]
        public void BindContext_Update_WithStrictBindAndNullReference_Throws()
        {
            var model = new LazyModel();
            var context = new BindContext<int>(() => model.Value.Length);
            var bind = new SpyBind<int>();
            context.Bind(bind, BindingOptions.Strict);
            model.Value = null;

            Assert.Throws<NullReferenceException>(() => context.Update());
        }

        [Test]
        public void BindContext_UnbindBinding_RemovesOnlyTargetBinding()
        {
            UnbindScenario scenario = CreateUnbindScenario(2);
            scenario.Context.Unbind(scenario.First);
            scenario.Source = 9;
            scenario.Context.Update();
            AssertUnbindBehavior(scenario.First, scenario.Second);
        }

        [Test]
        public void ViewModelContracts_BindingCoreInterfaces_ArePresent()
        {
            AssertContractType<IBindContext>("IBindContext");
            AssertContractType<IBindedCollection<int, int>>("IBindedCollection`2");
            AssertContractType<IBindedProperty<int, int>>("IBindedProperty`2");
            AssertContractType<IBindings>("IBindings");
        }

        [Test]
        public void ViewModelContracts_ExtensionInterfaces_ArePresent()
        {
            AssertContractType<IBindSet<int, int>>("IBindSet`2");
            AssertContractType<IBindSource>("IBindSource");
            AssertContractType<ICollectionHandler<int, int>>("ICollectionHandler`2");
            AssertContractType<IViewModel>("IViewModel");
        }

        private void ExecuteCloseAndAssert(ClosableViewModel viewModel, SpyNavigation navigation)
        {
            viewModel.Close();
            Assert.AreEqual(1, navigation.CloseCalls);
            Assert.AreSame(viewModel, navigation.LastClosedController);
            Assert.AreEqual(1, viewModel.OnClosedCalls);
        }

        private StrictBindScenario CreateStrictBindScenario(int source)
        {
            StrictBindScenario scenario = new StrictBindScenario(source);
            scenario.Context.Bind(scenario.Bind, BindingOptions.Strict);
            return scenario;
        }

        private void AssertStrictBindUpdates(SpyBind<int> bind)
        {
            Assert.AreEqual(2, bind.Values.Count);
            Assert.AreEqual(3, bind.Values[0]);
            Assert.AreEqual(8, bind.Values[1]);
        }

        private LazyNullScenario CreateLazyNullScenario()
        {
            LazyNullScenario scenario = new LazyNullScenario();
            scenario.Context.Bind(scenario.Bind, BindingOptions.Lazy);
            return scenario;
        }

        private UnbindScenario CreateUnbindScenario(int source)
        {
            UnbindScenario scenario = new UnbindScenario(source);
            scenario.Context.Bind(scenario.First, BindingOptions.Strict);
            scenario.Context.Bind(scenario.Second, BindingOptions.Strict);
            return scenario;
        }

        private void AssertUnbindBehavior(SpyBind<int> first, SpyBind<int> second)
        {
            Assert.AreEqual(1, first.Values.Count);
            Assert.AreEqual(2, second.Values.Count);
            Assert.AreEqual(9, second.Values[1]);
        }

        private void AssertContractType<TContract>(string expectedName)
        {
            Assert.AreEqual(expectedName, typeof(TContract).Name);
        }
    }

    public sealed class StrictBindScenario
    {
        public StrictBindScenario(int initialValue)
        {
            source = initialValue;
            Bind = new SpyBind<int>();
            Context = new BindContext<int>(() => source);
        }

        public BindContext<int> Context { get; }
        public SpyBind<int> Bind { get; }
        public int Source { set => source = value; }
        private int source;
    }

    public sealed class LazyNullScenario
    {
        public LazyNullScenario()
        {
            Model = new LazyModel();
            Bind = new SpyBind<int>();
            Context = new BindContext<int>(() => Model.Value.Length);
        }

        public LazyModel Model { get; }
        public SpyBind<int> Bind { get; }
        public BindContext<int> Context { get; }
    }

    public sealed class UnbindScenario
    {
        public UnbindScenario(int initialValue)
        {
            source = initialValue;
            First = new SpyBind<int>();
            Second = new SpyBind<int>();
            Context = new BindContext<int>(() => source);
        }

        public BindContext<int> Context { get; }
        public SpyBind<int> First { get; }
        public SpyBind<int> Second { get; }
        public int Source { set => source = value; }
        private int source;
    }

    public sealed class LazyModel
    {
        public string Value { get; set; } = "seed";
    }

    public sealed class SpyBind<T> : IBind<T>
    {
        public List<T> Values { get; } = new List<T>();

        public void Update(T value)
        {
            Values.Add(value);
        }
    }

    public partial class TestViewModel : global::Scaffold.MVVM.ViewModel
    {
        public bool InitializeCalled { get; private set; }

        protected override void Initialize()
        {
            InitializeCalled = true;
        }
    }

    public partial class ClosableViewModel : global::Scaffold.MVVM.ViewModel
    {
        public int OnClosedCalls { get; private set; }

        protected override void OnClosed()
        {
            OnClosedCalls++;
            base.OnClosed();
        }
    }

    public partial class CountingInitializeViewModel : global::Scaffold.MVVM.ViewModel
    {
        public int InitializeCalls { get; private set; }

        protected override void Initialize()
        {
            InitializeCalls++;
        }
    }

    public sealed class SpyNavigation : INavigation
    {
        public int CloseCalls { get; private set; }
        public IViewController LastClosedController { get; private set; }
        public IViewController CurrentController => null;

        public void Open<TViewController>(TViewController controller, bool closeCurrent = false, NavigationOptions options = null) where TViewController : IViewController
        {
        }

        public void Close<TViewController>(TViewController controller) where TViewController : IViewController
        {
            CloseCalls++;
            LastClosedController = controller;
        }

        public IViewController Return()
        {
            return null;
        }
    }
}




