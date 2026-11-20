**Ambition Control v0.6.0-alpha.2** — MelonLoader mod for **Big Ambitions EA build 3233** (IL2CPP, MelonLoader 0.7.3). Single-player only. Alpha / pre-release.

### What it does
An **Ambition Control app on the in-game smartphone**:
- **Money tab** — Add / Remove / Set with a preview and confirmation, applied through the game's own transaction system (recorded as "Cheat") and verified by reading the balance back. One-step Undo. Limits: $10M per change, balance $0–$100M.
- **Game options tab** — switch the game's own custom-game options: energy drain, happiness drain, aging, vehicle damage, vehicle fuel use, wholesale/import limits. Stored in your save when the game saves; switch back ON to restore.
- Status readout (day/time, money, net worth, energy, hunger, happiness). F10 overlay kept as a fallback.

### Install
1. Back up `%USERPROFILE%\AppData\LocalLow\Hovgaard Games\Big Ambitions\SaveGames`.
2. Copy `AmbitionControl.dll` from the ZIP into the game's `Mods` folder (MelonLoader must already be installed).
3. Start the game, load a save, open the phone → **Ambition Control**.

Full instructions, settings and uninstall steps are in `README.txt` inside the ZIP.

### Tested (build 3233, MelonLoader 0.7.3)
- Verified in-game with log evidence: loading + build guard, phone app, Add, Remove, Undo, save → reload persistence, state reset on reload, status readout, all six option switches set OFF (energy/happiness confirmed to apply live).
- Owner-reported without log evidence: Set, over-removal refusal, Undo expiry.
- Not yet exercised: switching options back ON; in-game effect of the aging/vehicle/wholesale options.

Other game builds: changes are blocked automatically.

### Checksums (SHA-256)
- `AmbitionControl.dll`: `f2ef90364160680fc758faa49e75ec2fbed64a4728fd877e3d8265f1c11bbd0a`
- `AmbitionControl-v0.6.0-alpha.2-ea3233-melonloader.zip`: `67bb8831b6d136d54faeaa7d40e0abe872a4efefb688a9c144010642d0a075a3`

Not affiliated with Hovgaard Games.
