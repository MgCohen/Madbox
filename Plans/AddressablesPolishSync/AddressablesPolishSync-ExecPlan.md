# Addressables Asset-Layer Revamp with Child Registration Preload Injection

This ExecPlan is a living document. The sections `Progress`, `Surprises & Discoveries`, `Decision Log`, and `Outcomes & Retrospective` must be kept up to date as work proceeds.

This document must be maintained in accordance with `PLANS.md` at the repository root.

## Purpose / Big Picture

After this change, Addressables startup will no longer be an Infra-layer concern in bootstrap composition. We will introduce a dedicated Asset layer that runs before Infra, initializes Addressables there, reads one preload definition asset, preloads configured assets, and delegates preloaded instances into child-scope registration during async initialization.

The user-visible behavior is that services in the next scope (and optionally later scopes, based on policy) can resolve required preloaded assets directly from DI without knowing anything about Addressables keys, labels, or gateway calls. Addressables remains the loading implementation detail, while consumers receive ready-to-use typed instances.

This plan also keeps the previously requested polish scope in one track: shared preload config source-of-truth constant, regression test for mixed-validity wrappers, and deferred synchronous handle API.

## Progress

- [x] (2026-03-18 18:20Z) Authored initial polish-and-sync ExecPlan from current Addressables runtime state and user requirements.
- [x] (2026-03-18 18:49Z) Revised ExecPlan to include Asset-layer bootstrap revamp, single preload file flow, and child-scope delegated registration from Addressables async initialization.
- [ ] Execute Milestone 1: Introduce Asset bootstrap layer before Infra and move Addressables installer/initializer ownership there.
- [ ] Execute Milestone 2: Promote preload identifier to one shared source-of-truth constant and convert preload discovery to one preload definition asset file.
- [ ] Execute Milestone 3: Add regression test for "multiple wrappers loaded, one invalid entry" and verify fail-before/fix/pass-after.
- [ ] Execute Milestone 4: During Addressables async initialization, preload entries and delegate type+instance registrations into child scope.
- [ ] Execute Milestone 5: Add deferred synchronous asset-handle API with explicit state enum and preserve existing async behavior.
- [ ] Execute Milestone 6: Update docs and run full quality gate until clean.

## Surprises & Discoveries

- Observation: Bootstrap currently builds only one layer installer and it is Infra.
  Evidence: `Assets/Scripts/App/Bootstrap/Runtime/BootstrapScope.cs` returns only `BootstrapInfraInstaller`.

- Observation: Addressables currently registers `IAsyncLayerInitializable` inside `AddressablesInstaller`, which makes initialization happen in whatever layer installs this installer.
  Evidence: `Assets/Scripts/Infra/Addressables/Container/AddressablesInstaller.cs`.

- Observation: New scope system already supports delegated child registrations during async initialization through `ILayerInitializationContext`.
  Evidence: `Assets/Scripts/Infra/Scope/Runtime/ScopeInitializer.cs` and `Assets/Scripts/Infra/Scope/Runtime/Contracts/ILayerInitializationContext.cs`.

- Observation: `RegisterInstanceForChild` requires `Lifetime.Singleton`, so preload instance delegation must use singleton lifetime.
  Evidence: `Assets/Scripts/Infra/Scope/Tests/ScopeInitializerTests.cs` (`InvalidLifetimeInitializer`).

- Observation: Preload label string is currently duplicated and hardcoded.
  Evidence: `Assets/Scripts/Infra/Addressables/Runtime/Implementation/AddressablesStartupCoordinator.cs`, `Assets/Scripts/Infra/Addressables/Tests/AddressablesGatewayTests.cs`, `Docs/Infra/Addressables.md`.

## Decision Log

- Decision: Introduce `BootstrapAssetInstaller` and place it before `BootstrapInfraInstaller` in `BootstrapScope.BuildLayerInstallers()`.
  Rationale: Makes asset warmup and delegated registrations available to Infra scope startup deterministically.
  Date/Author: 2026-03-18 / Codex + User direction

- Decision: Keep Addressables runtime assembly location unchanged for this revamp; change startup ownership by layer ordering and installer wiring first.
  Rationale: Delivers behavior quickly with low migration risk; physical module relocation can be a later refactor if still needed.
  Date/Author: 2026-03-18 / Codex

- Decision: Use one preload definition asset file as startup input instead of runtime label discovery for multiple wrapper files.
  Rationale: User requested a single preload file and deterministic startup source.
  Date/Author: 2026-03-18 / Codex + User direction

- Decision: Delegate preloaded assets to child scopes using `RegisterInstanceForChild(serviceType, instance, Lifetime.Singleton, policy)`.
  Rationale: Enables DI-native consumption by dependent services without Addressables coupling.
  Date/Author: 2026-03-18 / Codex + User direction

- Decision: Use `ChildScopeDelegationPolicy.AllDescendants` by default for preloaded asset instances.
  Rationale: Ensures readiness is available not only to immediate Infra child but also to later layers unless explicitly narrowed.
  Date/Author: 2026-03-18 / Codex

- Decision: Keep async gateway methods as-is and add deferred synchronous handle methods additively.
  Rationale: Backward-compatible API expansion.
  Date/Author: 2026-03-18 / Codex + User direction

## Outcomes & Retrospective

This section will be updated as milestones complete. At completion it will summarize scope ordering changes, preload-injection behavior, API additions, migration notes, and final gate evidence.

## Context and Orientation

Bootstrap layering is controlled by `Assets/Scripts/App/Bootstrap/Runtime/BootstrapScope.cs`, which currently returns only one installer (`BootstrapInfraInstaller`). That means all startup-initializable services currently run inside Infra child scope.

Addressables composition is currently wired by `Assets/Scripts/Infra/Addressables/Container/AddressablesInstaller.cs`. It registers `IAddressablesGateway`, `IAddressablesAssetClient`, and `IAsyncLayerInitializable` (`AddressablesLayerInitializer`).

Scope startup execution now supports delegated child registration:
`IAsyncLayerInitializable.InitializeAsync(ILayerInitializationContext context, IObjectResolver resolver, CancellationToken token)`
and `context.RegisterInstanceForChild(...)`.
`LayeredScope` applies delegated registrations when creating child scopes.

Current preload flow discovers wrappers by label in `AddressablesStartupCoordinator`. The target design in this plan changes that to one preload definition asset file and injects loaded instances into child scope.

In this plan, "single preload file" means one Addressable ScriptableObject (or equivalent single root asset) that contains all preload entries used during startup.

## Plan of Work

Milestone 1 introduces explicit Asset-first layering in bootstrap.

Add `BootstrapAssetInstaller` under `Assets/Scripts/App/Bootstrap/Runtime/`.

Move Addressables installer call out of `BootstrapInfraInstaller` into `BootstrapAssetInstaller`.

Update `BootstrapScope.BuildLayerInstallers()` from:
Infra-only
to:
Asset then Infra.

Validate that Addressables async initializer runs in Asset layer and Infra initializers run after delegated registrations are available.

Milestone 2 makes preload startup deterministic and removes brittle duplication.

Create shared startup constants (for preload file key and any remaining label constants) in one runtime type.

Replace hardcoded literals in runtime, tests, and docs.

Change startup preload discovery from "resolve all wrappers by label" to "load one preload definition asset file" and build requests from that file.

Keep entry validation behavior strict and actionable.

Milestone 3 adds regression protection for mixed wrapper validity.

Extend `AddressablesGatewayTests` with the required scenario:
multiple wrappers are considered, one contains invalid entry.

Prove fail-before with test red, implement fix/guard, then prove pass-after.

Assert that preload apply path does not run when invalid config is encountered.

Milestone 4 injects preloaded assets into child scope during async initialization.

During Asset-layer `AddressablesLayerInitializer.InitializeAsync(...)`, after gateway/preload startup logic resolves assets, register each preloaded instance into child scope:
`context.RegisterInstanceForChild(resolvedServiceType, resolvedAssetInstance, Lifetime.Singleton, ChildScopeDelegationPolicy.AllDescendants)`.

Define duplicate-service-type behavior explicitly (recommended: fail fast if more than one preload maps to the same registration service type).

Ensure registrations happen only after successful preload of each entry and that failures abort initialization with clear errors.

Milestone 5 adds deferred synchronous handle API.

Add handle state enum in contracts:
`Loading`, `Ready`, `Faulted`, `Released`.

Extend `IAssetHandle` with state/readiness/awaitable completion members.

Add additive deferred methods on `IAddressablesGateway` (for key/reference/typed-reference) that return handle immediately.

Refactor handle and lease-store internals so existing async methods remain behavior-compatible and deferred methods share ownership/reference-count logic.

Milestone 6 updates docs and closes quality gates.

Update `Docs/Infra/Addressables.md` and `Docs/App/Bootstrap.md` with new Asset-layer startup sequence and preload injection semantics.

Run EditMode, PlayMode, analyzers, and full gate repeatedly until clean.

## Clear Snippets for Big Changes

Use these snippets as implementation anchors. Final names can differ, but behavior must match.

Bootstrap layer ordering change in `BootstrapScope.BuildLayerInstallers()`:

    // Before
    protected override IReadOnlyList<ILayerInstaller> BuildLayerInstallers()
    {
        return new ILayerInstaller[] { new BootstrapInfraInstaller(navigationSettings, viewHolder) };
    }

    // After
    protected override IReadOnlyList<ILayerInstaller> BuildLayerInstallers()
    {
        return new ILayerInstaller[]
        {
            new BootstrapAssetInstaller(),
            new BootstrapInfraInstaller(navigationSettings, viewHolder)
        };
    }

New Asset installer owning Addressables registration:

    internal sealed class BootstrapAssetInstaller : ILayerInstaller
    {
        public void Install(IContainerBuilder builder)
        {
            AddressablesInstaller installer = new AddressablesInstaller();
            installer.Install(builder);
        }
    }

Preload source-of-truth constants (single place):

    internal static class AddressablesPreloadConstants
    {
        public const string ConfigAssetKey = "addressables/preload/config";
        public const string ConfigLabel = "addressables-preload-config";
    }

Addressables initializer registering preloaded instances to child scope:

    public async Task InitializeAsync(ILayerInitializationContext context, IObjectResolver resolver, CancellationToken cancellationToken)
    {
        await gateway.InitializeAsync(cancellationToken);
        IReadOnlyList<PreloadedAssetRegistration> registrations = await preloadInjection.LoadRegistrationsAsync(cancellationToken);

        foreach (PreloadedAssetRegistration registration in registrations)
        {
            context.RegisterInstanceForChild(
                registration.ServiceType,
                registration.Instance,
                Lifetime.Singleton,
                ChildScopeDelegationPolicy.AllDescendants);
        }
    }

Deferred sync-handle API shape (additive):

    public interface IAssetHandle
    {
        string Id { get; }
        Type AssetType { get; }
        bool IsReleased { get; }
        AssetHandleState State { get; }
        bool IsReady { get; }
        Task WhenReady { get; }
        void Release();
    }

    public interface IAddressablesGateway
    {
        Task<IAssetHandle<T>> LoadAsync<T>(AssetKey key, CancellationToken cancellationToken = default) where T : UnityEngine.Object;
        IAssetHandle<T> Load<T>(AssetKey key, CancellationToken cancellationToken = default) where T : UnityEngine.Object;
    }

Regression test target for mixed wrapper validity:

    [Test]
    public void InitializeAsync_WhenMultipleWrappersAndOneInvalid_ThrowsBeforePreloadApply()
    {
        // Arrange two wrappers resolved by startup: first valid, second invalid.
        // Act + Assert initialization throws InvalidOperationException.
        // Assert preload apply side-effect count remains zero.
    }

## Concrete Steps

Run all commands from repository root `C:\Users\mtgco\.codex\worktrees\4717\Madbox`.

1. Baseline tests:

    powershell -NoProfile -ExecutionPolicy Bypass -File ".\.agents\scripts\run-editmode-tests.ps1" -AssemblyNames "Madbox.Addressables.Tests"
    powershell -NoProfile -ExecutionPolicy Bypass -File ".\.agents\scripts\run-editmode-tests.ps1" -AssemblyNames "Madbox.Scope.Tests"

2. Implement Milestone 1 and rerun both test assemblies.

3. Implement Milestone 2 and Milestone 3 with test-first bugfix loop:

    powershell -NoProfile -ExecutionPolicy Bypass -File ".\.agents\scripts\run-editmode-tests.ps1" -AssemblyNames "Madbox.Addressables.Tests"

4. Implement Milestone 4 preload child-registration injection and validate with Addressables + Scope tests.

5. Implement Milestone 5 deferred sync API and rerun Addressables tests.

6. Run broader checks:

    powershell -NoProfile -ExecutionPolicy Bypass -File ".\.agents\scripts\run-playmode-tests.ps1" -AssemblyNames "Madbox.Addressables.PlayModeTests"
    powershell -NoProfile -ExecutionPolicy Bypass -File ".\.agents\scripts\check-analyzers.ps1"
    .\.agents\scripts\validate-changes.cmd

7. If gate fails, fix and rerun step 6 until clean.

## Validation and Acceptance

Acceptance is met only when all behavior statements are true.

Bootstrap layer sequence includes Asset before Infra, and Addressables startup runs in Asset layer.

Addressables preload startup reads from one preload definition asset file and no runtime code path relies on duplicated hardcoded preload labels.

A regression test exists for "multiple wrappers loaded, one invalid entry" with fail-before/fix/pass-after evidence.

After Asset-layer async initialization completes, child scopes can resolve preloaded assets directly from DI without using Addressables gateway APIs.

Delegated instance registrations use valid lifetime/policy and pass scope tests.

Deferred handle API returns immediately with explicit state lifecycle and does not regress existing async API behavior.

Docs describe new startup order and preload injection flow.

Required validation commands:

    powershell -NoProfile -ExecutionPolicy Bypass -File ".\.agents\scripts\run-editmode-tests.ps1" -AssemblyNames "Madbox.Addressables.Tests"
    powershell -NoProfile -ExecutionPolicy Bypass -File ".\.agents\scripts\run-editmode-tests.ps1" -AssemblyNames "Madbox.Scope.Tests"
    powershell -NoProfile -ExecutionPolicy Bypass -File ".\.agents\scripts\run-playmode-tests.ps1" -AssemblyNames "Madbox.Addressables.PlayModeTests"
    powershell -NoProfile -ExecutionPolicy Bypass -File ".\.agents\scripts\check-analyzers.ps1"
    .\.agents\scripts\validate-changes.cmd

## Idempotence and Recovery

All milestones are additive-first and can be applied incrementally.

If Asset-layer split causes startup regressions, keep the new installer file and revert only layer-order wiring first, then reintroduce ordering after tests stabilize.

If preload child-registration produces ambiguous duplicate service mappings, fail fast and require explicit config disambiguation rather than silent overwrite.

If deferred handle refactor destabilizes release semantics, keep new contracts but temporarily route deferred methods through existing async load path until lease-store refactor is corrected.

## Artifacts and Notes

Expected touched files include:

`Assets/Scripts/App/Bootstrap/Runtime/BootstrapScope.cs`
`Assets/Scripts/App/Bootstrap/Runtime/BootstrapInfraInstaller.cs`
`Assets/Scripts/App/Bootstrap/Runtime/BootstrapAssetInstaller.cs` (new)
`Assets/Scripts/Infra/Addressables/Container/AddressablesInstaller.cs`
`Assets/Scripts/Infra/Addressables/Runtime/Implementation/AddressablesLayerInitializer.cs`
`Assets/Scripts/Infra/Addressables/Runtime/Implementation/AddressablesStartupCoordinator.cs`
`Assets/Scripts/Infra/Addressables/Runtime/Implementation/AddressablesPreloadConfigRequestBuilder.cs`
`Assets/Scripts/Infra/Addressables/Runtime/Implementation/AddressablesLeaseStore.cs`
`Assets/Scripts/Infra/Addressables/Runtime/Implementation/AssetHandle.cs`
`Assets/Scripts/Infra/Addressables/Runtime/Contracts/IAddressablesGateway.cs`
`Assets/Scripts/Infra/Addressables/Runtime/Contracts/IAssetHandle.cs`
`Assets/Scripts/Infra/Addressables/Tests/AddressablesGatewayTests.cs`
`Assets/Scripts/Infra/Scope/Tests/ScopeInitializerTests.cs` (if injection behavior needs dedicated verification)
`Docs/Infra/Addressables.md`
`Docs/App/Bootstrap.md`

Execution evidence to append while implementing:

Fail-before and pass-after output for mixed-wrapper invalid-entry regression test.

Evidence that preloaded assets are resolvable from child scope after Asset-layer initialization.

Final clean `validate-changes.cmd` summary.

## Interfaces and Dependencies

Stable interfaces to preserve:

`Madbox.Addressables.Contracts.IAddressablesGateway`
`Madbox.Addressables.Contracts.IAssetHandle`
`Madbox.Addressables.Contracts.IAssetHandle<T>`
`Madbox.Scope.Contracts.IAsyncLayerInitializable`
`Madbox.Scope.Contracts.ILayerInitializationContext`

New/updated interface expectations:

Additive deferred load methods on `IAddressablesGateway`.

Handle state enum contract and readiness members on `IAssetHandle`.

Preload constants single source-of-truth type.

Boundary rules:

Addressables remains an infra module implementation detail, but bootstrap layer ownership moves to Asset-first startup sequencing.

No presentation/UI dependencies in runtime contracts.

Keep asmdef dependencies explicit and analyzer-clean.

---

Revision Note (2026-03-18 / Codex): Created initial polish-focused plan for constants, regression test, and deferred sync handle API.
Revision Note (2026-03-18 / Codex): Expanded plan with Asset-layer bootstrap revamp, single preload file startup, and child-scope instance delegation during async initialization.
