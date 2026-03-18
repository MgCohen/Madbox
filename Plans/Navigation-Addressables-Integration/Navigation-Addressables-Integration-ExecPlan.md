# Make Navigation Load Views Through Addressables

This ExecPlan is a living document. The sections `Progress`, `Surprises & Discoveries`, `Decision Log`, and `Outcomes & Retrospective` must be kept up to date as work proceeds.

This document must be maintained in accordance with `PLANS.md` at the repository root.

## Purpose / Big Picture

After this refactor, Navigation will still use Addressables for non-context views, but with clearer ownership and lower coupling: Navigation no longer registers preload requests, startup loads all `ViewConfig` metadata and keeps prefab handles resident, and runtime manages an instance buffer/cache for fast back-and-forth navigation.

This plan keeps `INavigation` API unchanged. Transition orchestration remains the same entry point, but transitions will wait for point readiness when a view is still loading.

## Progress

- [x] (2026-03-18 14:55Z) Authored initial ExecPlan and executed first migration pass (Addressables integration + tests + clean quality gate).
- [x] (2026-03-18 16:07Z) Completed first migration pass with clean `.agents/scripts/validate-changes.cmd`.
- [x] (2026-03-18 16:20Z) Re-scoped plan to cache/residency model based on review feedback: remove navigation preload registration, keep startup-driven config load policy external, add runtime instance cache, keep `INavigation` unchanged.
- [ ] Execute Milestone 1: Remove preload registration logic from Navigation module and keep runtime focused on load/cache/transition behavior only.
- [ ] Execute Milestone 2: Add prefab-handle residency service for non-context `ViewConfig` entries (load once, never release during app lifetime).
- [ ] Execute Milestone 3: Add view instance buffer/cache (instance lifecycle policy independent from prefab handle residency).
- [ ] Execute Milestone 4: Implement low-impact open pipeline where transitions wait for point readiness without adding new public navigation API.
- [ ] Execute Milestone 5: Expand tests/docs and run `.agents/scripts/validate-changes.cmd` until clean.

## Surprises & Discoveries

- Observation: Existing `NavigationTransitions` already runs an async queue (`RunTransitions` + `ProcessNextTransition`), which is a safe seam to introduce readiness waiting without changing `INavigation` signatures.
  Evidence: `Assets/Scripts/Infra/Navigation/Runtime/Implementation/NavigationTransitions.cs`.

- Observation: Current implementation ties successful sync open to preload assumptions, which is brittle when preload policy ownership is moved outside Navigation.
  Evidence: current provider behavior expects completed load path before point creation.

## Decision Log

- Decision: Remove preload registration responsibilities from `NavigationInstaller`.
  Rationale: Preload policy is being centralized in a separate config-driven branch and should not live in controllers/navigation DI wiring.
  Date/Author: 2026-03-18 / Codex + User

- Decision: Treat addressable load result as prefab source, never as a persistent instance.
  Rationale: Prevent lifecycle confusion; loaded asset stays resident, instances remain Navigation-owned runtime objects.
  Date/Author: 2026-03-18 / Codex + User

- Decision: Keep prefab handles resident for app lifetime after startup load.
  Rationale: Remove runtime unload churn for navigation prefabs and guarantee predictable open latency.
  Date/Author: 2026-03-18 / Codex + User

- Decision: Add instance buffer/cache for opened views with explicit release policy.
  Rationale: Improve back-and-forth performance while keeping memory policy explicit and testable.
  Date/Author: 2026-03-18 / Codex + User

- Decision: Do not introduce new public Navigation API.
  Rationale: Transitions already provide orchestration seam; readiness waiting should be internal.
  Date/Author: 2026-03-18 / Codex + User

## Outcomes & Retrospective

First migration pass is complete and validated. This revised plan defines the second pass focused on ownership cleanup and runtime performance semantics.

Expected end state for this pass:

- Navigation has no preload registration logic.
- Startup process (outside navigation controllers) loads required `ViewConfig` prefab handles and keeps them resident.
- Navigation runtime uses a view instance cache for reuse and deterministic release policy.
- Transition flow waits for point readiness when load is pending, without changing `INavigation` interface.

## Context and Orientation

Relevant files for second pass:

- `Assets/Scripts/Infra/Navigation/Container/NavigationInstaller.cs`
- `Assets/Scripts/Infra/Navigation/Runtime/Implementation/NavigationProvider.cs`
- `Assets/Scripts/Infra/Navigation/Runtime/Implementation/NavigationPoint.cs`
- `Assets/Scripts/Infra/Navigation/Runtime/Implementation/NavigationController.cs`
- `Assets/Scripts/Infra/Navigation/Runtime/Implementation/NavigationTransitions.cs`
- `Assets/Scripts/Infra/Navigation/Runtime/Implementation/ViewConfig.cs`
- `Assets/Scripts/Infra/Navigation/Tests/NavigationTests.cs`
- `Docs/Infra/Navigation.md`

Terms used in this pass:

- Prefab handle residency: a loaded addressable prefab handle retained for app lifetime.
- Instance cache: pooled/reusable instantiated view game objects keyed by view config/type.
- Point readiness: whether a `NavigationPoint` has a live `IView` instance bound and ready for transition open/focus.

## Plan of Work

Milestone 1 removes preload logic from Navigation wiring. Delete addressables preload registration from `NavigationInstaller` and any related dependencies (`IAddressablesPreloadRegistry`, preload mode references) in Navigation container assembly. Keep only service registration needed for runtime navigation flow.

Milestone 2 introduces a prefab-handle residency service in Navigation runtime (or adjacent infra adapter) that, given navigation settings/configs loaded at startup, resolves each addressable prefab once via gateway and stores its handle. Residency service never releases these handles during normal app lifetime.

Milestone 3 introduces instance buffer/cache policy for non-context views. Cache stores instantiated view objects separately from prefab handles. Reopen path should prefer cached instance when valid; close path should either return instance to cache or destroy based on policy (for example cache size or explicit view schema flag). Prefab handle residency remains unchanged.

Milestone 4 adds readiness-aware transition flow without new API. `INavigation.Open(...)` keeps current signature. Internally, point creation may involve async work; `NavigationTransitions` should wait for `to` point readiness before running open animation/open sequence. Define and implement explicit order:

1. Resolve target config and options.
2. Request ready point from provider/cache service.
3. Queue transition request immediately.
4. Transition processor awaits point readiness (if pending).
5. Run close/hide for `from`.
6. Run open/focus for `to`.
7. Emit transition finished event.

Milestone 5 updates tests and docs. Add coverage for cache hit, cache miss, close policy behavior, readiness waiting order, and no API change to `INavigation` callers.

## Concrete Steps

Run all commands from repository root `C:\Users\mtgco\.codex\worktrees\ebeb\Madbox`.

1. Implement Milestones 1-4 incrementally.

2. After each milestone run targeted tests:

    powershell -NoProfile -ExecutionPolicy Bypass -File ".\.agents\scripts\run-editmode-tests.ps1" -AssemblyNames "Scaffold.Navigation.Tests"
    powershell -NoProfile -ExecutionPolicy Bypass -File ".\.agents\scripts\run-editmode-tests.ps1" -AssemblyNames "Madbox.Addressables.Tests"

3. End-of-milestone quality gate:

    .\.agents\scripts\validate-changes.cmd

4. If gate fails, fix all diagnostics and rerun until clean.

## Validation and Acceptance

Acceptance is complete when all of the following are true:

- Navigation no longer registers preload entries.
- Prefab handles for navigation configs are loaded once at startup and not released during runtime navigation flow.
- Runtime view instance cache is active and reused on back-and-forth navigation paths.
- Close policy for cached instances is deterministic and tested.
- `INavigation` public API remains unchanged.
- Transition pipeline waits for point readiness before open/focus sequence, with tested order of operations.
- `.agents/scripts/validate-changes.cmd` passes with analyzers clean.

For any bug fix discovered while implementing these milestones, add/update regression test first, verify fail-before and pass-after, then run full gate.

## Idempotence and Recovery

Milestones are additive. If a milestone partially fails, keep new tests and revert only incomplete implementation in that milestone.

Cache/residency changes must remain reversible behind internal adapters so fallback to direct instantiate path is possible during debugging, but only one production path should remain at milestone completion.

## Artifacts and Notes

Expected files to change in this pass:

- `Assets/Scripts/Infra/Navigation/Container/NavigationInstaller.cs`
- `Assets/Scripts/Infra/Navigation/Runtime/Implementation/NavigationProvider.cs`
- `Assets/Scripts/Infra/Navigation/Runtime/Implementation/NavigationPoint.cs`
- `Assets/Scripts/Infra/Navigation/Runtime/Implementation/NavigationController.cs`
- `Assets/Scripts/Infra/Navigation/Runtime/Implementation/NavigationTransitions.cs`
- `Assets/Scripts/Infra/Navigation/Tests/NavigationTests.cs`
- `Docs/Infra/Navigation.md`

Evidence to append while executing:

- cache hit/miss test outputs,
- readiness ordering test outputs,
- final `validate-changes.cmd` summary.

## Interfaces and Dependencies

Requirements for this pass:

- Keep `INavigation` unchanged.
- Keep `ViewConfig.Asset` as addressable prefab source.
- Keep `Scaffold.Navigation` depending on `Madbox.Addressables` for load handle contracts.
- Remove `IAddressablesPreloadRegistry` usage from Navigation container wiring.
- Add only internal runtime interfaces/types for cache/readiness orchestration.

Non-goals:

- Defining global preload policy in Navigation.
- Exposing new public navigation methods.
- Conflating prefab handle residency with view instance cache ownership.

---

Revision Note (2026-03-18 / Codex): Created initial ExecPlan for migrating Navigation to Addressables.
Revision Note (2026-03-18 / Codex): Executed first migration pass with clean quality gate.
Revision Note (2026-03-18 / Codex): Re-scoped to second-pass refactor for preload decoupling, prefab residency, instance cache, and readiness-aware transitions without API changes.
