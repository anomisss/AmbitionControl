---
status: partial
phase: 10-rivals (Milestone F part 1) + World retest
build: v0.7.0-alpha.5 (DLL SHA-256 106bc9ac505d620f66efc83cb08014364406f892540058f8336fad45e7f3882c)
started: 2026-11-20
---

Run on a COPY of a save — **defeating a rival is permanent in that save.** Evidence = MelonLoader/Latest.log + owner observation.

| # | Test | Steps | Expected | Result |
|---|---|---|---|---|
| 1 | Tabs / layout | Visit all seven tabs | Labels fit; selected tab highlighted gold; nothing cut off | PARTIAL — log: Money (default), Player, World, Staff, Rivals, Options visited; Economy tab not clicked. No layout complaint from owner. |
| 2 | Rival list | Open Rivals | One row per neighbourhood: name, area, state (active / not active / defeated), weekly income; matches the game's Rivals app | PARTIAL — Rivals tab opened 3× (02:57:55, 02:58:02, 02:58:17), no errors logged; row contents not yet confirmed by owner. |
| 3 | Guard | Press Defeat on a "not active" rival (button should be greyed) | Nothing happens | NOT RUN |
| 4 | Defeat preview | Press Defeat on an active rival once | Status: "Defeat <name>? PERMANENT…"; button reads Confirm; nothing changed yet | NOT RUN |
| 5 | Defeat apply | Press Confirm | Log `Rival defeated (verified): <name> …`; row shows defeated; game shows its own defeat message | NOT RUN — no `Rival defeated` line in log. |
| 6 | Defeat all (optional) | Defeat all → Confirm | Status counts; log line per rival | NOT RUN |
| W1 | World: refuel | In your own car, part-empty tank: Refuel vehicle | Log `Vehicle refuel verified` (from 08-UAT #3) | NOT RUN |
| W2 | World: pause | Pause game, Resume game | Log `Pause False -> True`, `True -> False` (08-UAT #5) | PASS — log 02:58:59 `Pause True -> False`, 02:59:02 `Pause False -> True`, 02:59:07 `Pause True -> False`. |
| W3 | World: speed | 2x, then 1x | Log `Speed 200% requested; Time.timeScale …` (08-UAT #6) | PARTIAL — 10x pressed 4×; `Time.timeScale 1 -> 1` each time (0 -> 0 while paused). No readback evidence the speed changed; owner observation needed. 2x/1x not pressed. |
| W4 | World: save | Save game → Confirm save | Log `SaveGame("<name>") returned True` (08-UAT #8) | NOT RUN |

## Summary
total: 10 · pass: 1 · partial: 3 · fail: 0 · not run: 6

Evidence: Latest.log session 2026-11-20 02:57–02:59 (save day 10). Speed: `Time.timeScale` is not a usable readback (GameSpeedController tweens it via `timeScaleTween`); next build should read `GameSpeedController.currentGameSpeed` instead.
