---
status: testing
phase: 14-game-style
build: v0.7.0-alpha.10 (DLL SHA-256 ebf9200ba4d6513975d9dc2af28780246d98e58208fa0df8fbb88065bf88b9c3)
started: 2026-11-20
---

Visual change. Evidence = owner observation + MelonLoader/Latest.log (`Phone app style: N/7 game sprites`, no errors).

| # | Test | Steps | Expected | Result |
|---|---|---|---|---|
| 1 | Matches the game | Open a game app (e.g. Persona), then Ambition Control | Same background, white rounded card, similar text size; looks like it belongs | NOT RUN |
| 2 | Tabs | Click through all eight tabs | White active tab with underline moving; grey inactive; nothing cut off | NOT RUN |
| 3 | Buttons | Look at grey / green / red buttons; a disabled one | Game button art; disabled ones faded; captions readable | NOT RUN |
| 4 | Status bar | Refill energy (green), Save game once (yellow), over-remove money (red) | Game gradient bars | NOT RUN |
| 5 | Clicks | Click empty space around the card | Nothing in the world reacts | NOT RUN |
| 6 | Log | After closing | `Phone app style: 7/7 game sprites …, bold font found.`; no errors | NOT RUN |

## Summary
total: 6 · pass: 0 · fail: 0 · not run: 6
