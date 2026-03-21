# Establish Barebones LiveOps UGS Plumbing (DTO + Cloud Code + Unity Bridge)

This ExecPlan is a living document. The sections `Progress`, `Surprises & Discoveries`, `Decision Log`, and `Outcomes & Retrospective` must be kept up to date as work proceeds.

This document must be maintained in accordance with `PLANS.md` at the repository root.

## Purpose / Big Picture

After this change, the project will have a minimal but working end-to-end LiveOps path: Unity sends one typed request, Cloud Code receives it through one function entrypoint, and Unity receives one typed response. This creates the foundational plumbing needed to add game modules later without redesigning transport and contracts.

The user-visible behavior is simple and testable: a Unity-side service call such as `PingRequest` returns a `PingResponse` from Cloud Code using shared DTO types that are consumed by both backend and Unity.

## Progress

- [x] (2026-03-21 00:00Z) Authored initial ExecPlan for initial plumbing scope (no module system yet), including milestones, validation loop, and recovery notes.
- [x] (2026-03-21 00:10Z) Reordered milestones to start from Cloud Code + Unity bridge first per product direction.
- [x] (2026-03-21 00:15Z) Reordered milestones again so Unity service bridge is implemented first as a hard prerequisite for load path.
- [x] (2026-03-21 00:45Z) Execute Milestone 1: Added Unity bridge module (`Madbox.LiveOps`) with installer wiring, `ILiveOpsService`, `LiveOpsUgsService`, and fallback response mode enabled by default for load-safe startup.
- [x] (2026-03-21 00:57Z) Execute Milestone 2: Added backend Cloud Code project (`Backend/LiveOps/LiveOps.CloudCode`) with ping function entrypoint and connected Unity bridge to call module endpoint `LiveOps.PingRequest` when fallback is disabled.
- [x] (2026-03-21 01:22Z) Execute Milestone 3: Added `Backend/LiveOps/LiveOps.DTO`, created `Backend/LiveOps/LiveOps.sln`, and wired CloudCode project reference to shared contract source.
- [x] (2026-03-21 01:28Z) Execute Milestone 4: Added `LiveOpsBootstrapProbe` verification component and reran validation gate (compilation PASS, EditMode PASS, one existing PlayMode failure and pre-existing analyzer debt remain).
- [x] (2026-03-21 01:29Z) Record outcomes and implementation evidence for Milestones 1-4.

## Surprises & Discoveries

- Observation: This repository currently has no root-level backend `.sln` for Cloud Code service projects.
  Evidence: Workspace search for `*.sln` in repository root returned no solution files.

- Observation: Existing codebase has DI installer patterns in Unity (`Installer`/scope installers), but no current Cloud Code or UGS runtime bridge implementation to reuse directly.
  Evidence: Search in `Assets/Scripts` for `CloudCode`, `Unity.Services.CloudCode`, and `ICloudService` returned no matches.

- Observation: Unity package version `com.unity.services.cloudcode@2.7.0` is not resolvable in this repository environment, while `2.10.2` resolves and compiles.
  Evidence: `validate-changes.cmd` initially failed package resolution with `2.7.0`; rerun after pinning `2.10.2` passed compilation and EditMode tests.

- Observation: Registering UGS initialization as bootstrap `IAsyncLayerInitializable` caused PlayMode failures in unlinked Unity dashboard environments.
  Evidence: PlayMode reported `UnityProjectNotLinkedException`; removing bootstrap-time initialization registration and keeping on-demand initialization restored baseline behavior.

- Observation: This repository currently has one pre-existing PlayMode failure unrelated to LiveOps plumbing.
  Evidence: `Madbox.Addressables.Tests.PlayMode.AddressablesBootstrapPlayModeTests.BootstrapScene_ResolvesGateway_LoadsAndReleasesAddressable` continues to fail bootstrap completion in validation runs after LiveOps milestones.

## Decision Log

- Decision: Start with one end-to-end request/response (`PingRequest`/`PingResponse`) before introducing module orchestration.
  Rationale: A single vertical slice validates transport, serialization, package boundaries, and deployment assumptions at the lowest complexity.
  Date/Author: 2026-03-21 / Codex

- Decision: Keep shared contracts in a dedicated DTO project targeting `netstandard2.1`.
  Rationale: `netstandard2.1` is compatible with both Unity and backend .NET targets and mirrors the proven Scaffold pattern.
  Date/Author: 2026-03-21 / Codex

- Decision: Scope this plan to plumbing only, not full game-module architecture.
  Rationale: The user requested initial plumbing first; module orchestration can be layered after transport reliability is proven.
  Date/Author: 2026-03-21 / Codex

- Decision: Start implementation order with Cloud Code and Unity bridge before shared DTO extraction.
  Rationale: This gets a runnable vertical transport slice immediately, then stabilizes shared contracts as a refactor step.
  Date/Author: 2026-03-21 / Codex

- Decision: Start with Unity bridge before Cloud Code.
  Rationale: Project load/bootstrap requires the Unity-side service seam to exist first; backend can be attached afterward without blocking startup.
  Date/Author: 2026-03-21 / Codex

- Decision: Keep `LiveOpsUgsService.UseFallback = true` by default and perform UGS initialization on-demand when backend calls are explicitly enabled.
  Rationale: This preserves startup/load behavior in local or CI contexts where Unity project linking is unavailable while still enabling a real backend call path.
  Date/Author: 2026-03-21 / Codex

## Outcomes & Retrospective

All four planned milestones for initial plumbing were implemented. Unity now has a scoped LiveOps bridge (`ILiveOpsService` + `LiveOpsUgsService`) with explicit fallback mode for local startup resilience, and backend has a Cloud Code ping endpoint plus dedicated backend projects (`LiveOps.CloudCode`, `LiveOps.DTO`) grouped in `Backend/LiveOps/LiveOps.sln`.

The shared-contract extraction is implemented as source-sharing between Unity and backend (`PingRequest`/`PingResponse` under `Madbox.LiveOps.Contracts`) with backend DTO project referencing the same contract files. A runtime verification seam (`LiveOpsBootstrapProbe`) exists for manual in-scene ping checks.

Repository gate results after milestone completion: compilation PASS, EditMode PASS, PlayMode has one existing Addressables failure not introduced by this plan, and analyzers report substantial pre-existing repository debt outside the LiveOps scope.

## Context and Orientation

This repository is a Unity project rooted at `C:\Users\mtgco\.cursor\worktrees\Madbox\iog` with runtime code under `Assets/Scripts`. It currently lacks a backend solution and Cloud Code service code. The goal is to introduce a small parallel backend workspace under repository root without disturbing existing gameplay architecture.

In this plan, “DTO” means a shared class library containing request/response and payload classes used by both Unity and Cloud Code. “Unity bridge” means a Unity runtime service that performs Cloud Code HTTP/module calls and converts JSON payloads into typed DTO objects. “Cloud Code entrypoint” means a backend method decorated as a callable Cloud Code function that receives the DTO request and returns DTO response.

Use Scaffold’s architecture as conceptual guidance only; do not copy unnecessary modules. The initial shape should be:

- Unity runtime service under `Assets/Scripts/Infra/LiveOps/Runtime/` using UGS Cloud Code SDK.
- Cloud Code project exposing one function with minimal request/response contract (temporary local contract is acceptable until DTO extraction).
- Shared DTO extraction into `Backend/LiveOps/LiveOps.DTO` once the first end-to-end call works.
- Optional DTO dll copy to `Assets/Plugins/DTO` after shared-contract extraction is complete.

## Plan of Work

Milestone 1 starts with Unity bridge creation first. Create `ILiveOpsService` and `LiveOpsUgsService` in `Assets/Scripts/Infra/LiveOps/Runtime/` plus installer registration in `Assets/Scripts/Infra/LiveOps/Container/`. Add a local fallback or stub execution mode (for example returning a deterministic local ping response when backend endpoint is not configured) so application startup and dependency injection load path can succeed before Cloud Code exists.

Milestone 2 adds Cloud Code backend immediately after Unity seam is in place. Create `Backend/LiveOps/LiveOps.CloudCode` targeting .NET 6 with Unity Cloud Code package references and implement one callable function for ping. Then switch `LiveOpsUgsService` from fallback mode to real backend call (keeping fallback behind explicit flag only if needed for local startup resilience).

Milestone 3 extracts shared DTO contracts into a dedicated `LiveOps.DTO` project and updates both Cloud Code and Unity bridge to consume those shared types. Create `Backend/LiveOps/LiveOps.sln`, add both projects, add project reference from Cloud Code to DTO, and configure DTO distribution strategy for Unity (plugin dll copy or source-sharing fallback) while preserving the already-working ping flow.

Milestone 4 finalizes verification and gate compliance. Confirm the Unity bridge still receives typed responses after DTO extraction, then run repository validation gate and record exact outcomes in this plan.

## Concrete Steps

Run all commands from repository root `C:\Users\mtgco\.cursor\worktrees\Madbox\iog` unless otherwise stated.

1. Add Unity bridge and DI wiring first (including temporary fallback behavior).

   Expected: project loads with LiveOps service registered and callable even before Cloud Code endpoint is available.

2. Create Cloud Code project.

    dotnet new classlib -n LiveOps.CloudCode -f net6.0 -o Backend\LiveOps\LiveOps.CloudCode

3. Add Cloud Code package references to `LiveOps.CloudCode`.

    dotnet add Backend\LiveOps\LiveOps.CloudCode\LiveOps.CloudCode.csproj package Com.Unity.Services.CloudCode.Core
    dotnet add Backend\LiveOps\LiveOps.CloudCode\LiveOps.CloudCode.csproj package Com.Unity.Services.CloudCode.Apis
    dotnet add Backend\LiveOps\LiveOps.CloudCode\LiveOps.CloudCode.csproj package Microsoft.Extensions.Logging.Abstractions

4. Build Cloud Code project.

    dotnet build Backend\LiveOps\LiveOps.CloudCode\LiveOps.CloudCode.csproj

   Expected: Cloud Code project compiles successfully.

5. Create and wire shared DTO project after ping path works.

    dotnet new sln -n LiveOps -o Backend\LiveOps
    dotnet new classlib -n LiveOps.DTO -f netstandard2.1 -o Backend\LiveOps\LiveOps.DTO
    dotnet sln Backend\LiveOps\LiveOps.sln add Backend\LiveOps\LiveOps.DTO\LiveOps.DTO.csproj
    dotnet sln Backend\LiveOps\LiveOps.sln add Backend\LiveOps\LiveOps.CloudCode\LiveOps.CloudCode.csproj
    dotnet add Backend\LiveOps\LiveOps.CloudCode\LiveOps.CloudCode.csproj reference Backend\LiveOps\LiveOps.DTO\LiveOps.DTO.csproj

6. Run repository quality gate.

    .\.agents\scripts\validate-changes.cmd

   If this gate fails, fix all diagnostics and rerun until clean.

## Validation and Acceptance

Acceptance is complete when all of the following are true and evidenced in this plan:

- Cloud Code exposes one callable function that receives `PingRequest` and returns `PingResponse`.
- Unity has one registered service (`ILiveOpsService` + implementation) capable of calling that function and getting typed output.
- Unity bridge exists before backend wiring and can run in explicit fallback mode so load/bootstrap does not fail.
- Backend build passes for `LiveOps.DTO` and `LiveOps.CloudCode`.
- DTO layer contains one real request/response pair (`PingRequest`/`PingResponse`) shared by both Unity and backend (after Milestone 3 extraction).
- Manual or automated probe demonstrates observable behavior: calling the Unity bridge returns success response data from Cloud Code.
- `.agents/scripts/validate-changes.cmd` passes cleanly.

Behavior-oriented acceptance example:

    Input: Unity calls liveOpsService.PingAsync(new PingRequest("hello"))
    Output: PingResponse { Ok = true, Message = "hello", ServerTimeUtc = "<non-empty>" }

If network/backend is unavailable, failure must be explicit and debuggable (clear log or failure response semantics).

## Idempotence and Recovery

All steps are additive and can be rerun. `dotnet new` commands will fail if targets already exist; in reruns, skip creation and continue with `dotnet sln add`/`dotnet build`. If project references are duplicated, remove duplicate entries from `.csproj` and rebuild.

If Unity cannot load DTO types from plugin assembly immediately, switch to source-sharing fallback for the first milestone only (copy DTO source under Unity runtime folder) and document this as a temporary bridge in `Decision Log`; then convert back to dll/plugin once assembly boundaries are stable.

If Cloud Code package version conflicts occur, pin package versions compatible with the current Unity Cloud Code environment and record chosen versions in `Decision Log` with reason.

## Artifacts and Notes

Expected new/updated artifacts during execution:

- `Backend/LiveOps/LiveOps.sln`
- `Backend/LiveOps/LiveOps.DTO/LiveOps.DTO.csproj`
- `Backend/LiveOps/LiveOps.DTO/*` request/response contract files
- `Backend/LiveOps/LiveOps.CloudCode/LiveOps.CloudCode.csproj`
- `Backend/LiveOps/LiveOps.CloudCode/*` service entrypoint files
- `Assets/Scripts/Infra/LiveOps/Runtime/*` Unity bridge service interfaces/implementations
- `Assets/Scripts/Infra/LiveOps/Container/*` installer/wiring files
- `Assets/Plugins/DTO/*` (if dll-copy strategy is used in this milestone)

Execution evidence should be appended here during implementation, including concise build output and validation summaries.

Milestone 1/2 evidence:

    Command: dotnet build Backend/LiveOps/LiveOps.CloudCode/LiveOps.CloudCode.csproj
    Result: Build succeeded (warnings only; output dll generated).

    Command: .\.agents\scripts\validate-changes.cmd
    Result: Compilation PASS, EditMode PASS (185/185), PlayMode 2/3 passed with one existing Addressables PlayMode failure, analyzer diagnostics still present in existing codebase.

Milestone 3/4 evidence:

    Command: dotnet build Backend/LiveOps/LiveOps.sln
    Result: Build succeeded for LiveOps.DTO and LiveOps.CloudCode.

    Command: .\.agents\scripts\validate-changes.cmd
    Result: Compilation PASS, EditMode PASS (185/185), PlayMode 2/3 with same existing Addressables bootstrap failure.

## Interfaces and Dependencies

At the end of Milestone 4, the following interfaces and contracts must exist:

- DTO contracts:
  - `LiveOps.DTO.Contracts.ModuleRequest` (or equivalent base request)
  - `LiveOps.DTO.Contracts.ModuleResponse` (or equivalent base response)
  - `LiveOps.DTO.Ping.PingRequest`
  - `LiveOps.DTO.Ping.PingResponse`

- Cloud Code entrypoint:
  - One function callable by Unity, receiving `PingRequest`, returning `PingResponse`.

- Unity bridge:
  - `ILiveOpsService` with one method to send `PingRequest`.
  - `LiveOpsUgsService` implementation using Unity UGS Cloud Code SDK.

Dependencies expected:

- Backend: `Com.Unity.Services.CloudCode.Core`, `Com.Unity.Services.CloudCode.Apis`, `Microsoft.Extensions.Logging.Abstractions`.
- Unity: `Unity.Services.CloudCode` package and asmdef references compatible with runtime folder where `LiveOpsUgsService` is compiled.

All dependency choices and version pins must be recorded in this ExecPlan as they are decided.

---

Revision Note (2026-03-21 / Codex): Created initial plumbing-focused ExecPlan for a barebones LiveOps vertical slice (DTO + Cloud Code entrypoint + Unity UGS bridge), intentionally excluding full module orchestration until transport is validated.
Revision Note (2026-03-21 / Codex): Executed Milestone 1 and 2. Added `Madbox.LiveOps` Unity bridge and `LiveOps.CloudCode` backend ping endpoint; adjusted Unity Services package pinning and initialization strategy to keep startup load-safe.
Revision Note (2026-03-21 / Codex): Executed Milestone 3 and 4. Added shared DTO backend project/solution wiring and Unity probe verification while documenting residual non-LiveOps gate failures.
