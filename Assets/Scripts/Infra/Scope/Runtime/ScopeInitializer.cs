using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Madbox.Scope.Contracts;
using VContainer;
using VContainer.Unity;
#pragma warning disable SCA0002
#pragma warning disable SCA0005
#pragma warning disable SCA0006
#pragma warning disable SCA0012

namespace Madbox.Scope
{
    public sealed class ScopeInitializer
    {
        private readonly HashSet<IAsyncLayerInitializable> initialized = new HashSet<IAsyncLayerInitializable>();
        private readonly List<DelegatedChildRegistration> delegatedChildRegistrations = new List<DelegatedChildRegistration>();
        private readonly object sync = new object();

        public void Reset()
        {
            lock (sync)
            {
                initialized.Clear();
                delegatedChildRegistrations.Clear();
            }
        }

        public void ApplyDelegatedChildRegistrations(IContainerBuilder builder, IObjectResolver parentResolver)
        {
            if (builder == null) { throw new ArgumentNullException(nameof(builder)); }
            IReadOnlyList<DelegatedChildRegistration> snapshot = TakeRegistrationsSnapshotAndPrune(parentResolver);
            foreach (DelegatedChildRegistration registration in snapshot)
            {
                registration.ApplyTo(builder);
            }
        }

        public void ApplyDelegatedChildRegistrations(IContainerBuilder builder)
        {
            ApplyDelegatedChildRegistrations(builder, null);
        }

        public async Task InitializeScopeAsync(LifetimeScope scope, CancellationToken cancellationToken)
        {
            EnsureNotCanceled(cancellationToken);
            IReadOnlyList<IAsyncLayerInitializable> resolved = ResolveInitializersFromScope(scope);
            IReadOnlyList<IAsyncLayerInitializable> pending = FilterPendingInitializers(resolved);
            IObjectResolver resolver = scope == null ? null : scope.Container;
            await InitializePendingAsync(pending, resolver, cancellationToken);
            RememberInitialized(pending);
        }

        public async Task InitializeInitializersAsync(IReadOnlyList<IAsyncLayerInitializable> initializers, IObjectResolver resolver, CancellationToken cancellationToken)
        {
            EnsureNotCanceled(cancellationToken);
            IReadOnlyList<IAsyncLayerInitializable> pending = FilterPendingInitializers(initializers);
            await InitializePendingAsync(pending, resolver, cancellationToken);
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

        private async Task InitializePendingAsync(IReadOnlyList<IAsyncLayerInitializable> pending, IObjectResolver resolver, CancellationToken cancellationToken)
        {
            if (pending.Count == 0) { return; }
            Task[] tasks = BuildInitializationTasks(pending, resolver, cancellationToken);
            await Task.WhenAll(tasks);
        }

        private Task[] BuildInitializationTasks(IReadOnlyList<IAsyncLayerInitializable> pending, IObjectResolver resolver, CancellationToken cancellationToken)
        {
            Task[] tasks = new Task[pending.Count];
            LayerInitializationContext context = new LayerInitializationContext(registration => RegisterDelegatedChildRegistration(registration, resolver));
            for (int i = 0; i < pending.Count; i++) { tasks[i] = InitializeServiceAsync(context, pending[i], resolver, cancellationToken); }
            return tasks;
        }

        private async Task InitializeServiceAsync(ILayerInitializationContext context, IAsyncLayerInitializable initializer, IObjectResolver resolver, CancellationToken cancellationToken)
        {
            try { await initializer.InitializeAsync(context, resolver, cancellationToken); }
            catch (OperationCanceledException) { throw; }
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

        private void RegisterDelegatedChildRegistration(DelegatedChildRegistration registration, IObjectResolver ownerResolver)
        {
            lock (sync)
            {
                DelegatedChildRegistration ownedRegistration = new DelegatedChildRegistration(registration.Policy, registration.ApplyTo, ownerResolver);
                delegatedChildRegistrations.Add(ownedRegistration);
            }
        }

        private IReadOnlyList<DelegatedChildRegistration> TakeRegistrationsSnapshotAndPrune(IObjectResolver parentResolver)
        {
            lock (sync)
            {
                if (delegatedChildRegistrations.Count == 0) { return Array.Empty<DelegatedChildRegistration>(); }
                List<DelegatedChildRegistration> snapshot = delegatedChildRegistrations
                    .Where(registration => registration.IsApplicableTo(parentResolver))
                    .ToList();
                delegatedChildRegistrations.RemoveAll(registration =>
                    registration.IsApplicableTo(parentResolver) &&
                    registration.Policy == ChildScopeDelegationPolicy.NextChildOnly);
                return snapshot;
            }
        }
    }
}
#pragma warning restore SCA0012
#pragma warning restore SCA0006
#pragma warning restore SCA0005
#pragma warning restore SCA0002
