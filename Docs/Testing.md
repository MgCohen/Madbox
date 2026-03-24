# Testing (repository scripts)

## TL;DR

- Purpose: How to run automated checks in this repository and how PowerShell validation scripts behave.
- Location: `.agents/scripts/` (see file list under Related).
- Depends on: Unity Editor (for test scripts), PowerShell, optional `UNITY_PATH` / `-UnityPath`.
- Used by: Contributors and CI; milestone gate `validate-changes.cmd`.
- Runtime/Editor: host-machine scripts invoking Unity batch/test modes.

Keywords: validate-changes, EditMode, PlayMode, analyzers, PowerShell

## Responsibilities

- Owns: Documentation for `validate-changes.cmd` pipeline order, script parameters, exit codes, troubleshooting.
- Does not own: Test authoring strategy (see `Docs/AutomatedTesting.md`), module-specific APIs.
- Boundaries: Execution and tooling; link to `Architecture.md` for module map.

## Public API

| Symbol / script | Purpose | Inputs | Outputs | Failure behavior |
|---|---|---|---|---|
| `validate-changes.cmd` | Full quality gate (compile, tests, analyzers). | Optional args via inner `.ps1` | Exit code 0–3 | Non-zero on failure (see below). |
| `run-editmode-tests.ps1` | Headless EditMode tests. | `-ProjectPath`, `-UnityPath`, `-AssemblyNames`, … | Console + XML | Non-zero on test failure. |
| `run-playmode-tests.ps1` | Headless PlayMode tests. | Same family | Console + XML | Non-zero on failure. |
| `check-analyzers.ps1` | Analyzer diagnostics + analyzer test project. | `-ProjectPath`, … | Deduped diagnostics | Non-zero if blockers remain. |

## Setup / Integration

1. Clone repo; stay at repository root for commands below.
2. Install Unity version from `ProjectSettings/ProjectVersion.txt` (or set `-UnityPath` / `UNITY_PATH`).
3. On Windows PowerShell 5.x, do **not** use `&&` to chain — use `;` or `cmd /c` (see How to Use).

## How to Use

1. Run the full gate from the repo root:

```powershell
& ".\.agents\scripts\validate-changes.cmd"
```

2. **PowerShell 5.x:** `&&` is not a statement separator. Use `;`, or:

```powershell
Set-Location "c:\path\to\Madbox"; cmd /c ".agents\scripts\validate-changes.cmd"
```

```powershell
cmd /c "cd /d c:\path\to\Madbox && .agents\scripts\validate-changes.cmd"
```

3. **Pipeline order** inside the gate:

   1. `check-scripts-asmdef-references.ps1`
   2. `check-pragma-warning-suppressions.ps1`
   3. `check-unity-compilation.ps1`
   4. `run-editmode-tests.ps1` (if compile precheck passes)
   5. `run-playmode-tests.ps1` (if compile precheck passes)
   6. `check-analyzers.ps1` (analyzer tests + solution build)

4. **Targeted runs:**

```powershell
& ".\.agents\scripts\run-editmode-tests.ps1"
& ".\.agents\scripts\run-playmode-tests.ps1"
powershell -ExecutionPolicy Bypass -File ".\.agents\scripts\check-analyzers.ps1"
```

5. **Coverage** (separate from the default gate): `run-coverage-audit.cmd` — see `AutomatedTesting.md` for strategy.

## Examples

### Minimal

```powershell
& ".\.agents\scripts\validate-changes.cmd"
```

### Realistic

```powershell
& ".\.agents\scripts\run-editmode-tests.ps1" -AssemblyNames "Madbox.Bootstrap.Tests"
```

### Guard / Error path

```powershell
# Exit 1: compile/tests failed; Exit 2: analyzer diagnostics; Exit 3: both
# "Scripts have compiler errors" — fix compile before tests run
```

## Best Practices

- Run from repo root unless passing explicit `-ProjectPath`.
- Close duplicate Unity instances before headless test runs.
- Read `AGENTS.md` and `Docs/AutomatedTesting.md` before authoring tests.

## Anti-Patterns

- Chaining with `&&` on Windows PowerShell 5.x.
- Expecting coverage artifacts from `validate-changes.cmd` — coverage is opt-in via audit script.

## Testing

- This document describes **how to run** tests; it is not backed by a test assembly.
- Validation: run `validate-changes.cmd` after doc/script changes affecting the gate; expect exit code `0`.
- Bugfix rule: if a script regression is fixed, add or update a test **for that script** only if the repo introduces script tests; otherwise verify manually with a full gate run.

## AI Agent Context

- Invariants: `validate-changes.cmd` is the default milestone gate; scripts resolve Unity via `-UnityPath`, `UNITY_PATH`, or `ProjectVersion.txt`.
- Allowed Dependencies: Documented scripts under `.agents/scripts/`.
- Forbidden Dependencies: N/A (documentation).
- Change Checklist: update this doc when pipeline order or parameters change; keep `AGENTS.md` in sync.
- Known Tricky Areas: PowerShell version differences; Unity exclusive lock.

## Related

- `Docs/AutomatedTesting.md`
- `Architecture.md`
- `AGENTS.md`
- `.agents/scripts/check-unity-compilation.ps1`
- `.agents/scripts/run-editmode-tests.ps1`
- `.agents/scripts/run-playmode-tests.ps1`
- `.agents/scripts/check-analyzers.ps1`
- `.agents/scripts/validate-changes.cmd`
- `.agents/scripts/validate-changes.ps1`

## Changelog

- 2025-03-23: Restructured to `Module-Documentation-Standard.md` (preserved script list and pipeline order).
