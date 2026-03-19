# GameView Character Controls and Visual Behavior Integration

This ExecPlan is a living document. The sections `Progress`, `Surprises & Discoveries`, `Decision Log`, and `Outcomes & Retrospective` must be kept up to date as work proceeds.

This document must be maintained in accordance with `PLANS.md` at the repository root.

This plan builds on existing checked-in work in `Plans/Battle-Loop-WhiteBox/Battle-Loop-WhiteBox-ExecPlan.md`, `Plans/BattleIntentCommandPipeline/BattleIntentCommandPipeline-ExecPlan.md`, and the scope guidance captured in `Research/Archero-Sample-Research-Plan.md`.

## Purpose / Big Picture

After this change, pressing Play in `Assets/Scenes/MainScene.unity` will allow a player to drag the on-screen virtual joystick and immediately see the hero move in the dragged direction with walk animation feedback. This is a visual-integration milestone, so we intentionally keep combat-side behavior minimal: attack behavior for this slice only needs to trigger attack animation, without damage, status, projectile, or advanced effect systems.

The goal is to prove that the current scene setup can drive a basic playable character loop through view-side components: input capture, direction output, movement application, and animation toggles. Then we add a lightweight behavior-component mapping path so player behavior definitions can attach matching view components. Finally, we prepare the architecture split where raw input does not directly mutate gameplay state and instead goes through intent messages as a middle step.

## Progress

- [x] (2026-03-19 18:24Z) Authored initial ExecPlan with milestone order aligned to requested scope: basic controls first, visual behavior mapping next, intent split last.
- [x] (2026-03-19 18:34Z) Execute Milestone 1: Implemented `VirtualJoystickInput` + `PlayerMovementViewBehavior` and `MainScene` auto-wiring so drag direction drives hero movement and walk/idle animation switching.
- [x] (2026-03-19 18:34Z) Execute Milestone 2: Implemented `PlayerAttackAnimationBehavior` (animation-only trigger path with debug key) plus tests validating attack lock trigger behavior.
- [ ] Execute Milestone 3: Add lightweight player behavior-definition -> view-component mapper (single registration dictionary in view/app layer).
- [ ] Execute Milestone 4: Introduce input-intent separation so joystick publishes move intent and movement executes from consumed intent.
- [ ] Execute Milestone 5: Add/update EditMode and PlayMode tests, run `.agents/scripts/validate-changes.cmd` until clean, and update docs.

## Surprises & Discoveries

- Observation: `App/GameView` exists and currently acts as a white-box UI shell, so this effort should extend existing app/view modules rather than create a new gameplay module from scratch.
  Evidence: `Assets/Scripts/App/GameView/Runtime/GameView.cs` and `Docs/App/GameView.md`.

- Observation: The battle runtime already has movement and attack intent event types, which supports the requested future split between input and intent without inventing a new core protocol.
  Evidence: `Assets/Scripts/Core/Battle/Runtime/Events/BattleEvents.cs` and router registrations in `Assets/Scripts/Core/Battle/Runtime/Events/BattleEventRouter.cs`.

- Observation: Current `GameView` runtime uses broad pragma suppressions; this plan must avoid adding new suppressions unless explicitly approved by the user in-thread.
  Evidence: `Assets/Scripts/App/GameView/Runtime/GameView.cs` and `Assets/Scripts/Core/Battle/Runtime/Services/GameViewModel.cs`.

- Observation: `MainScene` hero is a prefab instance without scene-specific added components, so runtime auto-wiring was the most stable way to enable joystick/movement/attack behavior without brittle YAML prefab-instance edits.
  Evidence: `Assets/Scenes/MainScene.unity` prefab instance block `&337934964` has `m_AddedComponents: []`.

## Decision Log

- Decision: Keep this implementation in view/app-facing modules only (`Assets/Scripts/App/*` or dedicated view-side modules), and do not place Unity presentation logic in `Core` gameplay modules.
  Rationale: Matches architecture invariants and the explicit request to keep this visual integration out of `Game` core logic.
  Date/Author: 2026-03-19 / Codex + User

- Decision: Execute in four functional milestones where each one is independently playable or testable.
  Rationale: Supports quick validation and keeps sample scope small.
  Date/Author: 2026-03-19 / Codex + User

- Decision: If a milestone is blocked by missing non-visual contracts or modules, pause and ask the user instead of inventing new modules.
  Rationale: Explicit request to avoid creating new modules unless needed and confirmed.
  Date/Author: 2026-03-19 / Codex + User

## Outcomes & Retrospective

Planned outcome at completion:

A reviewer can open `MainScene`, press Play, drag the joystick, and see directional hero movement with walking animation. A minimal attack behavior can be triggered to play attack animation. Behavior-definition mapping exists in one view-layer registration point. Input and movement are split so input emits intents and movement consumes intents. The project remains analyzer-clean and covered by focused tests for this new visual flow.

## Context and Orientation

The current repository already has base app/view and battle runtime modules. This plan extends those modules with visual controls and lightweight behaviors, and does not attempt full combat integration.

Relevant current files:

- `Assets/Scenes/MainScene.unity`: scene that already contains baseline character and virtual joystick setup to be made functional.
- `Assets/Scripts/App/GameView/Runtime/GameView.cs`: current Unity-facing GameView entry point.
- `Assets/Scripts/Core/Battle/Runtime/Services/GameViewModel.cs`: current app-facing controller used by GameView.
- `Assets/Scripts/Core/Battle/Runtime/Events/BattleEvents.cs`: intent/event shapes (including movement and attack related events).
- `Assets/Scripts/App/Bootstrap/Runtime/BootstrapInfraInstaller.cs`: current DI install composition.
- `Docs/App/GameView.md`: module documentation to update when responsibilities expand.

Terms used here:

Virtual joystick means the existing on-screen control that emits a 2D drag direction. Movement behavior means a Unity-side component that applies transform/character movement and animation updates from direction input. Intent means an explicit action message (for example, move started or move stopped) that sits between raw input and resulting gameplay/view action.

## Plan of Work

Milestone 1 delivers the smallest visual loop. We wire a joystick reader component to publish a normalized direction vector and movement state each frame, then a player movement view behavior consumes that data to move the player in world space and switch walk animation on and off. This milestone must work with existing scene objects and prefabs in `MainScene`, with no dependence on attack logic, status systems, damage systems, or backend/live-ops data.

Milestone 2 introduces a minimal attack view behavior that only triggers attack animation. It does not apply damage, spawn projectiles, or evaluate hit results. A simple public method or event entry point is enough so this behavior can be manually triggered and later integrated with intent flow.

Milestone 3 adds a lightweight behavior mapping bridge in the view/app layer. The player view behavior host receives the player definition (or behavior definition list already available through existing app flow), resolves each definition type through one dictionary-based registry, and attaches the mapped Unity behavior component to the player GameObject. Each component runs independently after configuration. This milestone mirrors existing project patterns and keeps registration in one place.

Milestone 4 adds input-intent separation. Joystick input stops directly controlling transform movement and instead emits movement intents through the existing game/event path. Movement behavior reacts to consumed intent state. This establishes the middleman architecture requested for follow-up milestones while keeping this slice visual-first.

Milestone 5 completes safety and quality. Add or update tests for the new components and key flow transitions, run quality scripts until clean, and update `Docs/App/GameView.md` (and any additional module doc touched) to keep docs aligned with behavior.

## Concrete Steps

Run all commands from repository root: `C:\Users\mtgco\.codex\worktrees\1005\Madbox`.

1. Review and align current scene/prefab references before editing.

    rg -n "GameView|MainScene|Player|Joystick|Animator" Assets/Scenes Assets/Prefabs Assets/Scripts/App Assets/Scripts/Core

2. Implement Milestone 1 components and scene wiring, then run focused tests.

    powershell -NoProfile -ExecutionPolicy Bypass -File ".\.agents\scripts\run-editmode-tests.ps1" -AssemblyNames "Madbox.GameView.Tests"
    powershell -NoProfile -ExecutionPolicy Bypass -File ".\.agents\scripts\run-playmode-tests.ps1" -AssemblyNames "Madbox.Bootstrap.PlayModeTests"

3. Implement Milestone 2 animation-only attack behavior and update tests.

    powershell -NoProfile -ExecutionPolicy Bypass -File ".\.agents\scripts\run-editmode-tests.ps1" -AssemblyNames "Madbox.GameView.Tests"

4. Implement Milestone 3 behavior-definition mapping registry and component host logic; run tests.

    powershell -NoProfile -ExecutionPolicy Bypass -File ".\.agents\scripts\run-editmode-tests.ps1"

5. Implement Milestone 4 intent split integration, then run full quality gate.

    .\.agents\scripts\validate-changes.cmd

6. If quality gate fails, fix all failures and diagnostics, and rerun until clean.

    .\.agents\scripts\validate-changes.cmd

## Validation and Acceptance

Acceptance is complete only when all checks below are true:

1. In `MainScene`, dragging the virtual joystick moves the player in the drag direction.
2. While movement direction magnitude is above threshold, walk animation is active.
3. Releasing joystick (or neutral direction) stops movement and returns animation to idle.
4. A minimal attack behavior can be triggered and visibly plays attack animation.
5. Player behavior mapping can attach at least movement and attack view behaviors through a single dictionary registration location.
6. Input-to-movement path uses intent mediation in the final milestone (raw input no longer directly mutates movement transform).
7. No new modules are created unless explicitly approved when a hard blocker is found.
8. Updated EditMode/PlayMode tests for this flow pass.
9. `.agents/scripts/validate-changes.cmd` passes clean with analyzer diagnostics.

For any bug discovered while implementing this plan, add or update a regression test first, confirm fail-before/fix/pass-after, and keep the milestone quality loop intact.

## Idempotence and Recovery

This plan is incremental and safe to repeat by milestone. If scene wiring breaks during a milestone, revert only the current milestone wiring (without destructive git resets), reapply components in smaller steps, and rerun focused tests before full gate. If intent-split work introduces instability, keep the direct-input path behind a temporary local toggle until parity is proven, then remove the fallback before milestone completion.

## Artifacts and Notes

Expected touched areas during execution:

- `Plans/GameView-Character-Controls/GameView-Character-Controls-ExecPlan.md`
- `Assets/Scripts/App/GameView/Runtime/*`
- `Assets/Scripts/App/GameView/Container/*` (if DI registration updates are needed)
- `Assets/Scripts/App/GameView/Tests/*`
- `Assets/Scenes/MainScene.unity` and related prefab references already present in scene
- `Docs/App/GameView.md`

If additional module docs are touched, keep documentation under `Docs/` and update only modules affected by this slice.

## Interfaces and Dependencies

Expected view/app-side contracts at end of this plan (final names can vary slightly to fit existing analyzer conventions):

- A joystick input adapter in app/view layer that exposes movement direction and active state.
- A player movement view behavior component that consumes movement data (initially direct, later from intent state), moves the player, and updates animator parameters.
- A player attack view behavior component with a minimal trigger method that plays attack animation only.
- A player behavior host/mapper that resolves behavior definition type to Unity component type through one dictionary registration point.
- An intent bridge path that emits movement intents and updates movement state from consumed intents.

Mandatory dependency rules for this work:

- Unity/MonoBehaviour and animation code stays in app/view-side modules.
- Core modules remain Unity-agnostic and should only receive intents/events through existing contracts.
- Do not introduce new `#pragma warning disable` directives without explicit user approval in this thread.

Revision Note (2026-03-19 / Codex): Created initial ExecPlan for phased visual character controls integration: joystick movement and walk animation first, animation-only attack second, behavior mapping third, and input-intent separation last.
Revision Note (2026-03-19 / Codex): Marked Milestones 1 and 2 complete after implementing view-side joystick movement, walk/idle animation switching, animation-only attack behavior, and focused EditMode/PlayMode validation.
