# Scaffold.MVVM.Base

## TL;DR

- Purpose: shared MVVM contract primitives reused by model, viewmodel, and view modules.
- Location: `Assets/Scripts/Infra/BaseMVVM/Runtime/`.
- Depends on: none.
- Used by: `Scaffold.MVVM.Model`, `Scaffold.MVVM.ViewModel`, `Scaffold.MVVM.View`.
- Runtime/Editor: runtime contract assembly only.

## Responsibilities

- Owns shared MVVM contract primitives (`INestedObservableProperties`, nested-observable attributes, adapter/converter abstractions).
- Owns lightweight binding options contract (`BindingOptions`) consumed by higher-level modules.
- Does not own runtime binding orchestration, registry internals, or Unity-facing presentation behavior.
- Boundaries: pure C# contract layer, no scene/runtime object ownership.

## Public API

| Symbol | Purpose | Inputs | Outputs | Failure behavior |
|---|---|---|---|---|
| `INestedObservableProperties` | Contract for registering nested observable members. | Registration requests from implementing type. | Exposes nested observable registration contract. | Misuse is compile-time/implementation concern, not runtime exception by contract. |
| `NestedObservableObjectAttribute` | Marks types that participate in nested observable processing. | Attribute placement on class/type. | Metadata consumed by analyzers/runtime integrations. | Invalid usage surfaces via analyzer or consumer validation. |
| `NestedPropertyAttribute` | Marks nested properties for observable traversal. | Attribute placement on property/member. | Metadata used by binding/observable consumers. | Invalid placement is surfaced by analyzer/consumer checks. |
| `Adapter<T>` | Defines target adaptation contract for bindings. | Source value/context from binder. | Adapted target value/type. | Adapter implementation decides guard behavior. |
| `Converter<TFrom, TTo>` | Defines value conversion contract. | `TFrom` source value. | `TTo` converted value. | Converter implementation decides guard behavior. |
| `BindingOptions` | Shared options contract for strict/lazy binding behavior. | Option flags/values. | Consistent options payload for consumers. | Invalid combinations are handled by consuming modules. |

## Setup / Integration

1. Add asmdef reference to `Scaffold.MVVM.Base` from modules that need MVVM base contracts.
2. Consume contract types from `Scaffold.MVVM.Binding` namespaces instead of re-defining equivalents.
3. Keep orchestration logic in dependent modules (`Model`, `ViewModel`, `View`) and keep this module contract-only.
4. Fast check: `Scaffold.MVVM.Base` should not require references to Unity-specific assemblies.

## How to Use

1. Implement `INestedObservableProperties` in model-like types that expose nested observable members.
2. Annotate relevant types/members with nested-observable attributes where registration metadata is needed.
3. Use `Adapter<>` and `Converter<,>` contracts for binding translation points in dependent modules.
4. Pass shared `BindingOptions` through higher-level bind setup to keep behavior consistent.

## Examples

### Minimal

```csharp
public sealed class PlayerStats : INestedObservableProperties
{
    // Module consumers implement registration semantics.
}
```

### Realistic

```csharp
public sealed class HealthToTextConverter : Converter<int, string>
{
    public override string Convert(int value) => $"HP: {value}";
}
```

### Guard / Error path

```csharp
public sealed class StrictPositiveConverter : Converter<int, int>
{
    public override int Convert(int value)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(value));
        }

        return value;
    }
}
```

## Best Practices

- Keep this module free of runtime orchestration and Unity object concerns.
- Prefer extending existing base contracts over adding parallel contract types.
- Treat attributes here as boundary metadata and validate usage in analyzers or consumer modules.
- Keep contracts stable to avoid ripple changes across ViewModel/View modules.
- Use analyzer checks after contract changes to catch boundary regressions quickly.

## Anti-Patterns

- Putting bind execution logic inside this module.
  Migration: move runtime behavior into `Scaffold.MVVM.Model`/`Scaffold.MVVM.ViewModel`/`Scaffold.MVVM.View`.
- Adding Unity dependencies here for convenience.
  Migration: expose contracts in BaseMVVM and implement Unity-facing behavior in App/Infra modules.
- Duplicating adapter/converter abstractions in dependent modules.
  Migration: reuse `Adapter<>` and `Converter<,>` from this module.

## Testing

- Test assemblies: covered by dependent module tests (`Scaffold.MVVM.Model.Tests`, `Scaffold.MVVM.ViewModel.Tests`, `Scaffold.MVVM.View.Tests`) and analyzer tests.
- Run:

```powershell
dotnet test "Analyzers/Scaffold/Scaffold.Analyzers.Tests/Scaffold.Analyzers.Tests.csproj" -c Release --nologo
& ".\.agents\scripts\validate-changes.cmd"
```

- Expected pass signal: analyzer tests pass and the validation gate reports no failures/diagnostics.
- Bugfix rule: add/update a regression test in the affected consuming module before applying the fix.

## AI Agent Context

- Invariants:
  - `Scaffold.MVVM.Base` remains a contract-only assembly.
  - Types in this module stay reusable by both View and ViewModel paths.
- Allowed Dependencies:
  - BCL only.
- Forbidden Dependencies:
  - `UnityEngine` and Unity presentation assemblies.
  - Runtime implementation assemblies that would invert MVVM boundaries.
- Change Checklist:
  - Update this doc for any API/contract changes.
  - Run analyzer tests and validation gate.
  - Verify dependent modules still compile/reference unchanged symbols.
- Known Tricky Areas:
  - Attribute semantics can drift from analyzer expectations.
  - Generic adapter/converter signature changes cascade broadly.

## Related

- `Docs/Infra/Model.md`
- `Docs/Core/ViewModel.md`
- `Docs/App/View.md`
- `Architecture.md`
- `Docs/Testing.md`

## Changelog

- 2026-03-18: Reworked to module documentation standard sections, added usage/examples/anti-pattern/testing and AI context details.
- 2026-03-17: Initial module baseline authored for MVVM base contracts.
