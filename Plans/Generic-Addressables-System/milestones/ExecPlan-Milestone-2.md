# Milestone 2 - Catalog Update, Content Download, and CDN Readiness

## Goal

Extend the generic gateway internals so initialization can update addressable catalogs, evaluate download size, and download required new content while preserving the same consumer API from Milestone 1. This milestone adds live-content readiness and CDN support without exposing update/download mechanics to feature modules.

At completion, runtime can start with up-to-date catalog/content according to policy, and failures are surfaced as controlled errors/telemetry instead of raw engine exceptions.

## Deliverable

1. Internal update/download coordinator integrated into gateway initialization path.
2. CDN-capable configuration model for environment-specific remote catalog/content endpoints.
3. Internal policies for update timing, retry behavior, and non-critical failure handling.
4. EditMode tests for successful and failing catalog update/download scenarios.
5. Documentation updates describing operational behavior and fallback strategy.

## Plan

1. Add internal services that perform:
   - check for catalog updates,
   - apply updates when available,
   - compute download sizes,
   - download dependencies required by current policy.
2. Keep `IAddressablesGateway` unchanged; initialization triggers update/download flow internally.
3. Add CDN/environment configuration abstraction in Infra so remote paths are controlled centrally.
4. Define failure policy:
   - critical failures block startup only when core-required content is unavailable,
   - non-critical failures degrade gracefully and emit telemetry.
5. Add tests using fake adapter layers to validate:
   - update available and download succeeds,
   - no updates available path,
   - download failure path with controlled result,
   - retry path behavior where configured.
6. Re-run milestone quality loop:
   - `powershell -NoProfile -ExecutionPolicy Bypass -File ".\.agents\scripts\run-editmode-tests.ps1"`
   - `powershell -NoProfile -ExecutionPolicy Bypass -File ".\.agents\scripts\run-playmode-tests.ps1"`
   - `powershell -NoProfile -ExecutionPolicy Bypass -File ".\.agents\scripts\check-analyzers.ps1"`
   - `& ".\.agents\scripts\validate-changes.cmd"`
   - fix failures and rerun until clean.
7. Commit milestone changes.

## Snippets and Samples

Expected behavior example:

    await _addressablesGateway.InitializeAsync(ct);
    // Internal flow:
    // 1) check catalog
    // 2) update catalog if needed
    // 3) download required dependencies
    // 4) mark gateway ready for load requests

Expected test intent examples:

    InitializeAsync_WhenCatalogHasUpdates_AppliesUpdateAndDownloadsDependencies
    InitializeAsync_WhenDownloadFails_ReturnsControlledFailureAndTelemetry
    InitializeAsync_WhenNoCatalogUpdates_SkipsDownloadAndCompletesReady
