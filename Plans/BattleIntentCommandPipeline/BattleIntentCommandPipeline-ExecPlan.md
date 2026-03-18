# Battle Intent-Command-Event Pipeline Migration

This ExecPlan is a living document. The sections `Progress`, `Surprises & Discoveries`, `Decision Log`, and `Outcomes & Retrospective` must be kept up to date as work proceeds.

This document must be maintained in accordance with `PLANS.md` at the repository root.

## Purpose / Big Picture

After this change, the battle runtime will process gameplay through a clear pipeline: intents (requests) are converted into commands (domain actions), commands mutate rich domain models (`Player`, `Enemy`), and domain events are emitted as facts. This makes `Game` and `EnemyService` smaller, makes action flows easier to test, and removes event-type branching from service classes.

A developer will be able to run battle tests and observe the same gameplay outcomes as today (win/lose rule completion, combat outcomes), while the architecture is cleaner: `Intent -> Command -> DomainEvent` with a single intent ingress and a single event egress.

## Progress

- [x] (2026-03-18 13:25Z) Authored initial ExecPlan with phased migration strategy, explicit file targets, and validation commands.
- [x] (2026-03-18 13:48Z) Updated flow model after review: commands no longer return result objects, attack/cooldown behavior moved to behavior list ticking, and player/enemy attack flow definitions rewritten.
- [x] (2026-03-18 14:02Z) Updated enemy-attack ownership to presentation-driven confirmation model.
- [x] (2026-03-18 14:15Z) Revised attack integration to direct behavior binding in prefab/view (no runtime attack-check request event) and added player auto-attack flow.
- [x] (2026-03-18 15:10Z) Implemented Milestone 1/2/3/4 in runtime code: command-based routing, player attack migration, enemy-hit migration, projectile-based player auto-attack, and service cleanup.
- [x] (2026-03-18 15:12Z) Added/updated Battle tests for projectile flow and preserved simulation coverage for existing win/lose rules.
- [x] (2026-03-18 15:14Z) Analyzer diagnostics clean (`TOTAL:0`) and EditMode tests passing (`130/130`).
- [x] (2026-03-18 15:33Z) Ran `.agents/scripts/validate-changes.cmd` with clean results: compilation PASS, EditMode PASS, PlayMode PASS, analyzers `TOTAL:0`.
- [x] (2026-03-18 15:15Z) Updated `Outcomes & Retrospective` with delivered behavior and current environment blocker.

## Surprises & Discoveries

- Observation: Existing battle tests already encode end-to-end simulation outcomes for timer and enemy-elimination rule combinations.
  Evidence: `Assets/Scripts/Core/Battle/Tests/GameTests.cs` contains simulation tests for timer-only, enemy-only, and combined rule sets.

- Observation: `Game` is already relatively lean, but `EnemyService` still contains command-like event handling and validation edge logic that should move to ingress/channel boundaries.
  Evidence: `Assets/Scripts/Core/Battle/Runtime/EnemyService.cs` includes `TryHandleTryPlayerAttack` and `TryHandleEnemyHitObserved` methods.

- Observation: Repository analyzers enforce strict placement/order constraints, so interface/type layering had to stay in analyzer-compliant runtime shape rather than introducing extra public message types outside expected boundaries.
  Evidence: intermediate diagnostics during implementation (`SCA0015`, `SCA0025`) required consolidation back to single runtime event surface with command routing kept internal.

## Decision Log

- Decision: Keep three semantic message categories (`Intent`, `Command`, `DomainEvent`) but avoid inheritance where intent/command are treated as events.
  Rationale: Intents are requests, commands are executable actions, and events are immutable facts; conflating them causes loop and meaning ambiguity.
  Date/Author: 2026-03-18 / Codex

- Decision: Introduce a small common marker (`IBattleMessage`) only for channel transport concerns, not domain semantics.
  Rationale: Supports unified plumbing without sacrificing type meaning.
  Date/Author: 2026-03-18 / Codex

- Decision: Migrate incrementally by gameplay flow (attack first, then enemy hit) with parity tests at each step.
  Rationale: Reduces risk and preserves determinism while architecture changes.
  Date/Author: 2026-03-18 / Codex

- Decision: Commands publish domain events during execution through the execution context; commands do not return result containers.
  Rationale: Keeps command contract simple and aligns with the requirement that events are the only byproduct channel.
  Date/Author: 2026-03-18 / Codex

- Decision: Enemy runtime behavior processing is behavior-list driven; cooldown ticking is delegated to behaviors, and attack triggering is an explicit automatic intent.
  Rationale: Removes hardcoded cooldown logic from service and aligns behavior ownership with rich domain models.
  Date/Author: 2026-03-18 / Codex

- Decision: Enemy attack resolution remains presentation-driven and uses direct behavior binding on prefab/view components. Runtime does not emit attack-check request events.
  Rationale: Runtime data layer cannot evaluate line-of-sight or physical blockers reliably; view/physics layer reads behavior readiness and confirms whether attack can connect.
  Date/Author: 2026-03-18 / Codex

- Decision: Player auto-attack while idle follows the same presentation-driven pattern as enemy attack checks.
  Rationale: Keeps spatial checks and movement-state awareness in view while preserving authoritative state mutation in runtime commands.
  Date/Author: 2026-03-18 / Codex

- Decision: Player auto-attack resolves via projectile lifecycle, not direct instant damage.
  Rationale: Projectile travel and collision are presentation simulation concerns; runtime applies authoritative damage only after hit-observed intent.
  Date/Author: 2026-03-18 / Codex

## Outcomes & Retrospective

Implemented outcomes in this pass:

- `BattleEventRouter` now dispatches through command factories per intent/event type.
- `EnemyService` no longer handles event routing methods; it now focuses on enemy state mutation/query and hit resolution helpers.
- Projectile lifecycle is implemented for player idle auto-attack:
  - `PlayerAutoAttackObserved` -> `PlayerProjectileSpawned`
  - `PlayerProjectileHitObserved` -> authoritative damage -> `PlayerAttack`/`EnemyKilled`.
- `Player` and `EnemyRuntimeState` hold richer mutation methods for cooldown/damage state updates.
- Docs updated in `Docs/Core/Battle.md`.

Validation evidence:

- Analyzer check: `TOTAL:0`.
- EditMode tests: `Total 130 / Passed 130 / Failed 0`.
- Added projectile-path regression tests in `Assets/Scripts/Core/Battle/Tests/GameTests.cs`.

Remaining gap:

- Deferred by explicit decision:
  - Command-to-behavior coupling: some commands still resolve concrete runtime behavior classes directly (for example, contact attack behavior lookup) instead of consuming a behavior capability contract. This is accepted for now and scheduled for future cleanup.
  - Runtime behavior factory inheritance support: behavior runtime mapping currently resolves by exact registered definition type. This is accepted for now; polymorphic/fallback resolution will be addressed in a future increment if derived behavior definitions are introduced.

## Context and Orientation

The current battle module is in `Assets/Scripts/Core/Battle/Runtime/` with tests in `Assets/Scripts/Core/Battle/Tests/`.

Key current files:

- `Assets/Scripts/Core/Battle/Runtime/Game.cs`: battle orchestrator with `Start`, `Tick`, and `Trigger(BattleEvent)`.
- `Assets/Scripts/Core/Battle/Runtime/BattleEventRouter.cs`: routes event types to enemy-service handlers.
- `Assets/Scripts/Core/Battle/Runtime/EnemyService.cs`: manages enemy runtime data and currently performs event-specific handling.
- `Assets/Scripts/Core/Battle/Runtime/Player.cs`: player domain model.
- `Assets/Scripts/Core/Battle/Runtime/BattleEvents.cs`: current event/input-output DTOs.
- `Assets/Scripts/Core/Battle/Runtime/GameRules.cs` and rule files: game end evaluation.

Terms used in this plan:

- Intent: an external gameplay request (for example, player attack input or observed contact hit) that does not mutate state by itself.
- Command: an executable domain action with all data resolved and logic attached.
- Domain event: an immutable fact emitted after state changes (for example, enemy killed, player damaged).
- Channel: in-process queue/dispatcher that receives intents, executes mapped commands, and publishes resulting domain events.

Constraints from repository rules:

- Core logic remains Unity-agnostic (no `MonoBehaviour` in this module).
- Analyzer diagnostics must remain clean.
- Changes require tests and documentation updates.

## Plan of Work

Milestone 1 establishes scaffolding only. Add new message interfaces and execution channel classes while keeping the existing `BattleEventRouter` path active. This milestone must be no-op for gameplay behavior and only introduces plumbing and tests for deterministic dispatch order.

Milestone 2 migrates player attack flow. A player attack intent is validated at ingress/factory level and mapped to a concrete resolve-attack command. Command execution calls rich domain model behavior (enemy/player state) and publishes domain events through the execution context. `Game` routes intents into the channel and forwards published events to `EventTriggered`.

Milestone 3 migrates enemy hit flow with parity for damage, cooldown, and player death semantics. The same channel/command flow will handle `EnemyHitObserved` intent. Existing simulation tests must still pass.

Milestone 4 removes obsolete branching and dead paths from `BattleEventRouter`/`EnemyService`, updates docs, and runs the full quality gate. Any temporary adapters used in earlier milestones are removed in this stage.

### File-Level Edit Plan

1. Add message abstractions and channel in `Assets/Scripts/Core/Battle/Runtime/`:

- `BattleMessages.cs` (or equivalent): `IBattleMessage`, `IBattleIntent`, `IBattleCommand`, `IBattleDomainEvent`.
- `IntentCommandChannel.cs`: receives intents, resolves command factories, executes commands, emits domain events.
- `BattleExecutionContext.cs`: immutable references used by commands (`Player`, enemy state access, emit helper if required).

2. Add command implementations:

- `Commands/ResolvePlayerAttackCommand.cs`
- `Commands/ResolvePlayerProjectileHitCommand.cs`
- `Commands/ResolveEnemyHitObservedCommand.cs`

Each command must contain command-local logic and call rich entity methods rather than procedural mutation helpers.

3. Refactor domain models and service boundaries:

- `EnemyRuntimeState.cs`: add rich methods such as `ApplyDamage(int damage)` and `TickBehaviors(float deltaTime)`. Cooldown ticking must happen through behavior objects, not service-level hardcoding. View-side mapped behavior components read readiness and initiate hit-confirmed intents.
- `Player.cs` and/or player runtime behavior state: add idle/auto-attack behavior state needed so view can read when player auto-attack is allowed while not moving.
- `Player.cs`: keep/extend rich methods as needed (`ApplyDamage` already exists).
- `EnemyService.cs`: keep lookup/lifecycle/tick responsibilities; remove event-specific handler methods.

4. Update orchestrator and routing:

- `Game.cs`: replace direct event router dispatch with intent channel dispatch.
- `BattleEventRouter.cs`: either reduce to intent-to-command factory adapter or retire if channel supersedes it.

5. Update and extend tests in:

- `Assets/Scripts/Core/Battle/Tests/GameTests.cs`
- Add focused channel/command tests in `Assets/Scripts/Core/Battle/Tests/` (new file allowed if needed).

6. Update docs:

- `Docs/Core/Battle.md` to describe the new pipeline and responsibilities.

## Concrete Steps

Run all commands from repository root `C:\Unity\Madbox`.

1. Verify current baseline before migration:

    powershell -NoProfile -ExecutionPolicy Bypass -File ".\.agents\scripts\check-analyzers.ps1"
    powershell -NoProfile -ExecutionPolicy Bypass -File ".\.agents\scripts\run-editmode-tests.ps1"

Expected: analyzer output includes `TOTAL:0`; EditMode report shows all tests passing.

2. Implement Milestone 1 scaffolding and add no-op parity tests.

3. Run milestone checks:

    powershell -NoProfile -ExecutionPolicy Bypass -File ".\.agents\scripts\run-editmode-tests.ps1"
    powershell -NoProfile -ExecutionPolicy Bypass -File ".\.agents\scripts\check-analyzers.ps1"

Expected: no behavior regressions and `TOTAL:0`.

4. Implement Milestone 2 (player attack migration) and run relevant targeted tests first, then full EditMode.

    powershell -NoProfile -ExecutionPolicy Bypass -File ".\.agents\scripts\run-editmode-tests.ps1"

Expected: attack path tests and simulation tests still pass.

5. Implement Milestone 3 (enemy hit migration) and re-run tests.

    powershell -NoProfile -ExecutionPolicy Bypass -File ".\.agents\scripts\run-editmode-tests.ps1"

Expected: damage/death/cooldown-related tests still pass.

6. Implement Milestone 4 cleanup and execute milestone quality gate:

    & ".\.agents\scripts\validate-changes.cmd"

Expected: gate completes with no test failures and analyzer diagnostics clean.

## Validation and Acceptance

Acceptance is behavioral and must be demonstrated with tests:

- Existing battle simulation outcomes remain true:
  - timer-only rule reaches lose when time runs out.
  - enemy-elimination rule reaches win when all enemies die.
  - combined rules lose when timer expires with enemies alive.
  - combined rules win when enemies are killed before timer expires.

- Event semantics remain true:
  - player attack emits attack and enemy-killed facts when lethal.
  - enemy hit emits player-damaged and player-killed facts as appropriate.
  - player auto-attack emits projectile-spawn facts first, then damage/kill facts only after projectile-hit confirmation intent.

- Architectural acceptance:
  - `EnemyService` no longer contains event-type routing methods.
  - command classes encapsulate action logic.
  - `Game` uses a single intent ingress that drives command execution and event emission.
  - commands do not return result objects; emitted facts are represented only as domain events.
  - enemy cooldown ticking is behavior-list driven.
  - enemy attack hit resolution is presentation-driven via view-confirmed intents.
  - player auto-attack while idle is initiated by view behavior binding and resolved through confirmed intents.

- Quality acceptance:
  - `.agents/scripts/validate-changes.cmd` passes cleanly.
  - analyzer report shows `TOTAL:0`.

## Idempotence and Recovery

This migration is designed to be additive first and subtractive later. During Milestones 1-3, keep temporary adapters so behavior can be compared safely before final cleanup.

If a milestone introduces regressions:

- Re-run focused battle tests to isolate the failing flow.
- Re-enable previous adapter path temporarily (do not delete old path until parity is proven).
- Re-run EditMode and analyzer checks before progressing.

All commands in this plan are safe to run multiple times. Quality scripts are idempotent and should be re-run after each fix.

## Artifacts and Notes

Capture concise proof snippets in this section during execution.

Example evidence format to append while implementing:

    Analyzer check:
    TOTAL:0

    EditMode report:
    Total: 128
    Passed: 128
    Failed: 0

    Behavioral proof:
    GameTests.Simulation_TimerAndEnemyRules_WhenEnemiesDieBeforeTimer_CompletesAsWin -> Passed

## Interfaces and Dependencies

Target interfaces to exist at end of migration (names may vary slightly if analyzers require naming adjustments):

- In `Assets/Scripts/Core/Battle/Runtime/`:

    public interface IBattleMessage {}
    public interface IBattleIntent : IBattleMessage {}
    public interface IBattleCommand : IBattleMessage
    {
        void Execute(BattleExecutionContext context);
    }
    public interface IBattleDomainEvent : IBattleMessage {}

- Command channel contract:

    internal sealed class IntentCommandChannel
    {
        public void Register<TIntent>(Func<TIntent, IBattleCommand> factory) where TIntent : class, IBattleIntent;
        public void Dispatch(IBattleIntent intent, BattleExecutionContext context);
    }

- Rich domain expectations:

    internal sealed class EnemyRuntimeState
    {
        public int ApplyDamage(int amount);
        public void TickBehaviors(float deltaTime);
        public bool CanAttemptAttack();
    }

    public class Player
    {
        public bool CanAutoAttackWhileIdle();
    }

- Mapping policy:

  `Intent -> Command` mapping lives in one place (game/channel setup), not scattered across services.

Dependencies remain inside `Madbox.Battle` runtime assembly unless a new cross-module contract is explicitly needed.

## Redefined Flows (Aligned with Basic-Game-Domain and New Pipeline)

### Player Attack Flow

Created intents and events:

- Intent: `PlayerAttackRequestedIntent` (source: ViewModel from player input such as button click or target select).
- Domain events:
  - `PlayerAttackPerformedEvent` (attack accepted and applied).
  - `EnemyKilledEvent` (if target HP reaches zero).
  - Invalid inputs are ignored (no rejection event).

Flow:

1. View captures player action and sends input to ViewModel.
2. ViewModel creates `PlayerAttackRequestedIntent` and sends it to `Game` intent ingress.
3. Intent channel uses registered factory `<PlayerAttackRequestedIntent, ResolvePlayerAttackCommand>`.
4. Factory validates actor/target references and builds command only when valid.
5. `ResolvePlayerAttackCommand.Execute(...)` mutates rich domain models (`EnemyRuntimeState.ApplyDamage(...)`) and publishes domain events directly through context.
6. `Game` forwards published domain events to `EventTriggered`.
7. `GameRuleEvaluator` runs during tick and can complete game based on resulting state.

### Enemy Attack Flow

Created intents and events:

- Intent: `EnemyHitObservedIntent` (source: view/collision observer after presentation/physics confirms an attack actually connected).
- Domain events:
  - `PlayerDamagedEvent` (damage applied to player).
  - `PlayerKilledEvent` (player reaches zero HP).
  - optional `EnemyAttackPerformedEvent` can be emitted when hit is accepted and applied.

Flow:

1. During `Game.Tick`, enemy behavior ticking runs via `EnemyRuntimeState.TickBehaviors(deltaTime)` for all behaviors.
2. View-side prefab component reads mapped behavior state and checks whether attack can be attempted now (`CanAttemptAttack` semantics).
3. View performs spatial/physics checks (distance, line-of-sight, blockers, collision feasibility).
4. If presentation confirms a valid hit/contact, ViewModel sends `EnemyHitObservedIntent` into `Game` intent ingress.
5. Intent channel maps to `ResolveEnemyHitObservedCommand`.
6. Command applies authoritative damage/cooldown rules and publishes resulting domain events.
7. `Game` forwards domain events (`PlayerDamagedEvent`, `PlayerKilledEvent`, optional `EnemyAttackPerformedEvent`) through `EventTriggered`.
8. `GameRuleEvaluator` determines completion from updated state on tick.

### Player Auto-Attack While Idle Flow

Created intents and events:

- Intent: `PlayerAutoAttackObservedIntent` (source: view simulation when player is not moving and player auto-attack behavior is ready).
- Intent: `PlayerProjectileHitObservedIntent` (source: view/projectile simulation when a spawned projectile collides with a valid enemy).
- Domain events:
  - `PlayerProjectileSpawnedEvent` (auto-attack accepted and projectile is spawned with projectile id and targeting metadata).
  - `PlayerAttackPerformedEvent` (optional combat fact when projectile hit is accepted and applied).
  - `EnemyKilledEvent` (if target HP reaches zero).

Flow:

1. During view simulation, player movement state is observed; when player is idle, auto-attack behavior can be considered.
2. View-side mapped player behavior reads runtime readiness (`CanAutoAttackWhileIdle` semantics).
3. View checks enemy presence/range/LOS using presentation physics data.
4. If a valid target exists, ViewModel sends `PlayerAutoAttackObservedIntent` into `Game` intent ingress.
5. Intent channel maps to `ResolvePlayerAttackCommand` (or dedicated `ResolvePlayerAutoAttackCommand`), which applies cooldown/state and emits `PlayerProjectileSpawnedEvent` (no direct damage yet).
6. View receives `PlayerProjectileSpawnedEvent`, spawns and simulates projectile travel, and evaluates projectile collision.
7. When projectile hit is confirmed, ViewModel sends `PlayerProjectileHitObservedIntent` into `Game`.
8. Intent channel maps to `ResolvePlayerProjectileHitCommand`, which applies authoritative damage and publishes resulting combat domain events.
9. `Game` forwards `PlayerAttackPerformedEvent` and `EnemyKilledEvent` through `EventTriggered`.
10. `GameRuleEvaluator` determines completion from updated state on tick.

Revision Note (2026-03-18): Initial ExecPlan created to implement the requested Intent-Command-Event architecture with rich domain models and staged migration safeguards.
Revision Note (2026-03-18): Updated after review to remove command result return objects, define behavior-list-driven enemy cooldown/attack execution, rename attack command flow around validated intents and resolver commands, and add explicit player/enemy flow definitions (intent origin + event outputs + sequence).
Revision Note (2026-03-18): Updated attack ownership model to direct behavior binding in prefab/view, removed runtime attack-check request events, and added player idle auto-attack flow using the same view-confirmed intent pattern.
Revision Note (2026-03-18): Added projectile lifecycle semantics for player idle auto-attack so hit resolution is not treated as direct damage.
