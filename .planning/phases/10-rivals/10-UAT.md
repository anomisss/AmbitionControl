---
status: testing
phase: 10-rivals (Milestone F part 1) + World retest
build: v0.7.0-alpha.5 (DLL SHA-256 106bc9ac505d620f66efc83cb08014364406f892540058f8336fad45e7f3882c)
started: 2026-11-20
---

Run on a COPY of a save — **defeating a rival is permanent in that save.** Evidence = MelonLoader/Latest.log + owner observation.

| # | Test | Steps | Expected | Result |
|---|---|---|---|---|
| 1 | Tabs / layout | Visit all seven tabs | Labels fit; selected tab highlighted gold; nothing cut off | NOT RUN |
| 2 | Rival list | Open Rivals | One row per neighbourhood: name, area, state (active / not active / defeated), weekly income; matches the game's Rivals app | NOT RUN |
| 3 | Guard | Press Defeat on a "not active" rival (button should be greyed) | Nothing happens | NOT RUN |
| 4 | Defeat preview | Press Defeat on an active rival once | Status: "Defeat <name>? PERMANENT…"; button reads Confirm; nothing changed yet | NOT RUN |
| 5 | Defeat apply | Press Confirm | Log `Rival defeated (verified): <name> …`; row shows defeated; game shows its own defeat message | NOT RUN |
| 6 | Defeat all (optional) | Defeat all → Confirm | Status counts; log line per rival | NOT RUN |
| W1 | World: refuel | In your own car, part-empty tank: Refuel vehicle | Log `Vehicle refuel verified` (from 08-UAT #3) | NOT RUN |
| W2 | World: pause | Pause game, Resume game | Log `Pause False -> True`, `True -> False` (08-UAT #5) | NOT RUN |
| W3 | World: speed | 2x, then 1x | Log `Speed 200% requested; Time.timeScale …` (08-UAT #6) | NOT RUN |
| W4 | World: save | Save game → Confirm save | Log `SaveGame("<name>") returned True` (08-UAT #8) | NOT RUN |

## Summary
total: 10 · pass: 0 · fail: 0 · not run: 10
