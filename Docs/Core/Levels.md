# Levels (Core)

## Purpose

Authoring-time level data: Addressables scene reference, enemy spawn entries (`AssetReferenceT<Enemy>` from **`Madbox.Enemies`**), and serialized rule assets consumed by `Madbox.Battle`.

## Public API

| Type | Role |
|------|------|
| `LevelDefinition` | ScriptableObject holding **`LevelId`** (matches remote-config IDs for the Level module), scene reference, `LevelEnemySpawnEntry` list, and `LevelRuleDefinition` rules. |
| `PlayerLoadoutDefinition` | ScriptableObject with one player **`AssetReference`** and a list of weapon **`AssetReference`** entries; consumed by **`PlayerService`** / **`PlayerFactory`** in `Madbox.Bootstrap.Runtime` (see `Docs/App/GameView.md`). Bootstrap preloads it via **`PlayerLoadoutAssetProvider`** (address key **`Player Loadout`**). |

## Usage

1. Create a `Level Definition` asset via **Create > Madbox > Levels > Level Definition**.
2. Set **`LevelId`** to match remote-config level IDs for the Level module (see LiveOps `LevelGameData` / `LevelConfig` ordering).
3. Assign `SceneAssetReference` and populate `enemyEntries` with Addressable enemy prefabs.
4. Add rule assets under **Create > Madbox > Levels > Rules** and reference them from the level’s `gameRules` list.
5. For main-menu listing, add the Addressables label **`MadboxLevels`** to each `LevelDefinition` asset that should appear after bootstrap preload (see `MainMenuLevelsIntegration-ExecPlan.md`).
6. Optional: create **Create > Madbox > Player > PlayerLoadout** for Addressables player + weapon prefab list; add the asset to Addressables with address **`Player Loadout`** so **`PlayerLoadoutAssetProvider`** preloads it and **`PlayerService`** receives **`PlayerLoadoutDefinition`** by injection before **`PlayerFactory.CreateReadyPlayerAsync`** (see `Docs/App/GameView.md`).

## Design notes

- Level assets stay data-only; evaluation logic lives in `Madbox.Battle` handlers registered against rule types.
- Client-side grouping with LiveOps progression is integrated in a separate pass.
