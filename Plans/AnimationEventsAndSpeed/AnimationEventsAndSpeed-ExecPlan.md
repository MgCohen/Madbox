# Animation Events for Player/Enemy Behaviors and Per-Clip Speed Scaling

This ExecPlan is a living document. The sections `Progress`, `Surprises & Discoveries`, `Decision Log`, and `Outcomes & Retrospective` must be kept up to date as work proceeds.

This document must be maintained in accordance with `PLANS.md` at the repository root.

This plan complements view-side character control work described in `Plans/GameView-Character-Controls/GameView-Character-Controls-ExecPlan.md` and `Plans/ArcherInputAndPlayerRefactor/ArcherInputAndPlayerRefactor-ExecPlan.md` when those modules are present in the tree. If those paths are absent, treat the integration points below as the contract the implementing branch must satisfy once `App/GameView` (or equivalent) exists.

## Purpose / Big Picture

After this work, a designer can place timed markers on an attack (or other) animation clip in the Unity Animation window. When playback reaches a marker, the running character’s view layer raises a typed, stable identifier that routes to a registered handler. Gameplay-facing code (for example a player attack behavior) can react at that exact frame—spawn a projectile, play a VFX cue, start a hit window, or enqueue an intent for core battle logic—without hard-coding string function names scattered across clips and code.

Separately, attack (or other) animations can play faster or slower based on stats such as attack speed, while movement or idle animations keep their normal speed. A reviewer verifies this in Play Mode by observing a projectile spawn synchronized with an animation marker and by changing an attack-speed value and seeing only the attack motion accelerate.

## Progress

- [x] (2026-03-22) Authored ExecPlan: animation event bridge, ScriptableObject identity, registry routing, service/battle integration shape, and per-animation speed strategy.
- [x] (2026-03-22) Added explicit milestones and Progress checklist items (PLANS.md compliance).
- [ ] Execute Milestone 1: ScriptableObject event identity, sample assets, module documentation stub for the owning assembly.
- [ ] Execute Milestone 2: `MonoBehaviour` animation event router, register and unregister API, EditMode tests for dispatch and unknown-id behavior.
- [ ] Execute Milestone 3: Player prefab wiring, clip events calling the router, one gameplay-facing handler (for example projectile spawn or intent) through a narrow facade.
- [ ] Execute Milestone 4: Enemy (or second character) reuses the same router and registration pattern; document shared vs per-enemy event assets.
- [ ] Execute Milestone 5: Attack-speed multiplier on animator attack states only; view sets float from stats; verify locomotion unchanged in Play Mode.
- [ ] Execute Milestone 6: Complete `Docs/` for the module, run `.agents/scripts/validate-changes.cmd` until clean, update `Outcomes & Retrospective`.

## Milestones

Milestone 1 establishes **authoring identity**. Implement the ScriptableObject type with a stable serialized key (integer recommended for clip `intParameter`), create at least one real asset under a clear folder such as `Assets/Data/AnimationEvents/`, and add or extend the module doc under `Docs/` so designers know how to pick an asset and which numeric value must appear on clips. At the end of this milestone, the project compiles and the new types are discoverable in the editor; no runtime routing is required yet.

Milestone 2 delivers the **router core**. Add the single public animation callback method on a component colocated with the character `Animator`, implement lookup keyed by stable id, support multiple handlers per id if that is the chosen contract, and add EditMode tests that simulate `AnimationEvent` payloads and assert handler invocation and safe behavior for unknown or zero ids. Validation is automated tests plus a quick manual Play Mode smoke test with a temporary `Debug.Log` handler if desired.

Milestone 3 is the **player vertical slice**. Wire the hero (or main test character) so an attack clip fires an event at a chosen frame, the router dispatches, and a behavior or facade performs one observable effect aligned with the current battle or view pipeline (spawn, intent, or authoritative request through existing APIs). Acceptance is reproducible in the main gameplay scene or a dedicated test scene documented in this plan’s Concrete Steps.

Milestone 4 is **enemy parity** where applicable. Reuse the router and SO ids for shared semantics; add enemy-specific assets only when behavior diverges. Enemies whose `Animator` lives on a child object must host or forward callbacks per Unity’s rules (router on the object Unity targets). Milestone 4 is complete when at least one enemy path is documented and either implemented or explicitly deferred with rationale in the Decision Log.

Milestone 5 is **per-animation speed**. Animator controllers gain a float (or equivalent) applied only inside attack-related states or layers; view code sets it from attack speed stat. Acceptance requires a before and after observation in Play Mode: attack motion scales, idle or walk does not. Add a regression test if the project can assert animator parameter values without brittle animation-controller coupling; otherwise document manual acceptance and keep a minimal parameter-set test.

Milestone 6 is **closure**. Finish module documentation, run the repository quality gate until analyzers and tests are clean, record outcomes and gaps in `Outcomes & Retrospective`, and capture any Unity version-specific animation event quirks in `Surprises & Discoveries`.

## Surprises & Discoveries

- Observation: (placeholder for implementation) Unity forwards animation events to a named public method on a component on the same GameObject as the `Animator` (or on the `Animator` itself if the method lives there). If no matching method exists, the editor typically warns at import time; at runtime missing handlers can log errors depending on Unity version and setup.
  Evidence: (fill in during implementation with Unity version from `ProjectSettings/ProjectVersion.txt` and observed console output.)

## Decision Log

- Decision: Treat animation timing as a view/presentation concern; any authoritative state change (damage, cooldown consumption, spawn requests that affect simulation) crosses into core through existing battle or intent APIs rather than mutating domain from arbitrary animation callbacks.
  Rationale: Matches `Architecture.md` (no Unity in core assemblies; core stays testable).
  Date/Author: 2026-03-22 / Planning

- Decision: Use a ScriptableObject asset as the authoring-time identity for an animation event, backed by a stable integer or GUID serialized on the asset, and duplicate that value into each clip event’s payload (for Unity animation events, the integer parameter field is the practical carrier).
  Rationale: Clips cannot reference ScriptableObject assets directly in the event payload in a type-safe way; a numeric or string key authored to match the SO keeps clips and code in sync without scattering magic strings in C# call sites.
  Date/Author: 2026-03-22 / Planning

- Decision: Prefer a dedicated router component (or small subsystem) on the character that receives Unity’s callback and dispatches to registered delegates keyed by the SO identity, instead of implementing dozens of `public void OnFoo()` methods per clip function name.
  Rationale: Unity animation events are traditionally string-function-name based; one entry point keeps naming stable and moves all branching to data-driven registration.
  Date/Author: 2026-03-22 / Planning

- Decision: Scale attack speed with animator-driven multipliers (float parameters and/or state machine configuration) rather than `Animator.speed` on the whole component.
  Rationale: `Animator.speed` multiplies every layer and state; the requirement is to affect only specific motions (for example attack) while leaving locomotion unchanged.
  Date/Author: 2026-03-22 / Planning

## Outcomes & Retrospective

At completion, summarize here: which assemblies and types were added, how a clip is authored end-to-end, how attack speed is tuned, and any follow-ups (for example enemy parity, network rewind, or animation event validation tools).

## Context and Orientation

Madbox splits Unity-facing code from pure core logic. Anything that touches `Animator`, `AnimationClip`, or `MonoBehaviour` animation callbacks belongs in an app or presentation assembly (for example under `Assets/Scripts/App/`). Core battle logic continues to expose intents, commands, or services that view code calls; animation callbacks must not pull `UnityEngine` types into `Assets/Scripts/Core/*` assemblies.

**Animation clip event** means a time-stamped marker on an animation clip configured in the Unity Animation window. At runtime Unity calls a method you name on a component. The callback receives an `AnimationEvent` struct carrying optional `int`, `float`, and `string` fields. Those fields are the reliable bridge between clip authoring and code.

**Stable event identity** means a value that does not change when you rename the `.asset` file, as long as the asset retains its serialized id field. The ScriptableObject wrapper exists so designers and programmers share one named asset (for example `Attack_ReleaseProjectile`) instead of agreeing on raw strings in multiple places.

**Registry-style routing** means the same pattern as `RuleHandlerRegistry` in `Assets/Scripts/Core/Battle/Runtime/RuleHandlerRegistry.cs` or publish/subscribe via `IEventBus` in `Assets/Scripts/Infra/Events/`: at startup or character spawn, code registers handlers for keys; when an event fires, the router looks up the handler and invokes it. For animation events, registration is expected to be per-character or per-prefab instance so different characters can map the same logical event id to different behaviors.

**Desired flow (concrete):**

1. Player attack behavior starts attack animation on the `Animator` (existing or planned view behavior).
2. The clip reaches a authored event; Unity invokes the single router method on the character.
3. The router reads the integer (or string) payload, resolves the matching `AnimationEventDefinition` (or raw id), and invokes registered callbacks (or raises a small view-local event, or calls injected `IBattleGame` / facade methods).
4. The behavior or service spawns the projectile (view prefab + core intent, depending on existing battle pipeline).

Discover current player and enemy view scripts before editing; repository layout may evolve. From repository root, use search commands such as:

    rg -n "PlayerBehavior|IPlayerBehavior|EnemyActor|Animator" Assets/Scripts Assets/Prefabs Assets/Scenes

Align new types with whatever module already owns the hero and enemies (often `Madbox.GameView` or similar `.asmdef`).

## Plan of Work

### A. ScriptableObject identity

Add a `ScriptableObject` type (name to be fixed during implementation, for example `AnimationEventId` or `CharacterAnimationEventDefinition`) that exposes at minimum:

- A read-only stable key suitable for clip payloads, preferentially `int` (simple to set on each animation event row) or a fixed-width string if you standardize on `stringParameter` in clips.
- Optional display name and help text for designers.

Author one asset per logical moment (for example `PlayerRangedAttack_Release`, `EnemyMelee_HitStart`). Document in `Docs/` for the owning module per repository standards.

### B. Unity callback entry point

Add a `MonoBehaviour` on the character prefab (for example `CharacterAnimationEventRouter`) that implements exactly one public method Unity can target from clips, for example `void OnAnimationEvent(AnimationEvent evt)`. In that method:

- Read the payload field chosen during authoring (recommended: `evt.intParameter` carrying the stable id).
- Look up registered handlers for that id.
- Invoke handlers with a small context struct (instance id, `Animator`, optional `Transform`, optional reference to view model or battle bridge).

If `intParameter` is zero or unknown, log a clear warning in development builds and return without throwing, so missing data does not break play mode during iteration.

### C. Registration API

Expose registration methods mirroring project conventions, for example:

- `void Register(AnimationEventId id, Action<AnimationEventContext> handler)` and matching `Unregister`, or
- Dictionary population from serialized lists in the inspector: pairs of `AnimationEventId` asset references and `UnityEvent` / serializable callbacks where appropriate.

Behaviors that care about timing (player attack, enemy wind-up) register in `OnEnable` and unregister in `OnDisable`. For multiple listeners per id, either allow multicast (list of delegates) or document first-registration-wins; pick one approach in implementation and test it.

Avoid hard-coded string comparisons in handlers; strings stay confined to the single router entry method name required by Unity and optional clip function name field pointing at that method.

### D. Mapping events back through services

Define a narrow facade interface in the appropriate assembly boundary if core must be invoked (for example `IPlayerCombatBridge` with `RequestSpawnProjectile(...)` implemented in app layer and forwarding to `BattleGame` or intent pipeline). Animation router stays injectable or serialized with that facade.

Do not call static singletons from new code; prefer VContainer registration consistent with `Assets/Scripts/App/Bootstrap/Runtime/Layers/BootstrapInfraInstaller.cs` and sibling installers.

If the tree already contains a rich `BattleEventRouter` / intent system (see `Plans/BattleIntentCommandPipeline/BattleIntentCommandPipeline-ExecPlan.md`), emit the same intent types from the facade rather than inventing parallel channels.

### E. Enemy parity

Enemy actors using the same animation system should reuse the same router and SO ids where semantics match (generic `Melee_HitFrame`) or use distinct SO assets when behavior differs. Enemies that use a different prefab root should still host the router on the object that receives animation events (the object with the `Animator`).

### F. Per-animation speed (attack speed stat)

Implement attack-speed scaling without global `Animator.speed`:

- **Preferred:** Add a float animator parameter (for example `AttackSpeedMultiplier`) applied only in the animator controller graph for attack states (blend tree speed, state speed multiplier, or transition to a dedicated attack sub-state machine). View code sets the float from stats each frame or when stats change.
- **Alternative:** Dedicated animator layer for attacks with its own weight and speed tuning, if the controller already splits locomotion and attacks.
- **Discouraged for this requirement:** Mutating imported `AnimationClip` asset speed at runtime (affects all users of the clip) or whole-animator speed (conflicts with requirement).

Document the chosen parameter names in the same module doc as the router so animators and programmers share one vocabulary.

## Concrete Steps

All commands assume a POSIX shell from repository root `/workspace` (adjust drive letters on Windows if needed).

1. Discover integration targets:

    rg -n "Animator|AnimationEvent|PlayerAttack|EnemyActor" Assets/Scripts Assets/Prefabs
    rg -n "BattleGame|Trigger\\(|Raise\\(" Assets/Scripts/Core Assets/Scripts/App

2. Add ScriptableObject definition and at least one sample asset under an appropriate `Resources` or Addressables path if tests need to load it; otherwise place under a clear `Assets/Data/AnimationEvents/` folder.

3. Implement router `MonoBehaviour`, registration API, and one end-to-end path (player attack spawn).

4. Add Edit Mode tests that do not require Play Mode if possible: test the registry and id resolution with plain C# objects. Where Unity’s `AnimationEvent` must be simulated, use a minimal `AnimationEvent` struct population in tests. If assembly definitions block test access, mirror public API in test-friendly ways without weakening production encapsulation.

5. Run quality gate from repository root (PowerShell as documented in `AGENTS.md`):

    .agents/scripts/validate-changes.cmd

   If Unity is unavailable in an agent environment, record the blocker in `Surprises & Discoveries` and complete validation locally.

6. Update the module markdown under `Docs/` that owns the new types (required by repository rules).

## Validation and Acceptance

Acceptance is behavioral:

1. Open the main gameplay scene with the hero (for example `Assets/Scenes/MainScene.unity` if present). Enter Play Mode, trigger an attack, and confirm that at the authored frame the expected side effect occurs (projectile spawn, debug log gated behind development, or test double invocation in play tests).
2. Change attack speed multiplier (or stat that feeds it) and confirm attack animation accelerates while idle or walk does not.
3. Automated: new tests cover id resolution and handler invocation; regression test ensures unknown ids do not crash and registered ids invoke exactly once per event fire when a single handler is registered.
4. `validate-changes.cmd` completes with analyzers clean and tests passing per project norms.

## Idempotence and Recovery

Adding animation events to clips is safe to repeat. Removing an event id from a clip without removing registration yields harmless no-ops at runtime if you handle unknown ids; removing registration while clips still fire should log a warning. ScriptableObject assets can be deleted after removing references from clips and registries.

## Artifacts and Notes

Indented examples below are illustrative, not prescriptive naming.

Example clip authoring row:

- Function: `OnAnimationEvent` (must match router method)
- Time: chosen frame
- Int: matches `Attack_ReleaseProjectile` asset’s stable id

Example handler registration sketch:

    router.Register(attackReleaseId, ctx => combatBridge.RequestSpawnProjectile(ctx));

## Interfaces and Dependencies

By the end of implementation, the following should exist (exact names flexible but concepts required):

- `ScriptableObject` carrying stable animation event identity and designer-facing metadata.
- `CharacterAnimationEventRouter` (or equivalent) `MonoBehaviour` with public void `OnAnimationEvent(AnimationEvent evt)` (or the exact signature Unity expects for your clip settings).
- Registration API for `Action<AnimationEventContext>` (or `UnityAction`) keyed by the SO identity.
- `AnimationEventContext` struct: minimally `Animator source`, `int intParameter`, `float floatParameter`, `string stringParameter`, and optional weak references to view services.
- `IAttackAnimationSpeed` or behavior method that sets animator float `AttackSpeedMultiplier` (name fixed in implementation) from stats.
- `.asmdef` references updated so core assemblies do not reference Unity animation types; bridge interfaces live on the correct side of the boundary.

Revision note (2026-03-22): Initial plan authored from architecture docs and existing registry/event patterns; implementation should fill exact file paths after repository search.

Revision note (2026-03-22): Added Milestones section and six Progress checklist items; `PLANS.md` was always available at repository root—milestones were omitted in the first draft by oversight, not missing inputs.
