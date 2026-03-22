# LiveOps

Minimal UGS **Cloud Code** backend under `LiveOps/`. Unity loads wire types from **`Assets/Plugins/Madbox.LiveOps.DTO/Madbox.LiveOps.DTO.dll`**.

## What is included

| Area | Role |
|------|------|
| **PingPong** | `PingPongService` — endpoint `PingRequest` → `PongResponse` |
| **Gold / Level / Tutorial / Global** | `IGameModule` types + `*ConfigModule` for local JSON config |
| **GameData** | `GameData`, `IGameModuleData`, `GameDataExtensions.GetKey<T>()` |
| **Requests / responses** | `InitializeGameModulesRequest`, `GameDataRequest`, `GameDataResponse`, `ModuleRequest` / `ModuleResponse`, level & tutorial complete types |
| **GameModulesController** | `InitializeGameModulesRequest`, `GameDataRequest` batch init |
| **ModuleConfig** | `ICloudCodeSetup` — registers DI (global namespace) |
| **SignalModule** | Used by `ModuleRequestHandler` when resolving module requests |
| **ModuleRequestHandler** | Saves player cache after responses; used by level/tutorial completes |
| **FetchData** | `UnityPlayerData`, `UnityGameState`, `LocalOnlyRemoteConfig` (reads `Project/Configs/*.json` only) |

Auth for batch init: if `request.AuthKey` matches `GameState` value at `ModuleKeys.Auth` / `ModuleKeys.UnityToken`, **server** modules run; otherwise **client** modules run.

## Build DTO (Unity plugin)

```powershell
dotnet build "LiveOps\LiveOps.DTO\Madbox.LiveOps.DTO.csproj" -c Release
Copy-Item "LiveOps\LiveOps.DTO\bin\Release\netstandard2.0\Madbox.LiveOps.DTO.dll" "Assets\Plugins\Madbox.LiveOps.DTO\Madbox.LiveOps.DTO.dll" -Force
```

## Build Cloud Code module

```powershell
dotnet build "LiveOps\Project\LiveOps.csproj" -c Release
```

Deploy with `Configs` next to the module if you rely on local JSON. Top-level keys in each JSON file must match DTO type names (e.g. `TutorialConfigData`).

`LiveOps/Directory.Build.props` disables repo Roslyn analyzers for these projects.
