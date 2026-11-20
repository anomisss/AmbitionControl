---
status: testing
phase: 16-business (Milestone I) + carried retest
build: v0.7.0-alpha.12 (DLL SHA-256 3d71a0b2426bab6a6a9d825d26f85ac5d7810a188492761a52cbb7daf3b5d1a7)
started: 2026-11-20
---

Evidence = MelonLoader/Latest.log + owner observation.

| # | Test | Steps | Expected | Result |
|---|---|---|---|---|
| 1 | Tabs | Nine tabs | Labels readable (auto-size); underline follows | NOT RUN |
| 2 | Business list | Open Business | Your buildings with name, address, rented/owned; log `Businesses: N rented/owned — …` | NOT RUN |
| 3 | Satisfaction | Compare with BizMan | Overall + parts match the game's numbers | NOT RUN |
| 4 | Go | Outdoors on foot, press Go on a business | You arrive at the business; log `Teleport verified: <name> (<address>)` | NOT RUN |
| 5 | Go guard | Inside a building or vehicle | Go greyed; hint says why | NOT RUN |
| R1 | Skip 1h (carried) | World → Skip 1h → Confirm 1h | Log `Time skip finished: … (reached)` | NOT RUN |

## Summary
total: 6 · pass: 0 · fail: 0 · not run: 6
