# Project State

## Project Reference

See: .planning/PROJECT.md (updated 2026-11-19)

**Core value:** Money changes apply through the game's own transaction path, are verified by readback, and never corrupt a save.
**Current focus:** Phase 2 — Menu Input Isolation

## Current Position

Phase: 2 of 5 (Menu Input Isolation)
Plan: 1 of 1 in current phase
Status: Checkpoint — installed (DLL SHA-256 d36973b1…), awaiting owner in-game test (plan task 4)
Last activity: 2026-11-19 — InputBlocker (Harmony prefixes on InputActionHelper) implemented and compiled

Progress: [██░░░░░░░░] 20%

## Accumulated Context

### Decisions

- [Phase 1]: Menu clicks come from `Mouse.current`; IMGUI only draws (IMGUI clicks failed over the world)
- [Phase 1]: Only `GUI.Box/Button/Label(Rect, string)` are proven at runtime on this IL2CPP build
- [Phase 2]: Block click-through by patching `InputActionHelper` mouse reads, filtered to Click/RightClick/Zoom/CursorScroll

### Pending Todos

- Owner to push local commits: `git push origin compat/ea-0.9.3233-melonloader`

### Blockers/Concerns

- In-game testing can only be done by the owner; the game must be closed to install a new DLL (file lock)
- IL2CPP may inline `InputActionHelper` methods at some call sites; those reads would not be filtered

## Deferred Items

- v2 features (player needs, time controls) need API discovery on build 3233

## Session Continuity

Last session: 2026-11-19
Stopped at: Phase 2 plan 02-01 tasks 1-3 done; task 4 (owner in-game test) pending
Resume file: .planning/phases/02-menu-input-isolation/02-01-PLAN.md
