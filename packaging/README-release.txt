AMBITION CONTROL {VERSION}  -  MelonLoader mod for Big Ambitions
=====================================================================

A single-player utility menu that lives as an app on the in-game smartphone.

WHAT'S IN THIS ZIP
  AmbitionControl.dll   the mod (ready-built; copy into the game's Mods folder)
  README.txt            this file
  CHANGELOG.md          version history and test status
  SHA256SUMS.txt        checksum of the DLL

REQUIREMENTS (exactly what this build was tested on)
  - Big Ambitions Early Access, build 3233 (EA 0.9)
  - MelonLoader 0.7.3 (IL2CPP, net6) already installed and run once
  Other game builds: money changes and option switches are blocked automatically.

INSTALL
  1. Close the game.
  2. BACK UP YOUR SAVES: copy the folder
       %USERPROFILE%\AppData\LocalLow\Hovgaard Games\Big Ambitions\SaveGames
     somewhere safe.
  3. Copy AmbitionControl.dll into the game's "Mods" folder
     (the folder next to "Big Ambitions.exe"; MelonLoader created it).
  4. Start the game. The MelonLoader console should show:
       [Ambition_Control] v... loaded. Open the "Ambition Control" app on the in-game phone ...
       [Ambition_Control] Game build 3233 matches the verified target.
  5. Load a save, open the smartphone and tap the "Ambition Control" app.

USING IT
  Money tab
    - Build an amount with +1K ... +10M (Clear resets it).
    - Stage Add / Stage Remove / Stage Set -> check the preview -> Confirm change.
    - The mod reads the balance back and only reports "Verified" if it really changed.
    - Undo reverses the last change while the same save is loaded and the balance hasn't moved since.
    - Limits: $10,000,000 per change; resulting balance $0 - $100,000,000.
    - Changes are recorded by the game as "Cheat" transactions.
  Game options tab
    - Energy drain, Happiness drain, Aging, Vehicle damage, Vehicle fuel use, Wholesale/import limits.
    - These are the game's own custom-game options. ON = normal game behaviour, OFF = turned off.
    - A change is stored in your save when the game saves. Switch back ON to restore normal behaviour.
  F10 overlay
    - Older fallback menu (press F10). Clicks on it can reach the game world behind it; prefer the phone app.

SETTINGS  (game folder\UserData\MelonPreferences.cfg, section [AmbitionControl])
  PhoneApp = true              add the phone app
  PlayerTogglesEnabled = true  show the Game options switches
  MoneyWritesEnabled = true    allow money changes (false = read-only)
  AllowOtherBuilds = false     allow changes on builds other than 3233 (untested, at your own risk)
  ToggleKey = "F10"            key for the fallback overlay
  BlockClickThrough = true     overlay input filter (overlay only)

UPDATE
  Close the game and replace AmbitionControl.dll in the Mods folder. Settings are kept.

UNINSTALL
  Delete Mods\AmbitionControl.dll. Optionally remove the [AmbitionControl] section from
  UserData\MelonPreferences.cfg. Money already added, and game options already switched, stay in
  any save that was saved while the mod was used (switch options back ON first if you want them restored).

LOGS / TROUBLESHOOTING
  game folder\MelonLoader\Latest.log - every Ambition Control action is logged with a [Ambition_Control] tag.
  If the phone app hits repeated errors it turns itself off; F10 still works.

TEST STATUS OF THIS BUILD (honest summary - see CHANGELOG.md)
  Verified in-game on build 3233: loading, phone app, Add, Remove, Undo, save -> reload keeps changes,
  status readout, all six option switches set OFF (energy/happiness confirmed to take effect live).
  Owner-reported (no log evidence): Set, over-removal refusal, Undo expiry.
  Not exercised: switching options back ON; in-game effect of aging/vehicle/wholesale options.

Single-player use only. Not affiliated with Hovgaard Games. No license file is included in the source
repository: https://github.com/anomisss/AmbitionControl (branch compat/ea-0.9.3233-melonloader).
