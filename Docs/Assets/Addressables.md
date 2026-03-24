# Addressables (Assets)

## TL;DR

- Purpose: Small Addressables runtime focused on loading APIs, gateway initialization, and provider/registrar contracts for bootstrap preload.
- Location: `Assets/Scripts/Assets/Addressables/Runtime/` (`Madbox.Addressables`), tests `Madbox.Addressables.Tests` / PlayMode.
- Depends on: `Madbox.Scope`, `Unity.Addressables`, `VContainer`, `Scaffold.Maps`; Editor may use CCD Management for Build & Release.
- Used by: Bootstrap, any service loading assets by reference or label.
- Runtime/Editor: runtime gateway; Editor CCD workflow documented below.

Keywords: Addressables, gateway, preload, CCD, IAssetProvider

## Responsibilities

- Owns: `IAddressablesGateway` (init + load APIs), reference tracking/release policy, `IAssetProvider` / `IAssetRegistrar` contracts, `AssetProvider<TAsset>` base.
- Does not own: Feature-specific preload lists (owned by providers in bootstrap), or Cloud Code.
- Boundaries: Gateway does not embed preload config parsing — preload happens via provider/registrar flow outside the gateway.

## Public API

| Symbol | Purpose | Inputs | Outputs | Failure behavior |
|---|---|---|---|---|
| `IAddressablesGateway.InitializeAsync` | Best-effort catalog/content sync at startup. | CT | Ready gateway | Logs/throws per Addressables outcome. |
| `IAddressablesGateway.LoadAsync<T>(...)` | Load by `AssetReference`, `AssetReferenceT<T>`, or label. | Ref/label | Loaded asset/handle | Addressables load failures async. |
| `IAssetProvider.PreloadAsync` | Provider-local preload. | CT | Preloaded assets | Provider-specific errors. |
| `IAssetRegistrar.Register` | Typed child registration after preload. | Builder | DI registrations | Throws if assets missing. |

## Setup / Integration

1. `AddressablesInstaller` registers scoped `AddressablesGateway` + handler; startup runs `IAsyncLayerInitializable` on gateway.
2. Bootstrap installers register concrete `IAssetProvider` / `IAssetRegistrar` pairs (for example level definitions, player loadout).
3. For CCD: link Unity project to Unity Cloud, configure Addressables remote profile and **Build to CCD** (details below and in `Docs/Guides/Upload-Addressables-CCD.md`).

### Remote catalog and Cloud Content Delivery (CCD)

Addressable Asset Settings enable **Build Remote Catalog** and **CCD** (`Assets/AddressableAssetsData/AddressableAssetSettings.asset`). **Remote Catalog Build Path** and **Remote Catalog Load Path** follow profile variables (**RemoteBuildPath** / **RemoteLoadPath**).

Sample remote group: **Remote Weapons (Sample)** — prefabs under `Assets/Prefabs/Weapons/` with remote paths.

**Editor workflow (illustrative):** Link project to Unity Cloud (**Edit > Project Settings > Services**), configure CCD bucket, set Addressables profile **Remote** paths to CCD, then **Build to CCD** or follow `Docs/Guides/Upload-Addressables-CCD.md`. Runtime still runs `SyncCatalogAndContentAsync` on gateway init; CCD changes where URLs resolve.

## How to Use

1. Inject `IAddressablesGateway` for runtime loads; await `InitializeAsync` during scope startup.
2. Implement `IAssetProvider` for typed preload groups; implement `IAssetRegistrar` to expose preloaded assets to child scopes.
3. Release handles exactly once via gateway/handler policy.

## Examples

### Minimal

```csharp
var asset = await gateway.LoadAsync<GameObject>(reference, cancellationToken);
```

### Realistic

```csharp
// Bootstrap: provider preloads label MadboxLevels, registrar exposes IAssetGroupProvider<LevelDefinition>
await provider.PreloadAsync(ct);
registrar.Register(childBuilder);
```

### Guard / Error path

```csharp
// Double-release of same handle — follow handler policy; use using/Dispose patterns where applicable
```

## Best Practices

- Route all loads through `IAddressablesGateway` for consistent tracking.
- Keep provider preload typed and module-local.
- Keep registrar minimal — only register assets already preloaded.

## Anti-Patterns

- Loading Addressables directly with static `Addressables.LoadAssetAsync` in feature code — bypasses gateway policy.
- Putting preload parsing inside `AddressablesGateway` — moved to providers by design.

## Testing

- EditMode: `Madbox.Addressables.Tests`
- PlayMode: `Madbox.Addressables.PlayModeTests`
- Run:

```powershell
& ".\.agents\scripts\run-editmode-tests.ps1" -AssemblyNames "Madbox.Addressables.Tests"
& ".\.agents\scripts\run-playmode-tests.ps1" -AssemblyNames "Madbox.Addressables.PlayModeTests"
```

- Expected: all tests pass, zero failures.
- Bugfix rule: add/update regression test first.

## AI Agent Context

- Invariants: gateway initializes before consumer loads in layered startup; providers own preload ownership.
- Allowed Dependencies: Scope, Addressables, Maps.
- Forbidden Dependencies: Feature UI, battle logic.
- Change Checklist: run Addressables test assemblies; update guides if CCD paths change.
- Known Tricky Areas: remote catalog URLs vs local; CCD auth for private buckets.

## Related

- `Architecture.md`
- `Docs/Infra/Scope.md`
- `Docs/Guides/Upload-Addressables-CCD.md`
- `Assets/Scripts/Assets/Addressables/Runtime/Contracts/IAddressablesGateway.cs`
- `Assets/Scripts/Assets/Addressables/Runtime/Implementation/AddressablesGateway.cs`

## Changelog

- 2025-03-23: Module doc path aligned with code under `Assets/Scripts/Assets/Addressables/` (`Docs/Assets/Addressables.md`).
- 2025-03-23: Restructured to module documentation standard; moved CCD notes under Setup / Integration; gateway-centered loading API documented.
- Prior: Documented CCD remote catalog setup and **Remote Weapons (Sample)** group.
