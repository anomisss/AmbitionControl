---
status: partial
phase: 18-bulk (Milestone K + everything not yet run)
build: v0.7.0-alpha.15 run 1 (2026-11-21 01:44); now v0.7.0-alpha.16 (DLL SHA-256 9ed5c01521bcab9cfae7c8eb040b1781061f690ecf7906e93406363fbd97341f)
started: 2026-11-21
---

Bulk test on a COPY of a save. Evidence = MelonLoader/Latest.log + owner observation. Items 17-x replace 17-UAT.

| # | Area | Test | Steps | Expected | Result |
|---|---|---|---|---|---|
| A1 | Money | Keypad | Money: Keypad, type 2 5 000, Stage Add, Confirm | Amount $25,000; balance +$25,000 verified; game ignores the typed keys | PARTIAL — keypad typed 2,5,000 → staged Add $25,000 correctly (01:47:45); the add itself FAILED because the alpha.15 revenue hook crashed ChangeMoney (see D2). Retest on alpha.16 |
| A2 | Money | Keypad limits | Keypad: Del removes a digit; type past $100M | Del works; status "Can't type more" | NOT RUN |
| B1 | Staff | Select employee | Staff: tap a row | Row bold with ">"; hint changes | INCONCLUSIVE — 01:45:16 `Phone click: Row 1`, no wage clicks followed; alpha.16 logs `Staff: selected …`. Retest |
| B2 | Staff | Wage steps | +$1, +10%, -10%, -$1, Market | Log `Wage …: name a -> b (verified)` each; row updates | NOT RUN |
| B3 | Staff | All to market (renamed) | All to market → Confirm all | As 15-UAT #5 | NOT RUN |
| C1 | Business | Hold on | Business: Hold satisfaction 100% | Log `Satisfaction hold ON: n/n verified`; rows show 100% | NOT RUN |
| C2 | Business | Hold survives | Skip 4h (World) with the hold on | Still 100%; log `hook re-applied` and/or `fallback re-applied` | NOT RUN |
| C3 | Business | Hold off | Tap again; skip 1h | Log `Satisfaction hold OFF`; values return to the game's own | NOT RUN |
| D1 | Economy | Revenue x2 | Economy: tap Sales revenue (mod) until x2 | Log `2/2 money hooks installed`, `set to 2 (verified)` | PASS — 01:45:01-06 cycled 1.5/2/3/5/10/1 x2, every step `set to … (verified)`; preference saved (now 10x) |
| D2 | Economy | Revenue applied | Let a business sell (or skip 1h) | Log `Revenue hook: first sale seen`, `Revenue x2: sale $a -> $b`, `Revenue readback … (verified)`; button shows +$extra | FAIL (alpha.15) — 01:47:48 `[Il2CppInterop] During invoking native->managed trampoline … NullReferenceException … ChangeMoney(Single, TransactionType, IntPtr, IntPtr, IntPtr)`; money writes then disabled by readback guard (expected $15,370,182, read $15,345,182). Fixed in alpha.16 by removing the hooks (history scan). Retest |
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
| K1 | All | Layout | Every tab | Everything inside the card, readable | PARTIAL — Economy, World, Staff, Business, Rivals, Quest, Options, Player, Money opened with no errors (01:44-01:47); visual fit not reported |

## Summary
total: 33 · pass: 1 · partial: 2 · fail: 1 (fixed in alpha.16, retest) · inconclusive: 1 · not run: 28

### Findings from run 1 (Latest.log 2026-11-21 01:44-01:48, save day 11)
1. **Revenue hook crash (critical, fixed):** Harmony on `GameManager.ChangeMoney`/`ChangeMoneySafe` broke Il2CppInterop's native->managed trampoline (the `Nullable<int>` value-type parameter is passed as a null pointer), so every money change failed while the hook was installed. Lesson: never Harmony-patch IL2CPP methods with small value-type (struct/Nullable) parameters. alpha.16 removes the hooks and credits the extra from the game's transaction history instead.
2. **Time skip goal (fixed, needs recheck):** log `Time skip started: Day 11 04:02 -> Day 11 04:02` — `Timestamp.AddHours` mutates the object it is called on (`TimeHelper.Now()` result, possibly the live clock), so the earlier "Skip 1h PASS" (16-UAT R1, finished in 0.6 s) may have been a direct jump rather than a fast-forward. alpha.16 builds a new `Timestamp(totalMinutes)`, re-reads the clock to prove it was untouched, and logs real seconds taken. Re-run R1/C2.
3. Readback guard worked as designed: after the failed add, money writes were disabled for the session instead of retrying.

| # | Area | Test | Steps | Expected | Result |
|---|---|---|---|---|---|
| R1 | World | Skip recheck (alpha.16) | Skip 1h → Confirm | Log `Time skip started … (60 game minutes)`, then `finished … (reached) after N s real time` with N clearly > 1 s; clock advanced 1h | NOT RUN |
| R2 | Economy | Revenue no-hook (alpha.16) | Multiplier is 10x from your preference; let businesses sell (skip 1h) | Log `baseline set`, `Revenue x10: sale $a -> extra $b`, `Revenue extra credited (verified)`; no Il2CppInterop errors; Money tab add works | NOT RUN |
