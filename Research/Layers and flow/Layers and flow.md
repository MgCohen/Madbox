# Initialization Flow Research (Based on High-Level Diagram)

Date: 2026-03-17
Related: `Research/Archero-Sample-Research-Plan.md`, `Architecture.md`

![Initialization flow diagram](C:/Users/mtgco/.codex/worktrees/b2b7/Madbox/Research/Layers and flow/Flow Diagram.png)

## 1. Purpose

This document captures the proposed initialization flow diagram, clarifies the intended arrow semantics, and translates it into enforceable architecture guidance for Madbox.

Important clarification from author intent:

- Diagram arrows represent _flow of registration/provisioning_ ("gets passed to"), not compile-time dependency direction.
- `Game Logic (C#)` does not depend on Unity view/simulation. Unity-facing modules adapt and pass data/commands into game logic boundaries.

## 2. Reconstructed Diagram (High-Level)

```mermaid
flowchart LR
    PLAY([PLAY]) --> BOOT[Bootstrap<br/>(IoC Container)]
    BOOT --> IINFRA[Install Infra]
    IINFRA --> ICORE[Install Core]
    ICORE --> IMETA[Install Meta]
    IMETA --> IGAME[Install Game]
    IGAME --> IAPP[Install App]
    IAPP --> START([START])
    START --> MMV[Main Menu View]

    subgraph INFRA_ITEMS[Infra Systems]
      SCOPE[Scope]
      EVENTS[Events]
      NAV[Navigation]
      CLOUD[Cloud Code]
      UGS[UGS]
      MODELS[Models]
    end
    IINFRA --> INFRA_ITEMS

    subgraph CORE_ITEMS[Core Systems]
      ENT[Entities<br/>(Enemies, Characters)]
      GMODS[Game Modules]
      ADDR[Addressables]
      VM[View Model]
    end
    ICORE --> CORE_ITEMS

    subgraph META_ITEMS[Meta Services]
      LEVEL[Level Service]
      GOLD[Gold Service]
      ADS[Ads]
    end
    IMETA --> META_ITEMS

    subgraph GAME_ITEMS[Game Runtime]
      GLOGIC[Game Logic (C#)]
    end
    IGAME --> GAME_ITEMS
    META_ITEMS --> GLOGIC
    CORE_ITEMS --> GLOGIC

    subgraph APP_ITEMS[App/Presentation]
      MENU[Main Menu]
      SIM[Game Simulation (Unity)]
      VIEW[View]
    end
    IAPP --> APP_ITEMS
    GLOGIC --> APP_ITEMS
```

## 3. What This Flow Gets Right

1. Explicit boot phases:
   - `Infra -> Core -> Meta -> Game -> App` gives deterministic setup order and clearer startup diagnostics.
2. Separation of concerns:
   - Core/game logic remains plain C# and can be validated in EditMode tests.
   - Unity-specific behavior stays in app/presentation/simulation adapters.
3. Good fit with current stack:
   - VContainer composition in bootstrap.
   - Existing Scaffold modules (scope/events/navigation/mvvm) can stay in infra/app orchestration roles.
4. Supports incremental delivery:
   - Small playable loop can run before full live ops/ads integration, matching the sample research phasing.

## 4. Dependency Interpretation (Hard Rule)

Because arrow direction in this diagram is provisioning flow, compile-time dependencies must be documented separately.

Hard rules:

1. Compile-time references follow architecture boundaries, not diagram arrows.
2. `Game Logic (C#)` may define contracts consumed by App/Presentation adapters.
3. App/Presentation may call into `Game Logic` through contracts/use-cases; never the reverse.
4. No UnityEngine types in Core/Game/Meta domain logic assemblies.
5. Infra SDK wrappers (UGS/Cloud/Ads/Addressables) are adapters behind interfaces/contracts.
6. Bootstrap is the only place where concrete implementations are wired together.
7. Every cross-layer interaction must have:
   - Contract definition location.
   - Owning module.
   - Test coverage at contract boundary.

## 5. Suggested Organization for This Initialization Model

Use this startup composition pattern:

1. `InstallInfra`
   - Register scope/event bus/navigation/model services.
   - Register SDK adapters (UGS/cloud/persistence/addressables/ads).
   - Register logging/diagnostics and startup telemetry.
2. `InstallCore`
   - Register domain services and use-cases (movement, targeting, combat, spawning, weapons).
   - Register pure C# state and policies.
3. `InstallMeta`
   - Register progression/reward/session-level services using infra abstractions.
4. `InstallGame`
   - Register gameplay runtime orchestrator/state machine (pure logic coordinator).
5. `InstallApp`
   - Register Unity-facing presenters/controllers, scene bindings, and view factories.

Startup policy:

1. Boot with deterministic order and fail-fast only on critical missing services.
2. Non-critical external failures (remote config, ads) should degrade gracefully and keep app startup alive.
3. Cache + apply live ops in policy-defined timing (for assignment: guaranteed effect on next session).

## 6. What to Watch Out For

1. Phase leakage:
   - Avoid putting scene/view logic in `InstallGame`.
2. Service locator drift:
   - Avoid resolving container directly from random runtime classes.
3. Hidden coupling via static singletons:
   - Prefer explicit constructor injection through contracts.
4. Address key drift:
   - Never scatter literal addressable keys across modules.
5. Runtime ordering assumptions:
   - If service A requires service B's side effects, encode that relationship in installer ordering/tests.
6. Cross-module ownership confusion:
   - Keep one module as owner of each concept (combat ownership in core; ads ownership in infra/meta).

## 7. Addressables Adapter: What It Is and How to Implement

### 7.1 What it is

An addressables adapter is an infra-facing gateway that hides Unity Addressables APIs behind project contracts.

Responsibilities:

1. Convert strongly typed asset keys/contracts into Addressables calls.
2. Standardize load/release lifecycle.
3. Map failures into domain-safe results (not raw engine exceptions).
4. Keep key management centralized.

### 7.2 Recommended shape

1. Contracts assembly:
   - `AssetId`/`AssetKey` value type(s).
   - `IAssetCatalog` for known keys/groupings.
   - `IAssetLoader` abstraction for async load/release.
2. Infra runtime assembly:
   - `AddressablesAssetLoader : IAssetLoader`
   - `AddressablesAssetCatalog : IAssetCatalog`
3. Presentation/App usage:
   - Request by contract key, not by raw string.
   - Instantiate only in Unity-facing layer.
4. Core/Game usage:
   - Decide _what_ to spawn/equip via IDs/policies only.
   - Never call Addressables directly.

### 7.3 Minimal behavioral rules

1. Every successful load must have a defined release owner.
2. No synchronous waits on Addressables in gameplay loops.
3. Missing keys return controlled error objects + telemetry event.
4. Keep per-feature preload lists explicit (menu, gameplay, weapons, enemies).

## 8. Suggested Next Steps

1. Convert this flow into explicit module installer interfaces/contracts (one per phase).
2. Add a startup order test that asserts the registration phases execute in the intended sequence.
3. Add an architecture test/analyzer check preventing UnityEngine references in Core/Game/Meta assemblies.
4. Create the first version of `IAssetLoader` + key catalog and migrate one feature path (for example weapon prefab load) through it.
