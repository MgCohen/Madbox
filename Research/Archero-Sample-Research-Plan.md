# Archero-Like Sample Game Research and Delivery Plan

Date: 2026-03-16
Project: Madbox Unity Technical Assignment

## 1. Purpose and Scope

This document captures research findings and a practical implementation plan for building an Archero-like sample game in this repository, using the provided remote config source.

This is intentionally not an ExecPlan. It is a single research and planning artifact to align decisions before implementation.

## 2. Assignment Requirements (Extracted)

Mandatory requirements from the assignment PDF:

1. Hero movement in 3D environment with one-finger / mouse left click + drag (Archero-like behavior).
2. Enemies spawn at random map positions, move toward hero, and attack.
3. Three enemy prefabs are expected, with different stats.
4. Hero auto-attacks when not moving.
5. Hero auto-selects closest target in range and keeps target until dead or hero moves.
6. Hero can equip 3 weapons: curved sword, greatsword, longsword.
7. Weapon changes must affect:
   - attack animation speed
   - damage timing in animation
   - movement speed
   - attack range
8. Weapon selection method is up to us.
9. Unity Addressables must be used.
10. A full game loop is required.
11. Live Ops system required using provided Google Sheet JSON source.
12. Sheet edits must affect game on next session.
13. README must explain approach, time spent, difficulties, improvements, next steps, and Live Ops behavior.

## 3. Current Project Baseline (Research)

1. Scene: `Assets/Scenes/MainScene.unity` exists.
2. Prefabs available:
   - Hero: `Assets/Prefabs/Heroes/Hero.prefab`
   - Weapons: `Assets/Prefabs/Weapons/CurvedSword.prefab`, `GreatSword.prefab`, `LongSword.prefab`
   - Enemy: `Assets/Prefabs/Enemies/Bee.prefab` (only one enemy currently present)
3. Addressables package and settings are present under `Assets/AddressableAssetsData/`.
4. No `Assets/Scripts/` runtime module structure exists yet in this checkout.

Implication: we must introduce module structure and likely create two additional enemy variants (or equivalent distinct enemies) to satisfy the 3-enemy expectation.

## 4. Remote Config Findings

Endpoint used:

- `https://script.googleusercontent.com/a/macros/madboxgames.io/echo?...`

Payload shape:

- Root object with:
  - `entities: []`
  - `global: {}`

Observed keys:

- `global`: `key`, `number`, `bool`, `string`, `version`, `environment`, `bee_easy_attack`
- `entities` examples:
  - Generic entries: `Entity 0..3` with `plainJson`
  - Potentially useful entries: `BannerRefresh`, `InterstitialDelay`, `TutorialScene`, `SquareColor`, `bee_easy`, `player`

Important schema characteristics:

1. Mixed typing in `description` (string or number depending on entry).
2. Some entries include `number`; some do not.
3. Unknown or non-gameplay keys are present and should not break startup.

## 5. Unknown Keys Strategy (Recommended)

### 5.1 Two-Pass Config Pipeline

1. Pass A (Raw persistence): store the raw JSON snapshot exactly as received.
2. Pass B (Typed mapping): map known keys to typed gameplay values with defaults and validation.

Benefits:

- Forward compatibility with evolving sheet columns.
- No data loss for unknown keys.
- Deterministic gameplay config despite remote schema drift.

### 5.2 Key Classification Model

Classify keys into three buckets:

1. `KnownGameplay`
   - Directly affects combat/movement/spawn/tuning.
2. `KnownNonGameplay`
   - Supports UX/debug/live ops observability.
3. `Unknown`
   - Preserved and surfaced in diagnostics; no hard failure.

### 5.3 Validation and Fallback Rules

1. Required known gameplay key missing/invalid:
   - Use documented default.
   - Emit warning entry.
2. Unknown key:
   - Preserve raw value.
   - Record as unmapped.
3. Parse/type mismatch:
   - Never crash gameplay startup.
   - Downgrade to defaults for typed settings.

### 5.4 Promotion Workflow

Unknown keys can be promoted to typed mappings when needed:

1. Add mapping rule.
2. Add automated test for parsing and application.
3. Update Live Ops documentation and README notes.

### 5.5 Suggested Initial Mapping of Current Keys

Gameplay-relevant now:

1. `global.bee_easy_attack` -> Bee attack damage baseline.
2. `entities[name=bee_easy].description` -> Bee HP (numeric parse).
3. `entities[name=bee_easy].number` -> Bee move speed or reward value.
4. `entities[name=player].description` -> Player base HP.
5. `entities[name=player].number` -> Player base attack.

Non-gameplay or placeholder mapping (still used):

1. `BannerRefresh` -> ad cadence placeholder setting.
2. `InterstitialDelay` -> ad cooldown placeholder setting.
3. `TutorialScene` -> startup scene routing token.
4. `SquareColor` -> UI/debug theme setting.
5. `Entity 0..3` + `plainJson` -> generic tunables list shown in debug panel/log.

## 6. Layered Architecture Proposal

Use contracts-first module organization and explicit assembly references.

### 6.1 Layers

1. App Layer
   - Scene bootstrap, DI composition, high-level flow orchestration.
2. Presentation Layer (Unity-facing)
   - MonoBehaviours, animations, camera, UI adapters, input adapters.
3. Domain/Core Layer (Unity-agnostic where possible)
   - Combat rules, targeting, weapon modifiers, game loop state transitions.
4. Meta Layer
   - Session progression/rewards/score-like meta state.
5. Live Ops Layer
   - Typed remote config model, mapping policy, defaults, application strategy.
6. Infrastructure Layer
   - HTTP client, JSON parser, persistence/cache, addressables adapters.
7. Test Layer
   - Per-module EditMode tests and minimal PlayMode runtime coverage.

### 6.2 Dependency Direction

1. Presentation depends on Domain contracts and App orchestrators.
2. Domain depends only on its own contracts/value types.
3. Live Ops runtime depends on Live Ops contracts + Infra abstractions.
4. App/Bootstrap may reference runtime modules to wire DI.
5. Non-bootstrap modules should consume other modules through `Contracts` assemblies only.

## 7. Proposed Modules for This Assignment

Suggested module tree under `Assets/Scripts/`:

1. `App/Bootstrap`
   - Startup orchestration and DI container setup.
2. `App/Gameplay`
   - Gameplay screen-level orchestration and state transitions.
3. `Core/Combat`
   - Health, damage, attack windows, death handling.
4. `Core/Movement`
   - Hero movement intent and movement state.
5. `Core/Targeting`
   - Closest-in-range selection, sticky targeting rule.
6. `Core/Weapons`
   - Weapon definitions and per-weapon modifiers.
7. `Core/Spawning`
   - Enemy random spawn policy and pacing.
8. `Meta/Session`
   - Run/session values (score, kills, run status).
9. `LiveOps/Config`
   - Typed config contracts and mapping engine.
10. `Infra/RemoteConfig`
   - Fetch/deserialize/cache raw remote payload.
11. `Infra/Persistence` (optional split)
   - Local cached config and small session persistence.
12. `Presentation/Gameplay3D`
   - Unity components for hero/enemy visuals, animations, camera, UI hooks.

Each module should follow:

- `Contracts/`
- `Runtime/`
- `Container/` (if DI registration needed)
- `Tests/`
- optional `Editor/`, `Samples/`

## 8. Addressables Usage Plan

Minimum compliant approach:

1. Register hero prefab, enemy prefabs/variants, and weapon prefabs as addressable entries.
2. Load selected gameplay content through Addressables at runtime (not only serialized scene references).
3. Keep address keys centralized in a small contract to avoid hardcoded string drift.

Recommended practical split:

1. Domain decides what to spawn/equip.
2. Infra/Addressables adapter resolves key -> prefab handle.
3. Presentation instantiates loaded prefab and binds visuals.

## 9. Gameplay Scope Plan (Archero-Like)

### 9.1 Movement and Attack

1. Drag input defines movement direction.
2. Hero attacks only while not moving.
3. Attack cycle uses weapon profile for:
   - attack speed multiplier
   - hit timing normalized time
   - attack range
4. Movement speed includes weapon modifier.

### 9.2 Targeting Rule

1. While idle: choose closest enemy in range.
2. Keep target locked until:
   - target dies, or
   - hero starts moving.

### 9.3 Enemies

1. Spawn enemies at valid random points around arena.
2. Nav/chase hero and attack in range.
3. Define at least 3 enemy stat profiles (Bee base + two variants).

## 10. Live Ops Behavior Plan

Session model:

1. On startup:
   - Load last cached config immediately.
   - Fetch fresh remote config in background.
2. On successful fetch:
   - Validate/map and cache raw + typed snapshot.
   - Apply according to chosen policy.

To satisfy assignment wording (next session effect):

1. Safe policy: apply fresh values next app launch only.
2. Optional improvement: also allow immediate non-critical application (UI/debug), keep gameplay tuning to next run.

## 11. Phased Delivery Plan

### Phase 1: Foundation

1. Create module skeleton and asmdefs.
2. Configure DI composition root.
3. Add basic docs for each module.

Deliverables:

- Module folders, asmdefs, DI bootstrap, docs stubs.

### Phase 2: Core Loop

1. Implement hero movement.
2. Implement enemy spawn/chase/attack.
3. Implement health/death and win/lose loop.

Deliverables:

- Playable run from start to end condition.

### Phase 3: Combat Depth

1. Implement auto-attack state machine.
2. Implement closest-target sticky selection.
3. Implement weapon switching + stat modifiers.

Deliverables:

- 3 weapon profiles demonstrably changing feel and stats.

### Phase 4: Live Ops

1. Implement remote fetch + parse + cache.
2. Implement typed mapping + defaults + unknown key handling.
3. Connect mapped values to gameplay tuning.

Deliverables:

- Config-driven behavior change after sheet edit on next session.

### Phase 5: Presentation and UX

1. Add basic HUD (HP/status/weapon/config info).
2. Optional juice: hit feedback, death effect, drop placeholder.

Deliverables:

- Clear playable/readable UX for reviewer.

### Phase 6: Quality and Submission

1. Add/adjust tests per module.
2. Ensure analyzers are clean.
3. Run quality gate script.
4. Complete README sections required by assignment.

Deliverables:

- Clean test/analyzer gate and reviewer-ready repository.

## 12. Testing and Validation Strategy

1. EditMode tests (priority):
   - targeting selection/stickiness
   - weapon modifier math
   - config mapping and fallback behavior
   - unknown key preservation logic
2. PlayMode tests (minimal critical):
   - scene bootstrap smoke
   - no fatal exceptions during short automated run
3. Manual acceptance checks:
   - drag movement works
   - idle auto-attack works
   - target lock rule is correct
   - each weapon changes speed/range/timing/move speed
   - remote config change reflected on next launch

## 13. Risks and Mitigations

1. Risk: remote schema drift
   - Mitigation: raw snapshot persistence + typed fallback defaults.
2. Risk: over-scoping beyond 15h recommendation
   - Mitigation: strict must-have scope, keep polish optional.
3. Risk: architecture violations from fast iteration
   - Mitigation: module boundaries early + analyzer gate often.
4. Risk: brittle gameplay due to hardcoded tuning
   - Mitigation: centralize tunables in config layer and weapon profiles.

## 14. Final Delivery Checklist

1. Mandatory gameplay requirements complete.
2. Addressables used at runtime.
3. Live Ops integrated and documented.
4. Three enemy stat profiles present.
5. Tests and analyzer checks passing.
6. README fully populated with assignment prompts.

## 15. Suggested Next Implementation Moves

1. Create module and asmdef skeleton first.
2. Build the smallest playable loop without Live Ops.
3. Integrate Live Ops once core loop is stable.
4. Finish tests/documentation and run full quality gate.
