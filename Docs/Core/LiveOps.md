# Core LiveOps

## TL;DR

- Purpose: Typed client for the deployed Cloud Code **LiveOps** module; bootstrap runs initial **`GameDataRequest`** via `IAsyncLayerInitializable` on `LiveOpsService`.
- Location: `Assets/Scripts/Core/LiveOps/Runtime/` (`Madbox.LiveOps`), installer `Madbox.LiveOps.Container`.
- Depends on: `Madbox.CloudCode`, `Madbox.Scope`, `Madbox.Ugs` (optional gate), `Madbox.LiveOps.DTO.dll`, Newtonsoft.Json, VContainer.
- Used by: Bootstrap, `IGameClientModule` implementations, any code calling LiveOps endpoints.
- Runtime/Editor: runtime client.

Keywords: LiveOps, Cloud Code, GameData, ModuleRequest, handlers

## Responsibilities

- Owns: `ILiveOpsService` / `LiveOpsService`, `CallAsync`, `GetModuleData<T>()`, `GameClientModuleBase<T>`, response dispatch to `IResponseHandler` implementations.
- Does not own: Cloud Code server projects under `LiveOps/`, UGS dashboard configuration, or UI.
- Boundaries: Client-side; payload shape `{ "request": <serialized ModuleRequest> }` for bindings.

## Public API

| Symbol | Purpose | Inputs | Outputs | Failure behavior |
|---|---|---|---|---|
| `ILiveOpsService.CallAsync<TResponse>` | Generic module call. | Request DTO | Response | Cloud Code errors propagate async. |
| `ILiveOpsService.GetModuleData<T>` | Typed slice after initial `GameDataRequest`. | None | Module data | Empty/default if initial fetch failed or type missing. |
| `GameClientModuleBase<T>` | Base for modules hydrating from `GameData`. | `InitializeAsync` | `protected data` | Skips hydration if LiveOps data absent. |
| `IResponseHandler` / `IResponseHandler<T>` | Handle nested `ModuleResponse` items; `HandledResponseType` selects type. | Dispatch | Handler side effects | Multiple handlers per type all run. |
| `LiveOpsService` | Implements `ILiveOpsService` + `IAsyncLayerInitializable`. | Resolver, CT | Initial GameData | Throws/logs per Cloud Code outcome. |

## Setup / Integration

1. `LiveOpsInstaller` registers `LiveOpsService` as `ILiveOpsService` and `IAsyncLayerInitializable` (scoped); invoked from `BootstrapCoreInstaller` with UGS/Cloud Code infra.
2. Register each response handler with `AsImplementedInterfaces()` so both `IResponseHandler` and `IResponseHandler<T>` resolve.
3. Register feature modules as `IGameClientModule` + `IAsyncLayerInitializable` after `LiveOpsService` when they need `GameData`.
4. Ensure `LiveOpsService` initializes before modules that call `GetModuleData<T>()` in their `InitializeAsync`.

## How to Use

1. Inject `ILiveOpsService` for module calls; await `CallAsync` with typed requests from `Madbox.LiveOps.DTO`.
2. For modules using `GameClientModuleBase<T>`, implement `InitializeAsync` and read `GetModuleData<T>()` after LiveOps layer completes.
3. Add handlers for nested response types you need to process from `ModuleResponse.Responses` (direct children only — no deep traversal).

## Examples

### Minimal

```csharp
var response = await liveOpsService.CallAsync<MyResponse>(new MyRequest(), cancellationToken);
```

### Realistic

```csharp
builder.Register<MyResponseHandler>(Lifetime.Scoped).AsImplementedInterfaces();
// Handler picked when nested response type matches HandledResponseType
```

### Guard / Error path

```csharp
// GetModuleData before initial GameData completes — ensure IAsyncLayerInitializable ordering in bootstrap
```

## Best Practices

- Keep `LiveOpsService` as the single coordinator for initial `GameDataRequest`.
- Register handlers in DI with `AsImplementedInterfaces()` to satisfy dispatch resolution.
- Keep client modules free of direct HTTP; use `ICloudCodeModuleService` / `ILiveOpsService`.

## Anti-Patterns

- Manually enumerating all `IGameClientModule` inside `LiveOpsService` — use `GameClientModuleBase<T>` pattern and bootstrap ordering instead.
- Deep-walking nested responses in custom code — use `IResponseHandler` pipeline for direct items.

## Testing

- Test assemblies: `Assets/Scripts/Core/LiveOps/Tests` — `LiveOpsInitializationTests`, `GameClientModuleBaseTests` (EditMode).
- Run:

```powershell
& ".\.agents\scripts\run-editmode-tests.ps1" -AssemblyNames "Madbox.LiveOps.Tests"
```

- Expected: all tests pass, zero failures.
- Bugfix rule: add/update regression test first.

## AI Agent Context

- Invariants: initial `GameDataRequest` populates aggregated `GameData`; dispatch only inspects direct `Responses` entries.
- Allowed Dependencies: `Madbox.CloudCode`, `Madbox.Ugs`, DTO, Infra scopes.
- Forbidden Dependencies: Feature UI, direct scene objects in service.
- Change Checklist: update DTO DLL when server contracts change; rerun LiveOps tests; align `LiveOps/` backend name with client module string.
- Known Tricky Areas: handler registration ordering vs `LiveOpsService` construction; `GetModuleData` timing.

## Related

- `Docs/LiveOps.md`
- `Docs/Meta/LiveOpsLevel.md`
- `Docs/Meta/Ads.md`
- `Docs/Guides/Upload-LiveOps-Cloud-Code-Backend.md`
- `Architecture.md`

## Changelog

- 2025-03-23: Restructured to module documentation standard (merged prior Registration/Tests into standard sections).
