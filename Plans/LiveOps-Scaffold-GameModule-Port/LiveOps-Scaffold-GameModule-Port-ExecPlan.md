# Replace Madbox LiveOps Backend with Scaffold GameModule (Verbatim Sources, LiveOps Product Name)

This ExecPlan is a living document. The sections `Progress`, `Surprises & Discoveries`, `Decision Log`, and `Outcomes & Retrospective` must be kept up to date as work proceeds.

This document must be maintained in accordance with `PLANS.md` at the repository root.

## Purpose / Big Picture

After this change, the **Cloud Code backend** lives in a **single self-contained tree** `LiveOps/` at the Madbox repo root. That tree **contains all three parts**: **Utility**, **DTO**, and the **main Cloud Code module** (still as **three separate `.csproj` projects** under one `LiveOps.sln`). Utility is **vendored inside** `LiveOps/Utility/` so nothing under `LiveOps/` depends on a sibling checkout such as `C:\Unity\Scaffold\Utility`. Sources are copied from the neighbor Scaffold projects with **no edits to `.cs` files**. The intentional Madbox identity is the **product and deployment name** **LiveOps** (folder name `LiveOps/`, Unity module string `"LiveOps"`, and documentation). The backend should build cleanly, publish the Cloud Code module, and produce Unity plugin DLLs that the game can reference.

Someone can verify success by running `dotnet build` on the new solution under `LiveOps/`, copying fresh DLLs into `Assets/Plugins/`, opening Unity, and confirming scripts compile. Automated proof is `.\.agents\scripts\validate-changes.cmd` from the repository root with no new analyzer or test regressions attributable to this port.

## Progress

- [x] (2026-03-21) Authored ExecPlan (initial): scope, source layout, Unity integration expectations, validation loop.
- [x] (2026-03-21) Copied Scaffold `GameModuleDTO`, `Project`, and `Utility` into `LiveOps/LiveOps.DTO`, `LiveOps/Project`, `LiveOps/Utility` (sources unmodified).
- [x] (2026-03-21) Added `LiveOps/LiveOps.sln`, `LiveOps.DTO/Madbox.LiveOps.DTO.csproj` (AssemblyName `Madbox.LiveOps.DTO`), `Project/LiveOps.csproj` (AssemblyName `LiveOps`); removed duplicate `OutputPath` from DTO csproj to avoid double `netstandard2.1` folder.
- [x] (2026-03-21) Built `dotnet build LiveOps\LiveOps.sln -c Release`; copied `Utility.dll` and `Madbox.LiveOps.DTO.dll` to `Assets/Plugins/`.
- [x] (2026-03-21) Removed Unity `Madbox.LiveOps` / Container / Tests folders; stripped bootstrap ping and `LiveOpsInstaller` wiring; removed `Madbox.LiveOps` + DTO precompile from `Madbox.Bootstrap.asmdef`; removed `Assets/Test.cs` (SCA0022).
- [x] (2026-03-21) Removed `Madbox.LiveOps*.csproj` from `Madbox.sln`; patched generated `Madbox.Bootstrap.Runtime.csproj` and `Assembly-CSharp.csproj` until Unity regen (see Surprises).
- [x] (2026-03-21) Updated `Docs/LiveOps.md`; added plugin metas for `Madbox.LiveOps.Utility`.
- [ ] Run `.\.agents\scripts\validate-changes.cmd` with **Unity closed** (project lock blocked headless compile in agent run); commit when gate is clean.

## Surprises & Discoveries

- Observation: `GameModuleDTO` csproj used `OutputPath` with `netstandard2.1` in the path; SDK already appends TFM, producing `...\netstandard2.1\netstandard2.1\` until removed.
  Evidence: First `dotnet build` output path for DTO.

- Observation: Headless `validate-changes.cmd` aborted compilation because another Unity instance held the project lock.
  Evidence: “It looks like another Unity instance is running with this project open.”

- Observation: Root `Madbox.LiveOps.csproj` / Container / Tests were Unity-generated; after deleting asmdefs, `dotnet sln remove` cleaned the solution. `Madbox.Bootstrap.Runtime.csproj` still referenced removed projects until Unity regenerates or manual patch.
  Evidence: `dotnet build` after port.

- Observation: `Assets/Test.cs` in default assembly triggered SCA0022 cross-`*.Runtime` references; file removed for port hygiene.
  Evidence: Analyzer output on `Assembly-CSharp.csproj`.

## Decision Log

- Decision: Treat **`.cs` files** from `C:\Unity\Scaffold\GameModule` and `C:\Unity\Scaffold\Utility\Utility` as **read-only** when copying; only **project, solution, and repository glue** files may be edited to make paths and outputs correct under `C:\Unity\Madbox\LiveOps`.
  Rationale: Matches the request to bring Scaffold functionality without rewriting logic; mechanical wiring is unavoidable.
  Date/Author: 2026-03-21 / Codex

- Decision: **Colocate Utility inside** `Madbox/LiveOps/Utility/` so the **LiveOps** deliverable is one folder in the repo that holds **main + DTO + Utility**. After the copy, **do not** leave a `ProjectReference` that points outside `LiveOps/` to Scaffold’s `Utility` tree. Scaffold’s `GameModule.sln` used `..\Utility\Utility\Utility.csproj`; in Madbox, equivalent references are sibling paths under `LiveOps\` only.
  Rationale: Single product root, no dependency on neighbor repo layout for day-to-day builds.
  Date/Author: 2026-03-21 / Codex

- Decision: Prefer **three `.csproj` projects** inside `LiveOps/` (Utility, DTO, main), **not** one merged `.csproj`. The DTO assembly must target **netstandard2.1** (Unity plugin) while the Cloud Code host targets **net6+**; one project with one `TargetFramework` cannot produce both outputs correctly without nonstandard multitargeting and mixed references. “Single project” in conversation means **one LiveOps solution / one folder**, not literally one C# project file.
  Rationale: Keeps the same build outputs Scaffold intended with minimal risk.
  Date/Author: 2026-03-21 / Codex

- Decision: **Namespaces and type names** inside copied DLLs remain **Scaffold’s** (`GameModule`, `GameModuleDTO`, `Utility`, etc.). Unity scripts that today use `using Madbox.LiveOps.DTO` **must** be updated to use the Scaffold namespaces **or** the port is incomplete. Keeping the **folder name** `LiveOps` does not imply keeping the old `Madbox.LiveOps.DTO` namespace in source.
  Rationale: Verbatim `.cs` from Scaffold cannot simultaneously preserve Madbox’s previous namespace layout without violating “no script modifications.”
  Date/Author: 2026-03-21 / Codex

- Decision: It is acceptable to set **AssemblyName** / output file names in `.csproj` so Unity plugin paths stay stable (for example continue emitting `Madbox.LiveOps.DTO.dll` for the DTO project) **as long as** public types still live in Scaffold namespaces; Unity usings must match **namespaces**, not file names.
  Rationale: Reduces churn in `.meta` and plugin folder naming while staying honest about type identities.
  Date/Author: 2026-03-21 / Codex

- Decision: If **ping/pong** does not exist in the ported contracts, **strip usages only**—remove the methods, call sites, interface members, probe hooks, and tests that mention `PingRequest` / `PongResponse`. **No** broad refactor and **no** substitute endpoint wired in place of ping as part of this port.
  Rationale: User asked for minimal change; dead calls are removed, not redesigned.
  Date/Author: 2026-03-21 / Codex

## Outcomes & Retrospective

**Shipped (2026-03-21):** Backend under `LiveOps/` matches Scaffold GameModule stack (Utility + DTO + main). Unity no longer has a `Madbox.LiveOps` client assembly; ping/pong and `ILiveOpsService` were removed with bootstrap ping lines only—no replacement Cloud Code API. Plugins: `Madbox.LiveOps.DTO.dll` + `Utility.dll` in `Assets/Plugins/`. **`Assets/Scripts/Core/LiveOps/LiveOps.ccmr`** left in place for manual repackage.

**Follow-up:** Regenerate/publish Cloud Code from `LiveOps.dll`; confirm dashboard module name matches future client calls. Re-run `validate-changes.cmd` with Unity closed. Let Unity refresh `*.csproj` files so manual Bootstrap/Assembly-CSharp edits are not stale.

## Context and Orientation

**Madbox today**

- Cloud Code backend sources live under `LiveOps/` beside the Unity project (not only under `Assets/`). `Directory.Build.props` in `LiveOps/` disables repository Roslyn analyzers for these projects.
- Unity integration for typed contracts lives under `Assets/Scripts/Core/LiveOps/` (`Madbox.LiveOps`, installer, tests). The runtime service currently calls Cloud Code module name **`"LiveOps"`** and uses **`Madbox.LiveOps.DTO`** types including ping/pong.
- Unity loads a precompiled contract DLL from `Assets/Plugins/Madbox.LiveOps.DTO/Madbox.LiveOps.DTO.dll`, declared in `Madbox.LiveOps.asmdef` under `precompiledReferences`.

**Scaffold source (neighbor checkout)**

- **Main module**: `C:\Unity\Scaffold\GameModule\Project\` — Cloud Code host project (`GameModule.csproj`), namespaces rooted at `GameModule.*`, includes `Core`, `Modules`, `Sample`, `Configs`, etc.
- **DTO**: `C:\Unity\Scaffold\GameModule\GameModuleDTO\` — `GameModuleDTO.csproj`, namespaces such as `GameModuleDTO.ModuleRequests`, depends on **Utility**.
- **Utility** (copy-from): `C:\Unity\Scaffold\Utility\Utility\` — `Utility.csproj`, `netstandard2.1`, helpers under `Utility.*`. **Madbox canonical location**: `Madbox/LiveOps/Utility/` (same files after port).

**Terms**

- **Cloud Code module name**: string Unity passes when calling a deployed module (today `"LiveOps"` in `LiveOpsService`). It is independent of C# namespace `GameModule`.
- **Plugin assembly**: a precompiled `.dll` Unity references from `Assets/Plugins/` via `precompiledReferences` or manual references.

## Plan of Work

First, treat the Scaffold tree as the **authoritative behavior**. Walk `GameModule` and list the three project boundaries (Utility, DTO, main). Copy **all** `.cs` files and required `Configs` JSON (and any other non-code assets referenced by relative paths) into `Madbox/LiveOps/` so **Utility, DTO, and main** are **siblings under `LiveOps/`** (for example `LiveOps/Utility/`, `LiveOps/LiveOps.DTO/`, `LiveOps/Project/`). No long-term reliance on `C:\Unity\Scaffold\Utility` after the copy.

Second, delete Madbox’s previous backend sources under `LiveOps/` that would conflict (old `LiveOps.DTO`, old `Project` files, obsolete Services/PingPong, etc.). Do not delete `Directory.Build.props` unless you replace it with an equivalent policy.

Third, author `LiveOps.sln` with three projects. The Scaffold `GameModule.sln` may contain stale GUID blocks; prefer a **fresh** solution that lists only the three projects with correct relative paths from `LiveOps/`.

Fourth, adjust **only** `.csproj` and `.sln` files so every `ProjectReference` resolves **inside** `LiveOps/` (for example from `LiveOps\Project\`, DTO is `..\LiveOps.DTO\*.csproj` and Utility is `..\Utility\Utility.csproj`). Match Scaffold’s target frameworks and package references so Cloud Code APIs resolve the same way.

Fifth, resolve **Unity** consumption. Scaffold’s DTO project references Utility; Unity’s `Madbox.LiveOps` assembly may need **two** precompiled DLLs. Add a plugin folder (for example `Assets/Plugins/Madbox.LiveOps.Utility/`) for `Utility.dll` if kept separate, and list both DLLs in `Madbox.LiveOps.asmdef` in dependency order. Update C# usings in `Assets/Scripts/Core/LiveOps/` to Scaffold namespaces **only where needed** for code that remains. If ping/pong types are gone, **delete** the corresponding interface members, service methods, probe code, and tests—**no** replacement flow required in this port.

Sixth, build Release, copy outputs to `Assets/Plugins/`, remove obsolete DLLs, run Unity compilation mentally by running the repo validation gate.

## Concrete Steps

All commands assume Windows PowerShell. Repository root: `C:\Unity\Madbox`.

**1) Copy sources (example layout)**

  Source A (main + DTO tree): `C:\Unity\Scaffold\GameModule`

  Source B (Utility, **only for initial copy**): `C:\Unity\Scaffold\Utility\Utility`

  Target root (canonical): `C:\Unity\Madbox\LiveOps` — this folder will contain **Utility + DTO + Project** with **no** post-port references to Scaffold paths.

  Copy `GameModuleDTO\**\*.cs` into `LiveOps\LiveOps.DTO\` (folder name on disk may differ; keep one DTO project).

  Copy `Project\**\*.cs`, `Project\Configs\**`, `Project\Sample\**`, etc., into `LiveOps\Project\` preserving relative structure.

  Copy the **entire** Scaffold `Utility\Utility\` tree (including `Utility.csproj`) into `LiveOps\Utility\` so `LiveOps\Utility\Utility.csproj` exists and DTO can reference `..\Utility\Utility.csproj`.

**2) Solution and projects**

  Create `LiveOps\LiveOps.sln` with **three** SDK-style projects under the **same** `LiveOps\` root: `Utility\Utility.csproj`, DTO csproj, main csproj. This is the recommended meaning of “LiveOps contains main, DTO, and Utility.” Rename the **main** output assembly in the csproj to **`LiveOps`** if you want the deployed binary identity to read as LiveOps while **default namespace / types** in `.cs` remain `GameModule` (assembly name change alone does not rewrite namespaces).

**3) Build backend**

  Working directory: `C:\Unity\Madbox\LiveOps`

  Example:

    dotnet build .\LiveOps.sln -c Release

  Fix **only** project path or package restore errors.

**4) Refresh Unity plugins**

  After a successful Release build, copy:

  - DTO output DLL (and its PDB optionally) into `Assets\Plugins\Madbox.LiveOps.DTO\`

  - `Utility.dll` into a dedicated plugins folder under `Assets\Plugins\` (new folder if needed)

  Delete any **old** DLL that no longer corresponds to a built project (for example prior `Madbox.LiveOps.DTO.dll` built from removed sources) so Unity cannot load a stale mix.

**5) Unity script edits (allowed; not “Scaffold .cs” copies)**

  Update `Assets/Scripts/Core/LiveOps/**/*.cs` and tests to compile against the new namespaces where types are still used. If ping/pong is absent, **remove** only the lines/methods/tests that referenced it—no substitute API in this step.

**6) Quality gate**

  Working directory: `C:\Unity\Madbox`

    .\.agents\scripts\validate-changes.cmd

  Iterate until EditMode and analyzer stages pass per repository policy. PlayMode failures pre-existing to LiveOps should be noted in `Surprises & Discoveries` rather than silently blamed on this port.

## Validation and Acceptance

Acceptance is **behavioral** and **build-based**:

- `dotnet build LiveOps\LiveOps.sln -c Release` succeeds with zero errors.

- Unity resolves types from the new plugin assemblies: no missing `GameModuleDTO` or `Utility` types from the `Madbox.LiveOps` assembly’s point of view.

- `validate-changes.cmd` completes without new failures introduced by this change.

- Cloud Code deployment: the packaged module runs the same module entry types Scaffold expects (`ModuleConfig` implementing `ICloudCodeSetup`, `ModuleRequestHandler`, registered modules). If Madbox uses a `.ccmr` or publish profile, regenerate it from the new `Project` output and confirm the dashboard module name matches the Unity client string (`"LiveOps"` unless intentionally changed everywhere).

## Idempotence and Recovery

Copying can be repeated: delete generated `bin/` and `obj/` under `LiveOps/` and rebuild. If a bad DLL was copied to `Assets/Plugins/`, remove it and recopy from a clean Release build. Git restore can recover pre-port `LiveOps/` if the branch was committed before deletion.

## Artifacts and Notes

Keep short transcripts here (build success lines, `validate-changes` summary) as you execute milestones.

## Interfaces and Dependencies

At completion, the following must hold:

- **Three** `.csproj` projects **inside** `LiveOps/` only: **Utility** (`netstandard2.1`, Newtonsoft as in Scaffold), **DTO** (`netstandard2.1`, project reference to sibling Utility, packages as in Scaffold), **Main module** (`net6.0` or Scaffold’s target, Cloud Code packages as in Scaffold, references DTO + Utility). No `ProjectReference` outside `LiveOps/`.

- **Unity** consumes precompiled **DTO** and **Utility** if DTO’s public API exposes Utility types or attributes.

- **Unity client** calls Cloud Code using module name **`LiveOps`** consistently with deployment configuration.

- **No** remaining references to `PingRequest` / `PongResponse` in Unity if those types are absent from the ported DTO; achieve that by **deleting** the old call sites and members, not by adding a replacement feature in the same change.

---

Revision history:

- 2026-03-21: Initial ExecPlan authored from user request (verbatim Scaffold port, three libraries, LiveOps naming, plugin rebuild, ping/pong removal allowed).

- 2026-03-21: Utility **vendored inside** `LiveOps/Utility/`; clarified “single LiveOps” = one folder + one solution with **three** projects, not one merged `.csproj` (framework split DTO vs host).

- 2026-03-21: Ping/pong: if missing from ported contracts, **minimal deletion** of usages only—no refactor or replacement endpoint in scope.

- 2026-03-21: **Execution complete** (agent): Scaffold copy, `LiveOps.sln`, plugin deploy, Unity LiveOps assembly removal, `Docs/LiveOps.md`; validation gate blocked by Unity project lock—user must re-run locally.
