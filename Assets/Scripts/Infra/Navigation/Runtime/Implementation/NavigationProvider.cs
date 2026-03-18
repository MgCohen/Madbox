using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Madbox.Addressables.Contracts;
using Scaffold.Navigation.Contracts;
using UnityEngine;

namespace Scaffold.Navigation
{
    internal class NavigationProvider
    {
        public NavigationProvider(NavigationSettings settings, Transform viewHolder, IAddressablesGateway addressables)
        {
            if (settings is null) { throw new ArgumentNullException(nameof(settings)); }
            if (viewHolder is null) { throw new ArgumentNullException(nameof(viewHolder)); }
            if (addressables is null) { throw new ArgumentNullException(nameof(addressables)); }
            this.settings = settings;
            this.viewHolder = viewHolder;
            this.addressables = addressables;
            FetchContextViews();
        }

        private readonly Transform viewHolder;
        private readonly NavigationSettings settings;
        private readonly IAddressablesGateway addressables;
        private readonly Dictionary<Type, IView> contextViews = new Dictionary<Type, IView>();

        public NavigationPoint GetNavigationPoint<TController>(TController controller, NavigationOptions options) where TController : IViewController
        {
            GuardController(controller);
            ViewConfig config = settings.GetViewConfig(typeof(TController));
            NavigationOptions resolved = ValidateNavigationOptions(options, config);
            return GetNavigationPoint(config, controller, resolved);
        }

        private NavigationPoint GetNavigationPoint(ViewConfig config, IViewController controller, NavigationOptions options)
        {
            if (TryGetContextView(config.ViewType, out IView view))
            {
                return new NavigationPoint(view, controller, config, true, options);
            }
            return CreateAssetNavigationPoint(config, controller, options);
        }

        private NavigationPoint CreateAssetNavigationPoint(ViewConfig config, IViewController controller, NavigationOptions options)
        {
            if (!TryGetAssetScreen(config, out IView view, out IAssetHandle<GameObject> handle)) { return null; }
            return new NavigationPoint(view, controller, config, false, options, handle);
        }

        private bool TryGetAssetScreen(ViewConfig config, out IView screen, out IAssetHandle<GameObject> handle)
        {
            handle = LoadPreloadedHandle(config);
            GameObject instance = CreateViewInstance(handle.Asset);
            screen = ResolveView(instance, handle);
            SetViewInactive(instance);
            return true;
        }

        private IAssetHandle<GameObject> LoadPreloadedHandle(ViewConfig config)
        {
            EnsureAssetReference(config);
            Task<IAssetHandle<GameObject>> task = addressables.LoadAsync<GameObject>(config.Asset);
            if (!task.IsCompleted)
            {
                throw new InvalidOperationException("Navigation view asset was not preloaded. Register view addressables with NeverDie preload mode before opening views.");
            }
            return task.GetAwaiter().GetResult();
        }

        private GameObject CreateViewInstance(GameObject prefab)
        {
            return GameObject.Instantiate(prefab, viewHolder);
        }

        private void EnsureAssetReference(ViewConfig config)
        {
            if (config.Asset == null)
            {
                throw new InvalidOperationException("Navigation view config is missing addressable reference.");
            }
        }

        private IView ResolveView(GameObject instance, IAssetHandle<GameObject> handle)
        {
            IView screen = instance.GetComponent<IView>();
            if (screen != null) { return screen; }
            UnityEngine.Object.Destroy(instance);
            handle.Release();
            throw new InvalidOperationException($"Addressable view '{instance.name}' does not implement {nameof(IView)}.");
        }

        private void SetViewInactive(GameObject instance)
        {
            instance.SetActive(false);
        }

        private void FetchContextViews()
        {
            IView[] views = viewHolder.GetComponentsInChildren<IView>(true);
            foreach (IView view in views)
            {
                contextViews[view.GetType()] = view;
                view.gameObject.SetActive(false);
            }
        }

        private NavigationOptions ValidateNavigationOptions(NavigationOptions options, ViewConfig config)
        {
            if (options != null) { return options; }
            return ResolveDefaultOptions(config);
        }

        private NavigationOptions ResolveDefaultOptions(ViewConfig config)
        {
            if (config.TryGetSchema<NavigationOptionsSchema>(out NavigationOptionsSchema schema))
            {
                return schema.Options;
            }
            return new NavigationOptions();
        }

        private bool TryGetContextView(Type screenType, out IView screen)
        {
            if (screenType != null && contextViews.TryGetValue(screenType, out IView screenInstance))
            {
                screen = screenInstance;
                return true;
            }
            screen = null;
            return false;
        }

        private void GuardController(IViewController controller)
        {
            if (controller == null) { throw new ArgumentNullException(nameof(controller)); }
        }
    }
}
