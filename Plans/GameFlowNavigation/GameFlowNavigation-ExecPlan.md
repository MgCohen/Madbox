# Main menu to gameplay wiring (navigation + battle bootstrap)

This ExecPlan is a living document. The sections `Progress`, `Surprises & Discoveries`, `Decision Log`, and `Outcomes & Retrospective` must be kept up to date as work proceeds.

If the file `PLANS.md` is present at the repository root, maintain this document in accordance with `PLANS.md`.

## Purpose / Big Picture

After this work, a person running the game in the Editor (or a built player) can open the main menu, click a level in the level list, and land on a dedicated gameplay screen while the selected level’s Addressable scene loads additively, battle domain state starts, enemies spawn from the level definition, and the player prefab from the existing loadout path appears in the world. Today the level buttons only log the definition name (`Debug.Log` in `LevelButtonCollectionHandlerBehaviour`), `BattleBootstrap` and its dependencies are not registered in the app container, and there is no navigation target for an in-game screen—so none of that end-to-end behavior exists yet.

## Progress

- [x] (2026-03-22) Fix level list button wiring: `Add` calls `WireLevelClick`; `Main Menu View` prefab references correct level button prefab GUID and `LevelButtonCollectionHandlerBehaviour` on the list root.
- [x] (2026-03-22) Added `Madbox.Gameplay` with `GameViewModel`, `GameView`, `GameSessionCoordinator`, `IGameFlowService`; Navigation `Game.asset` + `Game View.prefab`; Preload Addressables entry `GameView`.
- [x] (2026-03-22) `MainMenuViewModel` injects `IGameFlowService` and exposes `PlayLevel`; `MainMenuView` registers handler on `LevelButtonCollectionHandlerBehaviour`.
- [x] (2026-03-22) `BattleGameplayInstaller` registers transient `EnemyService`/`EnemyFactory`, singleton rule registry + `BattleGameFactory`, `GameSessionCoordinator`, bridges (`PlayerSpawnBridge`, `BootstrapMainMenuLauncher`).
- [x] (2026-03-22) Session uses `ISceneFlowService.LoadAdditiveAsync` + `Arena.TryFindInScene`; `BattleGameFactory.CreatePrepareStartAsync` prepares/spawns/starts without scene load.
- [x] (2026-03-22) `SceneFlowLoadOptions.Default` used (bootstrap shell managed during additive load).
- [x] (2026-03-22) Tests: `MainMenuViewModelTests` (`PlayLevel`, level button click); `BattleGameFactorySessionTests`; validate gate pending in this environment.
- [x] (2026-03-22) Docs: `Docs/App/Gameplay.md`, `Docs/App/MainMenu.md`, `Architecture.md` module map.

## Surprises & Discoveries

- Observation: `Main Menu View` prefab pointed `levelButtonPrefab` at a non-existent GUID; fixed to `Main Menu Level Button` (`8dad97f3e27aa5a4fad13045ed1d7502`) and assigned `LevelButtonCollectionHandlerBehaviour` on `Level List`.
  Evidence: prefab YAML before change.

- Observation: `Madbox.MainMenu` cannot reference `Madbox.Bootstrap` (cycle); `IGameFlowService` + `IMainMenuLauncher` + `IPlayerSpawnService` keep Gameplay free of Bootstrap while composition stays in bootstrap installers.
  Evidence: assembly graph.

## Decision Log

- Decision: Treat “passing the selected level” as carrying the authored `Madbox.Levels.LevelDefinition` reference from `AvailableLevel.Definition`, not a LiveOps row id alone.
  Rationale: `BattleBootstrap.StartBattleAsync` already takes `LevelDefinition` and reads `SceneAssetReference` and spawn entries from it.
  Date/Author: 2026-03-22 / planning agent

- Decision: Keep core simulation types (`BattleGame`, `EnemyService`) in Core assemblies; perform Unity/async orchestration from an App-layer `GameViewModel` (or a thin App service it calls), not from `Madbox.Battle` itself.
  Rationale: Preserves “no Unity presentation inside pure domain” and matches `Architecture.md` container layering.
  Date/Author: 2026-03-22 / planning agent

- Decision: Require **additive** scene load for levels so `Bootstrap` and Navigation remain loaded.
  Rationale: `BattleBootstrap.StartBattleAsync` defaults to `LoadSceneMode.Single`, which would tear down the bootstrap scene and break UI/navigation; `Docs/Infra/SceneFlow.md` already describes the intended additive pattern.
  Date/Author: 2026-03-22 / planning agent

## Outcomes & Retrospective

- End-to-end wiring: main menu level click → `IGameFlowService` → `GameViewModel` + additive scene → `BattleGame` + player spawn via `PlayerSpawnBridge`. Back button tears down additive scene and reopens main menu through `IMainMenuLauncher`.
- `BattleBootstrap` remains available for tests or tools that still want single-call scene+battle; product flow uses SceneFlow + `CreatePrepareStartAsync` to avoid unloading bootstrap.

## Context and Orientation

**Navigation and MVVM.** The app opens `MainMenuViewModel` from `BootstrapScope` with `navigation.Open(mainMenu)`. Before `Bind` runs on the view model, `NavigationInjection` (`Assets/Scripts/Infra/Navigation/Container/NavigationInjection.cs`) calls VContainer `Inject` on the controller, which satisfies `[Inject]` fields such as `IGoldService` and `ILevelService` on `MainMenuViewModel`. After injection, `NavigationController` calls `viewModel.Bind(this)`, which clears prior bindings and runs `Initialize()` on `Scaffold.MVVM.ViewModel` (`Assets/Scripts/Core/ViewModel/Runtime/ViewModel.cs`). Screen controllers keep a protected `navigation` reference and may call `navigation.Open(...)`, `navigation.Return()`, or `Close()` on themselves.

**Main menu level list.** `MainMenuView` binds `viewModel.AvailableLevels` to `LevelButtonCollectionHandlerBehaviour` (`Assets/Scripts/App/MainMenu/Runtime/MainMenuView.cs`). That handler instantiates `MainMenuLevelListItem` per `AvailableLevel` and sets the label (`Assets/Scripts/App/MainMenu/Runtime/LevelButtonCollectionHandlerBehaviour.cs`). There is a private `WireLevelClick` helper that should register `Button.onClick`, but nothing calls it from `Add` today—only `Debug.Log` runs from `HandleLevelClicked`, and only if something else wired the button.

**Levels and availability.** `ILevelService.GetAvailableLevels()` returns `AvailableLevel` entries combining LiveOps availability with `LevelDefinition` assets (`Assets/Scripts/Meta/Levels/Runtime/`). The gameplay flow should use `entry.Definition` for battle bootstrap.

**Battle bootstrap.** `Madbox.Battle.BattleBootstrap.StartBattleAsync` (`Assets/Scripts/Core/Battle/Runtime/BattleBootstrap.cs`) loads the level scene via Addressables, constructs `BattleGame` through `BattleGameFactory`, awaits `PrepareAndSpawnEnemiesFromLevelAsync` (enemy prefab loads and spawns), then calls `game.Start()`. It does **not** register services in VContainer today; tests construct `EnemyService`, `EnemyFactory`, and `RuleHandlerRegistry` manually (`Assets/Scripts/Core/Battle/Tests/BattleBootstrapTests.cs`).

**Player spawn.** `PlayerFactory` in `Madbox.Bootstrap.Runtime` (`Assets/Scripts/App/Bootstrap/Runtime/Player/PlayerFactory.cs`) loads the player prefab and weapons from `PlayerLoadoutDefinition` using `IAddressablesGateway`. It is already registered via `PlayerInstaller` inside `BootstrapCoreInstaller`.

**Scene flow.** `ISceneFlowService` (`Madbox.SceneFlow`) supports additive load/unload with optional bootstrap shell toggling. `BootstrapScope` already references `SceneFlowBootstrapShell` for the installer path (`Assets/Scripts/App/Bootstrap/Runtime/BootstrapScope.cs`). The gameplay orchestration should align with `Docs/Infra/SceneFlow.md` rather than ad-hoc `Addressables.LoadSceneAsync` duplicates, except where `BattleBootstrap` remains the single owner temporarily—if so, still use additive mode and document the follow-up to consolidate.

**Terminology.** “GameViewModel” in this plan means the **navigation controller** for the gameplay **screen** (MVVM `ViewModel` subclass), not the `Madbox.GameView` assembly, which today holds Unity presentation helpers (animation, joystick, weapons). Naming the new types `GameViewModel` / `GameView` is acceptable if namespaces distinguish `Madbox.App.Gameplay` (example) from `Madbox.App.GameView.*`.

## Plan of Work

First, fix the level list so clicks actually reach `HandleLevelClicked`: from `LevelButtonCollectionHandlerBehaviour.Add`, after instantiating the row, call the existing `WireLevelClick` with the item’s `Button` and the source `AvailableLevel`. Verify in Play Mode that a click logs once per button and does not leak listeners across rebinding.

Second, add a gameplay screen module following `.agents/workflows/create-module.md`: a `GameViewModel` that accepts the selected `LevelDefinition` (constructor argument or settable property initialized before `navigation.Open`), and a `GameView` that subclasses `UIView<GameViewModel>` and exposes a `MonoBehaviour` coroutine host for async work if the view model cannot be async safely. Register the pair in `Navigation Settings` with a new ViewConfig and Addressable prefab, mirroring how the main menu prefab is registered.

Third, inject `INavigation` into `MainMenuViewModel` and add a method such as `PlayLevel(AvailableLevel entry)` that validates `entry?.Definition`, constructs `GameViewModel` with that definition, and calls `navigation.Open(gameViewModel, closeCurrent: true)` (or `false` if product intent is to stack; default recommendation is `true` for a full-screen game). Keep gold and level list initialization unchanged.

Fourth, register battle dependencies in an installer under `Assets/Scripts/App/Bootstrap/Runtime/` (for example `BattleInstaller` composed from `BootstrapCoreInstaller` or an adjacent layer) so `GameViewModel` can receive `BattleBootstrap`, and register `EnemyFactory` + `EnemyService` + `RuleHandlerRegistry` + `BattleGameFactory` with appropriate lifetimes. If `EnemyService` is singleton-scoped for the whole app, add an explicit reset or new instance per session; otherwise prefer `Lifetime.Transient` for session-scoped services.

Fifth, implement session startup in `GameViewModel` (or a dedicated `IGameSessionController` service injected into it): call `BattleBootstrap.StartBattleAsync` with `loadSceneMode: LoadSceneMode.Additive`, choose spawn origins (constants or `Transform` references resolved after additive load), await enemy preparation, then call `PlayerFactory.CreateReadyPlayerAsync` with a parent under the loaded scene or world root. Store `BattleBootstrapResult` (game + scene handle) for later unload. Coordinate `ISceneFlowService` if the project standard is to route all additive loads through it; if `BattleBootstrap` is kept, still pass additive mode and ensure shell toggling matches `SceneFlowBootstrapShell` behavior.

Sixth, add tests and documentation. EditMode-test `MainMenuViewModel` with a fake `INavigation` that records the opened controller type and captured `LevelDefinition`. Add tests for the collection handler wiring if practical (EditMode instantiating the handler and asserting listener count). Run `.agents/scripts/validate-changes.cmd` from the repository root per `AGENTS.md` and fix analyzer issues.

## Concrete Steps

All commands assume the working directory is the repository root (`/workspace` or your local clone).

- After implementing C# changes, rebuild analyzer projects if touched: `dotnet build -c Release` in `Analyzers/Scaffold/Scaffold.Analyzers` when analyzers change (often not needed for this feature).

- Run EditMode tests for affected assemblies, for example:

    powershell -NoProfile -ExecutionPolicy Bypass -File ".\.agents\scripts\run-editmode-tests.ps1" -AssemblyNames "Madbox.MainMenu.Tests"

- Run the full milestone gate:

    .agents/scripts/validate-changes.cmd

  Expect: tests and analyzer checks pass with no new errors. On Linux cloud agents, invoke through PowerShell as documented in `AGENTS.md` / `Docs/Testing.md` if the `.cmd` wrapper is not executable.

- Manual sanity (Editor): Play from bootstrap, click a level, observe additive scene active, player and enemies present, `BattleGame.IsRunning` true until win/lose rules fire.

## Validation and Acceptance

Acceptance is behavioral. With a valid Addressable level definition and prefabs configured, clicking an unlocked level in the main menu opens the gameplay screen, loads the level scene **additively** without losing the bootstrap shell, starts `BattleGame`, spawns at least one enemy when the level definition lists spawn entries, and spawns the player from the loadout. Returning to the menu (if implemented in the same milestone) should unload additive content and release handles without errors; if return is deferred, document that limitation in `Progress` and `Outcomes`.

Automated proof should include a test that fails before wiring the level button click and passes after, and a test that `MainMenuViewModel` requests navigation with the correct `LevelDefinition` reference when `PlayLevel` is invoked.

## Idempotence and Recovery

Re-entering gameplay from the menu should not duplicate enemy registries or scene handles. If a session fails mid-load, catch exceptions, log, and avoid leaving partial instances; prefer `try/finally` around player instantiation (mirroring `PlayerFactory` cleanup). Scene unload should use the same API family as load (SceneFlow or Addressables) to avoid refcount leaks.

## Artifacts and Notes

Indicative files to touch or add (exact names may vary by implementation choice):

- `Assets/Scripts/App/MainMenu/Runtime/LevelButtonCollectionHandlerBehaviour.cs` — call `WireLevelClick` from `Add`.
- `Assets/Scripts/App/MainMenu/Runtime/MainMenuViewModel.cs` — inject `INavigation`, add `PlayLevel`.
- New gameplay module under `Assets/Scripts/App/...` — `GameViewModel`, `GameView`, `.asmdef`, tests, `Docs/App/...`.
- `Assets/Scripts/App/Bootstrap/Runtime/Layers/BootstrapCoreInstaller.cs` (or sibling installer) — register battle services.
- `Assets/Data/Navigation/*` — new ViewConfig mapping for `GameViewModel` / `GameView`.
- Optional: `Assets/Scripts/Core/Battle/Runtime/BattleBootstrap.cs` — only if additive default or SceneFlow integration is centralized here.

## Interfaces and Dependencies

At completion, the following should exist and compose:

- `MainMenuViewModel` with `[Inject] INavigation navigation` and a public method accepting `AvailableLevel` or `LevelDefinition` that calls `navigation.Open(new GameViewModel(definition), closeCurrent: true)` (exact ctor shape is an implementation detail as long as the definition is available before `Bind`).

- `GameViewModel : ViewModel` with access to `BattleBootstrap`, `PlayerFactory`, and any scene-flow service the project chooses, plus the selected `LevelDefinition` for the current session.

- VContainer registrations sufficient to construct the above without manual `new` for services (controllers may still be `new` at the call site like `MainMenuViewModel` today).

- `LevelButtonCollectionHandlerBehaviour` that wires `MainMenuLevelListItem.Button` clicks to `MainMenuViewModel` (via a view callback or direct view model reference—prefer routing through `MainMenuView` → `viewModel.PlayLevel` to keep Unity event wiring in the view).

Plan revision note: 2026-03-22 — Initial ExecPlan authored for end-to-end main menu → gameplay wiring with additive scenes, DI registration, and test expectations.

Plan revision note: 2026-03-22 — Execution pass: implemented `Madbox.Gameplay`, bootstrap `BattleGameplayInstaller`, prefab/navigation/addressables fixes, `BattleGameFactory.CreatePrepareStartAsync`, tests and docs.
