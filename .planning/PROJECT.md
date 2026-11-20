# Ambition Control

## What This Is

A single-player cheat/utility menu for **Big Ambitions**, loaded by **MelonLoader** as a DLL. It opens with F10 in-game and gives money controls plus read-only player/time status, with an emphasis on stability, save safety and honest verification. Built for the owner's own install (EA 0.9 build 3233).

## Core Value

Money changes made from the menu apply through the game's own transaction path, are verified by reading the balance back, and never corrupt a save.

## Requirements

### Validated

- ✓ Loads as a MelonLoader 0.7.3 mod on build 3233 (IL2CPP) without errors — v0.4.0-alpha.1 (in-game 2026-11-19)
- ✓ Build guard detects build 3233 at runtime — v0.4.0-alpha.1
- ✓ F10 menu opens/closes, can be dragged, and buttons respond — v0.4.0-alpha.1
- ✓ Add money via `GameManager.ChangeMoneySafe(..., TransactionType.Cheat, ...)` with readback verification ($1M add verified) — v0.4.0-alpha.1

### Active

- ✓ Phone app: menu inside the in-game smartphone, clicks stay in the UI, Add verified ($1.11M) — v0.5.0-alpha.1
- ✓ Remove, Undo, persistence, state reset verified in-game; Set and over-removal refusal owner-reported — v0.5.0-alpha.2/3


- [ ] Expanded features (candidates: player stats, time controls) — each only after its API is verified on build 3233
- [ ] Packaged release (DLL + install/uninstall instructions) once the money feature set is runtime-verified

### Out of Scope

- Multiplayer/online use or anti-cheat bypass — single-player utility only
- Official Hovgaard SDK build (`Assets/Mods/...`, v0.3.0) — kept as history in the tree; this project targets MelonLoader
- Direct save-file editing — changes go through game APIs only, saved by the game's own save flow
- Committing game DLLs, interop assemblies, saves or logs — proprietary/private; references come from the user's install

## Context

- Target: Big Ambitions EA 0.9 build 3233, Unity 2022.3.62f2, IL2CPP x64; MelonLoader v0.7.3 Open-Beta (net6). Game installed at a non-Steam path on the owner's PC.
- Game APIs are read from MelonLoader's generated `Il2CppAssemblies` (Il2CppInterop) and bound at compile time; a scratch Mono.Cecil dumper was used to inspect signatures.
- IL2CPP stripping matters at runtime: `GUI.Toolbar` (→ `GUIContent.Temp(string[])`) fails with "Method unstripping failed"; `GUIStyle(GUIStyle)` ctor is absent. Only `GUI.Box/Button/Label` with `Rect` + string are proven.
- IMGUI `GUI.Button` click detection did not fire over the game world; menu clicks are resolved from `Mouse.current` (Input System) instead.
- The game reads mouse input through static `Il2CppBigAmbitions.InputSystem.InputActionHelper` (click/scroll helpers, `Pressed/Pressing/Released/Value(PlayerAction)`).
- Prior handoff: `CLAUDE_HANDOFF.md`; user docs: `docs/MELONLOADER.md`; history: `CHANGELOG.md`.
- A separate third-party trainer (`BigAmbitionsTrainer.dll`) is broken on this build and was removed from `Mods`.

## Constraints

- **Compatibility**: build 3233 + MelonLoader 0.7.x only; money writes blocked on other builds unless `AllowOtherBuilds=true` — APIs verified only for 3233.
- **Safety**: no guessed APIs; every write path needs compile-time binding plus runtime readback; failures latch writes off for the session.
- **Toolchain**: .NET 8 SDK (user-local `~\.dotnet`), project targets net6.0; references via `GamePath` (local `GamePath.props.user`, git-ignored).
- **Repository**: develop on `compat/ea-0.9.3233-melonloader`; `main` fast-forwarded to it on owner request (2026-11-19, c253c5e). No releases/merges without the owner's go-ahead.
- **Verification honesty**: distinguish build-verified vs runtime-verified; only the owner can run in-game tests.

## Key Decisions

| Decision | Rationale | Outcome |
|----------|-----------|---------|
| Typed interop calls instead of reflection | Signature drift becomes a compile error / caught MissingMethodException | ✓ Good |
| Use game's `TransactionType.Cheat` via `ChangeMoneySafe` | Supported domain operation, logged like normal transactions | ✓ Good (Add verified) |
| Amount entry by buttons, no text field | IMGUI can't stop keystrokes reaching game hotkeys | ✓ Good |
| Fixed-rect `GUI.*` only, no GUILayout/Toolbar/Window | IL2CPP stripping | ✓ Good |
| Menu clicks from Input System, IMGUI only draws | IMGUI clicks failed over game world in-game | ✓ Good |
| Click-through blocking via Harmony prefixes on `InputActionHelper` | Game centralizes mouse reads there | ⚠️ Failed in-game (likely inlining) |
| Keep $10M per-change limit (balance $0–$100M), not configurable | Owner decision 2026-11-19; safety cap; picker may exceed it for Set, checked at Stage | ✓ Decided |
| Menu as an in-game smartphone app built from game uGUI | Game UI owns clicks; matches game look | ✓ Good (verified 2026-11-19) |

---
*Last updated: 2026-11-19 after GSD initialization (brownfield, inline)*
