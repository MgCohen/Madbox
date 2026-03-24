> **Disclaimer:** This README was written and dictated by me, but revised and formatted by AI.

# Madbox - Technical Test Notes

This README captures the execution notes for the game test.

---

## Introduction

**Important links**


| Location                 | Contents                                                                          |
| ------------------------ | --------------------------------------------------------------------------------- |
| `[Research/](Research/)` | Research notes and explorations                                                   |
| `[Docs/](Docs/)`         | All auto-generated documentation for the project (aligned with `Docs/` standards) |
| `[Plans/](Plans/)`       | ExecPlans and planning documents                                                  |
| `[Guides/](Guides/)`     | Short how-tos for uploading Addressables and backend assets                       |


**Why Unity Services?** The project needed a LiveOps-style surface: remote configuration alone (for example via **RemoteConfig** / **GSuit**) is only part of the story. Unity Gaming Services were chosen for fast integration on a time-boxed sample. The **architecture remains service-agnostic**—you could swap the backing provider behind the same boundaries.

**AI-first workflow.** Documentation, scripts, and agent-oriented tooling in this repo are shaped for use with AI assistants: clear entry points, explicit conventions, and validation hooks.

**Custom infrastructure.** This sample uses in-progress custom pieces (MVVM, navigation) because they were not the focus of the test and they reduced integration friction. They are implementation details, not claims about universal best practice.

**Authorship.** No line of code was hand-typed by me for this project; implementation was generated and iterated with AI tools, while **planning, architecture, and review** were done deliberately by yours truly as a personal challenge.

**Design trade-offs.** The brief asked that each addition demonstrate a **different skill** from what was already in the project. Some choices favor breadth and demonstration over the single “best” option for a production game.

### What is being delivered?

- **Addressables** — Full flow with remote catalogs and a documented catalog upload path.
- **LiveOps** — End-to-end client integration with player progression, remote configs, and dynamic messaging.
- **AI harness** — Custom Roslyn rules, validation scripts, and agent workflows (31 linter rules plus supporting automation).
- **Entities** — Attribute-driven entity authoring and runtime behavior.
- **UI architecture** — Source generation paired with MVVM-style separation between layers.
- **Composition** — Custom IOC layering on top of VContainer for scoped bootstrap and features.

---

## 1) Approach to the test (game-making phases)

The work was sequenced in distinct phases—research and planning first, then infrastructure, then vertical implementation—while staying aligned with the repo’s module map and boundaries (`Architecture.md`, `AGENTS.md`).

### Research (no code, no AI)

First, research as much as possible: clarify requirements, explore options, and capture a few directions with **hand sketches on paper**. This phase deliberately avoided code and AI tools so the problem space could settle without tooling bias.

### Deep research and early artifacts

Next, bring in tools for **deep research**: collect references, notes, and documents. With that material and my own conclusions, I used AI tools to produce **early sketches and plans**. Those artifacts live under `[Research/](Research/)`, including **diagrams and graphics** for the larger systems (drawn in a visual diagramming tool). Most of the big systems were sketched there before any serious implementation.

### Planning (ExecPlans)

After research came a **short planning pass** using the **ExecPlan strategy** described in `[PLANS.md](PLANS.md)` and the `[Plans/](Plans/)` folder: a fixed structure for plans, iterated with snippets, samples, API definitions, and structural outlines until the shape of the work was clear.

### Where to start: foundation before gameplay

The brief’s most visible and impactful slice would have been **gameplay and battle**. In practice, AI assistance is easier in a **more controlled environment**: the more of the stack is already defined and stable, the easier it is to steer generation toward the intended behavior and architecture. I therefore **started from the foundation** rather than from the battle loop first.

Because foundation was **not** the main focus of the test, the next move was to stand up **basic game infrastructure**. I pulled in **custom packages I had already built** (navigation, MVVM), brought in my **custom AI harness**, and grew it over the project—today that means **31 Roslyn analyzer rules**, validation scripts, and related automation.

### Implementation order

With **plans, research, infrastructure, and direction** in place, implementation proceeded **module by module from the ground up**, following the module layout that had already been sketched.

1. **Core platform layers** — LiveOps, Addressables, bootstrap, and whatever else the game needs to run end-to-end as a coherent client.
2. **Core / Meta (domain)** — Instead of jumping into huge feature systems, define the **main domain model** and early structures, then flesh out the Meta/Core layer: high-level services such as **level**, **game flow**, and **enemy** so the rest of the project shares a minimal, consistent contract.
3. **Choose a vertical** — With infrastructure, structure, and domain direction in place, pick **one side of the stack to finish first**. I started with the **backend**: a LiveOps-oriented service with a few simple modules (e.g. gold, level, ads). Time did not allow every module to be fully realized.
4. **Client-facing gameplay** — Then the **client**: enemies, battle, interactions, and feel. I played reference games (for example Archero-style titles) to study animation and pacing, then closed the loop with **final implementation**.
5. **Close-out** — Polish, documentation, and handoff.

### Quality loop (ongoing)

Throughout implementation, the repo’s **quality loop** stayed in play: Roslyn analyzers, regression tests where fixes land, and scripts under `.agents/scripts/` (for example `validate-changes.cmd`) as the milestone gate. That loop is **still evolving**—my personal workflow and harness are **not production-ready** yet; they were exercised here deliberately as **practice**, not as a finished pipeline.

---

## 2) Time spent per phase

| Phase                                                        | Approx. time |
| ------------------------------------------------------------ | ------------ |
| Research (no code, no AI)                                    | 1_h          |
| Deep research and early artifacts                            | 3_h          |
| Planning (ExecPlans)                                         | 1_h          |
| Foundation and infrastructure (navigation, MVVM, AI harness) | 1_h          |
| Core platform layers (LiveOps, Addressables, bootstrap)      | 3_h          |
| Core / Meta (domain)                                         | 2_h          |
| Backend vertical (LiveOps / Cloud Code modules)              | 1_h          |
| Client-facing gameplay                                       | 3_h          |
| Close-out (polish, documentation, handoff)                   | 2_h          |
| **Total**                                                    | 17_h         |


---

## 3) Features that were difficult and why

1. **Battle setup** — The final boundary where every module and layer had to meet to **start and run a match** was not planned tightly enough. Time pressure on top of that turned that seam into a **messy, shifting surface** instead of a clean contract.

2. **Automated testing** — As the project evolved, **AI-generated tests** often added noise rather than safety—even **solid tests were broken by later AI edits**, which created churn without a stable harness around it. The takeaway: I still need a **proper TDD-style workflow** (structure first, tests as guardrails) before leaning on generated tests at scale.

3. **Addressables Gateway** — The goal was a **small, ready-to-use Addressables wrapper** for the project. In practice it grew **more complex than necessary**: it does what we needed, but the design could be **much leaner and clearer** with another pass.

---

## 4) Features that could be improved and how

- **Graceful error handling and retry** — **LiveOps**, **initialization**, and **Addressables** all need clearer failure paths, retries, and degraded behavior. It is broadly missing today; I did not have time to do it justice.

- **Bootstrap layering** — The layered bootstrap was a **solid experiment**, but it still feels **heavier than necessary** for what I want the project to be. There is no one fatal flaw—mostly **cleanup and simplification**.

- **Custom analyzers (AI harness)** — These are part of the harness and still a **work in progress**. For a while they **nudged toward worse code** instead of better—and **traces of that are still visible in parts of the codebase today**. I am **tightening the rules incrementally** so the analyzers stay useful without fighting the codebase.

---

## 5) If I could go one step further

- **Richer LiveOps** — A more robust **LiveOps suite**: **rewards and inventory**, **client-side visual notifications**, and an **optimistic engine** (local-first updates with server reconciliation where it makes sense).

- **Performance and polish** — Profiling passes, allocation checks, and broader **code-quality cleanup** that had to be **skipped or deprioritized** to ship.

- **Calendar-based content** — Time-gated or scheduled content (events, rotations, daily/weekly hooks) on top of the existing remote-data story.

---

## 6) Live Ops system explanation

**Where to read more** — For implementation detail, deployment, and deeper notes:

| Kind | Pointers |
| ---- | -------- |
| **Docs** | [`Docs/LiveOps.md`](Docs/LiveOps.md) (backend layout and contracts), [`Docs/Core/LiveOps.md`](Docs/Core/LiveOps.md) (Unity client API and flow) |
| **Guides** | [`Docs/Guides/Upload-LiveOps-Cloud-Code-Backend.md`](Docs/Guides/Upload-LiveOps-Cloud-Code-Backend.md) (uploading the Cloud Code backend) |
| **Research** | [`Research/README.md`](Research/README.md) (index); e.g. [`Research/Backend/Backend-Research-and-Specs.md`](Research/Backend/Backend-Research-and-Specs.md) and other notes under [`Research/Backend/`](Research/Backend/) |

In code, LiveOps is the **typed Unity client** for a **Unity Cloud Code** module, using shared **DTO** assemblies (`Madbox.LiveOps.DTO.dll`). Conceptually, though, the system is simple:

### Events from config + persistence

Think of LiveOps as a **bag of events**. Each event is built from **two objects**: a **config** object (what the game designer / remote data says should be true) and a **persistence** object (what is stored for the player). By taking **player config** and **player persistence** for a given topic and **evaluating** them together, you produce **rich events** that can be passed through the rest of the game.

### Startup

When the game starts, a **central place** initializes **every module**: it loads the relevant config and persistence, evaluates them, and wires modules so the client has a coherent picture before feature code runs.

### Requests and responses

The client talks to **specific modules at any time** using **`ModuleRequest` / `ModuleResponse` pairs**. For example, an `AskRecommendationRequest` might return a `GetRecommendationsResponse`. Behind that call, the right **endpoint** loads or unloads whatever **config and persistence** it needs to **generate** that response.

### Nested responses (side effects)

On the **backend**, work for one call can trigger **side effects**. If a side effect produces its own **response**, that payload is **attached as a child** of the **main** task response and returned to the client as part of an **internal list** of nested responses—so one round-trip can carry the primary result plus follow-up work the server needed to report.

### RemoteConfig and GSuit

The starting point already had a **GSuit-driven** path for **configs**, but **not** for **player persistence**. I want LiveOps to be **server-authoritative**, so the **overall** system runs through **Unity Gaming Services** (Cloud Code in the middle, with clear ownership of data).

Split of responsibilities:

- **Config** — **RemoteConfig** and **GSuit** supply tunable, designer-facing configuration (where each plugs into the pipeline).
- **Persistence** — **Unity Cloud Save** holds per-player state the server can load, validate, and reconcile.
- **What the client consumes** — The **backend module** returns the **actual domain objects** you care about (merged, validated **events**), not oversized raw payloads for the UI to reinterpret.

Keeping **config and persistence minimal**—only what the pipeline needs—**saves cost** (storage, transfer, and cognitive surface) while the **authoritative story** still lives on the server.

---

## 7) Additional comments

- Single sources of truth: `**Architecture.md**` for module map, `**AGENTS.md**` for agent/build/test expectations, `**Docs/**` for per-module behavior.
- The stack is Unity **2022.3 LTS**, **URP**, **VContainer**, **MVVM**, **Addressables**, and **custom Roslyn analyzers** under `Analyzers/Scaffold/`.

