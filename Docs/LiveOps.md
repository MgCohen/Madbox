# LiveOps



Cloud Code backend under `LiveOps/` (Unity repo root): **DTO** (`LiveOps.DTO/`) and **main** module (`Project/`). Unity consumes precompiled **`Madbox.LiveOps.DTO.dll`** only (Newtonsoft.Json types and `GameModuleDTO.*` contracts).



## Layout



| Part | Path | Role |

|------|------|------|

| **DTO** | `LiveOps/LiveOps.DTO/` | Contracts (`GameModuleDTO.*` namespaces in source; assembly name `Madbox.LiveOps.DTO`) |

| **Main** | `LiveOps/Project/` | Cloud Code host (`GameModule.*`), `net6.0`, output assembly **`LiveOps.dll`** |



Build everything with **`LiveOps/LiveOps.sln`** (two projects: DTO + main).



## Unity plugins



After a Release build, copy:



- `LiveOps\LiveOps.DTO\bin\Release\netstandard2.1\Madbox.LiveOps.DTO.dll` → `Assets\Plugins\Madbox.LiveOps.DTO\`



## Build commands



```powershell

dotnet build "LiveOps\LiveOps.sln" -c Release

Copy-Item "LiveOps\LiveOps.DTO\bin\Release\netstandard2.1\Madbox.LiveOps.DTO.dll" "Assets\Plugins\Madbox.LiveOps.DTO\Madbox.LiveOps.DTO.dll" -Force

```



Deploy the **LiveOps** Cloud Code module (dashboard name should match what the client uses, e.g. `"LiveOps"`). Remote config is loaded from the configured HTTP or UGS Remote Config source only; there is no on-disk JSON fallback in the module.



`LiveOps/Directory.Build.props` disables repository Roslyn analyzers for these projects.



## Unity client



Use **`ILiveOpsService`** / **`LiveOpsService`** (`Madbox.LiveOps`, see `Docs/Core/LiveOps.md`) for typed **`ModuleRequest` / `ModuleResponse`** calls, or call **`ICloudCodeModuleService`** directly. Shared contracts live in **`Madbox.LiveOps.DTO.dll`** (`GameModuleDTO.*` namespaces).

