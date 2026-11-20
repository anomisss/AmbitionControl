---
status: testing
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
| 4 | Wages preview | Staff → Wages to market rate once | Status shows count + payroll before → after; nothing changes | NOT RUN |
| 5 | Wages apply | Confirm market wages | Log `Market wages: N/N verified`; MyEmployees shows new wages | NOT RUN |
| 6 | Candidate | Choose skill + level, Generate | Log `Candidate generated (verified): …`; candidate visible in MyEmployees | NOT RUN |
| 7 | Layout | Staff and World tabs | Everything fits inside the card | NOT RUN |

## Summary
total: 7 · pass: 0 · fail: 0 · not run: 7
