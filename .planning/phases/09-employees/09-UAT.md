---
status: complete (4 pass, 1 partial, 1 not run)
phase: 09-employees (Milestone E, part 1)
build: v0.7.0-alpha.4 (DLL SHA-256 5d53d475247d4536654567bbe8beb1f45f5933f3aad7d91abb3298095ffc90db)
started: 2026-11-19
---

Run on a COPY of a save that has hired employees. Evidence = MelonLoader/Latest.log + owner observation.

| # | Test | Steps | Expected | Result |
|---|---|---|---|---|
| 1 | Tabs / regression | Visit all six tabs | Labels fit; earlier tabs unchanged | PARTIAL — Staff and Options tabs switched (log); others not revisited |
| 2 | Employee list | Open Staff | Summary (count, avg satisfaction, wages/h) and up to 7 names per page; matches the game's employee app; log `Employees: N hired; satisfaction min …, max …; game LowSatisfactionThreshold T.` | PASS — `Employees: 5 hired; satisfaction min 44, max 100; game LowSatisfactionThreshold 20.` |
| 3 | Paging | Next > / < Prev (if more than 7 employees) | Page counter changes | PASS — 5 employees = 1 page; Next / Prev clicks stayed in range (clamped) |
| 4 | Max satisfaction preview | Tap Max satisfaction once | Status shows "for X of N employees? … Press Confirm."; nothing changed yet | PASS — first press logged, no change applied until second press |
| 5 | Max satisfaction apply | Tap Confirm | Log `Employee satisfaction set to 100: N/N verified`; list shows 100 | PASS — `Employee satisfaction set to 100: 5/5 verified (4 were below max).` |
| 6 | Recalculation | Let an in-game hour or two pass | Values drift back toward the game's own calculation (documents "temporary") | NOT RUN |

## Summary
total: 6 · pass: 4 · partial: 1 · fail: 0 · not run: 1
