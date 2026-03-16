# Architecture

This document describes the current repository architecture and where architectural controls live.

## TL;DR

- This is a fresh Unity baseline project.
- Runtime/gameplay module assemblies are not created yet.
- Repository guardrails currently come from analyzer projects, docs standards, and validation scripts.

## Architectural Drivers

- Keep future module boundaries explicit and enforceable through `.asmdef` dependencies.
- Keep gameplay/core logic isolated from Unity presentation concerns as modules are added.
- Preserve fast validation loops with scripted checks and analyzer feedback.

## Project Summary

Madbox is a Unity repository with architecture constraints enforced through:
- documentation standards under `Docs/Standards/`
- custom Roslyn analyzers under `Analyzers/`
- repository quality scripts under `.agents/scripts/`

As of 2026-03-16, the project does not yet include a runtime module tree under `Assets/Scripts/`.

## Tech Stack

- **Engine**: Unity 2022.3.17f1
- **Language**: C#
- **Rendering**: Universal Render Pipeline (URP)
- **Packages in use**: Addressables, AI Navigation, Cinemachine, TextMeshPro, Unity Test Framework
- **Code quality enforcement**: Roslyn analyzers (`Analyzers/Scaffold/Scaffold.Analyzers`)

## Current Repository Map

Top-level directories:
- `Assets/`: Unity assets, scenes, prefabs, settings, and addressables data.
- `Analyzers/`: Analyzer source and analyzer test projects.
- `Docs/`: Documentation and standards.
- `Plans/`: ExecPlans and milestone planning artifacts.
- `.agents/`: AI workflows and validation/test/analyzer scripts.
- `Packages/`: Unity package manifest/lock.
- `ProjectSettings/`: Unity project configuration.
- `UserSettings/`: Local Unity user settings.

## Module View (Current State)

Intent: show static module/assembly dependency direction.

Current state in this checkout (2026-03-16):
- No `Assets/Scripts/` directory exists yet.
- No `.asmdef` files are present yet.
- No generated Unity runtime `*.csproj` files are present at repository root.

Implication:
- Runtime module dependency graph is **not established yet**.
- When modules are introduced, update this section with real `.asmdef` dependencies.

## Runtime Flows (Current State)

Runtime MVVM/gameplay flow documentation is deferred until first runtime modules are added.

Current content-focused baseline:
- Scene: `Assets/Scenes/MainScene.unity`
- Content roots: `Assets/Art/`, `Assets/Prefabs/`, `Assets/AddressableAssetsData/`

## Dependency Rules (Target Rules for New Modules)

Allowed:
- Dependencies declared explicitly in `.asmdef` files.
- Core/domain logic staying Unity-agnostic where possible.

Forbidden:
- Hidden dependencies that bypass declared assembly references.
- MonoBehaviours in core/domain layers.
- Direct runtime reliance on analyzer projects.

## Docs Map (Current Files)

- `Docs/Analyzers/Analyzers.md`
- `Docs/Testing.md`
- `Docs/AutomatedTesting.md`
- `Docs/Standards/Architecture-Documentation-Standard.md`
- `Docs/Standards/Contracts-First-Module-Organization.md`
- `Docs/Standards/Module-Documentation-Standard.md`

## Architecture Controls

- Analyzer source: `Analyzers/Scaffold/Scaffold.Analyzers`
- Analyzer tests: `Analyzers/Scaffold/Scaffold.Analyzers.Tests`
- Analyzer output target: `Analyzers/Output/Scaffold.Analyzers.dll`
- Analyzer wiring: `Directory.Build.props`

Operational policy and workflows:
- `AGENTS.md`
- `PLANS.md`
- `MILESTONE.md`
- `.agents/workflows/create-module.md`
- `.agents/workflows/create-custom-analyzer.md`
- `.agents/workflows/check-analyzers.md`

## Quality Loop

Milestone gate (from repository root):

1. `& ".\.agents\scripts\validate-changes.cmd"`

Related scripts:
- `.agents/scripts/run-editmode-tests.ps1`
- `.agents/scripts/run-playmode-tests.ps1`
- `.agents/scripts/check-analyzers.ps1`

## Verification

- Full quality gate:
  - `& ".\.agents\scripts\validate-changes.cmd"`
- Analyzer-only gate:
  - `powershell -NoProfile -ExecutionPolicy Bypass -File ".\.agents\scripts\check-analyzers.ps1"`
- EditMode tests:
  - `powershell -NoProfile -ExecutionPolicy Bypass -File ".\.agents\scripts\run-editmode-tests.ps1"`
- PlayMode tests:
  - `powershell -NoProfile -ExecutionPolicy Bypass -File ".\.agents\scripts\run-playmode-tests.ps1"`

## Change Log

- 2026-03-16: Removed stale Scaffold runtime/module references and aligned architecture docs with current fresh-project filesystem state.
