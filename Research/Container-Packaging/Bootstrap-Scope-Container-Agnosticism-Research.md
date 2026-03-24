# Bootstrap and Scope Container-Agnostic Packaging Research

## Context

This research captures options for making Bootstrap and Scope reusable across projects that may use different DI containers (for example VContainer and Zenject), without overengineering into a custom container framework.

Current context from implementation review:

- `LayeredScope` currently inherits `VContainer.Unity.LifetimeScope`.
- `LayerInstallerBase` currently depends on `IContainerBuilder`, `IObjectResolver`, `LifetimeScope`, and catches `VContainerException`.
- Installers in app/infra modules use VContainer APIs directly (`IInstaller`, `Lifetime.Scoped`, `WithParameter`, `Resolve` patterns).
- Some app flow code resolves services directly from container (`finalScope.Container.Resolve<INavigation>()`).
- Additional portability concerns include `IObjectResolver` usage in runtime code paths and `[Inject]` attribute usage in ViewModels.

## Goal

Package the scope/bootstrap orchestration so it can be reused across projects with different IoC containers while preserving:

- Layered startup orchestration.
- Real DI scope semantics (child/sibling and disposal paths in future).
- Clear architecture boundaries (composition root owns concrete wiring).

## What is good in the current flow

- Composition is centralized into installers and a deterministic layer tree.
- Runtime features already depend on interfaces in many places (`ISceneFlowService`, shell contracts, etc.).
- Startup flow is explicit and documented; this is an excellent base for extraction into a reusable package.

## Key coupling hotspots

1. `LayeredScope : LifetimeScope` tightly binds orchestration host to VContainer.
2. `LayerInstallerBase` creates and builds VContainer child scopes directly.
3. Shared orchestration catches `VContainerException`.
4. Installer APIs are VContainer-first across modules.
5. Some app flow logic resolves dependencies from container at runtime instead of using explicit startup orchestration hooks.

## Paths forward

### Path A: Quarantine VContainer (low cost, partial portability)

Keep VContainer in Scope/Installers, but prevent DI-specific types from leaking elsewhere.

Pros:

- Fastest improvements.
- Low refactor risk.

Cons:

- Does not fully satisfy reusable package across different containers.
- Real swap cost remains concentrated in Scope package.

### Path B: Minimal DI adapter for Scope (recommended balanced path)

Keep layered orchestration logic, but depend on small container-neutral abstractions only for operations actually needed by Scope.

Potential abstractions:

- `IScopeFactory` for child/sibling scope creation.
- `IScopeHandle` for lifecycle/build/disposal and resolver access.
- `IServiceResolver` for resolve/try-resolve/enumeration.
- `IServiceRegistry` for registration entrypoint.

Pros:

- Supports reusable package goal.
- Keeps real scope semantics.
- Limits abstraction surface if disciplined.

Cons:

- Requires careful interface design to avoid API creep.
- Some existing installers may need adapter-aware wrappers.

### Path C: Pure orchestration core + container-specific composition host

Move all container operations out of Scope core; keep Scope as pure startup pipeline engine. Composition host bridges orchestration with actual container APIs.

Pros:

- Strongest portability and testability.
- Clean architecture boundaries.

Cons:

- Larger upfront refactor than Path B.
- More integration plumbing.

### Path D: Parallel implementations first (VContainer + Zenject), unify later

Build a Zenject-specific implementation in parallel with existing VContainer implementation to learn behavior gaps before locking abstractions.

Pros:

- Low speculative design risk.
- Real-world parity checks early.

Cons:

- Temporary duplicate maintenance.
- Risk of drift if unification is delayed.

## Recommended direction for packaging

Given the goal (reusable across projects with different containers) and future need for scope semantics (child/sibling/disposal), the preferred direction is:

1. Short-term hardening from Path A (easy wins below).
2. Evolve to Path B (minimal adapter), optionally validated by Path D experiments.
3. Keep Path C as long-term north star if Scope package matures into a foundational shared dependency.

## Easy wins (low-regret changes)

1. Remove container resolve calls from app flow orchestration where possible (for example bootstrap "open first screen" path).
2. Avoid container-specific exception handling in shared orchestration code (`VContainerException` in reusable core).
3. Keep VContainer-specific APIs confined to adapter/composition assemblies.
4. Replace `[Inject]` usage in reusable ViewModels with constructor injection.
5. Keep container-specific attributes out of package-level contracts.

## Risk paths to avoid

- Building a broad "universal DI API" mirroring full features of multiple containers.
- Encoding advanced binding semantics (all lifetimes, decorators, conditional binds) into shared abstractions.
- Mixing portability refactor with feature delivery in a single large migration.
- Leaving attribute-based injection in supposedly container-agnostic reusable modules.

## Proposed package split

Suggested structure:

- `Madbox.Scope.Core`  
  Container-agnostic layered orchestration and lifecycle pipeline.
- `Madbox.Scope.Abstractions`  
  Minimal DI bridge interfaces only.
- `Madbox.Scope.VContainer`  
  Concrete adapter implementation for VContainer.
- `Madbox.Scope.Zenject`  
  Concrete adapter implementation for Zenject.

This split keeps orchestration reusable, while container behavior remains in opt-in adapters.

## Validation checklist for future implementation

- Can current bootstrap sequence run unchanged behaviorally through adapter layer?
- Are initializers still deterministic and ordered as expected?
- Do child scope disposal semantics match current behavior?
- Can sibling scopes be introduced without changing core orchestration contracts?
- Are tests passing in both VContainer adapter and any Zenject prototype adapter?
- Are reusable assemblies free of container-specific attributes and exception types?

## Decision notes

- This research intentionally presents multiple paths, not one revamp plan.
- The module can become reusable without reimplementing a DI container if the abstraction budget is kept strict and scope-centric.
