# Gameplay (navigation screen)

## TL;DR

- Purpose: In-game navigation screen (`GameView` / `GameViewModel`) — additive level load, `BattleGame`, player spawn, win/lose popup when session completes.
- Location: `Assets/Scripts/App/Gameplay/Runtime/` (`Madbox.Gameplay`), composition in `Madbox.Bootstrap.Runtime` (`BattleGameplayInstaller`, `PlayerSpawnBridge`, `BootstrapMainMenuLauncher`).
- Depends on: `Scaffold.MVVM.*`, `Scaffold.Navigation`, `Madbox.Battle`, `Madbox.SceneFlow`, `Madbox.GameView`, `Madbox.Levels`, VContainer.
- Used by: `MainMenuViewModel` via `IGameFlowService`; `LevelButtonCollectionHandlerBehaviour` → `PlayLevel`.
- Runtime/Editor: runtime App module.

Keywords: game flow, session coordinator, battle, scene flow

## Responsibilities

- Owns: `IGameFlowService` opening `GameViewModel` with `LevelDefinition`, `GameSessionCoordinator` additive load/unload, `BattleGameFactory` session creation, internal win/lose popup on `SessionCompleted`, `IMainMenuLauncher` return path.
- Does not own: Main menu UI, LiveOps progression APIs (those stay on `LevelService`), raw Addressables groups.
- Boundaries: Orchestrates battle + scene flow; avoids Gameplay→MainMenu assembly cycles via `IMainMenuLauncher`.

## Public API

| Symbol | Purpose | Inputs | Outputs | Failure behavior |
|---|---|---|---|---|
| `IGameFlowService` | Opens gameplay navigation target with level context. | `LevelDefinition`, nav options | View open | Navigation errors surface from `INavigation`. |
| `GameSessionCoordinator` | Drives additive scene load via `ISceneFlowService`, teardown. | Session lifetime | Load result | Fails if Addressables scene missing. |
| `GameViewModel` / `GameView` | Present session; tick battle; show end popup. | Session events | UI | Popup state tied to session completion. |

## Setup / Integration

1. Register `BattleGameplayInstaller` from `BootstrapCoreInstaller` (sample already wired).
2. Add Game View prefab to Addressables if loaded at runtime (preload with main menu recommended).
3. Ensure Navigation Settings includes `Game` view mapping `GameView` / `GameViewModel`.
4. Level Addressable scenes should include `Arena` for spawn positions (see `Docs/App/GameView.md`).

## How to Use

1. From menu, call `IGameFlowService` with selected `AvailableLevel`’s `LevelDefinition`.
2. Let coordinator load the additive scene, then factory create `BattleGame` and spawn player.
3. Tick battle from `GameView` until session completes; read popup state for win/lose.
4. Return to menu via launcher abstraction (no direct MainMenu assembly reference from Gameplay).

## Examples

### Minimal

```csharp
await gameFlowService.OpenGameAsync(levelDefinition, cancellationToken);
```

### Realistic

```
Flow: MainMenu PlayLevel → IGameFlowService → GameViewModel + additive scene → BattleGameFactory → tick until SessionCompleted → popup → MainMenu launcher
```

### Guard / Error path

```csharp
// Missing Arena in level scene: spawn positions may fail — validate LevelDefinition scene content
```

## Best Practices

- Keep `IGameFlowService` as the only entry from MainMenu into gameplay navigation.
- Unload additive scenes in coordinator teardown to avoid duplicate loads.

## Anti-Patterns

- Loading battle scenes directly from MainMenu — bypasses coordinator and session lifecycle.
- Coupling Gameplay to MainMenu concrete types — use `IMainMenuLauncher`.

## Testing

- `Madbox.MainMenu.Tests`: level button and `PlayLevel` delegation.
- `Madbox.Battle.Tests`: `BattleGameFactory.CreatePrepareStartAsync` session start.
- Run:

```powershell
& ".\.agents\scripts\run-editmode-tests.ps1" -AssemblyNames "Madbox.MainMenu.Tests","Madbox.Battle.Tests"
```

- Expected: all tests pass, zero failures.
- Bugfix rule: add/update regression test first.

## AI Agent Context

- Invariants: one gameplay navigation stack entry per run; coordinator owns additive scene lifetime.
- Allowed Dependencies: Battle, SceneFlow, GameView, Levels, Navigation.
- Forbidden Dependencies: MainMenu concrete types.
- Change Checklist: run MainMenu + Battle tests; update `GameView.md` if session contract changes.
- Known Tricky Areas: `closeCurrent: true` navigation flags; popup vs session teardown order.

## Related

- `Docs/App/Battle.md`
- `Docs/App/GameView.md`
- `Plans/GameFlowNavigation/GameFlowNavigation-ExecPlan.md`
- `Docs/App/MainMenu.md`
- `Docs/Infra/SceneFlow.md`
- `Architecture.md`

## Changelog

- 2025-03-23: Restructured to module documentation standard.
