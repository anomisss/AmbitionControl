# Roadmap: Ambition Control

## Overview

From a verified-loading MelonLoader mod with one proven money operation, to a menu that doesn't leak input into the game, a fully in-game-verified money feature set, carefully verified extra features, and finally a packaged release.

## Phases

- [x] **Phase 1: MelonLoader Foundation** - Buildable mod, F10 menu, build guard, verified money Add
- [x] **Phase 2: Menu Input Isolation** - Menu clicks/scroll no longer reach the game world
- [x] **Phase 3: Money Feature Verification** - Remove, Set, Undo, persistence and save switching verified in-game
- [ ] **Phase 4: Expanded Features** - Player needs and time controls, each behind verified APIs
- [ ] **Phase 5: Release Packaging** - Installable package with docs, published with owner approval

## Phase Details

### Phase 1: MelonLoader Foundation
**Goal**: A real MelonLoader DLL that loads on build 3233, opens a working menu, and performs one verified money change
**Depends on**: Nothing (first phase)
**Requirements**: LOAD-01, LOAD-02, LOAD-03, MENU-01, MENU-02, MENU-03, MONEY-01
**Success Criteria** (what must be TRUE):
  1. Latest.log shows load + "Game build 3233 matches" with no mod errors ✓
  2. F10 menu opens, drags, and buttons respond in-game ✓
  3. Add $1M is applied and verified by readback ✓ (1,595,598.38 → 2,595,598.50)
**Plans**: Complete (commits 476afab → 75bb33d)

### Phase 2: Menu Input Isolation
**Goal**: Using the menu never moves the character, zooms the camera, or selects things in the world
**Depends on**: Phase 1
**Requirements**: MENU-04, MENU-05
**Success Criteria** (what must be TRUE):
  1. Clicking menu buttons while in the city does not move the player or select world objects
  2. Scrolling over the menu does not zoom the camera
  3. With the cursor outside the menu, or the menu closed, clicks/scroll/zoom work normally
  4. Log reports how many input patches applied; mod still works if a patch fails
**Plans**: 2 plans

Plans:
- [~] 02-01: Overlay click-through blocking — superseded by 02-02 (helper patches failed in-game)
- [x] 02-02: Ambition Control as an in-game smartphone app (uGUI) — runtime verified

### Phase 3: Money Feature Verification
**Goal**: Every money action and its safety rules are proven in-game, including persistence
**Depends on**: Phase 2
**Requirements**: MONEY-02, MONEY-03, MONEY-04, MONEY-05, MONEY-06, STAT-01
**Success Criteria** (what must be TRUE):
  1. Remove and Set produce "Verified"; over-removal is refused at preview
  2. Undo restores the prior balance; it expires after the balance changes
  3. Balance persists after save → quit → reload
  4. Switching saves clears staged/undo state
  5. Status tab matches the HUD
**Plans**: UAT (03-UAT.md) — 8/8 pass (5 log-verified, 3 owner-reported)

### Phase 4: Expanded Features
**Goal**: Add owner-chosen features without weakening save safety
**Depends on**: Phase 3
**Requirements**: FEAT-01, FEAT-02, FEAT-03
**Success Criteria** (what must be TRUE):
  1. Each feature's game API is confirmed from build-3233 interop before code is written
  2. Each write is verified by readback and can be disabled by preference
**Plans**: 04-01, 04-02 complete; FEAT-02 (time) and FEAT-04 (economy tools) optional

### Phase 5: Release Packaging
**Goal**: A downloadable, documented release the owner approves
**Depends on**: Phase 3 (Phase 4 optional)
**Requirements**: REL-01, REL-02
**Success Criteria** (what must be TRUE):
  1. ZIP contains only the DLL, README/install steps and SHA-256
  2. Published to GitHub only after owner approval
**Plans**: TBD

## Progress

| Phase | Plans Complete | Status | Completed |
|-------|----------------|--------|-----------|
| 1. MelonLoader Foundation | done | Complete | 2026-11-19 |
| 2. Menu Input Isolation | 1/1 (02-01 superseded) | Complete | 2026-11-19 |
| 3. Money Feature Verification | UAT 8/8 | Complete | 2026-11-19 |
| 4. Expanded Features | 2/2 (+ optional) | Core complete | 2026-11-19 |
| 5. Release Packaging | 0/TBD | Not started | - |
