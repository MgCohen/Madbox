# Levels (Core)

## Purpose

Authoring-time level data: Addressables scene reference, enemy spawn entries (`AssetReferenceT<EnemyActor>`), and serialized rule assets consumed by `Madbox.Battle`.

## Public API

| Type | Role |
|------|------|
| `LevelDefinition` | ScriptableObject holding **`LevelId`** (matches remote-config IDs for the Level module), scene reference, `LevelEnemySpawnEntry` list, and `LevelRuleDefinition` rules. |

## Usage

1. Create a `Level Definition` asset via **Create > Madbox > Levels > Level Definition**.
2. Set **`LevelId`** to match remote-config level IDs for the Level module.
3. Assign `SceneAssetReference` and populate `enemyEntries` with Addressable enemy prefabs.
4. Add rule assets under **Create > Madbox > Levels > Rules** and reference them from the level’s `gameRules` list.

## Design notes

- Level assets stay data-only; evaluation logic lives in `Madbox.Battle` handlers registered against rule types.
- Client-side grouping with LiveOps progression is integrated in a separate pass.
