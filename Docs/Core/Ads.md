# Core Ads

## TL;DR

- Purpose: sample **client** module for `AdData` and `WatchAdRequest` / `WatchAdResponse` using LiveOps.
- Location: `Assets/Scripts/Core/Ads/Runtime/` (`Madbox.Ads`), installer `Madbox.Ads.Container`.
- Depends on: `Madbox.LiveOps`, DTO plugin.
- Used by: bootstrap (`AdsInstaller` on Core layer); `WatchAdAsync` takes `ILiveOpsService` as an argument to avoid a DI cycle with `LiveOpsService`.

## Responsibilities

- `AdsClientModule` extends `GameClientModuleBase<AdData>`.
- `WatchAdAsync(ILiveOpsService liveOps, ...)` calls Cloud Code via `CallAsync(new WatchAdRequest())` and assigns returned `AdData` to `data`.
- `IsAdAvailable()` delegates to DTO when `data` is present.

## Notes

- Server `GameData` supplies `AdData` from the Ads Cloud Code module (`AdsService`); persistence and remote config are merged in `Initialize` on the server.

## Registration

`AdsInstaller` registers `AdsClientModule` as `IGameClientModule`, `IAsyncLayerInitializable`, and self. Invoked from `BootstrapCoreInstaller` **after** `LiveOpsInstaller`.

## Tests

EditMode: `Assets/Scripts/Core/Ads/Tests` (`AdsClientModuleTests`).
