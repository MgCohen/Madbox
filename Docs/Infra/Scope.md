# Scaffold Infra Scope

## TL;DR

- Purpose: reusable layered startup orchestration for application bootstrap.
- Location: `Assets/Scripts/Infra/Scope/Runtime/` and `Assets/Scripts/Infra/Scope/Runtime/Contracts/`.
- Depends on: BCL plus `VContainer` / `VContainer.Unity`.
- Used by: `Madbox.Bootstrap.Runtime` and services that initialize during layered startup.

## Responsibilities

- Owns `LayeredScope` startup lifecycle.
- Owns `LayerInstallerBase` recursive layer composition and build pipeline.
- Defines initialization contracts and analyzer exception attributes.
- Does not own feature-specific business rules.

## Lifecycle Order

`LayerInstallerBase` pipeline order is now:

1. `InitializeAsync(...)`
2. `OnCompletedAsync(...)`
3. `BuildChildrenAsync(...)`

This order allows a parent installer to prepare data in `OnCompletedAsync` before child scopes are created.

## Public API

| Symbol | Purpose | Inputs | Outputs | Failure behavior |
|---|---|---|---|---|
| `LayeredScope` | Coordinates startup with one root layer tree. | Root installer tree + cancellation token. | Initialized final scope and startup completion signal. | Throws on null tree root or startup failures. |
| `LayerInstallerBase` | Recursive installer with deterministic pipeline. | Parent scope and cancellation token. | Built child scope subtree. | Throws on invalid tree topology or initializer failures. |
| `IAsyncLayerInitializable.InitializeAsync(IObjectResolver, CancellationToken)` | Async startup contract for layer services. | Resolver and cancellation token. | Startup completion signal. | Cancellation propagates; non-cancellation failures are wrapped by startup orchestration. |

## Best Practices

- Keep startup layers explicit and deterministic.
- Treat `IAsyncLayerInitializable` as startup-only contract.
- Keep initializers side-effect bounded and idempotent.
- Use `OnCompletedAsync` for parent-owned data needed by child registration.

## Testing

- Test assemblies:
  - `Madbox.Scope.Tests`
  - `Madbox.Bootstrap.Tests`

Run:

```powershell
& ".\.agents\scripts\run-editmode-tests.ps1" -AssemblyNames "Madbox.Bootstrap.Tests"
```

## Related

- `Docs/App/Bootstrap.md`
- `Architecture.md`
- `Docs/Testing.md`

## Changelog

- 2026-03-21: Updated pipeline order to `InitializeAsync -> OnCompletedAsync -> BuildChildrenAsync` to support parent completion data before child creation.
- 2026-03-21: Kept recursive installer model and initialization contracts unchanged.
