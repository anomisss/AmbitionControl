---
status: testing
phase: 03-money-feature-verification
source: [02-02-SUMMARY.md]
started: 2026-11-19
updated: 2026-11-19
build: v0.5.0-alpha.2 (adds Session/Scene/app-hidden log lines for evidence)
---

## Current Test

number: 1-8 (one play session)
name: Money feature UAT on a COPY of a save
awaiting: user response ("Test done"); evidence read from MelonLoader/Latest.log

## Tests

### 1. Remove
expected: Phone app: Clear, +1K, Stage Remove, Confirm. Status "Verified", balance down $1,000. Log "Remove verified".
requirement: MONEY-02
result: [pending]

### 2. Over-removal refused
expected: Clear, then pick an amount larger than the current balance (e.g. +10M when the balance is under $10M), Stage Remove. Staging is refused with "Result would be negative or exceed $100M."; nothing applied.
requirement: MONEY-02
result: [pending]

### 3. Set
expected: Clear, +1M, Stage Set, Confirm. Balance becomes exactly $1,000,000. Log "Set verified: ... -> 1000000".
requirement: MONEY-03
result: [pending]

### 4. Undo
expected: Immediately after test 3, Undo. Balance returns to the value before Set. Log "Undo verified".
requirement: MONEY-04
result: [pending]

### 5. Undo expires
expected: Make a change, then let the in-game balance change on its own (spend money or wait for a transaction). Undo button becomes disabled.
requirement: MONEY-04
result: [pending]

### 6. Status readout
expected: Day/time, money, net worth, energy, hunger, happiness in the app match the game HUD.
requirement: STAT-01
result: [pending]

### 7. Switch phone apps
expected: Open another phone app; the Ambition Control panel disappears. Log "Phone app hidden: another phone app was opened." Reopen it: panel shows again.
requirement: MENU-05
result: [pending]

### 8. Persistence across save/reload
expected: Note balance, save the game (in-game save), quit to main menu, load the same save. Log "Session: save loaded, balance $X" equals the noted balance. Log also shows "Scene loaded" (state cleared).
requirement: MONEY-05, MONEY-06
result: [pending]

## Summary

total: 8
passed: 0
issues: 0
pending: 8
