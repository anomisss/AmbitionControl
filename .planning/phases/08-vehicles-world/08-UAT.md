---
status: testing
phase: 08-vehicles-world (Milestone C)
build: v0.7.0-alpha.3 (DLL SHA-256 551eb36161a3f378a16eee3591fcffef3f0066a4c844ad50cb5e0728b9210862)
started: 2026-11-19
---

Run on a COPY of a save. Evidence = MelonLoader/Latest.log + owner observation.

| # | Test | Steps | Expected | Result |
|---|---|---|---|---|
| 1 | Tabs / regression | Visit Money, Player, Economy, World, Options | Five tabs readable; earlier tabs unchanged | NOT RUN |
| 2 | Vehicle guard | On foot, open World | Repair/Refuel greyed; text "Not in a vehicle…" | NOT RUN |
| 3 | Refuel | In your own vehicle with less than a full tank: Refuel vehicle | Log `Vehicle refuel verified: fuel X -> max`; gauge full | NOT RUN |
| 4 | Repair | In your own damaged vehicle: Repair vehicle | Log `Vehicle repair verified: damage X -> 0` | NOT RUN |
| 5 | Pause / resume | Pause game, then Resume game | Log `Pause False -> True`, `True -> False`; world stops/starts | NOT RUN |
| 6 | Speed | Tap 2x, then 1x | Log `Speed 200% requested; Time.timeScale a -> b`; time visibly faster, then normal | NOT RUN |
| 7 | Traffic | Toggle traffic OFF then ON | Log `Traffic True -> False` / `False -> True`; cars stop spawning | NOT RUN |
| 8 | Save | Save game, then Confirm save | Log `SaveGame("<name>") returned True`; game shows its save notification | NOT RUN |

## Summary
total: 8 · pass: 0 · fail: 0 · not run: 8
