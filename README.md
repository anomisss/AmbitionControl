# Ambition Control

A **Big Ambitions** in-game utility/trainer. Versioned source maintained in GitHub.

## Current: MelonLoader build v0.6.0-alpha.2 (`main`; developed on `compat/ea-0.9.3233-melonloader`)

For players running **MelonLoader 0.7.x on Big Ambitions build 3233 (IL2CPP)**. **Download:** [v0.6.0-alpha.2 pre-release](https://github.com/anomisss/AmbitionControl/releases/tag/v0.6.0-alpha.2). Install, usage and build steps: [docs/MELONLOADER.md](docs/MELONLOADER.md).

## SDK variant: v0.3.0 — Native money controls (untested in-game)

- Open F10 to display a draggable Unity IMGUI menu.
- **Money:** see live balance; stage Add, Remove, or Set; review expected outcome; confirm to book a native game transaction. One-step undo, available only while the save and balance remain unchanged.
- **Player:** view current Energy, Hunger and Happiness (read-only).
- **Time:** view current Day, Hour and Minute (read-only).
- **Diagnostics:** opt-in metadata report.
- Other trainer categories remain planned.

The first money integration uses `GameManager.ChangeMoneySafe` from publicly available Big Ambitions mod code. This **has not been compiled or tested locally**. Back up your save before use.

See [build guide](docs/BUILDING.md), [changelog](CHANGELOG.md), [v0.3 implementation notes](docs/versions/v0.3.0.md).

## Getting it into the game
Copy `Assets/Mods/AmbitionControl` into a clone of the [official Big Ambitions SDK](https://github.com/hovgaardgames/bigambitions), import your local game DLLs, then use **Big Ambitions > Mod Builder > Build & Install**. Copying raw C# directly into ModsLocal does not work.

This project is independent from Hovgaard Games. Never commit proprietary game DLLs, saves, or local debug logs.
