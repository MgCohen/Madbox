# Govern Pragma Warning Suppressions with Policy, Gate, and Analyzer Enforcement

This ExecPlan is a living document. The sections `Progress`, `Surprises & Discoveries`, `Decision Log`, and `Outcomes & Retrospective` must be kept up to date as work proceeds.

This document must be maintained in accordance with `PLANS.md` at the repository root.

## Purpose / Big Picture

After this change, `#pragma warning disable` cannot be introduced silently. Contributors get immediate feedback in local validation and CI when new suppressions are added without approval, and they get analyzer diagnostics when runtime code uses unapproved suppression patterns. This closes the governance gap where suppressions could bypass policy accidentally.

A user can verify behavior by adding `#pragma warning disable` in a runtime C# file and observing a pragma gate failure and an analyzer diagnostic, then removing suppression or applying approved exception workflow and observing a clean gate.

## Progress

- [x] (2026-03-18 21:35Z) Authored initial ExecPlan with repository-specific paths, phased rollout, and acceptance criteria.
- [x] (2026-03-18 21:52Z) Updated `AGENTS.md` with explicit approval rule for new `#pragma warning disable` usage.
- [x] (2026-03-18 22:04Z) Added `.agents/scripts/check-pragma-warning-suppressions.ps1` and `.agents/scripts/pragma-warning-disable-allowlist.txt`, then integrated the gate into `.agents/scripts/validate-changes.ps1`.
- [x] (2026-03-18 22:12Z) Added `SCA0031` rule implementation and analyzer tests in `PragmaWarningDisableAnalyzerTests.cs`.
- [x] (2026-03-18 22:16Z) Updated documentation in `Docs/Analyzers/Analyzers.md` and `Docs/Testing.md`.
- [x] (2026-03-18 22:20Z) Ran targeted validation: pragma gate (`TOTAL:0`), analyzer tests (`Passed: 142`), analyzer build (`0 warnings, 0 errors`).
- [x] (2026-03-18 22:23Z) Ran `.agents/scripts/validate-changes.cmd` and recorded output evidence.
- [x] (2026-03-18 22:26Z) Completed retrospective and finalized ExecPlan updates.

## Surprises & Discoveries

- Observation: Current analyzer code already contains multiple `#pragma warning disable RS1035` directives in analyzer infrastructure files, so enforcement must support an explicit allowlist to avoid breaking existing intentional suppressions.
  Evidence: `Analyzers/Scaffold/Scaffold.Analyzers/ModuleRequiredFoldersAnalyzer.cs` and `Analyzers/Scaffold/Scaffold.Analyzers/ModuleAsmdefConventionAnalyzer.cs` contain existing disable pragmas.

- Observation: `validate-changes.cmd` is a wrapper; the integration point for new gates is `validate-changes.ps1`.
  Evidence: `.agents/scripts/validate-changes.cmd` only forwards execution to `.agents/scripts/validate-changes.ps1`.

- Observation: Diff-scanning `#pragma warning disable` patterns in all file types causes false positives in Markdown/policy text.
  Evidence: Initial script run flagged `AGENTS.md` and `Docs/Analyzers/Analyzers.md` lines that were documentation examples, not compiler directives.

- Observation: Running analyzer test and analyzer build concurrently can lock `obj\Release\netstandard2.0\Scaffold.Analyzers.dll`.
  Evidence: Parallel execution produced `CS2012: Cannot open ... because it is being used by another process`; rerunning sequentially succeeded.

- Observation: `validate-changes.cmd` can print Unity compiler errors in Details while still ending with `Compilation: PASS` and `Tests: PASS` in the summary.
  Evidence: Final run reported multiple `CS0535` errors for `Assets\Scripts\Core\Levels\Tests\AuthoringDefinitionsTests.cs` in details, but summary still showed pass status.

## Decision Log

- Decision: Deliver enforcement in three layers: policy text, diff-based script gate, and analyzer rule.
  Rationale: Policy sets expectation, script gate gives immediate PR/diff-level blocking, and analyzer gives semantic long-term enforcement across runtime code.
  Date/Author: 2026-03-18 / Codex

- Decision: Introduce a dedicated allowlist file for script-level exceptions.
  Rationale: Existing pragma uses are intentional in analyzer infrastructure; allowlist keeps the gate strict for new changes while enabling explicit, reviewable exceptions.
  Date/Author: 2026-03-18 / Codex

- Decision: Use a new analyzer ID `SCA0031` for pragma suppression governance.
  Rationale: Existing analyzer catalog currently occupies `SCA0001` through `SCA0030`; next available ID keeps rule mapping predictable.
  Date/Author: 2026-03-18 / Codex

- Decision: Scope the diff-based pragma gate to `.cs` files only.
  Rationale: The policy targets compiler pragmas in source code; checking non-C# files produced noise and blocked valid documentation updates.
  Date/Author: 2026-03-18 / Codex

## Outcomes & Retrospective

Suppression governance is now enforced in policy, scripting, and analyzer layers.

Delivered outcomes:

- `AGENTS.md` now explicitly forbids introducing new `#pragma warning disable` without same-thread user approval, plus reason and immediate restore requirements.
- `.agents/scripts/check-pragma-warning-suppressions.ps1` now detects newly introduced pragma disable lines from git diff, enforces C# scope, reason comments, immediate restore checks, and allowlist exceptions.
- `.agents/scripts/validate-changes.ps1` now runs a dedicated pragma suppression gate and reports it in summary output and agent diagnostics blocks.
- `SCA0031` now flags runtime `#pragma warning disable` usage in `Assets/Scripts/**/Runtime/**`, skipping tests/samples/generated paths.
- Analyzer docs and testing docs now include the new rule and gate workflow.

Validation outcome:

- Targeted checks passed (`check-pragma-warning-suppressions.ps1` total 0, analyzer tests 142/142, analyzer build succeeded).
- Full `validate-changes.cmd` returned exit code `0` with all summary gates passing in this run.
- Existing repository inconsistency remains: Unity compilation details in the same run showed unrelated pre-existing test compile errors while summary still reported PASS; this behavior predates this change and should be addressed separately.

## Context and Orientation

This repository uses layered enforcement: contributor policy in `AGENTS.md`, scripted quality gates under `.agents/scripts/`, and Roslyn analyzers under `Analyzers/Scaffold/Scaffold.Analyzers/`. The milestone quality gate runs through `.agents/scripts/validate-changes.cmd`, which delegates to `.agents/scripts/validate-changes.ps1`.

In this plan, “suppression governance” means controls for `#pragma warning disable` directives so they are never added casually. “Diff gate” means a script that inspects git diff and fails when newly added suppression directives violate policy. “Allowlist” means a repository file that explicitly lists approved paths/patterns where suppression is accepted. “Analyzer governance rule” means a Roslyn rule that detects suppression directives in runtime code and reports diagnostics.

Key files for this work:

- `AGENTS.md`
- `.agents/scripts/validate-changes.ps1`
- `.agents/scripts/check-pragma-warning-suppressions.ps1`
- `.agents/scripts/pragma-warning-disable-allowlist.txt`
- `Analyzers/Scaffold/Scaffold.Analyzers/PragmaWarningDisableAnalyzer.cs`
- `Analyzers/Scaffold/Scaffold.Analyzers.Tests/PragmaWarningDisableAnalyzerTests.cs`
- `Docs/Analyzers/Analyzers.md`
- `Docs/Testing.md`

## Plan of Work

Update policy text first, then introduce a fast diff gate, then add analyzer enforcement and tests, then update docs and run full validation. Keep changes additive and minimal, with no architecture boundary bypasses.

## Concrete Steps

Run all commands from repository root.

1. Baseline and gate checks:

    powershell -NoProfile -ExecutionPolicy Bypass -File ".\.agents\scripts\check-pragma-warning-suppressions.ps1"

2. Analyzer tests:

    dotnet test .\Analyzers\Scaffold\Scaffold.Analyzers.Tests\Scaffold.Analyzers.Tests.csproj -c Release --nologo

3. Analyzer build:

    dotnet build .\Analyzers\Scaffold\Scaffold.Analyzers\Scaffold.Analyzers.csproj -c Release --nologo

4. Full repository gate:

    .\.agents\scripts\validate-changes.cmd

## Validation and Acceptance

Acceptance criteria achieved:

- New `#pragma warning disable` lines in changed C# files are blocked by default unless allowlisted.
- New disable directives require reason comment and immediate matching restore in the updated file.
- Runtime C# pragma disable usage triggers `SCA0031` diagnostics.
- Docs and testing workflow include the new rule and script gate.

## Idempotence and Recovery

The gate and analyzer changes are idempotent. Re-running the same commands on unchanged code yields stable results.

If the pragma gate blocks intended legacy suppressions, add narrow allowlist entries instead of weakening gate logic.

If analyzer false positives appear, add a failing analyzer test first, then adjust rule logic and rerun full validation.

## Artifacts and Notes

Execution evidence:

    Command: powershell -NoProfile -ExecutionPolicy Bypass -File .\.agents\scripts\check-pragma-warning-suppressions.ps1
    Result: TOTAL:0

    Command: dotnet test .\Analyzers\Scaffold\Scaffold.Analyzers.Tests\Scaffold.Analyzers.Tests.csproj -c Release --nologo
    Result: Passed 142, Failed 0, Skipped 0

    Command: dotnet build .\Analyzers\Scaffold\Scaffold.Analyzers\Scaffold.Analyzers.csproj -c Release --nologo
    Result: Build succeeded. 0 Warning(s). 0 Error(s).

    Command: .\.agents\scripts\validate-changes.cmd
    Result: Scripts asmdef audit PASS (TOTAL:0), Pragma suppression gate PASS (TOTAL:0), Compilation PASS, Tests PASS, Analyzers PASS (TOTAL:0, BLOCKERS:0)

Edited and added files:

- `AGENTS.md`
- `.agents/scripts/check-pragma-warning-suppressions.ps1` (new)
- `.agents/scripts/pragma-warning-disable-allowlist.txt` (new)
- `.agents/scripts/validate-changes.ps1`
- `Analyzers/Scaffold/Scaffold.Analyzers/PragmaWarningDisableAnalyzer.cs` (new)
- `Analyzers/Scaffold/Scaffold.Analyzers.Tests/PragmaWarningDisableAnalyzerTests.cs` (new)
- `Docs/Analyzers/Analyzers.md`
- `Docs/Testing.md`

## Interfaces and Dependencies

The script gate exposes parseable output (`TOTAL` and `ISSUE` lines) and is integrated into `validate-changes.ps1` summary and agent sections.

The analyzer is implemented in namespace `Scaffold.Analyzers`, inherits `DiagnosticAnalyzer`, and uses `AnalyzerConfig.GetEffectiveDescriptor` and `AnalyzerConfig.ShouldSuppress` for `.editorconfig` behavior.

`SCA0031` contract:

- Title: runtime code should not suppress warnings with pragma disable.
- Message starts with `Error SCA0031:` and gives explicit remediation.
- Category: `Architecture`.

---

Revision Note (2026-03-18 / Codex): Created initial ExecPlan to implement pragma suppression governance with immediate script enforcement and long-term analyzer enforcement, based on user-requested policy and workflow changes.
Revision Note (2026-03-18 / Codex): Executed the plan end to end, recorded implementation details and validation evidence, and documented follow-up risk around current `validate-changes` summary/detail mismatch.
