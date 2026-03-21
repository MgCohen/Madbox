# Scaffold Infra Scope

## TL;DR

- Purpose: reusable layered startup orchestration for application bootstrap.
- Location: `Assets/Scripts/Infra/Scope/Runtime/` and `Assets/Scripts/Infra/Scope/Runtime/Contracts/`.
- Depends on: BCL plus `VContainer` / `VContainer.Unity` in runtime assembly.
- Used by: `Madbox.Bootstrap.Runtime` and services that initialize during layered startup.

## Responsibilities

- Owns `LayeredScope` startup lifecycle.
- Owns `LayerInstallerBase` recursive layer composition and build pipeline.
- Defines initialization contracts and analyzer exception attributes.
- Does not own feature-specific business rules.

## Public API

| Symbol | Purpose | Inputs | Outputs | Failure behavior |
|---|---|---|---|---|
| `LayeredScope` | Coordinates startup with one root layer tree. | Root installer tree + cancellation token. | Initialized final scope and startup completion signal. | Throws on null tree root or startup failures. |
| `LayerInstallerBase` | Concrete recursive installer with deterministic pipeline. | Parent scope and cancellation token. | Built child scope subtree. | Throws on invalid tree topology or initializer failures. |
| `IAsyncLayerInitializable.InitializeAsync(IObjectResolver, CancellationToken)` | Async startup contract for layer services. | Resolver and cancellation token. | Startup completion signal. | Cancellation propagates; non-cancellation failures are wrapped by startup orchestration. |
| `AllowSameLayerInitializationUsageAttribute` | Explicit opt-out for analyzer same-layer initialization rule. | Attribute applied to method/class. | Analyzer metadata for exception handling. | Incorrect usage remains analyzer-visible. |
| `AllowInitializationCallChainAttribute` | Explicit opt-out for transitive call-chain analyzer checks. | Attribute applied to method/class. | Analyzer metadata for exception handling. | Incorrect usage remains analyzer-visible. |

## Setup / Integration

1. Add asmdef reference to `Madbox.Scope` from bootstrap runtime modules.
2. Derive bootstrap composition root from `LayeredScope`.
3. Build one root installer tree via `BuildLayerTree()`.
4. Register startup services implementing `IAsyncLayerInitializable` in installers.
5. Keep startup logic deterministic and idempotent.

## How to Use

1. Create installers by inheriting from `LayerInstallerBase`.
2. Compose the tree with `AddChild(...)`.
3. In each installer, register container services in `Install(...)`.
4. Use `InitializeAsync(...)` for async startup work that depends on resolved services.
5. Keep child customization in `ConfigureChildBuilder(...)`.

## Example

```csharp
public sealed class ConfigWarmupService : IAsyncLayerInitializable
{
    public Task InitializeAsync(IObjectResolver resolver, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.CompletedTask;
    }
}
```

## Best Practices

- Keep startup layers explicit and deterministic.
- Treat `IAsyncLayerInitializable` as startup-only contract.
- Keep initializers side-effect bounded and idempotent.
- Use analyzer exception attributes only with documented rationale.

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

## Related

- `Docs/App/Bootstrap.md`
- `Docs/Analyzers/Analyzers.md`
- `Architecture.md`
- `Docs/Testing.md`

## Changelog

- 2026-03-21: Migrated to recursive `LayerInstallerBase` model, removed delegated child-registration contracts, and simplified `IAsyncLayerInitializable` signature to resolver + cancellation token.
- 2026-03-18: Reworked to module documentation standard and added missing setup/examples/anti-patterns/AI context sections.
- 2026-03-17: Consolidated scope contracts/runtime and documented layered initialization behavior.
