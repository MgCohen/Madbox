using System;
using System.Collections.Generic;
using System.Reflection;
using Madbox.App.Gameplay;
using Madbox.App.MainMenu;
using Madbox.Gold;
using Madbox.Gold.Contracts;
using Madbox.Levels;
using NUnit.Framework;
using Scaffold.Navigation.Contracts;
using UnityEngine;
using UnityEngine.UI;

namespace Madbox.App.MainMenu.Tests
{
    public class MainMenuViewModelTests
    {
        [Test]
        public void Bind_WhenServiceHasGold_ExposesInitialGold()
        {
            FakeGoldService service = new FakeGoldService(7);
            MainMenuViewModel viewModel = CreateBoundViewModel(service);
            Assert.AreEqual(7, viewModel.Wallet.CurrentGold);
        }

        [Test]
        public void AddOneGold_WhenInvoked_IncrementsGold()
        {
            FakeGoldService service = new FakeGoldService(3);
            MainMenuViewModel viewModel = CreateBoundViewModel(service);
            viewModel.AddOneGold();
            Assert.AreEqual(4, viewModel.Wallet.CurrentGold);
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
        public void View_Bind_KeepsPrefabTitleAndSubtitleText()
        {
            FakeGoldService service = new FakeGoldService(0);
            MainMenuViewModel viewModel = CreateBoundViewModel(service);
            using ViewFixture fixture = CreateViewFixture(viewModel);
            Assert.AreEqual("Fuleiro", fixture.TitleLabel.text);
            Assert.AreEqual("(Its a brazilian pun)", fixture.SubtitleLabel.text);
        }

        [Test]
        public void View_Bind_WhenLevelServiceHasEntries_CreatesLevelButton()
        {
            FakeGoldService gold = new FakeGoldService(0);
            AvailableLevel entry = new AvailableLevel(ScriptableObject.CreateInstance<LevelDefinition>(), GameModuleDTO.Modules.Level.LevelAvailabilityState.Unlocked);
            FakeLevelMenu menu = new FakeLevelMenu(entry);
            MainMenuViewModel viewModel = CreateBoundViewModel(gold, menu);
            using ViewFixture fixture = CreateViewFixture(viewModel);
            Transform levelList = fixture.Root.transform.Find("LevelButtonCollectionHandler/LevelList");
            Assert.IsNotNull(levelList);
            MainMenuLevelListItem[] items = levelList.GetComponentsInChildren<MainMenuLevelListItem>(true);
            Assert.AreEqual(1, items.Length);
        }

        [Test]
        public void LevelButton_WhenClicked_InvokesGameFlowWithDefinition()
        {
            FakeGoldService gold = new FakeGoldService(0);
            LevelDefinition def = ScriptableObject.CreateInstance<LevelDefinition>();
            AvailableLevel entry = new AvailableLevel(def, GameModuleDTO.Modules.Level.LevelAvailabilityState.Unlocked);
            FakeLevelMenu menu = new FakeLevelMenu(entry);
            FakeGameFlowService flow = new FakeGameFlowService();
            MainMenuViewModel viewModel = CreateBoundViewModel(gold, menu, flow);
            using ViewFixture fixture = CreateViewFixture(viewModel);
            Transform levelList = fixture.Root.transform.Find("LevelButtonCollectionHandler/LevelList");
            MainMenuLevelListItem item = levelList.GetComponentInChildren<MainMenuLevelListItem>(true);
            Assert.IsNotNull(item);
            Button levelButton = item.GetComponent<Button>();
            Assert.IsNotNull(levelButton);
            levelButton.onClick.Invoke();
            Assert.AreSame(def, flow.LastDefinition);
            UnityEngine.Object.DestroyImmediate(def);
        }

        [Test]
        public void Bind_WhenLevelServiceHasEntries_ExposesAvailableLevels()
        {
            FakeGoldService gold = new FakeGoldService(0);
            AvailableLevel entry = new AvailableLevel(ScriptableObject.CreateInstance<LevelDefinition>(), GameModuleDTO.Modules.Level.LevelAvailabilityState.Unlocked);
            FakeLevelMenu menu = new FakeLevelMenu(entry);
            MainMenuViewModel viewModel = new MainMenuViewModel();
            InjectMainMenuServices(viewModel, gold, menu, new FakeGameFlowService());
            viewModel.Bind(new FakeNavigation());
            Assert.AreEqual(1, viewModel.AvailableLevels.Count);
            Assert.AreSame(entry, viewModel.AvailableLevels[0]);
        }

        [Test]
        public void PlayLevel_WhenInvoked_DelegatesToGameFlowService()
        {
            FakeGoldService gold = new FakeGoldService(0);
            FakeLevelMenu menu = new FakeLevelMenu();
            FakeGameFlowService flow = new FakeGameFlowService();
            MainMenuViewModel viewModel = new MainMenuViewModel();
            InjectMainMenuServices(viewModel, gold, menu, flow);
            viewModel.Bind(new FakeNavigation());
            LevelDefinition def = ScriptableObject.CreateInstance<LevelDefinition>();
            AvailableLevel entry = new AvailableLevel(def, GameModuleDTO.Modules.Level.LevelAvailabilityState.Unlocked);
            try
            {
                viewModel.PlayLevel(entry);
                Assert.AreSame(def, flow.LastDefinition);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(def);
            }
        }

        private static MainMenuViewModel CreateBoundViewModel(FakeGoldService service, FakeLevelMenu levelMenu = null, FakeGameFlowService gameFlow = null)
        {
            MainMenuViewModel viewModel = new MainMenuViewModel();
            InjectMainMenuServices(viewModel, service, levelMenu ?? new FakeLevelMenu(), gameFlow ?? new FakeGameFlowService());
            viewModel.Bind(new FakeNavigation());
            return viewModel;
        }

        private static void InjectMainMenuServices(MainMenuViewModel viewModel, IGoldService goldService, ILevelService levelService, IGameFlowService gameFlowService)
        {
            Type type = typeof(MainMenuViewModel);
            FieldInfo goldField = type.GetField("goldService", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo levelField = type.GetField("levelService", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo flowField = type.GetField("gameFlowService", BindingFlags.Instance | BindingFlags.NonPublic);
            goldField.SetValue(viewModel, goldService);
            levelField.SetValue(viewModel, levelService);
            flowField.SetValue(viewModel, gameFlowService);
        }

        private static ViewFixture CreateViewFixture(MainMenuViewModel viewModel)
        {
            GameObject root = new GameObject("MainMenuViewTestRoot", typeof(RectTransform));

            GameObject goldGo = new GameObject("GoldText", typeof(RectTransform));
            goldGo.transform.SetParent(root.transform, false);
            Type tmpType = Type.GetType("TMPro.TextMeshProUGUI, Unity.TextMeshPro");
            Component goldLabel = tmpType != null
                ? goldGo.AddComponent(tmpType) as Component
                : goldGo.AddComponent<Text>() as Component;

            GameObject addGoldGo = new GameObject("AddGold", typeof(RectTransform), typeof(Image), typeof(Button));
            addGoldGo.transform.SetParent(root.transform, false);
            Button addGoldButton = addGoldGo.GetComponent<Button>();

            GameObject levelHandlerGo = new GameObject("LevelButtonCollectionHandler", typeof(RectTransform));
            levelHandlerGo.transform.SetParent(root.transform, false);
            LevelButtonCollectionHandlerBehaviour levelHandler = levelHandlerGo.AddComponent<LevelButtonCollectionHandlerBehaviour>();

            GameObject levelList = new GameObject("LevelList", typeof(RectTransform));
            levelList.transform.SetParent(levelHandlerGo.transform, false);

            GameObject levelPrefab = new GameObject("LevelButtonPrefab", typeof(RectTransform), typeof(Image), typeof(Button), typeof(MainMenuLevelListItem));
            levelPrefab.transform.SetParent(root.transform, false);
            levelPrefab.SetActive(false);
            Button levelItemButton = levelPrefab.GetComponent<Button>();
            GameObject levelLabelGo = new GameObject("LevelLabel", typeof(RectTransform));
            levelLabelGo.transform.SetParent(levelPrefab.transform, false);
            TMPro.TextMeshProUGUI levelLabel = levelLabelGo.AddComponent<TMPro.TextMeshProUGUI>();
            MainMenuLevelListItem levelListItem = levelPrefab.GetComponent<MainMenuLevelListItem>();
            SetPrivateField(levelListItem, "button", levelItemButton);
            SetPrivateField(levelListItem, "label", levelLabel);

            SetPrivateField(levelHandler, "levelButtonPrefab", levelListItem);
            SetPrivateField(levelHandler, "levelListContainer", levelList.transform);

            GameObject titleGo = new GameObject("TitleText", typeof(RectTransform));
            titleGo.transform.SetParent(root.transform, false);
            TMPro.TextMeshProUGUI titleLabel = titleGo.AddComponent<TMPro.TextMeshProUGUI>();

            GameObject subtitleGo = new GameObject("SubtitleText", typeof(RectTransform));
            subtitleGo.transform.SetParent(root.transform, false);
            TMPro.TextMeshProUGUI subtitleLabel = subtitleGo.AddComponent<TMPro.TextMeshProUGUI>();
            titleLabel.text = "Fuleiro";
            subtitleLabel.text = "(Its a brazilian pun)";

            MainMenuView view = root.AddComponent<MainMenuView>();
            SetMainMenuViewSerializedField(view, "goldLabel", goldLabel);
            SetMainMenuViewSerializedField(view, "addGoldButton", addGoldButton);
            SetMainMenuViewSerializedField(view, "levelButtonCollectionHandler", levelHandler);

            view.Bind(viewModel);
            return new ViewFixture(root, titleLabel, subtitleLabel);
        }

        private static void SetMainMenuViewSerializedField(MainMenuView view, string fieldName, object value)
        {
            FieldInfo field = typeof(MainMenuView).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            field?.SetValue(view, value);
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"Expected field '{fieldName}' on {target.GetType().Name}.");
            field.SetValue(target, value);
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
            if (component == null)
            {
                return false;
            }

            PropertyInfo property = component.GetType().GetProperty("text");
            if (property == null)
            {
                return false;
            }

            string value = property.GetValue(component) as string ?? string.Empty;
            return value.StartsWith("Gold:");
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
            public ViewFixture(GameObject root, TMPro.TextMeshProUGUI titleLabel, TMPro.TextMeshProUGUI subtitleLabel)
            {
                Root = root;
                TitleLabel = titleLabel;
                SubtitleLabel = subtitleLabel;
            }

            public GameObject Root { get; }
            public TMPro.TextMeshProUGUI TitleLabel { get; }
            public TMPro.TextMeshProUGUI SubtitleLabel { get; }

            public void Dispose()
            {
                if (Root == null)
                {
                    return;
                }

                UnityEngine.Object.DestroyImmediate(Root);
            }
        }

        private sealed class FakeGameFlowService : IGameFlowService
        {
            public LevelDefinition LastDefinition { get; private set; }

            public void PlayLevel(AvailableLevel entry)
            {
                LastDefinition = entry?.Definition;
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
