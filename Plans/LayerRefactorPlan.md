# Layer Refactor Plan

## Goal
Simplify layer composition and async initialization by removing interface/delegation plumbing and making recursion first-class in a concrete base class.

## Agreed Changes
- Remove `ILayerInstaller`; use a concrete `LayerInstallerBase` only.
- Keep `AddChild(...)` synchronous and structural only.
- Keep layer build/initialization asynchronous.
- Avoid passing `beforeInstall` callbacks and avoid passing scope/layer arguments through recursion.
- Store scope internally on the installer instance.
- Replace delegated child registration with parent hook methods.

## Final API Signatures

```csharp
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Madbox.Scope.Contracts;
using VContainer;
using VContainer.Unity;

namespace Madbox.Scope
{
    public abstract class LayerInstallerBase
    {
        // Fluent tree API
        public LayerInstallerBase AddChild(LayerInstallerBase child);
        public IReadOnlyList<LayerInstallerBase> Children { get; }

        // Entry points
        public Task BuildAsRootAsync(LifetimeScope rootScope, CancellationToken cancellationToken);
        protected Task BuildAsync(CancellationToken cancellationToken);

        // Scope access (set during build)
        protected LifetimeScope CurrentScope { get; }

        // Registration hooks
        protected abstract void Install(IContainerBuilder builder);
        protected virtual void ConfigureChildBuilder(LayerInstallerBase child, IContainerBuilder childBuilder);

        // Lifecycle hooks
        protected virtual Task InitializeAsync(CancellationToken cancellationToken);
        protected virtual Task OnCompletedAsync(CancellationToken cancellationToken);

        // Optional child orchestration override
        protected virtual Task BuildChildrenAsync(CancellationToken cancellationToken);

        // Initializer pipeline (kept internal/private in impl)
        protected virtual IReadOnlyList<IAsyncLayerInitializable> ResolveInitializers();
        protected virtual IReadOnlyList<IAsyncLayerInitializable> FilterPendingInitializers(
            IReadOnlyList<IAsyncLayerInitializable> resolved);

        // Optional: clear state for rebuild/restart scenarios
        public virtual void Reset();
    }
}
```

```csharp
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using VContainer.Unity;

namespace Madbox.Scope
{
    public abstract class LayeredScope : LifetimeScope
    {
        public bool IsBootstrapCompleted { get; private set; }

        // Choose one depending on your topology preference:
        protected abstract LayerInstallerBase BuildLayerTree();
        // or: protected abstract IReadOnlyList<LayerInstallerBase> BuildLayerTrees();

        protected abstract void OnBootstrapCompleted(LifetimeScope finalScope);

        protected Task StartAsync(CancellationToken cancellationToken);
    }
}
```

## Internal State (LayerInstallerBase)
Use private fields in implementation:
1. `LayerInstallerBase parent;`
2. `LifetimeScope parentScope;`
3. `LifetimeScope currentScope;`
4. `List<LayerInstallerBase> children;`
5. `List<IAsyncLayerInitializable> resolvedInitializersCache;`
6. `HashSet<IAsyncLayerInitializable> initializedRegistry;` (shared from root via parent chain)

## Execution Plan
1. Add `LayerInstallerBase` and move all layer composition to it.
2. Refactor `LayeredScope` to build root tree(s) and call `BuildAsRootAsync(...)`.
3. Move initializer resolution/execution logic from `ScopeInitializer` into `LayerInstallerBase`.
4. Keep dedup tracking via shared initialization registry.
5. Replace delegated child-registration behavior with `ConfigureChildBuilder(...)`.
6. Update concrete installers to inherit from `LayerInstallerBase` and use fluent chaining.
7. Remove obsolete classes once migration compiles and tests pass:
   - `ScopeInitializer`
   - `DelegatedChildRegistration`
   - `LayerInitializationContext`
   - `ChildRegistrationFactory`
   - `ChildScopeDelegationPolicy` (if no longer used)
8. Update tests for recursion order, parent-before-child initialization, dedup, and cancellation.

## Notes
- Decide dedup semantics before implementation:
  - by instance (matches current behavior), or
  - by type (stricter, may skip valid duplicate instances).
- Keep `AddChild` free of async behavior; all async work belongs in build/init hooks.
