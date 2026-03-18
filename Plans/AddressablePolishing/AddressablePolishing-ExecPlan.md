# Addressables Polishing Pass with Final Bootstrap PlayMode E2E

This ExecPlan is a living document. The sections `Progress`, `Surprises & Discoveries`, `Decision Log`, and `Outcomes & Retrospective` must be kept up to date as work proceeds.

This document must be maintained in accordance with `PLANS.md` at the repository root.

## Purpose / Big Picture

After this change, the Addressables module will be easier to maintain and safer to use because redundant and ambiguous internal paths will be removed, preload semantics will be more direct, and we will have final PlayMode end-to-end coverage through the real bootstrap path. A developer will be able to trust both EditMode lifecycle tests and one real bootstrap-driven PlayMode test that proves initialization, real loading, and release behavior together.

This plan intentionally excludes concurrency hardening (for example parallel `InitializeAsync` protection). Concurrency work will be handled in a separate follow-up plan.

## Progress

- [x] (2026-03-18 09:05Z) Authored merged ExecPlan by combining simplification scope with bootstrap-driven PlayMode E2E scope.
- [ ] Execute Milestone 1: Add regression and characterization tests for simplification targets before code changes.
- [ ] Execute Milestone 2: Simplify preload registration semantics and remove ambiguous untyped pathways.
- [ ] Execute Milestone 3: Remove dead/redundant internal paths and simplify dispatch/guard flows without behavior regression.
- [ ] Execute Milestone 4: Update Addressables module docs to match the polished architecture and usage guidance.
- [ ] Execute Milestone 5: Add bootstrap-driven Addressables PlayMode E2E test as the final validation milestone.
- [ ] Run `.agents/scripts/validate-changes.cmd` at the end of each milestone loop until clean, then finalize.

## Surprises & Discoveries

- Observation: Untyped preload registration overloads currently default to `UnityEngine.Object`, which can diverge from typed consumer load-token behavior.
  Evidence: `Assets/Scripts/Infra/Addressables/Runtime/Implementation/AddressablesPreloadRegistry.cs` non-generic `Register(...)` methods call `Register<UnityEngine.Object>(...)`.

- Observation: The module currently includes an empty implementation artifact.
  Evidence: `Assets/Scripts/Infra/Addressables/Runtime/Implementation/AddressablesPreloadBuffer.cs`.

- Observation: Addressables currently has no module-owned PlayMode assembly; existing tests are Editor-scoped.
  Evidence: `Assets/Scripts/Infra/Addressables/Tests/Madbox.Addressables.Tests.asmdef` includes `"includePlatforms": ["Editor"]`.

- Observation: Bootstrap PlayMode smoke tests already demonstrate stable patterns for scene load, startup completion waiting, and fatal-log capture.
  Evidence: `Assets/Scripts/App/Bootstrap/Tests/PlayMode/BootstrapScenePlayModeTests.cs`.

## Decision Log

- Decision: Merge simplification and PlayMode E2E scopes into one plan, with E2E as the final milestone.
  Rationale: This creates one linear implementation and validation story, ending with full runtime proof after internal polish.
  Date/Author: 2026-03-18 / Codex

- Decision: Keep concurrency changes out of scope.
  Rationale: User explicitly requested to skip concurrency issues for now.
  Date/Author: 2026-03-18 / Codex

- Decision: Preserve consumer-facing gateway usage while simplifying internals and preload registration semantics.
  Rationale: Refactor value should come from reduced internal complexity without forcing broad consumer migration.
  Date/Author: 2026-03-18 / Codex

- Decision: Require tests-first for bug-prone behavior changes.
  Rationale: Preload/release semantics are sensitive and must be protected by regression coverage before refactor edits.
  Date/Author: 2026-03-18 / Codex

## Outcomes & Retrospective

Merged plan authored; implementation not started in this revision. Expected final state is a direct, less redundant Addressables module plus a bootstrap-driven PlayMode end-to-end test proving real runtime integration.

## Context and Orientation

The Addressables module lives under `Assets/Scripts/Infra/Addressables/` and is split into runtime contracts, runtime implementation, container wiring, and tests.

- Contracts: `Assets/Scripts/Infra/Addressables/Runtime/Contracts/`
- Implementation: `Assets/Scripts/Infra/Addressables/Runtime/Implementation/`
- Container wiring: `Assets/Scripts/Infra/Addressables/Container/AddressablesInstaller.cs`
- Current module tests: `Assets/Scripts/Infra/Addressables/Tests/AddressablesGatewayTests.cs`

Bootstrap integration path:

- Infra installation: `Assets/Scripts/App/Bootstrap/Runtime/BootstrapInfraInstaller.cs`
- Bootstrap scene runtime flow: `Assets/Scenes/Bootstrap.unity`
- Existing bootstrap PlayMode test reference pattern: `Assets/Scripts/App/Bootstrap/Tests/PlayMode/BootstrapScenePlayModeTests.cs`

In this plan, “polishing” means removing non-essential indirection and duplicate pathways while preserving user-observable load/release behavior. “End-to-end PlayMode test” means running a Unity `[UnityTest]` that uses the actual `Bootstrap` scene and production DI/runtime wiring rather than fake clients.

## Plan of Work

Milestone 1 locks current behavior with tests. Expand `Assets/Scripts/Infra/Addressables/Tests/AddressablesGatewayTests.cs` so simplification targets are protected. If a bug is fixed in this milestone, first add/update a regression test that fails before the fix and passes after.

Milestone 2 simplifies preload registration semantics in `IAddressablesPreloadRegistry` and `AddressablesPreloadRegistry`. Remove or deprecate ambiguous untyped registration paths when they do not preserve typed preload ownership expectations. Keep registration explicit and aligned to runtime token usage.

Milestone 3 simplifies internals without changing concurrency behavior. Remove dead/leftover implementation artifacts (such as an empty preload buffer class if still unused) and replace unnecessary dispatch indirection in `AddressablesLeaseStore` with a clearer direct approach that preserves behavior and analyzer compliance.

Milestone 4 updates `Docs/Infra/Addressables.md` so architecture, preload guidance, and usage examples match the simplified implementation.

Milestone 5 adds module-owned bootstrap PlayMode E2E coverage:

1. Create `Assets/Scripts/Infra/Addressables/Tests/PlayMode/Madbox.Addressables.PlayModeTests.asmdef`.
2. Create `Assets/Scripts/Infra/Addressables/Tests/PlayMode/AddressablesBootstrapPlayModeTests.cs`.
3. In the test, load `Bootstrap` scene, wait for bootstrap completion, resolve `IAddressablesGateway` through runtime wiring, load one real addressable, assert handle lifecycle and double-release safety, and assert no fatal logs.

## Concrete Steps

Run all commands from `C:\Unity\Madbox`.

1. Baseline Addressables EditMode tests:

    powershell -NoProfile -ExecutionPolicy Bypass -File ".\.agents\scripts\run-editmode-tests.ps1" -AssemblyNames "Madbox.Addressables.Tests"

2. Implement Milestones 1-4 with repeated verification:

    powershell -NoProfile -ExecutionPolicy Bypass -File ".\.agents\scripts\run-editmode-tests.ps1" -AssemblyNames "Madbox.Addressables.Tests"

3. Implement Milestone 5 and run targeted PlayMode assembly:

    powershell -NoProfile -ExecutionPolicy Bypass -File ".\.agents\scripts\run-playmode-tests.ps1" -AssemblyNames "Madbox.Addressables.PlayModeTests"

4. End of each milestone loop:

    .\.agents\scripts\validate-changes.cmd

5. If gate fails, fix all reported failures and rerun `.agents/scripts/validate-changes.cmd` until clean.

## Validation and Acceptance

Acceptance is met when all of the following are true:

1. EditMode Addressables tests pass, including new/updated regression coverage for simplification changes.
2. Simplified preload registration semantics no longer rely on ambiguous untyped behavior that can diverge from typed runtime ownership expectations.
3. Dead or redundant implementation paths identified in this plan are removed or replaced with clearer direct implementations.
4. `Docs/Infra/Addressables.md` matches the polished architecture and expected usage.
5. New Addressables module PlayMode assembly passes bootstrap-driven E2E test(s).
6. E2E test verifies no fatal logs (`Assert`, `Error`, `Exception`) through bootstrap + load + release flow.
7. `.agents/scripts/validate-changes.cmd` is clean.

Required behavior that must still be observed:

- Same-key repeated loads share lifecycle and release only on final owner release.
- `PreloadMode.Normal` and `PreloadMode.NeverDie` remain correct for typed registration paths.
- `IAssetHandle.Release()` remains idempotent (double release safe).

## Idempotence and Recovery

The plan is additive-first and safe to rerun. If a simplification change regresses behavior, revert only that sub-change, keep new tests, and re-run the same regression path until green. If the PlayMode test becomes flaky due to scene timing, increase bounded waits conservatively and keep explicit failure messages for diagnosis.

Do not introduce concurrency fixes while executing this plan; keep scope strict for safe rollback and clear acceptance.

## Artifacts and Notes

Expected files touched:

- `Assets/Scripts/Infra/Addressables/Runtime/Contracts/IAddressablesPreloadRegistry.cs`
- `Assets/Scripts/Infra/Addressables/Runtime/Implementation/AddressablesPreloadRegistry.cs`
- `Assets/Scripts/Infra/Addressables/Runtime/Implementation/AddressablesLeaseStore.cs`
- `Assets/Scripts/Infra/Addressables/Runtime/Implementation/AddressablesPreloadBuffer.cs` (remove or replace if still unused)
- `Assets/Scripts/Infra/Addressables/Tests/AddressablesGatewayTests.cs`
- `Assets/Scripts/Infra/Addressables/Tests/PlayMode/Madbox.Addressables.PlayModeTests.asmdef`
- `Assets/Scripts/Infra/Addressables/Tests/PlayMode/AddressablesBootstrapPlayModeTests.cs`
- `Docs/Infra/Addressables.md`

Evidence to append during execution:

- Addressables EditMode run summaries per milestone.
- Fail-before / pass-after evidence for any bug-fix regression test.
- Targeted PlayMode summary for `Madbox.Addressables.PlayModeTests`.
- Final `validate-changes.cmd` clean summary.

## Interfaces and Dependencies

Stable interfaces involved:

- `Madbox.Addressables.Contracts.IAddressablesGateway`
- `Madbox.Addressables.Contracts.IAddressablesPreloadRegistry`
- `Madbox.Addressables.Contracts.IAddressablesAssetClient`
- `Madbox.Addressables.Contracts.IAssetHandle` and `IAssetHandle<T>`
- `Madbox.Scope.Contracts.IAsyncLayerInitializable` startup path integration

Dependency rules to preserve:

- Keep explicit `.asmdef` boundaries.
- Do not couple Addressables runtime contracts to presentation/UI.
- Keep Addressables infra free of feature-module dependencies.
- Keep analyzer compliance clean (`SCA*`).

Non-goals:

- Concurrency hardening.
- Unrelated architecture redesign outside Addressables polishing scope.

---

Revision Note (2026-03-18 / Codex): Created merged plan by combining `AddressablesDirectnessPass` and `AddressablesPlayModeE2ETest`, with PlayMode E2E explicitly positioned as the final milestone and concurrency fixes excluded per user request.
