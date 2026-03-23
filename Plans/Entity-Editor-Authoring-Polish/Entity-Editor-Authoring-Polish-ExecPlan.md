# Entity Editor Authoring Polish Pass

This ExecPlan is a living document. The sections `Progress`, `Surprises & Discoveries`, `Decision Log`, and `Outcomes & Retrospective` must be kept up to date as work proceeds.

This document must be maintained in accordance with `PLANS.md` at the repository root.

## Purpose / Big Picture

After this change, the editor authoring flow for enemies, levels, and level catalogs will be safer and more predictable for designers. The same authored assets should survive domain reloads, map to valid domain definitions, and follow strict assembly placement conventions without weakening analyzer protections. A developer should be able to create/update authoring assets in Inspector, enter Play Mode, and observe stable behavior with no hidden serialization or lifecycle regressions.

This polish plan is a follow-up to `Plans/Entity-Editor-Authoring/Entity-Editor-Authoring-ExecPlan.md` and focuses on correctness, maintainability, and test confidence rather than feature expansion.

## Progress

- [x] (2026-03-18 18:10Z) Reviewed planning standards (`PLANS.md`, `MILESTONE.md`) and architecture/testing context required for an ExecPlan polish pass.
- [x] (2026-03-18 18:20Z) Execute Milestone 1: added regression tests for behavior-definition serializability and `LevelCatalogSO` null-list handling; captured fail-before evidence (2 failing tests).
- [x] (2026-03-18 18:24Z) Execute Milestone 2: hardened authoring serialization for behavior definitions and made custom editor rule-add mutations explicitly persisted.
- [x] (2026-03-18 18:26Z) Execute Milestone 3: tightened SCA0024 config by disabling broad unknown-suffix placement and added analyzer regression test covering default strict behavior.
- [x] (2026-03-18 18:27Z) Execute Milestone 4: closed catalog guard gap (`levels == null`) and validated provider/catalog behavior through updated tests.
- [x] (2026-03-18 18:29Z) Execute Milestone 5: reran targeted tests and full quality gate; all checks clean (`validate-changes.cmd` PASS, analyzers `TOTAL:0`).

## Surprises & Discoveries

- Observation: `EnemyDefinitionSO` stores polymorphic behavior rules through `[SerializeReference]` using runtime behavior definition records.
  Evidence: `Assets/Scripts/Meta/Enemies/Authoring/Definitions/EnemyDefinitionSO.cs`.

- Observation: Editor-managed-reference add/remove flow is custom and should be proven across inspector apply/reload paths, not only reflection-driven tests.
  Evidence: `Assets/Scripts/Meta/Enemies/Editor/EnemyDefinitionSOEditor.cs` and `Assets/Scripts/Core/Levels/Tests/AuthoringDefinitionsTests.cs`.

- Observation: `.editorconfig` currently enables broad unknown-suffix placement for SCA0024, which can relax module asmdef placement constraints globally.
  Evidence: `.editorconfig` key `scaffold.SCA0024.allow_unknown_suffix_in_any_subfolder = true`.

- Observation: two concrete polish risks reproduced immediately once regression tests were added: missing `[Serializable]` markers for behavior definitions and null `levels` list handling in catalog lookup.
  Evidence: fail-before `Madbox.Levels.Tests` run reported 2 failures (`EnemyBehaviorDefinitions_AreMarkedSerializable_ForSerializeReferenceSupport`, `LevelCatalogSO_TryGetLevelReference_WhenLevelsListIsNull_ReturnsFalse`).

## Decision Log

- Decision: Keep this polish plan scoped to correctness, analyzer strictness, and test robustness; do not add new gameplay/runtime features.
  Rationale: The previous plan already delivered feature baseline, so this pass should reduce risk and improve confidence before future expansion.
  Date/Author: 2026-03-18 / Codex.

- Decision: Use regression-first workflow for every bug-level fix in this plan.
  Rationale: AGENTS and `PLANS.md` require fail-before/pass-after verification for bug fixes, and this area includes serialization-sensitive behavior.
  Date/Author: 2026-03-18 / Codex.

- Decision: Preserve module boundaries and avoid moving domain logic into Unity-facing assemblies during polish.
  Rationale: Architecture invariants require core runtime modules to remain Unity-agnostic and explicitly bounded by asmdefs.
  Date/Author: 2026-03-18 / Codex.

- Decision: Keep behavior definitions in runtime and add `[Serializable]` attributes instead of introducing parallel authoring DTO types in this pass.
  Rationale: This fixes `[SerializeReference]` authoring reliability with minimal change surface and no new mapping indirection.
  Date/Author: 2026-03-18 / Codex.

- Decision: Tighten policy through config (`allow_unknown_suffix_in_any_subfolder=false`) and add analyzer test coverage for strict-default behavior.
  Rationale: Enforces module ownership convention without removing configurable analyzer capability.
  Date/Author: 2026-03-18 / Codex.

## Outcomes & Retrospective

Polish scope completed with targeted bug fixes and regression coverage. Authoring behavior remains feature-equivalent but is more robust for serialization and catalog null-handling, while analyzer config now preserves stricter asmdef placement defaults.

Validation evidence:

- Fail-before regression proof: `run-editmode-tests.ps1 -AssemblyNames "Madbox.Levels.Tests"` reported 2 failures after regression tests were introduced.
- Pass-after regression proof: same targeted suite passed 15/15 after fixes.
- Analyzer unit coverage: `dotnet test .\Analyzers\Scaffold\Scaffold.Analyzers.Tests\Scaffold.Analyzers.Tests.csproj` passed 138/138 including new unknown-suffix strictness test.
- Full gate proof: `.agents/scripts/validate-changes.cmd` passed with Scripts asmdef audit `TOTAL:0`, EditMode 154/154, PlayMode 2/2, analyzers `TOTAL:0`.

## Context and Orientation

This plan targets Unity-facing authoring modules introduced in the entity editor authoring foundation. The runtime domain models for levels and enemy behavior definitions remain authoritative and should not be replaced in this pass.

Primary files and modules:

- Authoring definitions:
  - `Assets/Scripts/Meta/Enemies/Authoring/Definitions/EnemyDefinitionSO.cs`
  - `Assets/Scripts/Core/Levels/Authoring/Definitions/LevelDefinitionSO.cs`
  - `Assets/Scripts/Core/Levels/Authoring/Definitions/LevelEnemyEntrySO.cs`
  - `Assets/Scripts/Core/Levels/Authoring/Catalog/LevelCatalogSO.cs`
  - `Assets/Scripts/Core/Levels/Authoring/Catalog/AddressableLevelDefinitionProvider.cs`
- Authoring custom editor:
  - `Assets/Scripts/Meta/Enemies/Editor/EnemyDefinitionSOEditor.cs`
- Domain behavior definitions consumed by authoring:
  - `Assets/Scripts/Core/Levels/Runtime/Behaviors/EnemyBehaviorDefinition.cs`
  - `Assets/Scripts/Core/Levels/Runtime/Behaviors/MovementBehaviorDefinition.cs`
  - `Assets/Scripts/Core/Levels/Runtime/Behaviors/ContactAttackBehaviorDefinition.cs`
- Analyzer and config:
  - `.editorconfig`
  - `Analyzers/Scaffold/Scaffold.Analyzers/ModuleAsmdefConventionAnalyzer.cs`
  - `Analyzers/Scaffold/Scaffold.Analyzers.Tests/ModuleAsmdefConventionAnalyzerTests.cs`
- Tests and docs:
  - `Assets/Scripts/Core/Levels/Tests/AuthoringDefinitionsTests.cs`
  - `Docs/Meta/Enemies.md`
  - `Docs/Core/Levels.md`
  - `Docs/Analyzers/Analyzers.md`

Terms used in this plan:

- Managed reference: Unity serialization mode enabled by `[SerializeReference]` that stores polymorphic C# object instances in serialized data.
- Regression test: an automated test that reproduces a discovered failure before code changes and proves the failure is fixed after the change.
- Analyzer strictness: enforcement level for architecture and code-organization diagnostics, especially module asmdef placement (`SCA0024`).

## Plan of Work

Milestone 1 captures the current behavior with tests before changing implementation. Add focused tests that cover managed-reference authoring paths, null/guard behavior for catalog/provider code, and analyzer convention behavior for `.Authoring` assemblies. For bug-fix paths in this milestone, capture fail-before evidence.

Milestone 2 hardens behavior-rule authoring serialization and editor persistence. Ensure the polymorphic behavior entries used by `EnemyDefinitionSO` are serialized safely across inspector edits and domain reloads. Ensure add/remove operations through `EnemyDefinitionSOEditor` apply and persist deterministic state.

Milestone 3 narrows analyzer flexibility to keep module boundaries strict while still supporting valid `.Authoring` placement. Replace broad unknown-suffix allowance with explicit suffix mapping and deterministic candidate search where possible. Update or add analyzer tests to cover the intended strict behavior.

Milestone 4 addresses runtime-facing guard/lifecycle gaps in authoring catalog and provider paths. Ensure null/empty and missing-reference cases fail with explicit errors, and ensure load/release ownership expectations are verified through tests where applicable.

Milestone 5 updates docs to reflect polished behavior and reruns the full repository quality gate until clean.

## Concrete Steps

Run all commands from repository root: `C:\Users\mtgco\.codex\worktrees\8974\Madbox`.

1. Baseline targeted tests before changes:

    powershell -NoProfile -ExecutionPolicy Bypass -File ".\.agents\scripts\run-editmode-tests.ps1" -AssemblyNames "Madbox.Levels.Tests"

2. Baseline analyzer unit tests after any analyzer/config updates:

    dotnet test ".\Analyzers\Scaffold\Scaffold.Analyzers.Tests\Scaffold.Analyzers.Tests.csproj"

3. Per milestone verification loop:

    powershell -NoProfile -ExecutionPolicy Bypass -File ".\.agents\scripts\run-editmode-tests.ps1" -AssemblyNames "Madbox.Levels.Tests"
    dotnet test ".\Analyzers\Scaffold\Scaffold.Analyzers.Tests\Scaffold.Analyzers.Tests.csproj"

4. Required gate at milestone completion:

    .\.agents\scripts\validate-changes.cmd

5. If gate fails, fix all reported failures and rerun `.agents/scripts/validate-changes.cmd` until clean.

Expected success indicators:

- New/updated regression tests fail before relevant fix and pass after.
- No analyzer regressions introduced by SCA0024 changes.
- Final quality gate reports clean test/analyzer status.

## Validation and Acceptance

Acceptance is complete only when all outcomes below are observable:

1. Designers can add/edit/remove enemy behavior rules in `EnemyDefinitionSO` through Inspector and changes persist after re-open/domain reload.
2. `EnemyDefinitionSO.ToDomain()` and `LevelDefinitionSO.ToDomain()` produce valid domain objects for valid authored input and produce explicit failures for invalid input.
3. `LevelCatalogSO` and `AddressableLevelDefinitionProvider` handle null/empty/missing references predictably with test-backed error behavior.
4. SCA0024 still supports `.Authoring` asmdef placement but does not allow broad unknown suffix placement that weakens module ownership rules.
5. Updated automated tests cover the above and are green.
6. `.agents/scripts/validate-changes.cmd` passes cleanly.

## Idempotence and Recovery

All milestones are additive and can be repeated safely. If a polish change introduces regressions, keep the new tests and revert only the failing implementation delta, then re-run the same tests to confirm recovery. Keep analyzer changes incremental so any strictness rollback is limited to one config/analyzer commit scope.

If inspector serialization behavior differs by Unity runtime state, retain deterministic test helpers and isolate editor-specific behavior in editor assemblies only.

## Artifacts and Notes

Expected artifact updates during execution:

- Regression test additions in `Assets/Scripts/Core/Levels/Tests/AuthoringDefinitionsTests.cs`.
- Possible editor-mode persistence tests in an editor-scoped test assembly if required.
- SCA0024 config adjustments in `.editorconfig`.
- Analyzer behavior/test updates in `Analyzers/Scaffold/Scaffold.Analyzers/` and corresponding tests.
- Documentation updates under `Docs/Core/` and `Docs/Analyzers/`.

Evidence to append while executing:

- Fail-before and pass-after summaries for each bug-fix regression test.
- Analyzer test run summary before and after SCA0024 tightening.
- Final `validate-changes.cmd` summary with clean status.

## Interfaces and Dependencies

Interfaces/types that should remain stable after polish:

- `Madbox.Levels.EnemyDefinition`
- `Madbox.Levels.LevelDefinition`
- `Madbox.Levels.Behaviors.EnemyBehaviorDefinition` and concrete behavior definitions
- `Madbox.Addressables.Contracts.IAddressablesGateway`
- `Madbox.Addressables.Contracts.IAssetHandle<T>`

Prescriptive dependency constraints:

- Keep `Madbox.Levels` runtime assembly Unity-agnostic.
- Keep Unity Inspector/editor behavior in `Madbox.Enemies.Authoring` and `Madbox.Enemies.Editor`.
- Keep analyzer rules and `.editorconfig` aligned; do not rely on broad exemptions as a substitute for explicit folder mapping.
- Keep all cross-module references explicit in `.asmdef`.

Non-goals for this polish plan:

- New gameplay mechanics.
- Battle runtime redesign.
- Addressables concurrency redesign.
- Large folder/module reshuffles unrelated to authoring polish.

---

Revision Note (2026-03-18 / Codex): Initial polish ExecPlan created as follow-up to entity editor authoring foundation, with scope focused on serialization reliability, analyzer strictness, and regression test hardening.
Revision Note (2026-03-18 / Codex): Executed plan end-to-end, including fail-before/pass-after regression verification, analyzer strictness tightening, and full clean quality-gate validation.
