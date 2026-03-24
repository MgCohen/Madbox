# Levels (Meta authoring)

## TL;DR

- Purpose: Authoring-time level data — Addressables scene reference, enemy spawn entries, serialized rule assets consumed by `Madbox.Battle`.
- Location: `Assets/Scripts/Meta/Levels/Runtime/` (`Madbox.Levels`), tests `Assets/Scripts/Meta/Levels/Tests/` (`Madbox.Levels.Tests`).
- Depends on: `Madbox.Enemies` (enemy prefab references), Unity Addressables, ScriptableObject pipeline.
- Used by: `Madbox.Battle`, `Madbox.Gameplay`, LiveOps `LevelService` (joins with `LevelGameData`), main menu listing via label **`MadboxLevels`**.
- Runtime/Editor: runtime assets; authored in Editor.

Keywords: level, ScriptableObject, Addressables, rules, LevelDefinition

## Responsibilities

- Owns: `LevelDefinition`, `PlayerLoadoutDefinition` authoring types and serialized fields consumed by battle and bootstrap.
- Does not own: rule execution (handlers in `Madbox.Battle`), Cloud Code progression (`Madbox.Level` / `LevelGameData`), or UI.
- Boundaries: Data-only assets; evaluation logic lives in `Madbox.Battle`.

## Public API

| Symbol | Purpose | Inputs | Outputs | Failure behavior |
|---|---|---|---|---|
| `LevelDefinition` | ScriptableObject: `LevelId`, scene reference, enemy entries, `LevelRuleDefinition` list. | Asset authoring | Serialized level payload | Invalid refs caught at load/runtime in consumers. |
| `PlayerLoadoutDefinition` | ScriptableObject: player `AssetReference`, weapon entries for `PlayerFactory` / bootstrap. | Asset authoring | Loadout data | Missing Addressables keys fail at preload/spawn. |

## Setup / Integration

1. Create assets via **Create > Madbox > Levels > Level Definition** (and rules under **Create > Madbox > Levels > Rules**).
2. Set **`LevelId`** to match remote-config / LiveOps ordering for the Level module.
3. Assign `SceneAssetReference` and enemy `AssetReferenceT<Enemy>` entries.
4. Add label **`MadboxLevels`** to definitions that should appear after bootstrap preload (main menu).
5. Optional: create **Player Loadout** and register in Addressables with address **`Player Loadout`** for `PlayerLoadoutAssetProvider` (see `Docs/App/GameView.md`).

## How to Use

1. Author a new `LevelDefinition` and assign a valid Addressables scene.
2. Populate `enemyEntries` with Addressable enemy prefabs.
3. Attach rule assets to `gameRules` and ensure `Madbox.Battle` registers matching handlers.
4. Align `LevelId` with server/LiveOps config when using progression APIs.
5. For menu visibility, confirm Addressables label and preload group membership.

## Examples

### Minimal

```csharp
// LevelDefinition is data-only; consumed by BattleGameFactory after additive scene load
[SerializeField] private LevelDefinition level;
```

### Realistic

```
1. Create Level Definition asset, set LevelId = 1, assign Arena scene reference.
2. Add enemy entries pointing at Bee prefab Addressables.
3. Add TimeElapsedCompleteRule asset to gameRules.
4. Register TimeElapsedCompleteRuleHandler in RuleHandlerRegistry (Battle).
```

### Guard / Error path

```csharp
// Mismatched LevelId vs server: CompleteLevelAsync / menu ordering may desync — keep IDs aligned with LiveOps LevelConfig
```

## Best Practices

- Keep one source of truth for `LevelId` across assets and remote config.
- Use shared attribute folders (`Assets/Data/Attributes/`) per project conventions.
- Prefer labels (`MadboxLevels`) for discoverable level lists over hard-coded paths.

## Anti-Patterns

- Putting gameplay rule code on `LevelDefinition` or rule ScriptableObjects — use `RuleHandler` types in `Madbox.Battle`.
- Referencing scenes or enemies that are not Addressable — loads will fail at runtime.

## Testing

- Test assembly: `Madbox.Levels.Tests` (EditMode).
- Run:

```powershell
& ".\.agents\scripts\run-editmode-tests.ps1" -AssemblyNames "Madbox.Levels.Tests"
```

- Expected: all tests pass, zero failures.
- Bugfix rule: add/update regression test first.

## AI Agent Context

- Invariants: assets remain data-only; `LevelId` must stay consistent with LiveOps where used.
- Allowed Dependencies: `Madbox.Enemies`, Unity, Addressables.
- Forbidden Dependencies: `Madbox.Battle` implementation detail types in serialized fields beyond shared definitions.
- Change Checklist: update `Madbox.Levels.Tests`; verify Addressables groups; sync `Docs` if authoring menus change.
- Known Tricky Areas: `LevelId` vs menu order vs `LevelGameData.States` merge in `LevelService`.

## Related

- `Docs/App/Battle.md`
- `Docs/App/GameView.md`
- `Docs/Meta/LiveOpsLevel.md`
- `Plans/` (level integration plans as referenced in repo)
- `Architecture.md`

## Changelog

- 2025-03-23: Module doc path aligned with `Assets/Scripts/Meta/Levels/` (`Docs/Meta/Levels.md`) from the former `Docs/Core/` location.
- 2025-03-23: Restructured to module documentation standard; clarified Meta authoring assembly `Madbox.Levels` vs LiveOps `Madbox.Level`.
