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
- [ ] **MONEY-02**: Remove works and is refused when the result would be negative
- [ ] **MONEY-03**: Set works to an exact balance
- [x] **MONEY-04**: Undo reverses the last change only while the same save is loaded and the balance is unchanged since
- [x] **MONEY-05**: A verified change persists after the game saves, quits and reloads
- [x] **MONEY-06**: Staged/undo state clears when returning to the main menu or loading another save

### Status (STAT)

- [x] **STAT-01**: Status tab shows day/time, money, net worth, energy, hunger, happiness matching the in-game HUD

### Release (REL)

- [ ] **REL-01**: Release package contains only the built DLL plus install/upgrade/uninstall instructions and checksum
- [ ] **REL-02**: Changelog/version updated; release published only with owner approval

## v2 Requirements

Deferred; each needs API verification on build 3233 first.

- **FEAT-01**: Edit player needs (energy, hunger, happiness)
- **FEAT-02**: Time controls (skip hours / set time)
- **FEAT-03**: Additional economy tools (e.g. pay off loans/taxes) — scope TBD with owner

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
| MONEY-02 (refusal), MONEY-03 | Phase 3 | Pending (UAT round 2) |
| FEAT-01..03 | Phase 4 | Pending |
| REL-01, REL-02 | Phase 5 | Pending |

---
*Last updated: 2026-11-19 after GSD initialization*
