# Add Child Scope Registration Delegation to Layered Scope Startup

This ExecPlan is a living document. The sections `Progress`, `Surprises & Discoveries`, `Decision Log`, and `Outcomes & Retrospective` must be kept up to date as work proceeds.

This document must be maintained in accordance with `PLANS.md` at the repository root.

## Purpose / Big Picture

After this change, every async layer initializer receives both an initialization context and `IObjectResolver` in a single required signature, and can publish child-scope registrations while it runs. Those delegated registrations are automatically replayed when later child scopes are created. This enables dynamic wiring discovered at initialization time without bypassing `LayeredScope` or introducing multiple initializer entry points.

The new behavior is observable when an initializer adds registrations (`service type`, `instance`, `lifetime`, `delegation scope policy`) through the provided context and a later layer scope resolves those entries from its container without additional manual registration calls.

## Progress

- [x] (2026-03-18 12:35Z) Authored initial ExecPlan with child registration delegation scope and milestone validation loop.
- [x] (2026-03-18 13:12Z) Execute Milestone 1: Added Scope regression tests for delegated child registration behavior (`NextChildOnly`, `AllDescendants`, lifetime validation) and updated existing initializer tests to the new signature.
- [x] (2026-03-18 13:21Z) Execute Milestone 2: Broke `IAsyncLayerInitializable` to use context+resolver, implemented delegated child registration replay in `LayeredScope`, and updated Addressables initializer integration.
- [x] (2026-03-18 13:28Z) Execute Milestone 3: Updated `Docs/Infra/Scope.md` with signature and usage snippets, fixed analyzer diagnostics, and achieved clean `.agents/scripts/validate-changes.cmd`.

## Surprises & Discoveries

- Observation: Current scope creation has a single child-scope construction point in `LayeredScope.CreateAndInitializeLayerScopeAsync`, which is ideal for deterministic registration replay.
  Evidence: `Assets/Scripts/Infra/Scope/Runtime/LayeredScope.cs` creates each layer child scope with `parentScope.CreateChild(...)` in one method.
- Observation: Existing async initializer contract has only `InitializeAsync(CancellationToken)`, so this plan must update all implementations and tests in one milestone when breaking the signature.
  Evidence: `Assets/Scripts/Infra/Scope/Runtime/Contracts/IAsyncLayerInitializable.cs`.
- Observation: Introducing `IObjectResolver` in a public contract required direct `VContainer` asmdef references in consuming assemblies, including test assemblies.
  Evidence: compilation errors were resolved by adding `VContainer` to `Madbox.Addressables`, `Madbox.Addressables.Tests`, and `Madbox.Scope.Tests` asmdefs.
- Observation: Analyzer policy required strict method/member ordering and explicit guard clauses in private nested helper types.
  Evidence: initial validation surfaced SCA0003/SCA0005/SCA0006/SCA0009/SCA0012/SCA0014/SCA0017/SCA0020; final analyzer run returned `TOTAL:0`.

## Decision Log

- Decision: Break `IAsyncLayerInitializable` to a single required signature that includes both initialization context and `IObjectResolver`.
  Rationale: This removes parallel API paths and keeps one initializer entry point for all modules.
  Date/Author: 2026-03-18 / Codex
- Decision: Store delegated registrations in a scope-owned in-memory list and replay them during child scope builder configuration.
  Rationale: This matches the requested flow (collect during async initialize, apply at child build/registration time) and keeps startup deterministic.
  Date/Author: 2026-03-18 / Codex
- Decision: Use one context API that directly registers delegated entries instead of separate sink/definition abstraction layers.
  Rationale: A single context surface is easier to understand and reduces extra contracts while preserving explicit policy and type information.
  Date/Author: 2026-03-18 / Codex
- Decision: Include `VContainer.Lifetime` in delegated registration APIs and enforce singleton-only lifetime for delegated instances.
  Rationale: This captures requested registration metadata while keeping container semantics explicit and safe.
  Date/Author: 2026-03-18 / Codex

## Outcomes & Retrospective

Implemented outcome: async layer initialization now uses one signature (`ILayerInitializationContext`, `IObjectResolver`, `CancellationToken`), and initializers can delegate child registrations with explicit `Lifetime` and propagation policy. `LayeredScope` now replays delegated registrations during child scope build before installer execution.

Validation outcome:

- `Madbox.Scope.Tests`: 6 passed, 0 failed.
- `Madbox.Addressables.Tests`: 17 passed, 0 failed.
- `.agents/scripts/validate-changes.cmd`: compilation PASS, EditMode 152/152, PlayMode 2/2, analyzers `TOTAL:0`.

## Context and Orientation

The Scope module is under `Assets/Scripts/Infra/Scope/`. Runtime orchestration lives in:

- `Assets/Scripts/Infra/Scope/Runtime/LayeredScope.cs`
- `Assets/Scripts/Infra/Scope/Runtime/ScopeInitializer.cs`
- `Assets/Scripts/Infra/Scope/Runtime/Contracts/IAsyncLayerInitializable.cs`
- `Assets/Scripts/Infra/Scope/Runtime/Contracts/ILayerInstaller.cs`

Current behavior is:

1. `LayeredScope` builds child scopes in deterministic installer order.
2. `ScopeInitializer` resolves `IAsyncLayerInitializable` instances in the new scope and awaits them.
3. No built-in API exists for an initializer to publish registrations that should be applied to future child scopes.

Term definitions used in this plan:

- Child scope registration delegation: allowing startup code to publish container registrations during initialization so those registrations are applied automatically when later child scopes are built.
- Delegation scope policy: a small enum/value that decides how far a delegated registration should propagate (for this plan: at minimum to immediate children; optionally to all descendants if requested).
- Initialization context: an object passed to every initializer that exposes child-registration delegation methods.

## Plan of Work

Milestone 1 starts with tests in `Assets/Scripts/Infra/Scope/Tests/ScopeInitializerTests.cs` (and a new focused test file if needed) to encode required behavior before implementation. Add tests that fail before code changes for:

1. An initializer using the new signature can call context registration during `InitializeAsync`.
2. The next child scope receives and resolves that delegated registration at build time.
3. `IObjectResolver` is available during initialization and can resolve expected dependencies.
4. Duplicate initialization does not duplicate delegated registration replay unintentionally.

Milestone 2 updates contracts under `Assets/Scripts/Infra/Scope/Runtime/Contracts/`:

1. Change `IAsyncLayerInitializable` to:

    Task InitializeAsync(ILayerInitializationContext context, IObjectResolver resolver, CancellationToken cancellationToken);

2. Add `ILayerInitializationContext` with direct registration methods for delegated child scope entries (no separate sink interface).
3. Add a small policy type (for example `ChildScopeDelegationPolicy`) to indicate whether an entry applies to the next child only or to descendants.
4. Include `VContainer.Lifetime` in delegated registration methods.

Runtime implementation changes in `Assets/Scripts/Infra/Scope/Runtime/`:

1. Update `ScopeInitializer` to invoke the new signature for all initializers and provide both context and resolver.
2. Add a runtime store that collects delegated definitions during current layer initialization.
3. Update `LayeredScope.CreateAndInitializeLayerScopeAsync` to apply stored delegated definitions to the child builder before calling `installer.Install(builder)`.
4. Ensure replay ordering is deterministic and idempotent for repeated initializer instances already tracked by `ScopeInitializer`.
5. Update all existing initializer implementations and tests in repo that compile against `IAsyncLayerInitializable`.

Milestone 3 updates documentation and final validation:

1. Expand `Docs/Infra/Scope.md` public API and usage sections with the new initializer signature and delegation flow.
2. Include a concrete example showing an initializer that publishes delegated registrations and a later layer consuming them.
3. Run required quality gate and capture evidence in this plan sections.

## Concrete Steps

Run commands from repository root: `C:\Users\mtgco\.codex\worktrees\c80f\Madbox`.

1. Baseline scope tests before edits:

    powershell -NoProfile -ExecutionPolicy Bypass -File ".\.agents\scripts\run-editmode-tests.ps1" -AssemblyNames "Madbox.Scope.Tests"

2. After adding tests for delegated registrations (expect fail-before if behavior not implemented yet):

    powershell -NoProfile -ExecutionPolicy Bypass -File ".\.agents\scripts\run-editmode-tests.ps1" -AssemblyNames "Madbox.Scope.Tests"

3. After runtime implementation changes:

    powershell -NoProfile -ExecutionPolicy Bypass -File ".\.agents\scripts\run-editmode-tests.ps1" -AssemblyNames "Madbox.Scope.Tests"

4. Milestone quality gate:

    .\.agents\scripts\validate-changes.cmd

5. If quality gate fails, fix reported issues and rerun:

    .\.agents\scripts\validate-changes.cmd

Expected success signals:

- Scope tests pass with new delegation assertions.
- Existing initializer tests remain green.
- `.agents/scripts/validate-changes.cmd` reports a clean result (`TOTAL:0` analyzers and passing tests).

## Validation and Acceptance

Acceptance is met when all behaviors below are observable:

1. An initializer can publish one or more delegated registrations during async initialization through `ILayerInitializationContext`.
2. A later-created child scope resolves those delegated registrations without manual extra installer wiring.
3. `IObjectResolver` is available to initializers through the new signature and can be used during initialization.
4. Delegation replay follows declared scope policy and deterministic ordering.
5. `Docs/Infra/Scope.md` documents the new API and includes a realistic usage example.
6. `.agents/scripts/validate-changes.cmd` passes.

Regression expectation for this feature implementation:

- New tests proving delegation flow must fail before implementation and pass after implementation to demonstrate real behavior change.

## Idempotence and Recovery

The implementation is additive and safe to rerun. Re-running tests and validation scripts is safe.

If implementation introduces startup failures:

1. Keep tests in place.
2. Temporarily disable only the new delegation replay path behind a narrow conditional while preserving the new initializer invocation path.
3. Re-run `Madbox.Scope.Tests` to confirm baseline behavior is restored.
4. Reintroduce delegation changes incrementally until all tests and gate checks pass.

Avoid destructive cleanup commands; adjust only the touched Scope files and tests.

## Artifacts and Notes

Expected files to change:

- `Assets/Scripts/Infra/Scope/Runtime/LayeredScope.cs`
- `Assets/Scripts/Infra/Scope/Runtime/ScopeInitializer.cs`
- `Assets/Scripts/Infra/Scope/Runtime/Contracts/IAsyncLayerInitializable.cs` (signature update)
- `Assets/Scripts/Infra/Scope/Runtime/Contracts/ILayerInitializationContext.cs` (new)
- `Assets/Scripts/Infra/Scope/Runtime/Contracts/ChildScopeDelegationPolicy.cs` (new, if needed for policy clarity)
- `Assets/Scripts/Infra/Scope/Tests/ScopeInitializerTests.cs`
- `Docs/Infra/Scope.md`

Evidence snippets to append during execution:

    Fail-before test summary for new delegation tests.
    Pass-after test summary for `Madbox.Scope.Tests`.
    Final `.agents/scripts/validate-changes.cmd` clean summary.

Planned usage snippets for docs and tests:

    // New single initializer signature with context + resolver.
    public sealed class DynamicWarmupInitializer : IAsyncLayerInitializable
    {
        public Task InitializeAsync(
            ILayerInitializationContext context,
            IObjectResolver resolver,
            CancellationToken cancellationToken)
        {
            var config = resolver.Resolve<RuntimeConfig>();
            var service = new RuntimeConfigProvider(config);
            context.RegisterInstanceForChild(
                typeof(IRuntimeConfigProvider),
                service,
                Lifetime.Singleton,
                ChildScopeDelegationPolicy.AllDescendants);
            return Task.CompletedTask;
        }
    }

    // Later layer can resolve delegated service without extra installer registration.
    public sealed class ConsumerInitializer : IAsyncLayerInitializable
    {
        public Task InitializeAsync(
            ILayerInitializationContext context,
            IObjectResolver resolver,
            CancellationToken cancellationToken)
        {
            var provider = resolver.Resolve<IRuntimeConfigProvider>();
            return provider.WarmupAsync(cancellationToken);
        }
    }

## Interfaces and Dependencies

Required interfaces and types at completion:

1. `Madbox.Scope.Contracts.IAsyncLayerInitializable` is updated to:

    Task InitializeAsync(ILayerInitializationContext context, IObjectResolver resolver, CancellationToken cancellationToken);

2. `Madbox.Scope.Contracts.ILayerInitializationContext` exposes direct child-registration delegation APIs (no second entry-point interface).
3. `Madbox.Scope.Contracts.ChildScopeDelegationPolicy` (or equivalent small enum) defines propagation behavior.
4. Delegated registration APIs carry `VContainer.Lifetime`.

Dependency and boundary requirements:

1. Keep Scope runtime limited to BCL + `VContainer`/`VContainer.Unity`.
2. Do not introduce Unity presentation logic into scope contracts/runtime orchestration.
3. Keep analyzer compliance clean and respect existing asmdef boundaries.

## Revision Note

2026-03-18: Initial ExecPlan created to deliver child scope registration delegation from async initializers.
2026-03-18: Revised plan to use one breaking `IAsyncLayerInitializable` signature with `ILayerInitializationContext` + `IObjectResolver`, removed parallel initializer/sink API paths, and added concrete usage snippets.
2026-03-18: Executed plan end-to-end with lifetime-aware delegated registration, updated tests/docs/asmdefs, and closed quality gates cleanly.
