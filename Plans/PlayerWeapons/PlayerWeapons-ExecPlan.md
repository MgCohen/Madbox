# Player weapon authoring (Addressables prefabs, spawn service, visual selection)

This ExecPlan is a living document. The sections `Progress`, `Surprises & Discoveries`, `Decision Log`, and `Outcomes & Retrospective` must be kept up to date as work proceeds.

This document must be maintained in accordance with `PLANS.md` at the repository root.

## Purpose / Big Picture

After this work, the game can **author** which player prefab to use and which **three** weapon prefabs belong to that player, using **Addressables** for all runtime loads. **`PlayerService`** holds the **`PlayerWeaponLoadoutDefinition`** and exposes it **on demand**. **`PlayerFactory`** (used like the enemy spawn path) **loads every reference from that definition**, **spawns** the player and weapons, **assigns** them onto a **player behaviour** that owns **socket references**, and returns **only the ready player**. That behaviour exposes **visual weapon selection** (this slice does not model combat stats, damage, or input binding).

A reviewer can verify behavior by running tests and, in the Editor, seeing **only one weapon visually active** at a time while the others exist but are **disabled** (not destroyed), matching the chosen “pre-instance then enable or disable” approach.

## Progress

- [x] (2026-03-23) Aligned implementation with `Architecture.md` (App `GameView` + `Madbox.Addressables`, no Core coupling).
- [x] (2026-03-23) Defined **`PlayerWeaponLoadoutDefinition`** authoring SO (player + three weapon `AssetReference` fields).
- [x] (2026-03-23) Implemented **`PlayerService`** with **`Loadout`** and **`SetLoadout`**; registered scoped in **`BootstrapCoreInstaller`**.
- [x] (2026-03-23) Implemented **`PlayerFactory`**: loads via **`IAddressablesGateway`**, instantiates player and weapons, wires **`WeaponVisualController`**, releases handles after each load; returns **`GameObject`** only.
- [x] (2026-03-23) Added **`WeaponVisualController`** (three sockets, **`SetWeaponInstances`**, **`SetSelectedWeaponIndex`**).
- [x] (2026-03-23) Updated **`Docs/App/GameView.md`**; added EditMode tests in **`Madbox.GameView.Tests`**.
- [x] (2026-03-23) Implementation complete: loadout SO in `Madbox.Levels`, `PlayerService` + `PlayerFactory` in `Madbox.App.Bootstrap.Player`, `WeaponVisualController` in `Madbox.GameView`, DI in `BootstrapCoreInstaller`. EditMode tests pass for `Madbox.Bootstrap.Tests` and `Madbox.GameView.Tests`. Full `validate-changes.cmd` may still report pre-existing PlayMode analyzer totals unrelated to this feature; see `Decision Log` for assembly split.
- [ ] Optional: commit when ready.

## Surprises & Discoveries

- Observation: Unity batch compilation did not resolve `UnityEngine.AddressableAssets` inside `Madbox.GameView` even with `Unity.Addressables` listed; authoring `PlayerWeaponLoadoutDefinition` was moved to **`Madbox.Levels`** (already Addressables-clean), and **`PlayerService`** / **`PlayerFactory`** were placed under **`Madbox.Bootstrap.Runtime`** next to existing Addressables bootstrap wiring.
  Evidence: `Assets/Scripts/Meta/Levels/Runtime/PlayerWeaponLoadoutDefinition.cs`, `Assets/Scripts/App/Bootstrap/Runtime/Player/`.

- Observation: **`BootstrapCoreInstaller`** registers `PlayerService` and `PlayerFactory` in the same scoped layer as LiveOps, so **`IAddressablesGateway`** resolves from the asset installer tree.
  Evidence: `Assets/Scripts/App/Bootstrap/Runtime/Layers/BootstrapCoreInstaller.cs`.

- Observation: Roslyn analyzers **SCA0002** (method order), **SCA0006** (12-line methods), **SCA0014** (static in non-static class), and **NestedCallAnalyzer** strongly constrained **`PlayerFactory`**. The implementation uses a **private nested static class** `Spawning` for static helpers and keeps a single public instance entrypoint.
  Evidence: `Assets/Scripts/App/Bootstrap/Runtime/Player/PlayerFactory.cs`.

## Decision Log

- Decision: Weapons are **prefabs** loaded exclusively through **`IAddressablesGateway`** (no direct `Addressables.Load` calls scattered in feature code).
  Rationale: Matches `Docs/Infra/Addressables.md` and keeps release and handle policy consistent.
  Date/Author: 2026-03-22 / User + agent (planning)

- Decision: After initial spawn, weapon changes are expressed as **SetSelectedWeapon(index)** and implemented by **GameObject.SetActive** (or equivalent enable/disable on the weapon root), not by destroying and reloading Addressables for each swap.
  Rationale: User requested pre-instanced weapons with enable or disable for the visual path; this minimizes load spikes and simplifies swap to O(1) activation.
  Date/Author: 2026-03-22 / User + agent (planning)

- Decision: This slice explicitly excludes **gameplay stats**, **input wiring**, and **network or persistence** of loadout. Only authoring data, spawn, parenting, and visual selection belong here.
  Rationale: Narrows scope; follow-up ExecPlans can attach stats and input without redesigning the visual anchor contract if interfaces stay thin.
  Date/Author: 2026-03-22 / User + agent (planning)

- Decision: Player-facing “definition” for the active weapon is a **runtime handle** the player (or a small coordinator component) holds: which slot index is selected plus references to the three spawned weapon roots. Persistent **ScriptableObject** assets only describe **which Addressables to load** for the player and the three weapons, not mutable combat state.
  Rationale: Avoids confusing authored assets with per-session state; keeps SOs as load lists only.
  Date/Author: 2026-03-22 / User + agent (planning)

- Decision: **`PlayerService`** is the component that **owns the reference** to **`PlayerWeaponLoadoutDefinition`** and **provides it on demand** to the game (for example when the factory or bootstrap needs to build the character). Spawning is **not** mixed into this service unless the team later collapses roles; the factory performs load and spawn.
  Rationale: Clear separation between “which loadout is configured for this session or build” and “how to materialize that loadout into instances,” matching the user’s request for direct authoring and a dedicated factory.
  Date/Author: 2026-03-23 / User + agent (planning)

- Decision: **`PlayerFactory`** follows the same mental model as **`EnemyFactory`** plus Addressables loading: **collect refs from the loadout definition, load all, spawn all, assign sockets and weapon references on the player, return the ready player**. Callers receive **only the player instance**; weapon roots are reachable **only through components on that player** (no `PlayerLoadoutSpawnResult` or parallel struct).
  Rationale: Keeps the consumer API minimal and mirrors existing enemy spawn ergonomics.
  Date/Author: 2026-03-23 / User + agent (planning)

- Decision: **Hand or weapon socket `Transform`s** live on a **player MonoBehaviour** (recommended: dedicated **`WeaponVisualController`**; alternatives **`PlayerCore`** or **`AnimationController`** if the team wants fewer components). The factory **writes** the three spawned weapon roots into that behaviour after parenting.
  Rationale: Scene-authored sockets stay explicit; the factory does not hunt arbitrary hierarchy paths except where the behaviour exposes named child references.
  Date/Author: 2026-03-23 / User + agent (planning)

- Decision: **`PlayerWeaponLoadoutDefinition`** lives in **`Madbox.Levels`** (alongside other Addressables-backed authoring). **`PlayerService`** and **`PlayerFactory`** live in **`Madbox.Bootstrap.Runtime`** so **`Madbox.GameView`** stays free of **`Unity.Addressables`** assembly references while still owning **`WeaponVisualController`**.
  Rationale: Unity batch compilation did not resolve `UnityEngine.AddressableAssets` inside `Madbox.GameView` even with `Unity.Addressables` listed; splitting matches existing **`LevelAssetProvider`** patterns in bootstrap and keeps presentation assembly minimal.
  Date/Author: 2026-03-23 / agent (implementation)

## Outcomes & Retrospective

At completion, a novice can assign a **`PlayerWeaponLoadoutDefinition`** to **`PlayerService`**, call **`PlayerFactory`** (or the game’s equivalent entry point) to obtain a **single ready player `GameObject`** (or thin player type), and switch the visible weapon through the **player-side visual controller** without reloading assets. Remaining work for a full game loop (stats, attacks, input) can read the loadout from **`PlayerService`** and drive the same visual component.

## Context and Orientation

Madbox separates **Unity presentation** from **pure Core** logic. **MonoBehaviour** and Addressables instantiation belong in **App** or **Infra** layers that are allowed to reference Unity, not in assemblies that forbid `UnityEngine` (see `Architecture.md`).

Today, the player view exposes **`PlayerCore`** and **`PlayerViewData`** under `Assets/Scripts/App/GameView/Runtime/Player/`. There is **no** existing player loadout ScriptableObject; this plan adds authoring and spawn plumbing.

Addressables loading in this repository is centralized on **`IAddressablesGateway`** in `Assets/Scripts/Assets/Addressables/Runtime/`. The documentation `Docs/Infra/Addressables.md` describes load methods and the rule to release handles exactly once. The docs also mention a sample **Remote Weapons** group with sword prefabs; new weapon prefabs for this feature should follow the same Addressables grouping conventions the project uses for remote content.

Terms used in this plan:

**Authoring asset** means a Unity **ScriptableObject** created with **Create Asset Menu**, stored under `Assets/`, that holds **AssetReference** fields for one **player** prefab and **three** weapon prefabs. **`PlayerService`** means a registered type that **stores** the active **`PlayerWeaponLoadoutDefinition`** and lets game code **read it when needed** (factory, bootstrap, or UI). **`PlayerFactory`** means the class that mirrors the **enemy pipeline** at a high level: **enumerate references from the definition, load, instantiate, wire, return player**.

Reference implementation for enemies: **`EnemyFactory`** in `Assets/Scripts/Meta/Enemies/Runtime/EnemyFactory.cs` only instantiates; **`BattleGameFactory`** in `Assets/Scripts/Core/Battle/Runtime/BattleGameFactory.cs` loads Addressables then spawns. For the player, **`PlayerFactory`** may combine load and spawn in one type or split internally, but the **observable contract** to the game is: **input loadout (from `PlayerService` or passed in), output one ready player**. **Socket** means a **`Transform`** serialized on a player behaviour (for example **`WeaponVisualController`**) where weapon instances are parented.

## Plan of Work

Add **`PlayerWeaponLoadoutDefinition`** in an authoring-friendly assembly (same spirit as **`LevelDefinition`** under `Assets/Scripts/Meta/Levels/Runtime/`). Fields: one **player** **`AssetReference`** (or **`AssetReferenceT<GameObject>`**), **three** weapon **`AssetReference`** fields. No optional string anchor name is required if sockets are **serialized on the player prefab**.

Add **`PlayerService`**. It holds a **`PlayerWeaponLoadoutDefinition`** reference (set at bootstrap or from config). It exposes **`PlayerWeaponLoadoutDefinition Loadout`**, **`GetLoadout()`**, or equivalent so **`PlayerFactory`** and other systems can obtain the definition **on demand**. This service does **not** replace the factory; it is the **authoring hook** wired into DI.

Add **`PlayerFactory`**. The game calls it similarly to how **`BattleGameFactory`** uses **`EnemyService`** plus loads: **read all `AssetReference` entries from the loadout**, **await loads through `IAddressablesGateway`**, **instantiate the player prefab**, **instantiate each weapon prefab** and parent to the **socket `Transform`s** exposed by a behaviour on the player (see below), **assign the three weapon `Transform` or `GameObject` references** into that behaviour, **apply default visible slot** (enable slot 0, disable 1 and 2), **release Addressable handles** after successful instantiation if the project policy matches the enemy path (load prefab, instantiate, release handle; instances remain valid). Return type is **only the player** (`GameObject`, or a thin **`PlayerCore`**-like root if you introduce one). **Do not** introduce **`PlayerLoadoutSpawnResult`**; consumers discover weapons **only** through the player hierarchy and the visual controller.

Add or extend a behaviour on the player prefab, **`WeaponVisualController`** (recommended), with **serialized `Transform` fields** for weapon attach points (one socket or three, as design prefers) and **three fields or an array for the spawned weapon roots** populated by the factory at runtime. Optionally add **`SetSelectedWeaponIndex(int)`** here. Alternatives are **`PlayerCore`** or **`AnimationController`** only if the team wants fewer components; the ExecPlan recommends **`WeaponVisualController`** so animation and core stay focused.

Register **`PlayerService`** and **`PlayerFactory`** in the **gameplay or bootstrap installer** that owns the player. Extend the smallest existing scope; see `Assets/Scripts/App/Bootstrap/Runtime/`.

For **tests**, prefer **EditMode** tests on pure logic: range validation, “only one active child,” and fake gateway that returns prefab assets without hitting the network. If parenting must be asserted, add a minimal **PlayMode** test assembly following `Docs/AutomatedTesting.md`.

Update **`Docs/`** for any new module: at minimum a short file under `Docs/App/` or `Docs/Infra/` describing the authoring asset, the service interface, and teardown expectations (when handles are released), per repository documentation standards.

## Concrete Steps

All commands assume the working directory is the repository root, for example `c:\Unity\Madbox`.

1. Discover current DI and player spawn path so the service lands in the correct installer:

    Use the editor file search or `rg` for `InstallGame`, `GameView`, `PlayerCore`, and `IAddressablesGateway` under `Assets/Scripts`.

2. Add the authoring ScriptableObject and runtime types in the chosen assembly; add `.asmdef` references so the authoring assembly can reference Addressables and Unity UI only where needed.

3. Implement factory and service; register in VContainer.

4. Run focused tests:

    powershell -NoProfile -ExecutionPolicy Bypass -File ".\.agents\scripts\run-editmode-tests.ps1"

    Add `-AssemblyNames` with the new test assembly once known.

5. Run the full milestone gate:

    .agents\scripts\validate-changes.cmd

    On Windows, if `cmd` chaining is awkward, invoke from PowerShell as documented in `AGENTS.md`.

## Validation and Acceptance

Acceptance is **behavioral**:

- **`PlayerService`** returns the same **`PlayerWeaponLoadoutDefinition`** instance the game configured, for any consumer that needs authoring data.
- **`PlayerFactory`** (or the async method the game calls) returns **only** the **player** instance; the three weapons exist as children or assigned references on **`WeaponVisualController`** (or chosen behaviour).
- Calling **SetSelectedWeapon** or **`SetSelectedWeaponIndex`** on the visual controller makes exactly one weapon active; repeated calls do not reload Addressables.
- Addressables **release** policy matches the enemy path: no duplicate release, no leak on partial failure (factory must clean up).
- **validate-changes** completes with no new analyzer blockers.

## Idempotence and Recovery

Spawning twice without tearing down the first player should be explicitly unsupported or guarded (destroy previous player or reject second spawn). If a load fails mid-way, the factory must release any handles already acquired and destroy any partial instances.

## Interfaces and Dependencies

By the end of implementation, these concepts must exist as real types (exact names may differ if a naming review prefers alternatives, but responsibilities must match):

- **`PlayerWeaponLoadoutDefinition`**: ScriptableObject with one player **AssetReference** and **three** weapon **AssetReference** fields.

- **`PlayerService`**: holds **`PlayerWeaponLoadoutDefinition`**; exposes it to the game **on demand** (for example read-only property).

- **`PlayerFactory`**: depends on **`IAddressablesGateway`** (and optionally **`PlayerService`** to read the loadout, or take **`PlayerWeaponLoadoutDefinition`** as a method parameter). Public method shape is illustrative:

        Task<GameObject> CreateReadyPlayerAsync(Transform parent, Vector3 position, Quaternion rotation);

    or pass the loadout explicitly:

        Task<GameObject> CreateReadyPlayerAsync(PlayerWeaponLoadoutDefinition loadout, Transform parent, Vector3 position, Quaternion rotation);

    Implementation steps inside: **get all refs from definition, load all, spawn player, spawn weapons, assign to `WeaponVisualController`, default active slot, release handles, return player**.

- **`WeaponVisualController`** (or agreed behaviour): serialized **socket `Transform`s**; **three** slots for weapon instance references filled by the factory; **SetSelectedWeaponIndex(int)** for visuals.

- **`IAddressablesGateway`**: injected dependency for all loads.

**Explicitly out of scope for this section:** **`PlayerLoadoutSpawnResult`** or any secondary return type for weapons.

## Artifacts and Notes

Indented examples belong here after implementation (sample loadout asset path, sample log lines from successful spawn, short test output transcript).

---

Revision history:

- 2026-03-22: Initial authoring from user requirements (Addressables prefabs, spawn service plus factory, visual selection via enable or disable, out of scope: stats and input).

- 2026-03-23: Redirected to **`PlayerService`** (holds and exposes loadout definition), **`PlayerFactory`** (enemy-style: load all refs, spawn, assign, return **player only**), sockets on **`WeaponVisualController`** (or **PlayerCore** / **AnimationController**), removed **`PlayerLoadoutSpawnResult`**.
