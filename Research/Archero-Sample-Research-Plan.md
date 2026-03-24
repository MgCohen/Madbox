# Archero-style scope — archived research

**Status:** **Archived.** This file replaces a longer early research and delivery plan. It is **not** an implementation specification.

**Sample project scope:** This repository is intentionally **limited in scope**. It demonstrates **specific technologies and patterns** (layered bootstrap, MVVM-style UI, LiveOps + Cloud Code, Addressables, Roslyn analyzers, automated tests). Choices illustrate **tradeoffs and integration**, not a claim that every stack decision is optimal for all production games.

## Current source of truth

- **[`Architecture.md`](../Architecture.md)** — module boundaries, diagrams, verification  
- **[`AGENTS.md`](../AGENTS.md)** — build, test, and agent workflows  
- **[`Docs/`](../Docs/)** — per-module behavior and operational guides  

If anything below disagrees with **`Assets/Scripts/`**, **`.asmdef` references**, or **`Docs/`**, treat this file as **stale**.

## What this research was for

Early alignment around an Archero-like loop: one-finger movement, enemy waves, weapons with tunable stats, **Addressables**, a **full game loop**, and **Live Ops** (Cloud Code + DTOs) so remote data can drive tuning across sessions.

The **original** document also contained a proposed module tree, a phased delivery checklist, and a “current baseline” snapshot from an early checkout. That material is **obsolete**: the **implemented** module layout is under `Assets/Scripts/` and is documented in `Architecture.md` and `Docs/`.

## Related research (use with care)

These remain as **design notes**; reconcile with code when they conflict:

- [`Research/Layers and flow/Layers and flow.md`](Layers%20and%20flow/Layers%20and%20flow.md)  
- [`Research/Core-Loop/Core-Loop-Research-and-Specs.md`](Core-Loop/Core-Loop-Research-and-Specs.md)  
- [`Research/Battle/Battle-Research-and-Specs.md`](Battle/Battle-Research-and-Specs.md)  
- [`Research/Entities/Entity-Research-and-Specs.md`](Entities/Entity-Research-and-Specs.md)  

See also other notes under `Research/` for related context.
