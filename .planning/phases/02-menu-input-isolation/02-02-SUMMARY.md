---
phase: 02-menu-input-isolation
plan: 02
status: complete
completed: 2026-11-19
requirements: [MENU-04, MENU-05]
---

# 02-02 Summary: Ambition Control phone app

**Outcome:** Runtime verified on build 3233 / MelonLoader 0.7.3 (DLL SHA-256 C1441AF5…, commit 9e73e79).

## Evidence (MelonLoader Latest.log, 2026-11-19)

- `Phone app injected (cloned 'Persona', button component found, font Rubik-Medium SDF).`
- `Phone app opened.` / `Phone app: hid app screens [Persona]`
- `Phone click: +10K`, `+1M`, `Stage Add`, `Confirm change`
- `Add verified: 1595598.38 -> 2705598.50` (default $100K + $10K + $1M = $1,110,000 ✓)
- Owner report: "it works just fine" — no click-through from the phone UI.

## Not exercised in this run

- Undo, Remove, Set (Phase 3)
- Switching to another phone app and back (no log line for auto-hide)

## Decisions

- The phone app is the primary UI; the F10 overlay stays as a fallback. Plan 02-01 (overlay click-through
  blocking) is superseded: helper patches failed in-game, action disabling never tested.

## Files

- `MelonLoader/AmbitionControl.Melon/UI/PhoneApp.cs` (new), `AmbitionControlMod.cs` (wiring, `PhoneApp` pref)
