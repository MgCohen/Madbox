# Scaffold Infra Addressables

## TL;DR

- Purpose: central gateway for loading Addressables with strict handle ownership and internal reference counting.
- Location: `Assets/Scripts/Infra/Addressables/Runtime/` (boundary types under `Runtime/Contracts/`).
- Depends on: `Madbox.Scope`, `Unity.Addressables`, `Unity.ResourceManager`, `VContainer` (container module).
- Used by: bootstrap startup (`IAsyncLayerInitializable`) and any runtime consumer service that needs assets (for example `EnemiesService`).
- Runtime/Editor: runtime + container integration + EditMode tests.
- Keywords: addressables, preload, handle ownership, release, catalog, initialization.

## Responsibilities

- Owns `IAddressablesGateway` and typed `IAssetHandle<T>` contracts.
- Uses `AddressablesGateway` as an orchestrator and delegates internals to focused runtime services:
  - `AddressablesStartupCoordinator` for startup sync + preload request application.
  - `AddressablesLeaseStore` for loaded-entry reference counting, preload handoff ownership, and final release transitions.
- Owns preload registration (`IAddressablesPreloadRegistry`) with `Normal` and `NeverDie` policies.
- Owns Scope startup integration via `AddressablesLayerInitializer : IAsyncLayerInitializable`.
- Does not own feature-level cache policy (for example enemy-specific preload caches).
- Does not expose Unity Addressables lifecycle APIs directly to feature modules.

## Public API

| Symbol | Purpose | Inputs | Outputs | Failure behavior |
|---|---|---|---|---|
| `IAddressablesGateway.InitializeAsync` | Initialize gateway and run preloads | `CancellationToken` | `Task` | throws on preload/load failures |
| `IAddressablesGateway.LoadAsync<T>(AssetKey)` | Load one typed asset by key | key + token | `IAssetHandle<T>` | throws on missing/invalid key |
| `IAddressablesGateway.LoadAsync<T>(AssetLabelReference)` | Load all typed assets for label | label + token | list of `IAssetHandle<T>` | returns empty list when no locations |
| `IAddressablesGateway.LoadAsync<T>(AssetReference)` | Load one typed asset by Unity reference | reference + token | `IAssetHandle<T>` | throws on invalid reference |
| `IAddressablesGateway.LoadAsync<T>(AssetReferenceT<T>)` | Load one typed asset by strongly typed Unity reference | typed reference + token | `IAssetHandle<T>` | throws on invalid reference |
| `IAssetHandle.Release()` | Release exactly one owner reference | none | void | second call is safe no-op |
| `IAddressablesPreloadRegistry` | Register preload requests | key/reference/label + mode (+ optional generic type) | registration state | duplicates are tolerated |

## Setup / Integration

1. Add references to `Madbox.Addressables` (runtime consumers) or `Madbox.Addressables.Container` (composition roots).
2. Register `AddressablesInstaller` in Infra/bootstrap installer flow.
3. Ensure `ScopeInitializer` executes `IAsyncLayerInitializable` implementations so `AddressablesLayerInitializer` runs.
4. Register preload entries through `IAddressablesPreloadRegistry` in a container build callback or dedicated installer.

## How to Use

1. Let bootstrap initialize the gateway through Scope startup (`IAsyncLayerInitializable`).
2. Request assets through `IAddressablesGateway.LoadAsync<T>(...)`.
3. Hold the returned `IAssetHandle<T>` while the asset is needed.
4. Call `Release()` once when ownership ends.
5. Use preload registry for startup warmup; do not implement feature-local preload caches.

## Examples

### Minimal

```csharp
IAssetHandle<Enemy> enemyHandle = await gateway.LoadAsync<Enemy>(new AssetKey("enemy/bee"), ct);
Enemy enemy = enemyHandle.Asset;
enemyHandle.Release();
```

### Realistic

```csharp
public sealed class EnemiesService
{
    private readonly IAddressablesGateway gateway;

    public EnemiesService(IAddressablesGateway gateway)
    {
        this.gateway = gateway;
    }

    public Task<IAssetHandle<Enemy>> LoadEnemyAsync(string enemyId, CancellationToken ct)
    {
        return gateway.LoadAsync<Enemy>(new AssetKey($"enemy/{enemyId}"), ct);
    }

    public Task<IReadOnlyList<IAssetHandle<Enemy>>> LoadAllEnemiesAsync(CancellationToken ct)
    {
        AssetLabelReference label = new AssetLabelReference();
        label.labelString = "enemy";
        return gateway.LoadAsync<Enemy>(label, ct);
    }
}
```

### Guard / Error path

```csharp
IAssetHandle<Enemy> handle = await gateway.LoadAsync<Enemy>(new AssetKey("enemy/bee"), ct);
handle.Release();
handle.Release(); // safe no-op
```

### Preload Configuration

```csharp
public sealed class AddressablesPreloadInstaller : ILayerInstaller
{
    public void Install(IContainerBuilder builder)
    {
        builder.RegisterBuildCallback(container =>
        {
            IAddressablesPreloadRegistry preloads = container.Resolve<IAddressablesPreloadRegistry>();
            preloads.Register<Enemy>(new AssetKey("enemy/bee"), PreloadMode.Normal);
            AssetLabelReference label = new AssetLabelReference();
            label.labelString = "enemy";
            preloads.Register<Enemy>(label, PreloadMode.NeverDie);
        });
    }
}
```

## Best Practices

- Treat each returned handle as one owner; release exactly once.
- Keep feature services stateless regarding preload ownership.
- Prefer `PreloadMode.Normal` for fast-first-use content.
- Use `PreloadMode.NeverDie` only for always-resident startup-critical content.
- Register all preloads centrally in startup composition.
- Use catalog loads for grouped feature warmups; use key loads for direct requests.

## Anti-Patterns

- Calling Unity Addressables static APIs directly from feature modules.
- Keeping custom per-feature preload dictionaries.
- Releasing through a service-locator API instead of handle ownership.
- Releasing multiple times and expecting additional decrements.
- Mixing unrelated high-churn and low-churn content into one preload group without policy.

## Testing

- Test assembly: `Madbox.Addressables.Tests`.
- Run from repo root:

```powershell
& ".\.agents\scripts\run-editmode-tests.ps1" -AssemblyNames "Madbox.Addressables.Tests"
```

- Expected: all tests pass with zero failures.
- Bugfix rule: add/update regression test first, verify fail-before/fix/pass-after.

## AI Agent Context

- Invariants:
  - `Release()` is idempotent and decrements central reference counter at most once per handle.
  - underlying asset release occurs only when final owner reference is released.
  - `PreloadMode.Normal` handoff transfers owner handle to first consumer.
  - `PreloadMode.NeverDie` keeps gateway-owned resident reference.
  - `AddressablesGateway` should stay thin and orchestrate small internal services instead of accumulating all startup/load logic.
- Allowed Dependencies:
  - `Madbox.Scope`, Unity Addressables packages, VContainer container module.
- Forbidden Dependencies:
  - feature-specific gameplay modules.
  - direct UI/presentation coupling in runtime contracts.
- Change Checklist:
  - verify load/release tests for same key.
  - verify preload normal and never-die policy behavior.
  - verify scope initializer integration is still active.
  - verify no direct feature module Addressables calls were introduced.
- Known Tricky Areas:
  - preload handoff semantics for first consumer in `Normal` mode.
  - release idempotence under repeated calls.

## Related

- `Architecture.md`
- `Docs/Infra/Scope.md`
- `Docs/Testing.md`
- `Assets/Scripts/Infra/Scope/Runtime/Contracts/IAsyncLayerInitializable.cs`

## Changelog

- 2026-03-17: Initial module documentation for generic Addressables gateway, preload modes, strict handle ownership, and scope startup integration.
- 2026-03-18: Documented gateway-thinning architecture (`AddressablesStartupCoordinator`, `AddressablesLeaseStore`, `AddressablesPreloadBuffer`) while keeping the same public contracts and startup adapter seam.
- 2026-03-18: Migrated primary reference APIs to Unity-native types (`AssetReference`, `AssetReferenceT<T>`, `AssetLabelReference`) and marked legacy wrappers as compatibility-only.
