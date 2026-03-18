# Migrate Addressables Contracts to Unity Reference Types

This ExecPlan is a living document. The sections `Progress`, `Surprises & Discoveries`, `Decision Log`, and `Outcomes & Retrospective` must be kept up to date as work proceeds.

This document must be maintained in accordance with `PLANS.md` at the repository root.

## Purpose / Big Picture

After this migration, callers will use Unity-native Addressables key types (`AssetReference`, `AssetReferenceT<TObject>`, and `AssetLabelReference`) instead of custom wrapper structs for references. This reduces conceptual duplication, improves interoperability with inspector-authored assets, and aligns the module API with standard Unity Addressables usage.

The user-visible outcome is simpler and more recognizable Addressables APIs: less custom glue code, clearer onboarding for Unity developers, and lower maintenance overhead while preserving existing behavior through staged compatibility.

## Progress

- [x] (2026-03-18 01:20Z) Authored migration ExecPlan with phased compatibility and validation gates.
- [x] (2026-03-18 02:05Z) Execute Milestone 1: Baseline validated; characterization tests expanded to cover Unity `AssetReference` and `AssetReferenceT<T>` load paths.
- [x] (2026-03-18 02:14Z) Execute Milestone 2: Added Unity-native overloads in contracts and implementation (`AssetReference`, `AssetReferenceT<T>`, `AssetLabelReference`).
- [x] (2026-03-18 02:22Z) Execute Milestone 3: Migrated tests/docs/preload registration usage to Unity-native reference and label APIs.
- [x] (2026-03-18 02:28Z) Execute Milestone 4: Deprecated legacy wrappers (`AssetReferenceKey`, `CatalogKey`) and passed full `.agents/scripts/validate-changes.cmd` with analyzers clean.
- [x] (2026-03-18 02:29Z) Record final retrospective and close migration plan.

## Surprises & Discoveries

- Observation: Current API already has one custom reference wrapper (`AssetReferenceKey`) that semantically overlaps Unity `AssetReference` usage.
  Evidence: `Assets/Scripts/Infra/Addressables/Runtime/Contracts/AssetReferenceKey.cs` and `IAddressablesGateway.LoadAsync<T>(AssetReferenceKey)`.

- Observation: Current terminology uses `CatalogKey` where Unity commonly uses label/key concepts (`AssetLabelReference`, generic key object).
  Evidence: `CatalogKey` appears in gateway/public contracts and preload registration API.

## Decision Log

- Decision: Use additive migration first (introduce new overloads before deleting old ones).
  Rationale: Keeps behavior stable and allows progressive caller migration with minimal break risk.
  Date/Author: 2026-03-18 / Codex

- Decision: Keep strict quality gate at each milestone (`validate-changes.cmd`).
  Rationale: Contract migrations can cause wide compile ripple; frequent gate checks reduce regression blast radius.
  Date/Author: 2026-03-18 / Codex

## Outcomes & Retrospective

Plan authored; implementation not yet started under this document.

Migration completed with additive compatibility preserved. Unity-native reference and label types are now the primary API path, and legacy wrappers remain present only as deprecated compatibility types.

## Context and Orientation

Current custom key surface lives in:

- `Assets/Scripts/Infra/Addressables/Runtime/Contracts/AssetKey.cs`
- `Assets/Scripts/Infra/Addressables/Runtime/Contracts/CatalogKey.cs`
- `Assets/Scripts/Infra/Addressables/Runtime/Contracts/AssetReferenceKey.cs`

Current gateway and preload contracts using these types:

- `Assets/Scripts/Infra/Addressables/Runtime/Contracts/IAddressablesGateway.cs`
- `Assets/Scripts/Infra/Addressables/Runtime/Contracts/IAddressablesPreloadRegistry.cs`

Primary implementation and tests to migrate:

- `Assets/Scripts/Infra/Addressables/Runtime/Implementation/AddressablesGateway.cs`
- `Assets/Scripts/Infra/Addressables/Runtime/Implementation/AddressablesAssetClient.cs`
- `Assets/Scripts/Infra/Addressables/Tests/AddressablesGatewayTests.cs`
- `Docs/Infra/Addressables.md`

Unity-native target types for this migration:

- `UnityEngine.AddressableAssets.AssetReference`
- `UnityEngine.AddressableAssets.AssetReferenceT<TObject>`
- `UnityEngine.AddressableAssets.AssetLabelReference`

## Plan of Work

Milestone 1 establishes behavior lock before API expansion. Extend tests to characterize current behavior around key-based loads, label/catalog loads, preload modes (`Normal`, `NeverDie`), and reference-path loading so migration preserves semantics.

Milestone 2 introduces Unity-native overloads while preserving existing methods. Add gateway contract overloads using `AssetReference`, `AssetReferenceT<TObject>`, and `AssetLabelReference`. Implement adapter logic in gateway/client to resolve these to existing internal load paths, keeping release/ref-count behavior unchanged.

Milestone 3 migrates module usage to new overloads. Update tests, docs, and sample snippets to prefer Unity-native reference types. Keep old wrappers available but marked as migration-path types in docs.

Milestone 4 removes obsolete wrappers once all module usage is migrated and tests are green. Planned removal order:

1. Remove `AssetReferenceKey` usage from contracts/implementation/tests.
2. Replace or rename `CatalogKey` usage to label-based APIs (`AssetLabelReference`) where behavior maps directly.
3. Evaluate `AssetKey` retention: keep only if it still provides meaningful boundary value over plain Addressables key object usage; otherwise replace with Unity-native key pathways.

For every removal, ensure no hidden callsites remain via `rg` audit and verify quality gate clean.

## Concrete Steps

Run all commands from repository root `C:\Unity\Madbox`.

1. Baseline tests:

    powershell -NoProfile -ExecutionPolicy Bypass -File ".\.agents\scripts\run-editmode-tests.ps1" -AssemblyNames "Madbox.Addressables.Tests"

2. Add/adjust migration characterization tests and rerun same command.

3. Implement Milestone 2 additive overloads and run:

    powershell -NoProfile -ExecutionPolicy Bypass -File ".\.agents\scripts\run-editmode-tests.ps1" -AssemblyNames "Madbox.Addressables.Tests"

4. After each milestone, run full gate:

    .\.agents\scripts\validate-changes.cmd

5. If failures occur, fix and rerun until clean before moving to next milestone.

## Validation and Acceptance

Migration is accepted when all conditions hold:

- Gateway supports Unity-native references:
  - `LoadAsync<T>(AssetReference, ...)`
  - `LoadAsync<T>(AssetReferenceT<T>, ...)` (or equivalent strongly typed path)
  - `LoadAsync<T>(AssetLabelReference, ...)` (for grouped loads where applicable)
- Preload registration supports label/reference-based pathways consistent with runtime loading semantics.
- Existing load/release ownership behavior remains unchanged (verified by tests).
- All Addressables tests pass and full quality gate is clean.
- Module docs no longer present custom wrappers as primary API.

Validation commands:

    powershell -NoProfile -ExecutionPolicy Bypass -File ".\.agents\scripts\run-editmode-tests.ps1" -AssemblyNames "Madbox.Addressables.Tests"
    .\.agents\scripts\validate-changes.cmd

## Idempotence and Recovery

The migration is additive-first, so partial implementation is recoverable: if a milestone fails, keep previous contract overloads intact and revert only incomplete edits from the active milestone.

Do not remove any custom key type until all tests and docs are migrated and the full gate is green. This prevents half-migrated APIs from breaking downstream consumers.

## Artifacts and Notes

Files expected to change during migration:

- `Assets/Scripts/Infra/Addressables/Runtime/Contracts/IAddressablesGateway.cs`
- `Assets/Scripts/Infra/Addressables/Runtime/Contracts/IAddressablesPreloadRegistry.cs`
- `Assets/Scripts/Infra/Addressables/Runtime/Implementation/AddressablesGateway.cs`
- `Assets/Scripts/Infra/Addressables/Runtime/Implementation/AddressablesAssetClient.cs`
- `Assets/Scripts/Infra/Addressables/Tests/AddressablesGatewayTests.cs`
- `Docs/Infra/Addressables.md`

Potential removals in final phase (if fully superseded):

- `Assets/Scripts/Infra/Addressables/Runtime/Contracts/AssetReferenceKey.cs`
- `Assets/Scripts/Infra/Addressables/Runtime/Contracts/CatalogKey.cs`
- `Assets/Scripts/Infra/Addressables/Runtime/Contracts/AssetKey.cs` (conditional, based on final design choice)

## Interfaces and Dependencies

Primary dependency requirement:

- Addressables contracts/implementation must consistently reference `UnityEngine.AddressableAssets` types where APIs are now Unity-native.

Stability constraints:

- Preserve handle lifecycle invariants (`IAssetHandle<T>.Release()` idempotence and final-owner release semantics).
- Preserve preload behavior invariants (`Normal` handoff, `NeverDie` residency).
- Maintain `IAsyncLayerInitializable` startup integration path.

Non-goals for this migration:

- Reworking unrelated DI architecture.
- Changing gameplay-facing load policies.
- Expanding Addressables feature scope beyond key/reference type migration.

---

Revision Note (2026-03-18 / Codex): Created initial migration ExecPlan focused on replacing custom key wrappers with Unity-native Addressables reference types via phased compatibility.
Revision Note (2026-03-18 / Codex): Executed all migration milestones, updated contracts/tests/docs, deprecated legacy wrappers, and validated full quality gate clean.
