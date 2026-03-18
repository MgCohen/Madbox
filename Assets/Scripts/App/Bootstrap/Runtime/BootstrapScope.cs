using System;
using System.Collections.Generic;
using System.Reflection;
using Madbox.App.MainMenu;
using Madbox.Scope;
using Madbox.Scope.Contracts;
using Scaffold.Navigation;
using Scaffold.Navigation.Contracts;
using Scaffold.Types;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Madbox.App.Bootstrap
{
    public sealed class BootstrapScope : LayeredScope
    {
        [SerializeField] private NavigationSettings navigationSettings;
        [SerializeField] private Transform viewHolder;

        protected override void ValidateBootstrapState()
        {
            ValidateSerializedFields();
        }

        protected override IReadOnlyList<ILayerInstaller> BuildLayerInstallers()
        {
            return new ILayerInstaller[]
            {
                new BootstrapAssetInstaller(),
                new BootstrapInfraInstaller(navigationSettings, viewHolder)
            };
        }

        protected override void OnBootstrapCompleted(LifetimeScope finalScope)
        {
            Debug.Log("Bootstrap completed");
            EnsureMainMenuConfigured();
            OpenMainMenu(finalScope);
        }

        private void EnsureMainMenuConfigured()
        {
            if (navigationSettings == null) { return; }
            if (HasMainMenuConfig()) { return; }
            ViewConfig config = CreateMainMenuConfig();
            AddScreenConfig(config);
        }

        private bool HasMainMenuConfig()
        {
            IReadOnlyList<ViewConfig> screens = navigationSettings.Screens;
            if (screens == null) { return false; }
            return ContainsControllerType(screens, typeof(MainMenuViewModel));
        }

        private bool ContainsControllerType(IReadOnlyList<ViewConfig> screens, Type controllerType)
        {
            if (screens == null) { return false; }
            for (int i = 0; i < screens.Count; i++)
            {
                if (HasControllerType(screens[i], controllerType)) { return true; }
            }

            return false;
        }

        private bool HasControllerType(ViewConfig screen, Type controllerType)
        {
            if (screen == null) { return false; }
            Type currentType = ResolveControllerType(screen);
            return currentType == controllerType;
        }

        private Type ResolveControllerType(ViewConfig screen)
        {
            try
            {
                return screen.ControllerType;
            }
            catch (NullReferenceException)
            {
                return null;
            }
        }

        private ViewConfig CreateMainMenuConfig()
        {
            ViewConfig config = ScriptableObject.CreateInstance<ViewConfig>();
            object assetReference = CreateAssetReference("d1d8d7d8e4f24e6aa0aa0c1c4c5d9e41");
            TypeReference viewType = new TypeReference(typeof(MainMenuView));
            TypeReference controllerType = new TypeReference(typeof(MainMenuViewModel));
            SetViewConfigField(config, "asset", assetReference);
            SetViewConfigField(config, "viewType", viewType);
            SetViewConfigField(config, "controllerType", controllerType);
            return config;
        }

        private void AddScreenConfig(ViewConfig config)
        {
            FieldInfo field = ResolveScreensField();
            if (field == null) { return; }
            List<ViewConfig> screens = field.GetValue(navigationSettings) as List<ViewConfig>;
            if (screens == null) { return; }
            screens.Add(config);
        }

        private FieldInfo ResolveScreensField()
        {
            BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            return typeof(NavigationSettings).GetField("screens", flags);
        }

        private void SetViewConfigField(ViewConfig config, string name, object value)
        {
            BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            FieldInfo field = typeof(ViewConfig).GetField(name, flags);
            if (field == null) { return; }
            field.SetValue(config, value);
        }

        private object CreateAssetReference(string guid)
        {
            Type assetReferenceType = Type.GetType("UnityEngine.AddressableAssets.AssetReference, Unity.Addressables");
            if (assetReferenceType == null) { return null; }
            return Activator.CreateInstance(assetReferenceType, guid);
        }

        private void OpenMainMenu(LifetimeScope finalScope)
        {
            if (finalScope == null || finalScope.Container == null) { return; }
            INavigation navigation = finalScope.Container.Resolve<INavigation>();
            MainMenuViewModel mainMenu = new MainMenuViewModel();
            navigation.Open(mainMenu, closeCurrent: true);
        }

        private void ValidateSerializedFields()
        {
            EnsureNavigationSettings();
            EnsureViewHolder();
        }

        private void EnsureNavigationSettings()
        {
            if (navigationSettings != null) { return; }
            throw new InvalidOperationException("BootstrapScope requires NavigationSettings.");
        }

        private void EnsureViewHolder()
        {
            if (viewHolder != null) { return; }
            throw new InvalidOperationException("BootstrapScope requires a view holder Transform.");
        }
    }
}
