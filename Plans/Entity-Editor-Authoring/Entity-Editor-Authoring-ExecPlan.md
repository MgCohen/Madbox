# Editor Authoring Foundation for Levels, Enemies, and Gold

This ExecPlan is a living document. The sections `Progress`, `Surprises & Discoveries`, `Decision Log`, and `Outcomes & Retrospective` must be kept up to date as work proceeds.

This plan must be maintained in accordance with `PLANS.md` at the repository root.

## Purpose / Big Picture

After this change, designers can author level and enemy content directly in the Unity Editor using ScriptableObject assets, optionally configure gold and level lists, and attach a minimal enemy MonoBehaviour stub to prefabs for asset linkage. Core gameplay rules remain in pure C# domain modules, while Unity-facing authoring data is mapped into domain definitions at runtime.

A working outcome is visible when a developer creates `EnemyDefinitionSO` and `LevelDefinitionSO` assets, links them in a level list/config asset, and runs tests proving the authored assets map to valid domain `LevelDefinition` and battle rule definitions.

## Progress

- [x] (2026-03-18 15:11Z) Gathered architecture, planning, testing, and research context (`Architecture.md`, `PLANS.md`, `MILESTONE.md`, entity/layer/addressables research, core module docs) and captured scope constraints from user.
- [x] (2026-03-18 15:11Z) Authored initial ExecPlan for editor authoring with Addressables integration, battle-rule coverage, and record-to-class migration strategy.
- [ ] Execute Milestone 1: migrate authoring-facing record types in `Madbox.Levels` to plain serializable classes and update dependent tests.
- [ ] Execute Milestone 2: create Unity-facing authoring module (ScriptableObjects with in-asset mapping methods) for enemy/level/gold/level-list content.
- [ ] Execute Milestone 3: add baseline enemy MonoBehaviour authoring stub and prefab hookup path (no gameplay behavior logic).
- [ ] Execute Milestone 4: wire Addressables labels/keys/usages for authored assets and add validation tests.
- [ ] Execute Milestone 5: update docs and run milestone quality gate (`.agents/scripts/validate-changes.cmd`) until clean.

## Surprises & Discoveries

- Observation: Current core level behavior definitions and IDs use records (`LevelId`, `EntityId`, `MovementBehaviorDefinition`, `ContactAttackBehaviorDefinition`), which are inconvenient for direct Unity Inspector serialization.
  Evidence: `Assets/Scripts/Core/Levels/Runtime/LevelId.cs`, `Assets/Scripts/Core/Levels/Runtime/EntityId.cs`, `Assets/Scripts/Core/Levels/Runtime/Behaviors/*.cs`.

- Observation: Only one enemy prefab currently exists (`Bee.prefab`), so the initial authoring flow should be prefab-agnostic and not assume multiple existing enemy prefabs.
  Evidence: `Assets/Prefabs/Enemies/Bee.prefab`.

- Observation: Battle end rules are executable classes (not plain data objects), so level authoring needs explicit rule authoring inputs that map into `LevelGameRuleDefinition` instances.
  Evidence: `Assets/Scripts/Core/Levels/Runtime/Rules/LevelGameRuleDefinition.cs`, `EnemyEliminatedWinRuleDefinition.cs`, `TimeLimitLoseRuleDefinition.cs`, `PlayerDefeatedLoseRuleDefinition.cs`; and usage expectations in `Docs/Core/Battle.md`.

## Decision Log

- Decision: Unity editor authoring assets will live in a Unity-facing module under `Assets/Scripts/App/Authoring/` rather than in core domain modules.
  Rationale: Preserves architecture invariants that core logic remains pure C# and avoids Unity API coupling in `Core/*`.
  Date/Author: 2026-03-18 / Codex.

- Decision: Record types used in the editor-authoring path will be migrated to plain serializable classes instead of introducing adapters/wrappers.
  Rationale: Matches user requirement, simplifies Inspector serialization, and avoids maintaining duplicate representations.
  Date/Author: 2026-03-18 / Codex.

- Decision: Include battle-related level rule authoring support in scope (especially win/lose rule configuration), but do not expand runtime battle mechanics.
  Rationale: User explicitly requested battle definitions/rules support where needed, and level authoring must feed `GameRuleEvaluator` behavior.
  Date/Author: 2026-03-18 / Codex.

- Decision: Enemy MonoBehaviour implementation remains a minimal reference stub only (definition link + optional view references), with no substantial enemy behavior logic.
  Rationale: User requested that enemy MonoBehaviour behavior logic be deferred to view-stage implementation.
  Date/Author: 2026-03-18 / Codex.

- Decision: Addressables integration will use existing infra contracts (`IAddressablesGateway`, `AssetKey`, labels) and typed `AssetReference` fields in authoring assets when runtime loading is required.
  Rationale: Reuses existing package/module and keeps loading policy centralized in infra.
  Date/Author: 2026-03-18 / Codex.

- Decision: Default loading policy is on-demand per selected level/session, not "load all definitions into memory".
  Rationale: Aligns with `Docs/Infra/Addressables.md` ownership rules and avoids unnecessary resident memory.
  Date/Author: 2026-03-18 / Codex.

- Decision: `LevelEnemyEntrySO` will be a `[Serializable]` nested/support class (not a `ScriptableObject`) and does not need its own `ToDomain` method.
  Rationale: Entry objects are simple containers owned by `LevelDefinitionSO`; keeping mapping at level aggregate avoids fragmentation.
  Date/Author: 2026-03-18 / Codex.

- Decision: `EnemyDefinitionSO` will own a `[SerializeReference]` rule list and the plan includes a custom editor for practical polymorphic authoring.
  Rationale: Enables flexible rule composition directly in Inspector while keeping runtime rule classes authoritative.
  Date/Author: 2026-03-18 / Codex.

## Outcomes & Retrospective

Initial plan drafted. Implementation has not started yet, so no runtime outcomes are recorded yet. This section will be updated after each milestone with concrete shipped behavior, validation evidence, and any scope adjustments.

## Context and Orientation

Relevant baseline modules and files:

- Core level definitions (current domain schema):
  - `Assets/Scripts/Core/Levels/Runtime/EnemyDefinition.cs`
  - `Assets/Scripts/Core/Levels/Runtime/LevelEnemyDefinition.cs`
  - `Assets/Scripts/Core/Levels/Runtime/LevelDefinition.cs`
  - `Assets/Scripts/Core/Levels/Runtime/Behaviors/EnemyBehaviorDefinition.cs`
  - `Assets/Scripts/Core/Levels/Runtime/Behaviors/MovementBehaviorDefinition.cs`
  - `Assets/Scripts/Core/Levels/Runtime/Behaviors/ContactAttackBehaviorDefinition.cs`
  - `Assets/Scripts/Core/Levels/Runtime/Rules/*.cs`
- Battle rules consumption reference:
  - `Docs/Core/Battle.md`
- Enemy runtime module (domain, non-Unity):
  - `Assets/Scripts/Core/Enemies/Runtime/*`
- Addressables infra contracts/runtime:
  - `Assets/Scripts/Infra/Addressables/Runtime/Contracts/*`
  - `Assets/Scripts/Infra/Addressables/Runtime/Implementation/*`
- Existing prefab baseline:
  - `Assets/Prefabs/Enemies/Bee.prefab`

Terms used in this plan:

- Authoring asset: a Unity `ScriptableObject` used to edit game content in Inspector.
- Domain definition: pure C# data object consumed by runtime battle/enemy systems.
- Mapping: explicit conversion from Unity authoring asset to domain definition.
- Baseline enemy stub: minimal MonoBehaviour used only for prefab-level references, not gameplay AI/logic.

### Scope boundaries

In scope:

- `EnemyDefinitionSO`, `LevelDefinitionSO`, optional `GoldConfigSO`, and a level-list/config asset.
- Mapping from SO assets to `Madbox.Levels` domain objects including game rules used by battle.
- Record-to-class migration for authoring-facing level domain types.
- Addressables usage where runtime dynamic loading is needed.
- Tests, docs, analyzer compliance, and milestone quality gate.

Out of scope:

- Implementing enemy movement/attack MonoBehaviour gameplay logic.
- Rewriting battle command pipeline behavior.
- Building advanced enemy behavior MonoBehaviour logic beyond reference stub responsibilities.

## Plan of Work

Milestone 1 establishes serialization-friendly domain types without changing battle semantics. Convert authoring-facing record types in `Madbox.Levels` to serializable classes and preserve existing constructor guards/invariants. Update all callsites/tests affected by equality/value semantics changes.

Milestone 2 introduces a Unity-facing authoring module using the create-module workflow. Add ScriptableObject definitions for enemies, levels, optional gold config, and level list/config. Mapping logic is owned directly by authoring assets (`ToDomain` on root SOs), while `LevelEnemyEntrySO` remains a simple serializable entry object without independent mapping API. `EnemyDefinitionSO` includes a `[SerializeReference]` list for polymorphic rule authoring.

Milestone 3 adds a minimal enemy MonoBehaviour authoring stub in the Unity-facing module and integrates it into enemy prefab authoring. Keep fields focused on references required by assets and later view-layer logic, with no runtime combat behavior implementation.

Milestone 4 integrates Addressables for authored assets that should be loaded by key/reference at runtime (level list, selected level definition, only referenced enemy definitions, and needed prefab links). Reuse `Madbox.Addressables` contracts, keep keys/labels centralized, and add focused tests for mapping and load/release paths.

Milestone 5 completes documentation and quality gates: update module docs under `Docs/` (including any new module docs), run EditMode/PlayMode/analyzer checks via repository scripts, and keep rerunning `.agents/scripts/validate-changes.cmd` until all gates are clean.

## Concrete Steps

All commands run from repository root: `C:\Unity\Madbox`.

1. Create/prepare authoring module structure via workflow guidance:

    - `Get-Content -Raw ".\.agents\workflows\create-module.md"`
    - Create `Assets/Scripts/App/Authoring/{Runtime,Container,Tests}` and corresponding `.asmdef` files.

2. Implement Milestone 1 record-to-class migration in `Assets/Scripts/Core/Levels/Runtime/` and update dependent tests:

    - `powershell -NoProfile -ExecutionPolicy Bypass -File ".\.agents\scripts\run-editmode-tests.ps1" -AssemblyNames "Madbox.Levels.Tests,Madbox.Battle.Tests,Madbox.Enemies.Tests"`

3. Implement Milestone 2 authoring ScriptableObjects and in-asset mapping logic in `Assets/Scripts/App/Authoring/Runtime/`:

    - `powershell -NoProfile -ExecutionPolicy Bypass -File ".\.agents\scripts\run-editmode-tests.ps1" -AssemblyNames "Madbox.Authoring.Tests"`

4. Add Milestone 2 custom editor support for `EnemyDefinitionSO` `[SerializeReference]` rule authoring in `Assets/Scripts/App/Authoring/Editor/`:

    - Validate in Inspector that adding/removing/changing concrete rule entries is deterministic and persists after domain reload.

5. Implement Milestone 3 baseline enemy MonoBehaviour stub and prefab linkage:

    - Validate by opening `Assets/Prefabs/Enemies/Bee.prefab` and confirming required authoring component references are assignable.

6. Implement Milestone 4 Addressables integration updates and tests:

    - `powershell -NoProfile -ExecutionPolicy Bypass -File ".\.agents\scripts\run-editmode-tests.ps1" -AssemblyNames "Madbox.Addressables.Tests,Madbox.Authoring.Tests"`
    - Verify one level-session flow loads only selected content and releases all non-resident handles when session ends.

7. Run full quality gate for each milestone completion:

    - `& ".\.agents\scripts\validate-changes.cmd"`

Expected success signal after milestone completion is repository scripts finishing with passing EditMode/PlayMode checks and analyzer summary `TOTAL:0`.

## Validation and Acceptance

Acceptance is behavior-based and must be observable.

1. Authoring assets can be created from Unity Create menu for enemy, level, and config assets, and fields are editable in Inspector without wrapper/adaptor indirection for migrated types.

2. `LevelDefinitionSO` can represent enemy entries and battle completion rule inputs (including at least enemy-eliminated win and time-limit/player-defeat loss options), and `LevelDefinitionSO.ToDomain()` produces valid `LevelDefinition` objects consumed by battle runtime.

3. `LevelEnemyEntrySO` is a plain serializable class used by `LevelDefinitionSO` and does not require independent `ToDomain` API surface.

4. `EnemyDefinitionSO` supports polymorphic rule authoring through `[SerializeReference]` with a custom editor workflow that is practical for designers.

5. Enemy prefab can host the baseline authoring MonoBehaviour and reference an enemy definition asset without embedding gameplay behavior logic.

6. Addressables can load authored assets through existing gateway contracts (typed key/reference/label path), with deterministic release ownership behavior.
   Loading behavior must demonstrate non-global preload: load selected level content on demand, then release session-owned handles.

7. Automated tests cover:

- record-to-class migration behavior/invariants in `Madbox.Levels.Tests`.
- SO-to-domain mapping (including rule mapping) in new authoring tests.
- Addressables integration touchpoints and handle lifecycle where changed, including proof that only selected level/session assets are loaded and then released.

8. Full gate passes via `.agents/scripts/validate-changes.cmd`.

If implementation reveals a bug, add/update a regression test first, verify fail-before/pass-after, then proceed per milestone quality loop.

## Idempotence and Recovery

All planned changes are additive or localized refactors and can be rerun safely. If migration introduces type-compatibility breakage, recover by:

1. Re-running affected module tests to isolate failures.
2. Restoring constructor/guard semantics in migrated classes to match prior behavior.
3. Updating only callsites broken by equality semantics changes, avoiding cross-module behavior changes.

For Addressables and prefab wiring, keep changes incremental so each asset/config can be reverted independently if a milestone fails validation.

## Artifacts and Notes

Planned authored asset shape (high-level):

    Assets/Data/Entities/EnemyDefinitionSO.asset
    Assets/Data/Levels/LevelDefinitionSO.asset
    Assets/Data/Levels/LevelListConfigSO.asset
    Assets/Data/Meta/GoldConfigSO.asset

Planned code ownership shape (high-level):

    Assets/Scripts/Core/Levels/Runtime/*          # pure domain classes used by battle/enemies
    Assets/Scripts/App/Authoring/Runtime/*        # Unity ScriptableObjects + in-asset mapping + baseline MonoBehaviour stub
    Assets/Scripts/App/Authoring/Editor/*         # custom inspectors/property drawers for serialize-reference authoring
    Assets/Scripts/App/Authoring/Tests/*          # authoring and mapping EditMode tests

## Interfaces and Dependencies

Target interfaces/types that must exist by plan completion:

In `Assets/Scripts/App/Authoring/Runtime/`:

    [CreateAssetMenu(menuName = "Madbox/Authoring/Enemy Definition")]
    public class EnemyDefinitionSO : ScriptableObject
    {
        // Entity id, health, behavior settings, prefab/addressable reference fields.
        // [SerializeReference] list of rule definitions for polymorphic authoring.
        public EnemyDefinition ToDomain() { ... }
    }

    [Serializable]
    public class LevelEnemyEntrySO
    {
        // Enemy reference + count only. No own ToDomain method.
    }

    [CreateAssetMenu(menuName = "Madbox/Authoring/Level Definition")]
    public class LevelDefinitionSO : ScriptableObject
    {
        // Level id, reward, enemy entries, and battle-rule authoring entries.
        public LevelDefinition ToDomain() { ... }
    }

    public sealed class EnemyAuthoringReference : MonoBehaviour
    {
        // Baseline reference-only component for prefab/asset linkage.
    }

In `Assets/Scripts/Core/Levels/Runtime/`:

    [Serializable]
    public class LevelId { ... }

    [Serializable]
    public class EntityId { ... }

    [Serializable]
    public abstract class EnemyBehaviorDefinition { ... }

    [Serializable]
    public class MovementBehaviorDefinition : EnemyBehaviorDefinition { ... }

    [Serializable]
    public class ContactAttackBehaviorDefinition : EnemyBehaviorDefinition { ... }

Dependency rules to preserve:

- `Madbox.Levels` remains Unity-agnostic (`noEngineReferences: true`).
- Unity-facing authoring module may depend on `Madbox.Levels` and `Madbox.Addressables` contracts.
- `Madbox.Battle` and `Madbox.Enemies` continue consuming domain definitions, not Unity objects.
- Addressables ownership rules from `Docs/Infra/Addressables.md` are mandatory: use `IAssetHandle<T>` / `IAssetGroupHandle<T>` and release all session-scoped owners.

Revision Note (2026-03-18): Initial plan created to enable editor authoring for entities/levels/gold with Addressables integration, include battle-rule authoring inputs, and migrate authoring-facing records to serializable classes per user direction.
Revision Note (2026-03-18): Updated plan to keep mapping in root ScriptableObjects instead of `ILevelDefinitionMapper`, model `LevelEnemyEntrySO` as a plain serializable entry class without independent mapping API, and add `[SerializeReference]` rule authoring with custom editor support on `EnemyDefinitionSO`.
Revision Note (2026-03-18): Tightened Addressables scope to on-demand selected-level/session loading with explicit handle-release validation to avoid loading all content into memory.
