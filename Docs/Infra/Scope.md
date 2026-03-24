# Scaffold Infra Scope

## TL;DR

- Purpose: Layered startup orchestration for application bootstrap (`LayeredScope`, `LayerInstallerBase`, cross-layer resolution).
- Location: `Assets/Scripts/Infra/Scope/Runtime/` and `Assets/Scripts/Infra/Scope/Runtime/Contracts/`.
- Depends on: BCL, `VContainer`, `VContainer.Unity`.
- Used by: `Madbox.Bootstrap.Runtime` and any `IAsyncLayerInitializable` services.
- Runtime/Editor: runtime infrastructure.

Keywords: layered scope, bootstrap, IAsyncLayerInitializable, VContainer

## Responsibilities

- Owns: `LayeredScope` startup lifecycle, `LayerInstallerBase` recursive composition, `ICrossLayerObjectResolver`, initialization contracts and analyzer exception attributes.
- Does not own: Feature business rules, UI, or scene content.
- Boundaries: Infra-only; deterministic pipeline order below.

**Pipeline order** (`LayerInstallerBase`):

1. `InitializeAsync(...)`
2. `OnCompletedAsync(...)`
3. Optional `ILayeredScopeProgress.OnLayerPipelineStep(...)` (once per installer node, depth-first pre-order; `completedLayerIndex` is 1-based through `totalLayers`)
4. `BuildChildrenAsync(...)`

Parent installers can prepare data in `OnCompletedAsync` before child scopes are created.

**Layer progress:** `LayeredScope` accepts optional `ILayeredScopeProgress` (often a `MonoBehaviour` in the Inspector). The scope counts `LayerInstallerBase` nodes before `BuildAsRootAsync` and reports one step per node after that node’s `OnCompletedAsync` completes. If the listener is null, no callbacks fire.

## Public API

| Symbol | Purpose | Inputs | Outputs | Failure behavior |
|---|---|---|---|---|
| `LayeredScope` | Coordinates startup with one root layer tree. | Root installer tree + cancellation token | Initialized final scope | Throws on null tree root or startup failures. |
| `LayerInstallerBase` | Recursive installer with deterministic pipeline. | Parent scope and cancellation token | Built child scope subtree | Throws on invalid topology or initializer failures. |
| `ICrossLayerObjectResolver` | Resolves from deepest matching layer scope. | Type or inject target | Resolved instance | Throws if type missing in all layers. |
| `IAsyncLayerInitializable.InitializeAsync` | Async startup contract for layer services. | Resolver + CT | Completion | Cancellation propagates. |
| `ILayeredScopeProgress.OnLayerPipelineStep` | Optional UI/telemetry hook. | 1-based step, total | None | Keep cheap; marshal UI to main thread. |

## Setup / Integration

1. Subclass `LayerInstallerBase` for each layer; build a tree from the project bootstrap.
2. Assign optional `ILayeredScopeProgress` on `LayeredScope` for editor-driven progress UI.
3. Register `IAsyncLayerInitializable` implementations on the correct child scope so ordering matches dependencies (for example Addressables gateway before consumers).

## How to Use

1. Instantiate `LayeredScope` with the root installer configured in the inspector or code.
2. Await `BuildAsRootAsync` / project-specific entrypoints that wrap it.
3. Use `ICrossLayerObjectResolver` when a service must resolve from the deepest scope that registered a type.
4. Implement `IAsyncLayerInitializable` for async startup work that must complete before children build when using parent data in `OnCompletedAsync`.

## Examples

### Minimal

```csharp
await layeredScope.BuildAsRootAsync(cancellationToken);
```

### Realistic

```csharp
// Parent installer: load config in OnCompletedAsync, then children register consumers
public override async UniTask OnCompletedAsync(IObjectResolver resolver, CancellationToken ct)
{
    await LoadSharedConfigAsync(ct);
    await base.OnCompletedAsync(resolver, ct);
}
```

### Guard / Error path

```csharp
// Resolve before layer registered: ICrossLayerObjectResolver throws — register types on the expected layer
```

## Best Practices

- Keep startup layers explicit and deterministic.
- Treat `IAsyncLayerInitializable` as startup-only.
- Keep initializers side-effect bounded and idempotent.
- Use `OnCompletedAsync` for parent-owned data needed by child registration.

## Anti-Patterns

- Hidden static singletons for “late” services that bypass layer registration.
- Long-running work in `OnLayerPipelineStep` callbacks — keep progress hooks lightweight.

## Testing

- Test assemblies: `Madbox.Scope.Tests`, `Madbox.Bootstrap.Tests`.
- Run:

```powershell
& ".\.agents\scripts\run-editmode-tests.ps1" -AssemblyNames "Madbox.Bootstrap.Tests"
```

- Expected: all tests pass, zero failures.
- Bugfix rule: add/update regression test first.

## AI Agent Context

- Invariants: pipeline order `InitializeAsync` → `OnCompletedAsync` → optional progress → `BuildChildrenAsync` (per node); deepest-first resolution on `ICrossLayerObjectResolver`.
- Allowed Dependencies: VContainer, Unity (for UnityLifetimeScope integrations in consuming projects).
- Forbidden Dependencies: Feature modules from Scope assembly.
- Change Checklist: run Scope/Bootstrap tests when changing pipeline; update `Docs/App/Bootstrap.md`.
- Known Tricky Areas: cancellation during layered init; progress callback threading for UI.

## Related

- `Docs/App/Bootstrap.md`
- `Architecture.md`
- `Docs/Testing.md`

## Changelog

- 2025-03-23: Restructured to module documentation standard (lifecycle folded into Responsibilities; added Examples, Anti-Patterns, AI Agent Context).
- Prior: Documented `ICrossLayerObjectResolver`, `ILayeredScopeProgress`, pipeline order.
