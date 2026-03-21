# Simplify NavigationProvider Internals with Lean Strategy Chain and Non-Blocking Addressables Group Load

This ExecPlan is a living document. The sections `Progress`, `Surprises & Discoveries`, `Decision Log`, and `Outcomes & Retrospective` must be kept up to date as work proceeds.

This document must be maintained in accordance with `PLANS.md` at the repository root.

## Purpose / Big Picture

After this change, `NavigationProvider` will be easier to read and maintain, with less branching and less mixed responsibility inside one class. Internally, point resolution will run through a small ordered strategy chain (scene/context, buffer, addressables), each with minimal logic.

At the same time, we will correct the Addressables group sync path so `Load<T>(AssetLabelReference)` does not block on label resolution. It will return a group handle quickly and complete child handles in background, matching the expected non-blocking sync behavior.

The goal is strict simplification: minimum code, minimum abstractions, no extra validation layers in internal strategy classes.

## Progress

- [x] (2026-03-21 00:00Z) Authored ExecPlan with lean internal strategy refactor scope, non-blocking group-load correction, and decision analysis for `NavigationPoint` abstraction.
- [ ] Execute Milestone 1: Add/adjust focused regression coverage for non-blocking sync group load behavior.
- [ ] Execute Milestone 2: Introduce internal strategy chain in `NavigationProvider` with minimum interfaces and no additional invariant validation.
- [ ] Execute Milestone 3: Apply Addressables group sync correction and keep behavior parity for existing load/release semantics.
- [ ] Execute Milestone 4: Run validation gate and update documentation minimally.

## Surprises & Discoveries

- Observation: Single-asset sync load is already non-blocking in `AddressablesGateway` (`Load<T>(AssetReference)` returns a pending handle and completes asynchronously).
  Evidence: `Assets/Scripts/Assets/Addressables/Runtime/Implementation/AddressablesGateway.cs` starts `CompleteLoadAsync(...)` fire-and-forget.

- Observation: Group sync load currently blocks on label resolution through `.GetAwaiter().GetResult()`.
  Evidence: `Assets/Scripts/Assets/Addressables/Runtime/Implementation/AddressablesGateway.cs` in `Load<T>(AssetLabelReference)`.

- Observation: `NavigationProvider` currently mixes source selection, async materialization, buffer lifecycle, and context lookup in one class body.
  Evidence: `Assets/Scripts/Infra/Navigation/Runtime/Implementation/NavigationProvider.cs`.

## Decision Log

- Decision: Use internal strategy chain in `NavigationProvider` with a tiny internal interface and ordered list.
  Rationale: Reduces cognitive load in `GetNavigationPoint` without introducing public-facing abstractions.
  Date/Author: 2026-03-21 / Codex

- Decision: Keep strategy-level validation minimal and rely on existing `NavigationController` constructor checks and provider-level guard entry.
  Rationale: User requested minimum complexity and no extra validation overhead in internal-only classes.
  Date/Author: 2026-03-21 / Codex

- Decision: Do not introduce `NavigationPoint` inheritance in this milestone.
  Rationale: Current `NavigationPoint` already supports both ready (scene/buffer) and pending (addressables) flows with minimal state; splitting into abstract/derived types increases file count and branching pressure with low immediate payoff.
  Date/Author: 2026-03-21 / Codex

## Outcomes & Retrospective

Not executed yet. To be updated after implementation and validation.

Expected outcomes at completion:

1. `NavigationProvider` has flatter control flow via small ordered internal strategies.
2. `AddressablesGateway` group sync load no longer blocks on label resolution.
3. Existing navigation behavior remains unchanged in tests.

## Context and Orientation

Primary files in scope:

- `Assets/Scripts/Infra/Navigation/Runtime/Implementation/NavigationProvider.cs`
- `Assets/Scripts/Assets/Addressables/Runtime/Implementation/AddressablesGateway.cs`
- `Assets/Scripts/Infra/Navigation/Tests/NavigationTests.cs`
- `Assets/Scripts/Assets/Addressables/Tests/AddressablesGatewayTests.cs`

Important constraints for this plan:

- Internal-only abstractions (inside navigation implementation namespace/file area).
- No public API expansion for consumer modules.
- Keep changes small and local; avoid touching unrelated modules.
- Avoid adding guard boilerplate in every internal strategy.

## Plan of Work

Milestone 1 locks behavior for the specific bug and target architecture with minimal tests.

1. Add one focused test (or adjust existing one) proving `Load<T>(AssetLabelReference)` returns quickly and child handles complete asynchronously.
2. Keep test changes minimal and scoped to existing Addressables test assembly.

Milestone 2 introduces a lean internal strategy chain in `NavigationProvider`.

1. Add a private/internal interface in navigation implementation scope:

    interface INavigationPointSource
    {
        bool TryCreate(ViewConfig config, IViewController controller, NavigationOptions options, out NavigationPoint point);
    }

2. Implement three internal sources with minimum logic:

    - `ContextNavigationPointSource` (context views dictionary)
    - `BufferNavigationPointSource` (instance buffer)
    - `AddressablesNavigationPointSource` (async materialization path)

3. Build ordered list once in constructor and iterate in `GetNavigationPoint`.
4. Keep existing async addressables materialization behavior, just move it behind the addressables source.

Milestone 3 fixes group sync load in `AddressablesGateway`.

1. Replace blocking label resolution in sync group load.
2. Return group handle immediately with pending child handles that complete in background.
3. Preserve cancellation and release semantics at current behavior level.

Milestone 4 validates and documents.

1. Run targeted EditMode tests for Addressables and Navigation.
2. Run `.agents/scripts/validate-changes.cmd`.
3. Update docs minimally only if flow description becomes outdated.

## Concrete Steps

Run commands from repository root `C:\Unity\Madbox`.

1. Focused test runs while implementing:

    powershell -NoProfile -ExecutionPolicy Bypass -File ".\.agents\scripts\run-editmode-tests.ps1" -AssemblyNames "Madbox.Addressables.Tests"

    powershell -NoProfile -ExecutionPolicy Bypass -File ".\.agents\scripts\run-editmode-tests.ps1" -AssemblyNames "Scaffold.Navigation.Tests"

2. Full gate after implementation:

    .\.agents\scripts\validate-changes.cmd

## Validation and Acceptance

Behavior acceptance:

1. `NavigationProvider` resolves points in this order: context -> buffer -> addressables.
2. Scene/context and buffer paths continue returning ready points immediately.
3. Addressables path continues returning pending point then completing view asynchronously.
4. `AddressablesGateway.Load<T>(AssetLabelReference)` no longer blocks waiting for label resolution.

Simplification acceptance:

1. `NavigationProvider.GetNavigationPoint` contains minimal orchestration logic only.
2. Strategy classes are internal and minimal; no extra defensive validation noise.
3. No new public-facing contracts for feature consumers.

## Idempotence and Recovery

- The strategy-chain refactor is safe to apply incrementally: move one source at a time and rerun tests.
- If behavior changes unexpectedly, revert only the latest extracted source class and keep tests.
- Group sync correction should be isolated to `AddressablesGateway`; if regressions appear, revert that method only and keep characterization test.

## Artifacts and Notes

Evidence to capture during execution:

1. Targeted test pass for Addressables and Navigation assemblies.
2. Validation gate summary with analyzer/test status.
3. Short before/after snippet of `GetNavigationPoint` showing orchestration simplification.

## Interfaces and Dependencies

Internal strategy contract target (non-public):

    internal interface INavigationPointSource
    {
        bool TryCreate(ViewConfig config, IViewController controller, NavigationOptions options, out NavigationPoint point);
    }

Default source order:

1. Context source
2. Buffer source
3. Addressables source

`NavigationPoint` model decision in this plan:

- Keep current single `NavigationPoint` type.
- Do not add abstract `NavigationPoint` hierarchy yet.

Reasoning:

- Current type already supports both ready and pending materialization states with low complexity.
- Introducing inheritance now increases type count and migration churn without removing enough logic.
- If later requirements diverge strongly (different lifecycle/state/event semantics), create `SceneNavigationPoint` and `AddressablesNavigationPoint` in a separate focused milestone.

---

Revision Note (2026-03-21 / Codex): Created initial ExecPlan for lean internal strategy refactor in `NavigationProvider` plus non-blocking `AddressablesGateway` sync-group correction, with explicit minimalism constraints and `NavigationPoint` abstraction decision.
