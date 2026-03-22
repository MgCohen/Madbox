# Core Level (LiveOps progression)

## TL;DR

- Purpose: client holder for `LevelGameData` from aggregated `GameData` (distinct from gameplay `Madbox.Levels`).
- Location: `Assets/Scripts/Core/LiveOpsLevel/Runtime/` (`Madbox.Level`), installer `Madbox.Level.Container`.
- Depends on: `Madbox.LiveOps`, DTO plugin.

## Responsibilities

- `LevelService` extends `GameClientModuleBase<LevelGameData>`; `InitializeAsync` assigns `protected data` from `ILiveOpsService.GetModuleData<LevelGameData>()`.
- `LevelGameData` (DTO) exposes **`States`** only: a list of **`LevelStateEntry`** (`LevelId` + **`LevelAvailabilityState`**). Built on the server via **`new LevelGameData(LevelPersistence, LevelConfig)`** (config order defines progression).
- **`CompleteLevelAsync(int levelId)`** calls the backend; **`CompleteLevelResponse`** has **`Succeeded`** and **`CompletedLevelId`** when successful.

## Registration

`LevelInstaller` registers `LevelService` with **`AsImplementedInterfaces()`** and **`AsSelf()`**. Invoked from **`BootstrapMetaInstaller`** after LiveOps.

## Tests

EditMode: `Assets/Scripts/Core/LiveOpsLevel/Tests` (`LevelServiceTests`, `LevelGameDataTests`).
