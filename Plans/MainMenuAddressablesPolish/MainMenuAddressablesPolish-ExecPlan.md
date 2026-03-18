# Main Menu and Addressables Polish

This ExecPlan is a living document. The sections `Progress`, `Surprises & Discoveries`, `Decision Log`, and `Outcomes & Retrospective` must be kept up to date as work proceeds.

This document must be maintained in accordance with `PLANS.md` from the repository root.

## Purpose / Big Picture

After this work, Main Menu gold state will be modeled through the Gold module instead of a redundant menu-specific model, Bootstrap tests will focus only on Bootstrap responsibilities, and Addressables startup preload will use one clear configuration path without duplicated orchestration logic. The result is simpler startup behavior, less duplicated code, clearer architectural ownership, and a reliable setup guide that lets a new contributor configure preload correctly on the first try.

A contributor can verify the outcome by running module tests plus the milestone gate and seeing: Main Menu tests own Main Menu assertions, Bootstrap tests no longer assert Main Menu internals, Addressables preload still initializes and registers assets correctly, and documentation explains preload setup with concrete inspector steps.

## Progress

- [x] (2026-03-18 03:05Z) Collected current state and authored this ExecPlan at `Plans/MainMenuAddressablesPolish/MainMenuAddressablesPolish-ExecPlan.md`.
- [x] (2026-03-18 21:20Z) Executed Milestone 1: removed `MainMenuModel`, moved Main Menu state ownership to `GoldWallet` in `MainMenuViewModel`, and deleted obsolete model source/meta files.
- [x] (2026-03-18 21:25Z) Executed Milestone 2: kept Bootstrap PlayMode tests scoped to Bootstrap startup only by removing Main Menu UI assertions from Bootstrap test assembly.
- [x] (2026-03-18 21:45Z) Executed Milestone 3: removed duplicated preload bootstrap/wrapper runtime classes, introduced single-config preload loading with shared request provider, and migrated Addressables tests to the unified config flow.
- [x] (2026-03-18 21:55Z) Executed Milestone 4: documented preload setup steps for the single config path and updated module docs to reflect new ownership/boundaries.
- [x] (2026-03-18 22:01Z) Ran full milestone quality loop; `validate-changes.cmd` passed clean (`TOTAL:0` analyzers, all tests green).

## Surprises & Discoveries

- Observation: Startup preload parsing logic is duplicated in both `AddressablesLayerInitializer` and `AddressablesStartupCoordinator`, including bootstrap-config load and wrapper traversal.
  Evidence: both files independently call `TryLoadBootstrapConfigAsync(...)`, then traverse `AddressablesPreloadBootstrapConfig.Wrappers` and load `AddressablesPreloadConfigWrapper` assets.

- Observation: `BootstrapScenePlayModeTests` currently includes Main Menu flow assertions (`MainMenuView` discovery, gold label text change, add-gold button click), which couples Bootstrap tests to App/MainMenu behavior.
  Evidence: `Assets/Scripts/App/Bootstrap/Tests/PlayMode/BootstrapScenePlayModeTests.cs` contains `BootstrapScene_OpensMainMenu_AndAddGoldUpdatesTmpText` and Main Menu UI helper methods.

- Observation: Running Unity EditMode and PlayMode scripts in parallel blocks one run with “another Unity instance is running with this project open.”
  Evidence: first validation pass returned blocked reports until test commands were rerun sequentially.

- Observation: `BootstrapScopeValidationTests` targeted a removed private method (`ValidateSerializedFields`) and failed despite runtime code being valid.
  Evidence: full gate initially reported 3 failing Bootstrap tests; replacing the tests with `BuildLayerInstallers` guard checks restored green status.

## Decision Log

- Decision: Keep this plan inline (no separate milestone detail files).
  Rationale: scope is substantial but still coherent in one handoff-safe document, and each milestone can be described unambiguously here.
  Date/Author: 2026-03-18 / Codex.

- Decision: Use `GoldWallet` as the authoritative menu-domain state container and remove `MainMenuModel`.
  Rationale: user requirement explicitly calls out `MainMenuModel` as unnecessary and requests using Gold; this also removes one MVVM-level duplication.
  Date/Author: 2026-03-18 / Codex.

- Decision: Consolidate preload configuration to one scriptable asset type and one parsing path shared by startup consumers.
  Rationale: user requirement explicitly asks to remove duplicated config wrappers and duplicated preload flow.
  Date/Author: 2026-03-18 / Codex.

- Decision: Keep `AddressablesStartupCoordinator` but remove duplicated config parsing by introducing `AddressablesPreloadRequestProvider`.
  Rationale: preserves existing startup orchestration responsibilities while deleting repeated preload parsing logic.
  Date/Author: 2026-03-18 / Codex.

- Decision: Migrate Preload Addressables group entry to `Assets/Data/Preload/AddressablesPreloadConfig.asset` with address `addressables/preload/config`.
  Rationale: single-config loading requires the config asset itself to be the keyed addressable entry.
  Date/Author: 2026-03-18 / Codex.

## Outcomes & Retrospective

Implemented all planned milestones. Main Menu now uses Gold-domain state (`GoldWallet`) without a duplicate menu model, Bootstrap tests are now scoped to bootstrap responsibilities, and Addressables preload startup is simplified to one config asset with one shared loader path. Documentation now includes concrete preload setup instructions aligned to the implemented flow.

The quality gate outcome matches the plan purpose: full compilation and tests pass, PlayMode and EditMode are green, and analyzer diagnostics are clean (`TOTAL:0`). The primary lesson learned was operational: Unity test scripts must run sequentially in this environment to avoid project-lock contention.

## Context and Orientation

The relevant areas are:

- Main Menu runtime and tests:
  - `Assets/Scripts/App/MainMenu/Runtime/MainMenuModel.cs`
  - `Assets/Scripts/App/MainMenu/Runtime/MainMenuViewModel.cs`
  - `Assets/Scripts/App/MainMenu/Tests/MainMenuViewModelTests.cs`
  - `Assets/Scripts/App/MainMenu/Tests/Madbox.MainMenu.Tests.asmdef`

- Gold runtime and tests:
  - `Assets/Scripts/Meta/Gold/Runtime/GoldWallet.cs`
  - `Assets/Scripts/Meta/Gold/Tests/GoldWalletTests.cs`

- Bootstrap PlayMode tests:
  - `Assets/Scripts/App/Bootstrap/Tests/PlayMode/BootstrapScenePlayModeTests.cs`
  - `Assets/Scripts/App/Bootstrap/Tests/PlayMode/Madbox.Bootstrap.PlayModeTests.asmdef`

- Addressables startup/preload implementation:
  - `Assets/Scripts/Infra/Addressables/Runtime/Implementation/AddressablesLayerInitializer.cs`
  - `Assets/Scripts/Infra/Addressables/Runtime/Implementation/AddressablesStartupCoordinator.cs`
  - `Assets/Scripts/Infra/Addressables/Runtime/Implementation/AddressablesPreloadConfigWrapper.cs`
  - `Assets/Scripts/Infra/Addressables/Runtime/Implementation/AddressablesPreloadBootstrapConfig.cs`
  - `Assets/Scripts/Infra/Addressables/Runtime/Implementation/AddressablesPreloadConfigEntry.cs`
  - `Assets/Scripts/Infra/Addressables/Runtime/Implementation/AddressablesPreloadConfigRequestBuilder.cs`
  - `Assets/Scripts/Infra/Addressables/Tests/AddressablesGatewayTests.cs`

- Documentation:
  - `Docs/Infra/Addressables.md`
  - `Docs/App/Bootstrap.md`
  - `Docs/App/MainMenu.md`

Definitions used in this plan:

- Bootstrap: the startup composition root (`BootstrapScope`) that initializes layers and opens the first screen.
- Preload: startup loading of selected Addressables assets before regular feature usage.
- Regression test: a test that reproduces a known failure mode before a fix and passes after the fix.

## Plan of Work

Milestone 1 will remove `MainMenuModel` and route Main Menu state through Gold-domain ownership. In `MainMenuViewModel`, replace menu-local model storage with a Gold-backed state path (using `GoldWallet` directly or a thin Gold-backed adapter local to MainMenu runtime) so the menu no longer keeps a separate duplicated gold store. Update Main Menu tests to assert the same behavior via the new model path.

Milestone 2 will decouple Bootstrap test scope from Main Menu behavior. Keep bootstrap PlayMode tests focused on startup completion and fatal-log absence. Move Main Menu end-to-end assertions (gold label update and add button flow) to Main Menu-owned tests, creating a Main Menu PlayMode test assembly if needed.

Milestone 3 will simplify Addressables preload startup. Extract one shared preload-request loading path so both child-scope registration and gateway preload execution use identical parsing logic from a single implementation. Replace the two-stage preload config pair (`AddressablesPreloadBootstrapConfig` + `AddressablesPreloadConfigWrapper`) with one configuration asset model that directly contains preload entries. Remove obsolete classes and update all call sites and tests.

Milestone 4 will document preload setup in a practical, repeatable way. Update Addressables docs with explicit authoring steps (asset creation, key assignment, entry fields, reference type choices, and validation checks), update related docs references, and ensure examples match the new single-config model.

## Concrete Steps

Run all commands from repository root `C:\Users\mtgco\.codex\worktrees\29c9\Madbox`.

1. Baseline and focused test runs before edits:

    powershell -NoProfile -ExecutionPolicy Bypass -File ".\.agents\scripts\run-editmode-tests.ps1" -AssemblyNames "Madbox.MainMenu.Tests"
    powershell -NoProfile -ExecutionPolicy Bypass -File ".\.agents\scripts\run-playmode-tests.ps1" -AssemblyNames "Madbox.Bootstrap.PlayModeTests"
    powershell -NoProfile -ExecutionPolicy Bypass -File ".\.agents\scripts\run-editmode-tests.ps1" -AssemblyNames "Madbox.Addressables.Tests"

2. Milestone 1 edits:

    - Remove `Assets/Scripts/App/MainMenu/Runtime/MainMenuModel.cs`.
    - Update `MainMenuViewModel` to own Gold-backed state without a redundant MainMenu model.
    - Update `MainMenuViewModelTests` and related asmdef references if dependency graph changes.

3. Milestone 2 edits:

    - Trim `BootstrapScenePlayModeTests` to Bootstrap-only assertions.
    - Add/update Main Menu-owned end-to-end tests under `Assets/Scripts/App/MainMenu/Tests/` (and a PlayMode asmdef under MainMenu if required).

4. Milestone 3 edits:

    - Introduce a single preload config asset type (for example `AddressablesPreloadConfig`) containing `AddressablesPreloadConfigEntry` list.
    - Delete `AddressablesPreloadConfigWrapper.cs` and `AddressablesPreloadBootstrapConfig.cs`.
    - Refactor preload parsing into one reusable loader used by both `AddressablesLayerInitializer` and gateway startup logic.
    - Update Addressables tests to cover new config path and preserve current behavior.

5. Milestone 4 docs updates:

    - Update `Docs/Infra/Addressables.md` with a dedicated “Preload Setup” section using the new single-config flow.
    - Update `Docs/App/Bootstrap.md` and `Docs/App/MainMenu.md` references where behavior ownership changed.

6. Validation per milestone (must pass before marking milestone complete):

    powershell -NoProfile -ExecutionPolicy Bypass -File ".\.agents\scripts\run-editmode-tests.ps1" -AssemblyNames "Madbox.MainMenu.Tests"
    powershell -NoProfile -ExecutionPolicy Bypass -File ".\.agents\scripts\run-playmode-tests.ps1" -AssemblyNames "Madbox.Bootstrap.PlayModeTests"
    powershell -NoProfile -ExecutionPolicy Bypass -File ".\.agents\scripts\run-editmode-tests.ps1" -AssemblyNames "Madbox.Addressables.Tests"
    & ".\.agents\scripts\validate-changes.cmd"

7. If any milestone includes a bug fix behavior change, add/update regression test first, confirm fail-before, implement fix, then confirm pass-after for that exact test before running full gate.

## Validation and Acceptance

Acceptance criteria for this ExecPlan:

- Main Menu runtime no longer uses `MainMenuModel`; gold state is sourced from Gold-domain model ownership.
- Main Menu tests still prove initial gold bind and add-gold updates.
- Bootstrap PlayMode tests verify Bootstrap completion and startup health only, without Main Menu UI behavior assertions.
- Main Menu-specific end-to-end assertions live in Main Menu test scope.
- Addressables preload uses one config asset model and one shared loading/parsing path; obsolete wrapper/bootstrap-config classes are removed.
- Addressables tests pass and preserve current preload semantics (`Normal` and `NeverDie`, catalog and key paths).
- `Docs/Infra/Addressables.md` contains a concrete preload setup guide that matches the implemented config model.
- `.agents/scripts/validate-changes.cmd` exits clean.

## Idempotence and Recovery

All editing steps are idempotent when reapplied carefully because they replace existing code paths rather than creating duplicate runtime registrations. If a milestone fails partway, rerun the targeted module tests after each correction before rerunning `validate-changes.cmd`.

If deleting preload classes causes temporary compile failures, recover by introducing the replacement single-config type first, then migrate call sites, then remove old classes in the same milestone commit. Keep class/API renames in one focused commit to simplify rollback via `git revert <commit>`.

## Artifacts and Notes

Expected evidence snippets to capture during implementation updates in this plan:

- A short test output snippet showing Main Menu tests pass after model refactor.
- A short PlayMode snippet showing Bootstrap scene test passes without Main Menu assertions.
- A short Addressables test snippet showing startup preload cases still pass.
- A short analyzer/gate snippet showing `validate-changes.cmd` clean completion.

## Interfaces and Dependencies

Required end-state interfaces and dependencies:

- `Assets/Scripts/App/MainMenu/Runtime/MainMenuViewModel.cs` no longer exposes or relies on `MainMenuModel`.
- `Assets/Scripts/Meta/Gold/Runtime/GoldWallet.cs` remains pure C# and does not gain Unity presentation dependencies.
- `Assets/Scripts/App/Bootstrap/Tests/PlayMode/Madbox.Bootstrap.PlayModeTests.asmdef` remains scoped to Bootstrap behavior.
- Main Menu end-to-end tests live in Main Menu test assemblies and references.
- Addressables preload config type is singular and directly represents preload entries.
- Addressables preload request parsing/loading logic exists in one implementation used by both startup consumers.

Plan revision note: 2026-03-18 - Initial ExecPlan authored to implement requested polish across MainMenu state modeling, test boundary ownership, Addressables preload simplification, and preload setup documentation.
Plan revision note: 2026-03-18 - Executed all milestones, updated tests/docs/assets/runtime code, and recorded clean validation gate results.
