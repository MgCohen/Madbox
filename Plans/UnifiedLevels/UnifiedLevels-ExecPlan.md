# Unified level progression (LiveOps + client definitions)

This ExecPlan is a living document. The sections `Progress`, `Surprises & Discoveries`, `Decision Log`, and `Outcomes & Retrospective` must be kept up to date as work proceeds.

Repository planning rules live at `PLANS.md` (repository root). This document must be maintained in accordance with `PLANS.md`.

## Purpose / Big Picture

Today the project has **two separate “level” concepts**: (1) **LiveOps** progression in `GameModuleDTO.Modules.Level` (`LevelGameData`, `LevelPersistence`, Cloud Code `GameModule.Modules.Level.LevelService`) built from **multi-item** `ModuleProgress` rows, and (2) **client content** as Addressables-backed `Madbox.Levels.LevelDefinition` assets (see `Plans/MainMenuLevelsIntegration/MainMenuLevelsIntegration-ExecPlan.md` for the catalog direction). Main menu and other features must not juggle two incompatible models.

After this work, **one clear story** exists end-to-end: **remote config** declares which level IDs exist (and optional schedule metadata), **persistence** stores only **what the player finished** (and at most **one** “last selected” level ID for UX), the **LiveOps module** computes a **small, explicit per-level state** (blocked, unlocked, complete—and optionally **tease** for marketing locks), and a **single client-facing service** merges that payload with **loaded `LevelDefinition` assets** so consumers like **main menu** receive **one list** pairing **definition + state** (or an equivalent read-only structure). **`LevelPersistence`** and **`LevelGameData`** **stop inheriting** **`MultiProgressModuleData`** and become **simple** `IGameModuleData` types as sketched below. **`MultiProgressModuleData`** remains in the repo for **Tutorial** (`TutorialPersistence`, `TutorialGameData`) until a **separate** refactor removes it there and deletes the file—**out of scope** for this ExecPlan.

Completing a level uses a **dedicated client method** on the LiveOps level module that **calls the backend** via **`CompleteLevelRequest`** / **`CompleteLevelResponse`**. For the first iteration the response carries **only success or failure** (no refreshed game-data payload). **Refreshing** aggregated module data after a successful complete is a **separate step** (e.g. a later bootstrap or explicit refetch); this plan does not require that wiring yet.

A developer can verify success with **automated tests** for state derivation and unified mapping, **`.agents/scripts/validate-changes.cmd`** green, and a **manual** check: main menu entries reflect LiveOps locks and Addressables content together (e.g. locked rows hidden or disabled per product rules).

## Progress

- [x] (2026-03-22) **`LevelDefinition.LevelId`** added; must match remote-config ordered IDs.
- [x] (2026-03-22) Simplified **DTOs**: **`LevelPersistence`**, **`LevelConfig`**, **`LevelGameData`**, **`LevelProgression`**, **`LevelStateEntry`**, **`LevelAvailabilityState`**; Cloud Code **`Initialize`** / **`CompleteLevel`** updated. **Migration:** old **`LevelPersistence`** JSON with **`_progress`** no longer deserializes into completed IDs; treat as empty completed set until a migration path is added if needed.
- [x] (2026-03-22) **`CompleteLevelResponse(bool succeeded)`**; Cloud Code returns **`Succeeded`** only (no embedded **`LevelGameData`**).
- [x] (2026-03-22) **`LevelService.CompleteLevelAsync`** on client; **`IAvailableLevelsService`** / **`AvailableLevelsService`** registered in **`BootstrapMetaInstaller`** with default empty **`IReadOnlyList<LevelDefinition>`**.
- [ ] (deferred) Wire **MainMenu** to **`IAvailableLevelsService`** (optional follow-up).
- [x] (2026-03-22) **`Docs/Core/LiveOpsLevel.md`**, **`Docs/Core/Levels.md`** updated.
- [ ] Run **`.agents/scripts/validate-changes.cmd`** when Unity is not locking the project (batchmode requires exclusive project access).

## Surprises & Discoveries

- Observation: **`IAvailableLevelsService`** registration cannot live in **`Madbox.Level.Container`** without `Madbox.Level.Container` referencing **`Madbox.Levels`**, which failed **`Madbox.Level.Container.csproj`** resolution in the analyzer pass. Registration was moved to **`BootstrapMetaInstaller`** alongside **`Madbox.Levels`** reference on **`Madbox.Bootstrap.Runtime`**.
  Evidence: `dotnet build` on `Madbox.Level.Container.csproj` after adding `Madbox.Levels` to that asmdef still failed in the solution analyzer; bootstrap already composes meta modules.

## Decision Log

- Decision: **Level module only: stop using `MultiProgressModuleData`.** **`LevelPersistence`** and **`LevelGameData`** must **not** inherit **`MultiProgressModuleData`**. Replace with persistence focused on **completed level IDs** plus optional **`LastSelectedLevelId`**, and with **`LevelGameData`** as explicit derived-state snapshots (see **Explicit type sketches**). Server-side code derives **blocked / unlocked / complete** for every ID in config; clients do not re-implement progression rules except for local UX caching if explicitly allowed.
  Rationale: User request to simplify LiveOps level progression; single source of truth on the server for gating and rewards.
  Date/Author: 2026-03-22 / Agent (planning)

- Decision: **Tutorial keeps `MultiProgressModuleData` for now; delete the base class later.** **`TutorialPersistence`** and **`TutorialGameData`** continue to inherit **`MultiProgressModuleData`** until a **future** refactor. **`LiveOps/LiveOps.DTO/Modules/Common/MultiProgressModuleData.cs`** stays in the repository for that reason. Deleting the file and refactoring Tutorial is **explicitly out of scope** for this ExecPlan.
  Rationale: User asked to decouple level work from Tutorial; avoid a wide DTO churn in one milestone.
  Date/Author: 2026-03-22 / Agent (planning)

- Decision: **Progression rule (default)** — Config publishes an **ordered** list of level IDs (and optional per-entry schedule). Let **C** be the maximum completed ID in persistence **that appears in config order** (if none, treat as “no completed step”). Then: every ID **before** the next playable step in order is **Complete**; the **first** ID in order that is not completed is **Unlocked** if it is the immediate successor of the completed prefix (equivalently: exactly one **frontier** unlocked level in strict linear progression); all **later** IDs are **Blocked**. Example: config order `1,2,3,4`, completed `{1}` → `1` Complete, `2` Unlocked, `3` and `4` Blocked.
  Rationale: Matches the user’s worked example and existing Cloud Code intent in `LevelService.CompleteLevel` (sequential completion).
  Date/Author: 2026-03-22 / Agent (planning)

- Decision: **“Tease”** is an **optional** visibility mode: a level may appear in UI as **preview-only** (not playable) when **Blocked**, controlled by **config** (per-level flag or separate list), without changing **Unlocked** semantics for actual play.
  Rationale: User allowed “some may tease”; keeps binary lock/unlock for gameplay while supporting marketing.
  Date/Author: 2026-03-22 / Agent (planning)

- Decision: **Unified client type** — Expose **`IReadOnlyList<AvailableLevel>`** (name illustrative) where each item contains at minimum **`int LevelId`**, **`LevelDefinition Definition`** (nullable if asset missing for that ID), and **`LevelAvailabilityState`** (enum: e.g. `Blocked`, `Unlocked`, `Complete`, optional `TeaseBlocked`). Prefer a **list** over a bare dictionary for stable UI ordering (order = config order merged with definition load order; exact ordering rule to be fixed in implementation).
  Rationale: Main menu and other presenters need both **content** and **state** in one place; list preserves order; dictionary can be built by callers if needed.
  Date/Author: 2026-03-22 / Agent (planning)

- Decision: **Naming** — Keep **`Madbox.Level.LevelService`** as the LiveOps **`GameClientModuleBase<LevelGameData>`** holder if the DTO name stays; introduce a **different** type name for the unified facade (e.g. **`UnifiedLevelSelectionService`** / **`IAvailableLevelsService`**) to avoid colliding with the existing `LevelService` type in `Assets/Scripts/Core/LiveOpsLevel/`.
  Rationale: Existing `MainMenuLevelsIntegration` ExecPlan already reserved `LevelService` vs catalog naming; unified service is a third, composition role.
  Date/Author: 2026-03-22 / Agent (planning)

- Decision: **`Plans/MainMenuLevelsIntegration/MainMenuLevelsIntegration-ExecPlan.md`** assumed LiveOps and definition catalog stay separate; **this ExecPlan supersedes that separation** for **presentation-level** consumption—implementation should **update** that plan’s Decision Log or mark it superseded when unified API lands.
  Rationale: Avoid contradictory documentation.
  Date/Author: 2026-03-22 / Agent (planning)

- Decision: **`CompleteLevelResponse` v1** carries **no** embedded `LevelGameData`. It exposes **only** whether the server accepted the completion (e.g. a **`bool`** such as **`Succeeded`**, in addition to whatever **`ModuleResponse`** already exposes for status). Callers that need updated progression **later** obtain **`LevelGameData`** through the normal **aggregated game data** path (refetch or next session), not through this response.
  Rationale: User asked for minimal dynamics on the complete endpoint; keeps payload and client parsing trivial.
  Date/Author: 2026-03-22 / Agent (planning)

- Decision: **`CompleteLevelRequest`** carries **only** the **level id** (`int`). Callers that have an **`AvailableLevel`** (or unified row) pass **`availableLevel.LevelId`** (or equivalent). No separate “level structure” DTO is required on the wire unless product later demands audit metadata.
  Rationale: Single identifier matches simplified persistence and config.
  Date/Author: 2026-03-22 / Agent (planning)

## Outcomes & Retrospective

(Fill at completion.)

## Context and Orientation

**LiveOps side (server + DTO).** Cloud Code `LiveOps/Project/Modules/Level/LevelService.cs` loads `LevelPersistence` and `LevelConfig`, merges via `LevelGameData.From`, and enforces sequential completion in `CompleteLevel`. DTOs live under `LiveOps/LiveOps.DTO/Modules/Level/`. **This plan** changes **Level** types to **not** use **`MultiProgressModuleData`**. **`Tutorial`** DTOs under `LiveOps/LiveOps.DTO/Modules/Tutorial/` **continue** to inherit **`MultiProgressModuleData`** until a later refactor.

**Client LiveOps module.** `Assets/Scripts/Core/LiveOpsLevel/Runtime/LevelService.cs` (`Madbox.Level`) extends `GameClientModuleBase<LevelGameData>` and only pulls module data from `ILiveOpsService` during initialization.

**Gameplay / content side.** `Madbox.Levels.LevelDefinition` (`Assets/Scripts/Meta/Levels/Runtime/LevelDefinition.cs`) is a **ScriptableObject** with scene and rules; it does **not** currently expose a dedicated **numeric ID** field in the snippet reviewed for this plan. Unification **requires** a defined mapping from **LiveOps level ID** → **definition** (serialized int on the asset, or documented convention such as name parsing—prefer explicit int for analyzer clarity and stable builds).

**Bootstrap.** `BootstrapCoreInstaller` registers LiveOps and `LevelInstaller`; asset preload for definitions is described in `MainMenuLevelsIntegration-ExecPlan` via `AssetGroupProvider<LevelDefinition>`.

**Terms.** **Level ID** means the **integer** identifier agreed between remote config and client assets. **Level state** means **Blocked**, **Unlocked**, or **Complete** (plus optional **Tease** for UI). **Progression** means the ordered list from config, not arbitrary completion order.

## Explicit type sketches (keep config and persistence simple)

The following are **plain-language shapes** the implementation must follow. Names may match exactly or differ by prefix, but **field count and responsibility** should stay this small unless a later ExecPlan expands them. Do not add “maybe useful” columns to **config** or **persistence**. **Level** types below **do not** inherit **`MultiProgressModuleData`** (Tutorial types elsewhere still may, until a separate change).

**Level persistence (player data, minimal).** Only what must survive across sessions for progression and optional UX restore.

    class LevelPersistence : IGameModuleData
        Key = "LevelPersistence" (or existing convention)
        CompletedLevelIds : List<int>   // distinct; server normalizes order
        LastSelectedLevelId : int?      // optional; null if unused

**Level config (remote config, minimal).** What the server needs to know which levels exist and how rewards work. Schedule can stay a single list or a follow-up milestone.

    class LevelConfig : IGameModuleData
        Key = "LevelConfig"
        OrderedLevelIds : List<int>     // main track order
        RewardPerComplete : long        // or keep existing Reward naming
        // Optional later: TeaseLevelIds, DaySchedule, etc.—omit until required

**Aggregated snapshot to the client (after server derivation).** This can remain `LevelGameData` or a renamed type; it should expose **derived** states, not duplicate raw persistence rows.

    class LevelGameData : IGameModuleData
        Key = "LevelGameData"
        OrderedLevelIds : IReadOnlyList<int>     // copy or view of config order
        States : IReadOnlyList<LevelStateEntry>  // one per ordered id, same length
        RewardPerComplete : long

    class LevelStateEntry
        LevelId : int
        State : LevelAvailabilityState           // Blocked | Unlocked | Complete (+ Tease if used)

**Complete level (request / response, minimal wire).** Request is **only** the id. Response is **only** success or failure for v1.

    class CompleteLevelRequest : ModuleRequest<CompleteLevelResponse>
        LevelId : int

    class CompleteLevelResponse : ModuleResponse
        Succeeded : bool    // true iff server applied completion rules and persisted

**Client LiveOps level service (method to add).** The class stays the module holder; add an async method that **only** performs the network call. **No** automatic refresh of cached `LevelGameData` in this milestone.

    class LevelService : GameClientModuleBase<LevelGameData>
        Task<CompleteLevelResponse> CompleteLevelAsync(int levelId, CancellationToken cancellationToken)
            // body: return await liveOps.CallAsync(new CompleteLevelRequest(levelId), cancellationToken);
            // do not add gameplay or menu wiring here yet

**Unified row (for UI and gameplay pickers; separate from DTO).** Lives in a Unity-facing assembly; not sent over LiveOps wire as part of complete-level.

    class AvailableLevel
        LevelId : int
        Definition : LevelDefinition   // null if asset missing
        State : LevelAvailabilityState
        IsTease : bool                 // optional

## Plan of Work

**1. ID contract.** Add **`int`** (or **`LevelId`**) to `LevelDefinition` **or** document a single unambiguous mapping strategy. Every `LevelDefinition` that should participate in LiveOps progression must match **exactly one** entry in `LevelConfig.Levels` (or the successor field name).

**2. Simplify persistence (DTO + JSON).** Replace broad `MultiProgressModuleData` usage for level persistence with a type that serializes **only**:
   - **Completed level IDs** (distinct set or list; server normalizes),
   - Optionally **`LastSelectedLevelId`** (nullable int) for restoring highlight or quick-retry.

   Keep **`Key`** semantics compatible with existing player data caches where possible; if breaking change is unavoidable, add **version field** and migration in Cloud Code load path.

**3. Config shape.** Extend or replace `LevelConfig` so it carries:
   - **Ordered** level IDs for the main track,
   - **Reward** (if retained),
   - **Schedule / days** as required by product: e.g. parallel arrays, list of `{ day, levelId }`, or remote-config JSON the team already uses—**the plan requires one concrete schema** in code comments and tests (choose the smallest structure that supports “available days and levels”).

**4. Server derivation.** In Cloud Code **`Initialize`**, build the object sent to the client (whether `LevelGameData` refactored or a new `IGameModuleData` type) by:
   - Reading persistence + config,
   - Computing **per configured ID** a **`LevelAvailabilityState`** (and **Tease** flag if configured),
   - Avoiding redundant **per-level** progress rows unless still needed for non-level flags.

   Reimplement **`CompleteLevelRequest`** validation using the **same** derivation (single implementation shared by init and complete). Return **`CompleteLevelResponse`** with **`Succeeded == true`** only when persistence updated; otherwise **`Succeeded == false`** and set **`ModuleResponse`** status/message appropriately.

**5. DTO: complete level pair.** Align **`CompleteLevelRequest`** / **`CompleteLevelResponse`** with **Explicit type sketches**. Remove or stop relying on **`LevelGameData`** inside **`CompleteLevelResponse`** for v1. Update any code that assumed the old response shape (Cloud Code handler, tests, and future client callers).

**6. Client DTO consumption.** Update `LevelGameData` (or new type) so the Unity client receives **explicit** states, not raw `ModuleProgress` lists, unless backward compatibility requires a transition period.

**7. Unified service (new assembly or existing `Madbox.Levels` / `Madbox.Level` per boundaries).** Implement **`IAvailableLevelsService`** (final name TBD) that:
   - Depends on **`ILiveOpsService`** (or narrow interface) for module data,
   - Depends on **`ILevelDefinitionCatalogService`** or `IAssetGroupProvider<LevelDefinition>` for loaded definitions,
   - Builds **`IReadOnlyList<AvailableLevel>`** (name TBD): for each configured ID in **remote order**, attach the matching `LevelDefinition` by ID, attach **state** from module data, **skip or null** definitions missing locally with a **logged warning** (policy: hide vs show locked placeholder—record in Decision Log).

   Register the service in a **Container** installer called from **`BootstrapCoreInstaller`** after LiveOps and after level definitions are registered (order matters).

`**8. Main menu and tests.** Inject the unified service into **`MainMenuViewModel`** (or a smaller presenter), bind UI to **available levels**, and add **EditMode** tests: pure C# tests for **state derivation** on the server side (if testable in isolation), client tests for **mapping** with fake LiveOps data + fake definition lists.

**9. Documentation.** Update `Docs/Core/LiveOpsLevel.md`, `Docs/Core/Levels.md`, and this plan’s **Progress** when behavior is final.

## Concrete Steps

All commands assume PowerShell on Windows and repository root `c:\Unity\Madbox` (adjust if your path differs).

1. Implement DTO + Cloud Code + client module changes behind clear commits.
2. Add or update unit tests per `Docs/AutomatedTesting.md`.
3. Run:

    .\.agents\scripts\validate-changes.cmd

   Expect analyzer checks and tests passing with no errors.

4. Manual: Play Mode → main menu shows unified list; changing **mocked** LiveOps data changes which rows are playable.

## Validation and Acceptance

**Automated.** New tests demonstrate: (a) given ordered config and completed set, **derived states** match the rules in **Decision Log**; (b) unified client mapping joins definitions and states without duplicate IDs; (c) regression around **`InitializeAsync`** for `Madbox.Level` module still passes or is intentionally updated; (d) **Tutorial** module behavior unchanged (still uses **`MultiProgressModuleData`**). **`validate-changes.cmd`** completes cleanly.

**Manual.** With two definitions and config `1,2`, completing `1` remotely (or dev stub) shows `1` complete, `2` unlocked, and UI uses **one** API.

## Idempotence and Recovery

Prefer **additive** DTO fields first, then remove dead properties. If player data migration fails, **fail closed** (safe defaults: treat as no completed levels) and log—document exact behavior in **Decision Log**.

## Artifacts and Notes

Indicative paths (current tree):

- `LiveOps/Project/Modules/Level/LevelService.cs`
- `LiveOps/LiveOps.DTO/Modules/Level/LevelGameData.cs`, `LevelConfig.cs`, `LevelPersistence.cs` (**stop inheriting** `MultiProgressModuleData`)
- `LiveOps/LiveOps.DTO/Modules/Common/MultiProgressModuleData.cs` (**unchanged** in this plan; still used by Tutorial)
- `Assets/Scripts/Core/LiveOpsLevel/Runtime/LevelService.cs`
- `Assets/Scripts/Meta/Levels/Runtime/LevelDefinition.cs`
- `Plans/MainMenuLevelsIntegration/MainMenuLevelsIntegration-ExecPlan.md`

Example acceptance scenario (indented narrative):

    Config levels: [1,2,3,4]. Persistence completed: [1]. Expected states: 1 Complete, 2 Unlocked, 3 Blocked, 4 Blocked.

## Interfaces and Dependencies

**Unified client contract** (same as in **Explicit type sketches**; C#-style for copy/paste reference):

    namespace Madbox.Levels.Progression
    {
        public enum LevelAvailabilityState
        {
            Blocked = 0,
            Unlocked = 1,
            Complete = 2,
        }

        public sealed class AvailableLevel
        {
            public int LevelId { get; }
            public LevelDefinition Definition { get; }
            public LevelAvailabilityState State { get; }
            public bool IsTease { get; }
        }

        public interface IAvailableLevelsService
        {
            IReadOnlyList<AvailableLevel> GetAvailableLevels();
        }
    }

**Complete-level call path.** `Madbox.Level.LevelService.CompleteLevelAsync` depends on **`ILiveOpsService`** (injected by existing `LevelInstaller` / parent scope). It does **not** depend on **`IAvailableLevelsService`** for the request payload—only **`int levelId`** is required.

**Assembly boundaries.** `LevelDefinition` is Unity **Meta** (`Madbox.Levels`); LiveOps DTOs must remain **free of UnityEngine** in `LiveOps.DTO`. The unified service likely lives in **Core** or **Meta** with explicit `.asmdef` references—**do not** reference Unity assets from `LiveOps.DTO`.

---

**Revision history**

- 2026-03-22: Initial ExecPlan authored from repository inspection and user specification; progression rule and unified client contract recorded in **Decision Log**.

- 2026-03-22: Added **Explicit type sketches** (minimal config, persistence, complete-level DTOs, `LevelService.CompleteLevelAsync`); **Decision Log** entries for bool-only **`CompleteLevelResponse`** and request-by-id only; clarified deferred wiring for post-complete data refresh.

- 2026-03-22: (Superseded) Earlier draft added **repo-wide deletion** of **`MultiProgressModuleData.cs`** and **Tutorial** migration in the same plan.

- 2026-03-22: **Scoped** `MultiProgressModuleData` to **Level only**: Level DTOs stop using the base; **Tutorial** and **`MultiProgressModuleData.cs`** stay until a **later** refactor; removed former step 10, Progress item, and deletion-focused validation.

- 2026-03-22: **Implementation pass:** DTO + Cloud Code + client **`LevelService`**, **`IAvailableLevelsService`**, docs, tests; **`validate-changes`** may require closing Unity for compilation precheck.
