# Streamline Layer Composition and Initialization Flow

This ExecPlan is a living document. The sections `Progress`, `Surprises & Discoveries`, `Decision Log`, and `Outcomes & Retrospective` must be kept up to date as work proceeds.

This document must be maintained in accordance with `PLANS.md` at the repository root.

## Purpose / Big Picture

After this change, bootstrap composition uses one concrete recursive installer API instead of interface plus delegation plumbing. A contributor can build the startup tree with a fluent `AddChild(...)` model, run async initialization in deterministic parent-before-child order, and reason about one ownership path for scope state.

The outcome is observable by running bootstrap and scope tests: the system still starts successfully, initialization still runs in parallel inside each layer, already-initialized services are still deduplicated by instance, and the delegated child-registration contracts are no longer required because layer installers own child builder configuration directly.

## Progress

- [x] (2026-03-20 20:05Z) Confirmed design decisions with user: single root tree, instance-based dedup, parallel initializer execution, remove delegated child-registration API, and include analyzer work as a dedicated milestone file.
- [x] (2026-03-20 20:05Z) Authored initial ExecPlan and created one file-backed milestone for analyzer alignment.
- [ ] Execute Milestone 1: Implement `LayerInstallerBase` recursion pipeline and migrate `LayeredScope` to tree-based startup.
- [ ] Execute Milestone 2: Migrate bootstrap installers and addressables initialization flow away from delegated child registration.
- [ ] Execute Milestone 3 (Plans/LayerRefactor/milestones/ExecPlan-Milestone-3.md): Align startup analyzer metadata/contracts and analyzer tests to `Madbox.Scope.Contracts`.
- [ ] Execute Milestone 4: Update scope and bootstrap docs, run full quality gate, and record outcomes.

## Surprises & Discoveries

- Observation: The startup analyzer currently targets `Madbox.Initialization.Contracts.*` while runtime contracts in this repository are in `Madbox.Scope.Contracts.*`.
  Evidence: `Analyzers/Scaffold/Scaffold.Analyzers/InitializationSameLayerUsageAnalyzer.cs` contains hardcoded metadata names under `Madbox.Initialization.Contracts`, while `Assets/Scripts/Infra/Scope/Runtime/Contracts/IAsyncLayerInitializable.cs` uses `Madbox.Scope.Contracts`.
- Observation: Current delegated child registration is consumed in real runtime behavior by addressables preload registration.
  Evidence: `Assets/Scripts/Infra/Addressables/Runtime/Implementation/AddressablesGateway.cs` calls `ILayerInitializationContext.RegisterInstanceForChild(...)` during startup preload.

## Decision Log

- Decision: Use one root installer tree (`BuildLayerTree`) instead of multiple roots.
  Rationale: The goal is API streamlining, and a single tree makes recursion and execution order explicit with less surface area.
  Date/Author: 2026-03-20 / User + Codex
- Decision: Keep initializer dedup by instance.
  Rationale: This preserves current behavior and avoids unintentionally suppressing valid multiple instances of the same type.
  Date/Author: 2026-03-20 / User + Codex
- Decision: Keep initializer execution parallel per layer.
  Rationale: Existing behavior is parallel (`Task.WhenAll`) and preserving it avoids hidden startup regressions.
  Date/Author: 2026-03-20 / User + Codex
- Decision: Remove delegated child-registration contracts and move composition responsibility into layer installers/hooks.
  Rationale: This directly removes the indirection the refactor is intended to eliminate.
  Date/Author: 2026-03-20 / User + Codex
- Decision: Track analyzer alignment as a dedicated milestone detail file.
  Rationale: Analyzer realignment is meaningful scope with independent validation and deserves handoff-safe instructions.
  Date/Author: 2026-03-20 / User + Codex
- Decision: Keep layer execution order in a non-overridable template pipeline.
  Rationale: The core build flow must stay deterministic and safe (`Install -> InitializeAsync -> BuildChildrenAsync -> OnCompletedAsync`) even when subclasses customize hooks.
  Date/Author: 2026-03-20 / User + Codex
- Decision: Enforce `AddChild(...)` tree integrity guards.
  Rationale: The API is public, so it must reject invalid topology early (`null`, self-reference, cycles, and multi-parent attachments) to avoid late recursive failures.
  Date/Author: 2026-03-20 / User + Codex
- Decision: Keep layer scope state non-public and not part of derived-class API.
  Rationale: Scope visibility is implementation detail in this model; reducing surface area supports the streamlining goal and avoids misuse.
  Date/Author: 2026-03-20 / User + Codex

## Outcomes & Retrospective

Planned outcome at completion: Scope startup API is simpler and concrete, recursive layer composition is first-class, initialization behavior remains functionally equivalent where intended (ordering and parallelism), and analyzer metadata is aligned with actual runtime contracts.

Current status: Planning complete; implementation not started yet.

## Context and Orientation

The current startup flow is centered in `Assets/Scripts/Infra/Scope/Runtime/LayeredScope.cs` and `Assets/Scripts/Infra/Scope/Runtime/ScopeInitializer.cs`. `LayeredScope` currently requests a flat list of `ILayerInstaller` instances and creates one child scope per installer. `ScopeInitializer` resolves `IAsyncLayerInitializable` implementations and executes them in parallel, while also managing delegated child registrations through `ILayerInitializationContext`, `DelegatedChildRegistration`, and related runtime helpers.

The refactor replaces that split model with one concrete recursive installer base class. The recursive installer owns builder install hooks, scope references, initializer discovery/filtering, and child traversal. `LayeredScope` becomes a coordinator that builds one root tree and calls the root build entry point.

Key files for this plan are:

- `Assets/Scripts/Infra/Scope/Runtime/LayeredScope.cs`
- `Assets/Scripts/Infra/Scope/Runtime/ScopeInitializer.cs`
- `Assets/Scripts/Infra/Scope/Runtime/Contracts/ILayerInstaller.cs`
- `Assets/Scripts/Infra/Scope/Runtime/Contracts/IAsyncLayerInitializable.cs`
- `Assets/Scripts/Infra/Scope/Runtime/Contracts/ILayerInitializationContext.cs`
- `Assets/Scripts/Infra/Scope/Runtime/DelegatedChildRegistration.cs`
- `Assets/Scripts/Infra/Scope/Runtime/LayerInitializationContext.cs`
- `Assets/Scripts/Infra/Scope/Runtime/ChildRegistrationFactory.cs`
- `Assets/Scripts/App/Bootstrap/Runtime/BootstrapScope.cs`
- `Assets/Scripts/App/Bootstrap/Runtime/BootstrapAssetInstaller.cs`
- `Assets/Scripts/App/Bootstrap/Runtime/BootstrapInfraInstaller.cs`
- `Assets/Scripts/Infra/Addressables/Runtime/Implementation/AddressablesGateway.cs`
- `Assets/Scripts/Infra/Scope/Tests/ScopeInitializerTests.cs`
- `Assets/Scripts/App/Bootstrap/Tests/BootstrapScopeValidationTests.cs`
- `Docs/Infra/Scope.md`
- `Docs/App/Bootstrap.md`

Plain-language terms used in this plan:

- Layer installer tree: a parent-child object graph where each node defines service registrations and optional startup hooks, and recursion creates child scopes from parent scopes.
- Dedup by instance: if the same initializer object instance is encountered again, it is skipped; different instances of the same type still run.
- Parent-before-child initialization: a layer must complete its own startup phase before its children begin startup.

## Plan of Work

Milestone 1 introduces `LayerInstallerBase` in the scope runtime and ports initializer orchestration into this base class. The base class exposes synchronous `AddChild(...)` with topology guards (`null`, self, cycle, multi-parent), stores parent and scope state as private/internal implementation detail, and provides async lifecycle hooks under a non-overridable template pipeline (`Install -> InitializeAsync -> BuildChildrenAsync -> OnCompletedAsync`). `LayeredScope` is updated to request a single root installer via `BuildLayerTree()` and execute that root as startup entry.

Milestone 2 migrates concrete bootstrap composition to the new base class and removes the delegated child-registration model from addressables startup. Any child-scope registrations previously emitted via initialization context are expressed in installer-controlled child builder hooks so behavior remains deterministic without context delegation. In this milestone, simplify `IAsyncLayerInitializable` by removing `ILayerInitializationContext` from the startup contract and preserving parallel execution behavior.

Milestone 3 updates analyzer metadata and tests so initialization rule enforcement follows `Madbox.Scope.Contracts` names used by runtime code. This milestone is maintained in a dedicated file: `Plans/LayerRefactor/milestones/ExecPlan-Milestone-3.md`.

Milestone 4 cleans obsolete scope runtime types, updates docs, validates with repository gate scripts, and records evidence in this plan.

## Concrete Steps

Run commands from repository root: `C:\Unity\Madbox`.

1. Baseline before edits:

    powershell -NoProfile -ExecutionPolicy Bypass -File ".\.agents\scripts\run-editmode-tests.ps1" -AssemblyNames "Madbox.Scope.Tests"
    powershell -NoProfile -ExecutionPolicy Bypass -File ".\.agents\scripts\run-editmode-tests.ps1" -AssemblyNames "Madbox.Bootstrap.Tests"

2. Implement Milestone 1 and rerun focused tests:

    powershell -NoProfile -ExecutionPolicy Bypass -File ".\.agents\scripts\run-editmode-tests.ps1" -AssemblyNames "Madbox.Scope.Tests"

3. Implement Milestone 2 and rerun focused tests:

    powershell -NoProfile -ExecutionPolicy Bypass -File ".\.agents\scripts\run-editmode-tests.ps1" -AssemblyNames "Madbox.Scope.Tests"
    powershell -NoProfile -ExecutionPolicy Bypass -File ".\.agents\scripts\run-editmode-tests.ps1" -AssemblyNames "Madbox.Addressables.Tests"
    powershell -NoProfile -ExecutionPolicy Bypass -File ".\.agents\scripts\run-editmode-tests.ps1" -AssemblyNames "Madbox.Bootstrap.Tests"

4. Execute Milestone 3 from the milestone detail file and run analyzer checks:

    powershell -NoProfile -ExecutionPolicy Bypass -File ".\.agents\scripts\check-analyzers.ps1"
    dotnet test "Analyzers/Scaffold/Scaffold.Analyzers.Tests/Scaffold.Analyzers.Tests.csproj" -c Release --nologo

5. Final required quality gate:

    .\.agents\scripts\validate-changes.cmd

6. If gate fails, fix all reported failures and rerun:

    .\.agents\scripts\validate-changes.cmd

Expected success signals are green scope/bootstrap/addressables tests, analyzer checks with zero blockers, and a clean `validate-changes.cmd` run.

## Validation and Acceptance

Acceptance is complete when all following behavior is observable:

1. `LayeredScope` builds one root installer tree through `BuildLayerTree()` and startup succeeds.
2. Parent installer initialization completes before child installers start.
3. Initializers inside one layer execute in parallel and still honor cancellation and failure wrapping.
4. Dedup remains instance-based across the installer tree.
5. Core layer build sequencing is non-overridable and always runs in this order: `Install -> InitializeAsync -> BuildChildrenAsync -> OnCompletedAsync`.
6. `AddChild(...)` rejects invalid graph operations (`null`, self-reference, cycles, and multi-parent links) with explicit errors.
7. Delegated child-registration contracts and runtime helpers are removed from scope runtime, and their behavior is replaced by installer-controlled configuration hooks.
8. `AddressablesGateway` startup still preloads and exposes required services without `ILayerInitializationContext`.
9. Analyzer rule `SCA0026` still triggers on prohibited same-layer initialization usage and respects exception attributes under `Madbox.Scope.Contracts` naming.
10. `Docs/Infra/Scope.md` and `Docs/App/Bootstrap.md` describe the new API and flow accurately.
11. `.agents/scripts/validate-changes.cmd` passes.

For bug fixes discovered during implementation, add or update a regression test that fails before the fix and passes after it, then run the same validation loop.

## Idempotence and Recovery

All steps in this plan are safe to repeat. Tests and analyzer checks are idempotent. If a milestone introduces regressions, keep new tests, revert only the problematic implementation chunk, and reapply changes incrementally until tests pass.

If scope startup fails mid-migration, temporarily keep compatibility adapters in place (for example, adapter installers forwarding to `LayerInstallerBase`) while moving one subsystem at a time. Remove adapters only after focused tests and the full quality gate are clean.

No destructive repository commands are required for this plan.

## Artifacts and Notes

Files expected to be added:

- `Assets/Scripts/Infra/Scope/Runtime/LayerInstallerBase.cs`
- `Plans/LayerRefactor/milestones/ExecPlan-Milestone-3.md`

Files expected to be modified:

- `Assets/Scripts/Infra/Scope/Runtime/LayeredScope.cs`
- `Assets/Scripts/Infra/Scope/Runtime/ScopeInitializer.cs` or merged responsibilities into `LayerInstallerBase`
- `Assets/Scripts/Infra/Scope/Runtime/Contracts/IAsyncLayerInitializable.cs`
- `Assets/Scripts/App/Bootstrap/Runtime/BootstrapScope.cs`
- `Assets/Scripts/App/Bootstrap/Runtime/BootstrapAssetInstaller.cs`
- `Assets/Scripts/App/Bootstrap/Runtime/BootstrapInfraInstaller.cs`
- `Assets/Scripts/Infra/Addressables/Runtime/Implementation/AddressablesGateway.cs`
- `Assets/Scripts/Infra/Scope/Tests/ScopeInitializerTests.cs` or replacement tests for `LayerInstallerBase`
- `Assets/Scripts/App/Bootstrap/Tests/BootstrapScopeValidationTests.cs`
- `Docs/Infra/Scope.md`
- `Docs/App/Bootstrap.md`
- `Analyzers/Scaffold/Scaffold.Analyzers/InitializationSameLayerUsageAnalyzer.cs`
- `Analyzers/Scaffold/Scaffold.Analyzers.Tests/InitializationSameLayerUsageAnalyzerTests.cs`

Files expected to be removed after migration is complete and tests pass:

- `Assets/Scripts/Infra/Scope/Runtime/Contracts/ILayerInstaller.cs`
- `Assets/Scripts/Infra/Scope/Runtime/Contracts/ILayerInitializationContext.cs`
- `Assets/Scripts/Infra/Scope/Runtime/Contracts/ChildScopeDelegationPolicy.cs` (if no remaining usage)
- `Assets/Scripts/Infra/Scope/Runtime/DelegatedChildRegistration.cs`
- `Assets/Scripts/Infra/Scope/Runtime/LayerInitializationContext.cs`
- `Assets/Scripts/Infra/Scope/Runtime/ChildRegistrationFactory.cs`

Evidence to append during execution:

    Fail-before and pass-after summaries for any new regression tests.
    Focused test summaries per milestone.
    Final `validate-changes.cmd` summary including analyzer total.

## Interfaces and Dependencies

Required API at completion:

- `Madbox.Scope.LayerInstallerBase`
- `LayerInstallerBase AddChild(LayerInstallerBase child)`
- `IReadOnlyList<LayerInstallerBase> Children`
- `Task BuildAsRootAsync(LifetimeScope rootScope, CancellationToken cancellationToken)`
- `protected Task BuildAsync(CancellationToken cancellationToken)`
- `protected abstract void Install(IContainerBuilder builder)`
- `protected virtual void ConfigureChildBuilder(LayerInstallerBase child, IContainerBuilder childBuilder)`
- `protected virtual Task InitializeAsync(CancellationToken cancellationToken)`
- `protected virtual Task OnCompletedAsync(CancellationToken cancellationToken)`
- `protected virtual Task BuildChildrenAsync(CancellationToken cancellationToken)`
- `public virtual void Reset()`
- `LayeredScope` abstract member: `protected abstract LayerInstallerBase BuildLayerTree();`
- `Madbox.Scope.Contracts.IAsyncLayerInitializable` simplified contract: `Task InitializeAsync(IObjectResolver resolver, CancellationToken cancellationToken)`

Layer installer implementation constraints:

- `CurrentScope` is private/internal implementation state, not a protected extension point.
- Base sequencing method is non-overridable and enforces: `Install -> InitializeAsync -> BuildChildrenAsync -> OnCompletedAsync`.
- `AddChild(...)` must guard against invalid topology (`null`, self, cycles, multi-parent).

Contract and dependency constraints:

- Keep startup contracts in `Madbox.Scope.Contracts` unless intentionally moved and fully updated across analyzers and docs.
- Keep infra runtime free of presentation logic.
- Preserve explicit assembly references in asmdefs for any moved or removed contracts.
- Do not add new `#pragma warning disable` without explicit thread approval.

## Revision Note

2026-03-20: Initial ExecPlan created from `Plans/LayerRefactorPlan.md` to deliver a simplified concrete layer-tree API, preserve intended startup behavior (instance dedup and parallel initializers), remove delegated child-registration plumbing, and track analyzer alignment in a dedicated milestone detail file.
2026-03-20: Updated plan to enforce non-overridable build sequencing, require `AddChild(...)` graph guards, and simplify startup initializer contract by removing `ILayerInitializationContext`; kept scope state non-public per user direction.
