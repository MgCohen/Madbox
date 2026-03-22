# Game View

## TL;DR

- Purpose: Unity-facing player and combat presentation helpers (animation event routing, simple player behaviors, debug projectile spawn).
- Location: `Assets/Scripts/App/GameView/Runtime/` (`Madbox.GameView` assembly), tests in `Assets/Scripts/App/GameView/Tests/`.
- Depends on: Unity engine only (no Core assembly references).
- Used by: Hero and enemy prefabs; optional scene wiring for joystick via `VirtualJoystickInput` / `PlayerInputProvider`.
- Keywords: animation events, animator speed multiplier, player behavior runner.

## Responsibilities

- Owns `CharacterAnimationEventRouter` and `AnimationEventDefinition` ScriptableObjects for stable clip event ids.
- Owns lightweight player view data, behavior runner, movement/attack view behaviors, and crossfade-based `PlayerAnimationController`.
- Owns optional `CombatAnimationEventResponse` for spawning a visual projectile when a clip event fires (not authoritative battle logic).
- Does not own domain simulation, damage resolution, or networking.
- Does not reference `Assets/Scripts/Core/*` assemblies (keeps Unity presentation out of core).

## Public API

| Symbol | Purpose | Inputs | Outputs | Failure / edge behavior |
|--------|---------|--------|---------|-------------------------|
| `AnimationEventDefinition` | SO wrapping stable `int` id for clip payloads | Asset authoring | `StableId`, display metadata | `StableId` 0 is invalid for registration |
| `CharacterAnimationEventRouter` | Single Unity animation callback → multicast handlers | Clip calls `OnCharacterAnimationEvent` | Invokes registered delegates | Unknown id logs in dev/editor builds; no throw |
| `CharacterAnimationEventContext` | Payload for handlers | Built by router | `SourceAnimator`, raw `AnimationEvent` | n/a |
| `PlayerViewData` | Serialized move/attack stats on hero root | Inspector | `AttackSpeedStat` drives animator float | `AttackSpeedStat` clamps to minimum 0.05 |
| `PlayerAnimationController` | Locomotion crossfade + attack trigger; sets `AttackSpeedMultiplier` float | `SetLocomotionMoving`, `TriggerAttack` | Animator state changes | Skips locomotion while attack lock active |
| `PlayerBehaviorRunner` | Ordered `IPlayerBehavior` first-accept-wins | `Update` | Runs one behavior per frame | No-op if `PlayerCore` missing |
| `CombatAnimationEventResponse` | Registers on `OnEnable` for one `AnimationEventDefinition` | Release event + prefab | `Instantiate` projectile | Logs if prefab missing |
| `AttackSpeedMultiplierDriver` | Sets `AttackSpeedMultiplier` for non-player animators | `multiplier` field | Animator parameter | No-op if no `Animator` |
| `SimpleProjectile` | Forward motion + timed destroy | Start/Update | Moves along forward | n/a |

## Setup / Integration

1. Add `Madbox.GameView` reference to consuming assemblies if needed (prefabs only do not require code references).
2. On the **same GameObject as the `Animator`**, add `CharacterAnimationEventRouter`.
3. Create `AnimationEventDefinition` assets under `Assets/Data/AnimationEvents/`; set `stableId` and use that value as the clip event **Int** parameter.
4. In each clip (Animation window), add an event: **Function** = `OnCharacterAnimationEvent`, **Int** = definition `stableId`.
5. For attack speed scaling: add float parameter `AttackSpeedMultiplier` (default 1) on the Animator Controller; enable **Speed Parameter** on attack states only, set to `AttackSpeedMultiplier`. `PlayerAnimationController` sets it from `PlayerViewData.AttackSpeedStat`; enemies can use `AttackSpeedMultiplierDriver`.
6. Optional: add `CombatAnimationEventResponse` next to the router, assign release definition, projectile prefab, and spawn origin transform.

Common mistakes: placing `CharacterAnimationEventRouter` on the player root while the `Animator` lives on a child (Unity will not find the callback). Mismatch between clip int and SO `stableId`.

## How to Use

1. **Author an event id**: Create `AnimationEventDefinition`, note `stableId` (e.g. 1001).
2. **Tag the clip**: Add animation event at the desired time with function `OnCharacterAnimationEvent` and int 1001.
3. **React in code**: From a `MonoBehaviour` on the character, `GetComponent<CharacterAnimationEventRouter>()` and `Register(definition, ctx => { ... })`; unregister in `OnDisable`.
4. **Or use the prefab helper**: Assign `CombatAnimationEventResponse` fields for a quick projectile spawn demo.
5. **Tune attack speed**: Change `PlayerViewData` → `Attack Speed Stat` on the hero; idle/run states should keep speed parameter inactive so only attack accelerates.

## Examples

### Minimal handler registration

```csharp
void OnEnable()
{
    router.Register(releaseDefinition, OnRelease);
}

void OnDisable()
{
    router.Unregister(releaseDefinition, OnRelease);
}

void OnRelease(CharacterAnimationEventContext ctx)
{
    // Forward to a facade / intent; do not mutate core from here without a bridge.
}
```

### Realistic clip row (conceptual)

- Time: 0.25s into attack clip  
- Function: `OnCharacterAnimationEvent`  
- Int: `1001` (matches `PlayerRangedAttack_Release` asset)

### Guard example

- Int `0` on a clip → router logs a warning and does not dispatch.  
- No handler for id → router logs in dev/editor; safe no-op.

## Best Practices

- Keep one callback method name on clips (`OnCharacterAnimationEvent`) to avoid string sprawl in C#.
- Prefer SO assets over duplicating raw integers in comments only; the asset is the shared contract.
- Unregister handlers when behaviours disable to avoid leaks on pooled characters.
- Use `AttackSpeedMultiplier` only on attack states, not global `Animator.speed`.
- When connecting to battle core, introduce a narrow app-layer facade; do not reference Core from this assembly.

## Anti-Patterns

- Putting gameplay authority (damage, cooldown commit) directly inside animation callbacks without a defined ingress.
- Using `Animator.speed` for attack speed when locomotion must stay unchanged.
- Hard-coding many different `public void Foo()` methods on clips instead of routing through one entry point.

## Testing

- Assembly: `Madbox.GameView.Tests` (EditMode).
- From repository root (PowerShell):

```powershell
& ".\.agents\scripts\run-editmode-tests.ps1" -AssemblyNames "Madbox.GameView.Tests"
```

- Expect: all tests pass; router tests cover invoke, unknown id, zero int, and multicast.

## AI Agent Context

- Invariants: router lives on Animator object; `stableId` must be non-zero; handlers multicast per id.
- Allowed: Unity types only in this module; prefab YAML edits for wiring.
- Forbidden: new Core assembly references from `Madbox.GameView`.
- Change checklist: update clip int if SO id changes; revalidate Hero/Bee prefabs; run GameView tests.

## Related

- `Architecture.md`
- `Plans/AnimationEventsAndSpeed/AnimationEventsAndSpeed-ExecPlan.md`
- `Docs/Core/Battle.md`

## Changelog

- 2026-03-22: Initial module doc for animation event routing, player view behaviors, and attack speed multiplier.
