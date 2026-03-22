# Stabilize Joystick Input and Establish Componentized Player View Controls

This ExecPlan is a living document. The sections `Progress`, `Surprises & Discoveries`, `Decision Log`, and `Outcomes & Retrospective` must be kept up to date as work proceeds.

This document is maintained in accordance with `C:\Unity\Madbox\PLANS.md`.

## Purpose / Big Picture

After this change, a player in `MainScene` can touch anywhere to place the virtual joystick exactly at that touch location, drag without the joystick disappearing off screen, and release to reset cleanly. In the same iteration, touch interpretation becomes centralized in one component that can route joystick input and detect swipe gestures for future combat actions. Player controls in the view layer are simplified into a simple state-machine-like behavior loop: a main player script owns an ordered list of behaviors, each behavior decides whether it accepts control for the current frame, and only the first accepted behavior executes. This means movement can take priority over attack naturally, and when movement input stops the next behavior (attack) can take control.

You can see this working by running the scene, touching near all screen edges, and confirming the joystick stays visible and responsive. You can also perform a swipe and observe a deterministic swipe detection signal (logging or event), even though no gameplay action is attached yet.

## Progress

- [x] (2026-03-21 20:42Z) Create this ExecPlan file and align it with `C:\Unity\Madbox\PLANS.md`.
- [x] (2026-03-21 20:42Z) Implement joystick simplification in `Assets/Scripts/App/GameView/Runtime/VirtualJoystickInput.cs` (root follows pointer position using straightforward coordinate conversion).
- [x] (2026-03-21 20:42Z) Add joystick boundary clamping so the stick root cannot move off the visible parent rect.
- [x] (2026-03-21 20:42Z) Add regression test covering out-of-bounds pointer placement and asserting clamped anchored position within parent bounds.
- [x] (2026-03-21) Add `TouchInputRouter` in `Assets/Scripts/App/GameView/Runtime/` to own pointer down/drag/up routing and pointer ownership.
- [x] (2026-03-21) Define and wire minimal `InputContext` (`JoystickDrag` + `PointerEventData`) through an `IInputContextProvider` implementation (`PlayerInputProvider`).
- [x] (2026-03-21) Implement swipe detection in router (distance and duration thresholds) with detect/log behavior only.
- [x] (2026-03-21) Wire `TouchInputRouter` and joystick references in `Assets/Scenes/MainScene.unity` (joystick logic on `Joystick` child; full-screen `Touch Area` receives raycasts; joystick graphic `RaycastTarget` off).
- [x] (2026-03-21) Introduce explicit player view data component (`PlayerViewData`) and `PlayerCore` snapshot used by behaviors.
- [x] (2026-03-21) Add `PlayerBehaviorRunner` with ordered behaviors and first-accept-wins control flow.
- [x] (2026-03-21) Refactor movement into `IPlayerBehavior` (`PlayerMovementViewBehavior`) consuming `InputContext` when a `PlayerInputProvider` exists in the scene; legacy joystick `Update` path retained for tests without a provider.
- [x] (2026-03-21) Wire attack as `PlayerAttackViewBehavior` (`IPlayerBehavior`) ahead of centralized animation; debug Space handled there; `Hero` prefab disables duplicate debug attack on `PlayerAttackAnimationBehavior`.
- [x] (2026-03-21) Animator controller has no parameters; movement/attack remain crossfade-driven with behavior-owned calls (`SetMoving` / `TriggerAttack`). Parameter migration deferred until controller exposes bools/triggers.
- [ ] Run targeted test commands and `.agents/scripts/validate-changes.cmd` until clean (blocked here: Unity project already open in another Editor instance; rerun locally after closing the other instance).
- [ ] Commit milestone changes.

## Surprises & Discoveries

- Observation: The current joystick script already receives `PointerEventData` through `IPointerDownHandler`, `IDragHandler`, and `IPointerUpHandler`, so a separate low-level touch polling loop is not needed for this scope.
  Evidence: Existing implementation location `Assets/Scripts/App/GameView/Runtime/VirtualJoystickInput.cs`.

- Observation: Existing test coverage already includes joystick reposition behavior, which can be extended into a true regression test for the disappearing bug.
  Evidence: Existing test file `Assets/Scripts/App/GameView/Tests/CharacterVisualBehaviorsTests.cs`.

- Observation: Current animation control in player view is predominantly state-crossfade oriented and not parameter-first; this creates avoidable coupling between movement script and animation script.
  Evidence: Current runtime scripts in `Assets/Scripts/App/GameView/Runtime/` use movement/attack animation helpers that can be decomposed.

- Observation: Full validation gate is currently not clean due unrelated baseline failures outside milestone scope (PlayMode addressables bootstrap timeout and many analyzer diagnostics in other modules).
  Evidence: `.agents/scripts/validate-changes.cmd` run on 2026-03-21 completed with failing PlayMode test `Madbox.Addressables.Tests.PlayMode.AddressablesBootstrapPlayModeTests.BootstrapScene_ResolvesGateway_LoadsAndReleasesAddressable` and analyzer diagnostics in `Assets/Scripts/Core/*` and `Assets/Scripts/Infra/Scope/*`.

## Decision Log

- Decision: Keep this iteration strictly in the view/input layer; do not refactor `Core/Battle` now.
  Rationale: User explicitly requested view-first scope and fast progress on joystick/touch/player control behavior.
  Date/Author: 2026-03-21 / Codex

- Decision: Implement swipe as detect/log only.
  Rationale: User selected swipe detection without gameplay binding for this first pass; this keeps risk low and preserves forward compatibility.
  Date/Author: 2026-03-21 / Codex

- Decision: Use a top-level touch router component to decide routing to joystick vs gesture recognition.
  Rationale: Centralized touch ownership prevents duplicated touch logic and enables future actions (dash, charged shot, etc.) without rewriting joystick internals.
  Date/Author: 2026-03-21 / Codex

- Decision: Move toward Animator parameter/flag control owned by behavior components.
  Rationale: It removes unnecessary animation centralization and matches the intended architecture where each behavior sets only what it owns.
  Date/Author: 2026-03-21 / Codex

- Decision: Use an ordered behavior loop with first-accept-wins semantics in the player view layer.
  Rationale: This provides a simple, explicit state-machine-like system where movement preempts attack when joystick input exists, and attack becomes active only when movement does not accept control.
  Date/Author: 2026-03-21 / Codex

## Outcomes & Retrospective

Milestone 1 is implemented at code level: joystick root positioning is now clamped to parent bounds, and a regression test for out-of-bounds pointer placement has been added in `CharacterVisualBehaviorsTests`.

Milestone 2 is implemented: `TouchInputRouter` + `PlayerInputProvider` are wired in `MainScene`; `VirtualJoystickInput` lives on the `Joystick` root; `Hero` prefab includes `PlayerViewData`, `PlayerCore`, `PlayerBehaviorRunner`, movement + attack behaviors; `PlayerBehaviorRunnerTests` covers runner-driven movement with cleared vs non-cleared drag.

Remaining gap before milestone sign-off: run `.agents/scripts/validate-changes.cmd` (and fix any new regressions) when no other Unity instance holds the project lock, then commit.

## Context and Orientation

This repository contains a Unity project with gameplay view scripts under `Assets/Scripts/App/GameView/Runtime/` and scene wiring in `Assets/Scenes/MainScene.unity`.

The joystick is a virtual on-screen control. In plain language, the joystick root is the outer control image that should appear where the player touches, and the inner stick is the smaller image that moves inside the root to represent direction. A bug currently reported by the team is that joystick placement is sometimes offset from the touch point and can disappear outside visible bounds.

Relevant files for this task:

- `Assets/Scripts/App/GameView/Runtime/VirtualJoystickInput.cs`: current joystick pointer handling and direction output.
- `Assets/Scripts/App/GameView/Runtime/PlayerMovementViewBehavior.cs`: movement behavior that consumes joystick direction.
- `Assets/Scripts/App/GameView/Runtime/PlayerAttackAnimationBehavior.cs`: attack animation behavior to be decomposed/aligned with parameter-driven model.
- `Assets/Scripts/App/GameView/Tests/CharacterVisualBehaviorsTests.cs`: current test suite with joystick-related tests.
- `Assets/Scenes/MainScene.unity`: references and scene object wiring for touch area, joystick visuals, and player behavior scripts.

Term definitions used in this plan:

- Pointer event: Unity UI event data (`PointerEventData`) containing the touch/mouse screen position and pointer identity.
- Touch router: one component that receives pointer events and decides whether an interaction should control joystick movement or be interpreted as a gesture (such as swipe).
- Swipe: a quick directional finger movement recognized by minimum travel distance and maximum allowed time.
- Animator parameters/flags: named values on Unity `Animator` (booleans, floats, triggers) used by the Animator Controller to transition animations.
- Behavior runner: the main player view script that iterates through an ordered list of behavior components each frame.
- Accept control: a behavior returns true when it wants to own the current frame and execute; when true, later behaviors are skipped for that frame.

## Plan of Work

Begin by editing `Assets/Scripts/App/GameView/Runtime/VirtualJoystickInput.cs` to simplify position logic. The joystick root position must come directly from the pointer event position after conversion into the joystick parent rect coordinate system. Keep this conversion in one clear path used by pointer down and drag updates. Add explicit clamping against parent bounds so the root never leaves the visible area. Keep inner-stick direction logic as a simple radius-clamped vector with dead zone and reset on pointer up.

Next, create `Assets/Scripts/App/GameView/Runtime/TouchInputRouter.cs` as the new high-level touch owner. This component receives pointer down/drag/up events, tracks the active pointer ID for joystick ownership, and forwards joystick-relevant updates to the joystick input component. In parallel, it tracks touch start position/time and determines if movement qualifies as a swipe. For this milestone, swipe output is detect/log only. No combat action dispatch is attached yet.

After routing exists, introduce explicit player view data in a new component in `Assets/Scripts/App/GameView/Runtime/` (name fixed during implementation, for example `PlayerViewData.cs`) with hand-authored fields. Add a new main player script (for example `PlayerBehaviorRunner.cs`) that contains an ordered serialized list of behavior components and runs a first-accept-wins loop each frame. Define a small behavior interface (for example `IPlayerBehavior`) with methods equivalent to "accept control?" and "execute". Refactor movement logic into a behavior that reads joystick direction; when movement input is non-zero it accepts control, moves the player, updates movement-related Animator parameters, and blocks later behaviors. Refactor attack logic into the next behavior in the list; when movement does not accept control, attack can accept and execute (for this milestone, prepared and parameter-driven, without binding swipe to gameplay action yet).

Then update scene wiring in `Assets/Scenes/MainScene.unity` so `TouchInputRouter` is attached to the touch surface, references the joystick component, and player object references the new view data + behavior components. Remove obsolete or duplicate responsibilities from old scripts as needed, but keep changes additive until replacement is verified.

Finally, add and run regression tests for the joystick placement/disappearing bug and verify the full validation gate. If any gate fails, fix and rerun until clean.

## Concrete Steps

All commands below are run from repository root:

    Working directory:
    C:\Users\mtgco\.cursor\worktrees\Madbox\dvl

1) Inspect current files and references:

    rg "class VirtualJoystickInput|OnPointerDown|OnDrag|OnPointerUp" Assets/Scripts/App/GameView/Runtime
    rg "PlayerMovementViewBehavior|PlayerAttackAnimationBehavior|Animator" Assets/Scripts/App/GameView/Runtime
    rg "VirtualJoystickInput|Touch Area|Joystick|InnerStick" Assets/Scenes/MainScene.unity

Expected outcome: existing joystick and movement/animation touchpoints are confirmed before edits.

2) Implement joystick simplification and bounds clamp in `VirtualJoystickInput.cs`.

Expected outcome: pointer down positions root at touch; drag updates direction with stable radius/dead-zone behavior; root cannot leave visible area.

3) Add regression test in `Assets/Scripts/App/GameView/Tests/CharacterVisualBehaviorsTests.cs` (or a new adjacent test file if clarity requires) that:

    - Reproduces joystick disappearing or invalid out-of-bounds position before the fix.
    - Passes after the fix by asserting clamped anchored position within parent bounds.

4) Add `TouchInputRouter.cs` and wire it in `MainScene.unity`.

Expected outcome: router owns pointer stream, forwards joystick updates, and logs swipe detection events when thresholds are met.

5) Add/refactor player data + behavior-runner + movement/attack behavior components in `Assets/Scripts/App/GameView/Runtime/` and update scene references.

Expected outcome: movement and attack scripts consume explicit player data, are executed through ordered first-accept-wins control flow, and set Animator parameters directly.

6) Run tests and validation gate:

    .agents/scripts/validate-changes.cmd

If this gate reports failures, fix them and rerun the same command until it completes cleanly.

7) Run focused tests (exact command may depend on project test runner setup; use the repository-standard command and include the joystick regression test in the run).

Expected outcome: new regression test passes; no related test regressions.

8) Commit milestone changes once gate and tests are clean.

## Validation and Acceptance

A change is accepted only when all behavior below is observed:

- Joystick placement correctness: touching in `MainScene` causes joystick root to appear at the corresponding touch location.
- Joystick visibility safety: touching and dragging near each edge and corner never makes joystick root disappear off screen.
- Reset behavior: releasing touch resets direction and inner-stick position.
- Touch routing correctness: one pointer controls joystick, and swipe detection is emitted/logged when gesture thresholds are met.
- Player behavior architecture: main player runner loops through ordered behaviors; first behavior that accepts control executes and stops the loop for that frame.
- Movement priority behavior: movement behavior accepts when joystick input exists and attack does not run in the same frame.
- Idle-to-attack behavior: when movement input is zero, movement declines control and attack behavior is eligible to execute.
- Animator ownership model: movement/attack scripts set Animator parameters/flags directly and let Animator transitions handle animation flow.
- Regression safety: the joystick bug has a regression test that is documented as failing pre-fix and passing post-fix.
- Quality gate: `.agents/scripts/validate-changes.cmd` completes cleanly.

Manual verification scenario:

1. Open `MainScene`.
2. Enter Play Mode.
3. Touch center, left edge, right edge, top edge, and bottom edge; confirm joystick remains visible and aligned.
4. Drag in circles and quick flicks; confirm smooth directional response.
5. Perform a quick swipe outside joystick movement intent area; confirm swipe detect/log output appears.
6. Observe movement and attack animation transitions respond to Animator params/flags from behavior components.

## Idempotence and Recovery

This plan is safe to run repeatedly because changes are additive and test-driven. Re-running scene wiring and script edits should only overwrite intended behavior when done in the same files described above.

If a step fails:

- For compile errors after a refactor, revert only the in-progress file edit and re-apply smaller scoped edits.
- For scene reference breakage in `MainScene.unity`, re-open the scene and reattach components using the explicit file list in this plan.
- For failing regression tests, first verify test setup reproduces pre-fix bug condition, then verify clamping and coordinate conversion assumptions in joystick script.
- For validation gate failures, treat each report as blocking and rerun `.agents/scripts/validate-changes.cmd` until clean.

No destructive migration is included in this ExecPlan.

## Artifacts and Notes

During implementation, keep short evidence snippets here (indented) to prove behavior and gate status.

Planned artifact examples to capture:

    - Before fix: regression test failure assertion showing out-of-bounds joystick position.
    - After fix: same test passes with clamped bounds assertion.
    - Behavior loop evidence: short log/test evidence that movement acceptance prevents attack execution in the same frame.
    - Behavior loop evidence: short log/test evidence that attack can execute when movement input is zero.
    - Validation gate pass transcript from `.agents/scripts/validate-changes.cmd`.
    - Optional log snippet proving swipe detection output.

## Interfaces and Dependencies

Use Unity UI pointer interfaces and existing GameView runtime conventions.

Required interfaces and responsibilities at end of milestone:

- In `Assets/Scripts/App/GameView/Runtime/VirtualJoystickInput.cs`:
  - Continue exposing joystick direction as a stable read value for movement consumers.
  - Handle pointer-driven root positioning via direct pointer position conversion and bounds clamping.

- In `Assets/Scripts/App/GameView/Runtime/TouchInputRouter.cs` (new):
  - Implement pointer event handling for down/drag/up.
  - Track active pointer ownership for joystick routing.
  - Detect swipe with configurable threshold fields and emit/log detection.
  - Forward joystick-intended interactions to `VirtualJoystickInput`.

- In player behavior files under `Assets/Scripts/App/GameView/Runtime/`:
  - A shared player view data component with explicit hand-authored fields.
  - A behavior contract (interface or abstract base) that supports accept-and-execute semantics.
  - A main runner component that stores ordered behavior references and applies first-accept-wins flow each frame.
  - Separate movement and attack behavior components that consume shared data and set Animator parameters/flags directly.
  - Ordering rule in scene setup: movement behavior appears before attack behavior in the runner list.

- In `Assets/Scenes/MainScene.unity`:
  - Serialized references for router, joystick, and player components are valid and complete.

Dependencies:

- Existing Unity `EventSystem` and UI event dispatch pipeline in scene.
- Existing GameView runtime scripts and tests.
- Existing repository validation script `.agents/scripts/validate-changes.cmd`.

## API Contract and Boundaries (Implementation Snippets)

This section defines the exact boundaries to implement, so behaviors stay decoupled and input does not leak into persistent player state.

`PlayerState` is persistent game data owned by the player core component (health, movement speed, cooldown values, weapon state, alive/dead flags). `InputContext` is frame-scoped and minimal for this milestone: it carries only joystick drag direction and the current `PointerEventData`. It intentionally avoids gameplay command flags and additional event-dump complexity for now.

The behavior loop must be first-accept-wins. If no behavior accepts control in a frame, the runner must explicitly exit the currently active behavior and leave no active owner for that frame.

Indented reference snippet for minimal input context:

    using UnityEngine;
    using UnityEngine.EventSystems;

    public struct InputContext
    {
        public Vector2 JoystickDrag;                 // normalized movement intent from joystick drag
        public PointerEventData PointerEventData;    // latest pointer payload from UI event system

        public bool HasJoystickInput => JoystickDrag.sqrMagnitude > 0.0001f;
    }

    public interface IInputContextProvider
    {
        InputContext Current { get; }
        void EndFrame(); // optional no-op in this minimal phase
    }

Indented reference snippet for writing data into the input provider:

    using UnityEngine;
    using UnityEngine.EventSystems;

    public interface IInputContextProvider
    {
        InputContext Current { get; }
        void SetJoystickDrag(Vector2 drag, PointerEventData eventData);
        void ClearJoystickDrag();
        void EndFrame();
    }

    public sealed class PlayerInputProvider : MonoBehaviour, IInputContextProvider
    {
        private InputContext current;

        public InputContext Current => current;

        public void SetJoystickDrag(Vector2 drag, PointerEventData eventData)
        {
            current.JoystickDrag = Vector2.ClampMagnitude(drag, 1f);
            current.PointerEventData = eventData;
        }

        public void ClearJoystickDrag()
        {
            current.JoystickDrag = Vector2.zero;
        }

        public void EndFrame()
        {
            // no-op for this milestone
        }
    }

Indented reference snippet for router/joystick integration:

    using UnityEngine;
    using UnityEngine.EventSystems;

    public sealed class TouchInputRouter : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [SerializeField] private PlayerInputProvider inputProvider;
        [SerializeField] private VirtualJoystickInput joystick;

        public void OnPointerDown(PointerEventData eventData)
        {
            joystick.OnPointerDown(eventData);
            inputProvider.SetJoystickDrag(joystick.Direction, eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            joystick.OnDrag(eventData);
            inputProvider.SetJoystickDrag(joystick.Direction, eventData);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            joystick.OnPointerUp(eventData);
            inputProvider.ClearJoystickDrag();
        }
    }

Indented reference snippet for behavior contract:

    public interface IPlayerBehavior
    {
        bool CanTakeControl(PlayerState state, in InputContext input);
        void OnEnterControl(PlayerState state, in InputContext input);
        void Tick(PlayerState state, in InputContext input, float deltaTime);
        void OnExitControl(PlayerState state, in InputContext input);
    }

Indented reference snippet for runner (explicit exit when no winner):

    using UnityEngine;
    using System.Collections.Generic;

    public sealed class PlayerBehaviorRunner : MonoBehaviour
    {
        [SerializeField] private PlayerCore playerCore;
        [SerializeField] private MonoBehaviour inputProviderComponent; // IInputContextProvider
        [SerializeField] private List<MonoBehaviour> behaviorComponents; // IPlayerBehavior, ordered

        private IInputContextProvider inputProvider;
        private readonly List<IPlayerBehavior> behaviors = new();
        private IPlayerBehavior activeBehavior;

        private void Awake()
        {
            inputProvider = (IInputContextProvider)inputProviderComponent;
            foreach (var component in behaviorComponents)
            {
                behaviors.Add((IPlayerBehavior)component);
            }
        }

        private void Update()
        {
            var state = playerCore.State;
            var input = inputProvider.Current;

            IPlayerBehavior winner = null;
            foreach (var behavior in behaviors)
            {
                if (behavior.CanTakeControl(state, in input))
                {
                    winner = behavior;
                    break;
                }
            }

            if (winner != activeBehavior)
            {
                activeBehavior?.OnExitControl(state, in input);
                winner?.OnEnterControl(state, in input);
                activeBehavior = winner;
            }

            if (activeBehavior == null)
            {
                inputProvider.EndFrame();
                return;
            }

            activeBehavior.Tick(state, in input, Time.deltaTime);
            inputProvider.EndFrame();
        }
    }

Indented reference snippet for one behavior (movement):

    using UnityEngine;

    public sealed class MoveBehavior : MonoBehaviour, IPlayerBehavior
    {
        [SerializeField] private Transform actorRoot;
        [SerializeField] private Animator animator;
        [SerializeField] private string isMovingParam = "IsMoving";
        [SerializeField] private float movementDeadZone = 0.1f;

        public bool CanTakeControl(PlayerState state, in InputContext input)
        {
            return state.IsAlive && state.CanMove && input.JoystickDrag.magnitude > movementDeadZone;
        }

        public void OnEnterControl(PlayerState state, in InputContext input)
        {
            animator.SetBool(isMovingParam, true);
        }

        public void Tick(PlayerState state, in InputContext input, float deltaTime)
        {
            Vector3 move = new Vector3(input.JoystickDrag.x, 0f, input.JoystickDrag.y);
            actorRoot.position += move * state.MoveSpeed * deltaTime;

            if (move.sqrMagnitude > 0.0001f)
            {
                actorRoot.forward = move.normalized;
            }
        }

        public void OnExitControl(PlayerState state, in InputContext input)
        {
            animator.SetBool(isMovingParam, false);
        }
    }

Required player object MonoBehaviour components for this milestone:

- `PlayerCore` (owns persistent `PlayerState`).
- `PlayerBehaviorRunner` (ordered first-accept-wins arbiter).
- `TouchInputRouter` or equivalent `IInputContextProvider` implementation (produces abstract `InputContext`).
- `MoveBehavior` (priority 1).
- `AttackBehavior` (priority 2).
- Unity `Animator` component.

Recommended runner order for current goals: `MoveBehavior` first, `AttackBehavior` second. This enforces: movement input preempts attack, and attack can accept only when movement declines.

---

Revision Note (2026-03-21): Created this initial PLANS.md-compliant ExecPlan to replace prior chat-only planning format. Reason: repository requires ExecPlans to follow `C:\Unity\Madbox\PLANS.md` structure and file-location conventions so execution can proceed from a self-contained document.
Revision Note (2026-03-21): Clarified player architecture as a simple state-machine-like ordered behavior loop with first-accept-wins semantics (movement preempts attack while input exists). Reason: align implementation contract with user-requested control flow and remove ambiguity in behavior ownership per frame.
Revision Note (2026-03-21): Appended explicit API and boundary snippets for `InputContext`, behavior contract, runner semantics, and required player MonoBehaviours. Updated input model to remain abstract interaction data (clicks, positions, directions, pointer counts) with no gameplay-specific flags. Reason: lock implementation to clean boundaries and future extensibility.
Revision Note (2026-03-21): Simplified `InputContext` for immediate scope to include only joystick drag vector plus latest `PointerEventData`. Reason: implement only what is required now and avoid premature generalization.
Revision Note (2026-03-21): Added concrete `IInputContextProvider` population snippets and router integration example showing how joystick drag and `PointerEventData` are written each pointer event. Reason: remove ambiguity about where and how input data enters the behavior loop.
