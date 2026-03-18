# Build a Generic Addressables Gateway for Game Initialization

This ExecPlan is a living document. The sections `Progress`, `Surprises & Discoveries`, `Decision Log`, and `Outcomes & Retrospective` must be kept up to date as work proceeds.

This document must be maintained in accordance with `PLANS.md` at the repository root.

## Purpose / Big Picture

After this change, game systems can initialize and load any addressable content through one central API without creating feature-specific providers such as enemy-only or level-only loaders. Consumers call `Initialize` and `Load`, then release through the returned handle; catalog refresh, dependency download, and reference counting remain internal details.

This work is observable when bootstrap can initialize the gateway once, request assets by key/catalog/reference through a single generic interface, and release them safely while tests prove stable lifecycle behavior.

## Progress

- [x] (2026-03-17 23:30Z) Authored initial ExecPlan aligned to `Research/Entities/Entity-Research-and-Specs.md`, `Research/Entities/Entity-Addressables-Specs.md`, and `Research/Layers and flow/Layers and flow.md`.
- [x] (2026-03-18 03:40Z) Executed Milestone 1 (Plans/Generic-Addressables-System/milestones/ExecPlan-Milestone-1.md): delivered the generic runtime gateway, strict handle ownership with `Release`, preload registry modes, scope initializer integration, tests, and docs.
- [ ] (2026-03-18 03:40Z) Execute Milestone 2 (Plans/Generic-Addressables-System/milestones/ExecPlan-Milestone-2.md): completed catalog update/download sync in initialization and failure-path tests; remaining: explicit CDN configuration abstraction.
- [ ] (2026-03-18 03:40Z) Run final repository quality gate and confirm the full plan is complete with clean diagnostics (blocked by missing analyzer tests project file in repository).

## Surprises & Discoveries

- Observation: The repository currently contains Unity Addressables settings assets but no implemented `Assets/Scripts/.../Addressables` module under the runtime source tree.
  Evidence: `rg --files Assets/Scripts | rg -i "addressables|address"` returned no module hits.
- Observation: Repository quality gate analyzer step is blocked by a missing analyzer tests project file (`Scaffold.Analyzers.Tests.csproj`) unrelated to Addressables implementation.
  Evidence: `.agents/scripts/validate-changes.cmd` reports `BLOCKER: Analyzer tests project not found ...\Scaffold.Analyzers.Tests.csproj` while compilation and Unity tests pass.

## Decision Log

- Decision: Split the work into exactly two milestones: generic runtime first, remote update/CDN second.
  Rationale: This matches the requested delivery order and keeps the first playable initialization path independent from live content rollout complexity.
  Date/Author: 2026-03-17 / Codex
- Decision: Release ownership is handle-driven (`IAssetHandle.Release()`) instead of gateway-driven (`Gateway.Unload(handle)`).
  Rationale: This creates strict ownership semantics and avoids service-locator style release calls from feature modules.
  Date/Author: 2026-03-17 / Codex
- Decision: Use one type-agnostic provider/controller with generic methods instead of per-asset-type providers.
  Rationale: This satisfies the requirement that entities, levels, and future asset types use the same gateway without creating dedicated loaders.
  Date/Author: 2026-03-17 / Codex
- Decision: Preloading is centralized in a generic preload registry/service with explicit ownership modes (`Normal`, `NeverDie`) and is not implemented per feature service.
  Rationale: This prevents duplicated preload caches in services like `EnemiesService` and keeps preload policy consistent across modules.
  Date/Author: 2026-03-17 / Codex
- Decision: Catalog/content sync failures during initialization degrade gracefully (warning + continue) instead of aborting startup by default.
  Rationale: Startup should stay alive for non-critical remote failures while still allowing local content and preloads to work.
  Date/Author: 2026-03-18 / Codex

## Outcomes & Retrospective

Implemented outcome: Addressables module is now present with runtime, container, and tests assemblies; bootstrap infra wiring installs the module and scope startup initializes it via `IAsyncLayerInitializable`; consumers load through `IAddressablesGateway` and release through idempotent `IAssetHandle.Release`; preload registry supports `Normal` and `NeverDie`; and module docs were added.

Milestone 2 status: catalog check/update + dependency download sync runs during initialization through `IAddressablesAssetClient.SyncCatalogAndContentAsync`, with graceful failure behavior validated by tests. Remaining gap for full milestone intent is explicit CDN configuration abstraction and policy surface.

Quality status: Unity compilation precheck, EditMode tests, and PlayMode tests pass. Full gate remains blocked by repository analyzer test-project absence (`Scaffold.Analyzers.Tests.csproj` missing).

## Context and Orientation

Madbox currently uses modular assemblies under `Assets/Scripts/` with enforced boundaries and test requirements. The initialization research defines phase-based startup (`InstallInfra -> InstallCore -> InstallMeta -> InstallGame -> InstallApp`) and explicitly calls for an Addressables adapter in Infra that exposes project contracts to the rest of the game.

Entity research and addressables research documents require a scalable authoring/runtime flow where content like enemy definitions, level definitions, and prefabs can be resolved asynchronously, without pushing Unity object references or Addressables-specific concerns into core domain logic.

This plan introduces a new Infra module for Addressables with these characteristics:

1. One central gateway controls all addressables loading and unloading across asset types.
2. Public API remains small: initialize once, load assets, release handles.
3. Catalog refresh, download decisions, and handle/reference accounting stay internal.
4. Consumers request content by stable keys, catalog identifiers, or serialized references through one generic interface.

Key target paths in this plan:

1. `Assets/Scripts/Infra/Addressables/Runtime/` for runtime contracts and implementation.
2. `Assets/Scripts/Infra/Addressables/Container/` for VContainer installer and registration wiring.
3. `Assets/Scripts/Infra/Addressables/Tests/` for EditMode test coverage.
4. `Docs/Infra/Addressables.md` for module documentation.
5. `Assets/Scripts/App/Bootstrap/Runtime/BootstrapScope.cs` for layer registration order.
6. `Assets/Scripts/Infra/Scope/Runtime/Contracts/IAsyncLayerInitializable.cs` as the startup contract for the addressables initializer entry point.

Terms used in this plan:

1. Addressables gateway: the single service that receives load requests from consumers and returns releasable handles.
2. Asset handle: a lightweight object returned by load calls that tracks release ownership and gives typed access to loaded content.
3. Catalog: a named grouping or source context used to request multiple addressables (for example, all assets tagged for a feature).
4. Reference counting: internal tracking of how many active handles use the same loaded operation so assets are released only when safe.

## Plan of Work

The implementation starts by creating the Addressables module structure through the repository module workflow and defining stable contracts first. The public contract will include a minimal interface like `IAddressablesGateway` with `InitializeAsync` and generic `LoadAsync` overloads. Release is done only through the handle abstraction (`IAssetHandle.Release()`), not through gateway `Unload`. The overloads cover load by key, load by catalog, and load by serialized reference input while returning a typed handle abstraction.

Next, runtime implementation adds a central controller that delegates to one generic provider and one internal operation store. The operation store maps incoming load requests to Unity Addressables operations and tracks ownership/reference counts. The controller is the only place that coordinates lifecycle transitions, preventing scattered key usage and preventing direct Addressables calls in feature code.

After contracts and runtime internals are working, container wiring registers an addressables startup initializer into the Infra installation phase using `IAsyncLayerInitializable` from `Assets/Scripts/Infra/Scope/Runtime/Contracts/IAsyncLayerInitializable.cs`. `ScopeInitializer` in the layered scope startup flow becomes the single entry point that invokes addressables initialization through `Task InitializeAsync(CancellationToken)`. Bootstrap must not call gateway initialization directly; it must rely on scope-driven initialization order.

Milestone 2 extends internals with update and download behavior while preserving the same public API. Initialization performs catalog check/update and required dependency downloads based on policy and connectivity. CDN configuration is introduced as infra-owned settings so remote content location can be environment-specific without modifying feature modules.

Both milestones include unit/integration-style EditMode tests with fake addressables adapters to verify lifecycle, concurrency, and error handling deterministically.

## Concrete Steps

Run all commands from repository root: `C:\Unity\Madbox`.

1. Prepare module skeleton and baseline files.
   Commands:
    `Get-Content -Raw ".agents/workflows/create-module.md"`
    `rg --files Assets/Scripts/Infra | rg -i "Events|Navigation|Scope"`
2. Implement Milestone 1 scope and tests.
   Commands:
    `powershell -NoProfile -ExecutionPolicy Bypass -File ".\.agents\scripts\run-editmode-tests.ps1"`
    `powershell -NoProfile -ExecutionPolicy Bypass -File ".\.agents\scripts\check-analyzers.ps1"`
    `& ".\.agents\scripts\validate-changes.cmd"`
3. Implement Milestone 2 scope and tests.
   Commands:
    `powershell -NoProfile -ExecutionPolicy Bypass -File ".\.agents\scripts\run-editmode-tests.ps1"`
    `powershell -NoProfile -ExecutionPolicy Bypass -File ".\.agents\scripts\run-playmode-tests.ps1"`
    `powershell -NoProfile -ExecutionPolicy Bypass -File ".\.agents\scripts\check-analyzers.ps1"`
    `& ".\.agents\scripts\validate-changes.cmd"`

Expected success signals:

1. Addressables module exists with runtime/container/tests asmdefs and analyzer-clean code.
2. Scope startup resolves an addressables `IAsyncLayerInitializable` and invokes it in the intended layer order.
3. Consumers can load typed assets through one interface without feature-specific providers.
4. Catalog update and download flow can be exercised through tests and guarded runtime behavior.

## Validation and Acceptance

Milestone 1 acceptance is met when a consumer test can initialize the gateway, load assets by key/catalog/reference via generic endpoints, and release handles from the handle itself while internal reference tracking prevents premature release.

Milestone 2 acceptance is met when initialization can run catalog update and dependency download policy internally, including successful path and controlled-failure path tests, without changing the consumer-facing API.

Plan-level acceptance is met when:

1. `& ".\.agents\scripts\validate-changes.cmd"` passes after each milestone.
2. `Docs/Infra/Addressables.md` documents purpose, API, usage examples, and design choices.
3. No feature module performs direct Addressables API calls for standard load flows.
4. Addressables startup is triggered through `IAsyncLayerInitializable` and not through ad-hoc bootstrap calls.

## Main Entry Point and Consumer Example

The main runtime entry point for this plan is an Infra-layer initializer class (for example `AddressablesLayerInitializer`) that implements `Madbox.Scope.Contracts.IAsyncLayerInitializable`. Its responsibility is to call the gateway initialization once inside scope-managed startup ordering.

The explicit consumer example in this ExecPlan is `EnemiesService`. This is not a future optional example; Milestone 1 must include it in docs/tests to prove the generic API works for both single-item and list workflows.

Required `EnemiesService` usage flow:

1. Request startup initialization through scope (`IAsyncLayerInitializable`) before enemy loads.
2. Load one enemy by key (for example `enemy/{enemyId}`).
3. Load all enemies by catalog key (for example `enemy`).
4. Do not own preload caches in `EnemiesService`; preloading must come from a generic preload service.
5. Release one handle or all retained handles through `IAssetHandle.Release()`.

Strict ownership strategy required by this plan:

1. Every `LoadAsync` returns a new logical owner handle.
2. The code that receives the handle owns its lifetime and must call `Release` exactly once.
3. The central operation store decrements reference count on each `Release`.
4. Underlying Addressables release occurs only when reference count reaches zero.
5. `IAssetHandle` must be idempotent on repeated release attempts (second call is a no-op or controlled warning).
6. Feature services must not call a gateway-level release API.

Generic preload strategy required by this plan:

1. Preload registrations are centralized and consumed during gateway initialization.
2. `PreloadMode.Normal`: preload acquires one initial handle; first consumer receives that same handle reference and gateway drops ownership (simple handoff, no separate `Adopt` API).
3. `PreloadMode.NeverDie`: gateway retains the initial handle for full app lifetime (or until explicit shutdown), guaranteeing the asset stays resident.
4. `EnemiesService` can request assets normally but must not implement per-feature preload caches.

## Idempotence and Recovery

The plan is additive and safe to rerun. Creating the module, re-running tests, and re-running quality scripts are idempotent. If a milestone fails the gate, fix reported analyzer/test issues in-place and rerun the same commands until clean.

If remote-update behavior is unstable in Milestone 2, keep the public API unchanged and guard update/download logic behind internal policy flags so the baseline load/release path remains operational.

## Artifacts and Notes

During execution, append concise evidence snippets for:

1. Addressables gateway load/release tests passing.
2. Bootstrap initialization sequence including gateway initialization.
3. Catalog update/download tests with success and failure scenarios.
4. `validate-changes.cmd` clean output per milestone.

Sample `EnemiesService` snippet shape to include in docs/tests:

    await _gateway.InitializeAsync(ct);
    var beeHandle = await _gateway.LoadAsync<Enemy>(new AssetKey("enemy/bee"), ct);
    var allEnemyHandles = await _gateway.LoadAsync<Enemy>(new CatalogKey("enemy"), ct);
    beeHandle.Release();

Full `EnemiesService` sample snippet to include in `Docs/Infra/Addressables.md`:

    using System.Threading;
    using System.Threading.Tasks;

    public sealed class EnemiesService
    {
        private readonly IAddressablesGateway _gateway;

        public EnemiesService(IAddressablesGateway gateway)
        {
            _gateway = gateway;
        }

        public async Task<IAssetHandle<Enemy>> LoadEnemyAsync(string enemyId, CancellationToken ct)
        {
            return await _gateway.LoadAsync<Enemy>(new AssetKey($"enemy/{enemyId}"), ct);
        }

        public Task<IReadOnlyList<IAssetHandle<Enemy>>> LoadAllEnemiesAsync(CancellationToken ct)
        {
            return _gateway.LoadAsync<Enemy>(new CatalogKey("enemy"), ct);
        }
    }

Generic preload registry snippet (owned by Addressables Infra):

    using System.Threading;
    using System.Threading.Tasks;

    public enum PreloadMode
    {
        Normal,
        NeverDie
    }

    public interface IAddressablesPreloadRegistry
    {
        void Register(AssetKey key, PreloadMode mode);
        void Register(CatalogKey key, PreloadMode mode);
    }

    // Used during AddressablesLayerInitializer.InitializeAsync:
    // - Normal: preload and hand off the initial handle reference to first consumer.
    // - NeverDie: preload and keep gateway-owned handle alive for app lifetime.

Preload configuration snippet (installer-level):

    using Madbox.Scope.Contracts;

    public sealed class AddressablesPreloadInstaller : ILayerInstaller
    {
        public void Install(IContainerBuilder builder)
        {
            builder.RegisterBuildCallback(container =>
            {
                var preloads = container.Resolve<IAddressablesPreloadRegistry>();
                preloads.Register(new AssetKey("enemy/bee"), PreloadMode.Normal);
                preloads.Register(new CatalogKey("enemy"), PreloadMode.NeverDie);
            });
        }
    }

Startup entry-point snippet (Scope-owned):

    using System.Threading;
    using System.Threading.Tasks;
    using Madbox.Scope.Contracts;

    public sealed class AddressablesLayerInitializer : IAsyncLayerInitializable
    {
        private readonly IAddressablesGateway _gateway;

        public AddressablesLayerInitializer(IAddressablesGateway gateway)
        {
            _gateway = gateway;
        }

        public Task InitializeAsync(CancellationToken cancellationToken)
        {
            return _gateway.InitializeAsync(cancellationToken);
        }
    }

## Interfaces and Dependencies

The following interfaces and value types must exist by end of the plan, with concrete file placement decided during implementation within `Assets/Scripts/Infra/Addressables/Runtime/`:

1. `IAddressablesGateway`
   - `Task InitializeAsync(CancellationToken ct = default);`
   - `Task<IAssetHandle<T>> LoadAsync<T>(AssetKey key, CancellationToken ct = default) where T : UnityEngine.Object;`
   - `Task<IReadOnlyList<IAssetHandle<T>>> LoadAsync<T>(CatalogKey catalog, CancellationToken ct = default) where T : UnityEngine.Object;`
   - `Task<IAssetHandle<T>> LoadAsync<T>(AssetReferenceKey reference, CancellationToken ct = default) where T : UnityEngine.Object;`
2. `IAssetHandle` and `IAssetHandle<T>`
   - Expose request identity and loaded object.
   - Expose `void Release();` as the only consumer-facing release path.
   - No public methods for reference counting internals beyond release.
3. `IAddressablesPreloadRegistry` and `PreloadMode`
   - Generic preload registration consumed during initialization.
   - `PreloadMode.Normal` and `PreloadMode.NeverDie` are the only preload ownership modes.
4. `AssetKey`, `CatalogKey`, `AssetReferenceKey`
   - Immutable value objects that centralize identifiers.
5. Internal-only runtime collaborators
   - Generic provider for Addressables operations.
   - Operation store for ownership/reference counting.
   - Ownership transfer policy for `PreloadMode.Normal`.
   - Gateway-owned retained-handle store for `PreloadMode.NeverDie`.
   - Update/download coordinator (Milestone 2).
   - Telemetry/error mapper to convert engine failures into controlled results.

Dependencies and ownership rules:

1. Only the Addressables Infra module wraps Unity Addressables APIs.
2. Core/Game modules depend on contracts only and never on direct Addressables static calls.
3. App/Bootstrap can initialize the gateway but should not bypass it for loads.
4. Every load path has a deterministic release path through handle-owned `Release`.
5. Addressables startup initialization is owned by an Infra service implementing `IAsyncLayerInitializable` and executed by `ScopeInitializer`.
6. Preload ownership is centralized; feature services (including `EnemiesService`) must not implement preload caches.

## Revision Note

2026-03-17: Initial ExecPlan created to deliver a generic, centralized addressables system in two milestones, with Milestone 1 focused on core generic loading and Milestone 2 focused on catalog update, download, and CDN behavior.
2026-03-17: Added explicit `EnemiesService` sample flow and snippet expectations (single load, load-all, preload, release) to guide consumer-facing usage documentation and tests.
2026-03-17: Updated the main entry point to Scope-driven startup via `IAsyncLayerInitializable` and switched all API signatures in plan contracts from `ValueTask` to `Task`.
2026-03-17: Added full concrete code snippets for `EnemiesService` and `AddressablesLayerInitializer` so startup entry-point and consumer usage are immediately actionable.
2026-03-17: Changed ownership model to handle-driven release (`IAssetHandle.Release`), removed feature-level preload caching, and added a generic preload registry with `Normal` and `NeverDie` ownership modes.
2026-03-18: Executed Milestone 1 implementation, added Milestone 2 catalog/content sync internals with graceful failure test coverage, and recorded quality gate blocker caused by missing analyzer tests project file.
