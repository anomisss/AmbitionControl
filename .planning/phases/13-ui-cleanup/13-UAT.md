---
status: testing
phase: 13-ui-cleanup
build: v0.7.0-alpha.8 (DLL SHA-256 04354aa34597d4d6ee7cedce12e81fd6d1dba5870455369f3626bbcb4c1c68e4)
started: 2026-11-20
---

Layout-only change. Evidence = owner observation (+ MelonLoader/Latest.log for errors).

| # | Test | Steps | Expected | Result |
|---|---|---|---|---|
| 1 | Headers | Visit all eight tabs | Gold section headers with a thin line; no overlapping buttons or text; nothing cut off | NOT RUN |
| 2 | Status colours | Refill energy (green); press Save game once (amber); press a vehicle tool on foot is greyed — try Stage Remove beyond balance (red) | Bar colour matches the result | NOT RUN |
| 3 | Hint line | Switch tabs | One short line per tab; amber "Unavailable: …" when a tab is blocked (e.g. while saving) | NOT RUN |
| 4 | Footer | Watch the bottom line | `$balance | Day N HH:MM | Energy Hunger Happy` updating | NOT RUN |
| 5 | No regressions | Use one button per tab | Same behaviour as before; no errors in the log | NOT RUN |

## Summary
total: 5 · pass: 0 · fail: 0 · not run: 5
