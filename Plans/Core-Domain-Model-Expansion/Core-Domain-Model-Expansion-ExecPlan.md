# Core Domain Model Expansion for Combat Foundations

This ExecPlan is a living document. The sections `Progress`, `Surprises & Discoveries`, `Decision Log`, and `Outcomes & Retrospective` must be kept up to date as work proceeds.

This document must be maintained in accordance with `PLANS.md` from the repository root.

## Purpose / Big Picture

After this change, the project will have a domain-first combat modeling baseline that stays independent from Unity visuals. A contributor will be able to run pure C# tests and see that player loadout, weapon-driven attack data, movement state, target state, and spawn definitions are represented with explicit intents and events.

The visible proof will be EditMode tests that drive `Game` with intents such as movement start/stop, equip weapon, target selected/cleared, and attack-triggered updates, then assert state and emitted events. This gives us stable contracts before binding to scene objects, collisions, and animation-driven simulation.

## Progress

- [x] (2026-03-19 00:49Z) Created this ExecPlan at `Plans/Core-Domain-Model-Expansion/Core-Domain-Model-Expansion-ExecPlan.md` and aligned scope to domain-only expansion (no visual layer work).
- [x] (2026-03-19 01:08Z) Revised scope after review: removed heavy guard/validation and removed complex interaction logic from pure C# domain scope.
- [x] (2026-03-19 01:09Z) Execute Milestone 1: Added `WeaponId`, `WeaponProfile`, baseline weapon presets, expanded `Player` equipped-weapon state + equip API, and added `PlayerWeaponTests` (3 tests).
- [x] (2026-03-19 01:12Z) Ran required milestone quality gate: `.agents/scripts/validate-changes.cmd` passed (`TOTAL:0`, all EditMode/PlayMode tests green).
- [x] (2026-03-19 01:45Z) Execute Milestone 2: Added minimal movement/target state and attack-data surface to `Player` (`MovementSpeed`, `IsMoving`, `SelectedTargetId`, weapon-derived attack data).
- [x] (2026-03-19 01:45Z) Execute Milestone 3: Added new battle intents/events and command routing for equip/movement/target/auto-attack-data update flow from component simulation.
- [x] (2026-03-19 01:45Z) Execute Milestone 4: Added spawn/archetype definitions and spawn event contracts without world-space or planner logic.
- [x] (2026-03-19 01:45Z) Execute Milestone 5: Added focused contract tests and completed full quality loop with clean gate (`validate-changes.cmd` TOTAL:0).

## Surprises & Discoveries

- Observation: `PlayerAutoAttackBehaviorState` currently owns only cooldown and has no weapon, movement, or target context.
  Evidence: `Assets/Scripts/Core/Battle/Runtime/Behaviors/PlayerAutoAttackBehaviorState.cs` exposes `CanAttack`, `Tick`, and `StartCooldown` only.

- Observation: existing battle events include attack and projectile resolution, but there are no intents/events for loadout, movement, target acquisition, or target clearing.
  Evidence: `Assets/Scripts/Core/Battle/Runtime/Events/BattleEvents.cs` currently defines attack/projectile/hit/death events only.

- Observation: `Player` contains health and behaviors but no combat configuration model.
  Evidence: `Assets/Scripts/Core/Battle/Runtime/Player.cs` has no weapon/loadout properties.

- Observation: `Madbox.Battle.Tests` required an explicit `CommunityToolkit.Mvvm.dll` precompiled reference after adding test calls that mutate `Player` state through the new equip API.
  Evidence: compiler error `CS0012` on `PlayerWeaponTests.cs` was resolved by updating `Assets/Scripts/Core/Battle/Tests/Madbox.Battle.Tests.asmdef`.

- Observation: analyzer `SCA0012` requires a public entry guard on `EquipWeapon` even for lightweight domain behavior.
  Evidence: `validate-changes.cmd` failed until `if (weapon == null) return;` was added in `Player.EquipWeapon`.

- Observation: introducing weapon-derived cooldown changed existing timing assumptions in one legacy battle test.
  Evidence: `GameTests.Trigger_PlayerAutoAttackObserved_WhenProjectileHits_EmitsAttackAndEnemyKilled` failed until cooldown tick was updated from `0.5f` to `0.6f`.

- Observation: `SCA0006` line-length/method-size analyzer limits required splitting router registration into smaller private methods.
  Evidence: `BattleEventRouter.RegisterHandlers` had to be split into `RegisterCoreCombatHandlers`, `RegisterPlayerContractHandlers`, and `RegisterObservedCombatHandlers`.

## Decision Log

- Decision: Place weapon and player-combat state models in `Madbox.Battle` runtime first, instead of creating a new module now.
  Rationale: this keeps the first domain iteration small and lets us validate behavior with existing `Madbox.Battle.Tests`; extraction to a dedicated weapons module can happen after contracts stabilize.
  Date/Author: 2026-03-19 / Codex.

- Decision: Keep movement and targeting in core as minimal state/definition surfaces, not full decision logic.
  Rationale: movement/collision checks are view-side. Core should consume intents/events and keep only the data needed by simulation components.
  Date/Author: 2026-03-19 / Codex.

- Decision: Model spawn in this phase as definitions/contracts only, without randomization algorithms in core.
  Rationale: this phase is a modeling baseline; complex world interaction and spatial logic belong to component simulation.
  Date/Author: 2026-03-19 / Codex.

## Outcomes & Retrospective

This section will be completed as milestones ship. The expected outcome is a domain API that is expressive enough for assignment mechanics before visual implementation begins. Remaining work after this plan should be mostly adapter/presentation integration.

Milestones 1 through 5 are complete. Core now exposes lightweight state and contracts for loadout, movement, target state, attack data updates, and spawn/archetype definitions, while continuing to keep world-space logic out of pure C#. Validation outcomes are clean: compilation pass, full EditMode and PlayMode pass, analyzer `TOTAL:0`.

## Context and Orientation

Current battle domain lives in `Assets/Scripts/Core/Battle/Runtime/` and is already event-driven through `Game`, `BattleEventRouter`, and command classes under `Runtime/Events/`. Enemy definitions and rule definitions are declared in `Assets/Scripts/Core/Levels/Runtime/` and enemy runtime state lives in `Assets/Scripts/Meta/Enemies/Runtime/`.

In this plan, “intent” means an input command that asks the domain to update state (`player started moving`, `equip weapon`). “Event” means a domain output that reports state already updated (`weapon equipped`, `target changed`, `attack data updated`).

Key files that will be edited:

- `Assets/Scripts/Core/Battle/Runtime/Player.cs`
- `Assets/Scripts/Core/Battle/Runtime/Behaviors/PlayerAutoAttackBehaviorState.cs`
- `Assets/Scripts/Core/Battle/Runtime/Events/BattleEvents.cs`
- `Assets/Scripts/Core/Battle/Runtime/Events/BattleEventRouter.cs`
- `Assets/Scripts/Core/Battle/Runtime/Events/` command implementations (new files)
- `Assets/Scripts/Core/Battle/Runtime/Services/` or `Assets/Scripts/Core/Battle/Runtime/Behaviors/` (new definition files)
- `Assets/Scripts/Core/Battle/Tests/GameTests.cs` and additional focused tests in `Assets/Scripts/Core/Battle/Tests/`
- `Docs/Core/Battle.md` (update API and behavior docs after implementation)

## Plan of Work

Milestone 1 introduces a weapon-aware player model. We will add explicit domain types for weapon identity and weapon profile fields needed by assignment mechanics: attack cooldown, attack range, movement speed modifier, and optional attack timing metadata. `Player` will gain loadout/equipped-weapon state with lightweight defaults.

Milestone 2 introduces minimal movement and attack behavior definitions in pure C#. Movement modeling in this phase is intentionally small (`Speed` plus `IsMoving`). Attack modeling in this phase is also small (`Range`, `Cooldown`) with an internal behavior object that reads the equipped weapon and applies those values to runtime attack data.

Milestone 3 extends the battle pipeline with new intents and events. Router mappings and commands will be expanded so the domain can receive weapon equip, movement transitions, and target/attack outcomes generated by component simulation. Complex validation and world-interaction checks remain outside core.

Milestone 4 adds spawn and enemy-archetype behavior definitions and event contracts only. No random spawn planner, collision logic, or spatial resolution algorithm is included in this phase. Core will expose the data and events needed for later component mapping.

Milestone 5 completes regression coverage and quality loops. New tests will verify state modeling and event contracts while preserving existing behavior where still applicable.

## Concrete Steps

Run all commands from repository root `C:\Users\mtgco\.codex\worktrees\01c1\Madbox`.

1. Create and wire weapon domain types in `Madbox.Battle` runtime.

    - Add `WeaponId` and `WeaponProfile` (or equivalent strongly typed value objects) under `Assets/Scripts/Core/Battle/Runtime/`.
    - Extend `Player` to hold equipped weapon and expose a simple equip API.
    - Keep validation lightweight and focused on minimal structural correctness.

2. Add movement/attack behavior definitions and lightweight target state.

    - Add player movement state model (`speed` and `isMoving`).
    - Add attack behavior model (`range` and `cooldown`) and internal behavior that reads equipped weapon to build current attack data.
    - Keep target state as a simple selected target reference populated/cleared by incoming intents/events.

3. Expand intents/events and command routing.

    - Add battle intents such as `EquipPlayerWeaponIntent`, `PlayerMovementStarted`, `PlayerMovementStopped`, `TargetSelected`, `TargetCleared`, and `AutoAttackTriggered`.
    - Add output events such as `PlayerWeaponEquipped`, `PlayerMovementChanged`, `PlayerTargetChanged`, and `PlayerAutoAttackDataUpdated`.
    - Register new commands in `BattleEventRouter` and implement command handlers under `Assets/Scripts/Core/Battle/Runtime/Events/`.

4. Add spawn behavior definitions and contracts.

    - Add spawn definition types under `Assets/Scripts/Core/Levels/Runtime/` or `Assets/Scripts/Core/Battle/Runtime/Services/` depending on final ownership decision.
    - Add spawn-related intents/events that allow component simulation to inform core about spawn outcomes.

5. Add and run tests incrementally.

    - Extend `GameTests.cs` and add focused test files for weapon mapping, movement/attack data definitions, and contract events.
    - For every bug discovered during implementation, add regression tests first and verify fail-before/pass-after.

6. Run validation loop after each milestone.

    powershell -NoProfile -ExecutionPolicy Bypass -File ".\.agents\scripts\run-editmode-tests.ps1" -AssemblyNames "Madbox.Battle.Tests"
    powershell -NoProfile -ExecutionPolicy Bypass -File ".\.agents\scripts\run-editmode-tests.ps1" -AssemblyNames "Madbox.Levels.Tests"
    & ".\.agents\scripts\validate-changes.cmd"

7. Update docs after code lands.

    - Update `Docs/Core/Battle.md` to document new public domain contracts and event flow.
    - If spawn contracts are placed in Levels, update `Docs/Core/Levels.md` as well.

## Validation and Acceptance

Acceptance is met when all of the following are true:

- A `Player` instance has explicit equipped-weapon domain state and can switch weapon profile through domain intents.
- Attack data (`range`, `cooldown`) is derived from equipped weapon profile, not hardcoded constants.
- Movement data (`speed`, `isMoving`) and target selection state are represented in core as simple state definitions.
- Battle pipeline supports the new intents/events and updates state from component simulation outcomes.
- Spawn behavior definitions and spawn-related event contracts exist in core without implementing world-space logic.
- Existing relevant battle behavior remains green unless intentionally replaced, and `.agents/scripts/validate-changes.cmd` passes with analyzer `TOTAL:0`.

A minimal observable transcript expected near completion:

    [3/5] Running EditMode tests
    Total:   <updated>
    Passed:  <same as total>
    Failed:  0

    [5/5] Running analyzer check
    TOTAL:0

## Idempotence and Recovery

All planned edits are additive and can be retried safely. If routing breaks mid-migration, keep old commands intact until new intent/event handlers compile and tests pass, then remove deprecated paths in the same milestone. If a milestone introduces broad breakage, revert only that milestone commit with `git revert <commit>` and rerun targeted tests before reapplying in smaller increments.

When adding new events/intents, preserve simple defaults so test harnesses remain stable during transition.

## Artifacts and Notes

At each milestone completion, capture short evidence snippets in this file:

- New/updated test names proving weapon mapping and movement/attack state updates.
- One sample showing `PlayerAutoAttackDataUpdated` reflects equipped weapon fields.
- One sample showing spawn definition/events are wired without simulation algorithm logic.
- Final `validate-changes.cmd` clean summary.

Milestone 1 evidence:

    run-editmode-tests.ps1 -AssemblyNames "Madbox.Battle.Tests"
    Total:   18
    Passed:  18
    Failed:  0

    validate-changes.cmd
    Tests: PASS
    Analyzers: PASS (TOTAL:0, BLOCKERS:0)

Milestones 2-5 evidence:

    run-editmode-tests.ps1 -AssemblyNames "Madbox.Battle.Tests"
    Total:   26
    Passed:  26
    Failed:  0

    validate-changes.cmd
    [3/5] EditMode: Total 186, Passed 186, Failed 0
    [4/5] PlayMode: Total 2, Passed 2, Failed 0
    [5/5] Analyzers: TOTAL:0

## Interfaces and Dependencies

Required end-state interfaces and types:

- `Madbox.Battle.Player` must expose read-only equipped weapon state and a simple equip method.
- `Madbox.Battle` must include explicit weapon identity/profile value types.
- `Madbox.Battle.Events.BattleEvents` must define intents/events for equipment, movement state transitions, target selection lifecycle, and attack data updates.
- `Madbox.Battle.Events.BattleEventRouter` must map new intents to commands without breaking existing event routing.
- `Madbox.Battle.Behaviors.PlayerAutoAttackBehaviorState` (or a replacement) must use weapon-derived cooldown/range data.
- Targeting in this phase is represented as state + event contracts (no complex selection policy required).
- Spawning in this phase is represented as definitions + event contracts (no planner algorithm required).

Dependency constraints to preserve:

- No UnityEngine or MonoBehaviour references in `Assets/Scripts/Core/Battle/Runtime/` domain logic.
- Keep explicit `.asmdef` boundaries intact and analyzer compliant.
- Do not couple new domain types to presentation or scene objects.

Plan revision note: 2026-03-19 - Initial ExecPlan authored to expand core domain model for weapons, player combat state, baseline targeting/movement/spawn behaviors, and expected intents/events before visual integration.
Plan revision note: 2026-03-19 - Revised after review to keep core scope lightweight: minimal behavior definitions/state, no heavy validation, and no complex interaction logic in pure C#.
Plan revision note: 2026-03-19 - Executed Milestones 2-5, added intent/event command pipeline and spawn definitions/contracts, updated tests/docs, and recorded final clean quality gate evidence.
