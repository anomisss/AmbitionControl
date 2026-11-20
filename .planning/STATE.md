# Project State

## Project Reference

See: .planning/PROJECT.md (updated 2026-11-19)

**Core value:** Money changes apply through the game's own transaction path, are verified by readback, and never corrupt a save.
**Current focus:** Phase 3 — Money Feature Verification

## Current Position

Phase: 3 of 5 (Money Feature Verification)
Plan: UAT (03-UAT.md) — 8 owner tests
Status: Testing — v0.5.0-alpha.2 installed (DLL 4ae7098d…), awaiting owner UAT run
Last activity: 2026-11-19 — Phase 3 UAT checklist + evidence logging

Progress: [████░░░░░░] 40%

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
Stopped at: Phase 2 complete; next is Phase 3 (Remove, Set, Undo, persistence, save switching)
Resume file: .planning/phases/03-money-feature-verification/03-UAT.md
