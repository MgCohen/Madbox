# Milestone 1 - Generic Addressables Core and Central Gateway

## Goal

Deliver the first working version of an Infra Addressables module that is fully generic across asset types and exposes only `Initialize` and `Load` through gateway contracts, with release executed on each returned handle. At the end of this milestone, bootstrap and feature modules can request assets through one gateway without creating enemy-specific, level-specific, or prefab-specific providers.

This milestone solves the foundational architecture problem: centralize all addressable lifecycle control behind one service while keeping reference tracking, preload behavior, and release rules internal while enforcing strict handle ownership.

## Deliverable

1. New Addressables module under `Assets/Scripts/Infra/Addressables/` with runtime, container, and tests assemblies.
2. Public contracts for `IAddressablesGateway`, asset keys/catalog keys/reference keys, and typed handles.
3. Runtime implementation with one generic provider and one central lifecycle controller.
4. Scope-driven startup integration where an addressables initializer implements `IAsyncLayerInitializable` and initializes the gateway during layer startup.
5. EditMode tests proving generic load/release behavior by key, catalog, and reference.
6. Module documentation file `Docs/Infra/Addressables.md` with API and usage examples.
7. A documented `EnemiesService` sample usage flow that covers single load, load-all, and handle-owned release behavior.
8. A generic preload registry/service with `Normal` and `NeverDie` preload ownership modes.

## Plan

1. Create the module structure following repository module conventions and asmdef dependency rules.
2. Add contract types first, then implement runtime services behind those contracts.
3. Implement one central controller that owns operation tracking and delegates Addressables calls internally.
4. Add overloads for loading by key, catalog, and serialized reference with typed handles as outputs; remove gateway-level unload API.
5. Integrate gateway initialization into scope startup order (`ScopeInitializer`) through an `IAsyncLayerInitializable` implementation in Infra/App composition flow.
6. Add tests that verify:
   - initialize can run once safely,
   - same request can share internal operation state,
   - `IAssetHandle.Release` decrements the central reference counter and releases underlying asset only at zero references,
   - feature code does not need specialized providers,
   - feature services do not own preload caches (preload comes from generic preload registry).
7. Run milestone quality loop:
   - `powershell -NoProfile -ExecutionPolicy Bypass -File ".\.agents\scripts\run-editmode-tests.ps1"`
   - `powershell -NoProfile -ExecutionPolicy Bypass -File ".\.agents\scripts\check-analyzers.ps1"`
   - `& ".\.agents\scripts\validate-changes.cmd"`
   - fix failures and rerun until clean.
8. Commit milestone changes.

## Snippets and Samples

Expected usage shape after milestone:

    await _addressablesGateway.InitializeAsync(ct);
    var enemyHandle = await _addressablesGateway.LoadAsync<GameObject>(new AssetKey("enemy/bee"), ct);
    var levelHandles = await _addressablesGateway.LoadAsync<LevelDefinitionSO>(new CatalogKey("level"), ct);
    enemyHandle.Release();

Expected test intent examples:

    LoadAsync_WhenCalledForSameKey_TracksOwnershipWithoutDoubleRelease
    Release_WhenLastHandleReleased_ReleasesUnderlyingOperation
    LoadAsync_ByCatalog_ReturnsTypedHandleList
    InitializeAsync_WithPreloadNeverDie_KeepsGatewayOwnedHandleAlive
    InitializeAsync_WithPreloadNormal_AllowsFirstConsumerOwnershipTransfer

Expected `EnemiesService` sample shape after milestone:

    // Startup path: IAsyncLayerInitializable.InitializeAsync(ct) calls _gateway.InitializeAsync(ct)
    var oneEnemyHandle = await _gateway.LoadAsync<Enemy>(new AssetKey($"enemy/{enemyId}"), ct);
    var allEnemyHandles = await _gateway.LoadAsync<Enemy>(new CatalogKey("enemy"), ct);
    oneEnemyHandle.Release();
