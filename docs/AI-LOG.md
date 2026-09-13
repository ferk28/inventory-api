# AI Usage Log

This file records how generative AI (Claude, Anthropic) was used during the development of this technical test, as required by the test brief. It lists the main prompts per phase, what the AI produced, and what I did with that output. Architectural decisions derived from these sessions are recorded in `ADR.md`.

Tool: Claude (claude.ai, Project workspace with the test PDF attached).
Working language with the AI: Spanish. All generated artifacts (spec, code, docs) are in English per Clean Code rule 1.

---

## Phase 0 — Planning and specification (2026-09-12)

### Prompt 1 — Roadmap
> "Configuré un proyecto aquí para realizar una prueba. Te di un PDF y las instrucciones, necesito empezar a realizar este proyecto, ¿puedes decirme paso a paso para hacerlo, siguiendo los puntos que dice el PDF?"

**AI output:** An 8-step roadmap (repo + AI docs → solution structure → database → TDD → endpoints → OAuth2 → Docker → Clean Code checklist). The AI flagged that, because the test evaluates Spec-Driven Development, the specification had to be written and committed before any code.

**My action:** Accepted the roadmap. Reordered nothing. Decided to keep SPEC.md, AI-LOG.md, ADR.md and decisions.md in a `docs/` folder.

### Prompt 2 — Specification
> "Sí, por favor" (in response to the offer to draft `SPEC.md` before generating any code)

**AI output:** `docs/SPEC.md` v1: domain model, business rules, full API contract, CQRS handler map, auth setup, environment variables, Docker layout, test plan, Clean Code constraints. The AI made three design assumptions explicitly and asked me to confirm them (see ADR-001..003).

**My action:** Reviewed the spec. <!-- TODO: write what you changed, e.g. "Removed initialStock", "Kept soft delete" -->

### Prompt 3 — Step 0 in detail
> "En la conversación anterior de este proyecto me diste pasos a realizar, ¿podemos empezar con el paso 0 detalladamente?"

**AI output:** Repository setup instructions, templates for this file, `ADR.md` and `decisions.md`, and the first commit message.

**My action:** <!-- TODO -->

---

## Phase 1 — Solution structure
<!-- Add prompts here as you go. Keep the same format: prompt → AI output → my action. -->

## Phase 2 — Database

## Phase 3 — TDD: commands and queries

## Phase 4 — API endpoints and Swagger

## Phase 5 — OAuth2

## Phase 6 — Docker

## Phase 7 — README and final review

---

## Summary of how the AI was supervised

<!-- Fill this in at the end. Suggested points:
- What the AI proposed that you rejected and why.
- Bugs or deviations from the spec you caught in generated code.
- Where you wrote code by hand instead of prompting.
-->