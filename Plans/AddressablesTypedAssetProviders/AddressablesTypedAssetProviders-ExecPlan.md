# Move Addressables Preload to Typed Asset Providers and Thin the Gateway

This ExecPlan is a living document. The sections `Progress`, `Surprises & Discoveries`, `Decision Log`, and `Outcomes & Retrospective` must be kept up to date as work proceeds.

This document must be maintained in accordance with `PLANS.md` at the repository root.

## Purpose / Big Picture

After this change, preload behavior will no longer live inside `AddressablesGateway`. Instead, preload ownership will move to typed module providers (`AssetProvider<TAsset>`), so each module controls its own preload keys and child-scope registration in a type-safe way. The gateway becomes a small loading facade (load by reference/label), which reduces lines of code and lowers maintenance complexity.

A developer can verify the outcome by checking that: (1) gateway startup no longer parses preload config or stores preload dictionaries, (2) typed providers are resolved from DI and preload successfully, (3) child scope receives typed registrations without reflection-heavy gateway logic, and (4) all tests and `.agents/scripts/validate-changes.cmd` pass.

## Progress

- [x] (2026-03-21 00:00Z) Authored initial ExecPlan with a provider-first architecture, explicit class creation/removal list, migration snippets, and milestone validation sequence focused on line-count/complexity reduction.
- [ ] Execute Milestone 1: Add regression/characterization tests for current preload-to-child-registration behavior before refactor.
- [ ] Execute Milestone 2: Introduce `IAssetProvider` and generic `AssetProvider<TAsset>` foundation with DI wiring.
- [ ] Execute Milestone 3: Migrate bootstrap child registration to provider-driven typed registration and remove gateway preload cache contract.
- [ ] Execute Milestone 4: Remove old preload-config pipeline classes and gateway preload logic; keep gateway load-only.
- [ ] Execute Milestone 5: Update docs, run full quality gate, and finalize retrospective with complexity/LOC evidence.

## Surprises & Discoveries

- Observation: Gateway currently still owns preload config loading, translation, deduplication, and a preload asset dictionary used later by bootstrap child registration.
  Evidence: `Assets/Scripts/Infra/Addressables/Runtime/Implementation/AddressablesGateway.cs` contains `LoadPreloadRegistrationsAsync`, `ApplyPreloadAsync`, and `GetPreloadedAssets` flow.

- Observation: Child-scope preload registration currently depends on untyped `Type -> UnityEngine.Object` enumeration and runtime generic invocation.
  Evidence: `Assets/Scripts/App/Bootstrap/Runtime/BootstrapAssetInstaller.cs` resolves `IPreloadedAssetProvider` then registers each pair via `RegisterUntypedInstance` reflection.

- Observation: Preload responsibilities are split across multiple addressables abstractions that are no longer necessary if modules own typed preload declarations.
  Evidence: `IAssetPreloadHandler` and `AddressablesAssetPreloadHandler` exist only to convert preload config entries into registrations consumed by gateway startup.

## Decision Log

- Decision: Keep `IAddressablesGateway` as the stable loading contract for consumers, but remove preload ownership from it.
  Rationale: This keeps feature-call sites stable while shrinking gateway responsibility to one job.
  Date/Author: 2026-03-21 / Codex

- Decision: Introduce a provider abstraction per module with a shared generic base (`AssetProvider<TAsset>`) instead of keeping one generic preload config in gateway.
  Rationale: Module-local typed ownership is easier to evolve and supports child builder registration with compile-time types.
  Date/Author: 2026-03-21 / Codex

- Decision: Remove gateway-exposed preload dictionary contract after provider wiring is complete.
  Rationale: Eliminates untyped dictionary + reflection path and directly supports the objective of reducing lines and complexity.
  Date/Author: 2026-03-21 / Codex

## Outcomes & Retrospective

Not executed yet. This section will be updated after milestones are implemented and validated.

Expected measurable outcomes to record at completion:

1. Net line reduction in `AddressablesGateway.cs` and bootstrap preload registration path.
2. Number of deleted preload-specific classes/contracts.
3. Validation evidence that behavior remains correct (tests and full gate clean).

## Context and Orientation

Addressables and bootstrap files relevant to this refactor:

- `Assets/Scripts/Infra/Addressables/Runtime/Implementation/AddressablesGateway.cs` currently mixes load facade and preload startup logic.
- `Assets/Scripts/Infra/Addressables/Runtime/Contracts/IPreloadedAssetProvider.cs` exposes untyped preload dictionary.
- `Assets/Scripts/App/Bootstrap/Runtime/BootstrapAssetInstaller.cs` currently reads that dictionary and registers child scope instances by runtime `Type`.
- `Assets/Scripts/Infra/Addressables/Runtime/Contracts/IAssetPreloadHandler.cs` and `Assets/Scripts/Infra/Addressables/Runtime/Implementation/AddressablesAssetPreloadHandler.cs` convert config to preload registrations.
- `Assets/Scripts/Infra/Addressables/Runtime/Implementation/AddressablesPreloadConfig.cs` and related preload config entries/constants support current generic preload pipeline.
- `Assets/Scripts/Infra/Addressables/Container/AddressablesInstaller.cs` wires gateway and handlers.

In this plan, “typed provider” means a module-owned class that declares what to preload and what to expose to child scope using compile-time asset types. “Gateway load-only” means gateway is responsible only for `Load/LoadAsync` behavior, not preload orchestration or preload storage.

## Proposed Architecture

The runtime shape becomes provider-first:

1. Bootstrap resolves all registered `IAssetProvider` instances.
2. A bootstrap preload runner asks each provider to preload using the gateway.
3. Each provider returns typed assets for child registration (or directly registers through a typed callback).
4. Child builder registration no longer depends on gateway preload dictionary.
5. Gateway remains a thin asset loading facade.

Connection flow in plain terms:

- `IAddressablesGateway` is used by providers, not the other way around.
- Providers own preload keys/labels and module-specific interpretation.
- Bootstrap only orchestrates providers and registration; it does not parse generic preload config.

## Target APIs and Class Definitions

The following APIs are the target contract shape after migration.

Core provider contracts in `Assets/Scripts/Infra/Addressables/Runtime/Contracts/`:

    public interface IAssetProvider
    {
        Task PreloadAsync(IAddressablesGateway gateway, CancellationToken cancellationToken);
        void RegisterToChild(IContainerBuilder childBuilder);
    }

    public interface IAssetProvider<TAsset> : IAssetProvider
        where TAsset : UnityEngine.Object
    {
        bool TryGet(out TAsset asset);
    }

Generic base in `Assets/Scripts/Infra/Addressables/Runtime/Implementation/`:

    public abstract class AssetProvider<TAsset> : IAssetProvider<TAsset>
        where TAsset : UnityEngine.Object
    {
        protected TAsset LoadedAsset { get; private set; }

        public async Task PreloadAsync(IAddressablesGateway gateway, CancellationToken cancellationToken)
        {
            IAssetHandle<TAsset> handle = await LoadCoreAsync(gateway, cancellationToken);
            LoadedAsset = handle.Asset;
        }

        public bool TryGet(out TAsset asset)
        {
            asset = LoadedAsset;
            return asset != null;
        }

        public virtual void RegisterToChild(IContainerBuilder childBuilder)
        {
            if (LoadedAsset != null)
            {
                childBuilder.RegisterInstance(LoadedAsset);
            }
        }

        protected abstract Task<IAssetHandle<TAsset>> LoadCoreAsync(IAddressablesGateway gateway, CancellationToken cancellationToken);
    }

Example module provider (sample only, not implemented in this plan document):

    public sealed class LevelAssetProvider : AssetProvider<LevelDefinitionSO>
    {
        private readonly AssetLabelReference levelsLabel;

        public LevelAssetProvider(AssetLabelReference levelsLabel)
        {
            this.levelsLabel = levelsLabel;
        }

        protected override async Task<IAssetHandle<LevelDefinitionSO>> LoadCoreAsync(IAddressablesGateway gateway, CancellationToken cancellationToken)
        {
            // Sample only: real implementation may load by catalog reference or label strategy.
            AssetReferenceT<LevelDefinitionSO> firstLevel = ResolveFirstLevelReference();
            return await gateway.LoadAsync(firstLevel, cancellationToken);
        }

        private AssetReferenceT<LevelDefinitionSO> ResolveFirstLevelReference()
        {
            throw new NotImplementedException();
        }
    }

Bootstrap orchestration target in `BootstrapAssetInstaller`:

    IEnumerable<IAssetProvider> providers = parentResolver.Resolve<IEnumerable<IAssetProvider>>();
    foreach (IAssetProvider provider in providers)
    {
        await provider.PreloadAsync(gateway, cancellationToken);
        provider.RegisterToChild(childBuilder);
    }

The final implementation may choose a small dedicated coordinator class (for example `AssetProviderBootstrapRunner`) if needed for testability, but the responsibility split must remain provider-first and gateway-thin.

## Classes to Create, Touch, and Remove

### Classes to create

- `Assets/Scripts/Infra/Addressables/Runtime/Contracts/IAssetProvider.cs`
- `Assets/Scripts/Infra/Addressables/Runtime/Contracts/IAssetProviderT.cs` (or generic interface naming consistent with repository conventions)
- `Assets/Scripts/Infra/Addressables/Runtime/Implementation/AssetProvider.cs` (generic base `AssetProvider<TAsset>`)
- Optional small coordinator if needed:
  - `Assets/Scripts/App/Bootstrap/Runtime/AssetProviderBootstrapRunner.cs`

### Classes/files to modify

- `Assets/Scripts/Infra/Addressables/Runtime/Implementation/AddressablesGateway.cs`
  - Remove preload config loading, preload registration build/apply, preload dictionary, and `IPreloadedAssetProvider` implementation.
- `Assets/Scripts/Infra/Addressables/Container/AddressablesInstaller.cs`
  - Register provider contracts and remove obsolete preload handler wiring.
- `Assets/Scripts/App/Bootstrap/Runtime/BootstrapAssetInstaller.cs`
  - Replace dictionary-based reflection registration with provider orchestration and typed registration.
- `Docs/Infra/Addressables.md`
  - Document provider-first preload architecture and gateway load-only responsibility.
- Addressables and bootstrap tests under:
  - `Assets/Scripts/Infra/Addressables/Tests/`
  - `Assets/Scripts/App/Bootstrap/` test location (if present) or new tests in existing bootstrap test assembly.

### Classes/files to remove

- `Assets/Scripts/Infra/Addressables/Runtime/Contracts/IPreloadedAssetProvider.cs`
- `Assets/Scripts/Infra/Addressables/Runtime/Contracts/IAssetPreloadHandler.cs`
- `Assets/Scripts/Infra/Addressables/Runtime/Implementation/AddressablesAssetPreloadHandler.cs`
- Preload config pipeline files if no runtime callers remain:
  - `Assets/Scripts/Infra/Addressables/Runtime/Implementation/AddressablesPreloadConfig.cs`
  - related preload config entry/constants files in same folder

Removal rule: delete only when tests and compile prove no runtime usage remains. If editor tooling still needs part of config assets, keep editor-only pieces and remove runtime dependency from gateway.

## Plan of Work

Milestone 1 establishes behavior safety before structural changes. Add or update tests that lock current expected outcomes for preload handoff and child-scope registration so migration can prove no regressions.

Milestone 2 introduces provider contracts and the generic base with minimal implementation. Wire one fake or test provider first to prove bootstrap orchestration works end-to-end with typed registration.

Milestone 3 migrates bootstrap registration from gateway preload dictionary to provider orchestration. Keep additive compatibility temporarily if needed (bridge phase), then remove bridge once tests pass.

Milestone 4 removes preload responsibilities from gateway and deletes obsolete preload handler/config runtime classes. Keep gateway focused on load APIs and runtime guards.

Milestone 5 updates documentation and runs full validation loop until clean analyzer and test results.

For each milestone, follow repository quality loop:

1. Implement milestone scope.
2. If bug fix is involved, add regression test and confirm fail-before/fix/pass-after.
3. Run `.agents/scripts/validate-changes.cmd`.
4. Fix failures and rerun until clean.
5. Commit milestone changes.

## Concrete Steps

Run all commands from repository root `C:\Unity\Madbox`.

1. Baseline targeted tests before code movement:

    powershell -NoProfile -ExecutionPolicy Bypass -File ".\.agents\scripts\run-editmode-tests.ps1" -AssemblyNames "Madbox.Addressables.Tests"

2. Implement Milestone 1 tests and rerun the same command until green.

3. Implement Milestone 2 and 3 changes, then rerun:

    powershell -NoProfile -ExecutionPolicy Bypass -File ".\.agents\scripts\run-editmode-tests.ps1" -AssemblyNames "Madbox.Addressables.Tests"

4. Remove obsolete preload classes in Milestone 4, then run:

    powershell -NoProfile -ExecutionPolicy Bypass -File ".\.agents\scripts\check-analyzers.ps1"

5. Run required full gate at each milestone completion:

    .\.agents\scripts\validate-changes.cmd

Expected final state: EditMode tests pass, PlayMode tests pass, analyzer output is `TOTAL:0`, and no remaining runtime dependency on old preload-gateway pipeline.

## Validation and Acceptance

Acceptance is successful only when all behavior and simplification criteria below are met.

Behavior criteria:

1. Startup still initializes Addressables and gameplay continues to load required assets.
2. Providers preload assets before child registration, and child scope resolves those typed assets.
3. Existing gateway loading behavior (`Load/LoadAsync` by references/labels) remains functional.

Simplification criteria (explicit objective):

1. `AddressablesGateway` has no preload config loading and no preload dictionary cache.
2. Gateway no longer implements `IPreloadedAssetProvider`.
3. Reflection-heavy untyped child registration path in bootstrap is removed.
4. Obsolete preload handler/config runtime pipeline is deleted or isolated away from runtime.
5. Net code volume decreases in Addressables preload path (to be recorded with before/after line counts in retrospective).

Validation commands that must pass:

    powershell -NoProfile -ExecutionPolicy Bypass -File ".\.agents\scripts\run-editmode-tests.ps1"
    powershell -NoProfile -ExecutionPolicy Bypass -File ".\.agents\scripts\run-playmode-tests.ps1"
    .\.agents\scripts\validate-changes.cmd

## Idempotence and Recovery

This migration is additive-first and safe to rerun. If a milestone partially fails, keep the new tests, revert only incomplete runtime edits, and reapply in smaller steps.

If provider migration introduces startup ordering issues, keep a temporary compatibility bridge (old preload + new provider flow) for one milestone only, then remove it before completion to satisfy simplification goals.

If deleting preload config classes breaks non-runtime authoring workflows, move that part to editor-only scope and keep runtime gateway decoupled.

## Artifacts and Notes

As execution proceeds, store concise evidence snippets proving both behavior parity and simplification.

Required artifacts to capture:

1. Before/after line count for `AddressablesGateway.cs`.
2. List of deleted preload classes/contracts.
3. Test summaries for Addressables, bootstrap integration, and full gate.

Example evidence format to populate during execution:

    Command: .\.agents\scripts\validate-changes.cmd
    Result: scripts asmdef PASS, compilation PASS, EditMode PASS, PlayMode PASS, analyzers PASS (TOTAL:0)

## Interfaces and Dependencies

Stable boundary to preserve:

- `Madbox.Addressables.Contracts.IAddressablesGateway` remains the module loading facade used by consumers.

New dependency direction:

- Bootstrap depends on `IEnumerable<IAssetProvider>`.
- Providers depend on `IAddressablesGateway`.
- Gateway does not depend on concrete providers.

Architectural constraints to preserve:

- Keep Unity-specific runtime behavior in Infra/App layers.
- Keep explicit asmdef dependencies.
- Keep analyzer compliance with no unapproved pragma suppressions.
- Keep tests for all touched modules and add regression tests for bug fixes.

---

Revision Note (2026-03-21 / Codex): Created initial provider-first ExecPlan focused on reducing lines and complexity by removing preload logic from `AddressablesGateway`, with explicit create/touch/remove class map and implementation snippets.
