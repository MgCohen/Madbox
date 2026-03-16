# Contracts-First Module Organization

## Why this guide exists

This guide defines a stable module organization standard for Madbox while major refactors are still in progress. It is intended to make boundaries explicit, reduce accidental coupling, and help both humans and AI agents navigate module intent quickly.

## Goals

- Make cross-module dependencies obvious through assembly names and references.
- Keep public API surfaces small, intentional, and stable.
- Prevent implementation internals from leaking across module boundaries.
- Keep Unity-specific concerns out of core/domain boundaries unless explicitly required.

## Standard module shape

Use this baseline structure for each module under `Assets/Scripts/<Layer>/<Module>/`:

- `Contracts/`
- `Runtime/`
- `Container/` (optional; when the module has DI wiring)
- `Editor/` (optional)
- `Tests/`
- `Samples/` (optional)

Baseline assembly naming:

- `<Module>.Contracts`
- `<Module>.Runtime`
- `<Module>.Container` (optional)
- `<Module>.Editor` (optional)
- `<Module>.Tests`
- `<Module>.Samples` (optional)

## What belongs in contracts

`Contracts/` and `<Module>.Contracts` should include only boundary-safe types:

- Public interfaces consumed by other modules.
- Public models used for cross-module communication.
- Public events and event payloads used across module boundaries.
- Enums and value types required by the public boundary.

`Contracts/` should not include:

- Concrete service implementations.
- Internal orchestration classes, systems, or stateful managers.
- MonoBehaviours unless the module is explicitly Unity-presentation-facing.
- Tooling/editor-only helpers.

## What belongs in runtime implementation

`Runtime/` and `<Module>.Runtime` contain concrete behavior and internal mechanics:

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

- `<Module>.Runtime` depends on `<Module>.Contracts`.
- External consumer modules should depend on `<Module>.Contracts` only.
- Composition roots (for example `App/Bootstrap`) may depend on runtime modules to register implementations.
- Non-bootstrap modules should not depend on another module's runtime assembly.

## Public API and versioning discipline

Treat contracts as a product surface:

- Prefer additive changes over breaking changes.
- Keep boundary models immutable or effectively immutable where possible.
- Avoid exposing framework-specific details unless required by consumers.
- Document every public contract change in the matching module doc under `Docs/`.

## Analyzer and quality enforcement

Use analyzers to make this organization enforceable, not optional:

- Flag forbidden references to runtime implementation assemblies from non-bootstrap modules.
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

When implementation work is blocked or concurrent:

1. Define the target contracts surface in docs first.
2. Keep current runtime behavior stable.
3. Create top-level `Contracts/` and `Runtime/` folders with separate asmdefs.
4. Move boundary types into `Contracts/` in small batches and keep internals in `Runtime/`.
5. Add or update analyzer checks.
6. Run validation gate and fix failures before moving to next module.

This allows planning and alignment to continue without destabilizing in-flight development.
