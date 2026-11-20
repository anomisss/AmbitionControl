---
status: partial
phase: 16-business (Milestone I) + carried retest
build: v0.7.0-alpha.12 (DLL SHA-256 3d71a0b2426bab6a6a9d825d26f85ac5d7810a188492761a52cbb7daf3b5d1a7)
started: 2026-11-20
---

Evidence = MelonLoader/Latest.log + owner observation.

| # | Test | Steps | Expected | Result |
|---|---|---|---|---|
| 1 | Tabs | Nine tabs | Labels readable (auto-size); underline follows | PASS — nine tabs used (Business, World); no layout issue reported |
| 2 | Business list | Open Business | Your buildings with name, address, rented/owned; log `Businesses: N rented/owned — …` | PASS — 15:39:19.8 `Businesses: 2 rented/owned — 45 3rd Street @ ThirdStreet 45 sat 50; bizmart @ ThirdStreet 66 sat 73` |
| 3 | Satisfaction | Compare with BizMan | Overall + parts match the game's numbers | PARTIAL — values read (50, 73); not yet compared with BizMan by owner |
| 4 | Go | Outdoors on foot, press Go on a business | You arrive at the business; log `Teleport verified: <name> (<address>)` | PASS — 15:39:36.7 `Teleport verified: bizmart …, moved 165.9 m`; 15:39:44.9 `Teleport verified: 45 3rd Street …, moved 25.7 m` |
| 5 | Go guard | Inside a building or vehicle | Go greyed; hint says why | NOT RUN |
| R1 | Skip 1h (carried) | World → Skip 1h → Confirm 1h | Log `Time skip finished: … (reached)` | PASS — 15:39:51.3 `Time skip started (verified running)`, 15:39:51.9 `Time skip finished: … (reached)` |

## Summary
total: 6 · pass: 4 · partial: 1 · fail: 0 · not run: 1 (Go guard)

Evidence: Latest.log 2026-11-20 15:39 (save day 11), no errors. Found: Il2Cpp `Address`/`Timestamp` printed as type names when concatenated (cosmetic) — fixed in v0.7.0-alpha.13 via `GameText` ("45 Third Street", "Day 11 01:12").
