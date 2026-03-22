# Simple Remote Catalog for Addressables (Weapon Prefab Sample)

This ExecPlan is a living document. The sections `Progress`, `Surprises & Discoveries`, `Decision Log`, and `Outcomes & Retrospective` must be kept up to date as work proceeds.

This document must be maintained in accordance with `PLANS.md` at the repository root.

## Purpose / Big Picture

Today the project’s Addressables settings keep a **local catalog** bundled with the player (`m_BuildRemoteCatalog: 0` in `Assets/AddressableAssetsData/AddressableAssetSettings.asset`). That means the game does not download a separate **catalog file** from the network at startup; it only uses the catalog data that shipped inside the build.

After this work, a contributor can **build Addressables content** so that the **catalog JSON** and **asset bundles** for a small sample set are produced under a predictable output folder, **serve those files over HTTP** from a laptop (behaving like a minimal **CDN**), and **run the game** so that startup catalog sync (`AddressablesAssetClient.SyncCatalogAndContentAsync` in `Assets/Scripts/Assets/Addressables/Runtime/Implementation/AddressablesAssetClient.cs`) can detect an updated remote catalog and download the sample bundles. The **sample assets** are the three weapon prefabs that already live in **Default Local Group** with short addresses `GreatSword`, `CurvedSword`, and `LongSword` (files under `Assets/Prefabs/Weapons/`).

Someone can see it working by hosting the built `ServerData` tree, pointing the active Addressables **profile** `RemoteLoadPath` at that URL, building a player (or using an existing-build play mode workflow), starting the game, and observing that one of those prefabs loads successfully while its bundle is fetched from the HTTP host.

## Progress

- [x] (2026-03-22 00:00Z) Authored initial ExecPlan with concrete group/profile steps and weapon prefab sample.
- [ ] Execute editor configuration: remote group, remote catalog flags, profile load URL, move three weapon entries out of Default Local Group.
- [ ] Produce first remote Addressables build output under `ServerData/[BuildTarget]/` and verify catalog files exist next to bundles.
- [ ] Host `ServerData` with a static HTTP server; run player or editor play mode using built content; confirm `GreatSword` (or another sample address) loads.
- [ ] Update `Docs/Infra/Addressables.md` with a short “Remote catalog (sample)” subsection (paths, build, host, profile).
- [ ] Run `.agents/scripts/validate-changes.cmd` from the repository root and fix any failures until clean; commit.

## Surprises & Discoveries

- Observation: (none yet)
  Evidence: (none yet)

## Decision Log

- Decision: Use the three existing sword prefabs (`GreatSword`, `CurvedSword`, `LongSword`) as the sample remote content instead of inventing new assets.
  Rationale: They are already addressable entries in `Assets/AddressableAssetsData/AssetGroups/Default Local Group.asset` and map to real files under `Assets/Prefabs/Weapons/`.
  Date/Author: 2026-03-22 / Codex

- Decision: Put the sample entries in a **new** Addressables group (for example `Remote Weapons (Sample)`) with **remote** build and load paths, and **remove** those entries from **Default Local Group** so the sample is unambiguously remote-hosted.
  Rationale: Leaving duplicates in a local group would keep shipping the same assets locally and blur the proof that downloads occurred from the remote URL.
  Date/Author: 2026-03-22 / Codex

- Decision: Use the existing profile variables `RemoteBuildPath` and `RemoteLoadPath` already defined in `AddressableAssetSettings.asset` (`ServerData/[BuildTarget]` and a user-editable HTTP base) rather than introducing a new custom path system in code.
  Rationale: The runtime gateway already calls Unity’s `CheckForCatalogUpdates` / `UpdateCatalogs` / `DownloadDependenciesAsync`; no C# change is required for a basic remote catalog if Unity’s settings and URLs are correct.
  Date/Author: 2026-03-22 / Codex

## Outcomes & Retrospective

Not completed yet. When done, summarize what was configured, where built artifacts land, and any follow-ups (HTTPS, auth, CloudFront, Unity CCD, dynamic catalog URL).

## Context and Orientation

**Addressables** is Unity’s system for naming content with an **address** (for example `GreatSword`), packing that content into **bundles**, and writing a **catalog** that maps addresses to bundle locations. A **remote catalog** is a catalog file (and hash file) that the player downloads from a URL at runtime instead of relying only on the catalog embedded in the app. A **CDN** in this plan means any **static HTTP file host** that serves those files; a one-line static server on your machine is enough to prove the pipeline.

Relevant paths:

1. `Assets/AddressableAssetsData/AddressableAssetSettings.asset` — global toggles such as **Build Remote Catalog** and **Remote Catalog Build Path** / **Remote Catalog Load Path**.
2. `Assets/AddressableAssetsData/AssetGroups/Default Local Group.asset` — currently lists the weapon prefabs and other entries.
3. `Assets/Scripts/Assets/Addressables/Runtime/Implementation/AddressablesAssetClient.cs` — startup sync that updates catalogs and downloads dependencies when Unity reports updates.
4. `Docs/Infra/Addressables.md` — module documentation to extend with operational notes.

**Weapon prefab files (for human verification):**

- `Assets/Prefabs/Weapons/GreatSword.prefab`
- `Assets/Prefabs/Weapons/CurvedSword.prefab`
- `Assets/Prefabs/Weapons/LongSword.prefab`

## Plan of Work

First, open the Unity project in the Editor version pinned in `Architecture.md` (Unity `2022.3.50f1`) so Addressables UI matches this repository.

Create a new **Addressables group** named `Remote Weapons (Sample)`. Add the **Bundled Asset Group** schema (and any **content update** schema the template requires for your Addressables package version). In the bundled schema, set **Build Path** to the profile variable **RemoteBuildPath** and **Load Path** to **RemoteLoadPath**. These names refer to the entries under **Addressables Profiles** in the Addressables **Groups** window; in this repo’s settings asset they correspond to `ServerData/[BuildTarget]` for build output and a placeholder like `http://localhost/[BuildTarget]` for load (you will change the load value to your real host URL before testing).

Open **Default Local Group** and **move** the three serialized entries whose addresses are `GreatSword`, `CurvedSword`, and `LongSword` into `Remote Weapons (Sample)`. Do not leave copies in the default group. Keep the same **address strings** so existing code or references that use those keys keep working.

Open **Addressable Asset Settings** (menu **Window > Asset Management > Addressables > Settings**). Enable **Build Remote Catalog**. Assign **Remote Catalog Build Path** and **Remote Catalog Load Path** to profile variables that resolve next to your bundle output (the common pattern is to use the same **RemoteBuildPath** and **RemoteLoadPath** as bundles so the catalog JSON sits alongside bundles under `ServerData/[BuildTarget]/` and is downloaded from the same HTTP base). Save assets.

Run **Build > New Build > Default Build Script** (wording may vary slightly by package version) to produce bundles and the remote catalog files under the remote build folder. Confirm the output folder contains bundle files and catalog-related files (names typically include `catalog` and `.hash`).

Start a static HTTP server whose **document root** is the parent of the `[BuildTarget]` folder so that a URL shaped like `http://127.0.0.1:<port>/<BuildTarget>/...` matches **RemoteLoadPath**. For example, if **RemoteLoadPath** is `http://127.0.0.1:8080/[BuildTarget]`, serve the directory that contains the `StandaloneWindows64` (or your target) folder, not only the inner folder, so the substituted path resolves correctly.

Set the active profile’s **RemoteLoadPath** to that URL. Build a **development player** for the same **Build Target** you used for the Addressables build, install or run it, and ensure the machine running the player can reach the HTTP server (firewall loopback rules). Alternatively, use the Addressables **Play Mode Script** that uses **existing built content** if your team’s workflow prefers editor verification; the acceptance goal is the same: a load of `GreatSword` resolves and the bundle traffic comes from HTTP.

Finally, document the exact profile values, build menu path, and hosting command in `Docs/Infra/Addressables.md` so the next contributor does not rely on memory.

## Concrete Steps

Run shell examples from the repository root `/workspace` unless noted otherwise.

1. **Unity Editor (manual):** Create group `Remote Weapons (Sample)`; configure bundled schema paths to **RemoteBuildPath** / **RemoteLoadPath**; move `GreatSword`, `CurvedSword`, `LongSword` from **Default Local Group**; enable **Build Remote Catalog**; bind remote catalog paths; save.
2. **Unity Editor:** **Addressables > Build > New Build > Default Build Script** (or equivalent). Inspect `ServerData/<YourBuildTarget>/` (path follows **RemoteBuildPath**) for bundles + catalog artifacts.
3. **Host files (example):** If your built folder is `ServerData/StandaloneWindows64` and **RemoteLoadPath** is `http://127.0.0.1:8080/[BuildTarget]`, start Python’s static server from the `ServerData` directory so that `http://127.0.0.1:8080/StandaloneWindows64/` lists files:

        cd /workspace/ServerData
        python3 -m http.server 8080

   Adjust port and directory if your **RemoteLoadPath** differs. The rule is: the URL after substituting `[BuildTarget]` must resolve to the folder that contains the built catalog and bundles.

4. **Run the player** with networking allowed; trigger a load of `GreatSword` through gameplay or a temporary debug script that calls `Addressables.LoadAssetAsync<GameObject>("GreatSword")` or the project’s `IAddressablesGateway` equivalent. Release the handle after verification to avoid leaks during repeated tests.

5. **Quality gate (mandatory for milestone completion in this repo):** From repository root on Windows per `AGENTS.md`:

        & ".\.agents\scripts\validate-changes.cmd"

   On Linux environments without Unity, record the skip or failure reason in **Surprises & Discoveries** and still complete editor-side acceptance on a Windows+Unity workstation.

## Validation and Acceptance

Acceptance is behavior-first:

1. After a clean Addressables build, the remote build directory contains both **bundles** and **remote catalog** artifacts (file names include catalog-related stems; exact names depend on package version).
2. With the HTTP server running and **RemoteLoadPath** pointing at it, starting the game performs catalog sync without unhandled fatal errors, and loading address `GreatSword` yields a non-null `GameObject` prefab instance source (same for `CurvedSword` or `LongSword` if you spot-check one additional address).
3. Optional stronger check: enable verbose Addressables or network logging temporarily and confirm bundle requests hit `http://` to your host (only if your team allows transient logging).

If sync fails, `AddressablesGateway` logs a warning and continues; for this milestone, treat **successful load of the remote sample** as the pass condition, and file follow-up work if you need hard-fail on catalog mismatch.

## Idempotence and Recovery

Re-running **New Build** overwrites output under `ServerData/[BuildTarget]`; safe if you only use that tree for experiments. If profiles are wrong, revert **RemoteLoadPath** to a known good value in version control. Git may show changes under `Assets/AddressableAssetsData/`; commit them when the team agrees on the default profile wording (use a localhost URL in committed settings only if the whole team accepts it, or document that each developer overrides **RemoteLoadPath** locally without committing).

## Artifacts and Notes

Indented examples of what a successful directory listing might resemble after build (file names will vary by Unity version):

    ServerData/StandaloneWindows64/
      catalog_*.json
      catalog_*.hash
      *.bundle

## Interfaces and Dependencies

No new C# interfaces are required for the minimal configuration-only milestone. The existing `IAddressablesAssetClient.SyncCatalogAndContentAsync` remains the runtime entry for catalog refresh and dependency download.

If a future milestone adds **dynamic catalog URLs** (for example from remote config), that would be new surface area; this ExecPlan intentionally stays within Unity Addressables settings and static HTTP hosting.

---

Revision: 2026-03-22 — Initial plan authored for simple remote catalog using existing weapon prefab addresses moved to a dedicated remote group.
