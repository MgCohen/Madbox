# App MainMenu

## TL;DR

- Purpose: minimal Main Menu slice to validate bootstrap startup, DI, navigation + addressables view loading, and MVVM bind propagation for gold display.
- Location: `Assets/Scripts/App/MainMenu/`.
- Depends on: `Scaffold.MVVM.View`, `Scaffold.MVVM.ViewModel`, `Madbox.Gold`, `VContainer`, `Scaffold.Navigation` (ViewModel base).

## Responsibilities

- Owns menu-local viewmodel/view flow for displaying current gold backed by `GoldWallet`.
- Owns button-to-viewmodel command routing for `+1` gold.
- Does not own economy rules beyond invoking `IGoldService`.
- Does not start battles; the legacy game view module was removed from the project.

## Public API

- `MainMenuViewModel.AddOneGold()`: increments gold through injected `IGoldService`.
- `MainMenuView`: binds `viewModel.Gold` to TextMeshPro text and the Add Gold button to `AddOneGold`.

## Testing

- EditMode test assembly: `Madbox.MainMenu.Tests`.
- Run:

```powershell
& ".\.agents\scripts\run-editmode-tests.ps1" -AssemblyNames "Madbox.MainMenu.Tests"
```

## Notes

- All menu text uses `TextMeshProUGUI`.
- Main menu is opened by `BootstrapScope.OnBootstrapCompleted(...)`.
