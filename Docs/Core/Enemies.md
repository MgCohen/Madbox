# Core Enemies

## TL;DR
- Purpose: Owns enemy runtime models, behavior runtimes, and enemy lifecycle/tick service.
- Location: `Assets/Scripts/Core/Enemies/Runtime/` and `Assets/Scripts/Core/Enemies/Authoring/`.
- Depends on: `Madbox.Levels`, `Scaffold.MVVM.Model` (runtime), Unity Addressables (authoring).
- Used by: `Madbox.Battle` and tests in `Madbox.Enemies.Tests`.
- Runtime/Editor: pure runtime C# module plus module-local Unity authoring/editor assets.
- Keywords: enemy runtime, behavior runtime, lifecycle service, domain model, scriptableobject, authoring.

## Responsibilities
- Owns: `EnemyRuntimeState` as bindable runtime model.
- Owns: enemy behavior runtime contracts and implementations.
- Owns: `EnemyService` responsibilities (initialize, fetch, dispose, tick behaviors).
- Owns: runtime enemy creation from level definitions (`EnemyRuntimeStateFactory`).
- Owns: enemy authoring assets/components (`EnemyDefinitionSO`, `EnemyAuthoringReference`) and custom editor support.
- Does not own: battle intent routing/command mapping.
- Does not own: player model or game completion evaluation.
- Does not own: Unity movement/physics/view simulation.
- Boundaries: pure C# runtime, no UnityEngine dependencies.

## Public API
| Symbol | Purpose | Inputs | Outputs | Failure behavior |
|---|---|---|---|---|
| `EnemyService` | Lifecycle/query/tick service for runtime enemies | `LevelDefinition` ctor; runtime ids/enemy instances in methods | alive/dead counts and enemy retrieval/disposal outcomes | Ctor throws on null level; methods return `false` on invalid input/state |
| `EnemyRuntimeState` | Enemy runtime model | runtime `EntityId`, `EnemyDefinition`, initial health, behaviors | mutable health/alive state and readonly behavior list | Ctor throws on invalid args; `ApplyDamage` ignores non-positive values |
| `IEnemyBehaviorRuntime` | Runtime behavior contract | `Tick(float)` | behavior-specific state changes | Implementations ignore invalid ticks as needed |
| `ContactAttackBehaviorRuntime` | Contact-attack cooldown and damage gating | `ContactAttackBehaviorDefinition`, `TryConsume` inputs | applied damage and cooldown update | Returns `false` when on cooldown or invalid damage |

## Setup / Integration
1. Add asmdef dependency to `Madbox.Enemies` from consumers (for example `Madbox.Battle`).
2. Construct `EnemyService` with `LevelDefinition`.
3. Use `TryGetEnemy` to resolve runtime enemy models inside commands.
4. Call `EnemyService.Tick(deltaTime)` each frame to tick behavior runtimes.
5. Use `TryDisposeEnemy` only after the enemy is no longer alive.
- Common mistake: treating `EnemyService` as a command handler; keep it focused on lifecycle/query/tick.
- Common mistake: mutating enemies from view layer instead of command flow.

## How to Use
1. Build level enemy definitions in `Madbox.Levels`.
2. Initialize `EnemyService` for a match/session.
3. Resolve runtime enemies by id during command execution.
4. Mutate enemy domain model directly (`ApplyDamage`) and dispose when dead.
5. Tick behavior runtimes each frame.

## Examples
### Minimal
```csharp
EnemyService service = new EnemyService(levelDefinition);
service.Tick(0.016f);
```

### Realistic
```csharp
if (service.TryGetEnemy(targetId, out EnemyRuntimeState enemy))
{
    enemy.ApplyDamage(10);
    if (enemy.IsAlive == false)
    {
        service.TryDisposeEnemy(enemy);
    }
}
```

### Guard / Error path
```csharp
bool found = service.TryGetEnemy(null, out EnemyRuntimeState enemy); // false
bool disposed = service.TryDisposeEnemy(enemy); // false if null or still alive
```

## Best Practices
- Keep commands self-contained and mutate enemy models directly.
- Keep `EnemyService` limited to initialize/fetch/dispose/tick behavior responsibilities.
- Keep behavior-specific state inside behavior runtime classes.
- Keep public boundary contracts under `Contracts/` to satisfy analyzers.
- Preserve pure C# boundaries and no Unity-facing dependencies.
- Re-run analyzer checks after folder/namespace moves.

## Anti-Patterns
- Adding domain command logic directly to `EnemyService`.
- Adding battle event routing into enemy module classes.
- Pushing view concerns (movement/LOS/physics) into runtime enemy module.
- Migration guidance: move command orchestration to battle router/commands and keep this module domain-focused.

## Testing
- Test assembly: `Assets/Scripts/Core/Enemies/Tests/Madbox.Enemies.Tests.asmdef` (`Madbox.Enemies.Tests`).
- Run:
```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File ".\.agents\scripts\run-editmode-tests.ps1"
powershell -NoProfile -ExecutionPolicy Bypass -File ".\.agents\scripts\check-analyzers.ps1"
```
- Expected: relevant tests pass and analyzer `TOTAL:0`.
- Bugfix rule: add/update a regression test first, then verify fail-before/pass-after.

## AI Agent Context
- Invariants:
  - Enemy module stays Unity-agnostic and focused on enemy domain state.
  - `EnemyService` remains narrow: initialize, fetch, dispose, tick behaviors.
  - Behavior runtime state is encapsulated per behavior implementation.
- Allowed Dependencies:
  - `Madbox.Levels`
  - `Scaffold.MVVM.Model`
- Forbidden Dependencies:
  - `Madbox.Battle`
  - `Madbox.Gold`
  - UnityEngine/MonoBehaviour APIs
- Change Checklist:
  - Keep interfaces/contracts in `Runtime/Contracts` for public boundaries.
  - Keep service APIs returning bool for guardable operations.
  - Keep tests and analyzers green after refactors.
  - Verify asmdef references remain minimal.
- Known Tricky Areas:
  - Behavior contract namespace/folder alignment (`SCA0007` style constraints).
  - Correct alive/dead counter behavior on enemy disposal.

## Related
- `Architecture.md`
- `Docs/Testing.md`
- `Docs/AutomatedTesting.md`
- `Docs/Core/Battle.md`
- `Docs/Core/Levels.md`
- `Docs/Infra/Addressables.md`

## Changelog
- 2026-03-18: Rewrote module doc to match Module Documentation Standard section order and constraints.
- 2026-03-18: Added Enemy module ownership boundaries after extraction from Battle module.
- 2026-03-18: Added module-local enemy authoring/editor ownership (`Core/Enemies/Authoring` and `Core/Enemies/Editor`).
