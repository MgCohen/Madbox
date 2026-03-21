# Move Addressables Preload to Bootstrap Provider/Registrar Flow and Thin the Gateway

This ExecPlan is a living document. The sections `Progress`, `Surprises & Discoveries`, `Decision Log`, and `Outcomes & Retrospective` must be kept up to date as work proceeds.

This document must be maintained in accordance with `PLANS.md` at the repository root.

## Purpose / Big Picture

After this change, preload behavior will no longer live inside `AddressablesGateway`. Instead, bootstrap will own preload orchestration, and module assets will be handled by concrete providers that implement two clean interfaces: one interface dedicated to obtaining assets (`IAssetProvider`) and another dedicated to child-scope registration (`IAssetRegistrar`). The gateway becomes a small loading facade (load by reference/label), which reduces lines of code and lowers maintenance complexity.

A developer can verify the outcome by checking that: (1) `LayerInstallerBase` runs parent `InitializeAsync` and `OnCompletedAsync` before child creation, (2) `BootstrapAssetInstaller` preloads assets in `OnCompletedAsync` and stores them locally, (3) child scope receives typed registrations from `IAssetRegistrar` at creation time, (4) gateway startup no longer parses preload config or stores preload dictionaries, and (5) all tests and `.agents/scripts/validate-changes.cmd` pass.

## Progress

- [x] (2026-03-21 00:00Z) Authored initial ExecPlan with provider-first architecture and simplification goals.
- [ ] Execute Milestone 1: Add regression test proving parent completion currently happens too late for child registration needs, then fix `LayerInstallerBase` order.
- [ ] Execute Milestone 2: Introduce `IAssetProvider` and `IAssetRegistrar` split contracts plus concrete provider pattern.
- [ ] Execute Milestone 3: Move preload orchestration to `BootstrapAssetInstaller.OnCompletedAsync` and local cache, then register in `ConfigureChildBuilder`.
- [ ] Execute Milestone 4: Remove gateway preload ownership and old preload pipeline files/contracts.
- [ ] Execute Milestone 5: Update docs, run full quality gate, and finalize retrospective with complexity/LOC evidence.

## Surprises & Discoveries

- Observation: Gateway currently still owns preload config loading, translation, deduplication, and a preload asset dictionary used later by bootstrap child registration.
  Evidence: `Assets/Scripts/Infra/Addressables/Runtime/Implementation/AddressablesGateway.cs` contains `LoadPreloadRegistrationsAsync`, `ApplyPreloadAsync`, and `GetPreloadedAssets` flow.

- Observation: Child-scope registration currently requires parent data that is only available after parent completion, but parent completion currently runs after children are built.
  Evidence: `Assets/Scripts/Infra/Scope/Runtime/LayerInstallerBase.cs` currently executes `InitializeAsync -> BuildChildrenAsync -> OnCompletedAsync`.

- Observation: Preload responsibilities are split across multiple addressables abstractions that are no longer necessary if modules own typed preload declarations.
  Evidence: `IAssetPreloadHandler` and `AddressablesAssetPreloadHandler` exist only to convert preload config entries into registrations consumed by gateway startup.

## Decision Log

- Decision: Keep `IAddressablesGateway` as the stable loading contract for consumers, but remove preload ownership from it.
  Rationale: This keeps feature-call sites stable while shrinking gateway responsibility to one job.
  Date/Author: 2026-03-21 / Codex

- Decision: Split responsibilities into `IAssetProvider` (obtain/hold asset) and `IAssetRegistrar` (register typed services to child builder), with concrete providers implementing both.
  Rationale: This avoids overloading one interface with unrelated concerns and keeps contracts explicit.
  Date/Author: 2026-03-21 / Codex

- Decision: Fix build order bug in `LayerInstallerBase` so parent can complete preload before child creation.
  Rationale: `BootstrapAssetInstaller` must preload in `OnCompletedAsync` and consume results during child creation.
  Date/Author: 2026-03-21 / Codex

- Decision: Remove gateway-exposed preload dictionary contract after provider/registrar wiring is complete.
  Rationale: Eliminates untyped dictionary + reflection path and directly supports the objective of reducing lines and complexity.
  Date/Author: 2026-03-21 / Codex

## Outcomes & Retrospective

Not executed yet. This section will be updated after milestones are implemented and validated.

Expected measurable outcomes to record at completion:

1. Net line reduction in `AddressablesGateway.cs` and bootstrap registration path.
2. Number of deleted preload-specific classes/contracts.
3. Validation evidence that behavior remains correct (tests and full gate clean).

## Context and Orientation

Addressables and bootstrap files relevant to this refactor:

- `Assets/Scripts/Infra/Addressables/Runtime/Implementation/AddressablesGateway.cs` currently mixes load facade and preload startup logic.
- `Assets/Scripts/Infra/Addressables/Runtime/Contracts/IPreloadedAssetProvider.cs` exposes untyped preload dictionary.
- `Assets/Scripts/App/Bootstrap/Runtime/BootstrapAssetInstaller.cs` currently reads a gateway dictionary and registers child scope instances by runtime `Type`.
- `Assets/Scripts/Infra/Addressables/Runtime/Contracts/IAssetPreloadHandler.cs` and `Assets/Scripts/Infra/Addressables/Runtime/Implementation/AddressablesAssetPreloadHandler.cs` convert config to preload registrations.
- `Assets/Scripts/Infra/Addressables/Runtime/Implementation/AddressablesPreloadConfig.cs` and related preload config entries/constants support current generic preload pipeline.
- `Assets/Scripts/Infra/Addressables/Container/AddressablesInstaller.cs` wires gateway and handlers.
- `Assets/Scripts/Infra/Scope/Runtime/LayerInstallerBase.cs` currently builds children before parent `OnCompletedAsync`.

In this plan, “asset provider” means a module-owned concrete class that knows how to obtain/store its asset. “asset registrar” means a class that knows how to register assets into child `IContainerBuilder`. “Gateway load-only” means gateway is responsible only for `Load/LoadAsync` behavior, not preload orchestration or preload storage.

## Proposed Architecture

The runtime shape becomes bootstrap-driven with clean contracts:

1. `LayerInstallerBase` pipeline runs parent `InitializeAsync`, then parent `OnCompletedAsync`, then builds children.
2. `BootstrapAssetInstaller.OnCompletedAsync` resolves all providers/registrars, preloads assets, and stores loaded data in local fields.
3. During child creation, `BootstrapAssetInstaller.ConfigureChildBuilder` uses only local preloaded state and `IAssetRegistrar` methods to register typed assets.
4. Child builder registration no longer depends on gateway preload dictionary.
5. Gateway remains a thin asset loading facade.

Connection flow in plain terms:

- Concrete providers may depend on `IAddressablesGateway` in their constructor, but `IAssetProvider` interface itself does not require gateway parameters.
- `IAssetRegistrar` is the only contract that knows about `IContainerBuilder`.
- Bootstrap owns orchestration timing; providers own asset retrieval; registrars own typed registration.

## Target APIs and Class Definitions

The following APIs are the target contract shape after migration.

Core contracts in `Assets/Scripts/Infra/Addressables/Runtime/Contracts/`:

    public interface IAssetProvider
    {
        Task PreloadAsync(CancellationToken cancellationToken);
    }

    public interface IAssetProvider<TAsset> : IAssetProvider
        where TAsset : UnityEngine.Object
    {
        bool TryGet(out TAsset asset);
    }

    public interface IAssetRegistrar
    {
        void Register(IContainerBuilder builder);
    }

Generic base in `Assets/Scripts/Infra/Addressables/Runtime/Implementation/` (asset retrieval only):

    public abstract class AssetProvider<TAsset> : IAssetProvider<TAsset>
        where TAsset : UnityEngine.Object
    {
        protected TAsset LoadedAsset { get; private set; }

        public async Task PreloadAsync(CancellationToken cancellationToken)
        {
            IAssetHandle<TAsset> handle = await LoadCoreAsync(cancellationToken);
            LoadedAsset = handle.Asset;
        }

        public bool TryGet(out TAsset asset)
        {
            asset = LoadedAsset;
            return asset != null;
        }

        protected abstract Task<IAssetHandle<TAsset>> LoadCoreAsync(CancellationToken cancellationToken);
    }

Concrete provider+registrar example (sample only, not implemented in this plan document):

    public sealed class LevelAssetProvider : AssetProvider<LevelDefinitionSO>, IAssetRegistrar
    {
        private readonly IAddressablesGateway gateway;
        private readonly AssetReferenceT<LevelDefinitionSO> levelReference;

        public LevelAssetProvider(IAddressablesGateway gateway, AssetReferenceT<LevelDefinitionSO> levelReference)
        {
            this.gateway = gateway;
            this.levelReference = levelReference;
        }

        protected override Task<IAssetHandle<LevelDefinitionSO>> LoadCoreAsync(CancellationToken cancellationToken)
        {
            return gateway.LoadAsync(levelReference, cancellationToken);
        }

        public void Register(IContainerBuilder builder)
        {
            if (TryGet(out LevelDefinitionSO asset))
            {
                builder.RegisterInstance(asset);
            }
        }
    }

Bootstrap orchestration target in `BootstrapAssetInstaller`:

    private readonly List<IAssetProvider> localProviders = new List<IAssetProvider>();
    private readonly List<IAssetRegistrar> localRegistrars = new List<IAssetRegistrar>();

    protected override async Task OnCompletedAsync(IObjectResolver resolver, CancellationToken cancellationToken)
    {
        IEnumerable<IAssetProvider> providers = resolver.Resolve<IEnumerable<IAssetProvider>>();
        foreach (IAssetProvider provider in providers)
        {
            await provider.PreloadAsync(cancellationToken);
            localProviders.Add(provider);
            if (provider is IAssetRegistrar registrar)
            {
                localRegistrars.Add(registrar);
            }
        }
    }

    protected override void ConfigureChildBuilder(LayerInstallerBase child, IObjectResolver parentResolver, IContainerBuilder childBuilder)
    {
        for (int i = 0; i < localRegistrars.Count; i++)
        {
            localRegistrars[i].Register(childBuilder);
        }
    }

## Classes to Create, Touch, and Remove

### Classes to create

- `Assets/Scripts/Infra/Addressables/Runtime/Contracts/IAssetProvider.cs`
- `Assets/Scripts/Infra/Addressables/Runtime/Contracts/IAssetRegistrar.cs`
- `Assets/Scripts/Infra/Addressables/Runtime/Implementation/AssetProvider.cs` (generic base `AssetProvider<TAsset>`)

### Classes/files to modify

- `Assets/Scripts/Infra/Scope/Runtime/LayerInstallerBase.cs`
  - Fix pipeline order to allow parent completion before child creation.
- `Assets/Scripts/Infra/Addressables/Runtime/Implementation/AddressablesGateway.cs`
  - Remove preload config loading, preload registration build/apply, preload dictionary, and `IPreloadedAssetProvider` implementation.
- `Assets/Scripts/Infra/Addressables/Container/AddressablesInstaller.cs`
  - Register provider contracts and remove obsolete preload handler wiring.
- `Assets/Scripts/App/Bootstrap/Runtime/BootstrapAssetInstaller.cs`
  - Implement preload in `OnCompletedAsync`, store local provider/registrar state, and register in `ConfigureChildBuilder`.
- `Docs/Infra/Addressables.md`
  - Document provider/registrar architecture and gateway load-only responsibility.
- `Docs/Infra/Scope.md`
  - Document new parent lifecycle order and why it is required for child registration data.
- Addressables/Scope/bootstrap tests under:
  - `Assets/Scripts/Infra/Addressables/Tests/`
  - `Assets/Scripts/Infra/Scope/Tests/`
  - bootstrap test location (if present) or new tests in existing assembly.

### Classes/files to remove

- `Assets/Scripts/Infra/Addressables/Runtime/Contracts/IPreloadedAssetProvider.cs`
- `Assets/Scripts/Infra/Addressables/Runtime/Contracts/IAssetPreloadHandler.cs`
- `Assets/Scripts/Infra/Addressables/Runtime/Implementation/AddressablesAssetPreloadHandler.cs`
- Preload config pipeline files if no runtime callers remain:
  - `Assets/Scripts/Infra/Addressables/Runtime/Implementation/AddressablesPreloadConfig.cs`
  - related preload config entry/constants files in same folder

Removal rule: delete only when tests and compile prove no runtime usage remains. If editor tooling still needs part of config assets, keep editor-only pieces and remove runtime dependency from gateway.

## Plan of Work

Milestone 1 establishes behavior safety and fixes pipeline order bug. Add tests that prove parent completion must happen before child build for bootstrap preload registration, then change `LayerInstallerBase` order to `InitializeAsync -> OnCompletedAsync -> BuildChildrenAsync`.

Milestone 2 introduces split contracts (`IAssetProvider`, `IAssetRegistrar`) and generic base for asset retrieval only. Wire one fake/test concrete provider implementing both interfaces.

Milestone 3 migrates bootstrap behavior to local-state orchestration in `BootstrapAssetInstaller`: preload in `OnCompletedAsync`, register in `ConfigureChildBuilder`, and remove dictionary/reflection registration path.

Milestone 4 removes preload responsibilities from gateway and deletes obsolete preload handler/config runtime classes/contracts. Keep gateway focused on load APIs and runtime guards.

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

2. Add milestone 1 tests and rerun:

    powershell -NoProfile -ExecutionPolicy Bypass -File ".\.agents\scripts\run-editmode-tests.ps1" -AssemblyNames "Madbox.Scope.Tests"

3. Implement milestone 1 pipeline fix and rerun scope and addressables tests.

4. Implement Milestone 2 and 3 changes, then rerun:

    powershell -NoProfile -ExecutionPolicy Bypass -File ".\.agents\scripts\run-editmode-tests.ps1" -AssemblyNames "Madbox.Addressables.Tests"

5. Remove obsolete preload classes in Milestone 4, then run:

    powershell -NoProfile -ExecutionPolicy Bypass -File ".\.agents\scripts\check-analyzers.ps1"

6. Run required full gate at each milestone completion:

    .\.agents\scripts\validate-changes.cmd

Expected final state: EditMode tests pass, PlayMode tests pass, analyzer output is `TOTAL:0`, no remaining runtime dependency on old preload-gateway pipeline, and scope lifecycle tests validate the new parent-before-child completion order.

## Validation and Acceptance

Acceptance is successful only when all behavior and simplification criteria below are met.

Behavior criteria:

1. Startup still initializes Addressables and gameplay continues to load required assets.
2. Parent `OnCompletedAsync` runs before child creation and providers preload assets before child registration.
3. Child scope resolves typed assets via `IAssetRegistrar` registrations.
4. Existing gateway loading behavior (`Load/LoadAsync` by references/labels) remains functional.

Simplification criteria (explicit objective):

1. `AddressablesGateway` has no preload config loading and no preload dictionary cache.
2. Gateway no longer implements `IPreloadedAssetProvider`.
3. Reflection-heavy untyped child registration path in bootstrap is removed.
4. `IAssetProvider` interface no longer receives gateway; concrete providers may inject gateway internally.
5. Obsolete preload handler/config runtime pipeline is deleted or isolated away from runtime.
6. Net code volume decreases in Addressables preload path (to be recorded with before/after line counts in retrospective).

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
3. Evidence of fixed pipeline order in `LayerInstallerBase` tests.
4. Test summaries for Addressables, Scope/bootstrap integration, and full gate.

Example evidence format to populate during execution:

    Command: .\.agents\scripts\validate-changes.cmd
    Result: scripts asmdef PASS, compilation PASS, EditMode PASS, PlayMode PASS, analyzers PASS (TOTAL:0)

## Interfaces and Dependencies

Stable boundary to preserve:

- `Madbox.Addressables.Contracts.IAddressablesGateway` remains the module loading facade used by consumers.

New dependency direction:

- Bootstrap depends on `IEnumerable<IAssetProvider>` and uses `IAssetRegistrar` implementations discovered from providers.
- Concrete providers may depend on `IAddressablesGateway`.
- Gateway does not depend on concrete providers.

Architectural constraints to preserve:

- Keep Unity-specific runtime behavior in Infra/App layers.
- Keep explicit asmdef dependencies.
- Keep analyzer compliance with no unapproved pragma suppressions.
- Keep tests for all touched modules and add regression tests for bug fixes.

---

Revision Note (2026-03-21 / Codex): Created initial provider-first ExecPlan focused on reducing lines and complexity by removing preload logic from `AddressablesGateway`, with explicit create/touch/remove class map and implementation snippets.
Revision Note (2026-03-21 / Codex): Updated plan to include `LayerInstallerBase` pipeline bug fix (`InitializeAsync -> OnCompletedAsync -> BuildChildrenAsync`), moved preload orchestration to `BootstrapAssetInstaller` local state, split contracts into `IAssetProvider` and `IAssetRegistrar`, and removed optional coordinator path.
