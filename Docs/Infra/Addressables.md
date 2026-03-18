# Scaffold Infra Addressables

## TL;DR
- Purpose: central Addressables gateway with strict handle ownership and explicit preload policies.
- Location: `Assets/Scripts/Infra/Addressables/Runtime/` (contracts in `Runtime/Contracts/`).
- Depends on: `Madbox.Scope`, `Unity.Addressables`, `Unity.ResourceManager`, and `VContainer` (container wiring).
- Used by: bootstrap startup (`IAsyncLayerInitializable`) and runtime services that load content.
- Runtime/Editor: runtime module with EditMode + PlayMode integration tests.
- Keywords: addressables, preload, ownership, group handle, label load, release lifecycle.

## Responsibilities
- Owns stable contracts: `IAddressablesGateway`, `IAssetHandle<T>`, `IAssetGroupHandle<T>`, and preload registry contracts.
- Owns startup sync + preload orchestration through `AddressablesStartupCoordinator`.
- Owns reference-counted runtime ownership through `AddressablesLeaseStore`.
- Owns preload registration API (`IAddressablesPreloadRegistry`) and `PreloadMode` semantics.
- Owns bootstrap startup adapter `AddressablesLayerInitializer : IAsyncLayerInitializable`.
- Does not own feature-specific cache policy (for example, custom enemy cache dictionaries).
- Does not expose raw `UnityEngine.AddressableAssets.Addressables` calls to feature modules.
- Boundaries: runtime infrastructure only; no presentation/UI coupling in contracts.

## Public API
| Symbol | Purpose | Inputs | Outputs | Failure behavior |
|---|---|---|---|---|
| `IAddressablesGateway.InitializeAsync` | Startup sync + preload execution | `CancellationToken` | `Task` | throws for cancellation and fatal init/load failures |
| `IAddressablesGateway.LoadAsync<T>(AssetKey)` | Load one typed asset by key | key + token | `IAssetHandle<T>` | throws for invalid key or load failures |
| `IAddressablesGateway.LoadAsync<T>(AssetReference)` | Load one typed asset by Unity reference | reference + token | `IAssetHandle<T>` | throws for invalid reference |
| `IAddressablesGateway.LoadAsync<T>(AssetReferenceT<T>)` | Load one typed asset by strong reference | typed reference + token | `IAssetHandle<T>` | throws for invalid reference |
| `IAddressablesGateway.LoadAsync<T>(AssetLabelReference)` | Load typed assets by label as one release owner | label + token | `IAssetGroupHandle<T>` | returns group with 0 handles when no typed locations |
| `IAssetHandle.Release()` | Release single asset owner | none | void | idempotent; second call no-op |
| `IAssetGroupHandle.Release()` | Release all child handles at once | none | void | idempotent; second call no-op |
| `IAddressablesPreloadRegistry.Register<T>(...)` | Register typed preloads | key/reference/label + mode | registration state | duplicates tolerated; untyped overloads are unsupported |

## Setup / Integration
1. Add module dependency:
   `Madbox.Addressables` for runtime consumers, and `Madbox.Addressables.Container` for composition roots.
2. Register `AddressablesInstaller` inside bootstrap infra install flow.
3. Ensure `ScopeInitializer` executes `IAsyncLayerInitializable` so `AddressablesLayerInitializer` runs.
4. Register typed preloads in installer/build callback via `IAddressablesPreloadRegistry.Register<T>(...)`.

Quick checks:
1. `Bootstrap` scene reaches `IsBootstrapCompleted == true`.
2. `IAddressablesGateway` resolves from runtime DI scope.
3. Label loads return typed group handles and one-call release is available.

Common setup mistakes:
1. Using untyped preload registration overloads; use `Register<T>(...)` only.
2. Forgetting to release handles (or label group handle) after use.
3. Calling Unity `Addressables` APIs directly from feature services.

## How to Use
1. Let bootstrap initialize Addressables via scope startup.
2. For one asset, call `LoadAsync<T>(key/reference)`.
3. For typed label loads, call `LoadAsync<T>(label)` and keep the returned `IAssetGroupHandle<T>`.
4. For grouped lifecycle, release the group once when batch ownership ends.
5. Register preloads centrally with typed calls and correct `PreloadMode`.

## Examples
### Minimal
```csharp
IAssetHandle<EnemyDefinitionSO> enemy = await gateway.LoadAsync<EnemyDefinitionSO>(new AssetKey("enemy/bee"), ct);
EnemyDefinitionSO value = enemy.Asset;
enemy.Release();
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

    public Task<IAssetHandle<EnemyDefinitionSO>> LoadEnemyAsync(string enemyId, CancellationToken ct)
    {
        AssetKey key = new AssetKey($"enemy/{enemyId}");
        return gateway.LoadAsync<EnemyDefinitionSO>(key, ct);
    }

    public Task<IAssetGroupHandle<EnemyDefinitionSO>> LoadAllEnemiesAsync(CancellationToken ct)
    {
        AssetLabelReference label = new AssetLabelReference();
        label.labelString = "enemy";
        return gateway.LoadAsync<EnemyDefinitionSO>(label, ct);
    }
}
```

### Guard / Error path
```csharp
IAssetHandle<EnemyDefinitionSO> handle = await gateway.LoadAsync<EnemyDefinitionSO>(new AssetKey("enemy/bee"), ct);
handle.Release();
handle.Release(); // safe no-op
```

### Real Use Cases
```csharp
// 1) Load all enemy SOs
AssetLabelReference enemyLabel = new AssetLabelReference { labelString = "enemy" };
IAssetGroupHandle<EnemyDefinitionSO> enemies = await gateway.LoadAsync<EnemyDefinitionSO>(enemyLabel, ct);

// iterate typed handles
foreach (IAssetHandle<EnemyDefinitionSO> enemy in enemies.TypedHandles)
{
    EnemyDefinitionSO so = enemy.Asset;
}

// one-shot batch release
enemies.Release();

// 2) Load specific enemy SO
IAssetHandle<EnemyDefinitionSO> bee = await gateway.LoadAsync<EnemyDefinitionSO>(new AssetKey("enemy/bee"), ct);
EnemyDefinitionSO beeSo = bee.Asset;

// 3) Load prefab referenced by enemy SO
IAssetHandle<GameObject> prefab = await gateway.LoadAsync<GameObject>(beeSo.PrefabReference, ct);
GameObject enemyPrefab = prefab.Asset;
prefab.Release();
bee.Release();

// 4) Load navigation multi-type group (same label, separate typed calls)
AssetLabelReference navigationLabel = new AssetLabelReference { labelString = "navigation" };
IAssetGroupHandle<ViewConfigSO> views = await gateway.LoadAsync<ViewConfigSO>(navigationLabel, ct);
IAssetGroupHandle<NavigationConfigSO> globals = await gateway.LoadAsync<NavigationConfigSO>(navigationLabel, ct);
views.Release();
globals.Release();
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
            preloads.Register<EnemyDefinitionSO>(new AssetKey("enemy/bee"), PreloadMode.Normal);
            preloads.Register<EnemyDefinitionSO>(new AssetLabelReference { labelString = "enemy" }, PreloadMode.Normal);
            preloads.Register<ViewConfigSO>(new AssetLabelReference { labelString = "navigation" }, PreloadMode.NeverDie);
            preloads.Register<NavigationConfigSO>(new AssetLabelReference { labelString = "navigation" }, PreloadMode.NeverDie);
        });
    }
}
```

## Best Practices
- Treat each `IAssetHandle<T>` as one owner and release exactly once.
- Prefer label loads through `LoadAsync<T>(label)` when you naturally own a batch lifecycle.
- Use `group.TypedHandles` for typed access to loaded assets.
- Keep preload registration typed (`Register<T>(...)`) and centralized in composition.
- Use `PreloadMode.Normal` for handoff-first-use assets.
- Use `PreloadMode.NeverDie` only for always-resident startup-critical assets.
- Keep feature services free of direct `Addressables` static API usage.
- Keep runtime module boundaries explicit with `.asmdef` references.

## Anti-Patterns
- Calling Unity `Addressables` static methods directly from gameplay/feature modules.
- Holding feature-local preload dictionaries in services (duplicate policy ownership).
- Mixing unrelated asset families under one label and expecting one typed call to return all types.
- Releasing through ad-hoc global utility instead of ownership handles.
- Ignoring handle release (or group release), causing retained references and hidden memory pressure.

Migration guidance:
1. If you currently release each label-loaded item individually, switch to releasing the returned group when batch ownership fits your flow.
2. If you need per-item lifecycle differences, keep references to individual handles from `group.TypedHandles` and release them explicitly.

## Testing
- Test assemblies:
  - `Madbox.Addressables.Tests` (EditMode)
  - `Madbox.Addressables.PlayModeTests` (bootstrap E2E PlayMode)
- Run from repo root:
```powershell
& ".\.agents\scripts\run-editmode-tests.ps1" -AssemblyNames "Madbox.Addressables.Tests"
& ".\.agents\scripts\run-playmode-tests.ps1" -AssemblyNames "Madbox.Addressables.PlayModeTests"
```
- Expected:
  - EditMode + PlayMode tests pass with zero failures.
  - Group-handle tests prove one-call batch release and release idempotence.
- Bugfix rule:
  - Add/update regression test first, verify fail-before, then fix and verify pass-after.

## AI Agent Context
- Invariants:
  - `IAssetHandle.Release()` and `IAssetGroupHandle.Release()` are idempotent.
  - Underlying asset release happens only when final owner reference is released.
  - `PreloadMode.Normal` hands off initial owner to first consumer.
  - `PreloadMode.NeverDie` keeps gateway-owned resident reference.
  - `LoadAsync<T>(label)` must preserve typed filtering and return a group for one typed asset family only.
- Allowed Dependencies:
  - `Madbox.Scope`, Unity Addressables packages, VContainer container module.
- Forbidden Dependencies:
  - Feature-specific gameplay modules.
  - Presentation/UI contracts in Addressables runtime boundaries.
- Change Checklist:
  - Verify same-key shared load and final-owner release.
  - Verify typed preload behavior for `Normal` and `NeverDie`.
  - Verify group release releases all child handles once.
  - Verify bootstrap PlayMode E2E still resolves gateway and loads an addressable.
  - Verify analyzer diagnostics remain clean.
- Known Tricky Areas:
  - Normal preload handoff when label preloads and direct key loads overlap.
  - Typed label filtering vs multi-type label authoring expectations.
  - Preventing duplicate releases when group and child handles are both released.

## Related
- `Architecture.md`
- `Docs/Testing.md`
- `Docs/AutomatedTesting.md`
- `Docs/Infra/Scope.md`
- `Assets/Scripts/Infra/Addressables/Runtime/Contracts/IAddressablesGateway.cs`
- `Assets/Scripts/Infra/Addressables/Runtime/Contracts/IAssetHandle.cs`
- `Assets/Scripts/Infra/Addressables/Runtime/Contracts/IAssetGroupHandle.cs`
- `Assets/Scripts/Infra/Scope/Runtime/Contracts/IAsyncLayerInitializable.cs`

## Changelog
- 2026-03-17: Initial module documentation for generic Addressables gateway, preload modes, ownership semantics, and scope startup integration.
- 2026-03-18: Documented startup/gateway-thinning collaborators and Unity-native reference APIs.
- 2026-03-18: Added typed preload-only guidance and bootstrap-driven PlayMode E2E coverage notes.
- 2026-03-18: Added `IAssetGroupHandle<T>` and unified label-loading usage via `LoadAsync<T>(label)`, plus real use-case snippets for enemy and navigation flows.
