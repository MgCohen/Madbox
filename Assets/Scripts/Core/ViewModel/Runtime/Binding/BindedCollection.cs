using System.Linq.Expressions;
using System.Collections.Specialized;
using System.Collections.ObjectModel;
using System.Collections;
using Scaffold.Maps;
using CommunityToolkit.Mvvm.ComponentModel;
using UnityEngine;
using Scaffold.Navigation.Contracts;
using Scaffold.MVVM.Contracts;
using System.ComponentModel;
using System.Linq;
using System.Collections.Generic;
using System;
namespace Scaffold.MVVM.Binding
{
    internal class BindedCollection<TSource, TTarget> : IBindedCollection<TSource, TTarget>, IBind<ICollection<TSource>>
    {
        public BindedCollection(BindSet<TSource, TTarget> binding, ICollectionHandler<TSource, TTarget> handler, Action detach)
        {
            if (binding is null)
{
    throw new ArgumentNullException(nameof(binding));
}
            if (handler is null)
{
    throw new ArgumentNullException(nameof(handler));
}
            this.handler = handler;
            this.detach = detach;
        }

        private Dictionary<TSource, List<TTarget>> lookup = new Dictionary<TSource, List<TTarget>>();
        private ICollectionHandler<TSource, TTarget> handler;
        private Action detach;
        private ICollection<TSource> source;
        private bool disposed;

        public void Update(ICollection<TSource> value)
        {
            if (ReferenceEquals(source, value)) return;

            if (source is INotifyCollectionChanged oldObservable)
            {
                oldObservable.CollectionChanged -= HandleCollectionChanges;
            }

            AttachAndSeed(value);
            source = value;
        }

        private void AttachAndSeed(ICollection<TSource> value)
        {
            if (value is INotifyCollectionChanged newObservable)
            {
                newObservable.CollectionChanged -= HandleCollectionChanges;
                newObservable.CollectionChanged += HandleCollectionChanges;
            }

            if (value == null) return;
            SeedExistingItems(value);
        }

        private void SeedExistingItems(ICollection<TSource> value)
        {
            foreach (var item in value)
            {
                if (!lookup.TryGetValue(item, out List<TTarget> list))
                {
                    list = new List<TTarget>();
                    lookup[item] = list;
                }
                TTarget target = handler.Add(item);
                list.Add(target);
            }
        }

        public void HandleCollectionChanges(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e is null) throw new ArgumentNullException(nameof(e));
            ApplyCollectionItems(e.OldItems, false);
            ApplyCollectionItems(e.NewItems, true);
        }

        private void ApplyCollectionItems(IList items, bool addItems)
        {
            if (items == null) return;
            foreach (var item in items)
            {
                TSource sourceItem = (TSource)item;
                UpdateLookupItem(sourceItem, addItems);
            }
        }

        private void UpdateLookupItem(TSource sourceItem, bool addItems)
        {
            if (addItems)
            {
                List<TTarget> list = GetOrCreateTargets(sourceItem);
                TTarget target = handler.Add(sourceItem);
                list.Add(target);
                return;
            }

            if (!lookup.TryGetValue(sourceItem, out List<TTarget> existing) || existing.Count == 0) return;
            int lastIndex = existing.Count - 1;
            TTarget removed = existing[lastIndex];
            existing.RemoveAt(lastIndex);
            handler.Remove(removed);
        }

        private List<TTarget> GetOrCreateTargets(TSource sourceItem)
        {
            if (lookup.TryGetValue(sourceItem, out List<TTarget> list)) return list;
            list = new List<TTarget>();
            lookup[sourceItem] = list;
            return list;
        }

        public void Dispose()
        {
            if (disposed)
{
    return;
}
            disposed = true;

            if (source is INotifyCollectionChanged observable)
            {
                observable.CollectionChanged -= HandleCollectionChanges;
            }

            source = null;
            detach?.Invoke();
            detach = null;
        }

        public void Update()
        {
            if (source == null)
{
    return;
}
            Debug.Log("Collection Changed");
        }
    }
}
