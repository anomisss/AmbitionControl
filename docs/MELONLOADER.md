# Ambition Control — MelonLoader build (v0.4.0-alpha.1)

**Status: partly runtime verified** on build 3233 / MelonLoader 0.7.3: loading, F10 menu, dragging, clicks and a verified $1M Add work in-game. Remove, Set, Undo and save/reload persistence not yet tested. Back up your save before using money features.

## Verified target

| Item | Value | Evidence |
| --- | --- | --- |
| Game | Big Ambitions EA 0.9, build 3233 | user report; runtime guard reads `GameVersion.GetCurrent().buildNumber` |
| Backend | Unity 2022.3.62f2, IL2CPP x64 | `MelonLoader/Latest.log` ("Game Type: Il2cpp"), `GameAssembly.dll` present |
| Loader | MelonLoader v0.7.3 Open-Beta, net6 runtime | `MelonLoader/Latest.log` |
| Game APIs | `SaveGameManager.Current`, `GameInstance.Money/Day/Hour/Minute/Energy/Hunger/Happiness`, `GameManager.ChangeMoneySafe(float, TransactionType, DataHolder, Nullable<int>, Address, bool, bool)`, `Transaction.TransactionType.Cheat` | read from `MelonLoader/Il2CppAssemblies/Il2CppBigAmbitions.dll` and bound at compile time |

## Install (Windows)

1. Close the game.
2. **Back up your save folder** (copy it somewhere safe). Big Ambitions saves are under `%USERPROFILE%\AppData\LocalLow\Hovgaard Games\Big Ambitions\SaveGames`.
3. Copy `AmbitionControl.dll` into `<game folder>\Mods\` (the folder next to `Big Ambitions.exe` that already contains MelonLoader's `Mods` directory).
4. Start the game. In `<game folder>\MelonLoader\Latest.log` you should see:
   - `[Ambition_Control] v0.4.0-alpha.1 loaded. Press F10 in-game to open the menu.`
   - after the main menu appears: `Game build 3233 matches the verified target.`
5. Load a **copy** of a save and press **F10**.

**Upgrade:** replace the DLL. **Uninstall:** delete `Mods\AmbitionControl.dll` (optionally remove the `[AmbitionControl]` section from `UserData\MelonPreferences.cfg`). The mod writes nothing to save files directly; money changes go through the game's own transaction method and are saved only when the game saves.

## Using it

- **Phone app (v0.5+):** open the in-game smartphone and tap the **Ambition Control** app. It has the same money controls and status readout as the overlay, built from the game's own UI so clicks stay in the phone. Turn off with `PhoneApp = false`.
- **Money tab (F10 overlay):** build an amount with the +1K…+10M buttons (no typing, so game hotkeys are not triggered), then *Stage Add / Remove / Set* → check the preview → *Confirm change*. The mod reads the balance back and reports **Verified** only if it actually changed by the expected amount. *Undo* reverses the last change only while the same save is loaded and the balance has not changed since.
- **Status tab:** read-only day/time, money, net worth, energy, hunger, happiness.
- **Diagnostics tab:** detected build, loader state, preference values, session latch reason.

Safety rules built in: writes blocked if the build ≠ 3233 (unless `AllowOtherBuilds = true`), while the game is saving, with no save loaded, or after any failed readback/exception (latched off until restart). Limits: $10M per change, balance 0–$100M.

Preferences (`UserData\MelonPreferences.cfg`, section `[AmbitionControl]`): `ToggleKey` (Input System key name, default `F10`), `MoneyWritesEnabled`, `AllowOtherBuilds`, `BlockClickThrough` (default `true`; overlay only), `PhoneApp` (default `true`; adds the phone app).

### Known limitations
- Click-through blocking (v0.4.0-alpha.2, not yet tested in-game) filters the game's mouse reads through `InputActionHelper` while the cursor is over the menu. Game code that reads the mouse some other way, or where IL2CPP inlined the helper, can still react. Turn it off with `BlockClickThrough = false` if game controls misbehave.
- Player stats and time are read-only.

## In-game test checklist (record Passed / Failed / Not run)

1. Log shows the load line and build-3233 line, with no Ambition Control errors.
2. F10 opens/closes the menu at the main menu (shows "No save loaded") and in a loaded save.
3. Status tab values match the in-game HUD.
4. Add $1,000 → Confirm → status says *Verified*, HUD balance rises by $1,000, a transaction appears in the game's finance history.
5. Undo → *Undo verified*, balance back to original.
6. Remove more than the balance → rejected by preview, nothing applied.
7. Return to main menu and load another save → staged/undo state cleared.
8. Save, quit, reload → balance persisted as expected.

## Build from source

Requires the .NET 8 SDK (builds a `net6.0` library) and the game with MelonLoader run at least once (so `MelonLoader\Il2CppAssemblies` exists). Game DLLs are referenced from your install, never committed.

```bash
dotnet build MelonLoader/AmbitionControl.Melon -c Release -p:GamePath="C:\path\to\game"
```

Instead of `-p:GamePath`, you can set `BIG_AMBITIONS_DIR` or create a git-ignored `MelonLoader/AmbitionControl.Melon/GamePath.props.user`:

```xml
<Project><PropertyGroup><GamePath>C:\path\to\game</GamePath></PropertyGroup></Project>
```

Add `-p:DeployToGame=true` to copy the DLL into `<GamePath>\Mods`. Output: `MelonLoader/AmbitionControl.Melon/bin/Release/AmbitionControl.dll`.

Unit tests (pure money logic, no game needed): `dotnet test MelonLoader/AmbitionControl.Tests`.
