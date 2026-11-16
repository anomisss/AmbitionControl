# Changelog

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
