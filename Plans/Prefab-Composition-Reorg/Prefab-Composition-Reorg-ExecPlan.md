# Reorganize Hero and BeeEnemy prefab composition into lanes

This ExecPlan is a living document. The sections `Progress`, `Surprises & Discoveries`, `Decision Log`, and `Outcomes & Retrospective` must be kept up to date as work proceeds.

This document must be maintained in accordance with `PLANS.md` at the repository root.

## Purpose / Big Picture

After this work, both `Assets/Prefabs/Heroes/Hero.prefab` and `Assets/Prefabs/Enemies/BeeEnemy.prefab` share the same, predictable composition layout: a minimal root that contains generic entity behavior, plus child lane objects that group components by responsibility (`StateData`, `Capabilities`, `Reactors`, `Presentation`). This improves readability, onboarding, and maintenance without introducing hard boundaries between components.

A reviewer can verify success by opening each prefab and seeing the same lane hierarchy, with feature components moved to their lane while preserving runtime behavior (movement, damage response, death, visuals). The weapon slot path is also explicitly defined with a low-risk upgrade path.

## Progress

- [x] Authored this ExecPlan and aligned it to repository plan standards.
- [ ] Execute Milestone 1: Inventory current `Hero.prefab` and `BeeEnemy.prefab` components and map each to a lane.
- [ ] Execute Milestone 2: Create lane child objects and reparent components with no behavior change.
- [ ] Execute Milestone 3: Stabilize references, run tests, and validate no regressions.
- [ ] Execute Milestone 4: Introduce weapon slot improvements (only if trigger criteria are met).
- [ ] Run `.agents/scripts/validate-changes.cmd` and fix any new failures until clean.
- [ ] Update relevant documentation under `Docs/` for prefab composition conventions.

## Surprises & Discoveries

- Observation: None yet. This section must be updated during implementation with concrete evidence.
  Evidence: N/A.

## Decision Log

- Decision: Use the same lane names and hierarchy shape for both player and enemy prefabs.
  Rationale: Consistent structure lowers cognitive load and simplifies handoff between gameplay and content teams.
  Author: User + agent (planning)

- Decision: Keep cross-lane references allowed; lanes are organizational, not architectural boundaries.
  Rationale: The goal is clarity and maintainability without over-constraining Unity component wiring.
  Author: User + agent (planning)

- Decision: Keep root components minimal and generic (core entity component, `Damageable`, and global physics/collider concerns).
  Rationale: Prevent root bloat and make prefab entry points obvious.
  Author: User + agent (planning)

- Decision: Treat weapon slot changes as an incremental track, not a forced system rewrite.
  Rationale: Avoid premature complexity while still preparing for future multi-weapon or dynamic mount needs.
  Author: User + agent (planning)

## Outcomes & Retrospective

At completion, both prefabs expose the same organizational model, making component ownership obvious and reducing accidental coupling through ad hoc hierarchy growth. Remaining work, if any, should be additive and focused on new gameplay features rather than structural cleanup.

## Context and Orientation

This plan reorganizes composition for:

- `Assets/Prefabs/Heroes/Hero.prefab`
- `Assets/Prefabs/Enemies/BeeEnemy.prefab`

Current issue: the prefab composition is harder to scan because behavior components are not grouped by role. The target model keeps all behavior on one prefab but places components under lane children so responsibilities are visually grouped.

Definitions used in this plan:

- Root means the top-level prefab object (`Hero` or `BeeEnemy`) that should keep only generic and globally relevant components.
- Lane means a child GameObject used to group a responsibility area.
- `StateData` means state storage and flags (`CanMove`, `CanAct`, `IsAlive`, `IsStunned`, attributes, stats, loadout or equivalent).
- `Capabilities` means active logic initiated by actor intent (movement, attack, skill cast, dodge).
- `Reactors` means passive reactions to external events (hit reaction, knockback response, invulnerability windows, death response).
- `Presentation` means animation, model presentation, VFX/SFX, HUD binders, and visual mounts (including weapon visuals).

The lanes are not dependency boundaries. Components can reference across lanes when required by runtime flow.

## Plan of Work

Milestone 1 starts with an inventory of every component currently present in `Hero.prefab` and `BeeEnemy.prefab`. For each component, classify whether it belongs on root, `StateData`, `Capabilities`, `Reactors`, or `Presentation`. If a component has mixed responsibilities, keep it in the best-fit lane for now and log a follow-up split task rather than combining migration with refactoring.

Milestone 2 performs structure-only reorganization. Create missing lane children on each prefab with identical naming and order. Reparent components into the target lane objects while preserving serialized field values and object references. Keep root restricted to core/generic components and global collider or rigidbody setup.

Milestone 3 validates behavior and wiring. Verify that serialized references remain valid after reparenting, especially links to animation, hit/death events, and HUD binders. Run focused tests for gameplay and entity behavior, then run the repository quality gate. Fix regressions before proceeding.

Milestone 4 handles weapon-slot evolution. Keep existing fixed slot setup by default. Introduce a resolver abstraction only if current or incoming content requires dynamic socket selection, per-weapon offsets, or runtime swapping complexity beyond simple fixed attachment.

## Concrete Steps

All commands assume working directory is the repository root (`c:\Unity\Madbox`).

1. Inspect prefab and composition references:

    - Open `Assets/Prefabs/Heroes/Hero.prefab`.
    - Open `Assets/Prefabs/Enemies/BeeEnemy.prefab`.
    - List all attached components and current child hierarchy for both.

2. Build and keep a mapping list (component -> target lane):

    - Root: core entity component (`Player` or enemy root behavior), `Damageable`, global collider/rigidbody, other global behavior.
    - `StateData`: attributes/stats/loadout/state flags.
    - `Capabilities`: movement/attack/skill/dodge.
    - `Reactors`: hit, knockback, i-frames, death.
    - `Presentation`: animation, model, VFX/SFX, HUD, weapon visuals.

3. Reorganize each prefab:

    - Create lane children if missing: `StateData`, `Capabilities`, `Reactors`, `Presentation`.
    - Keep existing `Model` branch under `Presentation` (or move it there).
    - Reparent components according to mapping.
    - Keep component field values unchanged.

4. Validate in editor and tests:

    - Run focused tests first (assemblies that cover gameplay/entity/presentation touched by these prefabs).
    - Run full gate:

        .agents\scripts\validate-changes.cmd

    - If failures appear, fix and rerun until clean.

5. Update docs:

    - Add or update a docs file under `Docs/` that defines lane intent and naming conventions for actor prefabs.

## Validation and Acceptance

Acceptance is behavioral and structural:

- `Hero.prefab` and `BeeEnemy.prefab` each contain lane children named `StateData`, `Capabilities`, `Reactors`, and `Presentation`.
- Root on both prefabs contains only generic/global components and no lane-specific feature bloat.
- Runtime behavior remains unchanged for core interactions: move/attack (if applicable), receive damage, react to hit, death path, and visual presentation.
- Existing tests for touched modules pass, and `.agents/scripts/validate-changes.cmd` is clean for new changes.
- Weapon visual mount still works after reorganization; if weapon-slot enhancements are added, they are covered by tests.

## Idempotence and Recovery

This reorganization is safe to perform incrementally per prefab. If a reparenting step breaks references, revert that prefab only to the previous working state, then repeat with smaller moves. Do not combine logic refactors with hierarchy migration in the same step; keep migration structure-only first, then refactor if needed.

If migration reveals unknown dependencies tied to hierarchy paths, preserve compatibility by adding temporary adapter references on presentation components and remove them only after tests confirm stability.

## Interfaces and Dependencies

No new mandatory runtime interface is required for lane migration itself. Existing gameplay interfaces remain unchanged. Optional weapon-slot hardening can introduce a small resolver contract in presentation if needed, for example:

    public interface IWeaponMountPointResolver
    {
        Transform ResolvePrimaryMount();
        Transform ResolveMount(string socketId);
    }

Only introduce this optional interface if current fixed-slot authoring blocks real content needs (multiple weapon classes, runtime swapping, skin-specific offsets, or multi-hand mounts). Otherwise keep current fixed slot design.

Dependencies and modules likely impacted by this plan:

- Prefab assets under `Assets/Prefabs/Heroes/` and `Assets/Prefabs/Enemies/`.
- Presentation/runtime MonoBehaviours attached to those prefabs.
- Gameplay and entity tests that assert health, damage, and visual binder behavior.
- Documentation under `Docs/`.

## Artifacts and Notes

Add concise implementation evidence here during execution:

- Before/after hierarchy snapshots for each prefab.
- Any reference-fix notes discovered during migration.
- Test command outputs showing pass status.
- If weapon-slot enhancement is applied, include the trigger reason and resulting behavior proof.

---

Revision history:

- Initial plan authored from user requirements for lane-based prefab composition and weapon-slot strategy.
