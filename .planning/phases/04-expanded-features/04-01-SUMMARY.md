---
phase: 04-expanded-features
plan: 01
status: complete
completed: 2026-11-19
requirements: [FEAT-01]
---

# 04-01 Summary: Energy/happiness drain switches

**Outcome:** Runtime verified (partly) on build 3233 / MelonLoader 0.7.3 — v0.6.0-alpha.1, DLL SHA-256 8BEA46CC…, commit 2618f5c.

## Evidence

- Log 2026-11-19 21:06: `Happiness drain OFF verified.`, `Energy drain OFF verified.` (flag written and read back).
- Owner: with drain OFF, energy and happiness stopped dropping as in-game time passed → the game reads the flags live, no reload needed.
- Owner: whole phone panel fits on screen with the new row.
- Owner chose to keep both switches OFF in that save.

## Not exercised

- Switching back ON (restoring drain) — not run; expected to work by the same flag, unconfirmed.
- Whether the flags persist across save/reload (expected: they are part of the save's GameVariables).

## Notes for later features

Same mechanism, already verified present in GameVariables: disableAging, disableVehicleDamage, disableVehicleFuel,
disableWholesaleAndImportLimits. No hunger flag in build 3233.
