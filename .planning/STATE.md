# Project State

## Project Reference

See: .planning/PROJECT.md (updated 2026-11-19)

**Core value:** Money changes apply through the game's own transaction path, are verified by readback, and never corrupt a save.
**Current focus:** Phase 18 — remaining features + bulk test (owner prefers bulk testing)

## Current Position

Phase: 10 (Rivals)
Plan: Phase 16 implemented
Status: v0.7.0-alpha.15 installed (DLL 966fd628…); awaiting owner bulk UAT (18-UAT.md, includes 17-UAT). 16-UAT: business list, Go teleport, Skip 1h PASS. Earlier alpha.11: 15-UAT: market wages + candidate PASS; time skip not run. Style (alpha.10) approved by owner. Earlier alpha.8: 13-UAT partial (all tabs open, no errors; visual not confirmed); 12-UAT not run. Previously v0.7.0-alpha.6: 11-UAT partial: tabs, quest display, complete quest PASS; teleport + complete objective not run. Phase 10: pause + 10x speed PASS; rival defeat, refuel, save not run.
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
Stopped at: Phase 18 awaiting bulk in-game test
Resume file: .planning/phases/18-bulk/18-UAT.md
