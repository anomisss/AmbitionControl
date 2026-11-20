# Project State

## Project Reference

See: .planning/PROJECT.md (updated 2026-11-19)

**Core value:** Money changes apply through the game's own transaction path, are verified by readback, and never corrupt a save.
**Current focus:** Phase 4 — Expanded Features (FEAT-01 energy/happiness switches)

## Current Position

Phase: 4 of 5 (Expanded Features)
Plan: 04-02 (all six game option switches + tabs)
Status: Checkpoint — 0.6.0-alpha.2 installed (DLL f2ef9036…); awaiting owner in-game test of the Game options tab
Last activity: 2026-11-19 — 04-02: six game option switches, Money/Game options tabs

Progress: [███████░░░] 70%

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
Stopped at: Phase 3 complete; next choose Phase 4 features
Resume file: .planning/phases/04-expanded-features/04-02-PLAN.md
