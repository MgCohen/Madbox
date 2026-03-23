# Core LiveOps

## TL;DR

- Purpose: typed client for the deployed Cloud Code **LiveOps** module using shared DTO requests and responses; **bootstrap** runs an initial **`GameDataRequest`** via `IAsyncLayerInitializable` on `LiveOpsService`.
- Location: `Assets/Scripts/Core/LiveOps/Runtime/` (`Madbox.LiveOps`), installer `Madbox.LiveOps.Container`.
- Depends on: `Madbox.CloudCode`, `Madbox.Scope` (for `IAsyncLayerInitializable`), `Madbox.Ugs` (optional `IUgs` gate before Cloud Code), plugin `Madbox.LiveOps.DTO.dll`, `Newtonsoft.Json`, `VContainer`.
- Used by: bootstrap, feature modules (`IGameClientModule` implementations, `GameClientModuleBase<T>`), and any code that calls LiveOps endpoints.

## Responsibilities

- `ILiveOpsService` / `LiveOpsService`: `CallAsync`, `GetModuleData<T>()` (reads from the last successful initial `GameDataRequest` stored on the service).
- After each `CallAsync`, `LiveOpsService` walks `ModuleResponse.Responses` recursively and invokes registered `IResponseHandler<T>` instances for each nested response’s runtime type (see `IResponseHandler<T>`).
- `LiveOpsService` implements `IAsyncLayerInitializable`: performs the initial `GameDataRequest` and stores aggregated `GameData` internally. It does not coordinate other services; callers use `GetModuleData<T>()` when their layer runs after LiveOps has initialized.
- `GameClientModuleBase<T>` implements `IAsyncLayerInitializable`: resolves `ILiveOpsService` and assigns `protected data` from `GetModuleData<T>()`. Bootstrap layer ordering should run `LiveOpsService` before these modules when they need `GameData` populated. `IGameClientModule` exposes `Key` only; typed payload lives on the concrete type as `protected T data`.
- Payload shape `{ "request": <serialized ModuleRequest> }` for Cloud Code bindings.

## Public API

| Symbol | Purpose |
|--------|---------|
| `IGameClientModule` | Client module contract: `Key` only. |
| `GameClientModuleBase<T>` | Base class with `Key` defaulting to `typeof(T).Name`, `InitializeAsync` loading from `ILiveOpsService`. |
| `ILiveOpsService.CallAsync<TResponse>` | Generic module call. |
| `ILiveOpsService.GetModuleData<T>` | Typed slice of aggregated `GameData` after initial fetch. |
| `IResponseHandler<T>` | Optional handler for nested `ModuleResponse` items of type `T` inside `Responses`. |
| `LiveOpsService` | Implements `ILiveOpsService` and `IAsyncLayerInitializable`. |

## Registration

`LiveOpsInstaller` registers `LiveOpsService` as `ILiveOpsService` and `IAsyncLayerInitializable` (scoped). Runs from **`BootstrapCoreInstaller`** alongside other installers (for example `AdsInstaller`). Infra layer registers UGS and Cloud Code on the parent scope.

Register each concrete handler as `IResponseHandler<T>` (for example `builder.Register<MyHandler>(Lifetime.Scoped).As<IResponseHandler<GoldResponse>>()`). VContainer resolves `IEnumerable<IResponseHandler<T>>` when multiple handlers exist for the same nested response type.

Register concrete feature modules as `IGameClientModule` and `IAsyncLayerInitializable` when they should hydrate during bootstrap (see `AdsInstaller`). `LiveOpsService` does not enumerate client modules; it only stores `GameData` from the initial `GameDataRequest`.

## Tests

EditMode: `Assets/Scripts/Core/LiveOps/Tests` (`LiveOpsInitializationTests`, `GameClientModuleBaseTests`).
