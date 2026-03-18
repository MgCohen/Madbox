# Meta Gold

## Purpose

`Madbox.Gold` provides a minimal wallet model for meta progression currency.

## Public API

- `GoldWallet(int initialGold = 0)`
- `int CurrentGold { get; }`
- `bool TrySpend(int amount)`
- `void Add(int amount)`

## Usage Example

```csharp
GoldWallet wallet = new GoldWallet(initialGold: 10);
wallet.Add(5); // 15
bool spent = wallet.TrySpend(7); // true, balance = 8
```

## Design Notes

- `TrySpend` is non-throwing for invalid or unaffordable spend requests and returns `false`.
- `Add` validates positive input and throws on invalid amounts.
- The module has no `UnityEngine` dependency and is testable as pure C#.
