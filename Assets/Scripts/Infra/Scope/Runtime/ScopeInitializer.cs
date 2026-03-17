using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Madbox.Scope.Contracts;
using VContainer;
using VContainer.Unity;

namespace Madbox.Scope
{
    public sealed class ScopeInitializer
    {
        private readonly HashSet<IAsyncLayerInitializable> initialized = new HashSet<IAsyncLayerInitializable>();

        public void Reset()
        {
            initialized.Clear();
        }

        public async Task InitializeScopeAsync(LifetimeScope scope, CancellationToken cancellationToken)
        {
            EnsureNotCanceled(cancellationToken);
            IReadOnlyList<IAsyncLayerInitializable> resolved = ResolveInitializersFromScope(scope);
            IReadOnlyList<IAsyncLayerInitializable> pending = FilterPendingInitializers(resolved);
            await InitializePendingAsync(pending, cancellationToken);
            RememberInitialized(pending);
        }

        public async Task InitializeInitializersAsync(IReadOnlyList<IAsyncLayerInitializable> initializers, CancellationToken cancellationToken)
        {
            EnsureNotCanceled(cancellationToken);
            IReadOnlyList<IAsyncLayerInitializable> pending = FilterPendingInitializers(initializers);
            await InitializePendingAsync(pending, cancellationToken);
            RememberInitialized(pending);
        }

        private void EnsureNotCanceled(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
        }

        private IReadOnlyList<IAsyncLayerInitializable> ResolveInitializersFromScope(LifetimeScope scope)
        {
            if (scope == null) { return Array.Empty<IAsyncLayerInitializable>(); }
            return TryResolveInitializers(scope, out IReadOnlyList<IAsyncLayerInitializable> resolved)
                ? resolved
                : Array.Empty<IAsyncLayerInitializable>();
        }

        private bool TryResolveInitializers(LifetimeScope scope, out IReadOnlyList<IAsyncLayerInitializable> resolved)
        {
            try { resolved = ResolveInitializers(scope); return true; }
            catch (VContainerException) { resolved = Array.Empty<IAsyncLayerInitializable>(); return false; }
        }

        private IReadOnlyList<IAsyncLayerInitializable> ResolveInitializers(LifetimeScope scope)
        {
            IEnumerable<IAsyncLayerInitializable> initializers = scope.Container.Resolve<IEnumerable<IAsyncLayerInitializable>>();
            return initializers?.Where(initializer => initializer != null).ToArray() ?? Array.Empty<IAsyncLayerInitializable>();
        }

        private IReadOnlyList<IAsyncLayerInitializable> FilterPendingInitializers(IReadOnlyList<IAsyncLayerInitializable> initializers)
        {
            List<IAsyncLayerInitializable> pending = new List<IAsyncLayerInitializable>();
            foreach (IAsyncLayerInitializable initializer in initializers) { AddIfPending(pending, initializer); }
            return pending;
        }

        private void AddIfPending(ICollection<IAsyncLayerInitializable> pending, IAsyncLayerInitializable initializer)
        {
            if (initializer == null || initialized.Contains(initializer)) { return; }
            pending.Add(initializer);
        }

        private async Task InitializePendingAsync(IReadOnlyList<IAsyncLayerInitializable> pending, CancellationToken cancellationToken)
        {
            if (pending.Count == 0) { return; }
            Task[] tasks = BuildInitializationTasks(pending, cancellationToken);
            await Task.WhenAll(tasks);
        }

        private Task[] BuildInitializationTasks(IReadOnlyList<IAsyncLayerInitializable> pending, CancellationToken cancellationToken)
        {
            Task[] tasks = new Task[pending.Count];
            for (int i = 0; i < pending.Count; i++) { tasks[i] = InitializeServiceAsync(pending[i], cancellationToken); }
            return tasks;
        }

        private async Task InitializeServiceAsync(IAsyncLayerInitializable initializer, CancellationToken cancellationToken)
        {
            try { await initializer.InitializeAsync(cancellationToken); }
            catch (Exception exception) { throw CreateInitializationFailure(initializer, exception); }
        }

        private InvalidOperationException CreateInitializationFailure(IAsyncLayerInitializable initializer, Exception exception)
        {
            string message = $"Initialization failed in '{initializer.GetType().FullName}'.";
            return new InvalidOperationException(message, exception);
        }

        private void RememberInitialized(IReadOnlyList<IAsyncLayerInitializable> initializers)
        {
            foreach (IAsyncLayerInitializable initializer in initializers) { initialized.Add(initializer); }
        }
    }
}
