---
status: testing
phase: 18-bulk (Milestone K + everything not yet run)
build: v0.7.0-alpha.15 (DLL SHA-256 966fd62811ee95c089003f082c2a400667f4472213c7c8f1ab8ca7fb9102c610)
started: 2026-11-21
---

Bulk test on a COPY of a save. Evidence = MelonLoader/Latest.log + owner observation. Items 17-x replace 17-UAT.

| # | Area | Test | Steps | Expected | Result |
|---|---|---|---|---|---|
| A1 | Money | Keypad | Money: Keypad, type 2 5 000, Stage Add, Confirm | Amount $25,000; balance +$25,000 verified; game ignores the typed keys | NOT RUN |
| A2 | Money | Keypad limits | Keypad: Del removes a digit; type past $100M | Del works; status "Can't type more" | NOT RUN |
| B1 | Staff | Select employee | Staff: tap a row | Row bold with ">"; hint changes | NOT RUN |
| B2 | Staff | Wage steps | +$1, +10%, -10%, -$1, Market | Log `Wage …: name a -> b (verified)` each; row updates | NOT RUN |
| B3 | Staff | All to market (renamed) | All to market → Confirm all | As 15-UAT #5 | NOT RUN |
| C1 | Business | Hold on | Business: Hold satisfaction 100% | Log `Satisfaction hold ON: n/n verified`; rows show 100% | NOT RUN |
| C2 | Business | Hold survives | Skip 4h (World) with the hold on | Still 100%; log `hook re-applied` and/or `fallback re-applied` | NOT RUN |
| C3 | Business | Hold off | Tap again; skip 1h | Log `Satisfaction hold OFF`; values return to the game's own | NOT RUN |
| D1 | Economy | Revenue x2 | Economy: tap Sales revenue (mod) until x2 | Log `2/2 money hooks installed`, `set to 2 (verified)` | NOT RUN |
| D2 | Economy | Revenue applied | Let a business sell (or skip 1h) | Log `Revenue hook: first sale seen`, `Revenue x2: sale $a -> $b`, `Revenue readback … (verified)`; button shows +$extra | NOT RUN |
| D3 | Economy | Revenue off | Cycle back to off (1x) | New sales not boosted (no `Revenue x` lines) | NOT RUN |
| E1 | World | Get vehicle | On foot outdoors: tap type (e.g. Vord Pony), Get vehicle → Confirm get | Log `Vehicle added (verified): …; vehicles a -> a+1; balance change 0.00`; car appears nearby | NOT RUN |
| E2 | World | Get vehicle guard | Inside a building | Button disabled; hint/`Go outside first` | NOT RUN |
| 17-1 | Player | Age display | Player tab | `Age N (…)` matches Persona | NOT RUN |
| 17-2 | Player | Age older | Older 1 year → Confirm older | Log `Age change: command unit measured as … (verified)`; Persona N+1 | NOT RUN |
| 17-3 | Player | Age younger | Younger 1 year → Confirm younger | Back to N (verified) | NOT RUN |
| 17-4 | Quest | Mark spot | Outdoors on foot: Mark this spot | Log `Waypoint 'AmbitionControl' set at …` | NOT RUN |
| 17-5 | Quest | Return | Move away, Return to spot | Log `Teleport verified: marked spot, moved N m` | NOT RUN |
| F1 | World | Refuel / Clean | In your own car: Refuel, Clean | Log `Vehicle refuel verified` and `Vehicle clean verified`; fuel max, dirt 0 | NOT RUN |
| F2 | World | Traffic | Traffic OFF then ON | Log `Traffic True -> False` then back; cars vanish/return | NOT RUN |
| F3 | World | Save | Save game → Confirm save | Log `SaveGame("…") returned True`; save time updates in the load menu | NOT RUN |
| F4 | World | Skip guard | In a vehicle | Skip buttons disabled | NOT RUN |
| G1 | Quest | Teleport quest target / GPS | Set GPS; tap each | Log `Teleport verified: …` | NOT RUN |
| G2 | Quest | Complete objective | Complete objective → Confirm | Log `Objective completed (verified)` | NOT RUN |
| G3 | Quest | Personal goals | Complete all personal goals → Confirm | Log `Personal goals completed (verified)` | NOT RUN |
| G4 | Business | Go guard | Inside a building | Go buttons disabled | NOT RUN |
| H1 | Rivals | View | Rivals tab | Names/state per neighbourhood | NOT RUN |
| H2 | Rivals | Defeat (copy save!) | Defeat → Confirm on one active rival | Log `Rival defeated (verified)`; permanent | NOT RUN |
| I1 | Economy | Settings cycle | Tap each economy setting once | Log verified for each | NOT RUN |
| I2 | Economy | Preset | Easy preset → Confirm | Log `Easy preset applied and verified (n values changed)` | NOT RUN |
| J1 | Options | Unlocks | Courses, contacts, importers ON | Log verified; check in game apps (may need reload) | NOT RUN |
| J2 | Player | Invincibility OFF | Toggle on then off | Log `Invincibility OFF verified` | NOT RUN |
| K1 | All | Layout | Every tab | Everything inside the card, readable | NOT RUN |

## Summary
total: 33 · pass: 0 · fail: 0 · not run: 33
