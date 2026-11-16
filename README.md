# Ambition Control

An independent **Big Ambitions** utility and trainer project. Source is maintained in versioned Git history.

## Current: v0.2.0 (source; not yet game-tested)

- F10 draggable IMGUI control menu and menu settings persistence.
- Tabs for Money, Player, Time, Diagnostics and Settings.
- Read-only, opt-in game API metadata report.
- **No money, need, or time cheats are enabled yet.** No confirmed game-state adapter has been implemented.
- Official SDK registration and mod manifest source are included.

**Installation:** see [docs/BUILDING.md](docs/BUILDING.md). Copy `Assets/Mods/AmbitionControl` into the *official SDK project*, import your local game DLLs, then use its Mod Builder. Copying this source repository directly into ModsLocal will not work.

**Versions:** [CHANGELOG.md](CHANGELOG.md) and [v0.1 historical baseline](docs/versions/v0.1.0.md).

**SDK:** https://github.com/hovgaardgames/bigambitions

This project is community-developed and not affiliated with Hovgaard Games. Do not commit proprietary game DLLs, player saves, private logs, or local credentials.
