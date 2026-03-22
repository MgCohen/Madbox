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
    public abstract class LayerInstallerBase
    {
        private readonly List<LayerInstallerBase> children = new List<LayerInstallerBase>();
        private HashSet<IAsyncLayerInitializable> initializedRegistry = new HashSet<IAsyncLayerInitializable>(ReferenceComparer<IAsyncLayerInitializable>.Instance);
        private LayerInstallerBase parent;
        private LifetimeScope currentScope;
        private LifetimeScope finalScope;
        private LayerBuildProgressContext progressContext;

        public IReadOnlyList<LayerInstallerBase> Children => children;

        public LayerInstallerBase AddChild(LayerInstallerBase child)
        {
            if (child == null)
            {
                throw new ArgumentNullException(nameof(child));
            }

            if (ReferenceEquals(child, this))
            {
                throw new InvalidOperationException("A layer cannot be added as its own child.");
            }

            if (child.parent != null)
            {
                throw new InvalidOperationException("A layer cannot have more than one parent.");
            }

            if (ContainsInSubtree(child))
            {
                throw new InvalidOperationException("The child is already part of this tree.");
            }

            if (child.ContainsInSubtree(this))
            {
                throw new InvalidOperationException("Adding this child would create a cycle in the layer tree.");
            }

            child.parent = this;
            children.Add(child);
            return this;
        }

        public Task BuildAsRootAsync(LifetimeScope rootScope, CancellationToken cancellationToken, ILayeredScopeProgress progress = null)
        {
            if (rootScope == null)
            {
                throw new ArgumentNullException(nameof(rootScope));
            }

            AssignRegistry(new HashSet<IAsyncLayerInitializable>(ReferenceComparer<IAsyncLayerInitializable>.Instance));
            int totalLayers = CountLayerNodes();
            AssignProgressContext(progress != null ? new LayerBuildProgressContext(progress, totalLayers) : null);
            return BuildAsRootInternalAsync(rootScope, cancellationToken);
        }

        public virtual void Reset()
        {
            progressContext = null;
            currentScope = null;
            finalScope = null;

            for (int i = 0; i < children.Count; i++)
            {
                children[i].Reset();
            }

            if (parent == null)
            {
                initializedRegistry.Clear();
            }
        }

        internal LifetimeScope GetFinalScope()
        {
            return finalScope;
        }

        protected Task BuildAsync(CancellationToken cancellationToken)
        {
            return ExecuteBuildPipelineAsync(cancellationToken);
        }

        protected abstract void Install(IContainerBuilder builder);

        /// <summary>
        /// Invokes <see cref="IInstaller.Install"/> on the given installer. Use from <see cref="Install(IContainerBuilder)"/> overrides to avoid repeating <c>new XInstaller().Install(builder)</c>.
        /// </summary>
        protected static void Install(IContainerBuilder builder, IInstaller installer)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (installer == null)
            {
                throw new ArgumentNullException(nameof(installer));
            }

            installer.Install(builder);
        }

        protected virtual void ConfigureChildBuilder(LayerInstallerBase child, IObjectResolver parentResolver, IContainerBuilder childBuilder)
        {
        }

        protected virtual Task InitializeAsync(IObjectResolver resolver, CancellationToken cancellationToken)
        {
            return InitializeResolvedInitializersAsync(resolver, cancellationToken);
        }

        protected virtual Task OnCompletedAsync(IObjectResolver resolver, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        protected virtual async Task BuildChildrenAsync(CancellationToken cancellationToken)
        {
            LifetimeScope last = currentScope;
            for (int i = 0; i < children.Count; i++)
            {
                LayerInstallerBase child = children[i];
                last = await child.BuildFromParentAsync(currentScope, cancellationToken);
            }

            finalScope = last;
        }

        protected virtual IReadOnlyList<IAsyncLayerInitializable> ResolveInitializers()
        {
            if (currentScope == null)
            {
                return Array.Empty<IAsyncLayerInitializable>();
            }

            IEnumerable<IAsyncLayerInitializable> resolved;
            try
            {
                resolved = currentScope.Container.Resolve<IEnumerable<IAsyncLayerInitializable>>();
            }
            catch (VContainerException)
            {
                return Array.Empty<IAsyncLayerInitializable>();
            }

            if (resolved == null)
            {
                return Array.Empty<IAsyncLayerInitializable>();
            }

            return resolved.Where(initializer => initializer != null).ToArray();
        }

        protected virtual IReadOnlyList<IAsyncLayerInitializable> FilterPendingInitializers(IReadOnlyList<IAsyncLayerInitializable> resolved)
        {
            if (resolved == null || resolved.Count == 0)
            {
                return Array.Empty<IAsyncLayerInitializable>();
            }

            List<IAsyncLayerInitializable> pending = new List<IAsyncLayerInitializable>();
            for (int i = 0; i < resolved.Count; i++)
            {
                IAsyncLayerInitializable initializer = resolved[i];
                if (initializer == null || initializedRegistry.Contains(initializer))
                {
                    continue;
                }

                pending.Add(initializer);
            }

            return pending;
        }

        private async Task BuildAsRootInternalAsync(LifetimeScope rootScope, CancellationToken cancellationToken)
        {
            finalScope = await BuildFromParentAsync(rootScope, cancellationToken);
        }

        private async Task<LifetimeScope> BuildFromParentAsync(LifetimeScope parentScope, CancellationToken cancellationToken)
        {
            if (parentScope == null)
            {
                throw new ArgumentNullException(nameof(parentScope));
            }

            cancellationToken.ThrowIfCancellationRequested();
            currentScope = parentScope.CreateChild(builder =>
            {
                if (parent != null)
                {
                    parent.ConfigureChildBuilder(this, parentScope.Container, builder);
                }

                Install(builder);
            });

            if (currentScope.Container == null)
            {
                currentScope.Build();
            }

            await ExecuteBuildPipelineAsync(cancellationToken);
            return finalScope ?? currentScope;
        }

        private async Task ExecuteBuildPipelineAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            IObjectResolver resolver = currentScope?.Container;
            await InitializeAsync(resolver, cancellationToken);
            await OnCompletedAsync(resolver, cancellationToken);
            progressContext?.ReportCompletedStep();
            await BuildChildrenAsync(cancellationToken);
        }

        private async Task InitializeResolvedInitializersAsync(IObjectResolver resolver, CancellationToken cancellationToken)
        {
            IReadOnlyList<IAsyncLayerInitializable> resolved = ResolveInitializers();
            IReadOnlyList<IAsyncLayerInitializable> pending = FilterPendingInitializers(resolved);
            if (pending.Count == 0)
            {
                return;
            }

            Task[] tasks = new Task[pending.Count];
            for (int i = 0; i < pending.Count; i++)
            {
                tasks[i] = RunInitializerAsync(pending[i], resolver, cancellationToken);
            }

            await Task.WhenAll(tasks);

            for (int i = 0; i < pending.Count; i++)
            {
                initializedRegistry.Add(pending[i]);
            }
        }

        private static async Task RunInitializerAsync(IAsyncLayerInitializable initializer, IObjectResolver resolver, CancellationToken cancellationToken)
        {
            try
            {
                await initializer.InitializeAsync(resolver, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException($"Initialization failed in '{initializer.GetType().FullName}'.", exception);
            }
        }

        private void AssignRegistry(HashSet<IAsyncLayerInitializable> registry)
        {
            initializedRegistry = registry ?? throw new ArgumentNullException(nameof(registry));
            for (int i = 0; i < children.Count; i++)
            {
                children[i].AssignRegistry(registry);
            }
        }

        private int CountLayerNodes()
        {
            int count = 1;
            for (int i = 0; i < children.Count; i++)
            {
                count += children[i].CountLayerNodes();
            }

            return count;
        }

        private void AssignProgressContext(LayerBuildProgressContext context)
        {
            progressContext = context;
            for (int i = 0; i < children.Count; i++)
            {
                children[i].AssignProgressContext(context);
            }
        }

        private sealed class LayerBuildProgressContext
        {
            private readonly ILayeredScopeProgress listener;
            private readonly int totalLayers;
            private int completedStep;

            internal LayerBuildProgressContext(ILayeredScopeProgress listener, int totalLayers)
            {
                this.listener = listener;
                this.totalLayers = totalLayers;
            }

            internal void ReportCompletedStep()
            {
                listener?.OnLayerPipelineStep(++completedStep, totalLayers);
            }
        }

        private bool ContainsInSubtree(LayerInstallerBase target)
        {
            if (ReferenceEquals(this, target))
            {
                return true;
            }

            for (int i = 0; i < children.Count; i++)
            {
                if (children[i].ContainsInSubtree(target))
                {
                    return true;
                }
            }

            return false;
        }

        private sealed class ReferenceComparer<T> : IEqualityComparer<T> where T : class
        {
            public static readonly ReferenceComparer<T> Instance = new ReferenceComparer<T>();

            public bool Equals(T x, T y)
            {
                return ReferenceEquals(x, y);
            }

            public int GetHashCode(T obj)
            {
                return obj == null ? 0 : System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
            }
        }
    }
}
