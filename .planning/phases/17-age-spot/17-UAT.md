---
status: testing
phase: 17-age-spot (Milestone J)
build: v0.7.0-alpha.14 (DLL SHA-256 886cb7248cef9ec6f83e98fabdc63ace6eb49fc5267e59476e0aa2614c727a29)
started: 2026-11-20
---

Run on a COPY of a save. Evidence = MelonLoader/Latest.log + owner observation.

| # | Test | Steps | Expected | Result |
|---|---|---|---|---|
| 1 | Age display | Player tab | `Age N (D days, P days per year)`; N matches Persona | NOT RUN |
| 2 | Age preview | Older 1 year once | Yellow status `Change age N -> N+1?`; nothing changes | NOT RUN |
| 3 | Age apply | Confirm older | Log `Age change: command unit measured as … (verified)`; Persona shows N+1 | NOT RUN |
| 4 | Age back | Younger 1 year → Confirm younger | Back to N (verified) | NOT RUN |
| 5 | Mark spot | Outdoors on foot: Mark this spot | Log `Waypoint 'AmbitionControl' set at …` | NOT RUN |
| 6 | Return | Walk/drive away, get out, Return to spot | Log `Teleport verified: marked spot, moved N m` | NOT RUN |
| 7 | Layout | Player and Quest tabs | Everything fits in the card; texts readable (addresses/times now plain text) | NOT RUN |

## Summary
total: 7 · pass: 0 · fail: 0 · not run: 7
