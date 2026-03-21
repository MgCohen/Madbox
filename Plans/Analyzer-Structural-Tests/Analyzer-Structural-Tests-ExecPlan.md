# Add Structural Graph Test Infrastructure for Analyzer Rules

This ExecPlan is a living document. The sections `Progress`, `Surprises & Discoveries`, `Decision Log`, and `Outcomes & Retrospective` must be kept up to date as work proceeds.

This document must be maintained in accordance with `PLANS.md` at the repository root.

## Purpose / Big Picture

After this change, analyzer rules that depend on project-wide structure (assembly dependencies, same-layer call chains, external usage patterns) can be verified through deterministic structural tests instead of ad-hoc source snippets. This allows us to validate behaviors such as “externally consumed runtime API requires invariants” and “same-layer dependency members cannot be consumed during initialization call chains” with higher confidence and less brittle setup.

A contributor can verify the improvement by running analyzer tests and observing that graph-dependent rules are tested through explicit structural fixtures (assembly graph + source graph), including cases that were previously hard to model with single-file inputs.

## Progress

- [x] (2026-03-21 00:00Z) Authored initial ExecPlan with architecture, test-harness strategy, and phased migration path.
- [ ] Implement structural graph fixture model and builder utilities in analyzer tests.
- [ ] Integrate fixture model into `AnalyzerTestHarness` without breaking existing tests.
- [ ] Add structural test coverage for `SCA0012`/`SCA0017` external usage scenarios.
- [ ] Add structural test coverage for `SCA0026` same-layer initialization call-chain scenarios.
- [ ] Refactor or extend `SCA0022` tests to use shared structural fixtures where it reduces duplication.
- [ ] Update analyzer documentation and test guidance.
- [ ] Run analyzer tests, analyzer build, and `.agents/scripts/validate-changes.cmd`; iterate until clean.

## Surprises & Discoveries

- Observation: Existing tests already simulate parts of a graph (stub metadata references, temporary workspace asmdef files), but the setup is duplicated per analyzer and not formalized as one reusable model.
  Evidence: `AnalyzerTestHarness.cs` creates synthetic referenced assemblies; `RuntimeAssemblyBoundaryAnalyzerTests.cs` manually creates temp workspaces and asmdef trees.

- Observation: `SCA0026` mostly reasons from source symbol paths and call-chain data, while `SCA0022` reasons over referenced assembly names and filesystem module layouts.
  Evidence: `InitializationSameLayerUsageAnalyzer.cs` infers layer from `/Assets/Scripts/` source paths; `RuntimeAssemblyBoundaryAnalyzer.cs` scans referenced assemblies and `Contracts`/`Runtime` asmdef locations.

## Decision Log

- Decision: Start with a test-only structural graph fixture before introducing shared production analyzer graph services.
  Rationale: This captures value quickly, reduces regression risk, and avoids prematurely coupling analyzer runtime logic to a new abstraction.
  Date/Author: 2026-03-21 / Codex

- Decision: Model two graph dimensions explicitly in tests: assembly graph and source usage graph.
  Rationale: Current rules need both kinds of evidence (assembly references, file path layer ownership, call chains, external consumer mentions).
  Date/Author: 2026-03-21 / Codex

- Decision: Preserve backwards compatibility for existing analyzer tests while gradually migrating graph-dependent suites.
  Rationale: Big-bang harness rewrites are fragile; staged migration keeps feedback loops short.
  Date/Author: 2026-03-21 / Codex

## Outcomes & Retrospective

Not completed yet. Expected outcome is a reusable, deterministic structural test method for analyzer rules with fewer one-off fixtures and stronger cross-assembly scenario coverage.

## Context and Orientation

The analyzer implementation lives under `Analyzers/Scaffold/Scaffold.Analyzers/`, and analyzer tests live under `Analyzers/Scaffold/Scaffold.Analyzers.Tests/`. Today, tests mostly construct a single compilation plus optional stub references. This is enough for syntax-local rules, but graph-sensitive rules are harder to express and often duplicate setup logic.

In this plan, “structural graph” means a deterministic test representation of project structure that includes:

- Assembly nodes (for example `Madbox.MainMenu.Runtime`, `Madbox.Meta.Gold.Runtime`, `Madbox.Meta.Gold.Contracts`).
- Directed assembly references between those nodes.
- Source files attached to assemblies with repository-like paths (especially under `Assets/Scripts/...`) so analyzers can derive layer/module context.
- Optional source-level usage links (for example a consumer assembly mentioning a type or interface in another assembly).

This structural graph is a test fixture. It is not a runtime Unity feature and not a gameplay dependency.

Key files expected to change:

- `Analyzers/Scaffold/Scaffold.Analyzers.Tests/AnalyzerTestHarness.cs`
- New helper file(s) under `Analyzers/Scaffold/Scaffold.Analyzers.Tests/` for graph fixture modeling/building
- `Analyzers/Scaffold/Scaffold.Analyzers.Tests/InitializationSameLayerUsageAnalyzerTests.cs`
- `Analyzers/Scaffold/Scaffold.Analyzers.Tests/RuntimeAssemblyBoundaryAnalyzerTests.cs`
- `Analyzers/Scaffold/Scaffold.Analyzers.Tests/*Invariant*AnalyzerTests.cs` (where `SCA0012`/`SCA0017` external-consumption behavior is currently covered)
- `Docs/Analyzers/Analyzers.md`
- `Docs/AutomatedTesting.md` (if analyzer test guidance requires explicit structural test pattern documentation)

## Plan of Work

First, introduce a small test DSL (domain-specific language, meaning a fluent helper API) that describes assemblies, references, and source files as one fixture object. The fixture builder will emit Roslyn compilations and metadata references in the correct order so tests can express realistic graph scenarios with less boilerplate.

Second, extend `AnalyzerTestHarness` with new overloads that accept the graph fixture while preserving existing string-based methods. Existing tests remain valid. New structural tests can then be added incrementally.

Third, add focused graph tests for rules with real structural needs:

- `SCA0012` and `SCA0017`: verify external-consumption detection through cross-assembly type/interface usage.
- `SCA0026`: verify same-layer call-chain restrictions across helper methods and forwarding paths using realistic file paths and assembly partitions.
- `SCA0022`: migrate selected cases to shared fixture utilities where it simplifies setup, while keeping temp-workspace cases if filesystem behavior must remain integration-like.

Finally, update docs and run full validation gates.

## Concrete Steps

Run all commands from repository root (`C:\Unity\Madbox`).

1. Run analyzer tests before changes to establish baseline:

    dotnet test .\Analyzers\Scaffold\Scaffold.Analyzers.Tests\Scaffold.Analyzers.Tests.csproj -c Release --nologo

2. Implement structural fixture helpers and harness overloads.

3. Add/adjust analyzer tests for `SCA0012`, `SCA0017`, `SCA0026`, and selected `SCA0022` cases.

4. Re-run analyzer tests:

    dotnet test .\Analyzers\Scaffold\Scaffold.Analyzers.Tests\Scaffold.Analyzers.Tests.csproj -c Release --nologo

5. Rebuild analyzers:

    dotnet build .\Analyzers\Scaffold\Scaffold.Analyzers\Scaffold.Analyzers.csproj -c Release --nologo

6. Run repository quality gate:

    .\.agents\scripts\validate-changes.cmd

Expected outcomes:

- Analyzer tests pass with additional structural coverage.
- Analyzer build succeeds with zero new warnings/errors.
- `validate-changes` gate is clean (or any pre-existing unrelated failures are identified explicitly).

## Validation and Acceptance

Acceptance criteria for this plan:

- Structural fixture API exists and is used by at least two graph-dependent analyzer suites.
- `SCA0012` and/or `SCA0017` include a regression-style test that proves external consumption is detected from cross-assembly evidence, not only local syntax.
- `SCA0026` includes structural call-chain tests proving transitive same-layer dependency usage detection remains correct.
- `SCA0022` tests remain green and at least one duplicated setup pattern is replaced by shared fixture code.
- Full analyzer test run and repository validation gate complete successfully.

## Idempotence and Recovery

The fixture-based test infrastructure is additive. If migration of a test class introduces failures, the fallback is to keep old tests intact and migrate class-by-class. Harness overloads must not break current call sites.

If fixture abstraction becomes too complex, reduce scope: keep low-level fixture model internal and expose only minimal helper methods used by current graph-dependent tests.

If filesystem-sensitive behavior cannot be represented purely in-memory (for example `SCA0022` contracts folder probing), keep temporary workspace helpers for those cases and treat them as integration-style tests.

## Artifacts and Notes

Proposed fixture shape (illustrative, not final API):

    var graph = StructuralTestGraph
        .Create("Madbox.MainMenu.Runtime")
        .Assembly("Madbox.MainMenu.Runtime")
            .WithSource("Assets/Scripts/App/MainMenu/Runtime/MenuPresenter.cs", sourceMainMenu)
            .References("Madbox.Meta.Gold.Runtime")
        .Assembly("Madbox.Meta.Gold.Runtime")
            .WithSource("Assets/Scripts/Meta/Gold/Runtime/GoldService.cs", sourceGold)
        .Assembly("Madbox.Meta.Gold.Contracts")
            .WithSource("Assets/Scripts/Meta/Gold/Contracts/IGoldService.cs", sourceContract);

    var diagnostics = await AnalyzerTestHarness.GetDiagnosticsByIdAsync(
        graph,
        new RuntimeAssemblyBoundaryAnalyzer(),
        RuntimeAssemblyBoundaryAnalyzer.DiagnosticId);

Keep this API small and explicit. The objective is readability and deterministic graph setup, not a generic test framework.

## Interfaces and Dependencies

The structural fixture should remain in test scope only (`Scaffold.Analyzers.Tests`). Production analyzers should continue to consume Roslyn APIs and existing utilities unless a later milestone proves shared graph services are necessary.

Expected helper interfaces/classes (names may vary, but responsibilities should match):

- `StructuralTestGraph`: immutable test graph definition.
- `StructuralAssemblyNode`: assembly-level data (name, sources, references, optional analyzer options).
- `StructuralGraphBuilder` (or equivalent): compiles nodes into Roslyn compilations/references and executes analyzers.

Keep dependencies limited to:

- `Microsoft.CodeAnalysis` / `Microsoft.CodeAnalysis.CSharp`
- Existing xUnit test infrastructure
- Existing analyzer harness infrastructure

Avoid introducing external packages for graph modeling unless there is a demonstrated gap.

---

Revision Note (2026-03-21 / Codex): Created initial ExecPlan to add structural graph-based analyzer test infrastructure, with staged migration for `SCA0012`, `SCA0017`, `SCA0022`, and `SCA0026`.
