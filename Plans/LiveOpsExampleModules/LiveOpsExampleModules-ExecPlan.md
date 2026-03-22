# LiveOps example modules: Ads, Tutorial, and Level (single pipeline)

This ExecPlan is a living document. The sections `Progress`, `Surprises & Discoveries`, `Decision Log`, and `Outcomes & Retrospective` must be kept up to date as work proceeds.

This document must be maintained in accordance with `PLANS.md` at the repository root.

## Purpose / Big Picture

The Unity client loads aggregated **`GameData`** from Cloud Code via **`GameDataRequest`** during **`LiveOpsService.InitializeAsync`** (`Assets/Scripts/Core/LiveOps/Runtime/LiveOpsService.cs`). **`GameModulesController`** on the backend (`LiveOps/Project/Core/GameModule/GameModulesController.cs`) runs each registered server **`IGameModule`**, collects every returned **`IGameModuleData`**, and returns **`GameDataResponse`**.

This plan **simplifies** the example modules (**Ads**, **Tutorial**, **Level**) to **one pipeline per feature**: there is **no** separate optional **`GameModule<TConfig>`** and **no** second **`IGameModuleData`** entry for “config only.” Each feature exposes **one** main type—**`*GameData`**—that is what the client receives. On the server, a single **`GameModule<TGameData>`** implementation (named **`*Service`** in the examples below) loads **persistence** and **remote config**, **builds** that **`TGameData`**, and returns it from **`Initialize`**. On the client, a matching type may extend **`GameClientModuleBase<TGameData>`** (`Assets/Scripts/Core/LiveOps/Runtime/GameClientModuleBase.cs`, assembly **`Madbox.LiveOps`**): it implements **`IAsyncLayerInitializable`**; **`InitializeAsync`** resolves **`ILiveOpsService`**, calls **`GetModuleData<TGameData>()`**, and assigns the **`protected T data`** field (no **`DataModule`** property, no **`ApplyUpdate`**). **`IGameClientModule`** is only a **marker** with **`Key`** for DI. **`LiveOpsService` does not fan out** to client modules and **does not** expose a “wait for other service” API—**bootstrap layer ordering** must run **`LiveOpsService`** before modules that read **`GetModuleData`**. **`LiveOpsService.InitializeAsync`** only runs **`GameDataRequest`** and stores **`GameData`** internally; any code (including **`GameClientModuleBase`** or direct callers) reads via **`liveOpsService.GetModuleData<T>()`** (for example **`GetModuleData<TutorialModuleData>()`** per final DTO names).

After this work, the three features share the same mental model: **`*Persistence`** (player state for that module), **`*Config`** (designer / Remote Config settings), **`*GameData`** (the payload shipped in **`GameData`**). Example numeric IDs and reward values remain **sample data**; wiring LiveOps examples to the production client’s real content is **out of scope** for this milestone.

## Progress

- [x] (2026-03-22) Author ExecPlan; revise to **single-pipeline** model (no **`GameModule<TConfig>`**).
- [x] (2026-03-23) **Client plumbing (done in repo):** **`GameClientModuleBase<T>`** (assembly **`Madbox.LiveOps`**) implements **`IAsyncLayerInitializable`**, uses **`protected T data`**, **`IGameClientModule`** = **`Key`** only; **`LiveOpsService`** stores **`GameData`**, **`GetModuleData<T>()`** only (no cross-service wait/sync API, no fan-out to client modules). **`AdsClientModule`** matches this pattern. Docs: **`LiveOps.md`** (includes base class), **`Ads.md`**.
- [x] **DTOs (`LiveOps/LiveOps.DTO/`):** **`TutorialGameData`**, **`TutorialPersistence`**, **`TutorialConfig`**, **Level** / **Ads** equivalents (**`*GameData`**, **`*Persistence`**, **`*Config`**); single **`IGameModuleData`** per feature in **`GameData`**.
- [x] **Server (`LiveOps/Project/Modules/`):** **`TutorialService`**, **`LevelService`**, **`AdsService`** as **`GameModule<T*GameData>`**; **`ModuleConfig`** updated; old **`*Module` / `*ConfigModule`** files removed for those features.
- [x] **Client (`Assets/Scripts/Core/`):** **`Madbox.Tutorial`** / **`Madbox.Level`** (**`LiveOpsLevel/`**) **`TutorialService`** / **`LevelService`** + installers; **`AdsClientModule`** uses **`AdsGameData`**; **`BootstrapCoreInstaller`** registers **`LiveOpsInstaller`** before Tutorial / Level / Ads.
- [x] **Tests:** **`LiveOpsInitializationTests`**, **`AdsClientModuleTests`** updated; **`TutorialServiceTests`**, **`LevelServiceTests`** added.
- [x] **Docs:** **`Docs/Core/Tutorial.md`**, **`Docs/Core/LiveOpsLevel.md`**, **`Docs/Core/Ads.md`**, **`Architecture.md`** module map (**`LiveOps.md`** includes **`GameClientModuleBase`**).
- [ ] Run **`.agents/scripts/validate-changes.cmd`** when Unity is not locking the project; commit when green.

## Surprises & Discoveries

- Observation: (Fill during implementation.)
  Evidence: (Fill during implementation.)

## Decision Log

- Decision: **One pipeline per feature.** Exactly **one** **`GameModule<TGameData>`** on the server and **one** **`GameClientModuleBase<TGameData>`** on the client per example (**Ads**, **Tutorial**, **Level**). **Do not** register a separate **`GameModule<TConfig>`** for remote config.
  Rationale: User direction; config and persistence feed **into** **`Initialize`**, not into separate **`GameData`** slots.
  Date/Author: 2026-03-22 / Agent

- Decision: Name the client and server module classes **`*Service`** (e.g. **`TutorialService`**) in their respective assemblies. **Namespaces** must disambiguate (e.g. **`Madbox.Tutorial`** vs server **`GameModule.Modules.Tutorial`**) so **`TutorialService`** on the client and **`TutorialService`** on the server are not confused in documentation.
  Rationale: User direction.
  Date/Author: 2026-03-22 / Agent

- Decision: **`TGameData`** is the **sole** **`IGameModuleData`** returned for that feature’s **`Initialize`** and the type the client holds in **`GameClientModuleBase<TGameData>`**. **`TPersistence`** and **`TConfig`** are **supporting** types: loaded inside **`Initialize`**, combined to populate **`TGameData`**. Use **`IRemoteConfig`** and **`IPlayerData`** with **concrete generic** calls—**no** shared base class for persistence or config beyond those interfaces.
  Rationale: User direction.
  Date/Author: 2026-03-22 / Agent

- Decision: **`LiveOpsService`** does **not** fan out to **`IGameClientModule`**. It keeps the last successful **`GameData`** in an internal field; consumers use **`GetModuleData<T>()`** to read typed module data. **`GameClientModuleBase`** implements **`IAsyncLayerInitializable`** and hydrates its **`protected T data`** from **`GetModuleData<T>()`** in its own **`InitializeAsync`**—not orchestrated by **`LiveOpsService`**.
  Rationale: User direction; matches current client flow.
  Date/Author: 2026-03-22 / Agent

- Decision: **`LiveOpsService`** does **not** expose cross-service synchronization (for example **no** “wait until initial **`GameData`** is ready” API on **`ILiveOpsService`**). Services do not validate or gate on another service’s internal state; **layer / installer ordering** defines readiness.
  Rationale: User direction.
  Date/Author: 2026-03-22 / Agent

- Decision: **`IGameClientModule`** exposes only **`Key`**. **`GameClientModuleBase<T>`** holds payload in **`protected T data`** (field), not a public **`DataModule`** or **`ApplyUpdate`** pipeline.
  Rationale: User direction; slim surface for client modules.
  Date/Author: 2026-03-23 / Agent

- Decision: **`ModuleRequest` / `ModuleResponse`** for targeted Cloud Code operations (**`WatchAdRequest`**, **`CompleteTutorialRequest`**, etc.) stay **separate** from **`GameDataRequest`**; bootstrap still delivers state through **`GameModulesController`** aggregation.
  Rationale: Unchanged; mutation endpoints are not the initialization pipeline.
  Date/Author: 2026-03-22 / Agent

- Decision: LiveOps **example** progression IDs (tutorial steps, level numbers) remain **placeholders** until a later integration ties them to the real client.
  Rationale: User direction.
  Date/Author: 2026-03-22 / Agent

## Outcomes & Retrospective

(Fill at milestone completion.)

## Context and Orientation

**End-to-end flow (Tutorial as the reference example):**

1. **Client:** **`LiveOpsService`** (implements **`IAsyncLayerInitializable`**) runs **`GameDataRequest`**, receives **`GameData`**, and **stores it internally** (for example a private **`gameData`** field). There is **no** automatic pass to **`IGameClientModule`**.
2. **Client:** **`GameClientModuleBase<TutorialGameData>.InitializeAsync`** ( **`IAsyncLayerInitializable`** ) resolves **`ILiveOpsService`** and sets **`data = liveOps.GetModuleData<TutorialGameData>()`** (same pattern as **`AdsClientModule`**). Alternatively, any code may call **`liveOps.GetModuleData<T>()`** directly. Mutation endpoints (**`CallAsync`**) update **`data`** in the concrete module (e.g. assign from **`response.Data`**)—there is **no** shared **`ApplyUpdate`** on the base.
3. **Server:** On each **`GameDataRequest`**, **`GameModulesController`** calls **`TutorialService.Initialize(context, playerData, gameState, remoteConfig)`**.
4. **Server:** **`TutorialService.Initialize`** loads **`TutorialPersistence`** from **`IPlayerData`**, loads **`TutorialConfig`** from **`IRemoteConfig`**, constructs **`TutorialGameData`**, returns it. **`GameModulesController`** adds that single **`IGameModuleData`** to **`GameData`**.

**There is no `GameModule<TConfig>` in this design.** Remote config is read **inside** the same **`GameModule<TGameData>`** that also reads persistence and emits **`TGameData`**.

**Terms:**

- **`*GameData`** — Implements **`IGameModuleData`**; this is what **`GameModule<T>`** returns. On the client, the same type is what **`GetModuleData<T>()`** returns when **`T`** matches the module payload (for example **`typeof(TutorialModuleData).Name`** aligns with **`GameModule<T>.Key`** on the server).
- **`*Persistence`** — Player-specific state for that module (shape chosen for **`IPlayerData`**; often loaded with **`GetOrSet`** using a type that can be serialized/stored per player).
- **`*Config`** — Designer / Remote Config payload for that module (**`IRemoteConfig.Get(context, new TConfig())`**).

**Related plan:** `Plans/ModuleInitializer/ModuleInitializer-ExecPlan.md` describes earlier **`GameDataRequest`** bootstrap work; this ExecPlan **supersedes** the “optional config **`GameModule`**” idea for the **Ads / Tutorial / Level** examples.

## Plan of Work

First, **design and add DTOs** for **`TutorialGameData`**, **`TutorialPersistence`**, **`TutorialConfig`**, and the parallel **Level** and **Ads** types. Migrate or remove **`TutorialModuleData`**, **`TutorialConfigData`**, **`LevelModuleData`**, **`LevelConfigData`**, **`AdsModuleData`**, **`AdsConfigData`** as appropriate so naming matches **`GameData` / `Persistence` / `Config`** and there is exactly **one** **`IGameModuleData`** per feature in **`GameData`**.

Second, **refactor server modules**: one **`GameModule<TGameData>`** per feature; delete **`TutorialConfigModule`**, **`LevelConfigModule`**, and any redundant registrations; update **`ModuleConfig`**.

Third, **refactor client**: **`TutorialService`**, **`LevelService`**, and align **`Ads`** with **`GameClientModuleBase<TGameData>`** + **`IGameClientModule`** + **`IAsyncLayerInitializable`** as needed; **`LiveOpsService`** stays **aggregation-only** (internal **`gameData`**, **`GetModuleData<T>()`** only—no wait/sync helpers). Update installers; rename **`AdsClientModule`** only if the team adopts **`*Service`** everywhere.

Fourth, **tests and docs**, then **validate-changes**.

## Concrete Steps

All commands assume Windows PowerShell; working directory is the repository root `C:\Unity\Madbox` unless noted.

1. Edit DTOs under **`LiveOps/LiveOps.DTO/`** and server modules under **`LiveOps/Project/Modules/`** per **Interfaces and Dependencies**.

2. Edit client **`Assets/Scripts/Core/`** LiveOps + Tutorial + Level + Ads assemblies and **`BootstrapCoreInstaller`**.

3. Run:

        .agents\scripts\validate-changes.cmd

## Validation and Acceptance

- **`.agents/scripts/validate-changes.cmd`** completes with no new failures.
- **EditMode tests:** Stub **`GameData`** includes **`TutorialGameData`**, **`LevelGameData`**, **`AdsGameData`** (names per final DTOs); after **`InitializeAsync`**, **`GetModuleData<T>()`** returns non-null for each **`T`**.
- **Server unit or integration tests** (where the project already tests Cloud Code modules): **`Initialize`** returns a populated **`TGameData`** when persistence and remote config stubs provide values.

## Idempotence and Recovery

- **`Initialize`** on the server should remain safe to call once per Cloud Code invocation; persistence writes stay explicit in mutation endpoints (**`CompleteTutorial`**, etc.).
- If Unity locks the project, close the Editor and re-run **validate-changes**.

## Artifacts and Notes

Example server **`Initialize`** shape (pseudocode, indented):

    public override async Task<IGameModuleData> Initialize(...)
    {
        TutorialPersistence persistence = await playerData.GetOrSet(context, new TutorialPersistence());
        TutorialConfig config = await remoteConfig.Get(context, new TutorialConfig());
        TutorialGameData gameData = BuildTutorialGameData(persistence, config);
        return gameData;
    }

## Interfaces and Dependencies

At completion, for **each** of **Ads**, **Tutorial**, **Level**:

**Server (Cloud Code project, e.g. `GameModule.Modules.Tutorial`):**

- **`TutorialService : GameModule<TutorialGameData>`** — **`Initialize`** fetches **`TutorialPersistence`**, **`TutorialConfig`**, builds **`TutorialGameData`**, returns it.

**Client (e.g. `Madbox.Tutorial`):**

- **`TutorialService : GameClientModuleBase<TutorialGameData>`** — implements **`IGameClientModule`** ( **`Key`** only ) and **`IAsyncLayerInitializable`**; **`InitializeAsync`** sets **`protected data`** from **`GetModuleData<TutorialGameData>()`**. **`CallAsync`**-based flows update **`data`** in the concrete type (assign from response); **no** **`ApplyUpdate`** on the base.

**Shared DTO assembly (`Madbox.LiveOps.DTO`):**

- **`TutorialGameData : IGameModuleData`**
- **`TutorialPersistence`** — type used with **`IPlayerData`** (must be compatible with existing cache/serialization rules).
- **`TutorialConfig`** — type used with **`IRemoteConfig.Get`**.

**Orchestration:**

- **`GameModulesController`** — unchanged responsibility: aggregate all **`IGameModuleData`** instances into **`GameData`**.
- **`LiveOpsService`** — runs **`GameDataRequest`** during **`InitializeAsync`** and **stores** **`GameData`** internally; exposes **`GetModuleData<T>()`** for typed reads. **Does not** iterate **`IGameClientModule`** or expose cross-service **wait** / **ready** APIs.

**Explicitly out of scope:**

- A second **`GameModule`** per feature only for config.
- Base classes for persistence or config beyond **`IRemoteConfig`** / **`IPlayerData`** generic usage.

---

Revision history:

- 2026-03-22: Initial version (split config **`GameModule`**).
- 2026-03-22: Revised to **single pipeline**: one **`GameModule<TGameData>`** / **`GameClientModuleBase<TGameData>`**, **`*Persistence`** + **`*Config`** as inputs to **`Initialize`**, **no** optional **`GameModule<TConfig>`**; client/server types named **`*Service`** with namespace disambiguation.
- 2026-03-22: **`LiveOpsService`** clarified: **no** **`IGameClientModule`** fan-out; internal **`gameData`** + **`GetModuleData<T>()`**; client **`TutorialService`** uses **`GetModuleData<TutorialModuleData>()`** (or final **`TGameData`**) after init.
- 2026-03-23: Aligned with implemented **`GameClientModuleBase`**: **`IAsyncLayerInitializable`**, **`protected T data`**, **`IGameClientModule`** = **`Key` only**, **no** **`DataModule` / `ApplyUpdate` / `InitializeFromAsync`** from LiveOps, **no** **`WaitForInitialGameDataAsync`** on **`ILiveOpsService`**; **`LiveOpsService`** aggregation-only; layer ordering for readiness.
