# LiveOps Level (Meta)

## TL;DR

- Purpose: Client holder for `LevelGameData` from aggregated `GameData`, merged with Addressables `LevelDefinition` assets (namespace distinct from authoring `Madbox.Levels`).
- Location: `Assets/Scripts/Meta/Levels/LiveOps/Runtime/` (`Madbox.Level`), installer `Madbox.Level.Container`.
- Depends on: `Madbox.LiveOps`, `Madbox.Addressables`, `Madbox.Levels`, DTO plugin.
- Used by: `MainMenuViewModel` via `ILevelMenuService` / `ILevelService`, completion flows calling `CompleteLevelAsync`.
- Runtime/Editor: runtime service.

Keywords: LevelGameData, progression, AvailableLevel, ILevelMenuService

## Responsibilities

- Owns: `LevelService` (`GameClientModuleBase<LevelGameData>`), `GetAvailableLevels()`, `CompleteLevelAsync`, join of preloaded `LevelDefinition` with server `LevelGameData.States`.
- Does not own: Battle session logic, raw Addressables gateway, or menu UI layout.
- Boundaries: Client read model + progression calls; server authoritative for completion.

## Public API

| Symbol | Purpose | Inputs | Outputs | Failure behavior |
|---|---|---|---|---|
| `LevelService` | Hydrates from `GetModuleData<LevelGameData>()`, merges catalog + states. | `InitializeAsync` | `IReadOnlyList<AvailableLevel>` | Empty/partial if assets or data missing. |
| `ILevelMenuService` | Read model for menu (`GetAvailableLevels`). | None | Levels list | Depends on successful init. |
| `CompleteLevelAsync(int levelId)` | Cloud Code completion call. | level id | `CompleteLevelResponse` | `Succeeded` false on failure. |

## Setup / Integration

1. `LevelCatalogInstaller` registers `LevelService` as self, `ILevelMenuService`, `IGameClientModule`, `IAsyncLayerInitializable` from `BootstrapMetaInstaller` after LiveOps.
2. `BootstrapAssetInstaller` preloads `LevelAssetProvider` (label `MadboxLevels`).
3. Ensure `LevelGameData` / `LevelConfig` server ordering matches client `LevelId` on `LevelDefinition` assets.

## How to Use

1. Inject `ILevelService` or `ILevelMenuService` in menu/feature code.
2. Call `GetAvailableLevels()` to bind UI; each item joins LiveOps state with local `LevelDefinition`.
3. On level win, call `CompleteLevelAsync` with the played level id; handle `Succeeded` in UI.

## Examples

### Minimal

```csharp
var levels = levelService.GetAvailableLevels();
```

### Realistic

```csharp
var response = await levelService.CompleteLevelAsync(completedLevelId);
if (response.Succeeded) { /* refresh menu */ }
```

### Guard / Error path

```csharp
// LevelDefinition missing from preload group — level may not appear in AvailableLevel list even if server state exists
```

## Best Practices

- Keep `LevelId` aligned across `LevelDefinition`, server config, and completion calls.
- Refresh available levels after `CompleteLevelAsync` success if UI caches the list.

## Anti-Patterns

- Calling `CompleteLevelAsync` without a successful battle outcome — keep game rules in App/Battle first.
- Hard-coding menu order ignoring `LevelGameData.States` ordering.

## Testing

- EditMode: `Assets/Scripts/Meta/Levels/LiveOps/Tests` — `LevelServiceTests`, `LevelGameDataTests`.
- Run:

```powershell
& ".\.agents\scripts\run-editmode-tests.ps1" -AssemblyNames "Madbox.Levels.Tests"
```

- Expected: all tests pass, zero failures.
- Bugfix rule: add/update regression test first.

## AI Agent Context

- Invariants: `AvailableLevel` is join of preload catalog + `LevelGameData.States`; `LevelService` is same instance as `ILevelMenuService` in sample.
- Allowed Dependencies: `Madbox.LiveOps`, `Madbox.Levels`, Addressables providers.
- Forbidden Dependencies: Battle internals, direct scene loads.
- Change Checklist: update DTO/backend when `LevelGameData` changes; rerun level tests.
- Known Tricky Areas: config order vs UI order; missing Addressables label `MadboxLevels`.

## Related

- `Docs/Meta/Levels.md`
- `Docs/App/MainMenu.md`
- `Docs/Core/LiveOps.md`
- `Architecture.md`

## Changelog

- 2025-03-23: Module doc path aligned with Meta LiveOps level integration (`Docs/Meta/LiveOpsLevel.md`); title updated from the former `Docs/Core/` location.
- 2025-03-23: Restructured to module documentation standard.
