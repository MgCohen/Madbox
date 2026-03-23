# Game View

## TL;DR

- Purpose: Unity-facing player and combat presentation helpers (animation event routing, simple player behaviors, debug projectile spawn, weapon socket and visibility via `WeaponVisualController`), plus level-scene **`Arena`** markers for spawn positions and optional play bounds.
- Location: `Assets/Scripts/App/GameView/Runtime/` (`Madbox.GameView` assembly), tests in `Assets/Scripts/App/GameView/Tests/`.
- Depends on: Unity engine only. Loadout authoring asset `PlayerLoadoutDefinition` lives in **`Madbox.Levels`**; **`PlayerService`** and **`PlayerFactory`** live in **`Madbox.Bootstrap.Runtime`** and reference this assembly for `WeaponVisualController`. Preload is via **`PlayerLoadoutAssetProvider`** on the asset layer (see `BootstrapAssetInstaller`).
- Used by: Hero and enemy prefabs; optional scene wiring for joystick via `VirtualJoystickInput` / `PlayerInputProvider`; bootstrap registers `PlayerService` and `PlayerFactory` (see `BootstrapCoreInstaller`). `PlayerLoadoutDefinition` is registered into the layer scope after asset preload so `PlayerService` receives it by constructor injection.
- Keywords: animation events, animator speed multiplier, player behavior runner, weapon loadout, `WeaponVisualController`, `Arena`, `PlayerAttribute` (see **`Docs/App/PlayerAttributes.md`**).

## Responsibilities

- Uses `AnimationEventRouter` and optional `AnimationEventDefinition` ScriptableObjects for clip event ids (string `EventId`); see `Docs/App/Animation.md`.
- Owns lightweight player view data, behavior runner, movement/attack view behaviors, and generic `AnimationController` (cross-fade by state name, bool/float parameters).
- Owns `WeaponVisualController` (serialized list of socket transforms, spawned weapon instances, visible slot via `GameObject.SetActive`). Authoring asset `PlayerLoadoutDefinition` is in **`Madbox.Levels`**; `PlayerService` and `PlayerFactory` are in **`Madbox.Bootstrap.Runtime`**.
- Owns `PlayerAttackViewBehavior` for range-based attack targeting and animator bools (not authoritative battle logic).
- Owns `Arena` for level scenes: optional `BoxCollider` world bounds, optional enemy/player spawn `Transform`s, and `TryFindInScene` / `TryFindInLoadedScenes` helpers for code that runs after additive scene load.
- Does not own domain simulation, damage resolution, or networking.
- Does not reference `Assets/Scripts/Core/*` assemblies (keeps Unity presentation out of core).

## Public API

| Symbol | Purpose | Inputs | Outputs | Failure / edge behavior |
|--------|---------|--------|---------|-------------------------|
| `AnimationEventDefinition` | SO marker with string `EventId` for clip payloads | Asset authoring | `EventId` | Empty `EventId` is invalid for registration |
| `AnimationEventRouter` | Single Unity animation callback → multicast handlers | Clip calls `OnCharacterAnimationEvent` with Object ref to `AnimationEventDefinition` | Invokes `Action<AnimationEventContext>` | Missing / unregistered definition logs in dev/editor builds; no throw |
| `AnimationController` | `Play` / `GetBool` / `SetBool` / `SetFloat` on an `Animator` (string or `AnimationAttribute`) | View behaviours | Cross-fade, parameters | `Play` uses state hash internally |
| `PlayerData` | `IsAlive` / `CanMove` via `PlayerAttribute` plus `attributeEntries` | Inspector | `GetFloatAttribute` / `SetFloatAttribute`, `IsAlive` / `CanMove` | Missing attribute entry logs in dev/editor |
| `PlayerAttribute` | ScriptableObject id for a stat | Asset | `AttributeName` | n/a |
| `AnimationAttribute` | ScriptableObject id for an animator parameter name | Asset | `ParameterName` | n/a |
| `PlayerAttributeAnimatorDriver` | Maps `PlayerAttribute` → `AnimationAttribute` | `PlayerData.AttributeValueChanged` | `AnimationController` parameters | No-op if link or controller missing |
| `PlayerBehaviorRunner` | Ordered `IPlayerBehavior` first-accept-wins | `Update` | Runs one behavior per frame | No-op if `PlayerData` missing |
| `Projectile` | Optional forward motion, `ScheduleDestroyAfterSeconds`, trigger impact self-destruct; `GetComponent<Enemy>` + damage TODO | Start/Update/trigger | Moves along forward when enabled | Trigger collider + Rigidbody; targets expose `Enemy`; use **Projectile** layer |
| `WeaponVisualController` | List of socket `Transform`s and matching weapon roots; selection by index | `SetWeaponInstances`, `SetSelectedWeaponIndex` | `SelectedWeaponIndex` | Throws if socket/instance counts mismatch or instances not set |
| `Arena` | Level-scene marker for bounds and spawns | Inspector: optional box collider, spawn transforms | `TryGetWorldBounds`, spawn world positions, `TryFindInScene` | Bounds false when no box collider; spawns fall back to arena transform |

## Setup / Integration

1. Add `Madbox.GameView` reference to consuming assemblies if needed (prefabs only do not require code references).
2. **Arena (level scenes)**: Add `Arena` to a root or child in each Addressable level scene. Optionally assign a `BoxCollider` on the same object (or drag one into **Area Bounds Source**) for `TryGetWorldBounds`. Set **Enemy Spawn Point** / **Player Spawn Point** for explicit spawn transforms; otherwise the arena transform position is used. After `SceneManager` / Addressables loads the level scene, call `Arena.TryFindInScene(loadedScene, out var arena)` to read positions and bounds.
3. **Weapon loadout**: Create a **`PlayerLoadoutDefinition`** (see **`Docs/Core/Levels.md`**, menu **Create > Madbox > Levels > Player Loadout**). Assign Addressables for the player prefab (must include `WeaponVisualController` with the same number of sockets as weapon entries) and a list of weapon prefabs. Register the definition asset under the address **`Player Loadout`** (see `PlayerLoadoutAssetProvider`) so bootstrap preload registers it and **`PlayerService`** receives it in its constructor. Call `PlayerFactory.CreateReadyPlayerAsync` from **`Madbox.App.Bootstrap.Player`** (scoped in bootstrap). Switch visible weapon with `WeaponVisualController.SetSelectedWeaponIndex` (weapons stay instantiated; inactive slots are disabled).
4. On the **same GameObject as the `Animator`**, add `AnimationEventRouter`.
5. Create `AnimationEventDefinition` assets under `Assets/Data/AnimationEvents/`; set `EventId` (or rely on asset name) and use that same string as the clip event **String** parameter.
6. In each clip (Animation window), add an event: **Function** = `OnCharacterAnimationEvent`, **Object** = the `AnimationEventDefinition` asset (not the string field).
7. For attack speed scaling: add float parameter `AttackSpeedMultiplier` (default 1) on the Animator Controller; enable **Speed Parameter** on attack states only, set to `AttackSpeedMultiplier`. Create an `AnimationAttribute` asset with that parameter name. On the player, add a `PlayerAttribute` for attack speed, list it on `PlayerData`, and add `PlayerAttributeAnimatorDriver` with a link from that attribute to the `AttackSpeedMultiplier` `AnimationAttribute`. For non-player animators, set the float on `AnimationController` manually or via a small view script.
8. Clip events: call `AnimationEventRouter.OnCharacterAnimationEvent` with the **object** parameter set to the `AnimationEventDefinition` asset. Register handlers with `Register(definition, handler)` (see examples below).

Common mistakes: placing `AnimationEventRouter` on the player root while the `Animator` lives on a child (Unity will not find the callback). Wrong or missing Object reference on the clip event.

## How to Use

1. **Author an event id**: Create `AnimationEventDefinition`, note `EventId` (e.g. `attack_release`).
2. **Tag the clip**: Add animation event at the desired time with function `OnCharacterAnimationEvent` and Object set to the matching `AnimationEventDefinition` asset.
3. **React in code**: From a `MonoBehaviour` on the character, `GetComponent<AnimationEventRouter>()` and `Register(definition, ctx => { ... })` where `ctx` is `AnimationEventContext`; unregister in `OnDisable`.
4. **Tune attack speed**: Change `PlayerData` attribute entries (e.g. attack speed stat) on the hero; idle/run states should keep speed parameter inactive so only attack accelerates.

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

void OnRelease(AnimationEventContext context)
{
    Animator source = context.Animator;
    // Forward to a facade / intent; do not mutate core from here without a bridge.
}
```

### Realistic clip row (conceptual)

- Time: 0.25s into attack clip  
- Function: `OnCharacterAnimationEvent`  
- Object: `PlayerRangedAttack_Release` asset (same as registration)

### Guard example

- Missing object reference on a clip → router logs a warning and does not dispatch.  
- No handler for that definition → router logs in dev/editor; safe no-op.

## Best Practices

- Keep one callback method name on clips (`OnCharacterAnimationEvent`) and pass the definition asset on the Object field.
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

- Expect: all tests pass; `Madbox.Animation.Tests` covers the router (invoke, unknown id, empty string, multicast).

## AI Agent Context

- Invariants: router lives on Animator object; `AnimationEventDefinition.EventId` must be non-empty for registration; clip events reference the same asset instance you register; handlers multicast per definition.
- Allowed: Unity types only in this module; prefab YAML edits for wiring.
- Forbidden: new Core assembly references from `Madbox.GameView`.
- Change checklist: update clip Object reference if the `AnimationEventDefinition` asset changes; revalidate Hero/Bee prefabs; run `Madbox.Animation.Tests`.

## Related

- `Architecture.md`
- `Plans/AnimationEventsAndSpeed/AnimationEventsAndSpeed-ExecPlan.md`
- `Docs/Core/Battle.md`

## Changelog

- 2026-03-23: Documented `WeaponVisualController` and pointed loadout authoring (`PlayerLoadoutDefinition` in `Madbox.Levels`) + `PlayerService` / `PlayerFactory` in bootstrap to keep `Madbox.GameView` free of Addressables references.
- 2026-03-22: Initial module doc for animation event routing, player view behaviors, and attack speed multiplier.
