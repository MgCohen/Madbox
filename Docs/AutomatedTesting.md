# Automated Testing (authoring playbook)

## TL;DR

- Purpose: How to author effective automated tests in this Unity project — coverage strategy, structure, setup, and quality checks.
- Location: `Docs/AutomatedTesting.md` (this file); tests live under `Assets/Scripts/**/Tests/`.
- Depends on: Unity Test Framework, NUnit, repository scripts in `.agents/scripts/`.
- Used by: All modules; required reading before adding tests per `AGENTS.md`.
- Runtime/Editor: EditMode vs PlayMode as documented below.

Keywords: EditMode, PlayMode, pyramid, regression, coverage

## Responsibilities

- Owns: Test pyramid guidance, asmdef setup, naming/design checklist, regression workflow, coverage audit pointers, low-test exception rules.
- Does not own: Exact CI machine config or Unity Editor UI for Test Runner (see Unity docs).
- Boundaries: Authoring and review; execution commands live in `Docs/Testing.md`.

## Public API

This document has no code API. Use these **repository contracts**:

| Artifact | Purpose |
|---|---|
| `*.Tests.asmdef` | EditMode test assembly next to module. |
| `validate-changes.cmd` | Full gate including tests (see `Testing.md`). |
| `run-coverage-audit.cmd` / `.ps1` | Optional coverage audit (not part of default gate). |

## Setup / Integration

1. Read `Docs/Testing.md` for how to run tests and analyzer checks.
2. Create test assemblies per module (see **How to Use** — new assembly steps).
3. Add `com.unity.testtools.codecoverage` only when running coverage audits (`Packages/manifest.json`).

## How to Use

### Testing strategy (pyramid)

- Many fast EditMode tests for domain and module behavior (plain C# where possible).
- Fewer integration-style EditMode tests for wiring.
- Small PlayMode tests for bootstrap/scene-critical paths.

Prioritize coverage in this order:

1. Core game rules and state transitions.
2. Public module contracts and invariants.
3. App wiring and composition boundaries.
4. Critical runtime scene behavior (smoke + fatal-log detection).
5. Regression paths for discovered bugs.

### Layer-specific guidance

**Core and domain:** Prefer plain NUnit; avoid scene setup unless needed; assert behavior, not internals. Examples: `Meta/Gold/Tests`, Core tests as present in repo.

**Infra/App EditMode:** ViewModel/controller tests with fakes; minimal MonoBehaviour; destroy `GameObject` in teardown.

**PlayMode:** Critical E2E only; bounded waits; fail on Unity error/assert/exception logs.

### New test assembly

1. Add `ModuleName.Tests.asmdef` under the module’s `Tests/` folder.
2. Reference the module under test and required dependencies only.
3. Add `"optionalUnityReferences": ["TestAssemblies"]`.
4. EditMode: `"includePlatforms": ["Editor"]`. PlayMode: usually no `includePlatforms` restriction.
5. Disable `autoReferenced`; declare explicit references. Use `Madbox.Gold.Tests` / `Madbox.Bootstrap.Tests` asmdefs as templates.

### Regression workflow (bug fixes)

1. Write or update a failing test that reproduces the bug.
2. Confirm it fails before the fix.
3. Implement the fix.
4. Confirm the test passes.
5. Run `validate-changes.cmd` and resolve all failures.

### Coverage goals and audit

- Coverage is **not** produced by `validate-changes.cmd`. Use `run-coverage-audit` scripts when auditing.
- Default Cobertura path: `Coverage/Report/Cobertura.xml`.
- Filters: see `run-coverage-audit.ps1` (`-CoverageAssemblyFilters`).

### Test quality scorecard (milestone reviews)

Use multiple metrics together. For each module track: behavior/risk coverage, structural coverage as gap finder, test density, API contract coverage, reliability (no flake).

Suggested thresholds (tune with data): line coverage ≥70% Core/Meta/Infra, ≥60% App/Tools; branch coverage ≥60% / ≥50%; `TestsPer1kLoC` warning under 8, target 10+ for complex modules; `TestCodeRatio` warning under 0.5; boundary API coverage targets as in original baseline.

### Repository coverage baseline (indicative)

See `Plans/ModuleTestCoverage/ModuleTestCoverage-ExecPlan.md` for execution planning. Baseline snapshot (counts change over time):

- Examples called out in prior doc: `Infra/Navigation`, `Infra/ViewModel` — expansion priority; `Tools/Records` — documented low-test exception in `Docs/Tools/Records.md`.

### Low-test module exceptions

Rare marker/compatibility modules may keep minimal tests if documented in that module’s `Docs/` file with rationale, at least one smoke test, and trigger conditions for expanding coverage. Example: `Docs/Tools/Records.md`.

## Examples

### Minimal unit test

```csharp
[Test]
public void Add_WhenCalled_IncrementsTotal()
{
    var wallet = new GoldWallet();
    wallet.Add(1);
    Assert.AreEqual(1, wallet.Total);
}
```

### Regression template

```csharp
[Test]
public void Finish_CalledMultipleTimes_RaisesCompletedOnlyOnce()
{
    Game game = CreateGame();
    int callCount = 0;
    game.Completed += _ => callCount++;
    game.Start();
    game.Finish();
    game.Finish();
    Assert.AreEqual(1, callCount);
}
```

### Guard / Error path

```csharp
// Do not sleep/wait arbitrarily — use deterministic signals or bounded Unity test timeouts
```

## Best Practices

- Naming: `Method_WhenCondition_ExpectedResult`.
- Arrange / Act / Assert; one behavior per test.
- Deterministic setup; prefer fakes over real infrastructure.
- For analyzer tests depending on assembly topology, use shared structural fixtures (`StructuralTestGraph`) when the repo provides them.

## Anti-Patterns

- Mostly PlayMode tests for pure C# logic.
- Coupling tests to private implementation details.
- Missing `GameObject` cleanup in EditMode.
- Skipping analyzer checks after test-only changes.
- Gaming coverage metrics without behavior focus.

## Testing

- This playbook is validated by applying it when authoring tests; run `validate-changes.cmd` after substantive test additions.
- Expected: gate passes with zero failures when tests and product code are correct.
- Bugfix rule: every production bug gets a permanent regression test (`AGENTS.md`).

## AI Agent Context

- Invariants: every module keeps tests; bug fixes require regression tests; follow `Docs/Testing.md` for commands.
- Allowed Dependencies: UTF test assemblies, NUnit, Unity Test Framework.
- Forbidden Dependencies: N/A for tests beyond respecting `.asmdef` boundaries.
- Change Checklist: update module `Docs/` if exception policy applies; run gate; follow `ModuleTestCoverage` plan when expanding coverage.
- Known Tricky Areas: PlayMode flake; analyzer structural tests; coverage vs gate (coverage is separate).

## Related

- `Docs/Testing.md`
- `Architecture.md`
- `AGENTS.md`
- `Docs/Tools/Records.md`
- `Plans/ModuleTestCoverage/ModuleTestCoverage-ExecPlan.md`
- External: [Test Coverage (Martin Fowler)](https://martinfowler.com/bliki/TestCoverage.html), [Google code coverage best practices](https://testing.googleblog.com/2020/08/code-coverage-best-practices.html)

## Changelog

- 2025-03-23: Restructured to module documentation standard; condensed long baseline tables while preserving policies and pointers.
- Prior: Full per-module LOC baseline and extended metric definitions lived in this file — see git history if needed.
