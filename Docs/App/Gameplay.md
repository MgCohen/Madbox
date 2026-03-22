# Gameplay (navigation screen)

## TL;DR

- Purpose: In-game navigation screen (`GameView` / `GameViewModel`) that loads the selected level additively, starts `BattleGame`, and spawns the player.
- Location: `Assets/Scripts/App/Gameplay/Runtime/` (`Madbox.Gameplay`), composition in `Madbox.Bootstrap.Runtime` (`BattleGameplayInstaller`, `PlayerSpawnBridge`, `BootstrapMainMenuLauncher`).
- Depends on: `Scaffold.MVVM.*`, `Scaffold.Navigation`, `Madbox.Battle`, `Madbox.SceneFlow`, `Madbox.GameView` (Arena), `Madbox.Levels`, VContainer.
- Used by: `MainMenuViewModel` via `IGameFlowService`; `LevelButtonCollectionHandlerBehaviour` forwards clicks to `MainMenuViewModel.PlayLevel`.

## Responsibilities

- `IGameFlowService` opens `GameViewModel` with the selected `LevelDefinition` (`closeCurrent: true`).
- `GameSessionCoordinator` uses `ISceneFlowService` for additive loads, resolves `Arena` in the loaded scene for spawn origins, runs `BattleGameFactory.CreatePrepareStartAsync`, and spawns the player through `IPlayerSpawnService`.
- `IMainMenuLauncher` returns to the main menu without a Gameplay→MainMenu assembly cycle.

## Setup / Integration

1. Register `BattleGameplayInstaller` from `BootstrapCoreInstaller` (already wired).
2. Add `Game View` prefab to Addressables if navigation loads it at runtime (recommended: same preload group as main menu).
3. Ensure `Navigation Settings` includes the `Game` view config mapping `GameView` / `GameViewModel`.
4. Level Addressable scenes should include `Arena` for spawn positions (see `Docs/App/GameView.md`).

## Testing

- `Madbox.MainMenu.Tests`: level button and `PlayLevel` delegation.
- `Madbox.Battle.Tests`: `BattleGameFactory.CreatePrepareStartAsync` session start.

## Related

- `Plans/GameFlowNavigation/GameFlowNavigation-ExecPlan.md`
- `Docs/App/MainMenu.md`
- `Docs/Infra/SceneFlow.md`
