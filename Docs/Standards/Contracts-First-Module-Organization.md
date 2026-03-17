# Unified Runtime Module Organization

## Why this guide exists

This guide defines the current module organization standard for Madbox after consolidating split contracts/runtime assemblies into a single primary module assembly.

## Goals

- Make cross-module dependencies obvious through module-root assembly names and explicit asmdef references.
- Keep public API surfaces small, intentional, and stable.
- Prevent implementation internals from leaking across module boundaries.
- Keep Unity-specific concerns out of core/domain boundaries unless explicitly required.

## Standard module shape

Use this baseline structure for each module under `Assets/Scripts/<Layer>/<Module>/`:

- `Runtime/`
- `Runtime/Contracts/` (recommended when boundary types exist)
- `Container/` (optional; when the module has DI wiring)
- `Editor/` (optional)
- `Tests/`
- `Samples/` (optional)

Baseline assembly naming:

- `<Module>`
- `<Module>.Container` (optional)
- `<Module>.Editor` (optional)
- `<Module>.Tests`
- `<Module>.Samples` (optional)

## What belongs in boundary contracts

`Runtime/Contracts/` should include boundary-safe types:

- Public interfaces consumed by other modules.
- Public models used for cross-module communication.
- Public events and event payloads used across module boundaries.
- Enums and value types required by the public boundary.

Boundary contracts should not include:

- Concrete service implementations.
- Internal orchestration classes, systems, or stateful managers.
- MonoBehaviours unless the module is explicitly Unity-presentation-facing.
- Tooling/editor-only helpers.

## What belongs in runtime implementation

`Runtime/` and `<Module>` contain concrete behavior and internal mechanics:

- Service implementations.
- Internal systems and orchestration.
- Internal adapters to infrastructure.
- Internal state and lifecycle coordination.

Recommended internal organization inside runtime:

- `Runtime/Internal/Services`
- `Runtime/Internal/Systems`
- `Runtime/Internal/Adapters` (optional)

Use `internal` visibility by default for non-boundary types.

## Dependency direction rules

- External consumer modules depend on `<Module>` for both contracts and implementation.
- Composition roots (for example `App/Bootstrap`) may depend on container/runtime modules to register implementations.
- Non-bootstrap modules should still avoid foreign legacy `*.Runtime` assemblies.

## Public API and versioning discipline

Treat public boundary contracts as a product surface:

- Prefer additive changes over breaking changes.
- Keep boundary models immutable or effectively immutable where possible.
- Avoid exposing framework-specific details unless required by consumers.
- Document every public contract change in the matching module doc under `Docs/`.

## Analyzer and quality enforcement

Use analyzers to make this organization enforceable, not optional:

- Flag forbidden references to legacy runtime implementation assemblies from non-bootstrap modules.
- Flag boundary interfaces/types declared outside a `Contracts` path segment.
- Flag concrete implementation types accidentally made public when they should be internal.
- Keep analyzer diagnostics clean as part of milestone completion.

Run the quality gate from repository root:

`& ".\.agents\scripts\validate-changes.cmd"`

## Documentation checklist for module owners

When creating or splitting a module, update docs in `Docs/` with:

- Module purpose and ownership.
- Public contracts list (interfaces/models/events).
- Usage examples for consumers.
- Non-obvious design decisions and tradeoffs.
- Testing strategy and how to run relevant tests.

## Migration strategy (while refactor is in progress)

When consolidating a split module:

1. Move top-level `Contracts/` source files under `Runtime/Contracts/`.
2. Merge `*.Contracts` and `*.Runtime` asmdefs into `<Module>`.
3. Replace downstream asmdef references from `*.Contracts` or `*.Runtime` to `<Module>`.
4. Mark non-boundary implementation types `internal` where safe.
5. Update analyzer rules/config and tests for the new topology.
6. Run validation gate and fix failures before moving to next module.
