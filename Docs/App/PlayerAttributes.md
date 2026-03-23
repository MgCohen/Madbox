# Player attributes (GameView)

## Purpose

Named numeric stats use `PlayerAttribute` ScriptableObject assets (asset name as id) plus float storage on `PlayerData`. `IsAlive` and `CanMove` are the same pattern: dedicated `PlayerAttribute` assets plus entries in `attributeEntries`. Behaviors reference the same assets as the data entries so values stay aligned without duplicated string identifiers.

## Types

| Type | Role |
|------|------|
| `PlayerAttribute` | ScriptableObject; `AttributeName` is the human-readable id. |
| `AnimationAttribute` | ScriptableObject; `ParameterName` matches the asset name (used by `AnimationController` and `PlayerAttributeAnimatorDriver`). See `AnimationAttribute.cs`. |
| `PlayerAttributeEntry` | Serialized list item: attribute reference, float value, optional `UnityEvent<float>` when the value changes. |
| `PlayerData` | References `isAliveAttribute` and `canMoveAttribute`, holds `attributeEntries`, exposes `IsAlive` / `CanMove` (via attributes), `GetFloatAttribute` / `GetBoolAttribute` / `SetFloatAttribute` / `SetBoolAttribute`, and `AttributeValueChanged`. |
| `PlayerAttributeAnimatorDriver` | Maps each `PlayerAttribute` to an `AnimationAttribute` (animator parameter id) on `AnimationController` and applies on enable and when values change. |

## Bool convention

Booleans are stored as float: `> 0` is true, `0` is false. `SetBoolAttribute` writes `1` or `0`.

## Authoring

1. Create `PlayerAttribute` assets under `Assets/Data/PlayerAttributes/` (or your content folder).
2. Assign `IsAlive` and `CanMove` attribute assets on `PlayerData`, and add one list entry per attribute (including those two) with the default float (`1` for bool true).
3. On each behavior, assign the same `PlayerAttribute` reference used in that list.
4. Optional: add `PlayerAttributeAnimatorDriver` bindings (attribute → `AnimationAttribute` asset, float or bool mode). Create `AnimationAttribute` assets under `Assets/Data/AnimationAttributes/` for each animator parameter you reference.
