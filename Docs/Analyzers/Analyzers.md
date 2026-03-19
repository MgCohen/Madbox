# Scaffold Analyzers

## TL;DR

- Purpose: custom Roslyn analyzer package that enforces style, architecture, and MVVM/runtime-boundary rules.
- Location: source under `Analyzers/Scaffold/Scaffold.Analyzers/`; compiled DLL under `Analyzers/Output/`.
- Depends on: Roslyn APIs + repository `Directory.Build.props` analyzer wiring.
- Used by: all repository `.csproj` projects through analyzer injection.
- Runtime/Editor: IDE/build-time diagnostics only (Unity runtime does not execute analyzers).

## Responsibilities

- Owns SCA diagnostic rules and rule metadata.
- Owns analyzer config parsing (`AnalyzerConfig.cs`) and severity override behavior.
- Owns architecture enforcement diagnostics (for example namespace alignment and runtime assembly boundaries).
- Does not own Unity runtime behavior or gameplay logic.

## Public API

| Symbol | Purpose | Inputs | Outputs | Failure behavior |
|---|---|---|---|---|
| `Scaffold.Analyzers.dll` | Analyzer distribution artifact | analyzer source + build pipeline | Roslyn diagnostics surfaced in IDE/build | missing artifact means no diagnostics in consumers |
| `AnalyzerConfig` | Central config reader and descriptor override helper | `.editorconfig` values + analyzer options | effective rule descriptors/config values | invalid values fall back to defaults |
| `SCA0001`-`SCA0031` descriptors | Rule contracts consumed by IDE/build tooling | C# syntax/semantic model | diagnostics with code fixes/remediation guidance | suppressed/disabled severities skip reporting |

## Setup / Integration

1. Build analyzer project:

```bash
cd Analyzers/Scaffold/Scaffold.Analyzers
dotnet build -c Release
```

2. Ensure compiled artifact exists at `Analyzers/Output/Scaffold.Analyzers.dll`.
3. Keep `Directory.Build.props` analyzer include active so all projects receive diagnostics.
4. Run analyzer diagnostics from repo root:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File ".\.agents\scripts\check-analyzers.ps1"
```

## How to Use

1. Run `check-analyzers.ps1`.
2. Fix reported diagnostics in top-down file order.
3. Re-run analyzer checks.
4. Run full gate:

```powershell
& ".\.agents\scripts\validate-changes.cmd"
```

## Examples

### Minimal

```ini
[*.cs]
dotnet_diagnostic.SCA0006.severity = error
```

### Realistic

1. Hit `SCA0012` on a public runtime method.
2. Add leading guard/validation call.
3. Re-run analyzer checks to confirm diagnostic is cleared.

### Guard / Error path

```text
If Analyzers/Output/Scaffold.Analyzers.dll is missing, analyzer diagnostics will not load in consumer projects.
```

## Best Practices

- Keep rule IDs stable once published.
- Keep rule docs synchronized with current analyzer IDs and behavior.
- Prefer conservative diagnostics with precise messages and examples.
- Respect `.editorconfig` severity overrides via `AnalyzerConfig`.
- Add/update analyzer tests for every rule behavior change.

## Anti-Patterns

- Updating analyzer behavior without updating docs/tests.
- Introducing rule ID gaps or undocumented renumbering.
- Depending on analyzer diagnostics as runtime safety checks.

## Testing

- Analyzer tests: `Analyzers/Scaffold/Scaffold.Analyzers.Tests`.
- Build analyzer:

```bash
cd Analyzers/Scaffold/Scaffold.Analyzers
dotnet build -c Release
```

- Run repo analyzer gate:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File ".\.agents\scripts\check-analyzers.ps1"
```

- Expected: zero unexpected diagnostics for validated code paths.
- Bugfix rule: add/update regression test first, verify fail-before/fix/pass-after.

## AI Agent Context

- Invariants:
  - analyzer output DLL path and `Directory.Build.props` wiring remain valid.
  - rule IDs remain unique and documented.
  - analyzer changes are covered by analyzer tests.
- Allowed Dependencies:
  - Roslyn analyzer APIs and repo-shared analyzer infrastructure.
- Forbidden Dependencies:
  - Unity runtime APIs in analyzer execution logic.
- Change Checklist:
  - update rule docs and examples for changed diagnostics.
  - run analyzer project build.
  - run `.agents/scripts/check-analyzers.ps1`.
- Known Tricky Areas:
  - rule ID renumbering drift between docs and code.
  - stale analyzer DLL not rebuilt after source updates.

## Related

- `Architecture.md`
- `Docs/Testing.md`
- `Docs/Standards/Module-Documentation-Standard.md`
- `.agents/workflows/check-analyzers.md`

## Changelog

- 2026-03-15: Added architecture/location details and rule quick-start guidance.
- 2026-03-15: Added module-standard top sections while preserving full rules reference below.

## Architecture & Locations

### Source Code
The analyzer source and `.csproj` live outside the Unity Assets folder at:
`[Repository Root]/Analyzers/Scaffold/Scaffold.Analyzers/`

This contains one `.cs` file per rule, plus `AnalyzerConfig.cs` for shared configuration utilities.

### Compiled Artifact
When the project is built, the DLL is output to:
`[Repository Root]/Analyzers/Output/Scaffold.Analyzers.dll`

This file is committed to git. It is **not** placed inside `Assets/`; Unity does not process it.

### Integration
A `Directory.Build.props` file at the repo root automatically injects the DLL as a Roslyn analyzer into every `.csproj` in the tree:

```xml
<Project>
  <ItemGroup Condition="'$(MSBuildProjectName)' != 'Scaffold.Analyzers'">
    <Analyzer Include="$(MSBuildThisFileDirectory)Analyzers/Output/Scaffold.Analyzers.dll"
              Condition="Exists('$(MSBuildThisFileDirectory)Analyzers/Output/Scaffold.Analyzers.dll')" />
  </ItemGroup>
</Project>
```

Diagnostics surface in the IDE and to AI tooling via the language server. Unity's compiler never runs them.

## Quick Start (AI + Dev Workflow)

Use this section when you need fast fixes, then use the full rule catalog below for exact behavior and examples.

### Most frequently hit rules (fix-first order)

1. `SCA0012` / `SCA0017`: add invariant guards at public runtime entry points and constructors.
2. `SCA0006`: split long methods into smaller named steps.
3. `SCA0003`: remove nested calls by introducing intermediate locals.
4. `SCA0020`: reorder class members to expected layout.
5. `SCA0008` / `SCA0009` / `SCA0010`: normalize naming (`camelCase` private, `PascalCase` public).
6. `SCA0004` / `SCA0005`: use block bodies and single-line signatures/statements where required.
7. `SCA0021`: in MVVM descendants, use bind APIs instead of manual `PropertyChanged` wiring.

### Rule lookup tip

When you see `SCAxxxx` in diagnostics:

1. Jump to `## Rules Reference`.
2. Find `### SCAxxxx`.
3. Apply the compliant example pattern directly.

### Rule ID map

Active rule IDs in this repository:

- `SCA0001`-`SCA0031` (SCA0027 + SCA0028 + SCA0029 + SCA0030 + SCA0031 active)

---
## Rules Reference

### SCA0001 - No Method Comments

Methods must not have XML documentation comments or inline comments. The only exceptions are comments containing `todo` or `sample` (case-insensitive).

**Rationale:** Comments often compensate for poorly named or overly complex methods. Method names and structure should be self-documenting.

```csharp
// VIOLATION
/// <summary>Loads the player data from disk.</summary>
public void LoadPlayer()
{
    // read file
    var data = File.ReadAllText(path);
}

// COMPLIANT
public void LoadPlayer()
{
    var data = File.ReadAllText(path);
}

// ALLOWED (todo exception)
public void LoadPlayer()
{
    // todo: add error handling
    var data = File.ReadAllText(path);
}
```

---

### SCA0002 - Method Order

Instance methods must be declared after the methods that call them. If `A` calls `B`, then `B` must appear below `A` in the file. This enforces a top-down reading order.

Additionally, dependency blocks must be contiguous: if `A` calls `C`, an unrelated `B` should not appear between `A` and `C` unless `B` also depends on `C`.

Static methods are not evaluated by this rule (class-level placement for static members is covered by `SCA0020`).

**Rationale:** Code reads like a newspaper - high-level entry points at the top, implementation details below.

```csharp
// VIOLATION - Initialize calls Setup, but Setup appears first
public void Setup() { }
public void Initialize() { Setup(); }

// COMPLIANT
public void Initialize() { Setup(); }
public void Setup() { }
```

---

### SCA0003 - No Nested Calls

Function calls and object constructions must not be nested as arguments. Extract intermediate values to named local variables.

**Exception:** `nameof()` expressions are allowed to be nested.

```csharp
// VIOLATION
var result = Process(GetInput());
var obj = new Handler(new Config());

// COMPLIANT
var input = GetInput();
var result = Process(input);

var config = new Config();
var obj = new Handler(config);

// ALLOWED
Debug.LogError(nameof(MyClass));
```

---

### SCA0004 - Curly-Bracket Bodies Only

Methods in a class must use block bodies with curly brackets. Expression-body syntax (`=>`) is not allowed on method declarations.

```csharp
// VIOLATION
public int GetCount() => items.Count;

// COMPLIANT
public int GetCount()
{
    return items.Count;
}
```

---

### SCA0005 - No Multi-Line Signatures or Statements

Method and constructor signatures must fit on a single line. Method signatures include trailing generic constraints (`where T : ...`) and those constraints must stay on the same line as the signature.

Statements inside a method body must not span multiple lines.

**Exception:** Fluent/builder chains using member access (`.Method().Method()`) are permitted to span lines.

```csharp
// VIOLATION - signature spans multiple lines
public void Register(
    string name,
    int priority)
{ }

// COMPLIANT
public void Register(string name, int priority) { }

// VIOLATION - statement spans multiple lines
var result =
    someValue +
    otherValue;

// ALLOWED - fluent chain
builder
    .WithName("test")
    .WithPriority(1)
    .Build();
```

---

### SCA0006 - Small Functions

Methods must not exceed 8 lines of code (configurable). Refactor by extracting steps into well-named helper methods.

**Configuration:** Override the threshold in `.editorconfig`:
```ini
scaffold.SCA0006.max_lines = 12
```

```csharp
// VIOLATION - 9 lines
public void ProcessOrder(Order order)
{
    ValidateOrder(order);
    var items = FetchItems(order);
    ApplyDiscounts(items);
    CalculateTotals(items);
    SaveToDatabase(order);
    SendConfirmation(order);
    UpdateInventory(items);
    NotifyWarehouse(order);
    LogAuditTrail(order);  // line 9
}

// COMPLIANT - extracted
public void ProcessOrder(Order order)
{
    ValidateOrder(order);
    var items = PrepareItems(order);
    FinalizeOrder(order, items);
}
```

---

### SCA0007 - Namespace Must Match Folder Structure

Namespaces must end with the file's feature/scope folder path. Unity-specific segments (`Assets`, `Scripts`), the first folder under `Scripts` (the domain segment, e.g. `Core`, `Infra`), and the folders `Runtime` and `Implementation` are excluded from the required namespace path.

Files under a `Contracts/` path segment (for example `Runtime/Contracts/`) are expected to include `.Contracts` in namespace suffixes.

This rule applies only to files under `Assets/Scripts/` and skips generated files (e.g., `obj/`, `bin/`, `*.g.cs`).
All top-level namespace declarations in a file are validated (not just the first declaration).
`Assets/Scripts/Tools/Records/Runtime/IsExternalInit.cs` is explicitly exempted for C# record compatibility.
Compilation units that contain only assembly/module attributes (for example `AssemblyInfo.cs` with `[assembly: ...]`) are also exempt.

Root namespace resolution order:
1. `scaffold.SCA0007.root_namespace` from `.editorconfig` (explicit override)
2. `build_property.RootNamespace`
3. `build_property.MSBuildProjectName`

```csharp
// File: Assets/Scripts/Infra/Navigation/Container/NavigationInstaller.cs
// VIOLATION
namespace Utilities.Container.Navigation { }

// COMPLIANT
namespace Scaffold.Navigation.Container { }
```

```csharp
// File: Assets/Scripts/Infra/ViewModel/Runtime/Binding/BindSet.cs
// COMPLIANT (Runtime/Implementation skipped)
namespace Scaffold.MVVM.Binding { }
```

```csharp
// File: Assets/Scripts/Infra/Navigation/Runtime/Contracts/INavigation.cs
// COMPLIANT (Contracts path segment is kept)
namespace Scaffold.Navigation.Contracts { }
```

```ini
# Optional explicit root override
scaffold.SCA0007.root_namespace = Scaffold
```

---

### SCA0027 - One Top-Level Namespace Per File

Files under `Assets/Scripts/` must declare exactly one top-level namespace.
This prevents sibling namespace declarations from bypassing namespace/folder conventions.

`Assets/Scripts/Tools/Records/Runtime/IsExternalInit.cs` is explicitly exempted for C# record compatibility.
Assembly/module-attributes-only files (for example `AssemblyInfo.cs`) are exempt as metadata-only files.

```csharp
// VIOLATION
namespace Scaffold.Navigation.Contracts { }
namespace Scaffold.Navigation { }

// COMPLIANT
namespace Scaffold.Navigation.Contracts { }
```

---

### SCA0008 - No Underscore or Hungarian Prefixes on Private Fields

Private fields must not use `_` or `m_` prefixes.

```csharp
// VIOLATION
private int _count;
private string m_name;

// COMPLIANT
private int count;
private string name;
```

---

### SCA0009 - Private Fields Must Be camelCase

Private fields must start with a lowercase letter.

```csharp
// VIOLATION
private int Count;

// COMPLIANT
private int count;
```

---

### SCA0010 - Public Members Must Be PascalCase

Public fields, properties, and methods must start with an uppercase letter.

**Exceptions:** Unity's built-in members `gameObject` and `transform` are exempt. Override methods and operator overloads are also skipped.

```csharp
// VIOLATION
public int count;
public void processData() { }

// COMPLIANT
public int Count;
public void ProcessData() { }
```

---

### SCA0011 - Prefer Unity's Awaitable Over Task/ValueTask

Methods should return Unity's `Awaitable` type instead of `Task` or `ValueTask`. Use `Task`/`ValueTask` only when `Awaitable` is not applicable (e.g., non-Unity libraries, interfaces crossing module boundaries).

```csharp
// VIOLATION
public async Task LoadSceneAsync(string name) { }
public async ValueTask<int> FetchIdAsync() { }

// COMPLIANT
public async Awaitable LoadSceneAsync(string name) { }
```

---

### SCA0012 - Validate Invariants at Public Runtime Entry Points

Public runtime API methods should validate invariants at the beginning of the method body.

This rule analyzes public, non-override methods in `Runtime/` paths and skips `Tests/` and `Samples/`.
It also skips parameterless methods.

The first executable statement (after leading local declarations) must be:
- a guard clause (`if (...) return;` or `if (...) throw ...;`), or
- a validation call whose method name starts with `Validate`, `TryValidate`, `Ensure`, or `Guard`, or
- a null-coalescing assignment that normalizes a method parameter (`parameter ??= ...`).

```csharp
// VIOLATION
public void Send(Message message)
{
    Publish(message);
}

// COMPLIANT - validation call
public void Send(Message message)
{
    ValidateMessage(message);
    Publish(message);
}

// COMPLIANT - guard clause
public void Send(Message message)
{
    if (message == null) throw new ArgumentNullException(nameof(message));
    Publish(message);
}

// COMPLIANT - parameter normalization
public void GetAllStackedScreens(Func<NavigationPoint, bool> filter = null)
{
    filter ??= (point) => true;
    Process(filter);
}
```

**Optional configuration:** allow additional validation prefixes via `.editorconfig`:
```ini
scaffold.SCA0012.allowed_prefixes = CheckInvariant,AssertValid
```

---

## Configuration

All rules support per-rule severity override via `.editorconfig`:

```ini
[*.cs]
dotnet_diagnostic.SCA0001.severity = none      # suppress
dotnet_diagnostic.SCA0006.severity = error     # escalate to error
dotnet_diagnostic.SCA0011.severity = suggestion
dotnet_diagnostic.SCA0012.severity = warning
dotnet_diagnostic.SCA0013.severity = warning
dotnet_diagnostic.SCA0014.severity = warning
dotnet_diagnostic.SCA0015.severity = warning
dotnet_diagnostic.SCA0016.severity = warning
dotnet_diagnostic.SCA0017.severity = warning
dotnet_diagnostic.SCA0018.severity = warning
dotnet_diagnostic.SCA0019.severity = warning
dotnet_diagnostic.SCA0020.severity = warning
dotnet_diagnostic.SCA0021.severity = warning
dotnet_diagnostic.SCA0022.severity = warning
dotnet_diagnostic.SCA0023.severity = warning
dotnet_diagnostic.SCA0024.severity = warning
dotnet_diagnostic.SCA0026.severity = warning
dotnet_diagnostic.SCA0028.severity = warning
dotnet_diagnostic.SCA0029.severity = warning
dotnet_diagnostic.SCA0030.severity = warning
dotnet_diagnostic.SCA0031.severity = warning
```

---

### SCA0013 - Serializable Unity Models Must Use [SerializeField] Backing Fields

In Unity-facing modules, classes marked with `[Serializable]` should not expose public auto-properties. Use a private field marked with `[SerializeField]` and expose it through a public getter-only property.

This rule applies to files under `Assets/Scripts/` (excluding `Tests/` and `Samples/`) when the compilation references `UnityEngine*` assemblies.

```csharp
// VIOLATION
[Serializable]
public class ViewModel
{
    public int Count { get; set; }
}

// VIOLATION
[Serializable]
public class ViewModel
{
    public int Count { get; }
}

// COMPLIANT
[Serializable]
public class ViewModel
{
    [SerializeField] private int count;
    public int Count => count;
}
```

---

### SCA0014 - Restrict Static Methods in Non-Static Classes

Static methods declared inside non-static classes are disallowed unless they are:
- extension methods
- parsing/conversion helpers (`Parse*`, `TryParse*`, `From*`, `To*`)
- factory methods (`Create*`, `Build*`, `New*`)

Static methods in static classes are always allowed.

```csharp
// VIOLATION
public sealed class Game
{
    private static void EnsureWaitTimeout(TimeSpan timeout) { }
}

// COMPLIANT - instance method
public sealed class Game
{
    private void EnsureWaitTimeout(TimeSpan timeout) { }
}

// COMPLIANT - static utility class
public static class TimeUtility
{
    public static bool IsNegative(TimeSpan timeout) => timeout < TimeSpan.Zero;
}

// COMPLIANT - parsing/conversion helper
public sealed class LevelId
{
    public static LevelId Parse(string raw) => new LevelId();
}

// COMPLIANT - factory method
public sealed class LevelFactory
{
    public static LevelFactory CreateDefault() => new LevelFactory();
}
```

---

### SCA0015 - Place Extra Types by Usage Scope

When a file contains more than one top-level type, extra types (enums/classes/structs/interfaces/records) must follow one of these rules:
- If the extra type is used by another type or exposed in a public/protected API, it must be moved to its own file.
- If the extra type is local-only to the primary type, it must be declared after the primary type (or nested at the end of the primary type).

Primary type resolution:
- First preference: type name matches file name (for `Game.cs`, primary is `Game`)
- Fallback: first top-level type in the file

This rule applies to files under `Assets/Scripts/` and skips `Tests/`, `Samples/`, and generated/build outputs.

```csharp
// VIOLATION - local helper type declared before primary
internal enum GameState
{
    Initializing,
    Started,
    Finished
}

public sealed class Game
{
    private GameState state;
}

// COMPLIANT - local helper type declared after primary
public sealed class Game
{
    private GameState state;
}

internal enum GameState
{
    Initializing,
    Started,
    Finished
}

// VIOLATION - type exposed in public API, should be in GameState.cs
public sealed class Game
{
    public GameState State { get; private set; }
}
```

---

### SCA0016 - Use TextMeshProUGUI Instead of UnityEngine.UI.Text

Legacy `UnityEngine.UI.Text` is disallowed. Always use `TMPro.TextMeshProUGUI` for UI labels.

```csharp
// VIOLATION
using UnityEngine.UI;
public sealed class MainMenuView
{
    private Text currentLevelLabel;
}

// COMPLIANT
using TMPro;
public sealed class MainMenuView
{
    private TextMeshProUGUI currentLevelLabel;
}
```

---

### SCA0017 - Validate Invariants at Public Runtime Constructors

Public constructors in `Runtime/` paths should validate invariants at entry when they receive parameters.

This rule is intentionally conservative and skips:
- constructors without parameters
- non-public constructors
- delegating constructors (`: this(...)`)
- files under `Tests/` and `Samples/`

Accepted first-entry validation patterns:
- guard clause (`if (...) throw ...`)
- `ArgumentNullException.ThrowIfNull(...)` when supported by the target runtime/profile
- validation call with prefixes `Validate*`, `TryValidate*`, `Ensure*`, `Guard*`

For Unity versions/profiles where `ThrowIfNull` is unavailable, prefer explicit guard clauses:
- `if (value == null) throw new ArgumentNullException(nameof(value));`

```csharp
// VIOLATION
public sealed class MainMenuViewModel
{
    private readonly IGoldService goldService;

    public MainMenuViewModel(IGoldService goldService)
    {
        this.goldService = goldService;
    }
}

// COMPLIANT
public sealed class MainMenuViewModel
{
    private readonly IGoldService goldService;

    public MainMenuViewModel(IGoldService goldService)
    {
        if (goldService == null) throw new ArgumentNullException(nameof(goldService));
        this.goldService = goldService;
    }
}
```

**Optional configuration:** allow additional validation prefixes via `.editorconfig`:
```ini
scaffold.SCA0017.allowed_prefixes = CheckInvariant,AssertValid
```

---

### SCA0018 - MVVM Classes Should Use Module Base Types

Classes inside the MVVM module should not manually implement MVVM notifier interfaces.  
Prefer inheriting from `Scaffold.MVVM.Model` or `Scaffold.MVVM.ViewModel` so shared behavior and source-generated features stay consistent.

This rule checks files under `Assets/Scripts/Core/MVVM/`, `Assets/Scripts/Infra/Model/`, `Assets/Scripts/Infra/ViewModel/`, and `Assets/Scripts/Infra/View/`, and skips `Tests/` and `Samples/`.

```csharp
// VIOLATION
public class InventoryViewModel : IViewModel
{
}

// COMPLIANT
public partial class InventoryViewModel : ViewModel
{
}
```

For `IViewModel`, this diagnostic is raised when a class directly inherits from `object` and implements `IViewModel` without inheriting from `ViewModel`.

---

### SCA0019 - MVVM Generator Attributes Must Resolve

Known MVVM source-generator attributes must resolve to actual referenced types:
- `ObservableProperty`
- `NestedObservableObject`
- `BindSource`

If these attributes are unresolved, add references to:
- `CommunityToolkit.Mvvm`
- `CommunityToolkit.Mvvm.SourceGenerators`
- `MVVMCompositionGenerator`

```csharp
// VIOLATION (attribute unresolved)
public partial class InventoryViewModel
{
    [ObservableProperty]
    private int amount;
}

// COMPLIANT (attribute type available through references)
public partial class InventoryViewModel
{
    [ObservableProperty]
    private int amount;
}
```

---

### SCA0020 - Class Member Order

Class and struct members must follow this order:
- static properties
- const fields
- constructors
- indexers
- properties
- fields
- events
- instance methods
- static methods
- private nested types (`class`, `struct`, `enum`)
- operators

Special exception:
- a backing field may appear directly below its related property (for example `MyValue => myValue;` followed by `private int myValue;`).

```csharp
// VIOLATION
public sealed class Game
{
    public int Value => value;
    public Game() { }
    private int value;
}

// COMPLIANT
public sealed class Game
{
    public Game() { }
    public int Value => value;
    private int value;
}
```

---

### SCA0021 - MVVM Descendants Must Use Bind APIs (No Manual PropertyChanged Wiring)

Classes that inherit from `Scaffold.MVVM.Model`, `Scaffold.MVVM.ViewModel`, or `Scaffold.MVVM.ViewElement` must use the framework's bind APIs and base notification flow.

Manual `PropertyChanged` wiring/declarations in these descendants is disallowed, including:
- subscribing/unsubscribing to `PropertyChanged` with `+=`/`-=`
- declaring a `PropertyChanged` event
- manually calling `OnPropertyChanged(...)`
- manually calling `UpdateBinding(...)`
- manually calling `RegisterNestedProperties()`

```csharp
// VIOLATION
public sealed class InventoryView : ViewElement<InventoryViewModel>
{
    public void Attach(INotifyPropertyChanged vm)
    {
        vm.PropertyChanged += OnChanged;
    }
}

// COMPLIANT
public sealed class InventoryView : ViewElement<InventoryViewModel>
{
    protected override void OnBind()
    {
        Bind(() => viewModel.Amount, amount => amountLabel.text = amount.ToString());
    }
}
```

---

### SCA0022 - Restrict Cross-Module Runtime Assembly References

Assemblies must not reference another module's `*.Runtime` assembly unless they are composition roots (bootstrap assemblies) or module-local assemblies (same module root, for example `Madbox.Meta.Gold.Tests` referencing `Madbox.Meta.Gold.Runtime`).

This rule enforces cross-module runtime dependency direction:
- Non-bootstrap modules should avoid cross-module `*.Runtime` references.
- Use non-runtime module assemblies for cross-module dependencies when available.
- Exception: if the referenced module has no contracts surface, the runtime reference is allowed.

Optional config for explicit exceptions when repository layout cannot be resolved:
- `scaffold.SCA0022.no_contract_modules` (comma/semicolon list of module roots such as `Madbox.Legacy.Module;Scaffold.SomeTool`)

```csharp
// VIOLATION context:
// Assembly: Madbox.MainMenu.Runtime
// References: Madbox.Meta.Gold.Runtime
// Diagnostic: SCA0022
```

```csharp
// COMPLIANT context:
// Assembly: Madbox.MainMenu.Runtime
// References: Madbox.Meta.Gold
```

```csharp
// COMPLIANT context:
// Assembly: Madbox.Bootstrap.Runtime
// References: Madbox.Meta.Gold.Runtime
// (bootstrap composition root)
```

```csharp
// COMPLIANT context:
// Assembly: Madbox.MainMenu.Runtime
// References: Madbox.Meta.Gold.Runtime
// Madbox.Meta.Gold has Runtime only (no contracts surface)
```

---

### SCA0023 - Required Module Folders Must Exist

Each module under `Assets/Scripts/<Layer>/<Module>/` must contain required top-level folders.

Defaults:
- `Runtime`
- `Tests`

Optional (not required by default):
- `Container`
- `Editor`
- `Samples`

Config:
- `scaffold.SCA0023.required_folders` (comma/semicolon list)
- `scaffold.SCA0023.exempt_module_roots` (skip entire module roots, e.g. `Scaffold.Records`)
- `scaffold.SCA0023.exempt_requirements` (per-module folder exemptions, format `ModuleRoot=FolderA|FolderB;Other.Module=Runtime`)

```ini
[*.cs]
scaffold.SCA0023.required_folders = Runtime,Tests
scaffold.SCA0023.exempt_requirements = Scaffold.Records=Runtime|Tests
```

---

### SCA0024 - Module Asmdef Placement/Name Convention

Each assembly must declare its `.asmdef` in the expected module-relative location and the asmdef `"name"` must match the compiled assembly name.

Expected placement:
- `<ModuleRoot>/Runtime/<Assembly>.asmdef` (default modules)
- `<ModuleRoot>/Tests/<Assembly>.asmdef` (`*.Tests`)
- `<ModuleRoot>/Tests/PlayMode/<Assembly>.asmdef` (`*.PlayModeTests`)
- `<ModuleRoot>/Samples/<Assembly>.asmdef` (`*.Samples`)
- `<ModuleRoot>/Container/<Assembly>.asmdef` (`*.Container`)
- `<ModuleRoot>/Editor/<Assembly>.asmdef` (`*.Editor`)
- `<ModuleRoot>/Authoring/<Assembly>.asmdef` (`*.Authoring`)

Notes:
- Module root asmdef is disallowed by default (`<ModuleRoot>/<Assembly>.asmdef` is flagged).
- Top-level `Contracts` asmdef placement is not part of this convention.

Config:
- `scaffold.SCA0024.exempt_assemblies` (comma/semicolon list)
- `scaffold.SCA0024.suffix_folder_map` (custom `suffix=folder` mapping, `;` or `,` separated, supports multiple folders via `|`)
- `scaffold.SCA0024.disallow_module_root_asmdef` (`true`/`false`, default `true`)
- `scaffold.SCA0024.allow_unknown_suffix_in_any_subfolder` (`true`/`false`, default `false`)

Example:
```ini
scaffold.SCA0024.suffix_folder_map = .Runtime=Runtime;.Tests=Tests;.PlayModeTests=Tests/PlayMode;.Samples=Samples;.Container=Container;.Editor=Editor;.Authoring=Authoring
scaffold.SCA0024.disallow_module_root_asmdef = true
scaffold.SCA0024.allow_unknown_suffix_in_any_subfolder = true
```

---

### SCA0026 - Forbid Same-Layer Dependency Member Usage During Initialization

Types implementing `Madbox.Initialization.Contracts.IAsyncLayerInitializable` must not consume instance members of same-layer dependencies inside `InitializeAsync` call chains.

Allowed:
- pass or store same-layer references for later runtime use

Forbidden:
- calling instance methods on same-layer dependencies during initialization
- reading same-layer dependency instance values during initialization
- forwarding a same-layer dependency into helper methods that then consume member usage in the same call chain

Explicit exceptions:
- `[AllowSameLayerInitializationUsage]` on class/method
- `[AllowInitializationCallChain]` on helper methods to stop transitive chain analysis

```csharp
// VIOLATION
public Task InitializeAsync(CancellationToken token)
{
    int gold = goldService.GetCurrentGold();
    return Task.CompletedTask;
}

// COMPLIANT (pass/store only)
public Task InitializeAsync(CancellationToken token)
{
    Persist(goldService);
    return Task.CompletedTask;
}
```

---

### SCA0028 - Loop Bodies Require Braces and Next-Line Formatting

`for`, `foreach`, `while`, and `do-while` must use braces, and the opening brace must be on the next line after the loop header.

```csharp
// VIOLATION
for (int i = 0; i < 10; i++) Tick();

// VIOLATION
while (ready) { Tick(); }

// COMPLIANT
for (int i = 0; i < 10; i++)
{
    Tick();
}
```

---

### SCA0029 - Conditional Braces and Inline If Exception

Only one pattern may omit braces:
- single-line `if (...) statement;`
- no `else` branch
- exactly one embedded statement

All other `if/else` forms must use braces and next-line block formatting.

```csharp
// ALLOWED
if (ready) Tick();

// VIOLATION
if (ready)
    Tick();

// VIOLATION
if (ready) Tick();
else Tick();

// COMPLIANT
if (ready)
{
    Tick();
}
else
{
    Pause();
}
```

---

### SCA0030 - Runtime Dead Code for Non-Public Methods/Constructors

In `Runtime/` paths, non-public methods/constructors that are not referenced by non-test code are flagged as dead code.

This rule skips:
- public/protected public API members
- interface implementations
- override members
- files under `Tests/` and `Samples/`

```csharp
// VIOLATION (unused private runtime helper)
private void NormalizeState() { }

// COMPLIANT (used in non-test runtime flow)
public void Run()
{
    NormalizeState();
}

private void NormalizeState() { }
```

---

### SCA0031 - Runtime Code Must Not Use `#pragma warning disable`

In `Runtime/` paths under `Assets/Scripts/`, `#pragma warning disable` is forbidden.
This prevents silent suppression of compiler/analyzer diagnostics in production code.

This rule skips:
- files under `Tests/` and `Samples/`
- generated/build paths (`obj/`, `bin/`, `*.g.cs`)

```csharp
// VIOLATION
#pragma warning disable CS0168
public sealed class Game
{
    public void Run() { }
}
#pragma warning restore CS0168

// COMPLIANT (fix issue instead of suppression)
public sealed class Game
{
    public void Run() { }
}
```

Valid severity values: `error`, `warning`, `suggestion`, `info`, `hidden`, `silent`, `none`.

The `AnalyzerConfig.cs` class handles all config parsing. It caches overridden descriptors and provides:
- `GetEffectiveDescriptor()` - reads and applies severity from editorconfig
- `ShouldSuppress()` - returns true if severity is set to `none`
- `GetInt()` - reads integer config values (used by SCA0006 for line threshold)

---

## Adding a New Rule

1. Create `[RuleName]Analyzer.cs` in `Analyzers/Scaffold/Scaffold.Analyzers/`
2. Extend `DiagnosticAnalyzer`, declare a `DiagnosticDescriptor` with the next available `SCA{id}`
3. Override `Initialize(AnalysisContext context)` and register your syntax/symbol action
4. Use `AnalyzerConfig.GetEffectiveDescriptor()` and `ShouldSuppress()` at the call site so the rule respects editorconfig
5. Build: `dotnet build -c Release` from `Analyzers/Scaffold/Scaffold.Analyzers/`
6. Add a test in `Analyzers/Scaffold/Scaffold.Analyzers.Tests/`

Use the `/create-custom-analyzer` workflow (`.agents/workflows/create-custom-analyzer.md`) to scaffold the boilerplate automatically.


