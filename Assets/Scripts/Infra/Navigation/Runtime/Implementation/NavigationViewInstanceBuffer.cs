using System;
using System.Collections.Generic;
using Scaffold.Navigation.Contracts;
using UnityEngine;

namespace Scaffold.Navigation
{
    internal sealed class NavigationViewInstanceBuffer
    {
        public NavigationViewInstanceBuffer(Transform viewHolder, int maxPerView = 2)
        {
            if (viewHolder == null) { throw new ArgumentNullException(nameof(viewHolder)); }
            this.viewHolder = viewHolder;
            this.maxPerView = Math.Max(1, maxPerView);
        }

        private readonly Transform viewHolder;
        private readonly int maxPerView;
        private readonly Dictionary<ViewConfig, Stack<IView>> byConfig = new Dictionary<ViewConfig, Stack<IView>>();

        public bool TryTake(ViewConfig config, out IView view)
        {
            if (!TryValidateConfig(config, out view)) { return false; }
            if (!byConfig.TryGetValue(config, out Stack<IView> pool)) { view = null; return false; }
            return TryTakeValid(pool, out view);
        }

        public void Return(ViewConfig config, IView view)
        {
            if (!CanReturn(config, view)) { return; }
            Stack<IView> pool = GetOrCreatePool(config);
            if (ShouldDestroy(pool)) { DestroyView(view); return; }
            CacheView(view);
            pool.Push(view);
        }

        private bool TryValidateConfig(ViewConfig config, out IView view)
        {
            view = null;
            return IsValidConfig(config);
        }

        private bool IsValidConfig(ViewConfig config)
        {
            return config != null;
        }

        private bool CanReturn(ViewConfig config, IView view)
        {
            if (config == null || view == null) { return false; }
            return view.gameObject != null;
        }

        private bool ShouldDestroy(Stack<IView> pool)
        {
            return pool.Count >= maxPerView;
        }

        private void CacheView(IView view)
        {
            view.gameObject.transform.SetParent(viewHolder, false);
            view.gameObject.SetActive(false);
        }

        private void DestroyView(IView view)
        {
            UnityEngine.Object.Destroy(view.gameObject);
        }

        private Stack<IView> GetOrCreatePool(ViewConfig config)
        {
            if (byConfig.TryGetValue(config, out Stack<IView> pool)) { return pool; }
            Stack<IView> created = new Stack<IView>();
            byConfig[config] = created;
            return created;
        }

        private bool TryTakeValid(Stack<IView> pool, out IView view)
        {
            while (pool.Count > 0)
            {
                if (TryPopValidView(pool, out view)) { return true; }
            }
            view = null;
            return false;
        }

        private bool TryPopValidView(Stack<IView> pool, out IView view)
        {
            IView cached = pool.Pop();
            if (cached == null || cached.gameObject == null) { view = null; return false; }
            view = cached;
            return true;
        }
    }
}
