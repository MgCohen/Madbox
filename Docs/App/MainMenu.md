# App MainMenu

## TL;DR

- Purpose: Main Menu slice for bootstrap validation — DI, navigation + Addressables view loading, MVVM binding for gold and level buttons.
- Location: `Assets/Scripts/App/MainMenu/` (`Madbox.MainMenu.Runtime`, tests `Madbox.MainMenu.Tests`).
- Depends on: `Scaffold.MVVM.*`, `Madbox.Gold`, `Madbox.Gameplay` (`IGameFlowService`), `Madbox.Level` (`ILevelMenuService`), `Scaffold.Navigation`, VContainer.
- Used by: `BootstrapScope` opens after startup; `LevelButtonCollectionHandlerBehaviour` forwards clicks.
- Runtime/Editor: runtime UI.

Keywords: main menu, gold, levels, navigation

## Responsibilities

- Owns: `MainMenuViewModel` / `MainMenuView`, gold display and +1 sample, level list from `ILevelService.GetAvailableLevels()`, `PlayLevel` delegation to `IGameFlowService`.
- Does not own: Battle, scene flow, or economy rules beyond calling `IGoldService`.
- Boundaries: Menu read model; progression completion APIs live on `LevelService` (`CompleteLevelAsync`).

## Public API

| Symbol | Purpose | Inputs | Outputs | Failure behavior |
|---|---|---|---|---|
| `MainMenuViewModel.AddOneGold()` | Sample gold increment via `IGoldService`. | None | Wallet update | Service-level errors propagate. |
| `MainMenuViewModel.PlayLevel(AvailableLevel)` | Opens game flow via `IGameFlowService`. | Level selection | Navigation | Fails if flow service cannot open `GameViewModel`. |
| `MainMenuViewModel.AvailableLevels` | Binds UI from `ILevelService`. | None | Read-only list | Empty if LiveOps/Addressables not ready. |
| `MainMenuView` | TMP binding, `LevelButtonCollectionHandlerBehaviour` for rows. | Serialized refs | UI | Missing prefab refs fail at runtime. |

## Setup / Integration

1. Ensure `Navigation Settings` maps Main Menu view config; `Game` view maps `GameView` / `GameViewModel`.
2. Register menu services via bootstrap/meta installers (`Gold`, `Level`, `Gameplay`).
3. Preload Addressables for menu + level definitions per `Docs/Assets/Addressables.md`.
4. Default TMP font: `LiberationSans SDF` (`Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset`).

## How to Use

1. Open menu from `BootstrapScope.OnBootstrapCompleted` (already wired).
2. Bind gold label to wallet; wire Add Gold to `AddOneGold`.
3. Build level buttons from `AvailableLevels`; on click call `PlayLevel`.
4. For progression after a run, call `LevelService.CompleteLevelAsync` from appropriate game-over flow (not MainMenu-owned in minimal sample).

## Examples

### Minimal

```csharp
mainMenuViewModel.PlayLevel(selectedAvailableLevel);
```

### Realistic

```
Prefab: Assets/Prefabs/MainMenu/Main Menu View.prefab
Level rows: Main Menu Level List Item.prefab → LevelButtonCollectionHandlerBehaviour
```

### Guard / Error path

```csharp
// AvailableLevels empty: check LiveOps init + MadboxLevels label on LevelDefinition assets
```

## Best Practices

- Keep menu free of battle/scene load code — delegate to `IGameFlowService`.
- Use `ILevelMenuService` as read model; keep completion calls in gameplay/game-over paths.

## Anti-Patterns

- Calling `ISceneFlowService` directly from the menu — breaks navigation abstraction.
- Hard-coding level ids instead of `AvailableLevel` entries.

## Testing

- Test assembly: `Madbox.MainMenu.Tests` (EditMode).
- Run:

```powershell
& ".\.agents\scripts\run-editmode-tests.ps1" -AssemblyNames "Madbox.MainMenu.Tests"
```

- Expected: all tests pass, zero failures.
- Bugfix rule: add/update regression test first.

## AI Agent Context

- Invariants: `PlayLevel` always goes through `IGameFlowService`; level list originates from `LevelService` merge.
- Allowed Dependencies: MVVM stack, Gold, Gameplay, Level, Navigation.
- Forbidden Dependencies: Battle internals, direct Addressables in view code.
- Change Checklist: run `Madbox.MainMenu.Tests`; sync `Docs/App/Gameplay.md` if flow changes.
- Known Tricky Areas: `AvailableLevel` empty during early bootstrap — guard UI.

## Related

- `Docs/App/Gameplay.md`
- `Docs/Meta/LiveOpsLevel.md`
- `Docs/Meta/Gold.md`
- `Architecture.md`

## Changelog

- 2025-03-23: Restructured to module documentation standard.
