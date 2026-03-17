# Entity Addressables Specs

Date: 2026-03-17
Scope: Addressables organization and usage for entity/level authoring flow

Related document:

1. `Research/Entities/Entity-Research-and-Specs.md`

## 1. Goal

Support the authoring flow where designers drag `EnemyDefinitionSO` into `LevelDefinitionSO`, while keeping runtime loading scalable and compatible with pure C# simulation baking.

## 2. What Should Be Addressable

Mark assets as Addressable when they are loaded by key/reference at runtime, need async loading, or should be remotely updatable:

1. Enemy prefabs referenced by `EnemyDefinitionSO`.
2. Hero prefabs and weapon prefabs used by level/game composition.
3. Level definition assets (`LevelDefinitionSO`) if levels are loaded by id/menu selection.
4. Large optional visual content tied to entities (VFX prefab collections, skin variants).

Usually not worth making addressable (unless remote/content-update behavior is required):

1. Tiny always-resident configs loaded once at startup.
2. Editor-only helper assets and debug-only authoring data.

## 3. What Should Be AssetReference

Use `AssetReference` in serialized authoring assets when the target should load via Addressables:

1. In `EnemyDefinitionSO`:
   - `AssetReferenceGameObject EnemyPrefabRef`
2. In `LevelDefinitionSO` (if loading enemy definitions on demand):
   - `AssetReferenceT<EnemyDefinitionSO>` per entry
3. In view-only data assets:
   - `AssetReference` for optional projectile/VFX variants

Use direct object references for local-only or always-loaded content where async loading/content updates are not needed.

Rule of thumb:

1. Cross-module authoring links that can scale in count/size should default to `AssetReference`.
2. Pure domain C# models should not carry Unity object references; resolved Unity assets are mapped to domain DTOs.

## 4. Group Organization (Initial Proposal)

Current project baseline has only `Default Local Group`. Suggested split:

1. `Local_Static_Bootstrap`
   - Minimal startup essentials (small, rarely changed).
2. `Local_Static_CoreConfigs`
   - Shared configs that ship with app and update infrequently.
3. `Remote_Static_EntityDefinitions`
   - `EnemyDefinitionSO` and related authoring data intended for content updates.
4. `Remote_Static_EntityPrefabs`
   - Enemy/hero/weapon prefabs and tightly coupled assets.
5. `Remote_Static_LevelDefinitions`
   - `LevelDefinitionSO` assets and wave/setup data.
6. `Remote_Static_EntityVFX`
   - Heavy optional VFX/projectiles/variants that can update independently.

Group policy guidance:

1. Keep high-churn content in separate remote groups to reduce content-update blast radius.
2. Avoid mixing high-frequency and low-frequency changed assets in one group.
3. Keep bootstrap/startup bundles small.
4. Co-locate assets that are almost always loaded together.

## 5. Label Strategy

Add labels for flexible loading and diagnostics:

1. `entity`
2. `enemy`
3. `hero`
4. `weapon`
5. `level`
6. `vfx`
7. `bootstrap`

Example use:

1. Preload all `enemy` definitions for selected level tier.

## 6. Authoring-to-Runtime Flow with Addressables

1. Designer creates/edits `EnemyDefinitionSO` with an `AssetReferenceGameObject` prefab.
2. Designer drags `EnemyDefinitionSO` into `LevelDefinitionSO`.
3. Build/bake step validates all references.
4. Runtime loads `LevelDefinitionSO` (addressable or local).
5. Runtime resolves enemy definitions and prefab refs asynchronously.
6. Domain model is created from definition data.
7. View factory instantiates resolved prefab and binds it to domain entity id.

## 7. Build-Time Pure C# Simulation Alignment

1. Add an editor bake/export step:
   - `EnemyDefinitionSO` and `LevelDefinitionSO` -> pure C# serializable data (`json`/binary).
2. Ensure baked output contains stable ids and gameplay fields only.
3. Exclude Unity-only references from baked simulation payload.
4. Apply same validation rules to runtime Addressables content and bake output.

## 8. Guardrails

1. Never keep mutable runtime state in ScriptableObjects.
2. Do not pass `AssetReference` into core domain systems.
3. Validate unresolved/null `AssetReference` entries at edit time and CI.
4. Prefer typed `AssetReferenceT<T>` where possible for authoring safety.

## 9. References

1. Unity Addressables package manual overview: https://docs.unity3d.com/Packages/com.unity.addressables@1.21/manual/
2. Addressable Asset Settings: https://docs.unity3d.com/Packages/com.unity.addressables@1.21/manual/AddressableAssetSettings.html
3. Groups and schemas: https://docs.unity3d.com/Packages/com.unity.addressables@1.21/manual/GroupSchemas.html
4. Packing groups into bundles: https://docs.unity3d.com/Packages/com.unity.addressables@1.21/manual/PackingGroupsAsBundles.html
5. AssetReferences manual: https://docs.unity3d.com/Packages/com.unity.addressables@1.21/manual/AssetReferences.html
6. Loading AssetReferences: https://docs.unity3d.com/Packages/com.unity.addressables@1.21/manual/LoadingAssetReferences.html
7. Content update build flow: https://docs.unity3d.com/Packages/com.unity.addressables@1.21/manual/content-update-build-create.html
