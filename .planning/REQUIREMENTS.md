# Requirements: Ambition Control

**Defined:** 2026-11-19
**Core Value:** Money changes apply through the game's own transaction path, are verified by readback, and never corrupt a save.

## v1 Requirements

### Loader & Safety (LOAD)

- [x] **LOAD-01**: Mod loads under MelonLoader 0.7.3 on build 3233 with no errors in `Latest.log`
- [x] **LOAD-02**: Runtime build guard reads `GameVersion.GetCurrent().buildNumber`; writes blocked on other builds unless opted in
- [x] **LOAD-03**: Errors are logged once per distinct message, never every frame

### Menu (MENU)

- [x] **MENU-01**: F10 (rebindable) toggles the menu at main menu and in a loaded save
- [x] **MENU-02**: Menu can be dragged by its title bar; position persists
- [x] **MENU-03**: Menu buttons respond to clicks in-game
- [x] **MENU-04**: Clicks and scroll over the open menu do not act on the game world (no walking, camera zoom, world selection)
- [x] **MENU-05**: Game controls behave normally when the cursor is outside the menu or the menu is closed

### Money (MONEY)

- [x] **MONEY-01**: Add stages a preview, applies on confirm, and reports Verified only after balance readback matches
- [x] **MONEY-02**: Remove works and is refused when the result would be negative
- [x] **MONEY-03**: Set works to an exact balance
- [x] **MONEY-04**: Undo reverses the last change only while the same save is loaded and the balance is unchanged since
- [x] **MONEY-05**: A verified change persists after the game saves, quits and reloads
- [x] **MONEY-06**: Staged/undo state clears when returning to the main menu or loading another save

### Status (STAT)

- [x] **STAT-01**: Status tab shows day/time, money, net worth, energy, hunger, happiness matching the in-game HUD

### Release (REL)

- [x] **REL-01**: Release package contains only the built DLL plus install/upgrade/uninstall instructions and checksum
- [x] **REL-02**: Changelog/version updated; release published only with owner approval

## v2 Requirements (trainer expansion — see docs/FEATURE_GAP_MATRIX.md)

- [x] **PLAYER-01**: Refill energy, hunger, happiness, all (game setters, readback)
- [~] **PLAYER-02**: Invincibility toggle (game command, readback) — ON verified; OFF not yet run
- [x] **PLAYER-03**: Hold hunger full (experimental, session-only)
- [ ] **ECON-01**: Difficulty presets Easy/Normal/Hard from game assets (two-press confirm)
- [ ] **ECON-02**: Nine economy settings cycle through game preset values with readback
- [ ] **ECON-03**: Unlock courses/contacts/importer products, tutorial switches
- [ ] **WORLD-01**: Repair / refuel own current vehicle (ownership-checked, readback)
- [ ] **WORLD-02**: Pause/resume, game speed (experimental), traffic, save game
- [x] **STAFF-01**: Read-only employee list (paged, summary)
- [x] **STAFF-02**: Temporary max satisfaction with affected-count preview and readback
- [ ] **RIVAL-01**: Rival list per neighbourhood (name, state, weekly income)
- [ ] **RIVAL-02**: Defeat rival / all active rivals (game method, two-press confirm, readback)
- [ ] **QUEST-01**: Teleport to quest target / GPS destination (on foot, outdoors; position readback)
- [ ] **QUEST-02**: Complete current objective / quest (game commands, two-press confirm, readback)
- [ ] **VEH-03**: Clean own vehicle (game setter, dirtiness readback)
- [ ] **GOAL-01**: Complete all personal goals (game command, two-press confirm, count readback)
- [x] **TIME-01**: Skip hours via the game time machine (two-press, end-time readback)
- [ ] **EMP-03**: Bulk wages to market rate (payroll preview, two-press, readback)
- [x] **EMP-04**: Generate candidate by skill and level (count readback)
- [x] **BIZ-01**: List rented/owned buildings with customer satisfaction
- [x] **BIZ-02**: Teleport to a business (address command, movement readback)
- [ ] **AGE-01**: Change age ±1 year (game command, bounded, two-press, days readback)
- [ ] **TP-02**: Mark a spot and return to it (game waypoint, movement readback)
- [ ] **MONEY-KEY-01**: Type a custom amount on an on-screen keypad (no game keystrokes)
- [ ] **WAGE-02**: Change one employee's wage (bounded $1-$500/h, readback)
- [ ] **BIZ-03**: Hold customer satisfaction at 100% (session only, re-applied after game recalculation)
- [ ] **REV-01**: Multiply business sales revenue (Revenue transactions only, readback)
- [ ] **VEH-01**: Get a vehicle (game command, two-press, vehicle-list readback)

Deferred; each needs API verification on build 3233 first.

- [x] **FEAT-01**: Energy and happiness drain switches (game options; hunger has no game option in build 3233)
- **FEAT-02**: Time controls (skip hours / set time)
- [x] **FEAT-03**: Remaining game option switches (aging, vehicle damage, vehicle fuel, wholesale/import limits)
- **FEAT-04**: Additional economy tools (e.g. pay off loans/taxes) — scope TBD with owner

## Out of Scope

| Feature | Reason |
|---------|--------|
| Multiplayer / anti-cheat bypass | Single-player utility only |
| Direct save-file editing | Changes must go through game APIs |
| Official SDK variant | MelonLoader is the target; SDK kept as history on `main` |

## Traceability

| Requirement | Phase | Status |
|-------------|-------|--------|
| LOAD-01..03 | Phase 1 | Complete |
| MENU-01..03 | Phase 1 | Complete |
| MONEY-01 | Phase 1 | Complete |
| MENU-04, MENU-05 | Phase 2 | Complete (phone app) |
| MONEY-04..06, STAT-01 | Phase 3 | Complete (UAT round 1) |
| MONEY-02 (refusal), MONEY-03 | Phase 3 | Complete (owner-reported, no log evidence) |
| FEAT-01 | Phase 4 | Complete (OFF verified; ON not yet exercised) |
| FEAT-03 | Phase 4 | Complete (OFF + readback verified; effects and ON not exercised) |
| FEAT-02, FEAT-04 | Phase 4 | Not started (optional) |
| REL-01 | Phase 5 | Complete (ZIP built from tested DLL) |
| REL-02 | Phase 5 | Complete — GitHub pre-release v0.6.0-alpha.2 |

---
*Last updated: 2026-11-19 after GSD initialization*
