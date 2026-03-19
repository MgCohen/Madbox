# Scaffold Infra Addressables

## TL;DR
- Purpose: one gateway-centric Addressables runtime with explicit handle ownership and simple preload policy.
- Location: `Assets/Scripts/Infra/Addressables/Runtime/` (contracts in `Runtime/Contracts/`).
- Depends on: `Madbox.Scope`, `Unity.Addressables`, `VContainer`, `Scaffold.Maps`.
- Used by: bootstrap/scope startup (`IAsyncLayerInitializable`) and runtime services that load assets.
- Runtime/Editor: runtime module with EditMode + PlayMode integration tests.

## Responsibilities
- Exposes and owns the public loading contract: `IAddressablesGateway`.
- Owns startup flow inside `AddressablesGateway` (catalog sync + preload config load + preload apply).
- Owns reference tracking and release policy in `IAssetReferenceHandler` (`AddressablesAssetReferenceHandler`).
- Owns preload registration building in `IAssetPreloadHandler` (`AddressablesAssetPreloadHandler`).
- Owns preload config schema (`AddressablesPreloadConfig`, `AddressablesPreloadConfigEntry`) and `PreloadMode` semantics.
- Prevents feature modules from calling static Unity Addressables APIs directly.

This module no longer uses:
- `AssetKey`
- `AddressablesStartupCoordinator`
- `AddressablesPreloadRequestProvider`
- `AddressablesPreloadConfigRequestBuilder`
- `AddressablesLayerInitializer`
- `AddressablesLoadToken`
- `AddressablesLeaseStore` (replaced by handler)

## Public API
| Symbol | Purpose | Inputs | Outputs | Failure behavior |
|---|---|---|---|---|
| `IAddressablesGateway.InitializeAsync` | Startup sync + preload execution | `CancellationToken` | `Task` | cancellation throws; catalog/preload load failures are logged and startup continues without preload |
| `IAddressablesGateway.LoadAsync<T>(AssetReference)` | Load one typed asset by reference | reference + token | `IAssetHandle<T>` | throws for invalid reference/load failure |
| `IAddressablesGateway.LoadAsync<T>(AssetReferenceT<T>)` | Typed convenience overload | typed reference + token | `IAssetHandle<T>` | throws for invalid reference/load failure |
| `IAddressablesGateway.LoadAsync<T>(AssetLabelReference)` | Load typed assets by label | label + token | `IAssetGroupHandle<T>` | returns empty group when no typed keys resolve |
| `IAddressablesGateway.Load<T>(AssetReference/AssetReferenceT<T>)` | Deferred single-asset load | reference + token | `IAssetHandle<T>` | `WhenReady` faults on load failure |
| `IAddressablesGateway.Load<T>(AssetLabelReference)` | Deferred label group load | label + token | `IAssetGroupHandle<T>` | child handles complete/fault individually |
| `IAssetHandle.Release()` | Release single owner | none | void | idempotent |
| `IAssetGroupHandle.Release()` | Release all owned child handles | none | void | idempotent |

## Runtime Flow
1. `AddressablesInstaller` registers `AddressablesGateway` once as implemented interfaces.
2. Scope startup executes `IAsyncLayerInitializable` on the same gateway instance.
3. Gateway startup sequence:
   - best-effort `SyncCatalogAndContentAsync`
   - load preload config from key `addressables/preload/config`
   - build registrations from config entries through `IAssetPreloadHandler`
   - apply preloads through `IAssetReferenceHandler` with policy (`Normal`/`NeverDie`)
4. Runtime asset loads use gateway `LoadAsync`/`Load` APIs only.

Notes:
- Missing/invalid preload config logs a warning and startup continues without preload.
- Child-scope registration path enforces one preload registration per service type.

## Lease / Ownership Model
- Internal store key: `(Type assetType, string key)` backed by `Map<Type, string, AddressablesLoadedEntry>`.
- `AddressablesLoadedEntry` tracks:
  - loaded asset
  - `RefCount`
  - `Policy` (`PreloadMode.Normal` or `PreloadMode.NeverDie`)
- Acquire behavior:
1. lookup existing entry
2. if found, update ref/policy and return handle
3. if missing, load from client, create entry, return handle
- Release behavior:
1. decrement `RefCount` (if > 0)
2. if `RefCount` is 0 and policy is not `NeverDie`, remove entry and release underlying asset

## Setup / Integration
1. Add runtime dependency on `Madbox.Addressables`.
2. Register `Assets/Scripts/Infra/Addressables/Container/AddressablesInstaller.cs` in composition root.
3. Ensure scope startup executes `IAsyncLayerInitializable`.
4. Create one addressable preload config asset at key `addressables/preload/config`.

Quick checks:
1. Bootstrap reaches completion.
2. `IAddressablesGateway` resolves from DI scope.
3. `LoadAsync<T>(AssetReference)` and `LoadAsync<T>(AssetLabelReference)` return releasable handles.

## How to Use
### Single asset (async)
```csharp
AssetReference beeReference = new AssetReference("bee");
IAssetHandle<EnemyDefinitionSO> enemy = await gateway.LoadAsync<EnemyDefinitionSO>(beeReference, ct);
EnemyDefinitionSO value = enemy.Asset;
enemy.Release();
```

### Single asset (deferred)
```csharp
AssetReference beeReference = new AssetReference("bee");
IAssetHandle<EnemyDefinitionSO> handle = gateway.Load<EnemyDefinitionSO>(beeReference, ct);
await handle.WhenReady;
EnemyDefinitionSO value = handle.Asset;
handle.Release();
```

### Label group
```csharp
AssetLabelReference enemyLabel = new AssetLabelReference { labelString = "enemy" };
IAssetGroupHandle<EnemyDefinitionSO> enemies = await gateway.LoadAsync<EnemyDefinitionSO>(enemyLabel, ct);

foreach (IAssetHandle<EnemyDefinitionSO> item in enemies.TypedHandles)
{
    EnemyDefinitionSO so = item.Asset;
}

enemies.Release();
```

## Preload Configuration
```csharp
[CreateAssetMenu(menuName = "Madbox/Addressables/Preload Config", fileName = "AddressablesPreloadConfig")]
public class AddressablesPreloadConfig : ScriptableObject
{
    [SerializeField] private List<AddressablesPreloadConfigEntry> entries = new List<AddressablesPreloadConfigEntry>();
    public IReadOnlyList<AddressablesPreloadConfigEntry> Entries => entries;
}
```

```csharp
[Serializable]
public class AddressablesPreloadConfigEntry
{
    [SerializeField] private TypeReference assetType;
    [SerializeField] private PreloadReferenceType referenceType;
    [SerializeField] private AssetReference assetReference;
    [SerializeField] private AssetLabelReference labelReference;
    [SerializeField] private PreloadMode mode;
}
```

Preload authoring:
1. Create one preload config asset.
2. Mark it addressable at `addressables/preload/config`.
3. Add entries with correct `assetType`, reference kind, and `mode`.
4. Use `Normal` for handoff assets and `NeverDie` for resident startup assets.

## Best Practices
- Keep all loading through `IAddressablesGateway`.
- Release each handle/group exactly once.
- Use label loads when lifecycle is naturally batched.
- Keep preload definitions centralized in one config asset.
- Keep `NeverDie` usage minimal and explicit.

## Anti-Patterns
- Direct static `Addressables` calls in feature modules.
- Re-introducing custom key wrappers in public API.
- Splitting startup preload parsing/coordination into extra service layers.
- Ignoring handle/group release responsibility.

## Testing
- EditMode: `Madbox.Addressables.Tests`
- PlayMode: `Madbox.Addressables.PlayModeTests`

Run from repository root:
```powershell
& ".\.agents\scripts\run-editmode-tests.ps1" -AssemblyNames "Madbox.Addressables.Tests"
& ".\.agents\scripts\run-playmode-tests.ps1" -AssemblyNames "Madbox.Addressables.PlayModeTests"
```

Milestone quality gate:
```powershell
.\.agents\scripts\validate-changes.cmd
```

## Related
- `Architecture.md`
- `Docs/Testing.md`
- `Docs/AutomatedTesting.md`
- `Docs/Infra/Scope.md`
- `Assets/Scripts/Infra/Addressables/Runtime/Contracts/IAddressablesGateway.cs`
- `Assets/Scripts/Infra/Addressables/Runtime/Implementation/AddressablesGateway.cs`
- `Assets/Scripts/Infra/Addressables/Runtime/Implementation/AddressablesAssetReferenceHandler.cs`
- `Assets/Scripts/Infra/Addressables/Runtime/Implementation/AddressablesAssetPreloadHandler.cs`
- `Assets/Scripts/Infra/Addressables/Runtime/Implementation/AddressablesLoadedEntry.cs`

## Changelog
- 2026-03-18: Updated for gateway-centered simplification. Removed obsolete layers (`AssetKey`, startup coordinator/provider/builder/layer initializer/token), documented map-backed lease flow, and refreshed API/setup/examples to Unity reference-first usage.
