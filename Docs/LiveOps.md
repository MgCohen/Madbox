# LiveOps (repository layout)

## TL;DR

- Purpose: Describes the **LiveOps** Cloud Code solution beside the Unity project: DTO project, host project, Unity plugin, and build/deploy pointers.
- Location: `LiveOps/` at repository root; Unity consumes `Assets/Plugins/Madbox.LiveOps.DTO/Madbox.LiveOps.DTO.dll`.
- Depends on: .NET SDK for building `LiveOps/LiveOps.sln`; Unity Deployment package for Editor deploy (see guide).
- Used by: Backend developers; Unity client via `Madbox.LiveOps` (`Docs/Core/LiveOps.md`).
- Runtime/Editor: server .NET; Unity Editor for deployment workflows.

Keywords: LiveOps, Cloud Code, DTO, LiveOps.dll

## Responsibilities

- Owns: High-level map of `LiveOps/` folders, DTO build output path, copy command into Unity, pointer to Editor deployment guide.
- Does not own: Step-by-step UGS screenshots — see `Docs/Guides/Upload-LiveOps-Cloud-Code-Backend.md`.
- Boundaries: Unity player does not compile `LiveOps/` sources; contracts cross the plugin DLL boundary only.

## Public API

| Artifact | Purpose | Inputs | Outputs | Failure behavior |
|---|---|---|---|---|
| `LiveOps/LiveOps.sln` | Builds DTO + host projects. | `dotnet build` | `Madbox.LiveOps.DTO.dll`, `LiveOps.dll` | Compile errors fail build. |
| `Madbox.LiveOps.DTO.dll` | Shared request/response contracts for client and server. | DTO project compile | Plugin under `Assets/Plugins/...` | Mismatch with deployed backend causes runtime errors. |
| `LiveOps/Project/` | Cloud Code host (`GameModule.*`). | Deploy pipeline | Hosted module | Deploy failures in UGS dashboard/logs. |

## Setup / Integration

1. Build contracts: `dotnet build "LiveOps\LiveOps.sln" -c Release`.
2. Copy DTO to Unity:

```powershell
Copy-Item "LiveOps\LiveOps.DTO\bin\Release\netstandard2.1\Madbox.LiveOps.DTO.dll" "Assets\Plugins\Madbox.LiveOps.DTO\Madbox.LiveOps.DTO.dll" -Force
```

3. Deploy Cloud Code module using Unity **Services > Deployment** and `Assets/Scripts/Core/LiveOps/LiveOps.ccmr` — see `Docs/Guides/Upload-LiveOps-Cloud-Code-Backend.md`.
4. `LiveOps/Directory.Build.props` disables repository Roslyn analyzers for these projects.

## How to Use

1. Change DTO in `LiveOps/LiveOps.DTO/`, rebuild solution, copy DLL into `Assets/Plugins/...`.
2. Change server logic in `LiveOps/Project/`, deploy via Editor workflow.
3. Align Cloud Code module name with client (`ModuleRequest` / `"LiveOps"` default) — see `Docs/Core/LiveOps.md`.
4. Remote config is loaded from configured HTTP/UGS Remote Config; no on-disk JSON fallback in the sample module.

### Layout

| Part | Path | Role |
|------|------|------|
| **DTO** | `LiveOps/LiveOps.DTO/` | Contracts (`GameModuleDTO.*`; assembly `Madbox.LiveOps.DTO`) |
| **Main** | `LiveOps/Project/` | Cloud Code host, `net6.0`, output **`LiveOps.dll`** |

## Examples

### Minimal

```powershell
dotnet build "LiveOps\LiveOps.sln" -c Release
```

### Realistic

```
Edit DTO → build → copy DLL → deploy Cloud Code → run client with UGS auth
```

### Guard / Error path

```csharp
// Client and server DTO out of sync — always copy Release DLL after DTO edits before testing end-to-end
```

## Best Practices

- Build DTO in **Release** before copying into Unity for consistent optimizations and paths.
- Keep module name consistent between UGS dashboard, server assembly, and client requests.

## Anti-Patterns

- Editing only server code without updating client DTO when contracts change.
- Committing secrets or org-specific IDs into shared docs.

## Testing

- Server: build `LiveOps.sln` locally before deploy.
- Client: run `validate-changes.cmd` after plugin updates; use EditMode LiveOps tests (`Madbox.LiveOps.Tests`).
- Bugfix rule: contract changes need matching DTO + server + copied DLL.

## AI Agent Context

- Invariants: Unity references DTO **only** via plugin DLL; `LiveOps/` sources are not compiled by Unity.
- Allowed Dependencies: Documented paths only; verify against current Unity/UGS docs for environment-specific steps.
- Forbidden Dependencies: N/A (doc).
- Change Checklist: update `Docs/Core/LiveOps.md` and guides if deploy paths change; copy DLL after DTO edits.
- Known Tricky Areas: `.ccmr` path to solution; environment selection in Deployment window.

## Related

- `Docs/Core/LiveOps.md`
- `Docs/Guides/Upload-LiveOps-Cloud-Code-Backend.md`
- `Docs/Guides/Upload-Addressables-CCD.md`
- `Architecture.md`

## Changelog

- 2025-03-23: Restructured to module documentation standard (merged prior Layout/Build/Unity client sections).
