# Scaffold Infra Addressables

## TL;DR

- Purpose: keep Addressables runtime small and focused on loading APIs.
- Location: `Assets/Scripts/Infra/Addressables/Runtime/`.
- Depends on: `Madbox.Scope`, `Unity.Addressables`, `VContainer`, `Scaffold.Maps`.
- Used by: bootstrap startup and runtime services that load assets.

## Responsibilities

- Exposes `IAddressablesGateway` for loading by reference/label.
- Performs best-effort catalog/content sync at startup (`InitializeAsync`).
- Owns reference tracking and release policy in `AddressablesAssetReferenceHandler`.
- Provides provider contracts for bootstrap preload and child registration:
  - `IAssetProvider` / `IAssetProvider<TAsset>`
  - `IAssetRegistrar`
  - `AssetProvider<TAsset>` base class

This module no longer owns preload config parsing/build/apply inside the gateway.

## Public API

| Symbol | Purpose |
|---|---|
| `IAddressablesGateway.InitializeAsync` | Best-effort startup sync (no preload pipeline). |
| `IAddressablesGateway.LoadAsync<T>(AssetReference)` | Load one typed asset by reference. |
| `IAddressablesGateway.LoadAsync<T>(AssetReferenceT<T>)` | Typed convenience overload. |
| `IAddressablesGateway.LoadAsync<T>(AssetLabelReference)` | Load typed assets by label. |
| `IAddressablesGateway.Load<T>(...)` | Deferred load variants returning handle/group handle. |
| `IAssetProvider.PreloadAsync` | Provider-local preload entry point. |
| `IAssetRegistrar.Register` | Provider-local typed child registration entry point. |

## Runtime Flow

1. `AddressablesInstaller` registers one scoped `AddressablesGateway` plus required client/handler.
2. Scope startup executes `IAsyncLayerInitializable` on the gateway.
3. Gateway runs best-effort `SyncCatalogAndContentAsync`.
4. Runtime loading uses `Load/LoadAsync` APIs.
5. Bootstrap-level preload/registration is handled outside the gateway by provider/registrar flow.

## Provider and Registrar Flow

- `IAssetProvider` is responsible only for obtaining/storing assets.
- `IAssetRegistrar` is responsible only for writing typed registrations to child builders.
- Concrete classes can implement both interfaces.
- Concrete providers may inject `IAddressablesGateway` in constructor; the interface itself does not require it.

## Best Practices

- Keep all Addressables loads through `IAddressablesGateway`.
- Keep provider preload logic module-local and typed.
- Keep registrar logic minimal: only register assets already preloaded.
- Release handles/groups exactly once.

## Testing

- EditMode: `Madbox.Addressables.Tests`
- PlayMode: `Madbox.Addressables.PlayModeTests`

Run from repository root:

```powershell
& ".\.agents\scripts\run-editmode-tests.ps1" -AssemblyNames "Madbox.Addressables.Tests"
& ".\.agents\scripts\run-playmode-tests.ps1" -AssemblyNames "Madbox.Addressables.PlayModeTests"
```

## Related

- `Architecture.md`
- `Docs/Infra/Scope.md`
- `Assets/Scripts/Infra/Addressables/Runtime/Contracts/IAddressablesGateway.cs`
- `Assets/Scripts/Infra/Addressables/Runtime/Contracts/IAssetProvider.cs`
- `Assets/Scripts/Infra/Addressables/Runtime/Implementation/AddressablesGateway.cs`
- `Assets/Scripts/Infra/Addressables/Runtime/Implementation/AddressablesAssetReferenceHandler.cs`

## Changelog

- 2026-03-21: Moved preload ownership out of `AddressablesGateway` to provider/registrar bootstrap flow; removed preload config pipeline files and contracts.
- 2026-03-18: Updated for gateway-centered simplification and reference-first loading API.
