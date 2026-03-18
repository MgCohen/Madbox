using System;
using Madbox.Gold.Contracts;
using NUnit.Framework;
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
            Component label = ResolveGoldLabel(fixture.Root);
            Assert.IsNotNull(label);
            string text = ReadTextValue(label);
            Assert.AreEqual("Gold: 2", text);
        }

        private MainMenuViewModel CreateBoundViewModel(FakeGoldService service)
        {
            MainMenuViewModel viewModel = new MainMenuViewModel();
            viewModel.Construct(service);
            viewModel.Bind(null);
            return viewModel;
        }

        private ViewFixture CreateViewFixture(MainMenuViewModel viewModel)
        {
            GameObject root = new GameObject("MainMenuViewTestRoot", typeof(RectTransform));
            MainMenuView view = root.AddComponent<MainMenuView>();
            view.Bind(viewModel);
            return new ViewFixture(root);
        }

        private Component ResolveGoldLabel(GameObject root)
        {
            Component[] components = root.GetComponentsInChildren<Component>(true);
            return FindGoldLabel(components);
        }

        private Component FindGoldLabel(Component[] components)
        {
            for (int i = 0; i < components.Length; i++)
            {
                if (IsGoldLabel(components[i])) { return components[i]; }
            }
            return null;
        }

        private bool IsGoldLabel(Component component)
        {
            if (!IsTmpLabel(component)) { return false; }
            string value = ReadTextValue(component);
            return value.StartsWith("Gold:");
        }

        private bool IsTmpLabel(Component component)
        {
            return component != null && component.GetType().Name == "TextMeshProUGUI";
        }

        private string ReadTextValue(Component component)
        {
            if (component == null) { return string.Empty; }
            var property = component.GetType().GetProperty("text");
            if (property == null) { return string.Empty; }
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
                if (Root == null) { return; }
                UnityEngine.Object.DestroyImmediate(Root);
            }
        }

        private sealed class FakeGoldService : IGoldService
        {
            public FakeGoldService(int initialGold)
            {
                CurrentGold = initialGold;
            }

            public int CurrentGold { get; private set; }

            public event Action<int> GoldChanged;

            public void Add(int amount)
            {
                CurrentGold += amount;
                GoldChanged?.Invoke(CurrentGold);
            }
        }
    }
}
