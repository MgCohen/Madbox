# Core Levels

## TL;DR
- Purpose: Defines level/domain data used by runtime modules (battle and enemies).
- Location: `Assets/Scripts/Core/Levels/Runtime/`.
- Depends on: `Scaffold.Records`.
- Used by: `Madbox.Battle`, `Madbox.Enemies`, and tests in `Madbox.Levels.Tests`.
- Runtime/Editor: pure runtime C# module with EditMode tests.
- Keywords: level definition, enemy definition, behavior definition, game rules.

## Responsibilities
- Owns: value records (`LevelId`, `EntityId`) and level/enemy definitions.
- Owns: enemy behavior definition contracts (`EnemyBehaviorDefinition`, contact/movement definitions).
- Owns: level game-rule definitions and constructor guard invariants.
- Does not own: runtime mutation/state ticking (battle/enemies runtime modules).
- Does not own: event routing, command execution, or wallet/reward operations.
- Boundaries: pure C# domain/data only, no Unity-facing implementation.

## Public API
| Symbol | Purpose | Inputs | Outputs | Failure behavior |
|---|---|---|---|---|
| `LevelDefinition` | Root level data aggregate | level id, gold reward, enemy entries, optional game rules | immutable level metadata + collections | Ctor throws on null/invalid values, duplicate enemy type ids, empty enemy/rule sets |
| `EnemyDefinition` | Enemy archetype data | enemy type id, max health, behavior definitions | immutable enemy archetype | Ctor throws on invalid id/health/behaviors |
| `LevelEnemyDefinition` | Enemy archetype + count | `EnemyDefinition`, spawn count | immutable binding | Ctor throws on null/invalid count |
| `LevelGameRuleDefinition` + concrete definitions (`EnemyEliminatedWinRuleDefinition`, `TimeLimitLoseRuleDefinition`, `PlayerDefeatedLoseRuleDefinition`) | Declares match end conditions | `BattleContext` in `CheckRule` | `bool` + `GameEndReason` | Returns false when condition not met |
| `EnemyBehaviorDefinition` + concrete definitions (`MovementBehaviorDefinition`, `ContactAttackBehaviorDefinition`) | Declares enemy behavior configuration | constructor params per behavior | immutable behavior config | Ctor/record guards via consumer validation |
| `LevelId`, `EntityId` | Strongly typed IDs | string value | immutable record value | Guarded by consuming ctors |

## Setup / Integration
1. Add asmdef dependency to `Madbox.Levels` from consumer module.
2. Build `EnemyDefinition` + `LevelEnemyDefinition` entries.
3. Build `LevelDefinition` with optional explicit `GameRules` (or rely on defaults).
4. Pass `LevelDefinition` into runtime modules (`Game`, `EnemyService`).
5. Quick check: run analyzer script and ensure no boundary violations.
- Common mistake: passing duplicate enemy type ids in one level.
- Common mistake: assuming rules can be null/empty; constructor enforces non-empty normalized list.

## How to Use
1. Define enemy archetypes and their behavior definitions.
2. Compose level enemy entries with counts.
3. Define game rules for win/lose conditions.
4. Construct `LevelDefinition` and inject into runtime systems.

## Examples
### Minimal
```csharp
EnemyDefinition slime = new EnemyDefinition(new EntityId("slime"), 20, new EnemyBehaviorDefinition[0]);
LevelEnemyDefinition entry = new LevelEnemyDefinition(slime, 1);
LevelDefinition level = new LevelDefinition(new LevelId("level-1"), 5, new[] { entry });
```

### Realistic
```csharp
EnemyBehaviorDefinition[] behaviors =
{
    new MovementBehaviorDefinition(1.5f, 4f),
    new ContactAttackBehaviorDefinition(5, 1.2f, 0.8f)
};

EnemyDefinition brute = new EnemyDefinition(new EntityId("brute"), 50, behaviors);
LevelEnemyDefinition wave = new LevelEnemyDefinition(brute, 3);
LevelGameRuleDefinition[] rules =
{
    new EnemyEliminatedWinRuleDefinition(),
    new TimeLimitLoseRuleDefinition(60f)
};

LevelDefinition level = new LevelDefinition(new LevelId("arena-01"), 25, new[] { wave }, rules);
```

### Guard / Error path
```csharp
LevelEnemyDefinition[] entries =
{
    new LevelEnemyDefinition(new EnemyDefinition(new EntityId("slime"), 10, new EnemyBehaviorDefinition[0]), 1),
    new LevelEnemyDefinition(new EnemyDefinition(new EntityId("slime"), 12, new EnemyBehaviorDefinition[0]), 1)
};

// Throws: duplicate enemy type id "slime".
LevelDefinition invalid = new LevelDefinition(new LevelId("dup"), 0, entries);
```

## Best Practices
- Keep this module declarative: data definitions and guards only.
- Treat rule definitions as stateless; pass all dynamic runtime data through `BattleContext`.
- Keep behavior definitions configuration-only; runtime logic belongs outside Levels.
- Prefer explicit IDs and unique enemy type entries per level.
- Keep module Unity-agnostic and analyzer clean.
- Document new definitions in this module doc when added.

## Anti-Patterns
- Adding ticking/cooldown/runtime mutation logic to level definitions.
- Encoding rule evaluation state inside rule definitions.
- Referencing battle/enemies runtime services from this module.
- Migration guidance: move runtime behavior/commands into `Madbox.Battle` or `Madbox.Enemies` and keep Levels as pure definitions.

## Testing
- Test assembly: `Assets/Scripts/Core/Levels/Tests/Madbox.Levels.Tests.asmdef` (`Madbox.Levels.Tests`).
- Run:
```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File ".\.agents\scripts\run-editmode-tests.ps1"
powershell -NoProfile -ExecutionPolicy Bypass -File ".\.agents\scripts\check-analyzers.ps1"
```
- Expected: all relevant tests pass and analyzer `TOTAL:0`.
- Bugfix rule: add/update regression tests before applying the fix.

## AI Agent Context
- Invariants:
  - `LevelDefinition` must always contain valid enemy entries.
  - `GameRules` must be non-null and non-empty after normalization.
  - This module remains pure definitions (no runtime behavior execution).
- Allowed Dependencies:
  - `Scaffold.Records`
- Forbidden Dependencies:
  - `Madbox.Battle`
  - `Madbox.Enemies`
  - UnityEngine/MonoBehaviour APIs
- Change Checklist:
  - Keep constructor guards for null/invalid collections.
  - Preserve unique enemy-type enforcement in `LevelDefinition`.
  - Keep new rule types deriving from `LevelGameRuleDefinition`.
  - Re-run analyzer/test scripts.
- Known Tricky Areas:
  - Default/normalized game-rule behavior when optional rule list is omitted.
  - Duplicate enemy type detection semantics.

## Related
- `Architecture.md`
- `Docs/Testing.md`
- `Docs/AutomatedTesting.md`
- `Docs/Core/Battle.md`
- `Docs/Core/Enemies.md`

## Changelog
- 2026-03-18: Rewrote module doc to match Module Documentation Standard section order and constraints.
- 2026-03-18: Updated content for rules/behaviors subfolder organization and level-driven game rules.
