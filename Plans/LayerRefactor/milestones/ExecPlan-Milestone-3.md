# ExecPlan Milestone 3 - Analyzer Alignment for Scope Initialization Contracts

## Goal

This milestone aligns startup analyzer metadata names with the runtime contract namespace used in this repository. After completion, `InitializationSameLayerUsageAnalyzer` identifies `IAsyncLayerInitializable` implementations and exception attributes through `Madbox.Scope.Contracts` names, so analyzer diagnostics track the real startup contract surface.

This milestone supports the parent refactor by keeping architecture enforcement accurate while scope APIs are simplified.

## Deliverable

- Analyzer metadata constants updated to `Madbox.Scope.Contracts.*` names.
- Analyzer tests updated to compile sources under `Madbox.Scope.Contracts` and still assert `SCA0026` behavior.
- Verification output showing analyzer unit tests pass and analyzer script reports no blockers caused by this rename/alignment.

## Plan

1. Update `Analyzers/Scaffold/Scaffold.Analyzers/InitializationSameLayerUsageAnalyzer.cs` constants:
   `InitializationInterfaceName`, `AllowUsageAttributeName`, and `AllowCallChainAttributeName`.
2. Update test source snippets in `Analyzers/Scaffold/Scaffold.Analyzers.Tests/InitializationSameLayerUsageAnalyzerTests.cs` from `Madbox.Initialization.Contracts` to `Madbox.Scope.Contracts`.
3. Run analyzer test project and confirm behavior remains the same: diagnostics appear for prohibited same-layer usage and are suppressed only by explicit attributes.
4. Run repository analyzer check script to confirm no new blockers.
5. If any check fails, fix and rerun until clean.

## Validation Commands

Run from repository root `C:\Unity\Madbox`:

    dotnet test "Analyzers/Scaffold/Scaffold.Analyzers.Tests/Scaffold.Analyzers.Tests.csproj" -c Release --nologo
    powershell -NoProfile -ExecutionPolicy Bypass -File ".\.agents\scripts\check-analyzers.ps1"

Expected outcome:

- Analyzer unit tests pass with zero failures.
- `check-analyzers.ps1` reports no new blockers and totals remain acceptable for the parent milestone gate.

## Acceptance Criteria

1. Analyzer symbol lookup targets `Madbox.Scope.Contracts.IAsyncLayerInitializable`.
2. Analyzer attribute checks target `Madbox.Scope.Contracts.AllowSameLayerInitializationUsageAttribute` and `Madbox.Scope.Contracts.AllowInitializationCallChainAttribute`.
3. Analyzer tests still prove both positive and negative scenarios for `SCA0026`.
4. Parent ExecPlan can continue without analyzer namespace drift.

## Notes

This milestone intentionally changes analyzer metadata binding only. It does not broaden rule scope or change diagnostic semantics.
