# Refactor and Thin Addressables Gateway Without Behavior Regressions

This ExecPlan is a living document. The sections `Progress`, `Surprises & Discoveries`, `Decision Log`, and `Outcomes & Retrospective` must be kept up to date as work proceeds.

This document must be maintained in accordance with `PLANS.md` at the repository root.

## Purpose / Big Picture

After this change, Addressables startup and runtime loading behavior will remain the same for game features, but the implementation will be easier to maintain: startup/preload flow, reference counting, and type-dispatch responsibilities will be split into smaller internal services behind the existing `IAddressablesGateway` API. A developer can confirm the refactor is safe by running the existing Addressables tests, adding focused regression tests for edge cases discovered during migration, and passing the repository quality gate.

The user-visible outcome is stability plus easier iteration speed: no API break for callers using `IAddressablesGateway`, no bootstrap behavior change, and clearer internals that can be modified without touching unrelated concerns.

## Progress

- [x] (2026-03-18 00:00Z) Authored initial ExecPlan with architecture context, phased migration, validation steps, and acceptance criteria.
- [x] (2026-03-18 00:24Z) Execute Milestone 1: Established behavior baseline and added characterization tests for initialization idempotence, duplicate normal preload registration, catalog preload handoff, and reference-key loading.
- [x] (2026-03-18 00:33Z) Execute Milestone 2: Added `AddressablesStartupCoordinator` and migrated initialization/preload-request orchestration out of `AddressablesGateway`.
- [x] (2026-03-18 00:39Z) Execute Milestone 3: Added `AddressablesLeaseStore` and `AddressablesPreloadBuffer`, and migrated load/ref-count/release + normal-preload handoff internals out of `AddressablesGateway`.
- [x] (2026-03-18 00:55Z) Execute Milestone 4: Kept thin `AddressablesLayerInitializer` adapter, updated `Docs/Infra/Addressables.md`, and passed full `.agents/scripts/validate-changes.cmd` quality gate with analyzers clean.
- [x] (2026-03-18 00:56Z) Record retrospective and close the ExecPlan with completed evidence and final architecture decisions.

## Surprises & Discoveries

- Observation: The current gateway complexity is partly intentional because repository analyzers enforce small methods (`SCA0006` with max 8 lines), which encourages many helper methods and perceived repetition.
  Evidence: `.editorconfig` sets `scaffold.SCA0006.max_lines = 8` and `dotnet_diagnostic.SCA0006.severity = warning`.

- Observation: Current `AddressablesLayerInitializer` is a thin adapter, but it acts as an explicit startup seam in DI composition.
  Evidence: `Assets/Scripts/Infra/Addressables/Container/AddressablesInstaller.cs` registers `IAsyncLayerInitializable` to `AddressablesLayerInitializer`.

- Observation: Analyzer/doc text for `SCA0026` references `Madbox.Initialization.Contracts.*`, while runtime contracts currently live under `Madbox.Scope.Contracts.*`; this may weaken intended initialization-call-chain enforcement.
  Evidence: `Analyzers/Scaffold/Scaffold.Analyzers/InitializationSameLayerUsageAnalyzer.cs` metadata names differ from `Assets/Scripts/Infra/Scope/Runtime/Contracts/*.cs` namespaces.

- Observation: Full validation initially failed due to test-file analyzer style constraints (`SCA0002`, `SCA0006`) after adding characterization tests; fixing helper ordering and splitting a long test method restored clean gate.
  Evidence: First `.agents/scripts/validate-changes.cmd` run reported `TOTAL:2` in `AddressablesGatewayTests.cs`; final run reported `TOTAL:0`.

## Decision Log

- Decision: Keep `IAddressablesGateway` public contract unchanged during this refactor.
  Rationale: This preserves all existing feature callers and minimizes migration risk while we improve internals.
  Date/Author: 2026-03-18 / Codex

- Decision: Refactor in additive phases (introduce new internal collaborators first, then move logic, then remove dead code).
  Rationale: Additive migration makes failures easier to isolate and keeps tests meaningful at each step.
  Date/Author: 2026-03-18 / Codex

- Decision: Treat initializer merge as a late-stage decision after behavior is locked by tests.
  Rationale: The adapter is tiny; removing it early adds DI/rule risk without reducing core complexity.
  Date/Author: 2026-03-18 / Codex

- Decision: Keep `AddressablesLayerInitializer` as the startup adapter in this refactor.
  Rationale: It keeps startup wiring explicit and avoids potential dual-interface/single-instance registration ambiguity; complexity reduction target was achieved inside gateway internals without changing bootstrap seams.
  Date/Author: 2026-03-18 / Codex

## Outcomes & Retrospective

The refactor delivered a thinner `AddressablesGateway` orchestrator while preserving public API and startup behavior. Startup orchestration moved into `AddressablesStartupCoordinator`, runtime lease/ref-count ownership moved into `AddressablesLeaseStore`, and normal-preload handoff buffering moved into `AddressablesPreloadBuffer`.

Behavior parity was demonstrated by expanded Addressables characterization tests and a clean full validation gate (`validate-changes.cmd`: scripts asmdef PASS, compilation PASS, EditMode tests PASS, PlayMode tests PASS, analyzers PASS/TOTAL:0). The initializer adapter was intentionally retained as an explicit bootstrap seam.

## Context and Orientation

Addressables code for this task is in `Assets/Scripts/Infra/Addressables/`:

- Runtime contracts for consumers: `Assets/Scripts/Infra/Addressables/Runtime/Contracts/`.
- Runtime implementation: `Assets/Scripts/Infra/Addressables/Runtime/Implementation/`.
- DI wiring: `Assets/Scripts/Infra/Addressables/Container/AddressablesInstaller.cs`.
- Tests: `Assets/Scripts/Infra/Addressables/Tests/AddressablesGatewayTests.cs`.

Scope/bootstrap initialization contract is in `Assets/Scripts/Infra/Scope/Runtime/Contracts/IAsyncLayerInitializable.cs`, and scope runner behavior is in `Assets/Scripts/Infra/Scope/Runtime/ScopeInitializer.cs`.

For this plan, “gateway orchestrator” means `AddressablesGateway` remains the single public facade that coordinates internal services but no longer owns every algorithm directly. “Lease/ref-count service” means internal code that tracks loaded assets and decrements/releases only when final ownership is gone. “Preload buffer” means internal storage for `PreloadMode.Normal` handoff behavior.

## Plan of Work

Milestone 1 secures a behavior baseline before structural changes. Expand `AddressablesGatewayTests` with characterization coverage for edge cases that are currently implicit: duplicate preload registrations, catalog preload behavior, type mismatch guards, and initialization idempotence under repeated calls. For any discovered bug during this milestone, add a regression test that fails before a fix and passes after the fix. Keep tests in `Assets/Scripts/Infra/Addressables/Tests/AddressablesGatewayTests.cs` unless file size forces a split.

Milestone 2 extracts startup responsibilities from `AddressablesGateway`. Create internal collaborators in `Assets/Scripts/Infra/Addressables/Runtime/Implementation/` such as `AddressablesStartupCoordinator` and `AddressablesPreloadApplier` (exact names can be adjusted, but responsibilities must remain singular). Move `SyncCatalogAndContent`, preload snapshot iteration, catalog preload expansion, and preload-by-type dispatch into those collaborators. `AddressablesGateway.InitializeAsync` should delegate orchestration and keep only lifecycle guarding.

Milestone 3 extracts runtime loading ownership logic. Introduce an internal component (for example `AddressablesLeaseStore`) that owns `loaded` state, ref-count increment/decrement, and token-based release transitions. Introduce an internal preload owner/buffer component that owns `normalPreloaded` storage and handoff semantics. `AddressablesGateway.LoadAsync` methods become thin orchestration over these services and `IAddressablesAssetClient`.

Milestone 4 finalizes startup adapter strategy and documentation. Evaluate two options and choose one based on clarity and safety proven by tests:

- Keep `AddressablesLayerInitializer` as a thin adapter and update docs to explicitly call out why it exists.
- Replace adapter with direct registration of the gateway as both `IAddressablesGateway` and `IAsyncLayerInitializable` only if DI can guarantee a single shared scoped instance and analyzer rules remain clean.

Complete module documentation updates in `Docs/Infra/Addressables.md` with the new internal architecture and rationale.

## Concrete Steps

Run all commands from repository root `C:\Unity\Madbox`.

1. Baseline and focused tests.

    powershell -NoProfile -ExecutionPolicy Bypass -File ".\.agents\scripts\run-editmode-tests.ps1" -AssemblyNames "Madbox.Addressables.Tests"

   Expected: Addressables tests pass before refactor changes begin.

2. Implement Milestone 1 tests and run same command after each test change.

3. Implement Milestone 2 extraction, then run:

    powershell -NoProfile -ExecutionPolicy Bypass -File ".\.agents\scripts\run-editmode-tests.ps1" -AssemblyNames "Madbox.Addressables.Tests"

   Expected: No regressions in existing tests; newly added tests pass.

4. Implement Milestone 3 extraction, then rerun the Addressables test command.

5. Implement Milestone 4 wiring and docs update.

6. Run full milestone quality gate:

    .\.agents\scripts\validate-changes.cmd

   If the gate fails, fix reported failures/diagnostics and rerun the same command until clean.

## Validation and Acceptance

Acceptance is behavior-based and must be demonstrated by tests and startup integration behavior:

- Existing public API remains unchanged: `IAddressablesGateway` signatures in `Runtime/Contracts` are unchanged.
- Addressables load semantics are preserved:
  - two loads of same key share underlying asset load and release only after final handle release;
  - `PreloadMode.Normal` gives first consumer ownership handoff;
  - `PreloadMode.NeverDie` retains gateway-owned residency.
- Initialization semantics are preserved:
  - startup sync failure logs warning and continues preload flow;
  - repeated initialize calls remain idempotent and safe.
- Scope startup integration remains active through either retained adapter or verified direct dual-interface registration with single instance behavior.
- Required validation commands pass:

    powershell -NoProfile -ExecutionPolicy Bypass -File ".\.agents\scripts\run-editmode-tests.ps1" -AssemblyNames "Madbox.Addressables.Tests"
    .\.agents\scripts\validate-changes.cmd

For any bug fix introduced while refactoring, add/update regression test first, verify it fails before fix, then passes after fix, then run full gate.

## Idempotence and Recovery

This plan is additive-first. Each milestone can be rerun safely because new internal services are introduced behind unchanged public contracts. If a milestone partially fails, revert only that milestone’s incomplete changes and keep characterization tests; do not remove baseline tests once added.

If DI wiring changes cause runtime initialization issues, temporarily keep both code paths (adapter and direct registration) only for local verification, then settle on a single production path before finalizing. Ensure no duplicate initialization invocations by test.

## Artifacts and Notes

During execution, keep short evidence snippets in this section (command outputs and test result summaries) that prove behavior parity. Keep snippets concise and directly tied to acceptance criteria.

Milestone 1 evidence:

    Command: powershell -NoProfile -ExecutionPolicy Bypass -File ".\.agents\scripts\run-editmode-tests.ps1" -AssemblyNames "Madbox.Addressables.Tests"
    Result: Total 11, Passed 11, Failed 0, Skipped 0

Milestone 2/3 evidence:

    Command: powershell -NoProfile -ExecutionPolicy Bypass -File ".\.agents\scripts\run-editmode-tests.ps1" -AssemblyNames "Madbox.Addressables.Tests"
    Result: Total 11, Passed 11, Failed 0, Skipped 0 (after startup extraction and after lease/preload extraction)

Final milestone gate evidence:

    Command: .\.agents\scripts\validate-changes.cmd
    Result: scripts asmdef PASS, compilation PASS, EditMode PASS (109/109), PlayMode PASS (1/1), analyzers PASS (TOTAL:0)

Planned artifacts to update during execution:

- `Assets/Scripts/Infra/Addressables/Runtime/Implementation/AddressablesGateway.cs`
- New internal service files under `Assets/Scripts/Infra/Addressables/Runtime/Implementation/`
- `Assets/Scripts/Infra/Addressables/Container/AddressablesInstaller.cs` (only if wiring changes)
- `Assets/Scripts/Infra/Addressables/Tests/AddressablesGatewayTests.cs`
- `Docs/Infra/Addressables.md`

## Interfaces and Dependencies

The following interfaces and modules must remain the stable boundary for consumers:

- `Madbox.Addressables.Contracts.IAddressablesGateway`
- `Madbox.Addressables.Contracts.IAddressablesPreloadRegistry`
- `Madbox.Addressables.Contracts.IAddressablesAssetClient`

Internal collaborators introduced by this plan must remain `internal` and implementation-scoped within `Madbox.Addressables` runtime implementation folder.

Dependency constraints to preserve:

- No MonoBehaviour introduction in core/infra service logic.
- Keep dependencies explicit through existing `.asmdef` boundaries.
- Keep startup integration with `Madbox.Scope.Contracts.IAsyncLayerInitializable` working.
- Maintain analyzer compliance (`SCA*`) and full quality gate clean state.

Potential follow-up (outside this plan unless explicitly pulled in): align `SCA0026` analyzer metadata names with current `Madbox.Scope.Contracts` namespace so intended rule coverage is active again.

---

Revision Note (2026-03-18 / Codex): Created initial ExecPlan from audit findings to guide safe, incremental Addressables gateway thinning with behavior-locking tests and milestone quality gates.
Revision Note (2026-03-18 / Codex): Updated plan after full execution to record completed milestones, final decisions, validation evidence, and retrospective outcomes.
