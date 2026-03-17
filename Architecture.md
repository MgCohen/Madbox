# Architecture

This document describes the current repository architecture and where architectural controls live.

## TL;DR

- The project now has a modular runtime under `Assets/Scripts/` with explicit `.asmdef` boundaries.
- Current structure is split into `App`, `Core`, `Infra`, and `Tools` modules, with tests/samples per module.
- Architecture guardrails are enforced by docs standards, `.asmdef` dependencies, and custom analyzers.

## Architectural Drivers

- Keep module boundaries explicit and enforceable through `.asmdef` references.
- Keep core logic isolated from Unity-specific presentation concerns.
- Keep validation fast and repeatable with analyzer checks and scripted quality gates.

## Project Summary

Madbox is a Unity project with architecture constraints enforced through:
- documentation standards under `Docs/Standards/`
- custom Roslyn analyzers under `Analyzers/`
- repository quality scripts under `.agents/scripts/`
- assembly boundaries under `Assets/Scripts/**/*.asmdef`

Current state updated on 2026-03-17.

## Tech Stack

- **Engine**: Unity `2022.3.50f1`
- **Language**: C#
- **Architecture**: MVVM
- **Dependency Injection**: VContainer (`jp.hadashikick.vcontainer`)
- **Rendering**: Universal Render Pipeline (URP)
- **Key Packages**: Addressables, AI Navigation, Cinemachine, TextMeshPro, Unity Test Framework, Scaffold Schemas (`com.scaffold.schemas`)
- **Code generation in project**: `Assets/Generators/MVVM` (MVVM source generator binaries used by Unity-generated projects)
- **Code quality enforcement**: Roslyn analyzers (`Analyzers/Scaffold/Scaffold.Analyzers`)

## Current Repository Map

Top-level directories:
- `Assets/`: Unity assets, scenes, prefabs, data, generators, and scripts.
- `Analyzers/`: Analyzer source, tests, and output dll folder.
- `Docs/`: Module and standards documentation.
- `Research/`: Research artifacts.
- `Plans/`: ExecPlans and milestone planning artifacts.
- `.agents/`: Workflows and quality/test scripts.
- `Packages/`: Unity package manifest/lock.
- `ProjectSettings/`: Unity project configuration.
- `UserSettings/`: Local Unity user settings.

## Module View (Current State)

`Assets/Scripts/` has 32 assembly definitions:

- `App`
  - `Madbox.Bootstrap.Runtime`, `Madbox.Bootstrap.Tests`, `Madbox.Bootstrap.PlayModeTests`
  - `Scaffold.MVVM.View`, `Scaffold.MVVM.Samples`, `Scaffold.MVVM.View.Tests`
- `Core`
  - `Scaffold.MVVM.ViewModel`, `Scaffold.MVVM.ViewModel.Tests`
- `Infra`
  - Events: `Scaffold.Events.Contracts`, `Scaffold.Events.Runtime`, `Scaffold.Events.Container`, `Scaffold.Events.Samples`, `Scaffold.Events.Tests`
  - Model: `Scaffold.MVVM.Model`, `Scaffold.MVVM.Model.Tests`
  - Navigation: `Scaffold.Navigation.Contracts`, `Scaffold.Navigation.Runtime`, `Scaffold.Navigation.Container`, `Scaffold.Navigation.Samples`, `Scaffold.Navigation.Tests`
  - Scope: `Scaffold.Scope.Contracts`, `Scaffold.Scope.Runtime`, `Scaffold.Scope.Tests`
- `Tools`
  - Maps: `Scaffold.Maps`, `Scaffold.Maps.Samples`, `Scaffold.Maps.Tests`
  - Records: `Scaffold.Records`, `Scaffold.Records.Samples`, `Scaffold.Records.Tests`
  - Types: `Scaffold.Types`, `Scaffold.Types.Editor`, `Scaffold.Types.Samples`, `Scaffold.Types.Tests`

Primary production dependency direction:

- `Scaffold.MVVM.Model` <- `Scaffold.MVVM.ViewModel` <- `Scaffold.MVVM.View` <- `Madbox.Bootstrap.Runtime`
- `Scaffold.Navigation.Contracts` <- `Scaffold.MVVM.ViewModel` / `Scaffold.MVVM.View` / `Madbox.Bootstrap.Runtime`
- `Scaffold.Navigation.Runtime` <- `Madbox.Bootstrap.Runtime`
- `Scaffold.Scope.Contracts` <- `Scaffold.Scope.Runtime` <- `Madbox.Bootstrap.Runtime`
- `Scaffold.Events.Contracts` <- `Scaffold.Events.Runtime` <- `Scaffold.Events.Container` <- `Madbox.Bootstrap.Runtime`
- `Scaffold.Records` <- `Scaffold.Maps` <- `Scaffold.MVVM.ViewModel`
- `Scaffold.Types` <- `Scaffold.MVVM.View` and `Scaffold.Navigation.Runtime`
- `Scaffold.Schemas` (package assembly from `com.scaffold.schemas`) <- `Scaffold.Navigation.Runtime`
- `VContainer` / `VContainer.Unity` are consumed by `Scaffold.Scope.*`, `Scaffold.Navigation.Container`, `Scaffold.Events.Container`, and `Madbox.Bootstrap.Runtime`

Notes:
- Tests are split into Editor tests and PlayMode tests where applicable.
- Sample assemblies exist for View, Events, Navigation, Maps, Records, and Types modules.

## Runtime Flows (Current State)

Entry/content roots:
- Scenes: `Assets/Scenes/Bootstrap.unity`, `Assets/Scenes/MainScene.unity`
- Content roots: `Assets/Art/`, `Assets/Prefabs/`, `Assets/AddressableAssetsData/`, `Assets/Data/`
- Runtime scripts root: `Assets/Scripts/`

MVVM flow (high level):
- Model contracts/data: `Infra/Model`
- ViewModel orchestration: `Core/ViewModel`
- View/presentation: `App/View`
- App startup/composition root: `App/Bootstrap`

## Dependency Rules

Allowed:
- Dependencies declared explicitly in `.asmdef` files.
- Core/domain logic staying Unity-agnostic where possible.
- Container/bootstrap assemblies referencing framework dependencies (for example VContainer).

Forbidden:
- Hidden dependencies that bypass declared assembly references.
- MonoBehaviours in core/domain layers.
- Direct runtime reliance on analyzer projects.

## Docs Map (Current Files)

- `Docs/App/Bootstrap.md`
- `Docs/App/View.md`
- `Docs/Core/ViewModel.md`
- `Docs/Infra/Events.md`
- `Docs/Infra/Model.md`
- `Docs/Infra/Navigation.md`
- `Docs/Infra/Scope.md`
- `Docs/Tools/Maps.md`
- `Docs/Tools/Records.md`
- `Docs/Tools/Types.md`
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
- Assembly boundaries: `Assets/Scripts/**/*.asmdef`

Operational policy and workflows:
- `AGENTS.md`
- `PLANS.md`
- `MILESTONE.md`
- `.agents/workflows/create-module.md`
- `.agents/workflows/create-custom-analyzer.md`
- `.agents/workflows/check-analyzers.md`
- `.agents/workflows/coverage-audit.md`

## Quality Loop

Milestone gate (from repository root):

1. `& ".\.agents\scripts\validate-changes.cmd"`

Related scripts:
- `.agents/scripts/run-editmode-tests.ps1`
- `.agents/scripts/run-playmode-tests.ps1`
- `.agents/scripts/check-analyzers.ps1`
- `.agents/scripts/check-unity-compilation.ps1`
- `.agents/scripts/check-scripts-asmdef-references.ps1`
- `.agents/scripts/validate-changes.ps1`
- `.agents/scripts/run-coverage-audit.cmd`
- `.agents/scripts/run-coverage-audit.ps1`

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

- 2026-03-17: Replaced outdated fresh-project architecture notes with the current modular `Assets/Scripts` assembly map, dependency graph, docs inventory, and quality tooling.
