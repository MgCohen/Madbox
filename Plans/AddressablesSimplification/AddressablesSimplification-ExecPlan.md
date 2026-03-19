# Simplify Addressables Runtime to One Gateway-Centered Design

This ExecPlan is a living document. The sections `Progress`, `Surprises & Discoveries`, `Decision Log`, and `Outcomes & Retrospective` must be kept up to date as work proceeds.

This document must be maintained in accordance with `PLANS.md` at the repository root.

## Purpose / Big Picture

After this change, the Addressables module will be easier to reason about and cheaper to evolve. Feature code will still use one gateway contract, but the internals will remove unnecessary coordinator/provider/builder/token layers, remove reflection dispatch, and reduce startup plumbing. The user-visible outcome is unchanged behavior (startup preload, runtime loads, and release lifecycle), with less indirection and fewer classes to maintain.

The target is simplification, not feature expansion. We will keep required ownership semantics and tests, but remove abstractions that do not add practical value for this project size.

## Progress

- [x] (2026-03-18 23:30Z) Authored the initial ExecPlan covering all nine requested simplifications, conflict checks, migration snippets, and validation strategy.
- [ ] Execute Milestone 1: Baseline behavior characterization for current Addressables startup/load/release semantics, including regression safety for known cancellation/logging paths.
- [ ] Execute Milestone 2: Remove preload builder/provider pipeline and merge preload request creation into a simpler gateway-owned startup path.
- [ ] Execute Milestone 3: Remove layer initializer class and move initialization/startup preload orchestration into gateway only (registered as both gateway and async initializer).
- [ ] Execute Milestone 4: Replace `AssetKey` with Unity-native references for public API, keeping an internal lightweight lookup key string only where needed.
- [ ] Execute Milestone 5: Replace token/reflection-heavy lease logic with map-backed reference-counter entries and non-reflection typed dispatch.
- [ ] Execute Milestone 6: Update docs/tests/contracts, run full `.agents/scripts/validate-changes.cmd`, and finalize retrospective.

## Surprises & Discoveries

- Observation: Existing startup/preload flow is split across gateway, startup coordinator, preload provider, request builder, and layer initializer, which creates conceptual duplication for a small module.
  Evidence: runtime files under `Assets/Scripts/Infra/Addressables/Runtime/Implementation/` currently include all those layers at once.

- Observation: Reflection is currently used in both layer initialization and lease preload dispatch.
  Evidence: `AddressablesLayerInitializer` and `AddressablesLeaseStore` call `MakeGenericMethod(...)` and `Invoke(...)`.

- Observation: `AssetKey` is used broadly inside Addressables contracts and tests, but feature callers already rely heavily on `AssetReference`/`AssetReferenceT<T>` and label references.
  Evidence: `IAddressablesGateway` has both key and reference overloads; `AddressableLevelDefinitionProvider` already uses `AssetReferenceT<T>`.

## Decision Log

- Decision: This plan targets simplification over maximal separation-of-concerns, while preserving observable behavior.
  Rationale: The user goal is reducing overengineering and maintenance overhead for project scale.
  Date/Author: 2026-03-18 / Codex

- Decision: Keep one gateway-centered runtime design and remove preload builder/provider and standalone startup coordinator layers.
  Rationale: Those layers currently add indirection with minimal reuse benefit.
  Date/Author: 2026-03-18 / Codex

- Decision: Use `Scaffold.Maps.Map<Type, string, RefEntry>` for internal loaded-entry lookup, but do not enable advanced indexer features initially.
  Rationale: It supports straightforward `(assetType, key)` lookup and future query growth without building extra custom collection plumbing now.
  Date/Author: 2026-03-18 / Codex

- Decision: Public API will prioritize Unity-native references and labels; string-key overload can be retained only as migration bridge if needed.
  Rationale: This removes custom key wrapper overhead while preserving authoring friendliness.
  Date/Author: 2026-03-18 / Codex

## Outcomes & Retrospective

This section will be completed after Milestone 6 with final behavior parity evidence, simplification impact summary, and residual tradeoffs.

## Context and Orientation

Addressables module runtime code lives under `Assets/Scripts/Infra/Addressables/Runtime/`.

Current key files and responsibilities:

- `Contracts/IAddressablesGateway.cs`: public loading API surface.
- `Contracts/IAddressablesAssetClient.cs`: low-level Unity Addressables adapter contract.
- `Contracts/AssetKey.cs`: custom key type targeted for removal.
- `Implementation/AddressablesGateway.cs`: main facade plus runtime load APIs.
- `Implementation/AddressablesStartupCoordinator.cs`: startup orchestration layer targeted for removal.
- `Implementation/AddressablesPreloadRequestProvider.cs`: preload config load layer targeted for removal.
- `Implementation/AddressablesPreloadConfigRequestBuilder.cs`: preload validation/build layer targeted for removal.
- `Implementation/AddressablesLayerInitializer.cs`: scope startup adapter targeted for removal.
- `Implementation/AddressablesLeaseStore.cs`: loaded-entry/ref-count owner that currently uses token and reflection machinery.
- `Implementation/AddressablesLoadToken.cs`: token abstraction targeted for removal.
- `Container/AddressablesInstaller.cs`: DI registration to simplify to gateway-centric setup.

Cross-module call sites impacted by API changes:

- `Assets/Scripts/Infra/Navigation/Runtime/Implementation/NavigationProvider.cs`
- `Assets/Scripts/Core/Levels/Authoring/Catalog/AddressableLevelDefinitionProvider.cs`
- Addressables, Navigation, and Levels tests that currently implement `IAddressablesGateway` key overloads.

In this plan, “gateway-centered design” means one class owns startup flow and runtime load flow directly, while smaller helper structures remain only when they serve direct data ownership (for example, a simple map-backed reference counter entry store).

## Simplification Goals and Motives

The design motive is to reduce cognitive load without changing user-facing behavior. The current implementation disperses one runtime story across many files and internal services. That cost is not justified by the current module scope.

The outcomes we want are:

1. Fewer runtime classes and fewer moving parts for startup preload.
2. No custom key wrapper API for consumers when Unity-native references already solve the problem.
3. No reflection-based generic dispatch in hot runtime paths.
4. One simple ownership model for loaded assets with explicit retention policy.
5. Installer wiring that exposes one obvious runtime entry point.

## Requested Changes and Conflict Analysis

### Change 1: Remove extra startup orchestration layering

We will collapse startup orchestration into `AddressablesGateway` and remove `AddressablesStartupCoordinator`. This aligns with existing scope/bootstrap startup pipeline (`IAsyncLayerInitializable` execution) and avoids “coordinator calling client calling provider” chains.

Potential conflict: gateway can grow too large again.
Mitigation: keep internal private methods grouped by startup phases and use small data-only structs where needed, but no extra service classes unless behavior duplication appears.

### Change 2: Remove preload request provider and builder

We will delete:

- `Implementation/AddressablesPreloadRequestProvider.cs`
- `Implementation/AddressablesPreloadConfigRequestBuilder.cs`

Preload config parsing and minimal validation will live in one private gateway method. Validation will be pragmatic: only checks required to avoid runtime null/invalid-type crashes.

Potential conflict: less granular error messages.
Mitigation: keep one precise message format with config key and entry index; avoid broad exception wrapping.

### Change 3: Remove layer initializer class and move behavior into gateway

We will delete `Implementation/AddressablesLayerInitializer.cs` and have gateway satisfy startup initialization directly in DI registration. No adapter class will be introduced.

Potential conflict: DI may create distinct instances if registered incorrectly.
Mitigation: register gateway once as concrete scoped instance, then expose same instance for both `IAddressablesGateway` and `IAsyncLayerInitializable`.

### Change 4: Remove `AssetKey` from public-facing API

Preferred API shape after migration:

    public interface IAddressablesGateway
    {
        Task InitializeAsync(CancellationToken cancellationToken = default);
        Task<IAssetHandle<T>> LoadAsync<T>(AssetReference reference, CancellationToken cancellationToken = default) where T : UnityEngine.Object;
        Task<IAssetHandle<T>> LoadAsync<T>(AssetReferenceT<T> reference, CancellationToken cancellationToken = default) where T : UnityEngine.Object;
        Task<IAssetGroupHandle<T>> LoadAsync<T>(AssetLabelReference label, CancellationToken cancellationToken = default) where T : UnityEngine.Object;
        IAssetHandle<T> Load<T>(AssetReference reference, CancellationToken cancellationToken = default) where T : UnityEngine.Object;
        IAssetHandle<T> Load<T>(AssetReferenceT<T> reference, CancellationToken cancellationToken = default) where T : UnityEngine.Object;
        IAssetGroupHandle<T> Load<T>(AssetLabelReference label, CancellationToken cancellationToken = default) where T : UnityEngine.Object;
    }

If a bridge is needed during migration, keep temporary internal `string` key overloads in implementation only, not in contracts.

Potential conflict: existing tests and some runtime call sites use key overloads.
Mitigation: update call sites to references or labels; for places that only have string ids, create local `AssetReference` at boundary.

### Change 5: Simplify lease ownership (remove token plumbing, keep map-backed lookup)

We will remove `AddressablesLoadToken` and replace the loaded-entry store with:

    Map<Type, string, RefEntry> loaded;

`RefEntry` will hold:

- loaded asset reference
- `refCount`
- retention policy (`Normal` or `NeverDie` equivalent internal flag)

Potential conflict: dropping map-like lookup entirely would break cheap dedupe and release correctness.
Mitigation: keep map lookup as the minimal required structure; do not add advanced indexers now.

### Change 6: Remove reflection dispatch

We will remove runtime `MethodInfo` + `Invoke(...)` dispatch and replace with direct code paths. For type-based preload by config entry, use explicit non-reflection helpers keyed by known runtime data (for example, reference load path that already carries runtime type via Unity references), or a controlled delegate cache if strictly needed without reflection.

Potential conflict: dynamic type preload from config entries may still need generic binding.
Mitigation: push preload path to gateway methods that operate on references and labels rather than arbitrary `Type` invocation.

### Change 7: Keep release policy on reference-counter entry, not on handle API

The policy concept requested (do not release when refCount reaches zero for retained preloads) will be implemented in `RefEntry`. Handles remain thin ownership tokens with release callback.

Potential conflict: policy mixed into handle state can diverge across multiple handles.
Mitigation: centralize policy evaluation in ref-entry store only.

### Change 8: Acquire logic reduced to lookup/increment/load

Acquire semantics become:

1. lookup `(type, key)` in map
2. if found, increment `refCount` and return handle
3. if not found, load, insert with `refCount = 1`, return handle

Potential conflict: concurrent parallel loads for same key can race.
Mitigation: use one small lock around map read/write so concurrent callers wait only until map state is updated; do not block the lock for the full asset load.

### Change 9: Installer registers gateway only

`AddressablesInstaller` will stop registering standalone client/initializer services. Gateway will own its internal collaborators.

Potential conflict: tests or modules resolving removed services directly.
Mitigation: update tests to resolve only `IAddressablesGateway` and scope startup contract from gateway registration.

## Plan of Work

Milestone 1 establishes safety rails before simplification. We will add/update characterization tests for startup behavior, preload behavior (`Normal`/retained), and release semantics. If any bug is discovered during this phase, a regression test must be added first and verified fail-before/fix/pass-after.

Milestone 2 removes preload provider/builder/coordinator layers and folds startup/preload flow into gateway. At this point, behavior must remain equivalent through tests.

Milestone 3 removes layer initializer and rewires installer so one gateway instance also serves async layer initialization.

Milestone 4 migrates contract APIs away from `AssetKey`, updates call sites and tests to references/labels, and removes token class and reflection-based dispatch.

Milestone 5 simplifies lease store into map-backed reference entries with policy-aware release, then revalidates startup and runtime behavior.

Milestone 6 updates Addressables documentation and runs full repository quality gates until clean.

## API Migration Snippets

### Old call shape

    IAssetHandle<TestAsset> handle = await gateway.LoadAsync<TestAsset>(new AssetKey("enemy/bee"), ct);

### New call shape

    AssetReference reference = new AssetReference("bee");
    IAssetHandle<TestAsset> handle = await gateway.LoadAsync(reference, ct);

### Label batch remains first-class

    AssetLabelReference label = new AssetLabelReference { labelString = "enemy" };
    IAssetGroupHandle<TestAsset> group = await gateway.LoadAsync<TestAsset>(label, ct);

### Internal map-backed entry sketch

    private sealed class RefEntry
    {
        public UnityEngine.Object Asset;
        public int RefCount;
        public PreloadMode Policy;
    }

    // map key: (assetType, runtimeKeyString)
    private readonly Map<Type, string, RefEntry> loaded = new Map<Type, string, RefEntry>();

## Concrete Steps

Run all commands from repository root `C:\Users\mtgco\.codex\worktrees\6a86\Madbox`.

1. Read and keep updated during implementation:

    Architecture.md
    Docs/AutomatedTesting.md
    Docs/Infra/Addressables.md
    Plans/AddressablesSimplification/AddressablesSimplification-ExecPlan.md

2. Baseline tests before refactor:

    powershell -NoProfile -ExecutionPolicy Bypass -File ".\.agents\scripts\run-editmode-tests.ps1" -AssemblyNames "Madbox.Addressables.Tests"
    powershell -NoProfile -ExecutionPolicy Bypass -File ".\.agents\scripts\run-playmode-tests.ps1" -AssemblyNames "Madbox.Addressables.PlayModeTests"

3. Execute milestone changes incrementally, rerunning relevant Addressables tests after each step.

4. Run analyzer diagnostics frequently:

    powershell -NoProfile -ExecutionPolicy Bypass -File ".\.agents\scripts\check-analyzers.ps1"

5. After each milestone, run full gate:

    .\.agents\scripts\validate-changes.cmd

6. If gate fails, fix all issues and rerun until clean before marking milestone complete.

## Validation and Acceptance

Acceptance is behavior-based:

1. Bootstrap startup still completes and resolves addressables gateway.
2. Preload config still drives startup preload with expected retention behavior.
3. Loading same asset through gateway does not leak or double-release.
4. Consumer-facing gateway API no longer requires `AssetKey`.
5. Runtime no longer relies on `AddressablesPreloadConfigRequestBuilder`, `AddressablesPreloadRequestProvider`, `AddressablesStartupCoordinator`, `AddressablesLayerInitializer`, or `AddressablesLoadToken`.
6. Reflection dispatch removed from Addressables runtime implementation.
7. Full quality gate passes:

    .\.agents\scripts\validate-changes.cmd

For bug fixes discovered during migration, the corresponding regression test must fail before the fix and pass after the fix.

## Idempotence and Recovery

The plan is incremental and safe to rerun. Each milestone is additive-then-cleanup where possible. If an intermediate step breaks runtime behavior, revert only that milestone’s local edits, keep characterization tests, and continue from the last passing gate.

If API migration temporarily needs bridge overloads, keep them explicitly marked as transitional and remove them before final milestone completion.

## Artifacts and Notes

During execution, this section must store concise evidence snippets for each completed milestone, including:

- command executed
- pass/fail summary
- any notable behavioral confirmation

Do not store long logs; include only acceptance-relevant excerpts.

## Interfaces and Dependencies

Interfaces expected at completion:

- `IAddressablesGateway` with reference/label-first API surface.
- `IAssetHandle<T>` and `IAssetGroupHandle<T>` unchanged in ownership semantics.
- No public `AssetKey` dependency for gateway consumers.

Dependencies and boundaries:

- Keep module in infra/runtime boundary with explicit asmdef references.
- Keep Unity-facing API usage inside Addressables infra module.
- Keep startup integration via `IAsyncLayerInitializable` contract execution in scope pipeline.

Potentially impacted modules/tests to update in same milestone sequence:

- Addressables runtime tests (`Assets/Scripts/Infra/Addressables/Tests/`)
- Navigation tests using gateway key overload mocks
- Levels tests and authoring providers relying on gateway key overloads
- `Docs/Infra/Addressables.md` usage snippets and API section

---

Revision Note (2026-03-18 / Codex): Created new simplification-focused ExecPlan per user request, covering all nine requested changes, conflict checks, API migration snippets, and milestone validation loops.
Revision Note (2026-03-18 / Codex): Updated API and storage snippets per review feedback: added sync group-handle API shape, changed concurrency mitigation to minimal lock strategy, switched migration example to `new AssetReference("bee")`, and replaced boolean retention flag with policy field on `RefEntry`.
