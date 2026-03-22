using System;
using System.Collections.Generic;
using Madbox.Gold;
using Madbox.Gold.Contracts;
using Madbox.Levels;
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
        public void Construct_WhenLevelMenuHasEntries_ExposesAvailableLevels()
        {
            FakeGoldService gold = new FakeGoldService(0);
            AvailableLevel entry = new AvailableLevel(ScriptableObject.CreateInstance<LevelDefinition>(), GameModuleDTO.Modules.Level.LevelAvailabilityState.Unlocked);
            FakeLevelMenu menu = new FakeLevelMenu(entry);
            MainMenuViewModel viewModel = new MainMenuViewModel();
            viewModel.Construct(gold, menu);
            Assert.AreEqual(1, viewModel.AvailableLevels.Count);
            Assert.AreSame(entry, viewModel.AvailableLevels[0]);
        }

        private static MainMenuViewModel CreateBoundViewModel(FakeGoldService service)
        {
            MainMenuViewModel viewModel = new MainMenuViewModel();
            viewModel.Construct(service, new FakeLevelMenu());
            viewModel.Bind(new FakeNavigation());
            return viewModel;
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

        private sealed class FakeLevelMenu : ILevelService
        {
            public FakeLevelMenu(params AvailableLevel[] levels)
            {
                this.levels = levels ?? Array.Empty<AvailableLevel>();
            }

            private readonly AvailableLevel[] levels;

            public IReadOnlyList<AvailableLevel> GetAvailableLevels()
            {
                return levels;
            }
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

            public void Open<TViewController>(TViewController controller, bool closeCurrent = false, NavigationOptions options = null)
                where TViewController : IViewController
            {
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
