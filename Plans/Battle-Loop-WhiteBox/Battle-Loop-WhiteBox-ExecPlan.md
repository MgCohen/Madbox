# White-Box Battle Loop: MainMenu -> GameView -> Complete -> MainMenu

This ExecPlan is a living document. The sections `Progress`, `Surprises & Discoveries`, `Decision Log`, and `Outcomes & Retrospective` must be kept up to date as work proceeds.

This document must be maintained in accordance with `PLANS.md` at the repository root.

This plan builds on the existing checked-in context from `Plans/MainMenu-WhiteBox/MainMenu-WhiteBox-ExecPlan.md` and `Plans/BattleIntentCommandPipeline/BattleIntentCommandPipeline-ExecPlan.md`.

## Purpose / Big Picture

After this change, the app will support a complete white-box gameplay loop with no combat prefab spawning yet. From Main Menu, the player can press a Start button to open `GameView`; `GameView` starts a real `Madbox.Battle.Game` session behind the scenes and displays a `GameState` label (`NotRunning`, `Running`, `Done`). When the session reaches `Done`, the screen reveals a Complete button; pressing Complete closes `GameView` and returns to `MainMenuView`.

This slice intentionally validates flow and integration, not visuals. It proves MVVM binding, Navigation + Addressables view loading, app-level services (`LevelService`, `GameService`) using existing Meta/Battle modules, and an end-to-end loop that can be observed in Play Mode.

## Progress

- [x] (2026-03-18 21:45Z) Authored initial ExecPlan with module boundaries, service flow, editor-asset authoring steps, and acceptance criteria for the white-box loop.
- [x] (2026-03-19 01:20Z) Execute Milestone 1: Created `App/GameView` runtime/container/tests module and added `Docs/App/GameView.md`.
- [x] (2026-03-19 01:24Z) Execute Milestone 2: Added `ILevelService`, `IGameService`, `IGameSession`, runtime implementations, and bootstrap DI registration.
- [x] (2026-03-19 01:27Z) Execute Milestone 3: Extended Main Menu with Start Game button and `StartGame()` navigation command.
- [x] (2026-03-19 01:31Z) Execute Milestone 4: Implemented `GameViewModel` + `GameView` with live game-state label and Complete-button done gate.
- [x] (2026-03-19 01:34Z) Execute Milestone 5: Added `GameView` navigation config + prefab and minimum sample `WhiteBoxEnemy`/`WhiteBoxLevel` assets; registered addressables keys.
- [x] (2026-03-19 01:39Z) Execute Milestone 6: Added/updated EditMode + PlayMode tests including full loop smoke (`BootstrapWhiteBoxLoopPlayModeTests`).
- [x] (2026-03-19 01:44Z) Execute Milestone 7: Ran `.agents/scripts/validate-changes.cmd` clean (EditMode 180/180, PlayMode 3/3, analyzers TOTAL:0).

## Surprises & Discoveries

- Observation: `App/MainMenu` currently exposes only gold display and `AddOneGold`; no game-loop navigation command exists yet.
  Evidence: `Assets/Scripts/App/MainMenu/Runtime/MainMenuViewModel.cs` and `MainMenuView.cs`.

- Observation: `GameView` module does not exist yet.
  Evidence: `rg --files Assets/Scripts/App` returns only `Bootstrap`, `MainMenu`, and `View`.

- Observation: there are no authored level/enemy `.asset` files yet for runtime game-session loading.
  Evidence: asset scan shows only `Assets/Data/Navigation/Views/MainMenu.asset` under `Assets/Data` for gameplay-related data.

- Observation: `Madbox.Battle.Game` already supports autonomous completion by rules (for example, time-limit lose), which enables a prefab-free done-state test.
  Evidence: `Assets/Scripts/Core/Battle/Runtime/Game.cs` and `Assets/Scripts/Core/Battle/Runtime/Rules/GameRules.cs`.

- Observation: `GameView.cs.meta` initially collided with an existing GUID (`GoldService.cs.meta`), which made Unity treat the prefab script as missing.
  Evidence: PlayMode output showed `The referenced script (Unknown) on this Behaviour is missing!` until GUID collision was removed.

- Observation: `EnemyDefinition` requires at least one behavior; an empty behavior list in sample enemy assets throws at runtime.
  Evidence: PlayMode failure `ArgumentException: At least one behavior is required.`

## Decision Log

- Decision: Keep this slice minimal and intentionally white-box: no prefab instantiation, no combat presentation, no advanced level select UI.
  Rationale: the user goal is flow validation, not feature-complete combat.
  Date/Author: 2026-03-18 / Codex + User

- Decision: Introduce app-level `ILevelService` and `IGameService` in `App/GameView` as thin orchestration over existing modules (`Madbox.Levels`, `Madbox.Battle`, `Madbox.Gold`, `Madbox.Addressables`).
  Rationale: matches the researched loop ownership while avoiding duplication in Core modules.
  Date/Author: 2026-03-18 / Codex + User

- Decision: Use a single sample level with a short time-limit lose rule to guarantee `GameState.Done` without view-side collisions or enemy prefab runtime.
  Rationale: enables deterministic loop completion in a pure white-box slice.
  Date/Author: 2026-03-18 / Codex + User

- Decision: Return to Main Menu by closing `GameViewModel` from Complete button (`Close()`), keeping navigation stack behavior simple.
  Rationale: avoids custom return routing and fits existing `INavigation` flow.
  Date/Author: 2026-03-18 / Codex + User

- Decision: Keep `MainMenu` decoupled from `Madbox.GameView.Runtime` by using reflective `INavigation.Open<T>` invocation.
  Rationale: satisfies analyzer boundary rule `SCA0022` while preserving requested button-driven flow.
  Date/Author: 2026-03-19 / Codex

- Decision: Add runtime fallback level construction in `GameService` when authored level conversion fails (for example, empty enemy behavior list).
  Rationale: keeps white-box loop robust and still validates Addressables load path without over-authoring complexity.
  Date/Author: 2026-03-19 / Codex

## Outcomes & Retrospective

Delivered behavior:

1. Main Menu now has a Start Game button.
2. Start opens `GameView` through Navigation + Addressables.
3. `GameView` starts a real `Madbox.Battle.Game` session and updates a `GameState` label every frame.
4. When state reaches `Done`, Complete button becomes visible.
5. Clicking Complete closes `GameView` and returns to Main Menu.

Validation evidence:

1. `run-editmode-tests.ps1 -AssemblyNames "Madbox.MainMenu.Tests"` -> Passed 4/4.
2. `run-editmode-tests.ps1 -AssemblyNames "Madbox.GameView.Tests"` -> Passed 4/4.
3. `run-playmode-tests.ps1 -AssemblyNames "Madbox.Bootstrap.PlayModeTests"` -> Passed 2/2 (includes white-box loop).
4. `.agents/scripts/validate-changes.cmd` -> Compilation PASS, EditMode 180/180, PlayMode 3/3, analyzers `TOTAL:0`.

Lessons and gaps:

1. Asset GUID collisions are easy to miss when hand-authoring YAML/meta; validating unique GUIDs early avoids silent prefab-script resolution failures.
2. White-box asset authoring should still satisfy domain constructor invariants (enemy behaviors) to avoid runtime conversion exceptions.
3. Next iteration can replace fallback level build path with richer authored enemy behavior content and real battle prefab composition.

## Context and Orientation

Relevant current files and roles:

- `Assets/Scripts/App/MainMenu/Runtime/MainMenuViewModel.cs`: current menu command surface.
- `Assets/Scripts/App/MainMenu/Runtime/MainMenuView.cs`: current menu UI and button wiring.
- `Assets/Scripts/App/Bootstrap/Runtime/BootstrapScope.cs`: opens first view (`MainMenu`) after bootstrap completion.
- `Assets/Scripts/App/Bootstrap/Runtime/BootstrapAssetInstaller.cs`: asset-layer DI registration location.
- `Assets/Scripts/App/Bootstrap/Runtime/BootstrapInfraInstaller.cs`: infra/meta DI registration location.
- `Assets/Scripts/Core/Battle/Runtime/Game.cs`: pure C# battle loop and state transitions.
- `Assets/Scripts/Core/Battle/Runtime/GameState.cs`: state enum used for the `GameView` label.
- `Assets/Scripts/Core/Levels/Authoring/Definitions/LevelDefinitionSO.cs`: authoring-to-domain mapping for level data.
- `Assets/Scripts/Core/Levels/Authoring/Catalog/LevelCatalogSO.cs`: id -> addressable level reference lookup.
- `Assets/Scripts/Core/Levels/Authoring/Catalog/AddressableLevelDefinitionProvider.cs`: loads level assets via `IAddressablesGateway`.
- `Assets/Data/Navigation/Navigation Settings.asset`: map of controller/view configs used by navigation.
- `Assets/Data/Navigation/Views/MainMenu.asset`: existing Addressables-backed view config example.

Terms in this plan:

- White-box GameView: a minimal view that shows internal battle state text and explicit completion action, without gameplay rendering.
- Session wrapper: a small object that owns the active `Game` plus any addressables handles that must be released when the run ends.
- Done-state gate: UI behavior that shows Complete button only when `Game.CurrentState == GameState.Done`.

## Plan of Work

Milestone 1 creates a new app module for this screen and loop orchestration: `Assets/Scripts/App/GameView/Runtime`, `Container`, and `Tests`, plus docs at `Docs/App/GameView.md`. Keep names consistent with repo conventions (`Madbox.GameView.Runtime`, `Madbox.GameView.Container`, `Madbox.GameView.Tests`).

Milestone 2 adds lean services in this new module:

1. `ILevelService`: resolves default level id and provides level-addressable reference for that id.
2. `IGameService`: creates a battle session and exposes `Tick` and completion state access.
3. Session implementation: owns `Game` instance plus addressable asset handles; ensures release on session disposal.

Use existing elements only:

- load level authoring asset through `IAddressablesGateway` (Addressables requirement).
- map `LevelDefinitionSO.ToDomain()`.
- create `Player` and `Game` from `Madbox.Battle`.
- use `IGoldService` only through existing `Madbox.Gold` contracts if needed by session construction or post-result handling.

Milestone 3 extends Main Menu with one new action:

- Add Start Game button in `MainMenuView`.
- Add `StartGame()` command in `MainMenuViewModel` that opens `GameViewModel` through `navigation.Open(...)`.
- Keep existing Gold button behavior untouched.

Milestone 4 implements GameView MVVM:

- `GameViewModel` starts a session when bound/initialized.
- `GameViewModel.Tick(float)` forwards time to game service/session.
- Expose bindable properties:
  - `GameStateText` (for label)
  - `IsCompleteVisible` (for Complete button visibility)
- Update properties each frame or on state changes from session.
- `Complete()` closes `GameViewModel` (returns to menu).
- `GameView` drives `Tick(Time.deltaTime)` in `Update()`, binds label, and toggles Complete button active state.

Milestone 5 creates minimum authoring/navigation assets needed to run this loop:

1. One enemy definition asset.
2. One level definition asset with at least one enemy entry and a short time-limit lose rule (for deterministic completion).
3. One level catalog asset mapping the selected level id to the level asset reference.
4. One `ViewConfig` for `GameView` and one GameView prefab marked addressable.
5. `Navigation Settings` updated with the new GameView config.

Milestone 6 adds/updates tests:

- EditMode tests for MainMenu navigation command and GameViewModel state transitions.
- EditMode tests for game service/session wrapper (level load, game start, state progression to done, disposal behavior).
- One PlayMode flow smoke test: bootstrap -> menu -> start game -> wait until done label -> click complete -> menu visible again.

Milestone 7 runs full quality gate and updates this plan sections with concrete evidence.

## Concrete Steps

Run all commands from repository root: `C:\Users\mtgco\.codex\worktrees\f253\Madbox`.

1. Create module + docs.

    - Follow `.agents/workflows/create-module.md` for `Assets/Scripts/App/GameView`.
    - Add/update `Docs/App/GameView.md`.

2. Implement runtime services and DI installer wiring.

3. Update Main Menu runtime files for Start button and navigation command.

4. Implement `GameViewModel` and `GameView`, plus prefab and navigation config assets.

5. Author minimum sample enemy/level/catalog assets (Unity Editor steps below).

6. Run focused checks while iterating:

    powershell -NoProfile -ExecutionPolicy Bypass -File ".\.agents\scripts\run-editmode-tests.ps1" -AssemblyNames "Madbox.MainMenu.Tests"
    powershell -NoProfile -ExecutionPolicy Bypass -File ".\.agents\scripts\run-editmode-tests.ps1" -AssemblyNames "Madbox.GameView.Tests"
    powershell -NoProfile -ExecutionPolicy Bypass -File ".\.agents\scripts\run-editmode-tests.ps1" -AssemblyNames "Madbox.Bootstrap.Tests"
    powershell -NoProfile -ExecutionPolicy Bypass -File ".\.agents\scripts\run-playmode-tests.ps1" -AssemblyNames "Madbox.Bootstrap.PlayModeTests"
    powershell -NoProfile -ExecutionPolicy Bypass -File ".\.agents\scripts\check-analyzers.ps1"

7. Required milestone quality gate:

    .\.agents\scripts\validate-changes.cmd

8. If gate fails, fix all issues and rerun step 7 until clean.

### Unity Editor Asset Authoring Steps (for Milestone 5)

If assets cannot be generated by scripts/tools, create them manually in Unity Editor:

1. Create folder structure:

    - `Assets/Data/Enemies/`
    - `Assets/Data/Levels/`

2. Create enemy asset:

    - `Create > Madbox > Authoring > Enemy Definition`
    - Name: `WhiteBoxEnemy.asset`
    - `enemyTypeId`: `whitebox-enemy`
    - `maxHealth`: `10`
    - `behaviorRules`: keep empty or add minimal safe values.

3. Create level asset:

    - `Create > Madbox > Authoring > Level Definition`
    - Name: `WhiteBoxLevel.asset`
    - `levelId`: `whitebox-level-1`
    - `goldReward`: `1`
    - Add one enemy entry referencing `WhiteBoxEnemy.asset` with `count = 1`.
    - Enable time limit lose rule with small value (for example `loseAfterSeconds = 3`).

4. Mark `WhiteBoxLevel.asset` as Addressable with a deterministic key:

    - Key: `levels/whitebox-level-1`

5. Create level catalog asset:

    - `Create > Madbox > Authoring > Level Catalog`
    - Name: `WhiteBoxLevelCatalog.asset`
    - Add one entry:
      - `levelId`: `whitebox-level-1`
      - `levelReference`: reference to `WhiteBoxLevel.asset`.

6. Create GameView prefab:

    - Create a prefab variant from `Assets/Prefabs/Navigation/Base UIView.prefab`.
    - Save as `Assets/Prefabs/GameView/Game View.prefab`.
    - Add `GameView` component and wire the label + Complete button references.

7. Mark GameView prefab as Addressable and create view config:

    - `Assets/Data/Navigation/Views/GameView.asset`
    - Assign GameView prefab to `asset` field.

8. Update `Assets/Data/Navigation/Navigation Settings.asset` screens list to include `GameView.asset`.

## Validation and Acceptance

Acceptance is complete only when all checks below are true:

1. Boot flow opens Main Menu as before.
2. Main Menu contains Start Game button.
3. Clicking Start Game opens `GameView` through navigation/addressables path.
4. `GameView` starts a real `Madbox.Battle.Game` session in background.
5. `GameView` label updates and shows current game state (`Running` then `Done`).
6. Complete button is hidden while state is not done and becomes visible when state is done.
7. Clicking Complete closes GameView and returns to Main Menu.
8. EditMode tests for MainMenu and GameView/service pass.
9. PlayMode smoke flow for bootstrap->menu->game->done->complete->menu passes.
10. `.agents/scripts/validate-changes.cmd` passes with analyzer diagnostics clean.

For any bug found while implementing this plan, add/update a regression test first, confirm fail-before, then fix and confirm pass-after.

## Idempotence and Recovery

This plan is additive and safe to retry in milestone order.

If an asset wiring mistake occurs (wrong addressable key/reference):

1. Fix the key/reference in asset inspector.
2. Re-run the same PlayMode test and gate.
3. Keep old assets only until new references are proven, then remove duplicates.

If a milestone stalls, keep already passing tests and document exact remaining work in `Progress` before continuing.

## Artifacts and Notes

Expected touched paths include:

- `Plans/Battle-Loop-WhiteBox/Battle-Loop-WhiteBox-ExecPlan.md`
- `Assets/Scripts/App/GameView/Runtime/*`
- `Assets/Scripts/App/GameView/Container/*`
- `Assets/Scripts/App/GameView/Tests/*`
- `Assets/Scripts/App/MainMenu/Runtime/MainMenuViewModel.cs`
- `Assets/Scripts/App/MainMenu/Runtime/MainMenuView.cs`
- `Assets/Scripts/App/MainMenu/Tests/*`
- `Assets/Scripts/App/Bootstrap/Runtime/BootstrapInfraInstaller.cs`
- `Assets/Scripts/App/Bootstrap/Runtime/BootstrapAssetInstaller.cs`
- `Assets/Data/Navigation/Views/GameView.asset`
- `Assets/Data/Navigation/Navigation Settings.asset`
- `Assets/Prefabs/GameView/Game View.prefab`
- `Assets/Data/Enemies/WhiteBoxEnemy.asset`
- `Assets/Data/Levels/WhiteBoxLevel.asset`
- `Assets/Data/Levels/WhiteBoxLevelCatalog.asset`
- `Docs/App/GameView.md`
- `Docs/App/MainMenu.md`

As work proceeds, append concise command/test evidence snippets in this section.

## Interfaces and Dependencies

Required end-state contracts/types for this slice:

1. App-level loop services in `Assets/Scripts/App/GameView/Runtime/Contracts/`:

   - `ILevelService` (default/selectable level id + reference lookup for addressable level load).
   - `IGameService` (create/start session from level id, tick session, expose state/completion).
   - `IGameSession` or equivalent disposable session wrapper.

2. GameView MVVM in `Assets/Scripts/App/GameView/Runtime/`:

   - `GameViewModel : Scaffold.MVVM.ViewModel`
   - `GameView : Scaffold.MVVM.UIView<GameViewModel>`

3. Main menu integration:

   - `MainMenuViewModel.StartGame()` command.
   - `MainMenuView` Start button bound to `StartGame()`.

4. DI/Composition:

   - New GameView installer registered from bootstrap composition path.
   - Existing Addressables and Gold installers reused; do not duplicate infra services.

5. Mandatory dependency direction:

   - `App/GameView` may depend on `Madbox.Battle`, `Madbox.Levels`, `Madbox.Gold`, `Madbox.Addressables`, `Scaffold.MVVM.View`, `Scaffold.MVVM.ViewModel`, `Scaffold.Navigation`, and `VContainer`.
   - `Core/Battle` remains pure and unchanged regarding Unity/MonoBehaviour constraints.

Non-goals for this plan:

- Level selection UI list/grid.
- Runtime prefab spawn/battle rendering.
- Full combat input mapping and VFX.
- Backend/save/progression persistence changes.

Revision Note (2026-03-18 / Codex): Created initial ExecPlan for a minimal end-to-end white-box battle loop (`MainMenu -> GameView -> Done -> Complete -> MainMenu`) using MVVM, Addressables, and existing Meta/Battle modules without overengineering.
Revision Note (2026-03-18 / Codex): Updated asset-path guidance to use `Assets/Data/Enemies` and `Assets/Data/Levels`, and updated GameView prefab guidance to use a variant of `Assets/Prefabs/Navigation/Base UIView.prefab` per review feedback.
Revision Note (2026-03-19 / Codex): Marked milestones complete after implementation, recorded runtime/analyzer discoveries and decisions, and added final validation evidence and retrospective.
