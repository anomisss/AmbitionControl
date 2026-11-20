---
status: testing
phase: 12-clean-goals (Milestone G) + carried-over retests
build: v0.7.0-alpha.7 (DLL SHA-256 5cfb9a540c5675412dda2d172dfd5ee682508b7cffe85127bddd054cac53a063b)
started: 2026-11-20
---

Run on a COPY of a save — **completing personal goals cannot be undone.** Evidence = MelonLoader/Latest.log + owner observation.

| # | Test | Steps | Expected | Result |
|---|---|---|---|---|
| 1 | World layout | Open World | Clean vehicle + Save game share a row; vehicle line shows dirt; nothing cut off | NOT RUN |
| 2 | Clean vehicle | In your own dirty car: Clean vehicle | Log `Vehicle clean verified: dirtiness X -> 0`; car looks clean | NOT RUN |
| 3 | Goals count | Open Quest | Button shows `(done/total done)` | NOT RUN |
| 4 | Goals preview | Press Complete all personal goals once | Status asks to confirm with counts; nothing changes | NOT RUN |
| 5 | Goals apply | Press Confirm | Log `Personal goals completed (verified): A -> B of N`; Persona app shows goals completed | NOT RUN |
| R1 | Refuel (carried) | Own car, part-empty: Refuel vehicle | Log `Vehicle refuel verified` | NOT RUN |
| R2 | Save (carried) | Save game → Confirm save | Log `SaveGame(...) returned True` | NOT RUN |
| R3 | Teleport (carried) | Outdoors on foot: Teleport to quest target | Log `Teleport verified: quest target` | NOT RUN |
| R4 | Complete objective (carried) | Complete objective → Confirm | Log `Objective completed (verified)` | NOT RUN |

## Summary
total: 9 · pass: 0 · fail: 0 · not run: 9
