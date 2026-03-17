# Consolidate Contracts and Runtime Assemblies Module by Module

This ExecPlan is a living document. The sections `Progress`, `Surprises & Discoveries`, `Decision Log`, and `Outcomes & Retrospective` must be kept up to date as work proceeds.

This document must be maintained in accordance with `PLANS.md` at the repository root.

## Purpose / Big Picture

After this change, each targeted module will expose one primary runtime assembly instead of split `*.Contracts` and `*.Runtime` assemblies, while still preserving explicit module boundaries. This reduces assembly sprawl, simplifies references, and makes ownership clearer for developers and AI agents.

The change is observable when:

1. Contract/runtime paired modules no longer contain separate `Contracts/` and `Runtime/` asmdefs.
2. Downstream asmdefs that previously referenced `*.Contracts` reference the module base assembly instead.
3. Analyzer rules stop requiring contracts-first folder and dependency patterns for migrated modules.
4. `.agents/scripts/validate-changes.cmd` passes after each module milestone.

## Progress

- [x] (2026-03-17 22:06Z) Authored initial ExecPlan skeleton, module inventory strategy, and analyzer migration strategy.
- [x] (2026-03-17 22:52Z) Executed Milestone 1: Scope module consolidation (`Scaffold.Scope.Contracts` into `Scaffold.Scope`).
- [x] (2026-03-17 22:52Z) Executed Milestone 2: Events module consolidation (`Scaffold.Events.Contracts` into `Scaffold.Events`).
- [x] (2026-03-17 22:52Z) Executed Milestone 3: Navigation module consolidation (`Scaffold.Navigation.Contracts` into `Scaffold.Navigation`).
- [x] (2026-03-17 22:52Z) Executed Milestone 4: ViewModel module consolidation (`Scaffold.MVVM.ViewModel.Contracts` into `Scaffold.MVVM.ViewModel`).
- [x] (2026-03-17 22:52Z) Executed Milestone 5: View module consolidation (`Scaffold.MVVM.View.Contracts` into `Scaffold.MVVM.View`).
- [x] (2026-03-17 22:52Z) Executed Milestone 6: Analyzer rule updates for unified modules (`SCA0022`, `SCA0023`, `SCA0024`, `SCA0025`) and analyzer tests.
- [x] (2026-03-17 22:52Z) Executed Milestone 7: Repository-wide asmdef reference replacement and docs updates.
- [ ] (2026-03-17 22:52Z) Full quality gate clean pass is still blocked by environment issues (`Scaffold.Analyzers.Tests.csproj` missing; Unity compile precheck detected project lock).

## Surprises & Discoveries

- Observation: The repository currently has no `Plans/` folder checked in, even though `AGENTS.md` requires all planning docs under `Plans/`.
  Evidence: Root directory listing shows `PLANS.md` and `MILESTONE.md` but no `Plans/` directory.
- Observation: Analyzer rules currently encode contracts-first defaults (`Contracts,Runtime,Tests`) and cross-module `*.Runtime` restrictions that assume contracts assemblies exist.
  Evidence: `Analyzers/Scaffold/Scaffold.Analyzers/ModuleRequiredFoldersAnalyzer.cs`, `RuntimeAssemblyBoundaryAnalyzer.cs`, and `.editorconfig` keys for `SCA0022` and `SCA0023`.
- Observation: `.agents/scripts/check-analyzers.ps1` and `validate-changes.cmd` cannot complete analyzer stage because analyzer test project file is absent in this worktree.
  Evidence: `BLOCKER: Analyzer tests project not found at ...\Scaffold.Analyzers.Tests.csproj`.
- Observation: `validate-changes.cmd` compilation precheck reports a Unity project lock due another instance.
  Evidence: `Aborting batchmode due to fatal error: It looks like another Unity instance is running with this project open.`

## Decision Log

- Decision: Migrate module by module instead of a single large refactor.
  Rationale: This keeps failures localized, allows per-module verification, and matches the requested execution style.
  Date/Author: 2026-03-17 / Codex
- Decision: Treat “merge contract and runtime folders” as applying to modules that currently have both top-level `Contracts/` and `Runtime/` folders.
  Rationale: This avoids forcing unrelated modules into scope (for example modules that never had a contracts/runtime split).
  Date/Author: 2026-03-17 / Codex
- Decision: Update analyzer behavior as part of the same program of work rather than postponing.
  Rationale: Existing analyzer rules would otherwise block or misreport the target structure.
  Date/Author: 2026-03-17 / Codex

## Outcomes & Retrospective

No implementation work executed yet. This section must be updated after each milestone and at full completion with actual outcomes, open gaps, and lessons learned.

Implemented outcome: targeted modules now use unified assemblies (`Scaffold.Scope`, `Scaffold.Events`, `Scaffold.Navigation`, `Scaffold.MVVM.ViewModel`, `Scaffold.MVVM.View`) with boundary source relocated under `Runtime/Contracts/` and legacy top-level contracts asmdefs removed. Repository asmdef references were updated to remove dependencies on removed contracts/runtime assembly names. Analyzer rules and tests were updated to support the unified topology, and docs were updated to match.

Remaining gap: full quality gate cannot be declared clean in this environment until the analyzer test project exists (or analyzer script is adjusted) and Unity compile precheck can run without project lock contention.

## Context and Orientation

Madbox is organized under `Assets/Scripts/<Layer>/<Module>/` with assembly boundaries enforced by `.asmdef` files and Roslyn analyzers. The target change replaces two-assembly module surfaces (`<Module>.Contracts` + `<Module>.Runtime`) with one main assembly per module.

The initially targeted modules with both top-level folders are:

1. `Assets/Scripts/Infra/Scope` (`Contracts/`, `Runtime/`, `Tests/`)
2. `Assets/Scripts/Infra/Events` (`Contracts/`, `Runtime/`, `Container/`, `Samples/`, `Tests/`)
3. `Assets/Scripts/Infra/Navigation` (`Contracts/`, `Runtime/`, `Container/`, `Samples/`, `Tests/`)
4. `Assets/Scripts/Core/ViewModel` (`Contracts/`, `Runtime/`, `Tests/`)
5. `Assets/Scripts/App/View` (`Contracts/`, `Runtime/`, `Samples/`, `Tests/`)

Key analyzer files that enforce current contracts-first assumptions:

1. `Analyzers/Scaffold/Scaffold.Analyzers/RuntimeAssemblyBoundaryAnalyzer.cs` (`SCA0022`)
2. `Analyzers/Scaffold/Scaffold.Analyzers/ModuleRequiredFoldersAnalyzer.cs` (`SCA0023`)
3. `Analyzers/Scaffold/Scaffold.Analyzers/ModuleAsmdefConventionAnalyzer.cs` (`SCA0024`)
4. `Analyzers/Scaffold/Scaffold.Analyzers/ContractsBoundaryTypeAnalyzer.cs` (`SCA0025`)
5. `.editorconfig` analyzer settings (`scaffold.SCA0023.required_folders`, `scaffold.SCA0025.enforced_kinds`, severities)

Terms used in this plan:

- Module root: the folder `Assets/Scripts/<Layer>/<Module>/` and the shared assembly name prefix (for example `Scaffold.Scope`).
- Boundary type: a public type intended for consumption by another module (usually interfaces today).
- Unified module assembly: one primary assembly named by module root (for example `Scaffold.Scope`) replacing split contract/runtime assembly names.

## Plan of Work

Milestone 1 (Scope) establishes the migration pattern. Move public boundary types currently in `Infra/Scope/Contracts` into the unified assembly source layout under `Infra/Scope/Runtime` (or renamed primary folder), then delete `Scaffold.Scope.Contracts.asmdef`. Rename `Scaffold.Scope.Runtime` assembly to `Scaffold.Scope` in its asmdef, and update all asmdefs that reference `Scaffold.Scope.Contracts` to reference `Scaffold.Scope`. Keep only truly cross-module API types public; reduce visibility for implementation types that do not cross module boundaries.

Milestones 2 and 3 repeat the same strategy for `Infra/Events` and `Infra/Navigation`, including `Container`, `Samples`, and tests asmdefs. For each module, replace references to `<Module>.Contracts` with `<Module>` and ensure no downstream asmdef still points to removed contract assembly names. Preserve DI/container behavior by leaving container assemblies intact unless a specific simplification is required.

Milestones 4 and 5 apply the same consolidation to `Core/ViewModel` and `App/View`. Because these modules participate in MVVM chains and reference `Scaffold.MVVM.Base.Contracts`, update visibility carefully and run related tests to confirm binding/viewmodel flows remain intact. Any public type retained must be justified as a boundary surface; other runtime internals should become `internal`.

Milestone 6 updates analyzers and analyzer tests to support unified modules without contracts folders. Specifically, relax or redesign rules that currently require contracts-first topology, and codify the new boundary policy so diagnostics remain useful. Update `.editorconfig` defaults and exceptions to match the new convention.

Milestone 7 performs a repository-wide reference sweep for stale `*.Contracts` references, updates docs in `Docs/Infra/*.md`, `Docs/Core/ViewModel.md`, and `Docs/App/View.md`, and ensures standards documentation reflects the unified-assembly organization. Finish with full quality gate and capture evidence.

## Concrete Steps

Run all commands from repository root: `C:\Users\mtgco\.codex\worktrees\c8f3\Madbox`.

Before each module milestone:

1. Inventory the module’s asmdefs and references.
   Command:
    `rg --files Assets/Scripts/<Layer>/<Module> -g "*.asmdef"`
2. Find current references to module contracts assembly name.
   Command:
    `rg -n "<Module>.Contracts" Assets/Scripts -g "*.asmdef"`
3. Move/rename module files and asmdefs, then update references.
4. Run module-focused tests (if available) and then full milestone gate.
   Commands:
    `powershell -NoProfile -ExecutionPolicy Bypass -File ".\.agents\scripts\run-editmode-tests.ps1"`
    `powershell -NoProfile -ExecutionPolicy Bypass -File ".\.agents\scripts\run-playmode-tests.ps1"`
    `powershell -NoProfile -ExecutionPolicy Bypass -File ".\.agents\scripts\check-analyzers.ps1"`
    `& ".\.agents\scripts\validate-changes.cmd"`

Analyzer milestone commands:

1. Build analyzers after code changes.
   Command:
    `dotnet build -c Release .\Analyzers\Scaffold\Scaffold.Analyzers\Scaffold.Analyzers.csproj`
2. Run analyzer tests.
   Command:
    `dotnet test .\Analyzers\Scaffold\Scaffold.Analyzers.Tests\Scaffold.Analyzers.Tests.csproj -c Release`
3. Re-run repository analyzer checks.
   Command:
    `powershell -NoProfile -ExecutionPolicy Bypass -File ".\.agents\scripts\check-analyzers.ps1"`

Expected success signals:

- No `*.Contracts.asmdef` remains for migrated modules.
- `rg -n "<MigratedModule>.Contracts" Assets/Scripts -g "*.asmdef"` returns no hits.
- Analyzer build and analyzer tests pass.
- `.agents/scripts/validate-changes.cmd` reports clean gate.

## Validation and Acceptance

Acceptance is complete only when all targeted modules are unified and behavior remains stable.

For each module milestone:

1. Confirm assembly unification by inspecting module asmdefs and the absence of old contract asmdef.
2. Confirm consumer references now target the unified module assembly.
3. Run milestone quality gate and capture pass output.

For the overall plan:

1. Run `& ".\.agents\scripts\validate-changes.cmd"` and confirm all checks pass.
2. Run a final contracts-reference sweep:
   `rg -n "\.Contracts" Assets/Scripts -g "*.asmdef"`
   This should only return intentionally retained modules (if any are explicitly deferred and documented).
3. Verify updated docs reflect the new structure and rationale.

## Idempotence and Recovery

Each milestone is designed to be repeatable. Running reference sweeps, analyzer checks, and validation scripts multiple times is safe.

If a milestone fails:

1. Re-add temporary asmdef compatibility only within that module (if needed) to restore compileability.
2. Re-run module tests and analyzer checks.
3. Resume migration after diagnostics are clean.

Do not delete contracts folders for a module until all direct asmdef references are replaced and compilation succeeds for that milestone.

## Artifacts and Notes

When implementing, store concise evidence snippets here for each milestone:

    Milestone 1 (Scope) validation snippet:
    <paste short validate-changes pass summary>

    Milestone 2 (Events) asmdef sweep snippet:
    <paste short rg output showing no Scaffold.Events.Contracts references>

    Milestone 6 analyzer snippet:
    <paste short dotnet test summary for analyzer tests>

## Interfaces and Dependencies

Final module interfaces and dependencies must follow these rules:

1. Each migrated module has one primary assembly named by module root (for example `Scaffold.Scope`).
2. Cross-module consumers reference only that module root assembly, not split contract/runtime assembly names.
3. Internal implementation types that are not part of cross-module boundaries are marked `internal`.
4. Analyzer rules are updated to enforce the new topology and to avoid false positives tied to removed contracts-first assumptions.
5. Container/test/sample assemblies continue to reference module root assembly names and preserve current behavior.

## Revision Note

2026-03-17: Initial ExecPlan created to drive a module-by-module consolidation of contracts/runtime assemblies per the requested refactor direction.
2026-03-17: Updated after implementation to reflect completed milestones and environment blockers affecting final gate verification.
