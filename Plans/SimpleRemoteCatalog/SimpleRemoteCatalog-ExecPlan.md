# Simple Remote Catalog for Addressables (CCD + Weapon Prefab Sample)

This ExecPlan is a living document. The sections `Progress`, `Surprises & Discoveries`, `Decision Log`, and `Outcomes & Retrospective` must be kept up to date as work proceeds.

This document must be maintained in accordance with `PLANS.md` at the repository root.

## Purpose / Big Picture

Today the project’s Addressables settings keep a **local catalog** bundled with the player (`m_BuildRemoteCatalog: 0` and `m_CCDEnabled: 0` in `Assets/AddressableAssetsData/AddressableAssetSettings.asset`). The runtime still calls Unity’s catalog update APIs at startup (`AddressablesAssetClient.SyncCatalogAndContentAsync` in `Assets/Scripts/Assets/Addressables/Runtime/Implementation/AddressablesAssetClient.cs`), but there is no **remote** catalog or bundle host configured.

After this work, a contributor uses **Unity Cloud Content Delivery (CCD)** as the hosted **CDN**: Addressables **remote catalog** files and **bundles** for a small sample are built, **uploaded**, and exposed through a **release** on a CCD **bucket**. Players resolve content over **HTTPS** from Unity’s `client-api.unity3dusercontent.com` URLs—**no self-hosted HTTP server** and no manual `python -m http.server` step.

The **sample assets** remain the three weapon prefabs already in **Default Local Group** with short addresses `GreatSword`, `CurvedSword`, and `LongSword` (files under `Assets/Prefabs/Weapons/`). They move into a dedicated **remote** group so it is obvious they are delivered from CCD, not from the default local group.

Someone can see it working by linking the Unity project to a **Unity Cloud** project with CCD enabled, creating a **development** bucket, wiring Addressables **Remote** paths to that bucket (see below), running **Build & Release** (or the manual upload + release path), then playing the game or a player build and loading `GreatSword` successfully while bundles and catalog are fetched from CCD.

## Progress

- [x] (2026-03-22 00:00Z) Authored initial ExecPlan with weapon prefab sample and local HTTP hosting.
- [x] (2026-03-22) Revised plan to use **Unity Cloud Content Delivery (CCD)** integrated with Addressables; removed self-hosted static server as the primary path.
- [ ] Link the Unity Editor project to the correct **Unity Cloud Project** (Project Settings) and confirm **CCD** is available in the Unity Dashboard for that project.
- [ ] Create a CCD **bucket** (for example `Madbox Addressables Sample`) in the **development** environment; note **Project ID** and **Bucket ID** (or use the Editor-linked flow so IDs are implicit when picking the bucket in Addressables).
- [ ] Install **CCD Management** (`com.unity.services.ccd.management`) via **Package Manager** at a version **compatible with Addressables 1.19.15+** (this repo uses `com.unity.addressables` **1.22.2** in `Packages/manifest.json`—pick the latest **verified** CCD Management version for Unity **2022.3.50f1** shown in `Architecture.md`).
- [ ] In **Addressables** profile used for development: set the **Remote** path source to **Cloud Content Delivery** (Unity may prompt to install CCD Management if missing). Bind the profile to the sample **development** bucket and the **`latest`** badge behavior the tooling exposes (CCD assigns a **latest** badge to the newest **release**).
- [ ] Create group **Remote Weapons (Sample)** with **Bundled Asset Group** schema; set **Build Path** and **Load Path** to the **remote** pair that targets CCD (same as other remote groups in the official walkthrough). Move `GreatSword`, `CurvedSword`, `LongSword` from **Default Local Group** into this group; keep the same address strings.
- [ ] In **Addressable Asset Settings**, enable **Build Remote Catalog** and enable **CCD** integration if the Inspector exposes it (`m_CCDEnabled` should become **1** in `AddressableAssetSettings.asset` when using the CCD-backed remote profile). Set **Remote Catalog Build Path** and **Remote Catalog Load Path** to the **same remote path variables** as the sample group’s bundles so the catalog and bundles live under one consistent remote base (Unity’s CCD + Addressables docs use this pattern).
- [ ] Run **Window > Asset Management > Addressables > Groups** then **Build > Build & Release** (CCD Management workflow) so bundles **and** catalog are uploaded and a **release** is created (and **`latest`** updated). If you intentionally skip the package, follow the manual path: **New Build > Default Build Script**, then Dashboard **Upload** the **RemoteBuildPath** output folder, then **Create Release**.
- [ ] Validate in Editor (**Play Mode Script: Use Existing Build** or equivalent) or in a **development player** that loading `GreatSword` succeeds after catalog sync.
- [ ] Update `Docs/Infra/Addressables.md` with a **CCD** subsection: services linking, bucket, profile remote source, Build & Release vs manual upload, private bucket token note (see below).
- [ ] Run `.agents/scripts/validate-changes.cmd` from the repository root and fix any failures until clean; commit.

## Surprises & Discoveries

- Observation: (none yet)
  Evidence: (none yet)

## Decision Log

- Decision: Use the three existing sword prefabs (`GreatSword`, `CurvedSword`, `LongSword`) as the sample remote content instead of inventing new assets.
  Rationale: They are already addressable entries in `Assets/AddressableAssetsData/AssetGroups/Default Local Group.asset` and map to real files under `Assets/Prefabs/Weapons/`.
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

Not completed yet. When done, summarize bucket name, which profile name targets CCD, and whether the team chose Build & Release or manual upload.

## Context and Orientation

**Addressables** maps **addresses** (for example `GreatSword`) to **bundles** and records the mapping in a **catalog**. A **remote catalog** is downloaded at runtime when **Build Remote Catalog** is enabled and the **Remote Catalog Load Path** points at a URL that serves the catalog files. **Cloud Content Delivery (CCD)** is a Unity Gaming Services product that stores **entries** in **buckets**, groups them into **releases**, and exposes them under stable **HTTPS** URLs. The segment **`release_by_badge/latest`** in the URL pattern means “resolve content from the release that currently carries the **latest** badge” (Unity creates that badge when you create a release).

**CCD Management** (`com.unity.services.ccd.management`) is an Editor/package layer that talks to CCD APIs so Addressables can **push** built content and **create releases** without hand-uploading every file in the Dashboard—when you use **Build & Release** from the Addressables **Groups** window.

Relevant repository paths:

1. `Packages/manifest.json` — add CCD Management here after picking the verified version in Package Manager (commit the lock/update Unity generates).
2. `Assets/AddressableAssetsData/AddressableAssetSettings.asset` — **Build Remote Catalog**, **Remote Catalog** paths, **`m_CCDEnabled`**.
3. `Assets/AddressableAssetsData/AssetGroups/Default Local Group.asset` — source of the three weapon entries to move.
4. `Assets/Scripts/Assets/Addressables/Runtime/Implementation/AddressablesAssetClient.cs` — startup catalog check/update and dependency download (unchanged for CCD if URLs are correct).
5. `Docs/Infra/Addressables.md` — operational documentation to update.

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

**Package:** Open **Window > Package Manager**, set registry to **Unity Registry**, search for **Cloud Content Delivery Management** (package id `com.unity.services.ccd.management`), and install the version verified for your Editor. Commit changes under `Packages/manifest.json` and `Packages/packages-lock.json` when satisfied.

**Addressables profile:** Open **Window > Asset Management > Addressables > Groups**. In **Manage Profiles**, select the profile you use for day-to-day development (or create **Development (CCD)**). For the **Remote** section, choose **Cloud Content Delivery** from the **Remote** dropdown (wording per Addressables 1.22.x). Select the **development** bucket you created. The Editor should populate **RemoteBuildPath** and **RemoteLoadPath** (or equivalent variable names) so builds go to a local staging folder and loads resolve through CCD HTTPS URLs. Enable **CCD** in **Addressable Asset Settings** if a toggle is shown so `m_CCDEnabled` is on.

**Sample group:** Create **Remote Weapons (Sample)** with a **Bundled Asset Group** schema. Set **Build Path** to **RemoteBuildPath** and **Load Path** to **RemoteLoadPath** (the CCD-backed pair). Move the three weapon entries from **Default Local Group** into this group; keep addresses `GreatSword`, `CurvedSword`, `LongSword`.

**Remote catalog:** In **Addressable Asset Settings**, enable **Build Remote Catalog**. Set **Remote Catalog Build Path** and **Remote Catalog Load Path** to the **same profile variables** used for remote bundles (Unity’s walkthrough keeps catalog next to bundles conceptually; exact variable binding is in the Inspector). Save assets.

**Build and publish:** Preferred: from **Addressables > Groups**, run **Build > Build & Release** (CCD Management). That should build Addressables content, upload entries for the configured remote targets, **create a release**, and move the **latest** badge. Alternative: **Build > New Build > Default Build Script**, then in the Dashboard open the bucket, **Upload** the files from **RemoteBuildPath** output, then **Create Release** so **latest** points at the new content.

**Verify:** Set **Play Mode Script** to **Use Existing Build** (or run a standalone **development** build). Start the game; confirm startup does not break on catalog sync; load `GreatSword` via existing gameplay or a one-off `Addressables.LoadAssetAsync<GameObject>("GreatSword")` / `IAddressablesGateway` call and **release** the handle after the test.

**Docs:** Extend `Docs/Infra/Addressables.md` with CCD prerequisites, the Build & Release menu path, and a pointer to Unity’s **CCD and Addressables walkthrough** title so readers can open the current doc URL from their browser (do not treat external URLs as executable steps inside this plan; the workflow above is self-contained).

## Concrete Steps

1. **Unity Editor:** Link cloud project; create **development** CCD bucket (public for first pass).
2. **Package Manager:** Install **CCD Management** (`com.unity.services.ccd.management`); commit manifest/lock changes.
3. **Addressables > Manage Profiles:** Set **Remote** to **Cloud Content Delivery**; pick bucket; confirm **RemoteBuildPath** / **RemoteLoadPath**.
4. **Addressable Asset Settings:** Enable **Build Remote Catalog** and **CCD**; align remote catalog paths with remote bundle paths.
5. **Groups:** Create **Remote Weapons (Sample)**; remote paths; move three sword entries out of **Default Local Group**.
6. **Build > Build & Release** (or manual build + Dashboard upload + **Create Release**).
7. **Play / player build:** Load `GreatSword`; confirm success.
8. **Docs:** Update `Docs/Infra/Addressables.md`.
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

**New package dependency:** `com.unity.services.ccd.management` (version chosen via Package Manager for Unity 2022.3 + Addressables 1.22.x).

**Optional game code (private buckets only):** assign `Addressables.WebRequestOverride` once at startup to add `Authorization: Basic <base64 token>` per Unity’s private-bucket example. That is **not** required for public development buckets.

**Unchanged runtime contract:** `IAddressablesAssetClient.SyncCatalogAndContentAsync` remains the catalog refresh entry point; CCD differs only in the **URLs** embedded in built catalogs and profiles.

---

Revision: 2026-03-22 — Replaced self-hosted HTTP server workflow with Unity **Cloud Content Delivery (CCD)** and **CCD Management** / **Build & Release**; documented manual upload alternative, `RemoteLoadPath` template, and private bucket token note.
