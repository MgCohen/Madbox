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

        public void ApplyDelegatedChildRegistrations(IContainerBuilder builder, IObjectResolver parentResolver = null)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));
            IReadOnlyList<DelegatedChildRegistration> snapshot = TakeRegistrationsSnapshotAndPrune(parentResolver);
            foreach (DelegatedChildRegistration registration in snapshot)
            {
                registration.ApplyTo(builder);
            }
        }

        private IReadOnlyList<DelegatedChildRegistration> TakeRegistrationsSnapshotAndPrune(IObjectResolver parentResolver)
        {
            lock (sync)
            {
                if (delegatedChildRegistrations.Count == 0)
{
    return Array.Empty<DelegatedChildRegistration>();
}
                List<DelegatedChildRegistration> snapshot = delegatedChildRegistrations.Where(registration => registration.IsApplicableTo(parentResolver)).ToList();
                delegatedChildRegistrations.RemoveAll(registration =>
                    registration.IsApplicableTo(parentResolver) &&
                    registration.Policy == ChildScopeDelegationPolicy.NextChildOnly);
                return snapshot;
            }
        }

        public async Task InitializeScopeAsync(LifetimeScope scope, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested(); IReadOnlyList<IAsyncLayerInitializable> resolved = Array.Empty<IAsyncLayerInitializable>();
            if (scope != null)
            {
                try { IEnumerable<IAsyncLayerInitializable> initializers = scope.Container.Resolve<IEnumerable<IAsyncLayerInitializable>>(); resolved = initializers?.Where(initializer => initializer != null).ToArray() ?? Array.Empty<IAsyncLayerInitializable>(); }
                catch (VContainerException) { resolved = Array.Empty<IAsyncLayerInitializable>(); }
            }
            List<IAsyncLayerInitializable> pending = resolved.Where(initializer => initializer != null && !initialized.Contains(initializer)).ToList();
            IObjectResolver resolver = scope == null ? null : scope.Container; await InitializePendingAsync(pending, resolver, cancellationToken); initialized.UnionWith(pending);
        }

        public async Task InitializeInitializersAsync(IReadOnlyList<IAsyncLayerInitializable> initializers, IObjectResolver resolver, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            List<IAsyncLayerInitializable> pending = new List<IAsyncLayerInitializable>();
            foreach (IAsyncLayerInitializable initializer in initializers)
            {
                if (initializer == null || initialized.Contains(initializer)) continue;
                pending.Add(initializer);
            }
            await InitializePendingAsync(pending, resolver, cancellationToken);
            foreach (IAsyncLayerInitializable initializer in pending)
            {
                initialized.Add(initializer);
            }
        }

        private async Task InitializePendingAsync(IReadOnlyList<IAsyncLayerInitializable> pending, IObjectResolver resolver, CancellationToken cancellationToken)
        {
            if (pending.Count == 0)
{
    return;
}
            Task[] tasks = BuildInitializationTasks(pending, resolver, cancellationToken);
            await Task.WhenAll(tasks);
        }

        private Task[] BuildInitializationTasks(IReadOnlyList<IAsyncLayerInitializable> pending, IObjectResolver resolver, CancellationToken cancellationToken)
        {
            Task[] tasks = new Task[pending.Count]; LayerInitializationContext context = new LayerInitializationContext(registration => RegisterDelegatedChildRegistration(registration, resolver));
            for (int i = 0; i < pending.Count; i++)
            {
                tasks[i] = InitializeSingleAsync(pending[i]);
            }
            return tasks;
            async Task InitializeSingleAsync(IAsyncLayerInitializable initializer)
            {
                try { await initializer.InitializeAsync(context, resolver, cancellationToken); }
                catch (OperationCanceledException) { throw; }
                catch (Exception exception) { throw new InvalidOperationException($"Initialization failed in '{initializer.GetType().FullName}'.", exception); }
            }
        }

        private void RegisterDelegatedChildRegistration(DelegatedChildRegistration registration, IObjectResolver ownerResolver)
        {
            lock (sync)
            {
                DelegatedChildRegistration ownedRegistration = new DelegatedChildRegistration(registration.Policy, registration.ApplyTo, ownerResolver);
                delegatedChildRegistrations.Add(ownedRegistration);
            }
        }

    }
}
