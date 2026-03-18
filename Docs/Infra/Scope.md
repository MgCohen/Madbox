# Scaffold Infra Scope

## TL;DR

- Purpose: reusable layered startup orchestration for application bootstrap.
- Location: `Assets/Scripts/Infra/Scope/Runtime/` and `Assets/Scripts/Infra/Scope/Runtime/Contracts/`.
- Depends on: BCL plus `VContainer` / `VContainer.Unity` in runtime assembly.
- Used by: `Madbox.Bootstrap.Runtime` and services that initialize during layered startup.
- Runtime/Editor: runtime scope orchestration and contracts.

## Responsibilities

- Owns `LayeredScope` startup lifecycle across layer installers.
- Owns `ScopeInitializer` for resolving and awaiting `IAsyncLayerInitializable` services.
- Defines scope initialization contracts and analyzer-exception attributes.
- Does not own game rules, navigation policy, or feature-specific registrations.
- Boundaries: infra bootstrap orchestration only; domain/presentation behavior lives in other modules.

## Public API

| Symbol | Purpose | Inputs | Outputs | Failure behavior |
|---|---|---|---|---|
| `LayeredScope` | Coordinates layered startup in deterministic order. | Layer installer callbacks and cancellation token. | Initialized scope graph and startup completion. | Throws on critical setup failures to prevent invalid runtime startup. |
| `ScopeInitializer` | Resolves and runs `IAsyncLayerInitializable` instances safely. | Container scope and cancellation token. | Completed initialization tasks for a layer. | Wraps/propagates initialization failures with scope context. |
| `IAsyncLayerInitializable.InitializeAsync(CancellationToken)` | Contract for async startup work. | Cancellation token. | Asynchronous initialization completion signal. | Implementations decide recoverable vs fatal failures. |
| `AllowSameLayerInitializationUsageAttribute` | Explicit opt-out for analyzer same-layer initialization rule. | Attribute applied to method/class. | Analyzer metadata for exception handling. | Incorrect usage remains analyzer-visible. |
| `AllowInitializationCallChainAttribute` | Explicit opt-out for transitive call-chain analyzer checks. | Attribute applied to method/class. | Analyzer metadata for exception handling. | Incorrect usage remains analyzer-visible. |

## Setup / Integration

1. Add asmdef reference to `Madbox.Scope` from bootstrap runtime modules.
2. Derive bootstrap composition root from `LayeredScope`.
3. Register startup services implementing `IAsyncLayerInitializable` in the correct layer installer.
4. Use exception attributes sparingly and document why they are required.
5. Fast check: startup logs should show layer initialization in expected deterministic order.

## How to Use

1. Create or update your bootstrap scope to install layers in the order required by architecture (`Infra -> Core -> Meta -> Game -> App`).
2. Implement `IAsyncLayerInitializable` for services that need async startup logic.
3. Keep each initializer focused on its own service readiness and dependency-safe behavior.
4. Run scope tests and analyzer checks after adding/changing initialization behavior.

## Examples

### Minimal

```csharp
public sealed class ConfigWarmupService : IAsyncLayerInitializable
{
    public Task InitializeAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
```

### Realistic

```csharp
public sealed class GameBootstrapScope : LayeredScope
{
    protected override void InstallInfra(IContainerBuilder builder)
    {
        builder.Register<ConfigWarmupService>(Lifetime.Singleton).As<IAsyncLayerInitializable>();
    }
}
```

### Guard / Error path

```csharp
public sealed class RemoteConfigInitializer : IAsyncLayerInitializable
{
    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        bool loaded = await TryLoadAsync(cancellationToken);
        if (!loaded)
        {
            throw new InvalidOperationException("Remote config was required but failed to load.");
        }
    }
}
```

## Best Practices

- Keep startup layers deterministic and explicit.
- Treat `IAsyncLayerInitializable` as a narrow startup contract, not a general service lifecycle API.
- Keep initializer side effects local and idempotent where possible.
- Use analyzer exception attributes only with documented justification.
- Verify startup behavior through scope/bootstrap tests, not manual scene inspection only.

## Anti-Patterns

- Resolving services ad hoc from container in random runtime code.
  Migration: register dependencies in installers and consume via constructor injection.
- Running feature logic in bootstrap initializers.
  Migration: move feature behavior to runtime services and keep initializer to readiness checks.
- Ignoring initialization ordering requirements.
  Migration: encode required order in installer sequence and tests.

## Testing

- Test assemblies:
  - `Madbox.Scope.Tests`
  - `Madbox.Bootstrap.Tests`
- Run:

```powershell
& ".\.agents\scripts\run-editmode-tests.ps1" -AssemblyNames "Madbox.Scope.Tests"
& ".\.agents\scripts\run-editmode-tests.ps1" -AssemblyNames "Madbox.Bootstrap.Tests"
dotnet test "Analyzers/Scaffold/Scaffold.Analyzers.Tests/Scaffold.Analyzers.Tests.csproj" -c Release --nologo
```

- Expected pass signal: all listed test runs pass with zero failures.
- Bugfix rule: any scope/bootstrap bug fix must include a regression test that fails before the fix and passes after.

## AI Agent Context

- Invariants:
  - Layer initialization order remains deterministic.
  - Scope module owns startup orchestration, not feature behavior.
- Allowed Dependencies:
  - BCL, `VContainer`, and `VContainer.Unity`.
- Forbidden Dependencies:
  - UI/gameplay domain implementation details that do not belong to startup infra.
  - Unity presentation logic in scope orchestration classes.
- Change Checklist:
  - Confirm installer ordering still matches architecture.
  - Update/verify tests for changed initializers.
  - Run analyzer checks and validation gate.
- Known Tricky Areas:
  - Same-layer initialization exceptions can hide coupling if overused.
  - Async startup failures can surface only in composed bootstrap tests.

## Related

- `Docs/App/Bootstrap.md`
- `Docs/Analyzers/Analyzers.md`
- `Architecture.md`
- `Docs/Testing.md`

## Changelog

- 2026-03-18: Reworked to module documentation standard and added missing setup/examples/anti-patterns/AI context sections.
- 2026-03-17: Consolidated scope contracts/runtime and documented layered initialization behavior.
