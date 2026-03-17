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
    public class BindContext<T> : IBindContext
    {
        public BindContext(Func<T> getter)
        {
            if (getter is null) { throw new ArgumentNullException(nameof(getter)); }
            source = getter;
        }

        public bool IsEmpty => binds.Count == 0;

        private Func<T> source;
        private readonly List<BindRegistration> binds = new List<BindRegistration>();

        public void Bind(IBind<T> binding, BindingOptions options)
        {
            if (binding is null) { throw new ArgumentNullException(nameof(binding)); }
            BindRegistration registration = new BindRegistration(binding, options);
            binds.Add(registration);
            ApplyImmediateUpdate(registration, binding);
        }

        private void ApplyImmediateUpdate(BindRegistration registration, IBind<T> binding)
        {
            if (registration.Options.LazyEvaluation) { return; }
            T value = GetValue();
            binding.Update(value);
        }

        public void Update()
        {
            if (!CanUpdate()) { return; }
            if (!TryGetUpdateValue(out T value)) { return; }
            UpdateAllBinds(value);
        }

        private bool CanUpdate()
        {
            return source != null && binds.Count > 0;
        }

        private bool TryGetUpdateValue(out T value)
        {
            try { value = GetValue(); return true; }
            catch (NullReferenceException ex) { return HandleMissingNestedValue(ex, out value); }
        }

        private bool HandleMissingNestedValue(NullReferenceException ex, out T value)
        {
            value = default;
            if (HasStrictBind()) { throw ex; }
            return false;
        }

        private void UpdateAllBinds(T value)
        {
            foreach (BindRegistration bind in binds)
            {
                bind.Bind.Update(value);
            }
        }

        public void Unbind(IBind<T> binding)
        {
            if (binding is null) { throw new ArgumentNullException(nameof(binding)); }
            int index = FindBindIndex(binding);
            if (index >= 0) { binds.RemoveAt(index); }
        }

        private int FindBindIndex(IBind<T> binding)
        {
            for (int i = binds.Count - 1; i >= 0; i--)
            {
                if (ReferenceEquals(binds[i].Bind, binding)) { return i; }
            }

            return -1;
        }

        private bool HasStrictBind()
        {
            return binds.Exists(bind => bind.Options.LazyEvaluation == false);
        }

        private T GetValue()
        {
            return source();
        }

        public void Unbind()
        {
            if (!CanUnbind()) { return; }
            source = null;
            DisposeBinds();
            binds.Clear();
        }

        private bool CanUnbind()
        {
            return source != null || binds.Count > 0;
        }

        private void DisposeBinds()
        {
            foreach (BindRegistration bind in binds)
            {
                if (bind.Bind is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
        }

        private sealed class BindRegistration
        {
            public BindRegistration(IBind<T> bind, BindingOptions options)
            {
                if (bind is null) { throw new ArgumentNullException(nameof(bind)); }
                Bind = bind;
                Options = options ?? BindingOptions.Strict;
            }

            public IBind<T> Bind { get; }
            public BindingOptions Options { get; }
        }
    }
}




