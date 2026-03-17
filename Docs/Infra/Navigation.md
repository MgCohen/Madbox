# Scaffold Infra Navigation

## TL;DR

- Purpose: manage view-controller navigation stack and transitions.
- Location: `Assets/Scripts/Infra/Navigation/Contracts/` + `Assets/Scripts/Infra/Navigation/Runtime/`.
- Depends on: `Scaffold.Events.Contracts`, `Scaffold.Types`, `Scaffold.Records`, container abstractions.
- Used by: app screens and MVVM presentation flow.
- Runtime/Editor: runtime + container integration.
- Keywords: navigation stack, transitions, view config, middleware.

## Responsibilities

- Owns navigation contract (`INavigation`) and runtime implementation (`NavigationController`).
- Owns view-controller stack behavior (`NavigationStack`, `NavigationPoint`).
- Owns transition orchestration (`NavigationTransitions` and schemas).
- Owns DI integration (`NavigationInstaller`, `NavigationInjection`).
- Does not own app-specific business decisions or domain mutation logic.

## Public API

| Symbol | Purpose | Inputs | Outputs | Failure behavior |
|---|---|---|---|---|
| `INavigation.Open(...)` | Open target controller/view | controller + options | active navigation point | invalid config/path is ignored or guarded by provider checks |
| `INavigation.Close(...)` | Close a controller/view | controller | removed point or return transition | no-op when point not found |
| `INavigation.Return()` | Return to previous point | none | previous controller | guarded behavior when no previous point |
| `IViewController` | Controller lifecycle contract | `Bind(INavigation)` etc. | bound controller behavior | n/a |
| `IView` | View lifecycle contract | bind/open/hide/focus/close/order | runtime view behavior | state-specific operations may no-op |
| `NavigationInstaller` | Registers navigation services | container registry | navigation runtime wiring | fails when required contracts are unavailable |

## Setup / Integration

1. Reference `Scaffold.Navigation.Contracts` (and `Scaffold.Navigation.Runtime` for implementation/container wiring).
2. Configure `NavigationSettings` with controller/view mappings.
3. Register `NavigationInstaller` in composition root.
4. Open controllers through `INavigation`.

## How to Use

1. Implement controller type (`IViewController` or MVVM `ViewModel` descendant).
2. Implement view type (`IView` or MVVM view base).
3. Add `ViewConfig` mapping for controller/view.
4. Open/close/return with `INavigation`.

## Behavior Contracts

| Operation | Stack behavior | Transition behavior |
|---|---|---|
| `Open(controller, closeCurrent:false)` | current remains; new point appended and becomes current | previous point is typically hidden, then target opens/focuses |
| `Open(controller, closeCurrent:true)` | current removed before/while activating target | close sequence runs before target open |
| `Open(..., options.CloseAllViews=true)` | non-origin stacked points are removed/closed | target activation occurs after close sweep |
| `Close(current)` | equivalent to return to previous point | transition goes from current to previous |
| `Close(non-current)` | target point removed in place; current unchanged | close applies to removed point only |
| `Return()` | target is previous point; current removed | `GoTo(previous, closeCurrent:true, ...)` semantics |

- `NavigationTransitions.DoTransition(from, to, closeCurrent)` enqueues transitions and executes them serially.
- Default ordering is: close or hide `from` first, then open/focus `to`.
- `ViewConfig` resolution uses `NavigationSettings` mapping and may reuse context views under `viewHolder` before asset instantiation.
- Schema handlers:
- `TransitionViewSchema.Handler=Default` uses built-in close/hide/open flow.
- `TransitionViewSchema.Handler=Code` calls `IViewTransitionHandler.DoTransition(...)`.
- `AnimationViewSchema.Handler=Animator` plays configured state and waits for completion.
- `AnimationViewSchema.Handler=Code` calls `IViewAnimationHandler.AnimateView(...)`.

## Examples

### Open/Return Flow

```mermaid
sequenceDiagram
  participant App as Caller
  participant Nav as NavigationController
  participant Prov as NavigationProvider
  participant Stack as NavigationStack
  participant Tr as NavigationTransitions

  App->>Nav: Open(controller, options)
  Nav->>Prov: Resolve NavigationPoint
  Nav->>Stack: Push/replace point
  Nav->>Tr: DoTransition(from,to,closeCurrent)
  App->>Nav: Return()
  Nav->>Stack: PreviousPoint
  Nav->>Tr: DoTransition(current,previous,true)
```

### Minimal

```csharp
INavigation navigation = resolver.Resolve<INavigation>();
navigation.Open(new MainMenuViewController());
navigation.Return();
```

## Best Practices

- Keep navigation decisions in controllers/app orchestration.
- Use `NavigationOptions` explicitly for close-all/close-current behavior.
- Keep `ViewConfig` mappings complete and validated.
- Keep middleware focused on cross-cutting open behavior.

## Anti-Patterns

- Instantiating and toggling views directly outside navigation.
- Hiding navigation side effects in unrelated service layers.
- Mixing domain business rules into transition handlers.

## Testing

- Test assembly: `Scaffold.Navigation.Tests`.
- Run from repo root:

```powershell
& ".\.agents\scripts\run-editmode-tests.ps1" -AssemblyNames "Scaffold.Navigation.Tests"
```

- Expected: all tests pass with zero failures.
- Bugfix rule: add/update regression test first, verify fail-before/fix/pass-after.

## AI Agent Context

- Invariants:
  - stack order and current/previous semantics are preserved.
  - transitions maintain close/hide/open ordering.
  - controller-to-view mapping resolves through `ViewConfig`.
- Allowed Dependencies:
  - `Scaffold.Events.Contracts`, `Scaffold.Types`, `Scaffold.Records`, container abstractions.
- Forbidden Dependencies:
  - module-specific gameplay logic in navigation runtime.
- Change Checklist:
  - verify open/close/return tests.
  - verify options behavior tests.
  - verify transition event behavior.
- Known Tricky Areas:
  - closeCurrent vs closeAllViews interactions.

## Related

- `Architecture.md`
- `Docs/Infra/Model.md`
- `Docs/Core/ViewModel.md`
- `Docs/App/View.md`
- `Docs/Infra/Events.md`

## Changelog

- 2026-03-15: Rewritten to AI-first standard with navigation sequence diagram.
- 2026-03-15: Recovered stack semantics and transition/schema execution contracts.

- 2026-03-16: Added constructor null-guard coverage and single-point `Return()` behavior verification.
