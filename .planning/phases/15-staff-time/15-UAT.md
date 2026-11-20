---
status: partial
phase: 15-staff-time (Milestone H)
build: v0.7.0-alpha.11 (DLL SHA-256 d3e7c69aae773bae633ac58a5a15fdc2da8c04b15dd8c6596a5d11da44841b25)
started: 2026-11-20
---

Run on a COPY of a save. Evidence = MelonLoader/Latest.log + owner observation.

| # | Test | Steps | Expected | Result |
|---|---|---|---|---|
| 1 | Skip preview | World → Skip 1h once | Yellow status asks to confirm; button reads Confirm 1h; time unchanged | NOT RUN |
| 2 | Skip apply | Press Confirm 1h | Game fast-forwards like sleeping; log `Time skip started (verified running)` then `Time skip finished: … (reached)` | NOT RUN |
| 3 | Skip guard | In a vehicle | Skip buttons greyed | NOT RUN |
| 4 | Wages preview | Staff → Wages to market rate once | Status shows count + payroll before → after; nothing changes | PASS — log 15:31:42.7 first press (preview), no change logged until confirm |
| 5 | Wages apply | Confirm market wages | Log `Market wages: N/N verified`; MyEmployees shows new wages | PASS — 15:31:48.4 `Market wages: 2/2 verified; payroll 57.14 -> 54.84 per hour` |
| 6 | Candidate | Choose skill + level, Generate | Log `Candidate generated (verified): …`; candidate visible in MyEmployees | PASS — 15:32:14.7 `Candidate generated (verified): Wendy Henson (Skill: 100%), CustomerService 100/100; candidates 0 -> 1` (skill/level cycling clicked through all options) |
| 7 | Layout | Staff and World tabs | Everything fits inside the card | PASS (Staff tab used throughout; no layout issue reported) |

## Summary
total: 7 · pass: 4 · fail: 0 · not run: 3 (time skip)

Evidence: Latest.log 2026-11-20 15:31–15:32 (save day 11), no errors. Also re-verified: Max satisfaction 5/5 (2 were below max).
