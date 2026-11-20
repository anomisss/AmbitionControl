---
status: partial
phase: 11-quests-teleport (Milestone F part 2)
build: v0.7.0-alpha.6 (DLL SHA-256 2f685f82f2f1223e9d858aca6f33d8b92090d79e13c6abea29843fd37e04ff27)
started: 2026-11-20
---

Run on a COPY of a save — **completing objectives/quests cannot be undone.** Evidence = MelonLoader/Latest.log + owner observation.

| # | Test | Steps | Expected | Result |
|---|---|---|---|---|
| 1 | Tabs | Visit all eight tabs | Labels fit (incl. Economy, Options); nothing cut off | PASS — log 03:06:24–03:06:39: all eight tabs clicked (Economy, Money, Player, World, Staff, Rivals, Quest, Options); no layout issue reported. |
| 2 | Quest display | Open Quest tab | Shows quest + objective ids, or "tutorial is off" / "all finished" | PASS — quest ids read from the game (`QuestEmployeeUpgrades`, then `QuestDeliveries`) as logged at completion. |
| 3 | Teleport guard | Inside a building or vehicle | Teleport buttons greyed; block text says why | NOT RUN |
| 4 | Teleport to quest target | Outdoors on foot, press Teleport to quest target | You move to the objective; log `Teleport verified: quest target, moved N m` | NOT RUN |
| 5 | Teleport to GPS | Set a GPS destination in the game, go outside, press Teleport to GPS destination | You move there; log `Teleport verified: GPS destination …` | NOT RUN |
| 6 | Objective preview | Press Complete objective once | Status asks to confirm; button reads Confirm objective; nothing changes | NOT RUN |
| 7 | Objective apply | Press Confirm objective | Log `Objective completed (verified): A -> B`; game shows next objective | NOT RUN |
| 8 | Quest apply (optional) | Complete whole quest → Confirm quest | Log `Quest completed (verified): …`; game moves to next quest | PASS — log 03:06:39.5 first press (preview), 03:06:40.5 confirm, `Quest completed (verified): QuestEmployeeUpgrades -> QuestDeliveries`. |

## Summary
total: 8 · pass: 3 · fail: 0 · not run: 5 (teleport guard, both teleports, objective preview/apply)

Evidence: Latest.log session 2026-11-20 03:06 (save day 11). No errors logged.
