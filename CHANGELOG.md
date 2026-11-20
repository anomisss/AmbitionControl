# Changelog

## [0.4.0-alpha.1] — 2026-11-19 — First MelonLoader build (compat/ea-0.9.3233-melonloader)
- Confirmed target from the user's install: Big Ambitions build 3233, Unity 2022.3.62f2 **IL2CPP**, **MelonLoader v0.7.3** (net6).
- New standalone project `MelonLoader/AmbitionControl.Melon` (`MelonMod` entry, net6.0); SDK sources under `Assets/` untouched and not compiled into it.
- Replaced the reflection prototype `GameBridge` with typed calls to the generated interop. Corrected the money API to the real 3233 signature: `GameManager.ChangeMoneySafe(float, TransactionType, DataHolder, Nullable<int>, Address, bool, bool)` using the game's built-in `TransactionType.Cheat`.
- Money: stage → preview → confirm → balance readback verification; guarded one-step undo; writes latched off on any mismatch/exception; blocked while saving, with no save, or on builds ≠ 3233.
- F10 menu (rebindable via MelonPreferences) with Money / Status / Diagnostics tabs, fixed-rect IMGUI only (avoids stripped GUILayout/GUIStyle members); amount entry by buttons so no keystrokes reach game hotkeys.
- Added xUnit tests for the pure money logic.
- **Status:** Build verified (compiled against the user's generated Il2Cpp assemblies + MelonLoader 0.7.3); unit tests 12/12 passed. **Not runtime verified** — no in-game test yet. See `docs/MELONLOADER.md`.

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
