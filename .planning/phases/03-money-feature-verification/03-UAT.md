---
status: complete
phase: 03-money-feature-verification
source: [02-02-SUMMARY.md]
started: 2026-11-19
updated: 2026-11-19
build: v0.5.0-alpha.2 (adds Session/Scene/app-hidden log lines for evidence)
---

## Current Test

number: 1-8 (one play session)
name: Money feature UAT on a COPY of a save
awaiting: none

## Tests

### 1. Remove
expected: Phone app: Clear, +1K, Stage Remove, Confirm. Status "Verified", balance down $1,000. Log "Remove verified".
requirement: MONEY-02
result: pass
evidence: "Remove verified: 15145598.00 -> 15144598.00"

### 2. Over-removal refused
expected: Clear, then pick an amount larger than the current balance (e.g. +10M when the balance is under $10M), Stage Remove. Staging is refused with "Result would be negative or exceed $100M."; nothing applied.
requirement: MONEY-02
result: pass (owner-reported)
reported: "Tested, all worked"
note: no log evidence — Latest.log and MelonLoader/Logs show no game session after 20:57 (v0.5.0-alpha.2); alpha.3 ("Can't stage" message) never logged as run.

### 3. Set
expected: Clear, +1M, Stage Set, Confirm. Balance becomes exactly $1,000,000. Log "Set verified: ... -> 1000000".
requirement: MONEY-03
result: pass (owner-reported)
reported: "Tested, all worked"
note: no log evidence (see test 2).

### 4. Undo
expected: Immediately after test 3, Undo. Balance returns to the value before Set. Log "Undo verified".
requirement: MONEY-04
result: pass
evidence: "Undo verified: 5144598.00 -> 15144598.00" and "Undo verified: 6144598.00 -> 5144598.00"

### 5. Undo expires
expected: Make a change, then let the in-game balance change on its own (spend money or wait for a transaction). Undo button becomes disabled.
requirement: MONEY-04
result: pass (owner-reported)
reported: "Tested, all worked"
note: no log evidence (see test 2).

### 6. Status readout
expected: Day/time, money, net worth, energy, hunger, happiness in the app match the game HUD.
requirement: STAT-01
result: pass
reported: "Yes, they matched"

### 7. Switch phone apps
expected: Open another phone app; the Ambition Control panel disappears. Log "Phone app hidden: another phone app was opened." Reopen it: panel shows again.
requirement: MENU-05
result: pass
reported: "Ours hid, other showed"
note: no "Phone app hidden" log line; the game itself deactivates app screens (including ours) when switching. Harmless.

### 8. Persistence across save/reload
expected: Note balance, save the game (in-game save), quit to main menu, load the same save. Log "Session: save loaded, balance $X" equals the noted balance. Log also shows "Scene loaded" (state cleared).
requirement: MONEY-05, MONEY-06
result: pass
evidence: last verified balance $15,144,598.00; after save -> main menu -> reload "Session: save loaded, balance $15,144,279.00" (owner: -$319 was normal game spending). "Scene loaded: MainMenu ... cleared" and app re-injected.

## Summary

total: 8
passed: 8 (5 log-verified, 3 owner-reported: tests 2, 3, 5)
issues: 0
skipped: 0
