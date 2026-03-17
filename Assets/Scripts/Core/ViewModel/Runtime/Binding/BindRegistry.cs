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
    internal class BindRegistry
    {
        public BindRegistry(BindGroups groups)
        {
            if (groups is null) { throw new ArgumentNullException(nameof(groups)); }
            this.groups = groups;
        }

        private readonly BindGroups groups;
        private readonly Map<string, Type, IBindContext> registeredContexts = new Map<string, Type, IBindContext>();

        public RegistrationContext<TSource> GetOrCreateContext<TSource>(Expression<Func<TSource>> source)
        {
            if (source is null) { throw new ArgumentNullException(nameof(source)); }
            string path = source.GetPropertyName();
            Type type = typeof(TSource);
            BindContext<TSource> context = GetContext(path, type, source);
            return new RegistrationContext<TSource>(path, type, context);
        }

        private BindContext<TSource> GetContext<TSource>(string path, Type type, Expression<Func<TSource>> source)
        {
            if (TryGetContext(path, type, out BindContext<TSource> context)) { return context; }
            return CreateAndRegisterContext(path, type, source);
        }

        private bool TryGetContext<TSource>(string path, Type type, out BindContext<TSource> context)
        {
            if (registeredContexts.TryGetValue(path, type, out IBindContext found))
            {
                context = found as BindContext<TSource>;
                return context != null;
            }

            context = null;
            return false;
        }

        private BindContext<TSource> CreateAndRegisterContext<TSource>(string path, Type type, Expression<Func<TSource>> source)
        {
            Func<TSource> getter = source.Compile();
            BindContext<TSource> context = new BindContext<TSource>(getter);
            registeredContexts.Add(path, type, context);
            groups.Register(path, context);
            return context;
        }

        public void RemoveIfEmpty(string path, Type type, IBindContext context)
        {
            if (path is null) { throw new ArgumentNullException(nameof(path)); }
            if (type is null) { throw new ArgumentNullException(nameof(type)); }
            if (context is null) { throw new ArgumentNullException(nameof(context)); }
            if (!CanRemove(path, type, context)) { return; }
            UnregisterContext(path, type, context);
        }

        private bool CanRemove(string path, Type type, IBindContext context)
        {
            if (context.IsEmpty == false) { return false; }
            if (registeredContexts.TryGetValue(path, type, out IBindContext registered) == false) { return false; }
            return ReferenceEquals(registered, context);
        }

        private void UnregisterContext(string path, Type type, IBindContext context)
        {
            groups.Unregister(path, context);
            registeredContexts.Remove(path, type);
        }

        internal void Clear()
        {
            foreach (IBindContext context in registeredContexts.Values)
            {
                context.Unbind();
            }

            registeredContexts.Clear();
        }
    }

    internal class RegistrationContext<TSource>
    {
        public RegistrationContext(string path, Type sourceType, BindContext<TSource> context)
        {
            if (path is null) { throw new ArgumentNullException(nameof(path)); }
            if (sourceType is null) { throw new ArgumentNullException(nameof(sourceType)); }
            if (context is null) { throw new ArgumentNullException(nameof(context)); }
            Path = path;
            SourceType = sourceType;
            Context = context;
        }

        public string Path { get; }
        public Type SourceType { get; }
        public BindContext<TSource> Context { get; }
    }
}




