# Add Addressables Preload Config Wrappers Loaded from Addressables Group

This ExecPlan is a living document. The sections `Progress`, `Surprises & Discoveries`, `Decision Log`, and `Outcomes & Retrospective` must be kept up to date as work proceeds.

This document must be maintained in accordance with `PLANS.md` at the repository root.

## Purpose / Big Picture

After this change, preload policies will be authored as reusable Addressables config wrappers instead of scattered registration code. A developer will define preload entries in wrapper assets, place those wrappers under one Addressables label group, and startup will discover and apply them automatically.

The user-visible behavior is that preload authoring becomes deterministic and inspectable while runtime behavior remains the same: `PreloadMode.Normal` still hands off first ownership and `PreloadMode.NeverDie` still keeps gateway-owned residency.

## Progress

- [x] (2026-03-18 15:26Z) Authored initial ExecPlan with architecture context, typed config design, usage snippets, and validation/acceptance criteria.
- [x] (2026-03-18 15:35Z) Revised architecture to config-wrapper group discovery in Addressables startup, removed public preload API dependency from target design, and added property drawer scope.
- [x] (2026-03-18 16:10Z) Execute Milestone 1: Added preload config wrapper model with `TypeReference`, `AssetReference`/`AssetLabelReference`, and `PreloadReferenceType`.
- [x] (2026-03-18 16:12Z) Execute Milestone 2: Implemented startup wrapper discovery (`addressables-preload-config`) and direct preload-apply flow in startup coordinator.
- [x] (2026-03-18 16:13Z) Execute Milestone 3: Added side-by-side custom property drawer for preload entry authoring under new Addressables editor assembly.
- [x] (2026-03-18 16:31Z) Execute Milestone 4: Removed obsolete public preload registration surface, added/updated tests, updated docs, and passed `.agents/scripts/validate-changes.cmd`.

## Surprises & Discoveries

- Observation: Current preload pipeline is already centralized and startup-driven; `AddressablesGateway` takes `IAddressablesPreloadRegistry`, and `AddressablesStartupCoordinator` consumes the snapshot during `InitializeAsync`.
  Evidence: `Assets/Scripts/Infra/Addressables/Runtime/Implementation/AddressablesGateway.cs` and `Assets/Scripts/Infra/Addressables/Runtime/Implementation/AddressablesStartupCoordinator.cs`.

- Observation: Untyped preload registration is intentionally blocked.
  Evidence: `AddressablesPreloadRegistry.Register(AssetKey/AssetReference/AssetLabelReference, ...)` throws `NotSupportedException` in `Assets/Scripts/Infra/Addressables/Runtime/Implementation/AddressablesPreloadRegistry.cs`.

- Observation: Type serialization utility already exists and should be reused for config assets.
  Evidence: `Docs/Tools/Types.md` documents `TypeReference` in `Assets/Scripts/Tools/Types/`.

- Observation: The current initializer path (`AddressablesLayerInitializer -> IAddressablesGateway.InitializeAsync`) is the right single execution point for config discovery and preload application.
  Evidence: `Assets/Scripts/Infra/Addressables/Runtime/Implementation/AddressablesLayerInitializer.cs` and `AddressablesGateway.InitializeAsync`.

## Decision Log

- Decision: Move preload config discovery into Addressables startup using one config-wrapper label group and remove bootstrap build-callback registration.
  Rationale: Makes preload flow single-point and config-driven, removes per-system preload iteration, and avoids requiring public preload registration API for feature composition.
  Date/Author: 2026-03-18 / Codex + User direction

- Decision: Use `Scaffold.Types.TypeReference` in preload entries to serialize concrete asset types.
  Rationale: Existing utility is repository-standard for serializable type metadata and avoids custom type-string plumbing.
  Date/Author: 2026-03-18 / Codex

- Decision: Replace string preload source value with explicit serialized `AssetReference` and `AssetLabelReference` fields, selected by an enum named `PreloadReferenceType`.
  Rationale: Avoids raw string authoring errors and enables safer inspector UX with conditional display.
  Date/Author: 2026-03-18 / Codex + User direction

- Decision: Avoid `sealed` for newly introduced preload config classes and drawer classes in this change.
  Rationale: Aligns with requested style and keeps extension points open.
  Date/Author: 2026-03-18 / Codex + User direction

## Outcomes & Retrospective

Implementation is complete and quality gates are clean. The preload flow now runs from one startup point using wrapper discovery, with analyzer-clean runtime/editor/test coverage and updated module documentation.

## Context and Orientation

Addressables runtime currently has five relevant seams:

- Public preload registration API in `Assets/Scripts/Infra/Addressables/Runtime/Contracts/IAddressablesPreloadRegistry.cs`.
- In-memory preload store in `Assets/Scripts/Infra/Addressables/Runtime/Implementation/AddressablesPreloadRegistry.cs`.
- Startup application logic in `Assets/Scripts/Infra/Addressables/Runtime/Implementation/AddressablesStartupCoordinator.cs`.
- DI composition in `Assets/Scripts/Infra/Addressables/Container/AddressablesInstaller.cs`.
- Startup entry seam in `Assets/Scripts/Infra/Addressables/Runtime/Implementation/AddressablesLayerInitializer.cs`.

For this plan, "preload config wrapper" means an Addressable ScriptableObject that describes multiple preload requests. One preload request includes an asset type plus a reference-kind enum that chooses between a direct `AssetReference` or an `AssetLabelReference`, and a `PreloadMode`.

`TypeReference` is the existing serializable type wrapper from the Tools Types module (`Docs/Tools/Types.md`). It stores type identity safely and resolves to `System.Type` at runtime.

## Plan of Work

Milestone 1 introduces preload config data structures in Addressables runtime module and a wrapper ScriptableObject authoring asset. Keep model minimal: one wrapper asset contains a list of entries. Each entry stores:

- `TypeReference AssetType`
- `PreloadReferenceType ReferenceType` (`AssetReference` or `LabelReference`)
- `AssetReference AssetReference`
- `AssetLabelReference LabelReference`
- `PreloadMode Mode`

Add guards for invalid entries (null/unresolved type, missing selected reference, non-`UnityEngine.Object` type).

Milestone 2 adds runtime startup discovery in `AddressablesStartupCoordinator` (or a dedicated internal collaborator) that:

- resolves all wrapper keys from one dedicated label (for example `addressables-preload-config`),
- loads wrapper assets,
- converts entries to preload requests,
- applies preload requests directly through existing `AddressablesLeaseStore` path.

Keep the initialization sequence single-point: `AddressablesLayerInitializer -> gateway.InitializeAsync -> startup coordinator`.

Milestone 3 adds editor authoring quality with a custom property drawer for `AddressablesPreloadConfigEntry` that renders side-by-side columns for type, reference kind, selected reference field, and mode. The drawer should hide the irrelevant reference field based on `ReferenceType` and keep row height stable.

Milestone 4 locks behavior with tests and docs. Add tests for:

- config entry validation failures,
- wrapper discovery by label and conversion to preload requests,
- type resolution via `TypeReference`,
- end-to-end bootstrap path proving wrappers are discovered and config entries are preloaded on gateway init.

As part of Milestone 4, remove or internalize public preload registration API if it is no longer needed by production flow.

Update `Docs/Infra/Addressables.md` with authoring and wiring examples. Run full gate.

## Usage Snippets

### How the config looks

```csharp
using System;
using Scaffold.Types;
using UnityEngine;

namespace Madbox.Addressables
{
    [Serializable]
    public class AddressablesPreloadConfigEntry
    {
        [SerializeField] private TypeReference assetType;
        [SerializeField] private PreloadReferenceType referenceType;
        [SerializeField] private AssetReference assetReference;
        [SerializeField] private AssetLabelReference labelReference;
        [SerializeField] private PreloadMode mode;

        public TypeReference AssetType => assetType;
        public PreloadReferenceType ReferenceType => referenceType;
        public AssetReference AssetReference => assetReference;
        public AssetLabelReference LabelReference => labelReference;
        public PreloadMode Mode => mode;
    }

    public enum PreloadReferenceType
    {
        AssetReference = 0,
        LabelReference = 1
    }
}
```

```csharp
using System.Collections.Generic;
using UnityEngine;

namespace Madbox.Addressables
{
    [CreateAssetMenu(menuName = "Madbox/Addressables/Preload Config", fileName = "AddressablesPreloadConfig")]
    public class AddressablesPreloadConfigWrapper : ScriptableObject
    {
        [SerializeField] private List<AddressablesPreloadConfigEntry> entries = new List<AddressablesPreloadConfigEntry>();

        public IReadOnlyList<AddressablesPreloadConfigEntry> Entries => entries;
    }
}
```

### How to register config by code

```csharp
// Configure wrapper assets with Addressables label: "addressables-preload-config"
// Each wrapper holds preload entries; startup discovers and applies all wrappers automatically.
```

### How to register config by asset

```csharp
public class AddressablesPreloadConfigWrapper : ScriptableObject
{
    [SerializeField] private List<AddressablesPreloadConfigEntry> entries;
}
```

```text
Authoring step:
1) Create one or more AddressablesPreloadConfigWrapper assets.
2) Put those wrapper assets in Addressables group with label "addressables-preload-config".
3) No bootstrap code registration is required.
```

```csharp
public class AddressablesStartupCoordinator
{
    private const string PreloadConfigLabel = "addressables-preload-config";

    internal async Task RunInitializationAsync(CancellationToken cancellationToken)
    {
        await TrySyncCatalogAndContentAsync(cancellationToken);
        IReadOnlyList<AssetKey> wrapperKeys = await client.ResolveLabelAsync(
            typeof(AddressablesPreloadConfigWrapper),
            new AssetLabelReference { labelString = PreloadConfigLabel },
            cancellationToken);
        // load wrappers -> convert entries -> apply leaseStore preloads
    }
}
```

### Clear preload config -> gateway flow

```text
AddressablesPreloadConfigWrapper assets (Addressables label: "addressables-preload-config")
  -> AddressablesLayerInitializer calls IAddressablesGateway.InitializeAsync
    -> AddressablesStartupCoordinator resolves all wrapper keys by label
      -> coordinator loads wrappers and converts entries to typed preload requests
        -> AddressablesLeaseStore preloads and applies Normal/NeverDie ownership semantics
```

## Concrete Steps

Run all commands from repository root `C:\Users\mtgco\.codex\worktrees\5dc7\Madbox`.

1. Implement Milestone 1 and run Addressables EditMode tests:

    powershell -NoProfile -ExecutionPolicy Bypass -File ".\.agents\scripts\run-editmode-tests.ps1" -AssemblyNames "Madbox.Addressables.Tests"

2. Implement Milestone 2 startup wrapper discovery/apply logic and add/adjust tests; rerun same command.

3. Implement Milestone 3 property drawer and editor coverage; rerun Addressables tests.

    powershell -NoProfile -ExecutionPolicy Bypass -File ".\.agents\scripts\run-editmode-tests.ps1" -AssemblyNames "Madbox.Addressables.Tests"

4. Implement Milestone 4 docs/API cleanup and run full quality gate:

    powershell -NoProfile -ExecutionPolicy Bypass -File ".\.agents\scripts\run-playmode-tests.ps1" -AssemblyNames "Madbox.Addressables.PlayModeTests"
    .\.agents\scripts\validate-changes.cmd

Expected final state: Addressables tests pass, PlayMode bootstrap test passes, and analyzer total is clean.

## Validation and Acceptance

Acceptance criteria are behavior-first:

- A developer can create preload config wrapper assets, add typed entries (type + asset or label reference + mode), place wrappers under the dedicated config label, and observe startup preload without adding per-feature registration code.
- `PreloadMode.Normal` and `PreloadMode.NeverDie` behavior remains unchanged for first-consumer handoff and residency.
- Invalid config entries fail fast with actionable error messages before runtime load loops start.
- The preload flow has one execution point in Addressables startup initialization.
- No direct Unity Addressables static API usage leaks into feature modules.

Required validation commands:

    powershell -NoProfile -ExecutionPolicy Bypass -File ".\.agents\scripts\run-editmode-tests.ps1" -AssemblyNames "Madbox.Addressables.Tests"
    powershell -NoProfile -ExecutionPolicy Bypass -File ".\.agents\scripts\run-playmode-tests.ps1" -AssemblyNames "Madbox.Addressables.PlayModeTests"
    .\.agents\scripts\validate-changes.cmd

If any bug is found while implementing this plan, add/update a regression test first, verify fail-before/fix/pass-after, then run the full gate.

## Idempotence and Recovery

The plan is additive-first and retry-safe. If wrapper discovery fails, keep existing preload path temporarily while validating wrapper loading and conversion behavior, then remove duplicate path before finalizing.

If a config entry breaks startup due to bad data, recover by removing or fixing only that entry in the ScriptableObject and rerunning tests; no migration step mutates existing gameplay data.

## Artifacts and Notes

Planned files to create or modify during implementation:

- `Assets/Scripts/Infra/Addressables/Runtime/Contracts/` (new config contracts if needed)
- `Assets/Scripts/Infra/Addressables/Runtime/Implementation/` (wrapper discovery/loader + validators)
- `Assets/Scripts/Infra/Addressables/Runtime/Editor/` (new property drawer + editor asmdef if needed)
- `Assets/Scripts/Infra/Addressables/Tests/AddressablesGatewayTests.cs` and/or new focused test files
- `Docs/Infra/Addressables.md`

Execution evidence should be appended here as milestones complete (commands, pass/fail counts, and analyzer totals).

Execution evidence:

    Command: powershell -NoProfile -ExecutionPolicy Bypass -File ".\.agents\scripts\run-editmode-tests.ps1" -AssemblyNames "Madbox.Addressables.Tests"
    Result: Total 19, Passed 19, Failed 0, Skipped 0

    Command: powershell -NoProfile -ExecutionPolicy Bypass -File ".\.agents\scripts\run-playmode-tests.ps1" -AssemblyNames "Madbox.Addressables.PlayModeTests"
    Result: Total 1, Passed 1, Failed 0, Skipped 0

    Command: .\.agents\scripts\validate-changes.cmd
    Result: scripts asmdef audit PASS (TOTAL:0), compilation PASS, EditMode PASS (151/151), PlayMode PASS (2/2), analyzers PASS (TOTAL:0)

## Interfaces and Dependencies

The following runtime interfaces remain stable for consumers:

- `Madbox.Addressables.Contracts.IAddressablesGateway`

New internal/public types introduced by this plan should include:

- `AddressablesPreloadConfigEntry`
- `AddressablesPreloadConfigWrapper`
- `PreloadReferenceType`
- `AddressablesPreloadConfigEntryDrawer` (editor)
- startup discovery/apply collaborator for wrapper loading (name TBD during implementation)

Dependencies and boundaries:

- Use `Scaffold.Types.TypeReference` from `Assets/Scripts/Tools/Types/` for serialized type metadata.
- Keep Addressables runtime behavior and wrapper discovery inside Infra Addressables module; wrapper asset authoring remains content workflow only.
- Keep explicit `.asmdef` references updated when introducing new types across modules.
- Preserve analyzer compliance and existing initialization flow through `IAsyncLayerInitializable`.

---

Revision Note (2026-03-18 / Codex): Created initial ExecPlan to introduce ScriptableObject-based typed preload configs using `TypeReference`, with explicit bootstrap registration flow and behavior-preserving gateway integration.
Revision Note (2026-03-18 / Codex): Updated per user direction to make preload fully config-driven from Addressables wrapper label discovery at startup, rename entry enum to `PreloadReferenceType`, replace string source with `AssetReference`/`AssetLabelReference`, add custom property drawer scope, avoid `sealed` in proposed new classes, and remove build-callback-centric flow.
