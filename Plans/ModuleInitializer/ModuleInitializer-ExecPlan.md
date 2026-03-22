# Module initializer and client game modules (LiveOps GameDataRequest)

This ExecPlan is a living document. The sections `Progress`, `Surprises & Discoveries`, `Decision Log`, and `Outcomes & Retrospective` must be kept up to date as work proceeds.

This document must be maintained in accordance with `PLANS.md` at the repository root.

## Purpose / Big Picture

Today the Unity client can call LiveOps through `ILiveOpsService.CallAsync` with any `ModuleRequest<TResponse>`, and the backend already exposes a `GameDataRequest` entry point that aggregates `IGameModuleData` instances into a `GameDataResponse` (`LiveOps/Project/Core/GameModule/GameModulesController.cs`). What is missing is a **first-class client story**: a small, repeatable way to perform the **initial (and follow-up) game data fetch**, push the resulting `GameData` into **typed client modules**, and provide a **reference Ads-shaped module** living under `Assets/Scripts/Core/` so other features can copy the pattern.

After this work, a developer can register client modules in DI, and the **first `GameDataRequest` runs automatically during scope bootstrap** because **`LiveOpsService` implements `Madbox.Scope.Contracts.IAsyncLayerInitializable`** (`Assets/Scripts/Infra/Scope/Runtime/Contracts/IAsyncLayerInitializable.cs`) and performs the fetch (and optional fan-out to registered modules) inside **`InitializeAsync`**. There is **no** separate `IGameModulesInitializationService` or other orchestrator type. Automated tests prove payload mapping, dispatch, and initializer wiring using mocks where appropriate (no dashboard required for the unit-level behavior).

This builds on the LiveOps plumbing described in `Plans/LiveOps-Barebones-UGS-Plumbing/LiveOps-Barebones-UGS-Plumbing-ExecPlan.md` (typed DTO DLL, `Madbox.LiveOps`, Cloud Code module name `LiveOps`). That plan is **not** assumed to be re-read during execution; the **Interfaces and Dependencies** section below restates what matters for this feature.

## Progress

- [x] (2026-03-21) Author initial ExecPlan (ModuleInitializer scope, Ads sample module, LiveOps helper API).
- [x] (2026-03-21) Backend **`GameDataRequest`**: treat **null or empty `ModuleKeys`** as **no filter** (all server modules) in `LiveOps/Project/Core/GameModule/GameModulesController.cs`.
- [x] (2026-03-21) **`Madbox.LiveOps`**: references **`Madbox.Scope`**, **`Madbox.Ugs`**, **`VContainer`**; **`LiveOpsService`** implements **`IAsyncLayerInitializable`**, **`GetGameDataAsync`**, **`RefreshGameDataAsync`**; constructor-injected **`IEnumerable<IGameClientModule>`**; helper **`LiveOpsServiceDispatch`** (same file) for Roslyn ordering / static rules.
- [x] (2026-03-21) **`IGameClientModule`** in **`Madbox.LiveOps`**; **`GameClientModuleBase<T>`** in **`Madbox.GameModules`**; tests under **`Madbox.GameModules.Tests`**.
- [x] (2026-03-21) **Bootstrap:** **`LiveOpsInstaller`** from **`BootstrapCoreInstaller`**; **`AdsInstaller`** runs **before** **`LiveOpsInstaller`**; **`BootstrapScope`** sample **`WatchAd`** uses **`AdsClientModule.WatchAdAsync(ILiveOpsService)`** (avoids **`LiveOpsService` ↔ `AdsClientModule`** DI cycle).
- [x] (2026-03-21) **`Madbox.Ads`** module + **`Docs/Core/Ads.md`**, **`Docs/Core/GameModules.md`**, updated **`Docs/Core/LiveOps.md`**, **`Architecture.md`** module doc map.
- [x] (2026-03-21) **Tests:** **`GetGameDataAsync`**, **`LiveOpsInitializationTests`** (VContainer + mock cloud), **`AdsClientModuleTests`**, **`GameClientModuleBaseTests`**.
- [ ] Run `.agents/scripts/validate-changes.cmd` when the workspace compiles cleanly (precheck may fail if unrelated `.csproj` references missing files, or if Unity holds the project lock). Commit when green.

## Surprises & Discoveries

- Observation: **Empty `ModuleKeys`** on the server previously skipped every module; client “load all” needs **`new GameDataRequest()`** (parameterless) aligned with **`useKeyFilter`** on the server.
  Evidence: `GameModulesController.ProcessModulesSequentially` now uses `keys != null && keys.Count > 0` before filtering.

- Observation: **`LiveOpsService`** constructor-injecting **`IEnumerable<IGameClientModule>`** while **`AdsClientModule`** used **`ILiveOpsService`** creates a **VContainer cycle**; **`WatchAdAsync(ILiveOpsService)`** as an argument breaks the cycle.
  Evidence: Bootstrap sample and `AdsClientModule` API.

- Observation: **SCA0002 / SCA0014** led to **`LiveOpsServiceDispatch`** as an **`internal static`** helper in the same source file as **`LiveOpsService`** so the analyzer solution sees one compilation unit.
  Evidence: `check-analyzers.ps1` clean for `LiveOpsService.cs` after refactor.

## Decision Log

- Decision: Treat `C:\Unity\Scaffold\Assets\Scripts\Meta\GameModules\Runtime` as a **behavioral reference** (orchestration + typed module base + Ads sample), not as something to copy file-for-file into Madbox paths.
  Rationale: Scaffold uses different DI, namespaces, and `MonoBehaviour`-based `GameModule<T>`, which conflicts with Madbox **Core** rules (no UnityEngine in domain Core assemblies).
  Date/Author: 2026-03-21 / Agent

- Decision: Implement client module bases as **plain C# types** registered through VContainer, not as `MonoBehaviour` subclasses in Core.
  Rationale: `Architecture.md` states Core/domain assemblies must not depend on UnityEngine APIs; `Assets/Scripts/Meta/Gold/Runtime/Madbox.Gold.asmdef` already uses `noEngineReferences: true` as a precedent for non-Unity Core/Meta-style modules.
  Date/Author: 2026-03-21 / Agent

- Decision: Place the Ads **sample module** under `Assets/Scripts/Core/Ads/` (Runtime / Container / Tests) rather than under `Assets/Scripts/Meta/GameModules/Implementation/Modules/Ads` like Scaffold.
  Rationale: User direction to make Ads a first-class Core module in this repo; mirror the structural pattern used by `Assets/Scripts/Core/LiveOps/`.
  Date/Author: 2026-03-21 / Agent

- Decision: **No `IGameModulesInitializationService` and no extra orchestrator type.** **`LiveOpsService`** implements **`IAsyncLayerInitializable`** and owns the initial **`GameDataRequest`** plus fan-out to **`IEnumerable<IGameClientModule>`**.
  Rationale: User request to avoid a separate service type; one class already performs LiveOps calls.
  Date/Author: 2026-03-21 / Agent

- Decision: Define **`IGameClientModule`** in **`Madbox.LiveOps`** (Runtime), not in **`Madbox.GameModules`**.
  Rationale: **`Madbox.GameModules`** references **`Madbox.LiveOps`** for **`ILiveOpsService`**. If **`LiveOpsService`** referenced **`Madbox.GameModules`** for the module interface, that would create a **circular** assembly dependency. Keeping the small **client-module contract** next to **`ILiveOpsService`** breaks the cycle; **`GameClientModuleBase<T>`** lives in **`Madbox.GameModules`** and implements **`IGameClientModule`**.
  Date/Author: 2026-03-21 / Agent

- Decision: **`Madbox.LiveOps` Runtime references `Madbox.Scope`** so **`LiveOpsService`** can implement **`IAsyncLayerInitializable`**. This adds a **Core Runtime → Infra** assembly edge; accept for this feature or revisit by splitting a tiny contracts-only assembly later.
  Rationale: The interface type lives in Scope today; user chose not to add another implementing type.
  Date/Author: 2026-03-21 / Agent

- Decision: Invoke **`LiveOpsInstaller` from `BootstrapCoreInstaller`**, not **`BootstrapInfraInstaller`**, so **`LiveOpsService.InitializeAsync`** runs in the **Core** layer **after** the Infra layer’s initializers (for example **`Ugs`**) complete, and the **Core** container holds registrations for **`IGameClientModule`** implementations that **`InitializeAsync`** must resolve.
  Rationale: **`LayerInstallerBase`** runs a layer’s **`IAsyncLayerInitializable`** instances **before** building **child** scopes. If **`LiveOpsService`** stayed on the Infra scope, its **`InitializeAsync`** would run **before** **`BootstrapCoreInstaller`** created the Core child, so **`IEnumerable<IGameClientModule>`** registered only on Core would **not** exist yet. Moving **LiveOps** registration to **Core** fixes ordering while keeping **Ugs** and **Cloud Code** on Infra; the Core scope’s resolver still resolves **`IUgs`** and **`ICloudCodeModuleService`** from the parent when those are registered on the parent scope (VContainer **LifetimeScope** child inheritance behavior—verify during implementation and adjust if this project uses a non-inheriting child scope).
  Date/Author: 2026-03-21 / Agent

- Decision: At the **start** of **`LiveOpsService.InitializeAsync`**, ensure **UGS is initialized** before any Cloud Code call when **`IUgs`** is registered and implements **`IAsyncLayerInitializable`** (for example **`await ((IAsyncLayerInitializable)ugs).InitializeAsync(resolver, cancellationToken)`** or an equivalent idempotent gate). This guards against future changes that register **`LiveOpsService`** as **`IAsyncLayerInitializable`** on the **same** layer as **`Ugs`** (parallel **`Task.WhenAll`**); if **LiveOps** and **Ugs** always land on **different** layers as above, the extra await is still a **cheap idempotent** safety net.
  Rationale: **`LayerInstallerBase`** runs pending initializers in the **same** layer **in parallel**; Cloud Code requires UGS to be ready.
  Date/Author: 2026-03-21 / Agent

## Outcomes & Retrospective

Shipped: initial **`GameDataRequest`** on bootstrap via **`LiveOpsService` + `IAsyncLayerInitializable`**, **`IGameClientModule`** pipeline, **`Madbox.GameModules`** base, **`Madbox.Ads`** sample, server fix for empty module keys, documentation and EditMode tests.

Deferred / environment: full **`validate-changes.cmd`** may still report unrelated compilation or Unity lock issues on some machines; re-run when the repo’s Unity project is closed and backend `.csproj` inputs are consistent.

## Context and Orientation

**Reference project (on disk, outside this repo by default):** `C:\Unity\Scaffold\Assets\Scripts\Meta\GameModules\Runtime` contains:

- `Abstraction/IGameModule.cs` — client modules expose `DataModule`, `Initialize(GameData)`, and `UpdateData(IGameModuleData)`.
- `Abstraction/GameModule.cs` — abstract `MonoBehaviour` base `GameModule<T>` where `T : IGameModuleData`.
- `Implementation/GameModulesController.cs` — implements `IGameModulesService`: calls the cloud layer for an initial aggregate payload, stores `GameData`, then runs `Initialize` on each module; `FetchModuleData` issues a narrower `GameDataRequest` and routes responses back to modules by matching **CLR types** of `IGameModuleData`.

**Madbox backend (in-repo):** `LiveOps/Project/Core/GameModule/GameModulesController.cs` defines a Cloud Code function attributed with `[CloudCodeFunction(nameof(GameDataRequest))]` that builds a `GameData`, runs each **server-side** `IGameModule`’s `Initialize`, and returns `GameDataResponse`. The client already uses the same DTO names: `GameDataRequest` has `ModuleKeys` (`LiveOps/LiveOps.DTO/Core/ModuleRequest/Implementation/GameDataRequest.cs`). `ILiveOpsService` (`Assets/Scripts/Core/LiveOps/Runtime/LiveOpsService.cs`) wraps `ICloudCodeModuleService` and sends payload shape `{ "request": <serialized ModuleRequest> }`, which matches Cloud Code bindings used elsewhere.

**Bootstrap integration:** `IAsyncLayerInitializable` defines `Task InitializeAsync(IObjectResolver resolver, CancellationToken cancellationToken)`. `LayerInstallerBase` resolves `IEnumerable<IAsyncLayerInitializable>` from the **current** `LifetimeScope` container and runs **pending** entries (skipping instances already recorded in the shared registry). The app’s layer tree is built in `BootstrapScope` (Infra child then Core child). With **LiveOps** registered on **Core**, **`LiveOpsService.InitializeAsync`** runs during **Core** layer initialization, **after** **Infra** initializers such as **`Ugs`** have completed.

**Important implementation detail for “load everything”:** In `ProcessModulesSequentially`, the server passes `request.ModuleKeys` as the filter collection. If `ModuleKeys` is a **non-null empty array**, the condition `filterKeys != null && !filterKeys.Contains(gameModule.Key)` skips **every** module. The plan must either (a) change the server to treat empty as “no filter”, or (b) require the client to pass an explicit list of keys, or (c) use `null` for “all modules” if the DTO is extended—pick one approach and add a regression test on the side you change.

**Terminology:**

- **Game data** means an instance of `GameModuleDTO.GameModule.GameData` holding a list of `IGameModuleData` instances serialized with type information (`TypeNameHandling.Auto` on the list).
- **Client game module** means a small object that receives that data during startup or refresh and exposes strongly typed state to the rest of the app (for example `AdsModuleData`).
- **Module initializer** here means **`LiveOpsService.InitializeAsync`**: Cloud Code game data fetch plus applying results to registered **`IGameClientModule`** instances.

## Plan of Work

First, align the meaning of `GameDataRequest.ModuleKeys` between client and server so that an “initial load” does what product expects (typically: all registered modules). Update DTOs only if you choose a contract change; prefer backward-compatible server behavior when possible.

Second, add **`Madbox.Scope`** to **`Madbox.LiveOps.asmdef`**. On **`LiveOpsService`**, implement **`IAsyncLayerInitializable`**. In **`InitializeAsync`**, await an **idempotent UGS gate** (see Decision Log), then perform **`GameDataRequest`** (prefer a dedicated **`GetGameDataAsync`** on **`ILiveOpsService`** that shares implementation with **`CallAsync`**), then **`TryResolve` / resolve `IEnumerable<IGameClientModule>`** and initialize each module from **`response.GameData`**. Optionally add **`RefreshGameDataAsync`** on **`ILiveOpsService`** for partial module-key refresh using the same dispatch logic as **`InitializeAsync`** (Scaffold-style **`FetchModuleData`**), still implemented **only** on **`LiveOpsService`**.

Third, add **`IGameClientModule`** to **`Assets/Scripts/Core/LiveOps/Runtime/`** (namespace **`Madbox.LiveOps`**). Add **`Madbox.GameModules`** under **`Assets/Scripts/Core/GameModules/`** with **`noEngineReferences: true`**: **`GameClientModuleBase<T>`** implements **`IGameClientModule`**. **`GameModulesInstaller`** (Container assembly without **`Madbox.Scope`** unless needed) registers concrete modules **as `IGameClientModule`**; **`BootstrapCoreInstaller`** calls **`LiveOpsInstaller`** and **`GameModulesInstaller`** (and **`AdsInstaller`** when present).

Fourth, **move `LiveOpsInstaller` invocation** from **`BootstrapInfraInstaller`** to **`BootstrapCoreInstaller`**. Confirm **`ILiveOpsService`** resolution from scopes that only built Infra still works for any code paths that need it—if something required LiveOps during Infra **Install** only, adjust; typically resolution happens after full bootstrap and remains valid.

Fifth, update **`LiveOpsInstaller`** to register **`LiveOpsService`** as **`ILiveOpsService`** and **`IAsyncLayerInitializable`** (single scoped instance).

Sixth, create **`Assets/Scripts/Core/Ads/`** as a full module: Runtime, Container, Tests, **`Docs/Core/Ads.md`**. **`AdsClientModule`** derives from **`GameClientModuleBase<AdsModuleData>`**; **`WatchAd`** uses **`ILiveOpsService.CallAsync(new WatchAdRequest())`** (or **`GetGameDataAsync`** is not used for watch). Document that **`AdsConfigData`** is not populated from **`GameData`** on the current server unless a backend change adds it.

Seventh, document **`Docs/Core/GameModules.md`**, **`Docs/Core/LiveOps.md`**, and **`Docs/Core/Ads.md`** (per **`AGENTS.md`**).

## Concrete Steps

All commands assume Windows PowerShell; working directory is the repo root `C:\Unity\Madbox` unless noted.

1. **Inspect and fix module key filtering** on the server if needed (`LiveOps/Project/Core/GameModule/GameModulesController.cs`).

2. **Implement `LiveOpsService` + `IGameClientModule` + `Madbox.GameModules` + bootstrap moves** as above.

3. **Implement `Assets/Scripts/Core/Ads/`** with `.asmdef` split consistent with **`LiveOps`**.

4. **Run tests:** `powershell -NoProfile -ExecutionPolicy Bypass -File ".\.agents\scripts\run-editmode-tests.ps1"` and `.agents/scripts/validate-changes.cmd`.

## Validation and Acceptance

- **LiveOps `GetGameDataAsync`:** Mock **`ICloudCodeModuleService`**; assert module **`LiveOps`**, function **`GameDataRequest`**, payload shape.

- **Initializer:** Cast **`LiveOpsService`** to **`IAsyncLayerInitializable`** (or resolve from test container), invoke **`InitializeAsync`** with mocks; assert **`GameDataRequest`** and that stub **`IGameClientModule`** instances received data.

- **Ads sample:** Assert **`WatchAdRequest`** path and local **`AdsModuleData`** update from **`WatchAdResponse`**.

- **Gate:** `.agents/scripts/validate-changes.cmd` clean.

## Idempotence and Recovery

Additive bootstrap and registration changes. If **`LiveOps`** move to Core breaks an edge case, restore **`LiveOpsInstaller`** to Infra temporarily and introduce a **child-scope-only** registration pattern only after resolver inheritance is verified.

## Artifacts and Notes

Madbox uses **`GameDataRequest`** / **`GameDataResponse`** on the server; there is **no** `InitializeGameModulesRequest` in the current DTO set unless product adds one.

## Interfaces and Dependencies

**In `Madbox.LiveOps` Runtime (`Assets/Scripts/Core/LiveOps/Runtime/`):**

- **`IGameClientModule`**: **`string Key { get; }`**, **`IGameModuleData DataModule { get; }`**, **`Task InitializeFromAsync(GameData gameData)`**, **`void ApplyUpdate(IGameModuleData data)`** (names flexible).
- **`ILiveOpsService`**: existing **`CallAsync`**; add **`GetGameDataAsync`** (and optional **`RefreshGameDataAsync`**).
- **`LiveOpsService`**: implements **`ILiveOpsService`** and **`IAsyncLayerInitializable`**; owns fetch + fan-out.

**`Madbox.LiveOps.asmdef`:** add reference **`Madbox.Scope`**.

**In `Madbox.GameModules` (`Assets/Scripts/Core/GameModules/`):**

- **`GameClientModuleBase<T>`** : **`IGameClientModule`** where **`T : class, IGameModuleData`**.

**In `Madbox.GameModules.Container` (if used):**

- Registers **`IGameClientModule`** implementations only (no **`IAsyncLayerInitializable`** here).

**In `Madbox.Ads`:**

- **`AdsClientModule`** + installer; references **`Madbox.GameModules`** and **`Madbox.LiveOps`** as needed.

**Bootstrap:**

- **`BootstrapInfraInstaller`:** **Ugs**, **Cloud Code** (no **LiveOps**).
- **`BootstrapCoreInstaller`:** **`LiveOpsInstaller`**, **`GameModulesInstaller`**, feature installers (**Ads**).

---

Revision: 2026-03-21 — Initial authoring from user request and comparison with Scaffold `GameModules` runtime and Madbox LiveOps backend/DTO layout.

Revision: 2026-03-21 — Marked ExecPlan authoring step complete in `Progress`.

Revision: 2026-03-21 — Integrated initial `GameDataRequest` with `IAsyncLayerInitializable`, Core-vs-Infra layer ordering, Bootstrap adapter placement, and architecture note (implement interface, do not add Core → `Madbox.Scope` dependency on the GameModules runtime assembly).

Revision: 2026-03-21 — Dropped the separate Bootstrap adapter: **one Container-located concrete type** implements both `IGameModulesInitializationService` and `IAsyncLayerInitializable`; **`Madbox.GameModules.Container`** references **`Madbox.Scope`**; **`BootstrapCoreInstaller`** runs **`GameModulesInstaller`**.

Revision: 2026-03-21 — **User direction:** No **`IGameModulesInitializationService`**. **`LiveOpsService`** implements **`IAsyncLayerInitializable`** and performs **`GameDataRequest`** + module fan-out. **`IGameClientModule`** defined in **`Madbox.LiveOps`**. **`LiveOpsInstaller`** moves to **`BootstrapCoreInstaller`** so **`InitializeAsync`** can resolve Core-registered modules; **`Madbox.LiveOps`** references **`Madbox.Scope`**.

Revision: 2026-03-21 — **Executed:** Implemented **`Madbox.GameModules`**, **`Madbox.Ads`**, **`LiveOpsService`** extensions, server **`ModuleKeys`** filter fix, docs, tests; **`LiveOpsServiceDispatch`** for analyzer compliance; **`Madbox.Bootstrap`** references **`Madbox.GameModules`** for **`AdsClientModule`** inheritance visibility.

Revision: 2026-03-22 — **Superseded assembly split:** **`GameClientModuleBase<T>`** and **`GameClientModuleBaseTests`** moved into **`Madbox.LiveOps`** (`Assets/Scripts/Core/LiveOps/`); **`Madbox.GameModules`** assembly and **`Docs/Core/GameModules.md`** removed. Feature modules (**Ads**, **Tutorial**, **Level**) depend only on **`Madbox.LiveOps`**; no circular dependency because **`LiveOpsService`** does not reference concrete feature assemblies.
