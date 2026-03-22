# Establish Barebones LiveOps UGS Plumbing (DTO + Cloud Code + Unity Bridge)

This ExecPlan is a living document. The sections `Progress`, `Surprises & Discoveries`, `Decision Log`, and `Outcomes & Retrospective` must be kept up to date as work proceeds.

This document must be maintained in accordance with `PLANS.md` at the repository root.

## Purpose / Big Picture

After this change, the project has a minimal but working end-to-end LiveOps path: Unity sends one typed request, Cloud Code receives it through one function entrypoint, and Unity receives one typed response. This creates the foundational plumbing needed to add game modules later without redesigning transport and contracts.

The user-visible behavior is simple and testable: a Unity-side service call such as `PingRequest` returns a `PingResponse` from Cloud Code using shared DTO types that are built once in the backend and consumed by Unity as a DLL.

## Architecture (three independent concerns)

Responsibilities are split so other features can reuse **UGS** or **Cloud Code** without pulling in LiveOps, and LiveOps can stay focused on **application/backend contracts** without owning UGS initialization.

| Concern | Role | Unity location | Depends on |
|--------|------|----------------|------------|
| **UGS** | Initialize Unity Services and anonymous sign-in when something needs Cloud Code. | `Assets/Scripts/Infra/Ugs/Runtime/` (`Madbox.Ugs`) | Unity Services packages only |
| **Cloud Code** | Call module endpoints after UGS is ready. | `Assets/Scripts/Infra/CloudCode/Runtime/` (`Madbox.CloudCode`) | Cloud Code SDK (UGS init via `Madbox.Ugs` when used) |
| **LiveOps** | Map app calls to Cloud Code payloads and DTOs; no direct UGS usage. | `Assets/Scripts/Core/LiveOps/Runtime/` (`Madbox.LiveOps`) | `ICloudCodeModuleService` only (not UGS directly) |

**Registration order** (see `LiveOpsInstaller`): UGS → Cloud Code → LiveOps.

**Assembly layout:** `Madbox.Ugs` is its own assembly (`Assets/Scripts/Infra/Ugs/Runtime/Madbox.Ugs.asmdef`). `Madbox.CloudCode` lives under `Infra/CloudCode`. `Madbox.LiveOps` client code lives under `Core/LiveOps/Runtime` and depends on `Madbox.CloudCode` only (not UGS directly). `LiveOpsInstaller` is under `Core/LiveOps/Container` (`Madbox.LiveOps.Container`).

**Backend layout:** `Backend/LiveOps/LiveOps.sln` contains:

- **`Madbox.LiveOps.DTO`** (`netstandard2.1`): contracts only, namespace **`MadboxLiveOpsContracts`**. Post-build copies `Madbox.LiveOps.DTO.dll` into `Assets/Plugins/Madbox.LiveOps.DTO/` for Unity.
- **`LiveOps.CloudCode`** (`net8`): Cloud Code module handlers; references the DTO project and Unity Cloud Code NuGet packages.

DTO types are not hand-duplicated in Unity scripts; Unity references the generated plugin assembly. **`Assets/csc.rsp`** adds `-r:Assets/Plugins/Madbox.LiveOps.DTO/Madbox.LiveOps.DTO.dll` so Roslyn resolves the plugin when the asmdef graph alone does not pass the reference through in this setup.

## Progress

- [x] (2026-03-21 00:00Z) Authored initial ExecPlan for initial plumbing scope (no module system yet), including milestones, validation loop, and recovery notes.
- [x] (2026-03-21 00:10Z) Reordered milestones to start from Cloud Code + Unity bridge first per product direction.
- [x] (2026-03-21 00:15Z) Reordered milestones again so Unity service bridge is implemented first as a hard prerequisite for load path.
- [x] (2026-03-21 00:45Z) Execute Milestone 1: Added Unity bridge module (`Madbox.LiveOps`) with installer wiring, `ILiveOpsService`, and fallback response mode enabled by default for load-safe startup.
- [x] (2026-03-21 00:57Z) Execute Milestone 2: Added backend Cloud Code project (`Backend/LiveOps/LiveOps.CloudCode`) with ping function entrypoint and connected Unity bridge to call module endpoint when fallback is disabled.
- [x] (2026-03-21 01:22Z) Execute Milestone 3: Added DTO project, solution wiring, and Cloud Code reference to shared contracts.
- [x] (2026-03-21 01:28Z) Execute Milestone 4: Added `LiveOpsBootstrapProbe` verification component and reran validation gate (compilation PASS, EditMode PASS; PlayMode failures pre-existing).
- [x] (2026-03-21) Refactor: **Cloud Code** under `Infra/CloudCode`, **LiveOps** client under `Core/LiveOps` (UGS in **`Madbox.Ugs`**); LiveOps depends on Cloud Code only; DTOs supplied via **`Madbox.LiveOps.DTO`** DLL; plan text updated for current layout.
- [x] (2026-03-21) Refactor: Canonical UGS types (`IUgsInitializationService`, `UgsInitializationService`) live only in **`Madbox.Ugs`** (`Infra/Ugs`); removed duplicate `Madbox.LiveOps.Ugs` sources; **`LiveOpsInstaller`** registers **`Madbox.Ugs`** implementations.

## Surprises & Discoveries

- Observation: This repository currently has no root-level backend `.sln` for Cloud Code service projects beyond `Backend/LiveOps/LiveOps.sln`.
  Evidence: Workspace search for `*.sln` at repo root is limited; LiveOps backend lives under `Backend/LiveOps/`.

- Observation: Existing codebase has DI installer patterns in Unity (`Installer`/scope installers), but no prior Cloud Code bridge before this work.
  Evidence: Search in `Assets/Scripts` for `Unity.Services.CloudCode` before LiveOps showed no matches.

- Observation: Unity package version `com.unity.services.cloudcode@2.7.0` is not resolvable in this repository environment, while `2.10.2` resolves and compiles.
  Evidence: `validate-changes.cmd` initially failed package resolution with `2.7.0`; pinning `2.10.2` passed compilation and EditMode tests.

- Observation: Registering UGS initialization as bootstrap `IAsyncLayerInitializable` caused PlayMode failures in unlinked Unity dashboard environments.
  Evidence: PlayMode reported `UnityProjectNotLinkedException`; on-demand initialization keeps baseline behavior.

- Observation: PlayMode bootstrap / main-menu tests still fail in CI-style runs (scope / UI probes).
  Evidence: `BootstrapScope` not found within frame budget; unrelated to LiveOps wiring.

- Observation: Shared DTO namespace **`MadboxLiveOpsContracts`** avoids ambiguous resolution with Unity-side **`Madbox`** / **`Madbox.LiveOps`** namespaces.

- Observation: Method-order analyzer (SCA0002) requires callees to appear after callers with no “unrelated” methods between; small helpers were inlined or merged where needed in LiveOps runtime files.

## Decision Log

- Decision: Start with one end-to-end request/response (`PingRequest`/`PingResponse`) before introducing module orchestration.
  Rationale: A single vertical slice validates transport, serialization, package boundaries, and deployment assumptions at the lowest complexity.
  Date/Author: 2026-03-21 / Codex

- Decision: Keep shared contracts in a dedicated DTO project targeting `netstandard2.1`, built as **`Madbox.LiveOps.DTO.dll`** and copied into Unity `Assets/Plugins/Madbox.LiveOps.DTO/`.
  Rationale: Matches the “other project” pattern: single source of truth in the backend; Unity consumes the binary; no hand-maintained duplicate DTO scripts.
  Date/Author: 2026-03-21 / Codex

- Decision: Separate **UGS**, **Cloud Code**, and **LiveOps** as three logical modules (namespaces + services), with LiveOps depending on Cloud Code only—not UGS directly.
  Rationale: UGS remains available for other systems; LiveOps stays a thin app/backend layer; Cloud Code owns “needs UGS initialized first.”
  Date/Author: 2026-03-21 / Codex

- Decision: Scope this plan to plumbing only, not full game-module architecture.
  Rationale: The user requested initial plumbing first; module orchestration can be layered after transport reliability is proven.
  Date/Author: 2026-03-21 / Codex

- Decision: Keep `LiveOpsService.UseFallback = true` by default and perform UGS initialization on-demand when backend calls are explicitly enabled (via Cloud Code module).
  Rationale: Preserves startup/load behavior in local or CI contexts where Unity project linking is unavailable while still enabling a real backend call path.
  Date/Author: 2026-03-21 / Codex

- Decision: Use **`Assets/csc.rsp`** to reference `Madbox.LiveOps.DTO.dll` for Unity compilation when asmdef-only reference is insufficient for the batch compiler.
  Rationale: Ensures Roslyn sees the plugin assembly; revisit if a Unity-supported asmdef + precompiled reference path is verified in-Editor.
  Date/Author: 2026-03-21 / Codex

## Outcomes & Retrospective

Unity exposes a scoped LiveOps surface: **`ILiveOpsService`** + **`LiveOpsService`**, with **`ICloudCodeModuleService`** / **`CloudCodeModuleService`** and **`IUgsInitializationService`** / **`UgsInitializationService`** (types from **`Madbox.Ugs`**) registered in **`LiveOpsInstaller`**. Fallback mode remains the default for resilient local loads.

Backend **`Backend/LiveOps/LiveOps.sln`** builds **`Madbox.LiveOps.DTO`** and **`LiveOps.CloudCode`**. Contracts live under `MadboxLiveOpsContracts`. **`LiveOpsBootstrapProbe`** remains a manual ping check.

Repository gate (typical run): compilation PASS, EditMode PASS; PlayMode has existing bootstrap/main-menu failures; solution-wide analyzer debt remains large; **`Madbox.LiveOps`**-scoped analyzer warnings for the touched LiveOps runtime files were brought to zero in `dotnet build Madbox.LiveOps.csproj`.

## Context and Orientation

This repository is a Unity project with runtime code under `Assets/Scripts`. The goal is a small parallel backend workspace under `Backend/LiveOps` without disturbing existing gameplay architecture.

In this plan, “DTO” means the **`Madbox.LiveOps.DTO`** class library whose build output Unity imports as a plugin. “Unity bridge” means **`LiveOpsService`** calling **`CloudCodeModuleService`**, which ensures UGS is initialized before Cloud Code calls. “Cloud Code entrypoint” means a backend module method receiving `PingRequest` and returning `PingResponse`.

Use Scaffold’s architecture as conceptual guidance only; do not copy unnecessary modules.

## Plan of Work

Milestone 1 starts with Unity bridge creation first. Create `ILiveOpsService` and `LiveOpsService` in `Assets/Scripts/Core/LiveOps/Runtime/` plus installer registration in `Assets/Scripts/Core/LiveOps/Container/`. Add a local fallback or stub execution mode so application startup and dependency injection load path can succeed before Cloud Code exists.

Milestone 2 adds Cloud Code backend. Create `Backend/LiveOps/LiveOps.CloudCode` with Unity Cloud Code package references and implement one callable function for ping. Switch `LiveOpsService` from fallback mode to real backend call when fallback is disabled.

Milestone 3 defines shared DTOs in **`Madbox.LiveOps.DTO`**, updates Cloud Code to reference them, and configures Unity to consume **`Madbox.LiveOps.DTO.dll`** (plugin + `csc.rsp` as needed).

Milestone 4 finalizes verification and gate compliance. Confirm typed responses after DTO integration, then run repository validation gate and record outcomes in this plan.

## Concrete Steps

Run all commands from repository root `C:\Users\mtgco\.cursor\worktrees\Madbox\dok` unless otherwise stated.

1. Add Unity bridge and DI wiring first (including temporary fallback behavior).

   Expected: project loads with LiveOps service registered and callable even before Cloud Code endpoint is available.

2. Create or update Cloud Code project under `Backend/LiveOps/LiveOps.CloudCode`.

3. Add Cloud Code package references to `LiveOps.CloudCode` (see project file for current packages and versions).

4. Build backend solution:

   `dotnet build Backend\LiveOps\LiveOps.sln`

   Expected: DTO and Cloud Code projects compile; DTO post-build copies the DLL into `Assets/Plugins/Madbox.LiveOps.DTO/` when configured.

5. Run repository quality gate:

   `.\.agents\scripts\validate-changes.cmd`

   If this gate fails, fix blocking issues and rerun.

## Validation and Acceptance

Acceptance is complete when all of the following are true and evidenced in this plan:

- Cloud Code exposes one callable function that receives `PingRequest` and returns `PingResponse`.
- Unity has one registered service (`ILiveOpsService` + implementation) capable of calling that function and getting typed output.
- Unity bridge can run in explicit fallback mode so load/bootstrap does not fail.
- Backend build passes for `Madbox.LiveOps.DTO` and `LiveOps.CloudCode`.
- DTO layer contains one real request/response pair shared via DLL (not duplicated source in Unity).
- Manual or automated probe demonstrates observable behavior: calling the Unity bridge returns success response data when fallback is on, or from Cloud Code when fallback is off and the environment is linked.
- `.agents/scripts/validate-changes.cmd` passes compilation and tests per project policy (full analyzer TOTAL:0 may require broader codebase cleanup outside LiveOps).

Behavior-oriented acceptance example:

    Input: Unity calls liveOpsService.PingAsync(new PingRequest("hello"))
    Output: PingResponse { Ok = true, Message = "hello", ServerTimeUtc = "<non-empty>" } (or fallback equivalent when configured)

If network/backend is unavailable, failure must be explicit and debuggable (clear log or failure response semantics).

## Idempotence and Recovery

All steps are additive and can be rerun. `dotnet new` commands will fail if targets already exist; in reruns, skip creation and continue with `dotnet sln add`/`dotnet build`. If project references are duplicated, remove duplicate entries from `.csproj` and rebuild.

If Unity cannot load DTO types from the plugin assembly, verify `Assets/csc.rsp`, plugin import settings, and that `dotnet build` produced `Madbox.LiveOps.DTO.dll` in `Assets/Plugins/Madbox.LiveOps.DTO/`.

If Cloud Code package version conflicts occur, pin package versions compatible with the current Unity Cloud Code environment and record chosen versions in this plan with reason.

## Artifacts and Notes

Expected new/updated artifacts during execution:

- `Backend/LiveOps/LiveOps.sln`
- `Backend/LiveOps/Madbox.LiveOps.DTO/Madbox.LiveOps.DTO.csproj` and contract sources
- `Backend/LiveOps/LiveOps.CloudCode/LiveOps.CloudCode.csproj` and module entrypoints
- `Assets/Plugins/Madbox.LiveOps.DTO/Madbox.LiveOps.DTO.dll` (generated by build)
- `Assets/csc.rsp` (DTO reference for Unity compiler)
- `Assets/Scripts/Infra/Ugs/Runtime/*` (UGS initialization; **`Madbox.Ugs`**)
- `Assets/Scripts/Infra/CloudCode/Runtime/*` (`Madbox.CloudCode`)
- `Assets/Scripts/Core/LiveOps/Runtime/*`, `Assets/Scripts/Core/LiveOps/Container/*` (`Madbox.LiveOps`, `Madbox.LiveOps.Container`)

Execution evidence should be appended here during implementation, including concise build output and validation summaries.

Recent evidence:

    Command: dotnet build Backend/LiveOps/LiveOps.sln
    Result: Build succeeded for Madbox.LiveOps.DTO and LiveOps.CloudCode.

    Command: dotnet build Madbox.LiveOps.csproj
    Result: Build succeeded; no SCA warnings in Madbox.LiveOps assembly.

    Command: .\.agents\scripts\validate-changes.cmd
    Result: Compilation PASS, EditMode PASS; PlayMode failures pre-existing (bootstrap / main menu probes).

## Interfaces and Dependencies

At the end of Milestone 4, the following interfaces and contracts must exist:

- DTO contracts (namespace `MadboxLiveOpsContracts`): `PingRequest`, `PingResponse`, and service contract types as needed (for example `ILiveOpsService` in DTO if shared).
- Cloud Code entrypoint: one module function callable by Unity, receiving `PingRequest`, returning `PingResponse`.
- Unity:
  - `ILiveOpsService` / `LiveOpsService`
  - `ICloudCodeModuleService` / `CloudCodeModuleService`
  - `IUgsInitializationService` / `UgsInitializationService` (namespace **`Madbox.Ugs`**, assembly **`Madbox.Ugs`**)

Dependencies expected:

- Backend: Unity Cloud Code NuGet packages, `Microsoft.Extensions.Logging.Abstractions` as required by project template.
- Unity: `Unity.Services.CloudCode`, `Unity.Services.Core`, `Unity.Services.Authentication`, VContainer, and **`Madbox.LiveOps.DTO`** (plugin).

All dependency choices and version pins must be recorded in this ExecPlan as they are decided.

---

Revision Note (2026-03-21 / Codex): Created initial plumbing-focused ExecPlan for a barebones LiveOps vertical slice (DTO + Cloud Code entrypoint + Unity UGS bridge), intentionally excluding full module orchestration until transport is validated.

Revision Note (2026-03-21 / Codex): Documented three-module separation (UGS / Cloud Code / LiveOps), DTO DLL workflow, `csc.rsp`, and current class names; aligned paths with `dok` worktree.

Revision Note (2026-03-21 / Codex): UGS initialization types consolidated into **`Madbox.Ugs`** only; ExecPlan table, assembly layout, artifacts, and interface list updated (no `Madbox.LiveOps.Ugs` duplicate).
