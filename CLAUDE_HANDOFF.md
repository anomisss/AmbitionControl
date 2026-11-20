# CLAUDE HANDOFF — Ambition Control (Big Ambitions)

**Prepared:** 2026-11-19  
**Repository:** https://github.com/anomisss/AmbitionControl (public, owner `anomisss`)  
**Default branch:** `main` — baseline commit `019926f3302c4de306b1d98c7c71a6a66da95b4c`  
**Active compatibility branch:** `compat/ea-0.9.3233-melonloader`  
**Compatibility code commit before this handoff:** `f44e672bc07518a4d3f5aecd43134fe8b183c543`  
**Baseline source version:** v0.3.0 (SDK variant). **MelonLoader adaptation:** incomplete work in progress, informally `0.4.0-alpha.1` in source; **not an actual published/tested release**.

## 1. User's explicit goal and environment

Build and maintain **Ambition Control**, a single-player utility / cheat-menu mod for **Big Ambitions**. The user specifically reports:
- Game version/build: **0.9.3233**.
- **MelonLoader** is already installed.
- Wants the mod **made compatible with that exact installed game and loader setup**, with usable instructions for installation.
- Has authorized ongoing GitHub development iteration tracking for this project; respect existing repository/branch/history. Do not assume permission for destructive changes or publishing a release.
- Wants Claude to **take over development from this checkpoint**.

**Unknown and must verify:** actual MelonLoader version, game's managed-vs-IL2CPP backend for this installed build, local install path, generated interop assemblies, installed SDK/.NET versions, exact runtime APIs on 0.9.3233, and whether user can supply clean logs or local compilation evidence. The prior assistant *said* “Unity IL2CPP” but had **no direct game/loader logs**. Treat that as an unverified working assumption, not a proven requirement.

## 2. What exists in the repository

### `main`: v0.3.0 *official SDK mod* source

**Authoritative source path:** `Assets/Mods/AmbitionControl/`  
**Docs:** `README.md`, `docs/BUILDING.md`, `docs/versions/v0.3.0.md`, `CHANGELOG.md`.

Files and their intent:
- `Scripts/AmbitionControlMod.cs` — Big Ambitions **SDK** entry interface `IModBigAmbitions`, SDK context/asset registration, Unity `MonoBehaviour` host. **Not a MelonLoader entrypoint**.
- `Scripts/TrainerMenu.cs` — F10 Unity IMGUI menu with Money/Player/Time/Diagnostics/Settings tabs. Money operations are stage → preview → confirm, with guarded one-step undo. Player and time are read-only.
- `Scripts/MoneyCommand.cs` — mostly pure input and amount validation: $10M maximum per operation, $100M supported total. Best candidate to reuse in new loader project after arranging namespaces and build.
- `Scripts/MoneyController.cs` — direct calls to `SaveGameManager.Current.Money`, `GameManager.ChangeMoneySafe(...)` and `TransactionInfo`. **These APIs were only inferred/cross-referenced from other public mod code, not verified for game 0.9.3233**.
- `Scripts/TrainerSettings.cs` — persists window settings using Unity's persistent data location.
- `Scripts/ApiProbe.cs` — opt-in type/member metadata report.
- `AmbitionControl.asmdef`, `ModManifest.asset`, `Locales/en.json` — **SDK / Unity build infrastructure**, not usable as a stand-alone MelonLoader DLL.

**v0.3.0 verification:** source committed. Never built in target Unity SDK or executed in-game. No verified ready-to-install binary. Default branch currently remains this version.

### Compatibility branch: `compat/ea-0.9.3233-melonloader`

Exactly one additional source file at this checkpoint:
- `MelonLoader/GameBridge.cs` (commit `f44e672`) — a **prototype reflection-based adapter** intended to inspect `SaveGameManager`, `GameManager`, and `TransactionInfo` at runtime. Implements tentative read-only player/time state, guarded money adjustment/undo calls and a metadata-report writer. It chooses to disable writes unless it can verify an object pointer and certain signatures.

**Critical:** This file is **not compiled, connected, installed, or tested**. It has no MelonLoader mod subclass or startup hook, no `.csproj`, no compiled DLL, and no MelonLoader UI. Its public contract and reflection approach may not work on the actual 0.9.3233 runtime. Do not claim it works. The text `0.4.0-alpha.1` is a speculative source label, not an issued release.

**Design risks to investigate:** 
1. If the installed game uses IL2CPP, how does the installed MelonLoader expose generated interop/proxy assemblies and constructors? Does `System.Reflection` find the correct members and invocation types? If Mono, use the appropriate managed-assembly path instead.
2. `GameBridge` searches a particular `ChangeMoneySafe(float, TransactionInfo, bool, bool)` method and `TransactionInfo(string)` constructor — neither is verified for build 3233; signatures may differ. Keep money writes **disabled** until confirmed.
3. `Pointer` is treated as a save identity guard; wrapper shape/lifecycle is unverified. It may be unavailable and legitimately disable writes. Do not remove the safety check without replacing it with a **verified** save/session identity.
4. Reuse of the old `MoneyCommand` and `MoneyAction` requires a proper namespace/build strategy; current `GameBridge.cs` alone **will not compile as a self-contained project**.
5. MelonLoader input handling, IMGUI event behavior, cursor focus, scene teardown, and game UI conflicts remain unverified.
6. `GameBridge` metadata report is intentional opt-in and should not collect save contents or secrets.

## 3. Verified versus not verified

**Verified:** GitHub repo exists, public; `main` contains SDK v0.3.0 source; compatibility branch exists and contains the `GameBridge.cs` prototype; repository history is preserved.

**Not verified:** game build 3233's actual loader/backend/runtime API compatibility; any successful compilation; loading into the game; F10 menu operation in MelonLoader; money, undo, time or player functionality; runtime safety; distribution binary/install package.

Do **not** label the current branch a compatible or working build. It is a transfer checkpoint.

## 4. Next concrete milestone for Claude

Deliver the **first buildable, installable MelonLoader compatibility candidate** specifically targeting **Big Ambitions 0.9.3233**, without breaking the SDK `main` history.

Suggested execution:
1. Read current repo/branch and this document; inspect `MelonLoader/GameBridge.cs` and the SDK v0.3.0 source.
2. Confirm target game's scripting backend and installed **MelonLoader version** through actual user-provided loader logs/game metadata. Do not silently assume IL2CPP.
3. Define a new independent MelonLoader mod project (correct `MelonMod` or supported entrypoint, assembly attributes, compatible target framework/references, project file, build instructions), taking care **not** to compile the SDK `IModBigAmbitions` entry into the MelonLoader DLL.
4. Port safe/reusable business logic and implement F10 UI (including visibility, focus, cursor, scene lifecycle), read-only money/player/time diagnostics and descriptive unsupported states.
5. Validate money mutation API *against version 0.9.3233*. Do not call guessed money hooks. Money writes may remain disabled in first candidate pending evidence; clearly mark feature status. Preserve stage/preview/confirm, bounds, state readback and guarded undo when supported.
6. Implement a game-version/compatibility guard that fails safely for unexpected versions where reliable version metadata is available.
7. Build using **actual installed/generated references**, then perform focused tests; only claim runtime verification after an actual 0.9.3233 in-game test, ideally on a backup copy of a save.
8. Update project notes/changelog/version, commit coherent increments to the compatibility branch. Make a PR/merge only after evidence and with user authorization appropriate to the operation.
9. Package a real built DLL when available, with explicit Windows installation/upgrade/uninstall and log locations; never call a source-only ZIP an installable mod.

**Acceptance criteria for the first actual candidate:** loader detects and initializes the mod without exceptions; menu opens/closes; no unwanted mouse/input blocking; save-related UI handles missing/loaded saves gracefully; every supported feature has real readback evidence; unsupported write features remain visibly disabled; reinstall/removal does not damage saves. Record `Passed / Failed / Not run / Blocked` for each check.

## 5. Prior reference links (NOT proof for v0.9.3233)
- Big Ambitions official SDK: https://github.com/hovgaardgames/bigambitions
- Prior money method reference: https://github.com/capisoft-lib/BigAmbitions_BetterFines/blob/main/Scripts/FineService.cs
- Prior finance/player examples: https://github.com/kleeedolinux/CloverAPI

The official SDK and public examples may describe a **different game version or mod system**; inspect compatible signatures before use.

## 6. Communication and repository hygiene

- Start at `compat/ea-0.9.3233-melonloader`. Keep `main` intact.
- Do not publish the user's game DLLs, interop binaries, game save files, secrets, logs containing private data, or proprietary assemblies in this public repository.
- Ask only for missing *material* facts, especially the **MelonLoader version** and **a sanitized `MelonLoader/Latest.log`** if no direct local inspection is available. A game build string alone does not determine an API signature.
- Respect the existing project coding-agent instructions if the user provides their `MASTER CODING & MODDING ENGINEER — v2.0` prompt. Distinguish source complete, build verified and runtime verified.
- Existing SDK `docs/BUILDING.md` describes **official SDK build steps only**; do not present them as MelonLoader install steps.
- Single-player/user-authorized tooling; don't bypass anti-cheat, access controls or other players' authority.

## 7. Paste into Claude to start

> You are taking over my Big Ambitions mod project **Ambition Control**. Read this entire `CLAUDE_HANDOFF.md` from `anomisss/AmbitionControl` on branch `compat/ea-0.9.3233-melonloader`, inspect the actual source and git history, and continue development. My game is **Big Ambitions 0.9.3233**, and I already have **MelonLoader installed**. Do not treat old SDK source or the experimental `GameBridge.cs` as a working MelonLoader build. First establish actual loader/backend/interop compatibility; then implement the smallest valid buildable MelonLoader entry + UI + safe diagnostics, verify against my version, and only enable actual money writes when signatures and state readback are proven. Keep work in the compatibility branch, commit iterations with versions/changelog, and provide exact install/test steps and honest verification status. Don't ask me to re-explain known project history.
