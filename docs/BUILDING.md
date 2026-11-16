# Building Ambition Control v0.3.0

Official SDK: https://github.com/hovgaardgames/bigambitions

1. Install Unity **2022.3.62f2** and Big Ambitions through Steam.
2. Open a clone of the official SDK Unity project; use its welcome screen to **import your own game DLLs**. Reimport game DLLs after every game update.
3. Copy the `Assets/Mods/AmbitionControl` folder from this repository into the SDK project's `Assets/Mods/` folder.
4. Allow Unity to compile, then use **Big Ambitions > Mod Builder > Build & Install**.
5. Confirm the mod appears in the game's ModsLocal directory, then enable it.
6. **Duplicate your save first.** Load the duplicate, press F10, and check the displayed live money balance. Avoid testing while another mod is editing the same balance.
7. Stage **Add $10,000**, inspect the preview, click Confirm, then inspect both the actual in-game balance and the game's transaction history. Use Undo immediately on the test balance if desired.
8. Repeat for Remove and Set to validate the accounting integration. Undo is only available while the same loaded save and same balance remain active.
9. Check the in-game Player and Time tabs; these are *read-only* in v0.3.0.
10. If a step fails, preserve Unity Console errors or the game's Player.log; use Diagnostics > Write API candidate report for metadata discovery.

## Cautions

- Game method signatures are based on public source using `GameManager.ChangeMoneySafe` and `SaveGameManager.Current.Money`. **We have not compiled or run this mod inside the game.** If compiler errors occur, stop rather than patching with guessed APIs.
- The game represents cash as a float; the mod limits each transaction to $10M and supported total balances to $100M. Precision is not guaranteed for fractional dollars at high balances.
- Transactions use the localized custom key `ambitioncontrol:transaction_adjustment`. If this breaks any finance screen, report it and test on a new copy.
- GUI rendering uses Unity IMGUI; it may not prevent all gameplay inputs behind the window.
- A successful change is stored by the game when you save normally. No separate save-file writes are made by this mod.
