# Core Domain Modeling (Pure C#) with Subdomain Modules

This ExecPlan is a living document. The sections `Progress`, `Surprises & Discoveries`, `Decision Log`, and `Outcomes & Retrospective` must be kept up to date as work proceeds.

This plan must be maintained in accordance with `PLANS.md` at the repository root.

## Purpose / Big Picture

After this change, the project will have pure C# domain models split by subdomain instead of a single large domain module. This keeps boundaries clear and prevents a monolithic assembly from becoming crowded with unrelated concepts.

Delivery is incremental and risk-managed: first meta economy (`Gold`), then level definitions, then a dedicated battle event system, then battle runtime in increasing complexity (basic loop, advanced systems, full simulation). This creates fast validation cycles while preserving long-term architecture.

## Progress

- [ ] No stages started yet.
- [ ] Stage 1: implement `Meta/Gold` module + tests/docs and validate gate.
- [ ] Stage 2: implement `Core/Levels` module + tests/docs and validate gate.
- [ ] Stage 3: implement `Core/Battle` event channel (single input endpoint + single output callback) and validate gate.
- [ ] Stage 4: implement basic battle runtime (lifecycle + timer tick + minimal entity state transitions) and validate gate.
- [ ] Stage 5: implement advanced battle systems (behavior pipeline, richer events, end-condition evaluator integration) and validate gate.
- [ ] Stage 6: implement full simulation slice needed for milestone acceptance and validate gate.

## Surprises & Discoveries

- Observation: Existing infra (`Scaffold.MVVM.Model`) already supports Unity-free observable mutable state and can be reused by runtime state objects.
  Evidence: `Assets/Scripts/Infra/Model/Runtime/Model.cs`.

- Observation: Scope naturally contains two domain levels with different volatility: battle runtime rules and meta/content definitions.
  Evidence: Battle requires ongoing systems/runtime evolution while gold/level definitions are mostly stable contracts.

## Decision Log

- Decision: Do not centralize all domain types in `Core/GameDomain`.
  Rationale: Subdomain modules keep boundaries stable and avoid a catch-all assembly.
  Date/Author: 2026-03-17 / Codex.

- Decision: Use three modules for current scope.
  Rationale: `Core/Battle` owns runtime combat/session behavior, `Core/Levels` owns level definitions, `Meta/Gold` owns wallet/meta currency.
  Date/Author: 2026-03-17 / Codex.

- Decision: Use `GameState` with three explicit runtime moments: `NotRunning`, `Running`, `Done`.
  Rationale: Improves readability of before/during/after lifecycle without duplicating outcome semantics.
  Date/Author: 2026-03-17 / Codex.

- Decision: Do not expose `EndReason` as a game property.
  Rationale: Completion reason is emitted only through `OnCompleted(GameEndReason)`, keeping runtime API minimal.
  Date/Author: 2026-03-17 / Codex.

- Decision: Keep battle API concrete and direct (`Game` class), with `Trigger(...)` for input and `EventTriggered` for output.
  Rationale: Avoid unnecessary interface layering (`IBattleGame`) while preserving one input and one output event channel.
  Date/Author: 2026-03-17 / Codex.

- Decision: Remove string behavior discriminator (`BehaviorType`) and use C# type-based pattern matching for behavior definitions.
  Rationale: Concrete behavior types are simpler and safer than duplicated string tags.
  Date/Author: 2026-03-17 / Codex.

- Decision: Keep trigger resolution inside `Game` with direct type checks (`switch`/pattern matching), and do not introduce an event bus adapter or visitor layer in this milestone.
  Rationale: Lowest-complexity path for now; keeps flow explicit and easy to validate in tests.
  Date/Author: 2026-03-17 / Codex.

## Outcomes & Retrospective

No outcomes yet. Work has not started.

## Context and Orientation

Relevant repository context:

- `Architecture.md`: module boundary rules and dependency direction.
- `Docs/Infra/Model.md`: observable `Model` base for pure C# runtime state.
- `Research/Battle/Battle-Research-and-Specs.md`: domain authority and tick/input flow.
- `Research/Entities/Entity-Research-and-Specs.md`: immutable definitions vs runtime state.
- `Research/Core-Loop/Core-Loop-Research-and-Specs.md`: app/game orchestration expectations.

Domain terms used here:

- Definition: immutable data describing setup/configuration.
- Runtime state: mutable per-run state.
- Battle event: unified event contract used for both external input into game and output from game.
- Output callback: observer hook where the game emits events when something happened.

### Why separate `Levels` and `Battle`

`Core/Levels` changes when content schema changes (ids, enemy composition, rewards). `Core/Battle` changes when runtime rules change (event handling, tick behavior, system orchestration). Keeping them separate allows each area to evolve with minimal coupling.

### Why separate `Gold`

Gold is meta progression and can be validated independently from runtime combat. This makes it a good first slice for architecture validation before complex runtime behavior.

### Why `LevelDefinition` and `LevelRuntimeState` both exist

`LevelDefinition` is immutable setup data (which enemies and rewards a level has). `LevelRuntimeState` is mutable execution data for a specific run (spawned/alive/dead counts, completion state, instance-level status). This separation avoids mutating authored data and supports repeatable sessions/tests.

## Target Module Layout

Planned modules:

- `Assets/Scripts/Meta/Gold/`
- `Assets/Scripts/Meta/Gold/Tests/`
- `Assets/Scripts/Core/Levels/`
- `Assets/Scripts/Core/Levels/Tests/`
- `Assets/Scripts/Core/Battle/`
- `Assets/Scripts/Core/Battle/Tests/`

`Container/` folders are optional and only added where DI installers are required.

## API and Dependencies

Dependency intent:

- `Madbox.Gold` has no dependency on `Madbox.Levels` or `Madbox.Battle`.
- `Madbox.Levels` has no dependency on `Madbox.Gold` or `Madbox.Battle`.
- `Madbox.Battle` depends on `Madbox.Levels`, `Madbox.Gold`, and `Scaffold.MVVM.Model`.
- Tests reference only module assemblies plus test assemblies.
- No domain module references `UnityEngine`.

Initial API sketch:

    namespace Madbox.Gold;

    public class GoldWallet
    {
        int CurrentGold { get; }
        bool TrySpend(int amount);
        void Add(int amount);
    }

    namespace Madbox.Levels;

    public record LevelId(string Value);

    public record EntityId(string Value);

    public abstract record EnemyBehaviorDefinition;

    public record MovementBehaviorDefinition(float MoveSpeed, float FollowRange) : EnemyBehaviorDefinition;

    public record ContactAttackBehaviorDefinition(int Damage, float CooldownSeconds, float AttackRange) : EnemyBehaviorDefinition;

    public class EnemyDefinition { ... }

    public class LevelEnemyDefinition { ... }

    public class LevelDefinition { ... }

    namespace Madbox.Battle;

    public enum GameState
    {
        NotRunning,
        Running,
        Done
    }

    public enum GameEndReason
    {
        None,
        Win,
        Lose,
        ForceQuit
    }

    public abstract record BattleEvent;

    public class Game
    {
        GameState CurrentState { get; }
        float ElapsedTimeSeconds { get; }
        LevelId CurrentLevelId { get; }

        void Initialize();
        void Start();
        void Tick(float deltaTime);
        void Trigger(BattleEvent gameEvent);

        event Action<BattleEvent> EventTriggered;
        event Action<GameEndReason> OnCompleted;
    }

    public record TryPlayerAttack(EntityId ActorId, EntityId? TargetId) : BattleEvent;

    public record PlayerAttack(EntityId ActorId, EntityId TargetId, int Damage) : BattleEvent;

    public record EnemyKilled(EntityId EnemyId, EntityId KillerId) : BattleEvent;

    // Collision is produced by prefab/view and sent into Game through Trigger.
    public record EnemyHitObserved(EntityId EnemyId, EntityId PlayerId, int RawDamage) : BattleEvent;

    // Game output events after authoritative resolution.
    public record PlayerDamaged(EntityId PlayerId, EntityId SourceEnemyId, int AppliedDamage, int RemainingHp) : BattleEvent;

    public record PlayerKilled(EntityId PlayerId, EntityId KillerEnemyId) : BattleEvent;

## Battle Event System (Dedicated Scope)

Single-channel rules:

1. Every external interaction enters battle through one endpoint: `Trigger(BattleEvent gameEvent)`.
2. Every battle output leaves battle through one callback: `EventTriggered`.
3. No direct command response is required in this version.
4. If an input is not accepted by battle rules, no state change is applied.
5. Game completion is emitted through `OnCompleted(GameEndReason)` (no exposed `EndReason` property).
6. Event logging/persistence is external concern and not required inside `Game`.

Explicit attack example:

1. View sends a UI intent to ViewModel: `Player clicked attack button`.
2. ViewModel translates this intent into a domain input event: `TryPlayerAttack`.
3. ViewModel calls `game.Trigger(new TryPlayerAttack(actorId, targetId))`.
4. `Game.Trigger(...)` resolves the input with a direct type switch (`switch`/pattern matching) and routes to the matching handler (`HandleTryPlayerAttack`).
5. Handler validates battle state (running), actor/target validity, and attack cooldown/range.
6. Handler applies runtime changes (for example: target HP in runtime model, cooldown timestamps, kill flags).
7. Runtime state changes are observable through model/property binding (`Model`-based runtime state).
8. Based on applied changes, game emits output events through `EventTriggered`:
   - `PlayerAttack` when attack succeeds
   - `EnemyKilled` when HP reaches zero
9. If the battle ends after these updates, game emits `OnCompleted(GameEndReason)`.
10. If input is rejected, no state change and no output event.

Enemy hit resolution sequence (explicit):

1. Enemy movement decision and attack condition stay inside prefab/view components (no game event emitted for these).
2. On collision/hit, prefab/view emits one input to ViewModel (for example `EnemyHitObserved` payload).
3. ViewModel forwards it to game via `game.Trigger(new EnemyHitObserved(enemyId, playerId, rawDamage))`.
4. `Game.Trigger(...)` routes to `HandleEnemyHitObserved(...)`.
5. `HandleEnemyHitObserved(...)` validates authority rules: game running, entities alive, cooldown/window, and anti-duplicate hit guard.
6. If rejected, exit with no state change and no output event.
7. If accepted, update runtime `Model` state (player HP and related combat state).
8. Emit transient output event(s) through `EventTriggered`:
   - `PlayerDamaged` on accepted damage
   - `PlayerKilled` when HP reaches zero
9. If player death ends the match, emit `OnCompleted(GameEndReason.Lose)`.

Snippet:

    // View -> ViewModel:
    // "player clicked attack button"
    game.EventTriggered += OnGameEvent;
    game.OnCompleted += OnCompleted;

    // ViewModel -> Game:
    game.Trigger(new TryPlayerAttack(actorId, targetId));
    game.Trigger(new EnemyHitObserved(enemyId, playerId, rawDamage));

    public void Trigger(BattleEvent gameEvent)
    {
        switch (gameEvent)
        {
            case TryPlayerAttack attack:
                HandleTryPlayerAttack(attack);
                break;
            case EnemyHitObserved enemyHit:
                HandleEnemyHitObserved(enemyHit);
                break;
        }
    }

    void HandleTryPlayerAttack(TryPlayerAttack attack)
    {
        // Validate and mutate runtime state (Model-backed properties).
        // Emit transient combat events only when accepted.
        EventTriggered?.Invoke(new PlayerAttack(attack.ActorId, attack.TargetId!, damage));
        if (targetHp <= 0)
        {
            EventTriggered?.Invoke(new EnemyKilled(targetId, attack.ActorId));
        }
    }

    void HandleEnemyHitObserved(EnemyHitObserved hit)
    {
        // Validate authority/rules, then mutate Model-backed player state.
        EventTriggered?.Invoke(new PlayerDamaged(hit.PlayerId, hit.EnemyId, appliedDamage, remainingHp));
        if (remainingHp <= 0)
        {
            EventTriggered?.Invoke(new PlayerKilled(hit.PlayerId, hit.EnemyId));
            OnCompleted?.Invoke(GameEndReason.Lose);
        }
    }

    void OnGameEvent(BattleEvent gameEvent)
    {
        if (gameEvent is PlayerAttack attack)
        {
            // Trigger animation/VFX.
        }
        else if (gameEvent is EnemyKilled killed)
        {
            // Trigger death feedback.
        }
        else if (gameEvent is PlayerDamaged damaged)
        {
            // Trigger player hit feedback.
        }
        else if (gameEvent is PlayerKilled playerKilled)
        {
            // Trigger player death feedback.
        }
    }

    void OnCompleted(GameEndReason reason)
    {
        // Open result screen.
    }

## Plan of Work

### Stage 1 (Low Risk): Gold Module

Create and validate `Meta/Gold` with focused models/services plus tests/docs. Implement wallet behavior (add/spend/guards), run tests, and pass full gate.

### Stage 2 (Low/Medium): Levels Module

Create and validate `Core/Levels` with definition models and behavior definitions. Add tests for definition validity and composition invariants.

### Stage 3 (Medium): Battle Event System Foundation

Implement `Core/Battle` event pipeline only (single `Trigger` input endpoint and single `EventTriggered` callback output), lifecycle skeleton, and tests proving input-to-output flow.

### Stage 4 (Medium): Basic Battle Runtime

Add minimal runtime state (`NotRunning` -> `Running` -> `Done`), tick timer, enemy spawn/alive counters, and basic end-condition checks.

### Stage 5 (High): Advanced Battle Systems

Add internal behavior processing for enemy behavior definitions, richer state changes, damage processing paths, and additional event emissions.

### Stage 6 (High): Full Simulation Slice

Implement the complete simulation slice required by current milestone acceptance, validate with targeted tests plus full gate.

## Concrete Steps

All commands run from repository root: `C:\Users\mtgco\.codex\worktrees\663e\Madbox`.

1. Stage 1 implementation and tests (`Meta/Gold`).
2. Run:

    - `powershell -NoProfile -ExecutionPolicy Bypass -File ".\.agents\scripts\run-editmode-tests.ps1" -AssemblyNames "Madbox.Gold.Tests"`
    - `& ".\.agents\scripts\validate-changes.cmd"`

3. Stage 2 implementation and tests (`Core/Levels`).
4. Run:

    - `powershell -NoProfile -ExecutionPolicy Bypass -File ".\.agents\scripts\run-editmode-tests.ps1" -AssemblyNames "Madbox.Levels.Tests"`
    - `& ".\.agents\scripts\validate-changes.cmd"`

5. Stage 3 implementation and tests (Battle event system).
6. Run:

    - `powershell -NoProfile -ExecutionPolicy Bypass -File ".\.agents\scripts\run-editmode-tests.ps1" -AssemblyNames "Madbox.Battle.Tests"`
    - `& ".\.agents\scripts\validate-changes.cmd"`

7. Stage 4, 5, and 6 implementation with incremental tests.
8. After each stage, rerun:

    - `powershell -NoProfile -ExecutionPolicy Bypass -File ".\.agents\scripts\run-editmode-tests.ps1" -AssemblyNames "Madbox.Battle.Tests"`
    - `& ".\.agents\scripts\validate-changes.cmd"`

## Validation and Acceptance

Stage 1 acceptance:

- Gold module runs pure C# tests and passes quality gate.
- Wallet behavior is covered and deterministic.

Stage 2 acceptance:

- Levels module supports enemy behavior definitions and passes tests/gate.

Stage 3 acceptance:

- Battle has one input endpoint (`Trigger`) and one output callback (`EventTriggered`).
- Attack flow maps UI intent -> `TryPlayerAttack` input -> state mutation -> `PlayerAttack` / `EnemyKilled` outputs when valid.
- Enemy move/attack decision stays in prefab/view; collision is emitted as `EnemyHitObserved` into `Trigger`, resolved by `Game`, then emits `PlayerDamaged` / `PlayerKilled` as needed.
- Completion reason is provided by `OnCompleted(GameEndReason)` callback only.
- No direct command-response API exists.

Stage 4 acceptance:

- `GameState` transitions through `NotRunning`, `Running`, and `Done`.
- Tick updates elapsed time only while running.

Stage 5 acceptance:

- Behavior-based enemy processing works for movement/attack definitions.
- Advanced event emissions and state updates are covered by tests.

Stage 6 acceptance:

- Full simulation slice for milestone runs and passes full quality gate.

## Idempotence and Recovery

Scaffolding and test commands are safe to rerun. If analyzers fail, fix asmdef references and visibility first. Complete stages in order so rollback remains small and clear.

## Artifacts and Notes

This plan intentionally keeps UI/ViewModel concrete implementation out of scope while defining explicit protocol boundaries they will consume.

Revision Note (2026-03-17): Expanded progress breakdown, added dedicated battle event-system stage, switched battle communication to single-channel publish/callback model without direct command responses, updated lifecycle state to `NotRunning/Running/Done`, and replaced enemy movement enum strategy with behavior-definition composition.
Revision Note (2026-03-17): Simplified contracts to reduce abstraction: removed `IBattleGame` in favor of concrete `Game`, renamed channel members to `Trigger`/`EventTriggered`, moved end reason exposure to `OnCompleted(GameEndReason)`, replaced behavior string discriminator with type-based matching, and switched IDs to single-value records.
Revision Note (2026-03-17): Removed `sealed` from class/record examples and expanded attack flow to explicit View intent -> ViewModel command mapping -> direct in-`Game` event resolution -> model-backed state updates -> emitted transient output events.
Revision Note (2026-03-17): Refined enemy attack flow: movement/attack-condition checks remain in prefab/view logic; only collision (`EnemyHitObserved`) enters `Game` through `Trigger`, where authoritative resolution updates model state and emits `PlayerDamaged`/`PlayerKilled`.
