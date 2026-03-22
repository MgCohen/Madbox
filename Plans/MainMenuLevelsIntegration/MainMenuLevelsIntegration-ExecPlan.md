# Main Menu: Level definitions from Addressables (label preload + catalog service)

This ExecPlan is a living document. The sections `Progress`, `Surprises & Discoveries`, `Decision Log`, and `Outcomes & Retrospective` must be kept up to date as work proceeds.

Repository planning rules live at `PLANS.md` (repository root). This document must be maintained in accordance with `PLANS.md`.

## Purpose / Big Picture

After this work, when the player reaches the main menu, the game has already loaded every `LevelDefinition` asset that was marked in Addressables with a configured label (for example `MadboxLevels`). The main menu shows one button per loaded level. Tapping a button writes the level asset’s Unity name to the console log (no navigation to a game view yet).

A developer can verify success by tagging level assets with the label, entering Play Mode from the bootstrap flow, opening the main menu, and seeing one button per level; clicking a button should print that level’s `name` in the Console.

## Progress

- [x] (2026-03-22) ExecPlan aligned with `Madbox.Level.LevelService` (LiveOps) vs `Madbox.Levels.LevelDefinition` catalog naming.
- [x] (2026-03-22) Implemented `ILevelDefinitionCatalogService`, `LevelDefinitionCatalogService`, `LevelDefinitionCatalogInstaller`, `LevelDefinitionAssetProvider` (label `MadboxLevels`), bootstrap registration, Main Menu VM/View + tests.
- [ ] Run `.agents/scripts/validate-changes.cmd` until clean in your environment.

## Surprises & Discoveries

- Observation: (none yet)
  Evidence: (none yet)

## Decision Log

- Decision: Use a distinct type name for the gameplay level catalog service (`ILevelDefinitionCatalogService` / `LevelDefinitionCatalogService` in `Madbox.Levels`) instead of a type named only `LevelService`.
  Rationale: The repository already defines `Madbox.Level.LevelService` in `Assets/Scripts/Meta/Levels/LiveOps/` as a LiveOps `GameClientModuleBase<LevelGameData>` type. Reusing the name `LevelService` for `LevelDefinition` assets would confuse readers, analyzers, and dependency injection.
  Date: 2026-03-22

- Decision: Prefer `AssetGroupProvider<LevelDefinition>` (see `Assets/Scripts/Assets/Addressables/Runtime/Implementation/AssetGroupProvider.cs`) for label-based preloading instead of inventing a parallel loader.
  Rationale: The project already implements group load by `AssetLabelReference`, fills a list, and registers `IReadOnlyList<TAsset>` on the child scope via `IAssetRegistrar`. That matches the requested “preload all levels with a certain label” behavior with minimal new code.
  Date: 2026-03-22

- Decision: Optional companion settings asset `LevelBootstrapSettings` (ScriptableObject) holding the `AssetLabelReference` for level definitions, loaded by a narrow `AssetProvider<LevelBootstrapSettings>` mirroring `NavigationAssetProvider`, **or** a single `LevelDefinitionAssetProvider` with a fixed label if we want fewer assets.
  Rationale: `NavigationSettings` is a ScriptableObject listing view configs; navigation preload uses a fixed Addressables address string in `NavigationAssetProvider`. For levels, the label is the natural “switch” for which assets belong to the menu. A small settings object keeps the label editable without code changes; a fixed label in code is acceptable for an early milestone if the team prefers speed.
  Date: 2026-03-22

## Outcomes & Retrospective

- Integrated `origin/main` (2026-03-22) and resolved stash conflicts with this plan’s implementation.
- Main menu builds dynamic level buttons from `ILevelDefinitionCatalogService`; LiveOps `Madbox.Level.LevelService` remains separate for `CompleteLevelAsync` and remote-config IDs (`LevelDefinition.LevelId` matches that ordering per docs).

## Context and Orientation

**What “level” means here.** A **level definition** is a Unity `ScriptableObject` type `Madbox.Levels.LevelDefinition` (`Assets/Scripts/Core/Levels/Runtime/LevelDefinition.cs`). It is data-only (scene reference, enemy spawn lines, rules). This plan only needs to **list** those assets for the menu; it does not start a battle or load a scene on click.

**Addressables gateway.** `Madbox.Addressables.AddressablesGateway` (`Assets/Scripts/Assets/Addressables/Runtime/Implementation/AddressablesGateway.cs`) exposes `LoadAsync<T>(AssetLabelReference label, ...)`, which completes an `IAssetGroupHandle<T>` whose `Assets` list contains every loaded object of type `T` for that label. The gateway is initialized during layered bootstrap (catalog sync, then consumers).

**Group preload helper.** `AssetGroupProvider<TAsset>` (`Assets/Scripts/Assets/Addressables/Runtime/Implementation/AssetGroupProvider.cs`) implements `IAssetPreloader` (loads the group in `PreloadAsync`), `IAssetGroupProvider<TAsset>` (`TryGet` after load), and `IAssetRegistrar` (registers `IReadOnlyList<TAsset>` on the **child** container when `Register` runs). Subclasses only supply the label via `protected abstract AssetLabelReference LabelKey { get; }`.

**Single-asset preload pattern.** `NavigationAssetProvider` (`Assets/Scripts/App/Bootstrap/Runtime/Providers/NavigationAssetProvider.cs`) subclasses `AssetProvider<NavigationSettings>`, returns a fixed `AssetReference` key (`"Navigation Settings"`), and relies on `AssetProvider<T>.Register` to register the loaded `NavigationSettings` instance on the child scope. **Navigation** uses one settings object; **levels** use a **label** and many `LevelDefinition` assets, so the group provider is the closer analog than `AssetProvider<LevelDefinition>`.

**Navigation composition.** `NavigationController` (`Assets/Scripts/Infra/Navigation/Runtime/Implementation/NavigationController.cs`) takes `IAddressablesGateway` and passes it into `NavigationProvider`, which resolves view prefabs through Addressables. Main menu integration does not need to change `NavigationController`; it only needs loaded `LevelDefinition` instances and a small view change.

**Bootstrap layering.** `BootstrapAssetInstaller` (`Assets/Scripts/App/Bootstrap/Runtime/Layers/BootstrapAssetInstaller.cs`) registers Addressables infrastructure, registers concrete `IAssetPreloader` implementations (currently `NavigationAssetProvider`), then in `OnCompletedAsync` **awaits each preloader’s `PreloadAsync`**. Child scopes receive `IAssetRegistrar.Register` calls in `ConfigureChildBuilder`. New level preloaders must be registered here in a **safe order** (settings before group load if the label comes from settings).

**Existing LiveOps “Level” module.** `BootstrapCoreInstaller` (`Assets/Scripts/App/Bootstrap/Runtime/Layers/BootstrapCoreInstaller.cs`) already runs `Madbox.Level.Container.LevelInstaller`, which registers LiveOps-oriented `LevelService`. Do not merge those concerns; keep LiveOps level data and `LevelDefinition` catalog separate.

## Plan of Work

**1. Authoring and Addressables.** Choose or create a label string (for example `MadboxLevels`). Every `LevelDefinition` asset that should appear on the main menu must include that Addressables label. Document the exact string in `Docs/Core/Levels.md` only if the module doc is updated as part of the same change set (optional).

**2. Optional settings asset.** Add a ScriptableObject `LevelBootstrapSettings` (suggested path under `Assets/Scripts/Core/Levels/` or `Assets/Scripts/App/Bootstrap/Runtime/` depending on whether you treat it as core data or bootstrap config) with at least one serialized `AssetLabelReference` field pointing at the label used for `LevelDefinition` assets. Create a default asset under `Assets/Data/...` and give it an Addressables address consistent with the pattern used by `Navigation Settings` if you use `AssetProvider<LevelBootstrapSettings>` with a fixed `AssetReference` string in code.

**3. Asset layer: preloaders.** In `Madbox.Bootstrap` (same namespace area as `NavigationAssetProvider`), add:

- `LevelBootstrapSettingsAssetProvider` : `AssetProvider<LevelBootstrapSettings>` with `protected override AssetReference AssetKey => ...` pointing at your settings asset address (mirror `NavigationAssetProvider`).

- `LevelDefinitionAssetProvider` : `AssetGroupProvider<LevelDefinition>` with `LabelKey` taken from the loaded `LevelBootstrapSettings` (see “Preload order” below) **or** a fixed label if you skip the settings asset for the first milestone.

Register both with `RegisterProvider<T>` in `BootstrapAssetInstaller.Install`, **before** any consumer that needs levels, with **LevelBootstrapSettingsAssetProvider first** if the label is read from settings.

**Preload order constraint.** `IEnumerable<IAssetPreloader>` iteration order must place the settings `AssetProvider` before `LevelDefinitionAssetProvider` when the latter needs `TryGet` on the settings provider. Inject `IAssetProvider<LevelBootstrapSettings>` (the concrete settings provider registered as self) into `LevelDefinitionAssetProvider` and read the label in `LabelKey` only after the settings provider has completed `PreloadAsync`. If `LabelKey` is evaluated lazily during `PreloadAsync` of the group provider, ensure the settings provider has already populated `LoadedAsset`.

**4. Core layer: catalog service.** In `Madbox.Levels` (or `Madbox.Levels.Container` for DI-only types), define `ILevelDefinitionCatalogService` with a method such as `IReadOnlyList<LevelDefinition> GetDefinitions()` (or a property). Implement `LevelDefinitionCatalogService` whose constructor takes `IAssetGroupProvider<LevelDefinition>` (from `Madbox.Addressables.Contracts`) **or** `LevelDefinitionAssetProvider` typed as the group provider interface. `GetDefinitions` should use `TryGet` and return an empty read-only list when nothing was loaded, or throw if the contract requires preload success—state the choice in code and tests.

Add `LevelDefinitionCatalogInstaller` (name may vary) in a Container assembly that registers `ILevelDefinitionCatalogService` in the **core** layer. Wire it from `BootstrapCoreInstaller` similarly to `LevelInstaller` / `LiveOpsInstaller`, without removing the existing LiveOps `LevelInstaller`.

**5. Main menu.** Extend `MainMenuViewModel` to take `ILevelDefinitionCatalogService` via `[Inject]` alongside `IGoldService`. Expose a read-only list for the view. Update `MainMenuView` to create one button per definition under a dedicated `RectTransform` root (reuse the project’s existing TMP + `Button` creation helpers). On click, `Debug.Log(levelDefinition.name)`.

**6. Tests.** Add unit tests for `LevelDefinitionCatalogService` with a fake `IAssetGroupProvider<LevelDefinition>`. If you add logic to `LevelDefinitionAssetProvider` beyond the base class, add focused tests or a thin test double. Follow `Docs/AutomatedTesting.md`. Run `.agents/scripts/validate-changes.cmd` from the repository root as the quality gate.

## Concrete Steps

All commands assume PowerShell on Windows and repository root `c:\Unity\Madbox` (adjust if your path differs).

1. Implement types and installers described in Plan of Work.
2. In Unity, assign Addressables labels to level definition assets; confirm the catalog contains entries for the chosen label.
3. Run headless EditMode tests:

    powershell -NoProfile -ExecutionPolicy Bypass -File ".\.agents\scripts\run-editmode-tests.ps1"

4. Run the full gate:

    .\.agents\scripts\validate-changes.cmd

   Expect the script to report analyzer checks and tests passing with no errors.

5. Manual check: open the Bootstrap scene, enter Play Mode, open the main menu, confirm buttons and Console logs.

## Validation and Acceptance

**Automated:** `validate-changes.cmd` completes with no failures; new tests for `LevelDefinitionCatalogService` pass; existing Main Menu tests remain green (update them if the view model constructor gains a dependency—use a test double for `ILevelDefinitionCatalogService`).

**Manual:** After Play Mode loads the main menu, the number of level buttons equals the number of Addressable `LevelDefinition` assets with the configured label. Clicking a button prints exactly the Unity `name` of that `LevelDefinition` asset (the asset filename without path) to the Console.

## Idempotence and Recovery

Adding preloaders and services is additive. If preload fails (missing label, empty group), prefer showing zero level buttons and logging a **warning** rather than crashing bootstrap, unless the product owner mandates a hard failure—record the choice in the Decision Log.

Removing the feature later means unregistering providers and uninstalling the catalog service; Addressables labels can remain on assets without harm.

## Artifacts and Notes

Indicative references (current tree):

- `Assets/Scripts/Assets/Addressables/Runtime/Implementation/AssetGroupProvider.cs`
- `Assets/Scripts/App/Bootstrap/Runtime/Providers/NavigationAssetProvider.cs`
- `Assets/Scripts/App/Bootstrap/Runtime/Layers/BootstrapAssetInstaller.cs`
- `Assets/Scripts/App/Bootstrap/Runtime/Layers/BootstrapCoreInstaller.cs`
- `Assets/Scripts/Core/Levels/Runtime/LevelDefinition.cs`

## Interfaces and Dependencies

**ILevelDefinitionCatalogService** (new, suggested shape):

    public interface ILevelDefinitionCatalogService
    {
        IReadOnlyList<LevelDefinition> GetDefinitions();
    }

**LevelDefinitionCatalogService** (new): constructor accepts `IAssetGroupProvider<LevelDefinition>`; implementation delegates to `TryGet`.

**LevelDefinitionAssetProvider** (new): subclasses `AssetGroupProvider<LevelDefinition>`; depends on `IAddressablesGateway` and, if used, `IAssetProvider<LevelBootstrapSettings>` for label resolution.

**Assembly references:** Any `Madbox.Levels` type used from `Madbox.Bootstrap` providers must be allowed by `.asmdef` references (Bootstrap already references many assemblies; add `Madbox.Levels` if the provider lives in Bootstrap and touches `LevelDefinition`).

---

**Revision history**

- 2026-03-22: Initial ExecPlan authored from navigation/Addressables patterns and existing `AssetGroupProvider` / layered bootstrap flow.
