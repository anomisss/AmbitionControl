---
status: partial — owner chose to skip remaining tests (4–8) and continue
phase: 07-economy-controls
build: v0.7.0-alpha.2 (DLL SHA-256 8c4703a277932a959a885389f3608221b89ab1094f9f01d0114af71b098db3a4)
started: 2026-11-19
---

Run on a COPY of a save. Evidence = MelonLoader/Latest.log + owner observation.

| # | Test | Steps | Expected | Result |
|---|---|---|---|---|
| 1 | Tabs / regression | Open app; visit Money, Player, Economy, Options | Four tabs; Money and Player unchanged; Options shows 10 switches, nothing cut off | PARTIAL — Economy tab opened (log); other tabs/layout not reported |
| 2 | Presets load | Open Economy tab | Log `Game preset Easy: …`, `Normal: …`, `Hard: …` (the game's own values); labels show values + matching preset | PASS — log: Easy (tax 2%, market x0.7, salaries x0.5, rivals x0.7, bank x0.7, neg rate -0.3, promo x0.75, wholesale fee x0.1, importer fee x0.5); Normal (5%, x0.7, x0.7, x1, x0.7, -0.5, x0.55, x0.2, x0.75); Hard (30%, x1.3, x1.3, x1.2, x1.3, -5, x0.5, x0.3, x1) |
| 3 | Cycle a setting | Tap Tax (or Market prices) once | Log `Tax X% -> Y% verified.`; label updates | PASS — `Customer promotion x0.75 -> x0.5 -> x0.55 -> x0.75 verified` (returned to original) |
| 4 | Preset confirm | Tap "Hard preset" once, then "Confirm Hard" | First tap only shows "N of 9 settings would change"; second applies; log `Hard preset applied and verified`; label "Hard (current)" | NOT RUN |
| 5 | Restore | Apply your original preset (the one marked current before test 4) | All labels back to that preset | NOT RUN |
| 6 | Unlock switches | Options: toggle All courses unlocked ON then OFF | Log `All courses unlocked ON verified.` / `OFF verified.` | NOT RUN |
| 7 | Effect check (optional) | Tax/market price: observe in-game prices or tax screen; courses: check course list | Game reflects the change (may need reload) | NOT RUN |
| 8 | Layout | Screenshot of Economy tab | Labels readable, nothing cut off | NOT RUN |

## Summary
total: 8 · pass: 2 · partial: 1 · fail: 0 · not run: 5
