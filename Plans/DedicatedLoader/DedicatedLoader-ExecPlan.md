# Dedicated bootstrap loading screen (throwaway)

This ExecPlan is a living document. The sections `Progress`, `Surprises & Discoveries`, `Decision Log`, and `Outcomes & Retrospective` must be kept up to date as work proceeds.

This document must be maintained in accordance with `PLANS.md` at the repository root.

## Purpose / Big Picture

When the player starts the game, they see a **full-screen loading presentation** immediately while **`LayeredScope`** runs its **`LayerInstallerBase`** tree (VContainer child scopes, **`InitializeAsync`**, **`OnCompletedAsync`**, then children). When that run finishes, **`OnBootstrapCompleted`** runs and the **first navigated screen** (today: main menu via **`INavigation.Open`**) appears as it does now.

The **layer progress bar** is **not** bootstrap-specific: **`Madbox.Scope`** exposes a small, reusable hook tied to **`LayeredScope`** / **`LayerInstallerBase`** so **any** concrete **`LayeredScope`** subclass can assign a **scene** listener that updates UI. **`Madbox.App.Bootstrap`** only supplies **`BootstrapLoadingView`** (and optional tiny **view-only** child components) that **implement** that listener and own **all** progress **presentation** state—**no** static “layer progress” helper class.

After this work, a human can **enter Play Mode** on **`Assets/Scenes/Bootstrap.unity`**, see the loader, watch the bar advance **once per layer node** (depth-first order), then see the main menu. The loader stays **outside** **`INavigation`**.

## Progress

- [x] **`Madbox.Scope` (reusable):**
  - [x] Add **`ILayeredScopeProgress`** (or similarly named) under **`Assets/Scripts/Infra/Scope/Runtime/Contracts/`** with one method, e.g. **`void OnLayerPipelineStep(int completedLayerIndex, int totalLayers)`**, where **`completedLayerIndex`** is **1-based** through **`totalLayers`** and **`totalLayers`** is the number of **`LayerInstallerBase`** nodes in the built tree (count **before** **`BuildAsRootAsync`**).
  - [x] **`LayerInstallerBase`**: after **`await OnCompletedAsync(...)`** and **before** **`await BuildChildrenAsync(...)`** inside **`ExecuteBuildPipelineAsync`**, notify the optional listener (same step index semantics for every node in **depth-first pre-order**—matches how **`ExecuteBuildPipelineAsync`** runs: finish this node’s **`OnCompletedAsync`**, then recurse children left-to-right).
  - [x] **`LayeredScope`** (`Assets/Scripts/Infra/Scope/Runtime/LayeredScope.cs`): optional **`[SerializeField]`** reference to a **`MonoBehaviour`** that implements **`ILayeredScopeProgress`** (or assign **`Component`** and **`GetComponent`** at runtime—pick one pattern and stay consistent). **`RunStartupAsync`** attaches the listener to the **root** installer (or passes a **per-run** context into **`BuildAsRootAsync`**) so the whole tree reports without **Bootstrap** installers calling anything.
  - [x] Add or extend **`Docs/Infra/Scope.md`** with a short paragraph on **`ILayeredScopeProgress`** when implementing (repository rule: modules need docs—minimal blurb).
  - [x] **`Madbox.Scope.Tests`**: one focused test that a **stub** listener receives **N** callbacks in order with correct **total** (optional but preferred for regression).

- [x] **`Madbox.App.Bootstrap` (presentation only):**
  - [x] **`BootstrapLoadingView`**: implements **`ILayeredScopeProgress`**; updates a **bar** (e.g. **`Image`** fill or **`Slider`**) using **`completedLayerIndex / (float)totalLayers`**. Optionally split **view-only** pieces into small **`MonoBehaviour`** children (e.g. fill image vs. dim panel) **only** referenced/wired by **`BootstrapLoadingView`**—**no** separate global/static progress API.
  - [x] Queue UI updates on the **main thread** if async continuations might not be on Unity’s thread (e.g. stash pending value, apply in **`LateUpdate`**).
  - [x] **`BootstrapScope`**: optional **`[SerializeField]`** to the same **`BootstrapLoadingView`** (or wire **`LayeredScope`**’s progress field to it in the Inspector); at the start of **`OnBootstrapCompleted`**, call **`Hide()`** on the loading view before the rest (sample ad, **`OpenMainMenu`**).
  - [x] Add **`UnityEngine.UI`** to **`Madbox.Bootstrap.Runtime`** **`.asmdef`** if using **Image** / **Slider**.
  - [x] **Do not** add progress calls to **`BootstrapAssetInstaller`** / **`BootstrapInfraInstaller`** / **`BootstrapCoreInstaller`**—progress is entirely driven by **`LayerInstallerBase`**.

- [x] Manual acceptance: **`Bootstrap.unity`** → bar steps match **layer count** → main menu → loader hidden.
- [ ] Optional gate: **`.agents/scripts/validate-changes.cmd`**. Loader-specific PlayMode tests optional for throwaway.

## Surprises & Discoveries

- Observation: **`Bootstrap.unity`** wires **`BootstrapLoadingView`** on a child GameObject; **`BootstrapLoadingView`** also builds default Canvas/bar in **`Awake`** when **`progressFill`** is null.
  Evidence: `Assets/Scenes/Bootstrap.unity`, `BootstrapLoadingView.cs`.

- Observation: Unity **Edit Mode** tests do not run **`Awake`** on `LifetimeScope` children created via **`CreateChild`**, leaving **`Container`** null until **`Build()`** is invoked. **`LayerInstallerBase.BuildFromParentAsync`** now calls **`Build()`** when **`Container`** is missing so the pipeline matches Play Mode.
  Evidence: `LayerInstallerBase.cs`, `LayerInstallerProgressListenerTests.cs`.

## Decision Log

- Decision: **Reusable scope hook, not Bootstrap statics.** Progress signaling lives in **`Madbox.Scope`** (**`LayerInstallerBase`** + **`LayeredScope`**). **No** **`BootstrapLayerProgress`** or similar static helper.
  Rationale: Any future **`LayeredScope`** subclass gets the same behavior; Bootstrap stays thin.
  Date/Author: 2026-03-22 / Agent

- Decision: **Presentation state stays in the loading view.** **`BootstrapLoadingView`** implements **`ILayeredScopeProgress`** and may own child view-only components; the Scope assembly only defines the **contract** and **when** to call it.
  Rationale: User requested no detached static helper; UI owns display logic.
  Date/Author: 2026-03-22 / Agent

- Decision: **Step boundary** is **after** each layer’s **`OnCompletedAsync`** and **before** **`BuildChildrenAsync`** (same point **`LayerInstallerLifecycleOrderTests`** already cares about for “parent completed before children”).
  Rationale: One notification per **`LayerInstallerBase`** node, aligned with the existing pipeline.
  Date/Author: 2026-03-22 / Agent

- Decision: **Throwaway product scope** for **`BootstrapLoadingView`** art (solid colors OK), no **`INavigation`** registration, no analytics.
  Rationale: Prototype loader; replace later if needed.
  Date/Author: 2026-03-22 / Agent

- Decision: **Hide the loader** from **`BootstrapScope.OnBootstrapCompleted`** (or override **`OnBootstrapCompleted`** in a subclass) **before** opening the first screen—not inside **`NavigationInstaller`**.
  Rationale: Bootstrap owns post-scope app entry; navigation owns stacked screens after.
  Date/Author: 2026-03-22 / Agent

- Decision: **Spec vs. shipped:** This file is the design source until implemented; earlier static-helper approach is **obsolete**.
  Rationale: User direction.
  Date/Author: 2026-03-22 / Agent

## Outcomes & Retrospective

Implemented **`ILayeredScopeProgress`** in **`Madbox.Scope`**, **`BootstrapLoadingView`** + **`BootstrapScope.Hide`** in **`Madbox.App.Bootstrap`**, scene wiring for **`Bootstrap.unity`**, **`LayerInstallerProgressListenerTests`**, and **`Docs/Infra/Scope.md`** updates. Optional **`validate-changes.cmd`** should be run locally before merge.

## Context and Orientation

**`LayeredScope`** (`Assets/Scripts/Infra/Scope/Runtime/LayeredScope.cs`) builds a **`LayerInstallerBase`** tree, calls **`BuildAsRootAsync`**, then **`OnBootstrapCompleted`**. **`LayerInstallerBase`** (`LayerInstallerBase.cs`) runs **`InitializeAsync`**, **`OnCompletedAsync`**, then **`BuildChildrenAsync`** for each node.

**`BootstrapScope`** is one concrete **`LayeredScope`**; it is **not** the only possible user—hence **`ILayeredScopeProgress`** belongs under **`Infra/Scope`**.

**Navigation** remains for post-bootstrap screens only.

## Plan of Work

**Scope layer.** Introduce **`ILayeredScopeProgress`** in **`Madbox.Scope`**. Extend **`LayerInstallerBase`** so that, when a listener is attached for the current **build run**, it invokes **`OnLayerPipelineStep(completed, total)`** after **`OnCompletedAsync`** completes for that node and **before** child installers run. **`LayeredScope`** computes **`total`** once from the **root** installer (count all nodes in the tree). **`LayeredScope`** exposes an optional serialized reference to a **`MonoBehaviour`** implementing the interface and wires it before **`BuildAsRootAsync`**. Implementation detail (private **`AttachProgressToTree`** helper, or context object passed into **`BuildAsRootAsync`**) is left to the implementer as long as it stays **instance-scoped per startup run** and **does not** use static global state for progress.

**App layer.** Implement **`BootstrapLoadingView : MonoBehaviour, ILayeredScopeProgress`**. On each callback, set the bar’s normalized value from **`completed / (float)total`**. Implement **`Hide()`** to deactivate the loader root. Optionally add child **`MonoBehaviour`**s that only render parts of the UI; **`BootstrapLoadingView`** holds references and forwards updates—**no** second public progress API.

**BootstrapScope.** Assign the **`LayeredScope`** progress field to the **`BootstrapLoadingView`** instance in the scene (same object can satisfy both **listener** and **Hide** target). Override or extend **`OnBootstrapCompleted`** to call **`Hide()`** first.

**Do not** modify **`Bootstrap*Installer`** classes for progress.

## Concrete Steps

Working directory: repository root **`c:\Unity\Madbox`** (or your clone).

1. Implement **`ILayeredScopeProgress`**, **`LayerInstallerBase`** + **`LayeredScope`** wiring, and tests under **`Madbox.Scope`**.
2. Implement **`BootstrapLoadingView`** (+ optional child view scripts) and **`BootstrapScope`** + **`Madbox.Bootstrap.asmdef`** as above.
3. Open **`Assets/Scenes/Bootstrap.unity`**, assign **`LayeredScope`** progress reference and **`BootstrapLoadingView`**, save.
4. **Play** in Editor: stepped bar, then main menu.
5. Run **`.agents/scripts/validate-changes.cmd`** when ready to merge.

## Validation and Acceptance

1. Play **`Bootstrap.unity`**: loader visible at start.
2. Bar advances **in as many steps as there are layer installers** in **`BuildLayerTree()`** (today **three** nodes: asset → infra → core).
3. After **`OnBootstrapCompleted`**, loader hidden and main menu (or first navigation screen) shows.

**Automated tests:** Prefer at least one **`Madbox.Scope`** test for listener invocation; Bootstrap loader can stay manual for throwaway.

## Idempotence and Recovery

Safe to replay Play Mode. If **`ILayeredScopeProgress`** reference is **null**, **`LayeredScope`** behaves as today (no progress calls). If the loading view is missing, **`Hide()`** is skipped when reference null.

## Artifacts and Notes

    LayerInstallerBase.ExecuteBuildPipelineAsync
      -> InitializeAsync
      -> OnCompletedAsync
      -> [NEW] ILayeredScopeProgress.OnLayerPipelineStep(...)
      -> BuildChildrenAsync

## Interfaces and Dependencies

**`Madbox.Scope`**

- **`ILayeredScopeProgress`** — **`void OnLayerPipelineStep(int completedLayerIndex, int totalLayers)`** (names may be adjusted; keep semantics).
- **`LayeredScope`** — optional listener reference; **`.asmdef`** unchanged unless a new **Contracts** split is required (prefer keeping the interface next to existing **`Runtime/Contracts`** types).

**`Madbox.App.Bootstrap`**

- **`BootstrapLoadingView`** — **`MonoBehaviour`**, **`ILayeredScopeProgress`**, **`Hide()`**; **`UnityEngine.UI`** reference in **`Madbox.Bootstrap.Runtime`** **`.asmdef`** if needed.

---

**Revision note:** 2026-03-22 — Reworked plan: **Scope-first** **`ILayeredScopeProgress`**, **no** static layer-progress helper, **no** installer call sites in Bootstrap; loading view owns UI and optional child view components.
