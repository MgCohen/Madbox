# Core Level (LiveOps progression)

## TL;DR

- Purpose: client holder for `LevelGameData` from aggregated `GameData`, merged with Addressables `LevelDefinition` assets (distinct namespace from gameplay authoring `Madbox.Levels`).
- Location: `Assets/Scripts/Meta/Levels/LiveOps/Runtime/` (`Madbox.Level`), installer `Madbox.Level.Container` under `LiveOps/Container/`.
- Depends on: `Madbox.LiveOps`, `Madbox.Addressables` (group provider), `Madbox.Levels`, DTO plugin.

## Responsibilities

- `LevelService` extends `GameClientModuleBase<LevelGameData>`; `InitializeAsync` assigns `protected data` from `ILiveOpsService.GetModuleData<LevelGameData>()`, then **`OnInitializedAsync`** joins:
  - preloaded **`IAssetGroupProvider<LevelDefinition>`** (bootstrap **`LevelAssetProvider`**, label `MadboxLevels`), and
  - **`LevelGameData.States`** (config order, `LevelId` + **`LevelAvailabilityState`**),
  into **`IReadOnlyList<AvailableLevel>`** exposed via **`GetAvailableLevels()`** and **`ILevelMenuService`**.
- `LevelGameData` (DTO) exposes **`States`**: **`LevelStateEntry`** per configured id. Built via **`new LevelGameData(LevelPersistence, LevelConfig)`** (config order defines progression).
- **`CompleteLevelAsync(int levelId)`** calls the backend; **`CompleteLevelResponse`** has **`Succeeded`** and **`CompletedLevelId`** when successful.

## Registration

`LevelCatalogInstaller` registers `LevelService` as **`AsSelf()`**, **`ILevelMenuService`**, **`IGameClientModule`**, and **`IAsyncLayerInitializable`**. Invoked from **`BootstrapMetaInstaller`** after LiveOps. Asset preload registers **`LevelAssetProvider`** in **`BootstrapAssetInstaller`**.

## Tests

EditMode: `Assets/Scripts/Meta/Levels/LiveOps/Tests` (`LevelServiceTests`, `LevelGameDataTests`).
