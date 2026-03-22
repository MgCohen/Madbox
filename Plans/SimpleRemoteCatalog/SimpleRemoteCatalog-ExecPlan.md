# Simple Remote Catalog for Addressables (CCD + Weapon Prefab Sample)

This ExecPlan is a living document. The sections `Progress`, `Surprises & Discoveries`, `Decision Log`, and `Outcomes & Retrospective` must be kept up to date as work proceeds.

This document must be maintained in accordance with `PLANS.md` at the repository root.

## Purpose / Big Picture

Repository-side setup now enables a **remote catalog** and **CCD** in `Assets/AddressableAssetsData/AddressableAssetSettings.asset` (`m_BuildRemoteCatalog: 1`, `m_CCDEnabled: 1`, remote catalog paths bound to **RemoteBuildPath** / **RemoteLoadPath**). The three sword prefabs live in **Remote Weapons (Sample)** with **RemoteBuildPath** / **RemoteLoadPath**; they were removed from **Default Local Group**. The package `com.unity.services.ccd.management` **3.0.3** is in `Packages/manifest.json` for Editor **Build & Release**.

What remains is **Unity Cloud** work only: link the Editor to your cloud project, create a **development** CCD bucket, set the active Addressables profile’s **Remote** source to **Cloud Content Delivery** (so **RemoteLoadPath** becomes the real `unity3dusercontent.com` base instead of the placeholder `http://localhost/[BuildTarget]`), then **Build & Release** (or manual upload + **Create Release**) and verify `GreatSword` loads. Runtime startup still uses `AddressablesAssetClient.SyncCatalogAndContentAsync` in `Assets/Scripts/Assets/Addressables/Runtime/Implementation/AddressablesAssetClient.cs`.

## Progress

- [x] (2026-03-22 00:00Z) Authored initial ExecPlan with weapon prefab sample and local HTTP hosting.
- [x] (2026-03-22) Revised plan to use **Unity Cloud Content Delivery (CCD)** integrated with Addressables; removed self-hosted static server as the primary path.
- [ ] Link the Unity Editor project to the correct **Unity Cloud Project** (Project Settings) and confirm **CCD** is available in the Unity Dashboard for that project.
- [ ] Create a CCD **bucket** (for example `Madbox Addressables Sample`) in the **development** environment; note **Project ID** and **Bucket ID** (or use the Editor-linked flow so IDs are implicit when picking the bucket in Addressables).
- [x] (2026-03-22) Added **CCD Management** `com.unity.services.ccd.management` **3.0.3** to `Packages/manifest.json` and `Packages/packages-lock.json` (compatible with Addressables **1.22.2**). Open the project in Unity once so the Editor reconciles packages if versions drift.
- [ ] In **Addressables** profile used for development: set the **Remote** path source to **Cloud Content Delivery**. Bind the profile to the sample **development** bucket so **RemoteLoadPath** is the real HTTPS CCD base (replaces placeholder `http://localhost/[BuildTarget]` in the Default profile until you do this).
- [x] (2026-03-22) Created group **Remote Weapons (Sample)** with bundled + content-update schemas; **Build Path** / **Load Path** use profile IDs **RemoteBuildPath** / **RemoteLoadPath**. Moved `GreatSword`, `CurvedSword`, `LongSword` out of **Default Local Group**.
- [x] (2026-03-22) **Addressable Asset Settings:** **Build Remote Catalog** on, **CCD** on, **Remote Catalog Build/Load Path** set to **RemoteBuildPath** / **RemoteLoadPath** (same as remote bundles).
- [ ] Run **Build > Build & Release** (CCD Management) or manual build + Dashboard **Create Release** so **`latest`** points at uploaded catalog and bundles.
- [ ] Validate in Editor (**Play Mode Script: Use Existing Build** or equivalent) or in a **development player** that loading `GreatSword` succeeds after catalog sync.
- [x] (2026-03-22) Updated `Docs/Infra/Addressables.md` with **CCD** subsection and corrected module paths to `Assets/Scripts/Assets/Addressables/`.
- [ ] Run `.agents/scripts/validate-changes.cmd` from the repository root on a Windows machine with Unity and PowerShell; fix failures until clean (skipped in headless Linux agent: see Surprises).

## Surprises & Discoveries

- Observation: Cloud agent environment has no Unity Editor, `dotnet`, or PowerShell; `.agents/scripts/validate-changes.cmd` was not executed here.
  Evidence: `command -v dotnet` and `command -v pwsh` return not found on the execution host.

## Decision Log

- Decision: Use the three existing sword prefabs (`GreatSword`, `CurvedSword`, `LongSword`) as the sample remote content instead of inventing new assets.
  Rationale: They were already addressable entries in **Default Local Group** and map to real files under `Assets/Prefabs/Weapons/` (now in **Remote Weapons (Sample)**).
  Date/Author: 2026-03-22 / Codex

- Decision: Put the sample entries in a **new** Addressables group (for example `Remote Weapons (Sample)`) with **remote** build and load paths, and **remove** those entries from **Default Local Group** so the sample is unambiguously loaded from CCD, not from the local default group.
  Rationale: Duplicates in a local group would keep shipping the same assets inside the player and hide whether CCD delivery works.
  Date/Author: 2026-03-22 / Codex

- Decision: Use **Unity Cloud Content Delivery (CCD)** as the remote host instead of a laptop static file server.
  Rationale: CCD is Unity’s managed delivery layer (HTTPS, global edge, releases/badges); it matches “Unity CDN integration” and avoids operating custom HTTP infrastructure for the sample.
  Date/Author: 2026-03-22 / Codex

- Decision: Prefer the **CCD Management** package plus Addressables **Build & Release** for build-upload-release in one Editor flow; document the **manual** alternative (local build, Dashboard upload, Create Release) for environments where CI or policy blocks the package.
  Rationale: Unity documents both in “CCD and Addressables walkthrough”; the integrated flow reduces copy/paste URL mistakes.
  Date/Author: 2026-03-22 / Codex

- Decision: Do **not** add game code in this milestone for **public** buckets. If the team enables **bucket privacy**, require `Addressables.WebRequestOverride` to attach the **Bucket Access Token** (Unity documents `Authorization: Basic` plus base64-encoded token) before loads; record that in module docs and a follow-up task if Madbox uses private buckets.
  Rationale: Private buckets fail all downloads without the header; public dev buckets keep the first integration test minimal.
  Date/Author: 2026-03-22 / Codex

## Outcomes & Retrospective

**Done in repo:** Remote catalog + CCD flags, remote catalog paths, **Remote Weapons (Sample)** group, CCD Management package, module documentation.

**Still on a human with Unity + cloud access:** Link Services, pick CCD bucket in Addressables profile, **Build & Release**, play-mode verification of `GreatSword`, full **validate-changes** gate.

## Context and Orientation

**Addressables** maps **addresses** (for example `GreatSword`) to **bundles** and records the mapping in a **catalog**. A **remote catalog** is downloaded at runtime when **Build Remote Catalog** is enabled and the **Remote Catalog Load Path** points at a URL that serves the catalog files. **Cloud Content Delivery (CCD)** is a Unity Gaming Services product that stores **entries** in **buckets**, groups them into **releases**, and exposes them under stable **HTTPS** URLs. The segment **`release_by_badge/latest`** in the URL pattern means “resolve content from the release that currently carries the **latest** badge” (Unity creates that badge when you create a release).

**CCD Management** (`com.unity.services.ccd.management`) is an Editor/package layer that talks to CCD APIs so Addressables can **push** built content and **create releases** without hand-uploading every file in the Dashboard—when you use **Build & Release** from the Addressables **Groups** window.

Relevant repository paths:

1. `Packages/manifest.json` — includes `com.unity.services.ccd.management` **3.0.3** (Unity may adjust `packages-lock.json` further on first open).
2. `Assets/AddressableAssetsData/AddressableAssetSettings.asset` — **Build Remote Catalog**, **Remote Catalog** paths, **`m_CCDEnabled`**.
3. `Assets/AddressableAssetsData/AssetGroups/Default Local Group.asset` — no longer contains the three sword entries.
4. `Assets/AddressableAssetsData/AssetGroups/Remote Weapons (Sample).asset` — remote group for the swords.
5. `Assets/Scripts/Assets/Addressables/Runtime/Implementation/AddressablesAssetClient.cs` — startup catalog check/update and dependency download (unchanged for CCD if URLs are correct).
6. `Docs/Infra/Addressables.md` — includes CCD operational notes.

**Weapon prefab files (for human verification):**

- `Assets/Prefabs/Weapons/GreatSword.prefab`
- `Assets/Prefabs/Weapons/CurvedSword.prefab`
- `Assets/Prefabs/Weapons/LongSword.prefab`

**Manual RemoteLoadPath template (only if you configure paths by hand instead of the Editor CCD picker):** Unity’s CCD + Addressables walkthrough uses an **entry-by-path** base URL. Fill in your Unity Cloud **Project ID** and the target bucket’s **Bucket ID** (development vs production buckets differ):

    https://PROJECT_ID.client-api.unity3dusercontent.com/client_api/v1/buckets/BUCKET_ID/release_by_badge/latest/entry_by_path/content/?path=

The Addressables profile’s **RemoteLoadPath** must end with a **`?path=`** suffix (or equivalent pattern Unity generates for your package version) so internal IDs append correctly. When in doubt, use the **Cloud Content Delivery** option in the Addressables **Profiles** UI so Unity fills **RemoteBuildPath** and **RemoteLoadPath** consistently for the selected bucket.

## Plan of Work

Open the project in Unity **2022.3.50f1** (see `Architecture.md`).

**Services link:** In **Edit > Project Settings > Services** (or the **Unity Cloud** connection entry point your Editor version shows), link this repository’s Unity project to the **Unity Cloud Project** that will own CCD buckets. Without a linked project, CCD bucket pickers and API authentication in the Editor will not work.

**Dashboard: bucket:** In the Unity Dashboard, open **Cloud Content Delivery** for that cloud project. Create a bucket (for example `Madbox Addressables Sample`) in the **development** environment. For the first integration, leave the bucket **public** unless you are ready to implement `Addressables.WebRequestOverride` with a **Bucket Access Token** (Unity documents this for private buckets). If the bucket is **private**, every `UnityWebRequest` Addressables uses for catalog and bundles must carry the authorization header or downloads will fail with authorization errors.

**Package:** The repo already lists `com.unity.services.ccd.management` **3.0.3**. After first open in Unity, if Package Manager resolves a different patch, commit the lockfile diff the Editor writes.

**Addressables profile:** Open **Window > Asset Management > Addressables > Groups**. In **Manage Profiles**, select the profile you use for day-to-day development (or create **Development (CCD)**). For the **Remote** section, choose **Cloud Content Delivery** from the **Remote** dropdown (wording per Addressables 1.22.x). Select the **development** bucket you created. The Editor should populate **RemoteBuildPath** and **RemoteLoadPath** (or equivalent variable names) so builds go to a local staging folder and loads resolve through CCD HTTPS URLs. Enable **CCD** in **Addressable Asset Settings** if a toggle is shown so `m_CCDEnabled` is on.

**Sample group:** **Remote Weapons (Sample)** is already in the repo. After opening **Groups**, confirm schemas and paths look correct in the Inspector (Unity may reserialize minor fields).

**Remote catalog:** Already enabled in **Addressable Asset Settings**; confirm in Inspector after Unity import.

**Build and publish:** Preferred: from **Addressables > Groups**, run **Build > Build & Release** (CCD Management). That should build Addressables content, upload entries for the configured remote targets, **create a release**, and move the **latest** badge. Alternative: **Build > New Build > Default Build Script**, then in the Dashboard open the bucket, **Upload** the files from **RemoteBuildPath** output, then **Create Release** so **latest** points at the new content.

**Verify:** Set **Play Mode Script** to **Use Existing Build** (or run a standalone **development** build). Start the game; confirm startup does not break on catalog sync; load `GreatSword` via existing gameplay or a one-off `Addressables.LoadAssetAsync<GameObject>("GreatSword")` / `IAddressablesGateway` call and **release** the handle after the test.

**Docs:** Extend `Docs/Infra/Addressables.md` with CCD prerequisites, the Build & Release menu path, and a pointer to Unity’s **CCD and Addressables walkthrough** title so readers can open the current doc URL from their browser (do not treat external URLs as executable steps inside this plan; the workflow above is self-contained).

## Concrete Steps

1. **Unity Editor:** Link cloud project; create **development** CCD bucket (public for first pass).
2. **Package Manager:** Confirm **CCD Management** resolves (already in manifest).
3. **Addressables > Manage Profiles:** Set **Remote** to **Cloud Content Delivery**; pick bucket; confirm **RemoteBuildPath** / **RemoteLoadPath**.
4. **Addressable Asset Settings:** Confirm **Build Remote Catalog**, **CCD**, and remote catalog paths (already set in YAML).
5. **Groups:** Confirm **Remote Weapons (Sample)** and absence of swords from **Default Local Group**.
6. **Build > Build & Release** (or manual build + Dashboard upload + **Create Release**).
7. **Play / player build:** Load `GreatSword`; confirm success.
8. **Docs:** Already updated in repo; extend if your team adds bucket naming conventions.
9. **Quality gate:** From repository root on Windows per `AGENTS.md`:

        & ".\.agents\scripts\validate-changes.cmd"

   On Linux-only agents without Unity, note the limitation under **Surprises & Discoveries** and complete validation on a Windows+Unity machine.

## Validation and Acceptance

1. After **Build & Release** (or manual release), the CCD bucket’s **development** environment shows a **release** with the **latest** badge, and entries include catalog and bundle objects Addressables produced (exact names depend on package version).
2. With **RemoteLoadPath** pointing at CCD (via profile), running the game completes catalog sync without blocking the test, and `GreatSword` loads as a non-null prefab asset.
3. Optional: inspect Unity **Profiler** or temporary logging to confirm request hosts are under **`unity3dusercontent.com`** (only if allowed by team policy).

If sync fails, `AddressablesGateway` logs a warning and continues; for this milestone, **successful load of the remote sample from CCD** is still the primary pass signal.

## Idempotence and Recovery

**Build & Release** can be repeated; it creates new content entries and releases per your CCD workflow. If a release is bad, use Dashboard revision/promotion tools per Unity’s CCD docs; do not commit secrets (access tokens) into the repository. For **private** buckets, store tokens outside source control (environment, CI secret, or local-only script).

## Artifacts and Notes

After a local **Default Build Script** (manual path), Unity still writes bundles under the folder indicated by **RemoteBuildPath** (often under `ServerData/[BuildTarget]` or the path the CCD profile defines). Those files are the payload you upload if not using **Build & Release**.

Indented template for manual **RemoteLoadPath** base (replace placeholders):

    https://PROJECT_ID.client-api.unity3dusercontent.com/client_api/v1/buckets/BUCKET_ID/release_by_badge/latest/entry_by_path/content/?path=

## Interfaces and Dependencies

**Package dependency:** `com.unity.services.ccd.management` **3.0.3** in `Packages/manifest.json` (Unity 2022.3 + Addressables 1.22.x).

**Optional game code (private buckets only):** assign `Addressables.WebRequestOverride` once at startup to add `Authorization: Basic <base64 token>` per Unity’s private-bucket example. That is **not** required for public development buckets.

**Unchanged runtime contract:** `IAddressablesAssetClient.SyncCatalogAndContentAsync` remains the catalog refresh entry point; CCD differs only in the **URLs** embedded in built catalogs and profiles.

---

Revision: 2026-03-22 — Replaced self-hosted HTTP server workflow with Unity **Cloud Content Delivery (CCD)** and **CCD Management** / **Build & Release**; documented manual upload alternative, `RemoteLoadPath` template, and private bucket token note.

Revision: 2026-03-22 — Execution pass: committed Addressables YAML (**Remote Weapons (Sample)**, remote catalog + CCD flags), CCD Management package, and `Docs/Infra/Addressables.md`; Progress updated; validate-changes not run on headless Linux.
