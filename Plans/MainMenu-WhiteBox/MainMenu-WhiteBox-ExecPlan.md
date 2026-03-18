# Build a White-Box Main Menu Slice with MVVM + Navigation + Addressables + DI

This ExecPlan is a living document. The sections `Progress`, `Surprises & Discoveries`, `Decision Log`, and `Outcomes & Retrospective` must be kept up to date as work proceeds.

This document must be maintained in accordance with `PLANS.md` at the repository root.

## Purpose / Big Picture

After this change, the Bootstrap scene will open a very small Main Menu screen through Navigation using an Addressables-backed view prefab. The screen will show current gold and include one button to add `+1` gold. The click flow will be end-to-end white-box and observable: View click -> ViewModel method -> gold service mutation -> observable model update -> bound UI text refresh.

This milestone is intentionally simple. It is a confidence slice to prove startup flow, Addressables-backed navigation view loading, DI injection into ViewModel flow, and MVVM bind propagation all work together in the real app path.

## Progress

- [x] (2026-03-18 20:10Z) Authored initial ExecPlan with module boundaries, implementation sequence, and acceptance criteria.
- [ ] Execute Milestone 1: Create `App/MainMenu` module and add module docs.
- [ ] Execute Milestone 2: Add Gold runtime service + DI container registration that exposes observable state to presentation.
- [ ] Execute Milestone 3: Implement Main Menu Model/ViewModel/View + prefab + navigation asset wiring + bootstrap open.
- [ ] Execute Milestone 4: Add EditMode and PlayMode regression coverage for bind propagation and bootstrap-main-menu behavior.
- [ ] Execute Milestone 5: Run `.agents/scripts/validate-changes.cmd` clean and finalize outcomes.

## Surprises & Discoveries

- Observation: `Assets/Scripts/App/MainMenu/` does not currently exist in this worktree, despite legacy references in older docs/baselines.
  Evidence: `rg --files Assets/Scripts/App` returns only `Bootstrap` and `View`.

- Observation: Gold currently exists as domain wallet only (`GoldWallet`), without a DI-facing service abstraction for presentation.
  Evidence: `Assets/Scripts/Meta/Gold/Runtime/GoldWallet.cs` exists; no `GoldService` or `IGoldService` exists.

- Observation: Bootstrap scope currently finishes initialization but does not open a first controller/view.
  Evidence: `Assets/Scripts/App/Bootstrap/Runtime/BootstrapScope.cs` logs completion and contains a `//open first view` comment.

## Decision Log

- Decision: Keep `GoldWallet` as pure domain state and add a separate observable presentation-facing model/service path.
  Rationale: This keeps domain invariants in `GoldWallet` while giving MVVM binding a property-changed source.
  Date/Author: 2026-03-18 / Codex + User

- Decision: Introduce `Meta/Gold` DI registration through a `Container` slice (`Madbox.Gold.Container`) and consume it from `BootstrapInfraInstaller`.
  Rationale: This validates injection wiring explicitly and avoids ad-hoc object creation in ViewModel.
  Date/Author: 2026-03-18 / Codex + User

- Decision: Use an Addressables-backed Main Menu prefab (non-context view) mapped by `ViewConfig` in `Navigation Settings`.
  Rationale: This directly validates the Addressables + Navigation integration path instead of bypassing it with a scene-resident context view.
  Date/Author: 2026-03-18 / Codex + User

- Decision: Start with one simple button action (`AddOneGold`) and one bound text value.
  Rationale: This keeps the slice minimal while still validating click input, DI service call, and bind propagation.
  Date/Author: 2026-03-18 / Codex + User

## Outcomes & Retrospective

Not completed yet. Fill this section after Milestone 5 with:
1. What was delivered and how it maps to Purpose.
2. Validation evidence (tests and manual run observations).
3. Remaining gaps and next-slice suggestions.

## Context and Orientation

This repository currently has the required infrastructure but not the concrete Main Menu feature module.

Relevant files and why they matter:

- `Assets/Scripts/App/Bootstrap/Runtime/BootstrapScope.cs`: bootstrap completion hook where first screen open should happen.
- `Assets/Scripts/App/Bootstrap/Runtime/BootstrapInfraInstaller.cs`: infra install chain where gold container install should be added.
- `Assets/Scripts/Infra/Navigation/Runtime/Implementation/NavigationSettings.cs`: maps controller type -> `ViewConfig`.
- `Assets/Scripts/Infra/Navigation/Runtime/Implementation/ViewConfig.cs`: links a controller to an Addressables prefab implementing `IView`.
- `Assets/Scripts/Core/ViewModel/Runtime/ViewModel.cs`: base ViewModel bind lifecycle (`Bind(INavigation)` + binding update propagation).
- `Assets/Scripts/App/View/Runtime/View.cs` and `Assets/Scripts/App/View/Runtime/ViewElement.cs`: typed view bind behavior and property-changed reaction.
- `Assets/Scripts/Meta/Gold/Runtime/GoldWallet.cs`: existing pure gold domain state.
- `Assets/Data/Navigation/Navigation Settings.asset`: screen map asset to extend with Main Menu config.
- `Assets/Data/Navigation/Template View Config.asset`: existing template for new `ViewConfig` asset authoring.

Plain-language definitions used in this plan:

- Addressables-backed view: a UI prefab loaded at runtime via Addressables key/reference, not pre-placed in scene.
- ViewModel binding: registering source and target properties so View updates when data changes.
- White-box validation: intentionally validating internals and wiring boundaries (DI, navigation path, bind propagation), not only visual outcome.

## Plan of Work

Milestone 1 creates the `App/MainMenu` module through the repository workflow (`.agents/workflows/create-module.md`) instead of manual copy-paste. Create `Runtime` and `Tests` at minimum and only optional folders that are genuinely needed. Add `Docs/App/MainMenu.md` in the same change so module documentation is present from day one.

Milestone 2 extends Gold with a DI-ready service that wraps wallet mutation and exposes observable state for MVVM. Keep `GoldWallet` as authoritative domain balance holder. Add a lightweight `Model` descendant in `Meta/Gold/Runtime` for observable current gold and a service interface under `Runtime/Contracts` (to satisfy boundary analyzer conventions) plus implementation that updates both wallet and model in one place. Add `Meta/Gold/Container` installer registration and wire that installer from `BootstrapInfraInstaller`.

Milestone 3 implements main menu presentation:

`MainMenuModel` in app module for screen-local observable state (if needed), `MainMenuViewModel` deriving from `Scaffold.MVVM.ViewModel`, and `MainMenuView` deriving from `Scaffold.MVVM.View<MainMenuViewModel>`. The ViewModel receives `IGoldService` via injection, binds exposed screen property/properties in `Initialize()`, and exposes one public command method (`AddOneGold`) called by the View button callback. The View binds ViewModel state to centered text and routes button click into `viewModel.AddOneGold()`. Create a Main Menu prefab with this view script and mark it Addressable; create/update a `ViewConfig` asset pointing to this prefab; register that config in `Assets/Data/Navigation/Navigation Settings.asset`; open Main Menu in `BootstrapScope.OnBootstrapCompleted(...)` through `INavigation`.

Milestone 4 adds tests before final gate. Add focused EditMode tests in `App/MainMenu/Tests` that prove:
1) ViewModel initial gold exposure matches service state.
2) Calling `AddOneGold` increments service/model state and observable bound property.
3) View bind updates text when ViewModel changes (regression for bind pipeline).

Add one PlayMode test (module-local or bootstrap tests) that loads `Bootstrap` scene, waits for bootstrap completion, verifies Main Menu is active, triggers the button (or ViewModel command via resolved controller), and asserts the displayed gold increases by one.

Milestone 5 runs the required full gate, fixes all analyzer/test issues, reruns gate until clean, and updates this ExecPlan living sections with exact evidence and final outcomes.

## Concrete Steps

Run all commands from repository root `C:\Unity\Madbox`.

1. Scaffold `App/MainMenu` module per workflow and add docs:

    - Follow `.agents/workflows/create-module.md` with module path `Assets/Scripts/App/MainMenu`.
    - Ensure required folders exist (`Runtime`, `Tests`).
    - Add asmdefs using repository naming convention (runtime/test, with `optionalUnityReferences: ["TestAssemblies"]` on tests).
    - Add `Docs/App/MainMenu.md`.

2. Implement Gold service + container + bootstrap wiring.

3. Implement Main Menu runtime classes, prefab, and navigation settings wiring.

4. Run targeted tests after each milestone:

    powershell -NoProfile -ExecutionPolicy Bypass -File ".\.agents\scripts\run-editmode-tests.ps1" -AssemblyNames "Madbox.MainMenu.Tests"
    powershell -NoProfile -ExecutionPolicy Bypass -File ".\.agents\scripts\run-editmode-tests.ps1" -AssemblyNames "Madbox.Gold.Tests"
    powershell -NoProfile -ExecutionPolicy Bypass -File ".\.agents\scripts\run-playmode-tests.ps1" -AssemblyNames "Madbox.Bootstrap.PlayModeTests"

5. Run analyzer diagnostics check while iterating:

    powershell -NoProfile -ExecutionPolicy Bypass -File ".\.agents\scripts\check-analyzers.ps1"

6. Required milestone quality gate at the end:

    .\.agents\scripts\validate-changes.cmd

7. If gate fails, fix all failures, rerun the same command until clean, then update this plan sections.

## Validation and Acceptance

Acceptance is complete only when all behaviors below are true:

1. Opening `Bootstrap` scene causes `BootstrapScope` to complete and then opens Main Menu through `INavigation`.
2. Main Menu view instance is loaded through Addressables-backed navigation config (non-context path).
3. Main Menu shows current gold value as text at first render.
4. Clicking the Main Menu add button calls View -> ViewModel -> Gold service and increments gold by exactly `+1`.
5. Updated gold value is reflected on screen through MVVM binding propagation, without manual UI refresh calls.
6. DI is exercised in runtime path (ViewModel gets dependencies via container injection when opened by navigation).
7. `Madbox.MainMenu.Tests`, `Madbox.Gold.Tests`, and bootstrap PlayMode coverage all pass.
8. `.agents/scripts/validate-changes.cmd` passes with analyzer diagnostics clean.

For any bug discovered while implementing this plan, add/update regression test first, verify it fails before fix and passes after fix.

## Idempotence and Recovery

This plan is additive and safe to rerun if interrupted. If a milestone partially fails:

1. Keep passing tests and docs changes.
2. Revert only incomplete pieces from the current milestone.
3. Re-run targeted tests before continuing.

Authoring assets (`ViewConfig`, prefab, navigation settings) should be updated in small commits to simplify rollback if an asset reference/guid mistake appears.

## Artifacts and Notes

Expected touched paths include:

- `Plans/MainMenu-WhiteBox/MainMenu-WhiteBox-ExecPlan.md`
- `Assets/Scripts/App/MainMenu/Runtime/*`
- `Assets/Scripts/App/MainMenu/Tests/*`
- `Assets/Scripts/Meta/Gold/Runtime/*` (service/model/contracts additions)
- `Assets/Scripts/Meta/Gold/Container/*` (installer + asmdef)
- `Assets/Scripts/App/Bootstrap/Runtime/BootstrapInfraInstaller.cs`
- `Assets/Scripts/App/Bootstrap/Runtime/BootstrapScope.cs`
- `Assets/Data/Navigation/Navigation Settings.asset`
- `Assets/Data/Navigation/*MainMenu*ViewConfig*.asset` (new or renamed from template)
- `Assets/Prefabs/Navigation/*MainMenu*.prefab`
- `Docs/App/MainMenu.md`

Keep evidence snippets in this section as implementation proceeds (gate summary lines, targeted test summaries, and any key runtime logs).

## Interfaces and Dependencies

Required end-state interfaces/types (names may vary only if analyzers/naming conventions demand):

1. Gold boundary:
   - `Assets/Scripts/Meta/Gold/Runtime/Contracts/IGoldService.cs`
   - Exposes read current gold + mutation (`Add(int amount)` or `AddOne()`).
   - Keeps domain authority in wallet.

2. Gold implementation:
   - Service implementation in `Meta/Gold/Runtime`.
   - Depends on `GoldWallet` and updates an observable model suitable for bind.

3. Main menu presentation:
   - `MainMenuViewModel : Scaffold.MVVM.ViewModel`
   - `MainMenuView : Scaffold.MVVM.View<MainMenuViewModel>`
   - Optional `MainMenuModel : Scaffold.MVVM.Model` for screen-local state if needed.

4. DI wiring:
   - `GoldInstaller` registered by bootstrap infra installer.
   - MainMenu controller open path must be triggered from bootstrap completion.

5. Navigation/Addressables wiring:
   - `Navigation Settings` maps `MainMenuViewModel` controller type to `MainMenuView` config.
   - `ViewConfig.Asset` points to Addressables prefab implementing `IView`.

Non-goals for this slice:

- Advanced menu layout/animations.
- Persistence/cloud sync for gold.
- Multi-screen menu flow.

---

Revision Note (2026-03-18 / Codex): Created initial Main Menu white-box ExecPlan to validate startup flow, Addressables + Navigation, DI wiring, and MVVM bind propagation with a minimal gold increment interaction.
