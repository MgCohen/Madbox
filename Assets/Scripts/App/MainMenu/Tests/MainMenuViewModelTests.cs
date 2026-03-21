using System;
using Madbox.Battle.Services;
using Madbox.Gold;
using Madbox.Gold.Contracts;
using NUnit.Framework;
using Scaffold.Navigation.Contracts;
using UnityEngine;

namespace Madbox.App.MainMenu.Tests
{
    public class MainMenuViewModelTests
    {
        [Test]
        public void Bind_WhenServiceHasGold_ExposesInitialGold()
        {
            FakeGoldService service = new FakeGoldService(7);
            MainMenuViewModel viewModel = CreateBoundViewModel(service);
            Assert.AreEqual(7, viewModel.Gold);
        }

        [Test]
        public void AddOneGold_WhenInvoked_IncrementsGold()
        {
            FakeGoldService service = new FakeGoldService(3);
            MainMenuViewModel viewModel = CreateBoundViewModel(service);
            viewModel.AddOneGold();
            Assert.AreEqual(4, viewModel.Gold);
        }

        [Test]
        public void View_Bind_UpdatesGoldLabelWhenValueChanges()
        {
            FakeGoldService service = new FakeGoldService(1);
            MainMenuViewModel viewModel = CreateBoundViewModel(service);
            using ViewFixture fixture = CreateViewFixture(viewModel);
            viewModel.AddOneGold();
            Component label = BuildResolveGoldLabel(fixture.Root);
            Assert.IsNotNull(label);
            string text = BuildReadTextValue(label);
            Assert.AreEqual("Gold: 2", text);
        }

        [Test]
        public void StartGame_WhenCalled_OpensGameView()
        {
            FakeNavigation navigation = new FakeNavigation();
            MainMenuViewModel viewModel = CreateStartGameViewModel(navigation, "level-from-menu");
            viewModel.StartGame();
            BuildAssertOpenedGameView(navigation, "level-from-menu");
        }

        private static MainMenuViewModel CreateBoundViewModel(FakeGoldService service)
        {
            MainMenuViewModel viewModel = new MainMenuViewModel();
            viewModel.Construct(service);
            viewModel.Bind(new FakeNavigation());
            return viewModel;
        }

        private static MainMenuViewModel CreateStartGameViewModel(FakeNavigation navigation, string levelId)
        {
            FakeGoldService service = new FakeGoldService(0);
            MainMenuViewModel viewModel = new MainMenuViewModel();
            viewModel.Construct(service);
            viewModel.SelectedLevelId = levelId;
            viewModel.Bind(navigation);
            return viewModel;
        }

        private static void BuildAssertOpenedGameView(FakeNavigation navigation, string expectedLevelId)
        {
            Assert.IsNotNull(navigation.OpenedController);
            Assert.AreEqual("Madbox.Battle.Services.GameViewModel", navigation.OpenedController.GetType().FullName);
            GameViewModel gameViewModel = navigation.OpenedController as GameViewModel;
            Assert.IsNotNull(gameViewModel);
            Assert.AreEqual(expectedLevelId, gameViewModel.SelectedLevelId.Value);
        }

        private static ViewFixture CreateViewFixture(MainMenuViewModel viewModel)
        {
            GameObject root = new GameObject("MainMenuViewTestRoot", typeof(RectTransform));
            MainMenuView view = root.AddComponent<MainMenuView>();
            view.Bind(viewModel);
            return new ViewFixture(root);
        }

        private static Component BuildResolveGoldLabel(GameObject root)
        {
            Component[] components = root.GetComponentsInChildren<Component>(true);
            return BuildFindGoldLabel(components);
        }

        private static Component BuildFindGoldLabel(Component[] components)
        {
            for (int i = 0; i < components.Length; i++)
            {
                if (BuildIsGoldLabel(components[i]))
{
    return components[i];
}
            }
            return null;
        }

        private static bool BuildIsGoldLabel(Component component)
        {
            if (!BuildIsTmpLabel(component))
{
    return false;
}
            string value = BuildReadTextValue(component);
            return value.StartsWith("Gold:");
        }

        private static bool BuildIsTmpLabel(Component component)
        {
            return component != null && component.GetType().Name == "TextMeshProUGUI";
        }

        private static string BuildReadTextValue(Component component)
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

        private sealed class ViewFixture : IDisposable
        {
            public ViewFixture(GameObject root)
            {
                Root = root;
            }

            public GameObject Root { get; }

            public void Dispose()
            {
                if (Root == null)
{
    return;
}
                UnityEngine.Object.DestroyImmediate(Root);
            }
        }

        private sealed class FakeGoldService : IGoldService
        {
            public FakeGoldService(int initialGold)
            {
                wallet = new GoldWallet(initialGold);
            }

            private readonly GoldWallet wallet;

            public void Add(int amount)
            {
                wallet.Add(amount);
            }

            public GoldWallet GetWallet()
            {
                return wallet;
            }
        }

        private sealed class FakeNavigation : INavigation
        {
            public IViewController CurrentController => null;

            public IViewController OpenedController { get; private set; }

            public void Open<TViewController>(TViewController controller, bool closeCurrent = false, NavigationOptions options = null) where TViewController : IViewController
            {
                OpenedController = controller;
            }

            public void Close<TViewController>(TViewController controller) where TViewController : IViewController
            {
            }

            public IViewController Return()
            {
                return null;
            }
        }
    }
}



