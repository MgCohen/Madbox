# Scaffold Infra Scope

## TL;DR

- Purpose: reusable startup scope orchestration and contracts shared by app bootstraps.
- Location: `Assets/Scripts/Infra/Scope/Runtime/` (boundary types under `Runtime/Contracts/`).
- Depends on: BCL + `VContainer`/`VContainer.Unity` in runtime assembly.
- Used by: `Madbox.Bootstrap.Runtime` and runtime services that participate in startup initialization.
- Runtime/Editor: runtime contracts + reusable runtime scope orchestration.
- Keywords: layered scope, startup, async initialization, layer barrier, exceptions.

## Responsibilities

- Owns `LayeredScope`, the shared startup lifecycle between `LifetimeScope` and app-specific bootstrap implementations.
- Owns `ScopeInitializer`, which resolves and awaits `IAsyncLayerInitializable` services per created layer.
- Defines `IAsyncLayerInitializable` and explicit exception attributes consumed by analyzer enforcement.
- Does not own project-specific registrations, game rules, or first-screen routing policy.

## Public API

| Symbol | Purpose |
|---|---|
| `LayeredScope` | Generic startup orchestration: validate, create child scopes per layer, await initializers, and finalize startup. |
| `ScopeInitializer` | Resolves and runs pending `IAsyncLayerInitializable` instances with failure wrapping and duplicate-instance skipping. |
| `IAsyncLayerInitializable.InitializeAsync(CancellationToken)` | Async startup entry point for services that need bootstrap-time initialization. |
| `AllowSameLayerInitializationUsageAttribute` | Explicit opt-out for same-layer startup usage rule on method/class scope. |
| `AllowInitializationCallChainAttribute` | Explicit opt-out for transitive call-chain analysis on helper methods. |

## How to Use

1. Implement `IAsyncLayerInitializable` in runtime services that need startup work.
2. Keep `InitializeAsync` focused on local initialization and safe pass/store of references.
3. If a same-layer usage exception is required, apply an explicit attribute and document why.
4. Derive your concrete bootstrap from `LayeredScope` and override layer installers/completion hooks.

## Testing

- Scope runtime tests:
  - `Madbox.Scope.Tests`
- Also validated by bootstrap behavior tests and analyzer tests.
- Run from repo root:

```powershell
& ".\.agents\scripts\run-editmode-tests.ps1" -AssemblyNames "Madbox.Scope.Tests"
& ".\.agents\scripts\run-editmode-tests.ps1" -AssemblyNames "Madbox.Bootstrap.Tests"
dotnet test "Analyzers/Scaffold/Scaffold.Analyzers.Tests/Scaffold.Analyzers.Tests.csproj"
```

## Related

- `Docs/App/Bootstrap.md`
- `Docs/Analyzers/Analyzers.md`
- `Architecture.md`

## Changelog

- 2026-03-17: Consolidated `Madbox.Scope.Contracts` + `Madbox.Scope.Runtime` into `Madbox.Scope` and moved boundary types to `Runtime/Contracts/`.
- 2026-03-16: Renamed module to Scope and moved shared `LayeredScope`/`ScopeInitializer` orchestration from App Bootstrap to Infra Scope runtime.
