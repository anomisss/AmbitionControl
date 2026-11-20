# Changelog

## [0.7.0-alpha.1] — 2026-11-19 — Milestone B: Player utilities (trainer expansion)
- New **Player** tab in the phone app (tabs are now Money | Player | Game options; tab system generalized for more categories).
- **Refill energy / hunger / happiness / all** via the game's own setters (`EnergyHelper.SetCurrentEnergy`, `EnergyHelper.SetCurrentHunger`, `HappinessHelper.Happiness`); each refill is read back from `GameInstance` and logged `Refill verified: <need> before -> after`.
- **Invincibility** toggle via the game's `EnergyHelper.Command_ToggleInvincibility()`, verified by reading `EnergyHelper._invincibility`. Runtime only.
- **Hold hunger full** (experimental): build 3233 has no hunger option, so this tops hunger up (max once per second) while a save is loaded; session-only, turns itself off when the save changes.
- Same safety gate as money/options (build 3233, save loaded, not saving). New preference `PlayerToolsEnabled` (default true).
- New `docs/FEATURE_GAP_MATRIX.md`: full comparison with the reference trainer's advertised features and build-3233 API evidence (e.g. export multiplier does not exist in build 3233).
- **Status:** **Runtime verified (2026-11-19, build 3233):** refills logged `Energy 73.1 -> 100`, `Hunger 59.6 -> 100`, `Happiness 35 -> 100` and the HUD showed full (owner); Invincibility ON verified; Hold hunger kept hunger full (owner); tab switching works. Not run: Invincibility OFF, Hold-hunger reset on save change. Details: `.planning/phases/06-player-utilities/06-UAT.md`.

> **Latest release:** [v0.6.0-alpha.2 (pre-release)](https://github.com/anomisss/AmbitionControl/releases/tag/v0.6.0-alpha.2) — ZIP SHA-256 `67bb8831b6d136d54faeaa7d40e0abe872a4efefb688a9c144010642d0a075a3`.

## [0.6.0-alpha.2] — 2026-11-19 — All game option switches + tabs (GSD Phase 4, plan 04-02)
- Phone app now has **Money** and **Game options** tabs.
- Game options tab: six switches for the game's own custom-game options — **Energy drain, Happiness drain, Aging, Vehicle damage, Vehicle fuel use, Wholesale/import limits** (`GameVariables.disableEnergy/disableHappiness/disableAging/disableVehicleDamage/disableVehicleFuel/disableWholesaleAndImportLimits`, all verified in build-3233 interop and offered by the game's custom-game screen). Each change is read back; stored in the save when the game saves.
- `NeedToggles` generalized to `GameOptions` (one code path for all six).
- **Status:** **Runtime verified (2026-11-19, build 3233):** Game options tab works; Aging, Vehicle damage, Vehicle fuel use and Wholesale/import limits each set OFF and read back ("verified" in log); owner confirms both tabs display correctly. Not exercised: switching back ON, and the in-game effect of the four new options.

## [0.6.0-alpha.1] — 2026-11-19 — Energy/happiness drain switches (GSD Phase 4, plan 04-01)
- Phone app: **Energy drain ON/OFF** and **Happiness drain ON/OFF** buttons. They set the game's own custom-game options `GameInstance.gameVariables.disableEnergy` / `disableHappiness` (verified in build-3233 interop; the game exposes them in `CustomGameOptionsHandler`), read the value back, and report "(verified)".
- These options are part of the save: once the game saves, the setting stays with that save. Turn a switch back ON to restore normal drain.
- Blocked on non-3233 builds (unless `AllowOtherBuilds`), with no save loaded, or while saving. New preference `PlayerTogglesEnabled` (default true).
- Hunger has no game option in build 3233, so it is not included.
- **Status:** **Runtime verified (2026-11-19, build 3233):** both switches set OFF and read back ("verified" in log); owner confirms energy and happiness stop dropping immediately (no reload) and the panel layout fits. Switching back ON not yet exercised.

## [0.5.0-alpha.3] — 2026-11-19 — Phase 3 UAT round 1 + staging feedback
- UAT round 1 on build 3233 (log evidence in `.planning/phases/03-money-feature-verification/03-UAT.md`): **Remove** verified (15,145,598 → 15,144,598), **Undo** verified twice, **save → quit → reload** kept the mod's changes (reload $15,144,279 after $319 normal spending), state cleared and app re-injected after reload, Status matched the HUD, switching phone apps hides ours. Not yet run: over-removal refusal, Set, Undo expiry.
- Fix (UX): when staging is refused (e.g. amount above the $10M-per-change limit) the phone app now shows "Can't stage: <reason>" on the preview line; previously only the status line changed and repeated Stage clicks looked dead. The message clears when the amount changes.
- **Status:** Build verified; tests 12/12. Owner reports Set, over-removal refusal (with the new message) and Undo expiry work; **no log evidence** of an alpha.3 game session (last log 20:57, alpha.2).

## [0.5.0-alpha.2] — 2026-11-19 — Phase 3 test evidence logging
- Log lines for UAT evidence: `Session: save loaded, balance $X, day N`, `Session ended ... cleared`, `Scene loaded: <scene> (staged action and undo cleared)`, `Phone app hidden: another phone app was opened.`
- Added Phase 3 UAT checklist (`.planning/phases/03-money-feature-verification/03-UAT.md`).
- **Status:** Build verified; tests 12/12. Logging only; behaviour unchanged from 0.5.0-alpha.1.

## [0.5.0-alpha.1] — 2026-11-19 — In-game phone app (GSD Phase 2, plan 02-02)
- New **Ambition Control app on the in-game smartphone**: an app button cloned from the phone's own app bar (`FullMenu._appButtons`) with a fresh click event, and a panel built from the game's uGUI (`Image`, `Button`, `TextMeshProUGUI` with the phone's font) inside `FullMenu.appsContainer`.
- Panel: balance, amount picker, Stage Add/Remove/Set, preview, Confirm/Cancel, Undo, status, limits/blocked reason, day/time and needs readout. Uses the same readback-verified `MoneyService` as the overlay.
- Opening another phone app hides the panel (it watches the app screens; no extra hooks). Injection retries every 2 s while a save is loaded; disabled after 5 errors (overlay unaffected). New preference `PhoneApp` (default true).
- The F10 overlay remains as a fallback.
- **Status:** **Runtime verified (2026-11-19, build 3233):** app injected into the phone, opened, buttons respond, Add $1,110,000 verified by readback (1,595,598.38 -> 2,705,598.50); owner reports no click-through. Undo/Remove/Set and app switching not yet exercised.

## [0.4.0-alpha.3] — 2026-11-19 — Click-through: action-level blocking + diagnostics
- In-game (alpha.2): all 10 `InputActionHelper` patches applied, but clicks over the menu still reached the world. Hypothesis: helper calls inlined by IL2CPP and/or actions read directly.
- New layer: while the cursor is over the menu, the game's own `InputAction`s for Click/RightClick/Zoom/CursorScroll (from `InputActionHelper.PlayerInputActionMap`) are disabled, and only those the mod disabled are re-enabled on leave, scene change or shutdown.
- `Input diag:` log line (every 3 s while the menu is open) reports helper calls/blocked, actions held disabled and re-enables by the game, to show which path the game uses.
- Reference: the Alunas trainer (owner-supplied, not redistributed) builds a uGUI panel inside the in-game smartphone (`Il2CppUI.Smartphone.FullMenu`); candidate architecture for the menu.
- **Status:** Build verified; tests 12/12. **Not runtime verified.**

## [0.4.0-alpha.2] — 2026-11-19 — Menu input isolation (GSD Phase 2)
- Click-through blocking: Harmony prefixes on the game's `Il2CppBigAmbitions.InputSystem.InputActionHelper` mouse reads (left/right click helpers, scroll, and `Pressed/Pressing/Released/Value` for `Click`, `RightClick`, `Zoom`, `CursorScroll`) report no input while the cursor is over the open menu or a title drag is active. Keyboard actions untouched.
- Each patch applied independently; the log reports `Click-through blocking: N patches applied`. New preference `BlockClickThrough` (default true).
- Added GSD planning docs under `.planning/` (project, requirements, roadmap, state, Phase 2 plan).
- **Status:** Build verified; unit tests 12/12. **Not runtime verified.**

## [0.4.0-alpha.1] — 2026-11-19 — First MelonLoader build (compat/ea-0.9.3233-melonloader)
- Confirmed target from the user's install: Big Ambitions build 3233, Unity 2022.3.62f2 **IL2CPP**, **MelonLoader v0.7.3** (net6).
- New standalone project `MelonLoader/AmbitionControl.Melon` (`MelonMod` entry, net6.0); SDK sources under `Assets/` untouched and not compiled into it.
- Replaced the reflection prototype `GameBridge` with typed calls to the generated interop. Corrected the money API to the real 3233 signature: `GameManager.ChangeMoneySafe(float, TransactionType, DataHolder, Nullable<int>, Address, bool, bool)` using the game's built-in `TransactionType.Cheat`.
- Money: stage → preview → confirm → balance readback verification; guarded one-step undo; writes latched off on any mismatch/exception; blocked while saving, with no save, or on builds ≠ 3233.
- F10 menu (rebindable via MelonPreferences) with Money / Status / Diagnostics tabs, fixed-rect IMGUI only (avoids stripped GUILayout/GUIStyle members); amount entry by buttons so no keystrokes reach game hotkeys.
- Added xUnit tests for the pure money logic.
- **Status:** Build verified; unit tests 12/12 passed. **Partly runtime verified (2026-11-19, build 3233, ML 0.7.3):** mod loads, build guard passes, F10 menu opens, drags and accepts clicks, Add $1,000,000 applied through `ChangeMoneySafe` and verified by readback (1,595,598.38 -> 2,595,598.50). Not yet exercised: Remove, Set, Undo, save/reload persistence, save switching.
- In-game fixes during testing: `GUI.Toolbar` fails at runtime ("Method unstripping failed") -> plain buttons; IMGUI `GUI.Button` click detection did not fire over the game world -> clicks resolved from `Mouse.current` (Input System); title-bar dragging added; state and click logging added.

## [0.3.0] — 2026-11-16 — Native money controls (source-only)
- Implemented real game-API money integration via `SaveGameManager.Current.Money` and `GameManager.ChangeMoneySafe`.
- Added Add, Remove and Set workflows with input bounds, preview and explicit confirmation.
- Added single-step, save-identity-and-balance-guarded undo, with limitations described in docs.
- Added localized transaction key and updated SDK manifest.
- Player and World Time tabs now display live, read-only game values.
- **Status:** source committed, signatures cross-checked against public mod source; compilation and actual in-game behavior unverified.

## [0.2.0] — 2026-11-16 — SDK integration source
- Added actual `IModBigAmbitions` entry class, `[ModEntryOnInitializationLoad]`, and `[assembly: RegisterModClass]` based on official Example-Options.
- Added SDK-style manifest and assembly definition with canonical references.
- Added F10 IMGUI trainer, Money/Player/Time/Diagnostics/Settings panels, basic money validation, and preferences.
- Updated API discovery to inspect BigAmbitions game assemblies as well as Assembly-CSharp.
- Gameplay cheats disabled; adapter integration and runtime testing remained outstanding.
- **Status:** source generated and SDK structure compared with official examples; not compiled/in-game tested.

## [0.1.0] — 2026-11-16 — Historical starter
- Original local source ZIP prepared in ChatGPT.
- Draggable F10 IMGUI UI prototype, money command validation/staging, placeholder game bridge, settings and metadata diagnostics.
- **Status:** source-only; no SDK manifest or verified runtime entry point; not compiled or tested.
- Original local source ZIP was not imported into GitHub as an attachment.
