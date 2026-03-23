using System;
using System.Collections.Generic;
using Madbox.Scope.Contracts;
using VContainer;

namespace Madbox.Scope
{
    public sealed class CrossLayerObjectResolver : ICrossLayerObjectResolver
    {
        private readonly List<IObjectResolver> resolvers = new List<IObjectResolver>();
        private readonly object gate = new object();

        public void Reset()
        {
            lock (gate)
            {
                resolvers.Clear();
            }
        }

        public void RegisterScope(IObjectResolver resolver)
        {
            if (resolver == null)
            {
                throw new ArgumentNullException(nameof(resolver));
            }

            lock (gate)
            {
                if (resolvers.Contains(resolver))
                {
                    return;
                }

                resolvers.Add(resolver);
            }
        }

        public void Inject(object instance)
        {
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }

            IObjectResolver[] snapshot;
            lock (gate)
            {
                snapshot = resolvers.ToArray();
            }

            Exception lastException = null;
            for (int i = snapshot.Length - 1; i >= 0; i--)
            {
                IObjectResolver resolver = snapshot[i];
                if (resolver == null)
                {
                    continue;
                }

                try
                {
                    resolver.Inject(instance);
                    return;
                }
                catch (Exception exception)
                {
                    lastException = exception;
                }
            }

            throw new InvalidOperationException("Failed to inject the instance from any registered layer resolver.", lastException);
        }

        public T Resolve<T>()
        {
            if (TryResolve(out T instance))
            {
                return instance;
            }

            throw new InvalidOperationException($"Type '{typeof(T).FullName}' could not be resolved from any registered layer.");
        }

        public object Resolve(Type type)
        {
            if (TryResolve(type, out object instance))
            {
                return instance;
            }

            throw new InvalidOperationException($"Type '{type?.FullName}' could not be resolved from any registered layer.");
        }

        public bool TryResolve<T>(out T instance)
        {
            if (TryResolve(typeof(T), out object resolved))
            {
                instance = (T)resolved;
                return true;
            }

            instance = default;
            return false;
        }

        public bool TryResolve(Type type, out object instance)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            IObjectResolver[] snapshot;
            lock (gate)
            {
                snapshot = resolvers.ToArray();
            }

            for (int i = snapshot.Length - 1; i >= 0; i--)
            {
                IObjectResolver resolver = snapshot[i];
                if (resolver == null)
                {
                    continue;
                }

                try
                {
                    instance = resolver.Resolve(type);
                    return true;
                }
                catch (VContainerException)
                {
                }
            }

            instance = null;
            return false;
        }
    }
}
