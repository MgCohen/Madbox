# Ads (Meta)

## TL;DR

- Purpose: Sample client module for `AdData` and `WatchAdRequest` / `WatchAdResponse` via LiveOps Cloud Code.
- Location: `Assets/Scripts/Meta/Ads/Runtime/` (`Madbox.Ads`), installer `Madbox.Ads.Container`.
- Depends on: `Madbox.LiveOps`, DTO plugin `Madbox.LiveOps.DTO.dll`.
- Used by: Bootstrap `AdsInstaller` after LiveOps registration; `WatchAdAsync` takes `ILiveOpsService` to avoid DI cycles with `LiveOpsService`.
- Runtime/Editor: runtime client module.

Keywords: ads, LiveOps, Cloud Code, IGameClientModule

## Responsibilities

- Owns: `AdsClientModule` extending `GameClientModuleBase<AdData>`, `WatchAdAsync`, `IsAdAvailable`.
- Does not own: server Ads module implementation, UGS dashboard configuration, or UI.
- Boundaries: Client calls Cloud Code through `ILiveOpsService`; server merges `GameData` and remote config during `Initialize`.

## Public API

| Symbol | Purpose | Inputs | Outputs | Failure behavior |
|---|---|---|---|---|
| `AdsClientModule` | `IGameClientModule` + `IAsyncLayerInitializable` for `AdData`. | Bootstrap layer order after LiveOps | Hydrated `data` from `GameData` | Depends on successful initial `GameDataRequest`. |
| `WatchAdAsync(ILiveOpsService, ...)` | Calls Cloud Code `WatchAdRequest`. | LiveOps service | `WatchAdResponse` / updated `AdData` | Surfaced as async failure from Cloud Code. |
| `IsAdAvailable()` | Delegates to DTO when `data` present. | None | bool | False when data missing. |

## Setup / Integration

1. Ensure `LiveOpsInstaller` runs and completes initial `GameDataRequest` before ads module init when `AdData` is required.
2. Register `AdsInstaller` from `BootstrapCoreInstaller` **after** `LiveOpsInstaller`.
3. `AdsInstaller` registers `AdsClientModule` as `IGameClientModule`, `IAsyncLayerInitializable`, and self.

## How to Use

1. Resolve `AdsClientModule` or `IGameClientModule` after bootstrap completes.
2. Call `WatchAdAsync` with an injected `ILiveOpsService` when simulating a rewarded ad flow.
3. Read `IsAdAvailable()` before showing ad UI affordances.

## Examples

### Minimal

```csharp
await adsClient.WatchAdAsync(liveOps, cancellationToken);
```

### Realistic

```csharp
if (adsClient.IsAdAvailable())
{
    await adsClient.WatchAdAsync(liveOpsService, ct);
}
```

### Guard / Error path

```csharp
// LiveOps not initialized: ensure IAsyncLayerInitializable order — LiveOpsService before AdsClientModule
```

## Best Practices

- Pass `ILiveOpsService` into `WatchAdAsync` to avoid circular registration with `LiveOpsService`.
- Keep ad eligibility rules server-driven via `AdData` when possible.

## Anti-Patterns

- Calling Cloud Code without going through `ILiveOpsService` — breaks module dispatch and auth assumptions.
- Registering `AdsInstaller` before `LiveOpsInstaller` — `GameData` may be empty.

## Testing

- Test assembly: `Madbox.Ads.Tests` (EditMode), path `Assets/Scripts/Meta/Ads/Tests`.
- Run:

```powershell
& ".\.agents\scripts\run-editmode-tests.ps1" -AssemblyNames "Madbox.Ads.Tests"
```

- Expected: all tests pass, zero failures.
- Bugfix rule: add/update regression test first.

## AI Agent Context

- Invariants: `WatchAdAsync` always receives `ILiveOpsService` from caller; module participates in `IGameClientModule` pipeline.
- Allowed Dependencies: `Madbox.LiveOps`, DTO, VContainer.
- Forbidden Dependencies: UI assemblies, direct HTTP bypass of Cloud Code wrapper.
- Change Checklist: update DTO DLL if contracts change; rerun `Madbox.Ads.Tests`.
- Known Tricky Areas: bootstrap layer ordering vs `GameData` availability.

## Related

- `Docs/Core/LiveOps.md`
- `Architecture.md`

## Changelog

- 2025-03-23: Module doc path aligned with `Assets/Scripts/Meta/Ads/` (`Docs/Meta/Ads.md`); title and code paths updated from the former `Docs/Core/` location.
- 2025-03-23: Restructured to module documentation standard.
