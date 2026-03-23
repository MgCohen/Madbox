# Player attributes (GameView)

## Purpose

Named numeric stats use `PlayerAttribute` ScriptableObject assets (subclass of **`EntityAttribute`**, asset name as id) plus float storage on `PlayerData` (subclass of **`EntityData`**). `IsAlive` and `CanMove` are the same pattern: dedicated `PlayerAttribute` assets plus entries in the inherited **`attributeEntries`**. Behaviors reference the same assets as the data entries so values stay aligned without duplicated string identifiers.

## Types

| Type | Role |
|------|------|
| `PlayerAttribute` | **`EntityAttribute`** subclass; `AttributeName` is the human-readable id. |
| `AnimationAttribute` | In **`Madbox.Animation`**: `ParameterName` matches the asset name (used by `AnimationController` and `PlayerAttributeAnimatorDriver`). |
| `EntityAttributeEntry` | Base serialized list item on **`EntityData`**: attribute reference, float value, optional `UnityEvent<float>` when the value changes. |
| `PlayerData` | Extends **`EntityData`**: references `isAliveAttribute` and `canMoveAttribute`, inherits `attributeEntries`, exposes `IsAlive` / `CanMove` (via attributes) plus inherited attribute getters/setters and `AttributeValueChanged`. |
| `PlayerAttributeAnimatorDriver` | Thin subclass of **`EntityAttributeAnimatorDriver{PlayerData}`**; maps each `PlayerAttribute` to an `AnimationAttribute` on `AnimationController`. |

## Bool convention

Booleans are stored as float: `> 0` is true, `0` is false. `SetBoolAttribute` writes `1` or `0`.

## Authoring

1. Create `PlayerAttribute` assets under `Assets/Data/PlayerAttributes/` (or your content folder).
2. Assign `IsAlive` and `CanMove` attribute assets on `PlayerData`, and add one list entry per attribute (including those two) with the default float (`1` for bool true).
3. On each behavior, assign the same `PlayerAttribute` reference used in that list.
4. Optional: add `PlayerAttributeAnimatorDriver` bindings (attribute → `AnimationAttribute` asset, float or bool mode). Create `AnimationAttribute` assets under `Assets/Data/AnimationAttributes/` for each animator parameter you reference.
