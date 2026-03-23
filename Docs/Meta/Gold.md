# Meta Gold

## TL;DR

- Purpose: provides a minimal gold wallet model for meta progression currency.
- Location: `Assets/Scripts/Meta/Gold/Runtime/` and `Assets/Scripts/Meta/Gold/Authoring/`.
- Depends on: BCL only.
- Used by: battle reward paths and menu/progression orchestration through meta services.
- Runtime/Editor: pure C# runtime domain module plus module-local Unity authoring config asset.

## Responsibilities

- Owns wallet state and mutation rules for gold balance updates.
- Owns guard behavior for invalid add/spend requests.
- Owns authoring config asset for editor-time initial gold configuration (`GoldConfigSO`).
- Does not own reward policy decisions, UI display logic, or persistence/remote sync adapters.
- Boundaries: pure domain behavior, no Unity runtime concerns.

## Public API

| Symbol | Purpose | Inputs | Outputs | Failure behavior |
|---|---|---|---|---|
| `GoldWallet(int initialGold = 0)` | Creates wallet with initial balance. | Optional initial balance value. | Wallet instance with `CurrentGold` state. | Throws if constructor guards reject invalid initial value (if enforced by implementation). |
| `int CurrentGold { get; }` | Exposes current wallet balance. | None. | Current integer gold amount. | No failure path. |
| `bool TrySpend(int amount)` | Attempts to spend gold without throwing on insufficient funds. | Spend amount. | `true` when spend succeeds; `false` otherwise. | Returns `false` for invalid/unaffordable requests. |
| `void Add(int amount)` | Adds positive amount to wallet. | Amount to add. | Updated balance state. | Throws on invalid amount input. |

## Setup / Integration

1. Add asmdef reference to `Madbox.Gold` from consumers that need wallet behavior.
2. Inject or compose wallet usage from meta/game services; keep direct view coupling out of this module.
3. Keep reward policy in caller modules and use wallet as the stateful currency boundary.
4. Fast check: module should compile/run without Unity-specific references.

## How to Use

1. Construct `GoldWallet` with an optional starting balance.
2. Apply rewards using `Add`.
3. Check/spend costs using `TrySpend` and branch on the result.
4. Surface resulting balance in caller modules (menu/viewmodel) rather than embedding presentation logic here.

## Examples

### Minimal

```csharp
GoldWallet wallet = new GoldWallet();
wallet.Add(10);
```

### Realistic

```csharp
GoldWallet wallet = new GoldWallet(initialGold: 25);
wallet.Add(15);              // 40
bool purchased = wallet.TrySpend(30); // true, balance 10
```

### Guard / Error path

```csharp
GoldWallet wallet = new GoldWallet(initialGold: 5);
bool spent = wallet.TrySpend(10); // false, balance unchanged

Assert.IsFalse(spent);
Assert.AreEqual(5, wallet.CurrentGold);
```

## Best Practices

- Keep all gold mutation through wallet API to preserve invariants.
- Prefer `TrySpend` for gameplay/menu purchase paths to avoid exception-driven control flow.
- Keep this module free from persistence and transport details.
- Add regression tests when changing guard behavior or arithmetic rules.
- Keep API stable because many modules consume currency semantics.

## Anti-Patterns

- Mutating balance directly from consumer code.
  Migration: route all writes through `Add` and `TrySpend`.
- Embedding UI/presentation responsibilities in wallet types.
  Migration: expose balance via callers (viewmodel/controller).
- Adding external SDK dependencies into meta wallet logic.
  Migration: put SDK adapters in infra modules and call wallet through contracts.

## Testing

- Test assembly: `Madbox.Gold.Tests`.
- Run:

```powershell
& ".\.agents\scripts\run-editmode-tests.ps1" -AssemblyNames "Madbox.Gold.Tests"
```

- Expected pass signal: all `Madbox.Gold.Tests` pass with zero failures.
- Bugfix rule: add/update a regression test that fails before and passes after the fix.

## AI Agent Context

- Invariants:
  - Wallet balance must remain coherent after every operation.
  - Spend failures must not mutate state.
- Allowed Dependencies:
  - BCL only.
- Forbidden Dependencies:
  - `UnityEngine` and presentation modules.
  - Infra adapters (persistence/network) in wallet domain code.
- Change Checklist:
  - Add/adjust tests for any balance rule changes.
  - Verify callers still compile against stable API.
  - Run module tests and analyzer/validation checks.
- Known Tricky Areas:
  - Constructor and add/spend guard semantics must stay consistent across callers.
  - Silent behavior changes in spend failure paths can break purchasing flows.

## Related

- `Docs/App/Battle.md`
- `Docs/App/View.md`
- `Architecture.md`
- `Docs/Testing.md`

## Changelog

- 2026-03-18: Reworked to module documentation standard, including full API table, setup/usage/examples/testing, and AI context.
- 2026-03-17: Initial minimal wallet documentation.
- 2026-03-18: Added note for module-local gold authoring asset location under `Meta/Gold/Authoring`.
