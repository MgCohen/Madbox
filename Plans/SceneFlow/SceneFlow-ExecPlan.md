# Additive Addressable Scene Flow (Bootstrap Shell)

This ExecPlan is a living document. The sections `Progress`, `Surprises & Discoveries`, `Decision Log`, and `Outcomes & Retrospective` must be kept up to date as work proceeds.

This document must be maintained in accordance with `PLANS.md` at the repository root.

## Purpose / Big Picture

After this work, application code can **load and unload gameplay (or other) scenes additively** while the **Bootstrap scene stays loaded for the entire session**. Loads use **Addressables** scene references. During load and unload transitions, the player sees a **dedicated loading presentation** (a “transition loading view”) that is separate from the **bootstrap scope pipeline** loading bar (`ILayeredScopeProgress` / `BootstrapLoadingView`). Gameplay cameras and 3D content live in the additive scene; **UI stays in Bootstrap** and remains driven by **Navigation** and existing view models—this ExecPlan does not wire a full menu-to-battle product flow.

A human can verify the behavior by running **EditMode tests** for the scene-flow service (deterministic fakes or minimal Unity-backed tests as implemented), and optionally a **manual Play Mode** check once a sample additive scene and Addressables entry exist (not mandatory for closing the plan if tests cover the service contract).

**Additive scene** means Unity loads another scene on top of the already loaded Bootstrap scene so both exist in the hierarchy at once. **Addressable scene** means the scene asset is loaded through the Addressables system using an `AssetReference` (or project-approved typed reference) rather than only listing the scene in **File > Build Settings**.

**Bootstrap shell** means the scene that contains `BootstrapScope`, DI installers, Navigation root, and overlay UI is never unloaded and its root is not deactivated as part of normal scene flow (individual components such as a camera may be toggled—see below).

## Progress

- [ ] Author initial ExecPlan (this file) and record baseline repo references.
- [ ] Add `Madbox.SceneFlow` runtime module under `Assets/Scripts/Infra/SceneFlow/Runtime/` with contracts and implementation for additive load/unload via Addressables.
- [ ] Add `Madbox.SceneFlow.Container` (or register from an existing infra installer if the team prefers fewer assemblies) with VContainer registration for `ISceneFlowService`.
- [ ] Add `Madbox.SceneFlow.Tests` with automated tests for success paths, failure propagation, and handle release semantics (no handle leaks).
- [ ] Add presentation hook for transition loading: contract in SceneFlow (e.g. `ISceneFlowLoadingPresenter` or events) and a thin **Bootstrap** `MonoBehaviour` view that shows/hides full-screen overlay (implementation may be minimal art).
- [ ] Document optional **Bootstrap camera / AudioListener** policy: disable bootstrap camera while any additive “content” scene is active when UI is overlay-only; re-enable when the stack is empty. Cover in tests if pure-C# testable; otherwise document manual acceptance.
- [ ] Add `Docs/Infra/SceneFlow.md` per module documentation standard.
- [ ] Wire **Bootstrap** composition to register SceneFlow and assign the loading view reference (no product flow from Main Menu to battle required to complete this plan).
- [ ] Run `.agents/scripts/validate-changes.cmd` from the repository root and resolve all failures.
- [ ] Record integration notes for future consumers (e.g. `Madbox.Battle.BattleBootstrap` currently uses `Addressables.LoadSceneAsync` with a configurable `LoadSceneMode`; migrating it to **Additive** + this service is a **follow-up**, not part of this milestone unless explicitly pulled in).

## Surprises & Discoveries

- Observation: (fill during implementation) e.g. Unity EditMode limitations for `SceneManager` / Addressables scene operations.
  Evidence: (test output or doc note)

- Observation: (fill during implementation) e.g. interaction between multiple cameras and URP camera stacking when Bootstrap camera is disabled.
  Evidence: (scene setup note)

## Decision Log

- Decision: Introduce a dedicated **SceneFlow** infra module rather than embedding scene load/unload inside a single feature module (e.g. Battle only).
  Rationale: Multiple features will load additive content; a shared service avoids duplicated handle lifecycle and keeps Navigation/UI ownership clear.
  Date/Author: 2026-03-22 / User + Agent

- Decision: **Bootstrap scene is never unloaded** and is not deactivated as a whole; only **additive** Addressable scenes are loaded and unloaded.
  Rationale: Matches the shell + content model and keeps DI/Navigation stable.
  Date/Author: 2026-03-22 / User + Agent

- Decision: Use **Addressables** for all dynamically loaded scenes in this system (`LoadSceneMode.Additive`); track `AsyncOperationHandle<SceneInstance>` (or equivalent project types) and release/unload through a single code path.
  Rationale: Aligns with existing Addressables usage and remote catalog strategy (`Docs/Infra/Addressables.md`).
  Date/Author: 2026-03-22 / User + Agent

- Decision: **Transition loading UI** is separate from **bootstrap scope** loading (`ILayeredScopeProgress`). Transition loading is for mid-session content loads (e.g. entering a level); scope loading is for startup DI/layer pipeline.
  Rationale: Different user-facing meaning and timing; avoids overloading `BootstrapLoadingView`.
  Date/Author: 2026-03-22 / User + Agent

- Decision: **Navigation stays the owner of screen UI**; SceneFlow does not instantiate game views or push `INavigation` routes. Callers (future game flow) coordinate: show loading presenter → load scene → hide loading → open/close navigation as needed.
  Rationale: Preserves existing MVVM/navigation boundaries (`Docs/Infra/Navigation.md`).
  Date/Author: 2026-03-22 / User + Agent

- Decision: **Screen Space - Overlay** UI does not require a camera. Policy: when one or more additive content scenes are active, **disable** the Bootstrap **camera** (and avoid duplicate **AudioListener**—typically disable bootstrap listener or rely on content scene listener; pick one consistent rule and document it). Re-enable when no additive content scenes remain.
  Rationale: Prevents competing cameras and duplicate listeners; overlay UI remains interactive via `EventSystem` (ensure a single EventSystem—usually in Bootstrap).
  Date/Author: 2026-03-22 / User + Agent

- Decision: Do **not** require full **example product wiring** (main menu → level select → game → unload → main menu) to mark this ExecPlan complete; document that flow as a reference only.
  Rationale: User requested a reusable system first; product wiring lands in a later milestone.
  Date/Author: 2026-03-22 / User + Agent

## Outcomes & Retrospective

(To be filled at milestone completion. Summarize what shipped: public APIs, tests run, manual checks, and what remains—e.g. migrating `BattleBootstrap` to additive SceneFlow.)

## Context and Orientation

**Repository layout** is summarized in `Architecture.md`. **Infra** modules live under `Assets/Scripts/Infra/` and may depend on Unity and Addressables; **pure domain** modules must not take new Unity dependencies—scene loading belongs in Infra (or App presentation), not in core domain.

**Existing related code** (baseline for integration notes):

- `Assets/Scripts/Core/Battle/Runtime/BattleBootstrap.cs` loads a level scene via `Addressables.LoadSceneAsync` with a **`LoadSceneMode` parameter** defaulting to **Single**. A future change can switch to **Additive** and delegate to `ISceneFlowService` so Bootstrap remains.
- `Assets/Scripts/App/Bootstrap/Runtime/BootstrapScope.cs` completes bootstrap and opens the main menu through **`INavigation`**.
- `Assets/Scripts/App/Bootstrap/Runtime/BootstrapLoadingView.cs` (or equivalent) implements **startup** loading presentation tied to **`ILayeredScopeProgress`**—not the same as transition loading for additive scenes.

**Terms:**

- **Scene flow handle**: An opaque token (or small struct) returned when an additive scene is loaded that the caller must pass back to unload, so the service can release the correct Addressables handle and unload the scene instance safely.
- **Transition loading view**: A UI root in the Bootstrap scene (full-screen overlay) shown during additive load/unload operations orchestrated by SceneFlow.

## Plan of Work

**Module and contracts.** Create `Madbox.SceneFlow` with a narrow public contract, for example `ISceneFlowService`, exposing asynchronous methods to load an Addressable scene **additively** and to unload a previously loaded instance using a returned handle. The implementation uses `Addressables.LoadSceneAsync` with **`LoadSceneMode.Additive`**, awaits completion, validates success, and stores the **`AsyncOperationHandle<SceneInstance>`** (or project-standard wrapper) for later unload. Unload path must call the appropriate Addressables/Scene unload API and **release** the handle exactly once. The service should be safe to call from Unity’s main thread (Addressables scene APIs are main-thread oriented); document threading assumptions.

**Loading presentation.** Define an interface such as `ISceneFlowLoadingPresenter` with `Show()`, `Hide()`, or async variants if needed. The SceneFlow service accepts an optional presenter (injected) and invokes it around load/unload when enabled in options, so tests can pass a no-op. Implement a concrete **`MonoBehaviour`** in **`Madbox.App.Bootstrap`** (e.g. `SceneFlowTransitionLoadingView`) that toggles a **Canvas** or panel; wire it in **Bootstrap.unity** via serialized reference. Do not register this view inside **`INavigation`** unless a later plan requires it; default is a direct reference on the installer or `BootstrapScope`.

**Bootstrap camera and audio policy.** Optional serialized references on the presenter or a tiny `IBootstrapShellVisibility` helper: when load starts, disable Bootstrap **Camera** and **AudioListener** if present; when the last additive scene unloads, re-enable. If no camera exists in Bootstrap, the code path should no-op cleanly.

**Composition.** Register `ISceneFlowService` in the bootstrap infra layer (new `SceneFlowInstaller` or extension of an existing installer consistent with project patterns). Ensure `.asmdef` references include Addressables and any Unity modules required.

**Tests.** Add `Madbox.SceneFlow.Tests`: prefer fast EditMode tests using test doubles for Addressables if the team introduces an abstraction (`IAddressablesSceneLoader`) behind the service; if not, document reliance on **PlayMode** or limited Unity tests and still assert **release** semantics via injectable seams. At minimum, one test must fail if a handle would be double-released or forgotten (per repository testing standards in `Docs/AutomatedTesting.md`).

**Documentation.** Add `Docs/Infra/SceneFlow.md` describing purpose, API, loading presenter hook, camera/listener policy, and example pseudo-flow (menu → load → game → unload) without claiming product wiring is shipped.

**Future example flow (not implemented here).** Main menu shows level selection; user picks a level; game flow creates a session and calls SceneFlow to load the level scene additively while showing the transition loading view; 3D runs in the additive scene; UI screens live under Navigation in Bootstrap; on exit, show loading again, unload additive scene, return UI to main menu. This narrative guides tests and docs but requires separate feature work to connect `MainMenuViewModel` and battle/level services.

## Concrete Steps

Working directory: repository root (for example `c:\Unity\Madbox` on Windows).

1. Create `Assets/Scripts/Infra/SceneFlow/` with Runtime, optional Container, and Tests assemblies; mirror naming and dependency style from `Assets/Scripts/Infra/Scope/` or `Assets/Scripts/Infra/Navigation/`.

2. Implement `ISceneFlowService` and `SceneFlowService` with additive load/unload, optional `ISceneFlowLoadingPresenter`, and optional bootstrap camera/listener toggling.

3. Register services in the bootstrap composition path used at runtime (same entry point where other infra is registered).

4. Add `SceneFlowTransitionLoadingView` (or similarly named) under `Assets/Scripts/App/Bootstrap/Runtime/`, reference Unity UI if needed, update `Madbox.Bootstrap.Runtime.asmdef` if new references are required.

5. Author tests in `Madbox.SceneFlow.Tests`; run EditMode tests via `.agents/scripts/run-editmode-tests.ps1` with the new assembly name if filtered runs are needed.

6. Run `.agents/scripts/validate-changes.cmd` and fix analyzers, compilation, and tests until clean.

7. Update this ExecPlan `Progress`, `Surprises & Discoveries`, and `Outcomes & Retrospective` with evidence (commands and pass/fail summaries).

## Validation and Acceptance

**Automated:** From the repository root, run `.agents/scripts/validate-changes.cmd` and observe a clean gate: scripts audit pass, compilation pass, EditMode tests pass, PlayMode tests pass (if applicable), analyzers report `TOTAL:0` blockers per project scripts.

**Behavioral:** `ISceneFlowService` can load a scene by Addressable reference in **Additive** mode and unload it by handle without leaving the Bootstrap scene unloaded. If a transition loading presenter is wired, it becomes visible during load/unload operations in Play Mode (manual check optional if not fully automated).

**Non-requirement:** No end-to-end Main Menu → Battle flow must exist in production code for this plan to be complete.

## Idempotence and Recovery

Registering SceneFlow services multiple times in DI should be avoided (single registration). If load fails mid-flight, the service must not leak handles: release or unload on failure paths and surface exceptions or failed status to the caller. Document retry policy: callers may retry load; service remains stateless aside from tracked active handles per operation.

## Artifacts and Notes

Indented examples only when implementation exists; keep transcripts short.

    Expected: validate-changes.cmd ... PASS

## Interfaces and Dependencies

By the end of the milestone, the following should exist (exact names may be adjusted if a naming review prefers alternatives, but equivalent responsibilities must remain):

- `ISceneFlowService` with methods along the lines of:
  - `Task<SceneFlowLoadResult> LoadAdditiveAsync(AssetReference sceneReference, SceneFlowLoadOptions options, CancellationToken cancellationToken = default)`
  - `Task UnloadAsync(SceneFlowLoadResult result, CancellationToken cancellationToken = default)`  
  Where `SceneFlowLoadResult` carries at least an opaque handle id and enough data for unload (internally the `AsyncOperationHandle<SceneInstance>`).

- `ISceneFlowLoadingPresenter` with `void Show()` and `void Hide()` (or async if required by UI framework usage).

- `SceneFlowLoadOptions` flags may include: whether to show loading presenter, whether to toggle bootstrap camera/listener, and any future activation policy.

**Dependencies:** `Madbox.SceneFlow` references Unity Addressables packages and appropriate Unity modules; **Bootstrap** references SceneFlow for the MonoBehaviour loading view and DI wiring. **Core** domain modules should not reference SceneFlow directly; app or composition layers coordinate domain + SceneFlow.

## Revision History

- 2026-03-22: Initial ExecPlan authored (additive Addressable scene flow, Bootstrap persistent shell, transition loading view, optional camera/listener policy, example flow documented as non-blocking).
