# Project State

## Project Reference

See: .planning/PROJECT.md (updated 2026-11-19)

**Core value:** Money changes apply through the game's own transaction path, are verified by readback, and never corrupt a save.
**Current focus:** Phase 12 — Clean vehicle & personal goals (untested: teleport, complete objective, rival defeat, refuel, save)

## Current Position

Phase: 10 (Rivals)
Plan: Phase 12 implemented
Status: v0.7.0-alpha.7 installed (DLL 5cfb9a54…); awaiting owner UAT (12-UAT.md). Previously v0.7.0-alpha.6: 11-UAT partial: tabs, quest display, complete quest PASS; teleport + complete objective not run. Phase 10: pause + 10x speed PASS; rival defeat, refuel, save not run.
Last activity: 2026-11-20 — Rivals tab (list, guarded defeat)

Progress: [██████████] 100%

## Accumulated Context

### Decisions

- [Phase 1]: Menu clicks come from `Mouse.current`; IMGUI only draws (IMGUI clicks failed over the world)
- [Phase 1]: Only `GUI.Box/Button/Label(Rect, string)` are proven at runtime on this IL2CPP build
- [Phase 2]: Helper patches alone failed in-game; added disabling of the game InputActions while hovering (alpha.3)
- [Phase 2]: Candidate: move menu into the in-game smartphone as a uGUI app (pattern seen in the Alunas trainer)

### Pending Todos

- Owner to push local commits: `git push origin compat/ea-0.9.3233-melonloader`

### Blockers/Concerns

- In-game testing can only be done by the owner; the game must be closed to install a new DLL (file lock)
- IL2CPP may inline `InputActionHelper` methods at some call sites; those reads would not be filtered

## Deferred Items

- v2 features (player needs, time controls) need API discovery on build 3233

## Session Continuity

Last session: 2026-11-19
Stopped at: Phase 12 awaiting in-game test
Resume file: .planning/phases/12-clean-goals/12-UAT.md
