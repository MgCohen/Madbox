# Migrate Levels and Enemies to Unity-Native Assets

This ExecPlan is a living document. The sections `Progress`, `Surprises & Discoveries`, `Decision Log`, and `Outcomes & Retrospective` must be kept up to date as work proceeds.

This document must be maintained in accordance with `PLANS.md` at repository root (`PLANS.md`).

## Purpose / Big Picture

After this change, designers and engineers can build gameplay content directly with Unity assets instead of maintaining duplicate pure C# entity definitions. A level is authored as a ScriptableObject asset, enemies are authored as prefabs with MonoBehaviour components, and the runtime instantiates those prefabs through Addressables. The visible result is that a battle can start from a `LevelDefinitionV2` asset and spawn enemy prefabs with no dependency on legacy pure-C# level/enemy definition classes.

## Progress

- [x] (2026-03-21 00:00Z) Captured migration objective and architecture direction in an initial draft plan.
- [x] (2026-03-21 00:00Z) Reframed plan into repository-required ExecPlan structure from `PLANS.md`.
- [x] (2026-03-21 00:00Z) Created `Plans/UnityEntityCleanup/UnityEntityCleanup-ExecPlan.md` with the full plan content.
- [x] (2026-03-21 00:00Z) Reordered milestones to start with a full enemy pass before levels.
- [ ] Implement Milestone 1 (Enemies V2 full pass: prefab, factory, runtime manager, dumb forward behavior) and validate with `.agents/scripts/validate-changes.cmd` (completed: runtime assembly, actor/factory/registry/spawner/move-forward behavior, and tests added; remaining: resolve unrelated failing PlayMode test and repository-wide analyzer debt so validation gate is clean).
- [ ] Implement Milestone 2 (Levels V2 foundation consuming enemy prefabs) and validate with `.agents/scripts/validate-changes.cmd` (completed: V2 levels assembly, `LevelDefinitionV2`, spawn entry/runtime request models, and tests; remaining: resolve unrelated failing PlayMode test and repository-wide analyzer debt so validation gate is clean).
- [x] (2026-03-21 00:00Z) Simplified Levels V2 schema to Addressables-first references: `LevelEnemySpawnEntryV2` now stores enemy `AssetReference`, `LevelDefinitionV2` stores scene `AssetReference`, and unused ID fields were removed from the V2 level model.
- [ ] Implement Milestone 3 (Battle + services + Addressables V2 path), migrate one vertical slice, and validate. (2026-03-21: V2-only `Madbox.V2.Battle` — `GameFactoryV2`, `GameV2`, `RuleHandlerRegistryV2` + `TimeElapsedCompleteRuleHandlerV2`, data-only `LevelRuleDefinitionV2` / `TimeElapsedCompleteRuleV2`; no legacy migration wiring.)
- [ ] Run full regression/acceptance checks, remove migration blockers, and commit milestone outputs.
- [ ] Complete V2 migration for targeted content and decommission legacy paths.

## Surprises & Discoveries

- Observation: Previous planning output was not wrapped in the strict ExecPlan shape required by `PLANS.md`.
  Evidence: Missing mandatory sections (`Surprises & Discoveries`, `Decision Log`, `Outcomes & Retrospective`) and missing prescribed plan location.
- Observation: Milestone 1 implementation compiles and EditMode tests pass, but the repository validation gate still fails on pre-existing PlayMode and analyzer issues not introduced by this milestone.
  Evidence: `.agents/scripts/validate-changes.cmd` reports 1 failing PlayMode test (`Madbox.Addressables.Tests.PlayMode.AddressablesBootstrapPlayModeTests.BootstrapScene_ResolvesGateway_LoadsAndReleasesAddressable`) and analyzer `TOTAL:179` concentrated in existing non-V2 files.
- Observation: Milestone 2 additions compile and new edit-mode tests are discovered and passing (test total increased from 187 to 189), while V2-specific analyzer diagnostics were eliminated after refactoring.
  Evidence: `.agents/scripts/validate-changes.cmd` reports EditMode `Passed: 189`; remaining failures are the same existing PlayMode bootstrap test plus repository-wide analyzer backlog in non-V2 files.

## Decision Log

- Decision: Use an additive migration with a runtime toggle so legacy and V2 paths can coexist temporarily.
  Rationale: Reduces delivery risk and allows incremental validation while keeping the game runnable.
  Date/Author: 2026-03-21 / Codex

- Decision: Introduce isolated V2 assemblies and namespaces for levels and enemies.
  Rationale: Prevents type collisions and circular dependencies during migration.
  Date/Author: 2026-03-21 / Codex

- Decision: Enemies in V2 are prefab-only (MonoBehaviour composition), no new ScriptableObject enemy definition layer.
  Rationale: Matches requested simplification and removes duplicate model maintenance.
  Date/Author: 2026-03-21 / Codex

- Decision: Start execution with a full enemy pass before introducing Levels V2.
  Rationale: Enemies are standalone and can validate the runtime model (prefab + services + behavior) before level schema dependencies are added.
  Date/Author: 2026-03-21 / Codex

- Decision: Use V2-first folder layout (`V2/Levels`, `V2/Enemies`) instead of domain-first (`Levels/V2`, `Enemies/V2`).
  Rationale: Keeps versioned architecture grouped at the top level and makes future migrations easier to navigate.
  Date/Author: 2026-03-21 / Codex

- Decision: Use `AssetReference` (not raw strings) for scene and enemy entries in Levels V2, and remove V2 IDs that currently have no runtime value (for now).
  Rationale: Keeps level authoring aligned with the Addressables pipeline and reduces schema noise until identifiers are required by concrete gameplay features.
  Date/Author: 2026-03-21 / Codex

- Decision: Introduce V2 battle as a separate assembly (`Madbox.V2.Battle`) with `GameFactoryV2` and rule handlers; `LevelDefinitionV2` owns all battle configuration (scene, enemy spawn entries, rule assets). No legacy `Game`/`GameService` migration in this step.
  Rationale: Keeps the new vertical slice isolated and lets the level ScriptableObject remain the single source of truth for what runs in a battle.
  Date/Author: 2026-03-21 / Codex

- Decision: Replace per-rule `GameContextV2` + `TryEvaluate` on ScriptableObjects with **data-only** `LevelRuleDefinitionV2` assets, **`RuleHandler<TRule>`** implementations (each handler is constructed with its rule asset), and a **`RuleHandlerRegistryV2`** that builds handlers from `LevelDefinitionV2.GameRules` via **`CreateHandlers`**; **`Evaluate(GameV2 game, out GameEndReasonV2 reason)`** receives the full **`GameV2`** so handlers can read elapsed time, enemy registry, level, and later player/services without a slim state interface.
  Rationale: Keeps level assets as pure configuration while logic lives in testable handlers with access to full battle state.
  Date/Author: 2026-03-21 / Codex

- Decision: Use **`AssetReferenceT<EnemyActor>`** on `LevelEnemySpawnEntryV2`; remove **`EnemySpawnRequestV2` / `runtimeId`** and track enemies in **`EnemyServiceV2`** by instance; remove **`BattleFactoryV2`** / **`IEnemyPrefabResolverV2`** in favor of **`EnemyServiceV2.Spawn`** (which delegates to **`EnemyFactoryV2`**) at the composition boundary (future **`GameFactoryV2`** can load scene + wire services).
  Rationale: Less indirection for spawning; identity is the spawned `EnemyActor` instance unless a system truly needs stable IDs later.
  Date/Author: 2026-03-21 / Codex

## Outcomes & Retrospective

At this stage, the planning artifact is corrected to the repository's ExecPlan standard and is ready to be used for implementation. Implementation has not started yet in this revision. The key lesson is that compliance with `PLANS.md` formatting and living-document sections must be enforced before execution work begins.

Milestone 1 implementation has started and produced a working V2 enemy runtime surface (`EnemyActor`, `EnemyFactoryV2`, `EnemyServiceV2`, `EnemyMoveForwardBehaviour`) plus edit-mode tests. Remaining work for strict milestone closure is not implementation scope but validation gate cleanup of unrelated pre-existing failures.

Milestone 2 implementation has started and produced a working V2 level authoring surface (`LevelDefinitionV2`, `LevelEnemySpawnEntryV2`). Runtime uses `Level.EnemyEntries` directly; `GameFactoryV2.PrepareAndSpawnEnemiesFromLevelAsync` loads Addressables per entry and `GameV2.SpawnEnemyCopies` performs instantiation via `EnemyServiceV2`. As with Milestone 1, strict gate closure remains blocked by unrelated pre-existing PlayMode and analyzer failures.

## Context and Orientation

This repository currently contains gameplay logic that mixes Unity runtime objects with pure C# definition models for entities such as levels and enemies. In this plan, "legacy definitions" means those pure C# classes that describe level/enemy data independent of Unity assets. The migration target is:

- Level authoring via ScriptableObject (`LevelDefinitionV2`).
- Enemy authoring via prefab + MonoBehaviour components only.
- Service-level orchestration remains serializable C# classes, but receives Unity asset references resolved via Addressables.
- Battle startup still uses classes/services for orchestration, but consumes V2 asset inputs (level asset, enemy prefab list, spawn parameters).

Key repository paths to create/update for this plan:

- `Plans/UnityEntityCleanup/UnityEntityCleanup-ExecPlan.md` (this file)
- `Plans/UnityEntityCleanup/milestones/ExecPlan-Milestone-1.md` (optional; create only if scope becomes risky/long-running)
- `Assets/Scripts/V2/Levels/` (new level definitions and runtime adapter classes)
- `Assets/Scripts/V2/Enemies/` (new enemy runtime contracts/components)
- `Assets/Scripts/V2/Battle/` (battle bootstrap/resolver integration for V2)
- `Assets/Scripts/Services/` (service updates to use V2 asset inputs)
- `Assets/AddressableAssetsData/` (Addressables groups/entries for V2 assets)
- `.agents/scripts/validate-changes.cmd` (validation gate command; already exists)

If exact existing script paths differ, the first implementation task is to map these target folders to the project's actual equivalent locations and record the mapping in this section before writing production code.

## Plan of Work

Milestone 1 establishes a full Enemies V2 pass and is the first executable vertical slice. Implement enemy authoring as prefab + MonoBehaviour, a factory service that creates enemies, and a runtime manager service that tracks all alive enemies and exposes query/fetch methods for gameplay systems. Include a minimal "dumb" behavior component that moves each spawned enemy forward continuously so the new path has visible runtime behavior. Do not introduce any ScriptableObject enemy definition in V2.

Milestone 2 establishes Levels V2 as the new content source after enemy contracts are proven. Create a `LevelDefinitionV2` ScriptableObject (or a project-specific schema base class if one already exists and is actively used) that stores level identity, scene key/reference, enemy prefab spawn entries, and rule parameters. Add validation methods so broken references are caught in editor time. Then add a runtime adapter that transforms a level asset into the battle bootstrap input expected by current orchestration services.
For the current iteration, prefer an Addressables-first schema: scene and enemy fields are `AssetReference` values, and optional IDs should be omitted unless a concrete runtime consumer exists.

Milestone 3 wires Addressables and battle orchestration. Services should remain serializable C# classes but receive resolved references (loaded level assets, enemy prefabs) through a resolver boundary. Update battle startup flow so V2 content can launch a battle from `LevelDefinitionV2` and prefab enemies while legacy flow remains available behind a toggle. Validate one complete vertical slice end to end.

Milestone 4 performs migration and decommissioning. Migrate levels incrementally, add parity checks, and remove legacy level/enemy definition usage once targeted content is stable. Remove the temporary toggle and compatibility adapter only when tests and runtime validation prove V2 parity.

## Concrete Steps

Run all commands from repository root.

1. Baseline and locate current gameplay architecture.

    - `git status`
    - `rg "class .*Level|LevelDefinition|EnemyDefinition|Addressable|Battle" Assets/Scripts`
    - `rg "asmdef" Assets -g "*.asmdef"`

   Expected outcome: Identify exact files that currently define level/enemy models, battle startup, and service boundaries.

2. Implement Milestone 1 (Enemies V2 full pass).

    - Monobehaviour/prefab:
      - Add `EnemyActor` root component in `Assets/Scripts/V2/Enemies/` with identity/team/lifecycle fields needed by runtime systems.
      - Create at least one enemy prefab under project prefab folders with `EnemyActor` attached.
    - Service to create enemies (factory):
      - Add `EnemyFactoryV2` in `Assets/Scripts/V2/Enemies/` that instantiates enemy prefabs and performs initialization.
      - Define stable creation input (spawn position, rotation, optional owner/team/context).
    - Service to handle all enemies during gameplay:
      - Add `EnemyServiceV2` in `Assets/Scripts/V2/Enemies/` that spawns via `EnemyFactoryV2`, registers instances, and tracks spawned/despawned enemies.
      - Expose fetch/query methods for current enemies (all alive, by team, nearest candidate hooks if needed later).
    - Basic enemy behavior ("dumb" simulation):
      - Add `EnemyMoveForwardBehaviour` MonoBehaviour that moves forward every frame after spawn.
      - Ensure movement starts automatically after factory creation so behavior is visible in smoke tests.

   Expected outcome: A scene can spawn enemies via factory, registry can fetch active enemies, and each spawned enemy moves forward continuously.

3. Implement Milestone 2 (Levels V2 consuming enemy prefabs).

    - Add `LevelDefinitionV2` and related serializable entry structs under `Assets/Scripts/V2/Levels/`.
    - Enemy references in level entries must point to enemy prefabs used by `EnemyFactoryV2`.
    - Add editor/runtime validation for null refs and invalid spawn values.
    - Add adapter from level asset to battle bootstrap request.

   Expected outcome: One level asset can be authored and consumed by runtime through adapter path using enemy prefab references.

4. Implement Milestone 3 (Services + Addressables + battle V2 path).

    - Add/modify resolver in `Assets/Scripts/Services/` or equivalent to load V2 assets from Addressables.
    - Wire battle startup in `Assets/Scripts/V2/Battle/` (or existing battle bootstrap path) to accept V2 inputs.
    - Keep legacy path selectable for transition safety.

   Expected outcome: Vertical slice launches from V2 level asset and prefab enemy list.

5. Validate each milestone loop and commit.

    - If bug fix included: add regression test that fails before, passes after.
    - Run `.agents/scripts/validate-changes.cmd`
    - Fix failures and rerun until clean.
    - Commit milestone changes.

   Expected outcome: Every milestone has passing validation and a clean commit.

## Validation and Acceptance

Behavioral acceptance for this ExecPlan:

- Spawn at least one enemy prefab through `EnemyServiceV2` (which uses `EnemyFactoryV2`) and verify it is tracked by the service.
- Observe spawned enemies continuously moving forward due to `EnemyMoveForwardBehaviour`.
- Author a `LevelDefinitionV2` asset containing at least two enemy prefab entries and a scene reference/key.
- Run the game flow that starts battle from selected level using V2 enemy prefab references.
- Observe that enemies are instantiated from prefabs and behave using attached MonoBehaviour components.
- Confirm no runtime dependency on V2 path for legacy pure-C# enemy definition classes.
- Confirm Addressables resolves required assets in the V2 path.

Validation commands (run from repo root):

- `.agents/scripts/validate-changes.cmd`
- Project test command(s) used by repository (record exact command once discovered, for example edit-mode/play-mode tests).
- Optional smoke run command used by repository for local gameplay verification.

Expected validation evidence:

- Validation gate exits clean.
- New/updated tests pass.
- Manual smoke scenario demonstrates factory spawn -> runtime registry fetch -> forward movement -> level V2 battle spawn sequence.

## Idempotence and Recovery

This migration is additive-first and safe to repeat. Creating V2 classes/assets does not delete legacy paths. If a milestone fails midway, revert only that milestone's uncommitted changes and rerun from the last green commit. Keep the runtime toggle until migration parity is confirmed; this provides an immediate fallback to legacy behavior. Delay destructive cleanup (legacy deletion) until all acceptance checks pass on migrated content.

## Artifacts and Notes

Expected short evidence snippets to include as implementation proceeds:

    > .agents/scripts/validate-changes.cmd
    [PASS] compile checks
    [PASS] tests
    [PASS] static validation

    > (gameplay smoke)
    Loaded LevelDefinitionV2: level_id=...
    Resolved enemy prefabs: N
    Spawned enemies from prefabs: N
    Battle started successfully

When milestone detail files are needed, create and reference:

- `Plans/UnityEntityCleanup/milestones/ExecPlan-Milestone-1.md`
- `Plans/UnityEntityCleanup/milestones/ExecPlan-Milestone-2.md`
- `Plans/UnityEntityCleanup/milestones/ExecPlan-Milestone-3.md`

Only create these when scope/risk meets the criteria in `PLANS.md`.

## Interfaces and Dependencies

Prescriptive interfaces/types that must exist by end of migration:

- `Assets/Scripts/V2/Enemies/EnemyFactoryV2.cs`
  - Instantiates enemy prefabs and applies spawn initialization (used by `EnemyServiceV2`).
- `Assets/Scripts/V2/Enemies/EnemyServiceV2.cs`
  - Orchestrator: spawns through the factory, tracks active enemies, and exposes fetch/query/unregister for gameplay systems.
- `Assets/Scripts/V2/Enemies/EnemyMoveForwardBehaviour.cs`
  - MonoBehaviour that moves enemy forward every frame after creation.
- `Assets/Scripts/V2/Levels/LevelDefinitionV2.cs`
  - ScriptableObject containing scene `AssetReference` and enemy spawn entries.
- `Assets/Scripts/V2/Levels/LevelEnemySpawnEntry.cs`
  - Serializable entry containing prefab reference and spawn parameters.
- `Assets/Scripts/V2/Enemies/EnemyActor.cs`
  - MonoBehaviour root component required on enemy prefabs.
- `Assets/Scripts/V2/Battle/BattleBootstrapV2.cs` (or equivalent integration point)
  - Accepts V2 level data and prefab-based enemy inputs.
- `Assets/Scripts/Services/V2AssetResolver.cs` (or equivalent)
  - Resolves Addressables references for level/enemy assets for service consumption.

Dependencies:

- Unity ScriptableObject system for level authoring.
- Unity prefab/MonoBehaviour composition for enemy behavior and data.
- Unity Addressables for asset loading and delivery.
- Existing validation gate script: `.agents/scripts/validate-changes.cmd`.

---

Revision Note (2026-03-21 / Codex): Replaced non-compliant planning output with a `PLANS.md`-compliant ExecPlan structure, added mandatory living-document sections, mandatory file location, milestone execution loop, and explicit validation requirements.
Revision Note (2026-03-21 / Codex): Reordered milestones to execute an enemy-first full pass (prefab + factory + runtime manager + forward movement behavior) before levels, and updated validation/acceptance criteria accordingly.
Revision Note (2026-03-21 / Codex): Executed Milestone 1 implementation pass and updated progress/discoveries to reflect current validation blockers outside the new V2 enemy code.
Revision Note (2026-03-21 / Codex): Executed Milestone 2 implementation pass for V2 level definitions and runtime adapter models, then updated living sections with current validation state.
