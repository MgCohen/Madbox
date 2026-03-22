# Testing

## Purpose

This guide explains how to execute automated checks in this repository and how the PowerShell validation scripts behave.

For test authoring standards, coverage strategy, examples, and best practices, see [AutomatedTesting.md](AutomatedTesting.md).

## Running Tests And Gates

Run from repository root:

```powershell
& ".\.agents\scripts\validate-changes.cmd"
```

Current pipeline order:

1. `.agents/scripts/check-scripts-asmdef-references.ps1`
2. `.agents/scripts/check-pragma-warning-suppressions.ps1`
3. `.agents/scripts/check-unity-compilation.ps1`
4. `.agents/scripts/run-editmode-tests.ps1` (only if compilation precheck passes)
5. `.agents/scripts/run-playmode-tests.ps1` (only if compilation precheck passes)
6. `.agents/scripts/check-analyzers.ps1`
   Analyzer gate order inside this script:
   1. `dotnet test -c Release Analyzers/Scaffold/Scaffold.Analyzers.Tests/Scaffold.Analyzers.Tests.csproj`
   2. solution analyzer diagnostics build (`dotnet build`)

Coverage collection is fully separate from quality gate execution. `validate-changes.cmd` never generates coverage artifacts.

For explicit coverage audits, run:

```powershell
& ".\.agents\scripts\run-coverage-audit.cmd"
```

Targeted runs:

```powershell
& ".\.agents\scripts\run-editmode-tests.ps1"
& ".\.agents\scripts\run-playmode-tests.ps1"
powershell -ExecutionPolicy Bypass -File ".\.agents\scripts\check-analyzers.ps1"
powershell -ExecutionPolicy Bypass -File ".\.agents\scripts\check-scripts-asmdef-references.ps1"
powershell -ExecutionPolicy Bypass -File ".\.agents\scripts\check-pragma-warning-suppressions.ps1"
```

## Coverage Goals (Practical Targets)
Coverage goals and best practices are documented in [AutomatedTesting.md](AutomatedTesting.md).

## Parameters And Troubleshooting

### Script Parameters

- `check-unity-compilation.ps1`: `-ProjectPath`, `-UnityPath`, `-TimeoutMinutes` (default `10`)
- `run-editmode-tests.ps1`: `-ProjectPath`, `-UnityPath`, `-AssemblyNames`, `-EnableCoverage`, `-CoverageResultsPath`, `-CoverageOptions`, `-TimeoutMinutes` (default `30`)
- `run-playmode-tests.ps1`: `-ProjectPath`, `-UnityPath`, `-AssemblyNames`, `-EnableCoverage`, `-CoverageResultsPath`, `-CoverageOptions`, `-TimeoutMinutes` (default `30`)
- `check-analyzers.ps1`: `-ProjectPath`, `-TimeoutMinutes` (default `10`), `-AnalyzerTestsTimeoutMinutes` (default `10`)
  Default behavior excludes diagnostics from test assemblies. Add `-IncludeTestAssemblies` to include them.
- `check-scripts-asmdef-references.ps1`: `-ProjectPath`, `-ScriptsRoot` (default `Assets/Scripts`), `-ExcludedAssemblyNames`, `-ExcludedGuidReferences`
- `check-pragma-warning-suppressions.ps1`: `-ProjectPath`, `-AllowlistPath` (default `.agents/scripts/pragma-warning-disable-allowlist.txt`)
- `run-coverage-audit.ps1`: `-ProjectPath`, `-UnityPath`, `-AssemblyNames`, `-CoverageResultsPath`, `-CoverageAssemblyFilters`, `-KeepCoverageArtifacts`, `-CompilationTimeoutMinutes`, `-EditModeTimeoutMinutes`, `-PlayModeTimeoutMinutes`
- `validate-changes.ps1`: `-ProjectPath`, `-UnityPath`, `-AssemblyNames`, `-CompilationTimeoutMinutes`, `-EditModeTimeoutMinutes`, `-PlayModeTimeoutMinutes`, `-AnalyzerTimeoutMinutes`, `-AnalyzerTestsTimeoutMinutes`

### Exit Codes (`validate-changes`)

- `0`: compilation precheck passed, tests passed, analyzer checks clean
- `1`: compilation or tests failed/blocked
- `2`: analyzer diagnostics/blockers remain
- `3`: test gate and analyzer gate both failed

### Common Failures

- `Scripts have compiler errors`: fix compile errors first.
- Project already open in another Unity process: close it and rerun.
- Timeout: rerun with a larger timeout while investigating the root cause.

## Related Files

- `.agents/scripts/check-unity-compilation.ps1`
- `.agents/scripts/run-editmode-tests.ps1`
- `.agents/scripts/run-playmode-tests.ps1`
- `.agents/scripts/check-analyzers.ps1`
- `.agents/scripts/check-scripts-asmdef-references.ps1`
- `.agents/scripts/check-pragma-warning-suppressions.ps1`
- `.agents/scripts/run-coverage-audit.cmd`
- `.agents/scripts/run-coverage-audit.ps1`
- `.agents/scripts/validate-changes.cmd`
- `.agents/scripts/validate-changes.ps1`
- `Architecture.md`
- `AGENTS.md`
