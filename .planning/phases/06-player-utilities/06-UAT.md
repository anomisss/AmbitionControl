---
status: testing
phase: 06-player-utilities (Milestone B)
build: v0.7.0-alpha.1 (DLL SHA-256 d421c4f10d4e0237c0e442cda9a4e7a9b4632bbc0f23f438c0a638c0c35a3d4b)
started: 2026-11-19
---

Run on a COPY of a save. Evidence = MelonLoader/Latest.log lines quoted below + owner observation.

| # | Test | Steps | Expected | Result |
|---|---|---|---|---|
| 1 | Tabs / regression | Open phone app; click Money, Player, Game options | Three tabs switch; Money tab unchanged; Game options still show six switches | NOT RUN |
| 2 | Refill energy | Note energy; Player → Refill energy | Log `Refill verified: Energy X -> 100`; HUD energy full | NOT RUN |
| 3 | Refill hunger | Refill hunger | Log `Refill verified: Hunger X -> 100`; HUD hunger full | NOT RUN |
| 4 | Refill happiness | Refill happiness | Log `Refill verified: Happiness X -> 100`; HUD happiness full | NOT RUN |
| 5 | Refill all | Let needs drop a bit; Refill all needs | Status "All needs refilled (verified)"; three log lines | NOT RUN |
| 6 | Invincibility | Toggle ON, then OFF | Log `Invincibility ON verified.` / `OFF verified.` | NOT RUN |
| 7 | Hold hunger | Toggle ON; let in-game hours pass | Hunger stays ~full; log `Hold hunger full ON.` | NOT RUN |
| 8 | Hold hunger resets | With hold ON, return to main menu | Log `Hold hunger full OFF (save changed; N top-ups ...)` | NOT RUN |
| 9 | Scale check | After refill, compare app readout with HUD | Both show full (confirms 0–100 scale) | NOT RUN |

## Summary
total: 9 · pass: 0 · fail: 0 · not run: 9
