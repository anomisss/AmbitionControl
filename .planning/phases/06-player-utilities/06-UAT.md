---
status: complete (7 pass, 1 partial, 1 not run)
phase: 06-player-utilities (Milestone B)
build: v0.7.0-alpha.1 (DLL SHA-256 d421c4f10d4e0237c0e442cda9a4e7a9b4632bbc0f23f438c0a638c0c35a3d4b)
started: 2026-11-19
---

Run on a COPY of a save. Evidence = MelonLoader/Latest.log lines quoted below + owner observation.

| # | Test | Steps | Expected | Result |
|---|---|---|---|---|
| 1 | Tabs / regression | Open phone app; click Money, Player, Game options | Three tabs switch; Money tab unchanged; Game options still show six switches | PASS — log: Player, Game options, Money tab clicks |
| 2 | Refill energy | Note energy; Player → Refill energy | Log `Refill verified: Energy X -> 100`; HUD energy full | PASS — `Refill verified: Energy 73.1 -> 100`; owner: HUD full |
| 3 | Refill hunger | Refill hunger | Log `Refill verified: Hunger X -> 100`; HUD hunger full | PASS — `Refill verified: Hunger 59.6 -> 100`; owner: HUD full |
| 4 | Refill happiness | Refill happiness | Log `Refill verified: Happiness X -> 100`; HUD happiness full | PASS — `Refill verified: Happiness 35 -> 100`; owner: HUD full |
| 5 | Refill all | Let needs drop a bit; Refill all needs | Status "All needs refilled (verified)"; three log lines | PASS (mechanism) — three `Refill verified` lines; needs were already 100 |
| 6 | Invincibility | Toggle ON, then OFF | Log `Invincibility ON verified.` / `OFF verified.` | PARTIAL — `Invincibility ON verified.`; OFF not run |
| 7 | Hold hunger | Toggle ON; let in-game hours pass | Hunger stays ~full; log `Hold hunger full ON.` | PASS — `Hold hunger full ON.`; owner: hunger stayed full |
| 8 | Hold hunger resets | With hold ON, return to main menu | Log `Hold hunger full OFF (save changed; N top-ups ...)` | NOT RUN — game closed without returning to main menu |
| 9 | Scale check | After refill, compare app readout with HUD | Both show full (confirms 0–100 scale) | PASS — owner: HUD bars full after refill (0–100 scale confirmed) |

## Summary
total: 9 · pass: 7 · partial: 1 · fail: 0 · not run: 1
