# Scaffold.MVVM.Base

## TL;DR

- Purpose: shared MVVM contract primitives reused by Model, ViewModel, and View modules.
- Location: `Assets/Scripts/Infra/BaseMVVM/Runtime/`.
- Depends on: none.
- Used by: `Scaffold.MVVM.Model`, `Scaffold.MVVM.ViewModel`, `Scaffold.MVVM.View`, related tests/samples.
- Runtime/Editor: contracts-only assembly (`Scaffold.MVVM.Base`).

## Responsibilities

- Owns nested-observable contracts and attributes:
  - `INestedObservableProperties`
  - `NestedObservableObjectAttribute`
  - `NestedPropertyAttribute`
- Owns shared generic binding contracts:
  - `Adapter<>`
  - `Converter<,>`
  - `BindingOptions`
- Does not own binding engine execution flow (`TreeBinding`, `BindContext`, registries) or Unity view behavior.

## Public API

| Symbol | Purpose |
|---|---|
| `INestedObservableProperties` | contract for registering nested observable members |
| `NestedObservableObjectAttribute` | marks classes for nested observable registration |
| `NestedPropertyAttribute` | marks nested members for registration |
| `Adapter<>` | bind target adaptation contract |
| `Converter<,>` | bind value conversion contract |
| `BindingOptions` | strict/lazy binding option contract |

## Setup / Integration

1. Add asmdef reference to `Scaffold.MVVM.Base`.
2. Use `Scaffold.MVVM.Binding` types from this module.
3. Keep the module boundary-safe: no runtime orchestration code and no Unity scene logic.

## Testing

- Validate through dependent module tests and full gate:

```powershell
dotnet test Analyzers/Scaffold/Scaffold.Analyzers.Tests/Scaffold.Analyzers.Tests.csproj -c Release --nologo
& ".\.agents\scripts\validate-changes.cmd"
```

## AI Agent Context

- Invariants:
  - types here must remain reusable by both View and ViewModel bind-source paths.
  - module must stay independent from binding engine runtime internals.
- Allowed dependencies:
  - none.
- Forbidden dependencies:
  - `Scaffold.MVVM.ViewModel` runtime implementation and Unity presentation/runtime flow code.

## Related

- `Docs/Infra/Model.md`
- `Docs/Core/ViewModel.md`
- `Docs/App/View.md`
- `Architecture.md`
