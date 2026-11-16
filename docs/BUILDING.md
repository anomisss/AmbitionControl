# Building Ambition Control v0.2.0

Official SDK: https://github.com/hovgaardgames/bigambitions

1. Install Unity 2022.3.62f2 (with the macOS Build Support module if requested by the SDK) and Big Ambitions through Steam.
2. Clone the official SDK and open its Unity project.
3. Import the game's DLLs using the SDK welcome/setup flow, including after game version changes.
4. Copy this repository's `Assets/Mods/AmbitionControl` folder into the SDK project's `Assets/Mods/` directory. Do **not** replace the entire SDK.
5. Wait for Unity compilation. The mod manifest is an SDK BAModManifest asset with ModId `AmbitionControl`, version `0.2.0`, and Windows target. If Unity reports a broken assembly reference, reassign the `AmbitionControl.asmdef` to the manifest's ModAssembly field and save.
6. Use **Big Ambitions > Mod Builder > Build & Install**. Verify validation results and the resulting `ModsLocal` install.
7. Launch the game, activate the locally installed mod, and press F10. Try changing tabs and reopening the window. No cheats should change game state.
8. Open Diagnostics and click **Write API candidate report**, if game integration discovery is required. It writes an `api-candidates.txt` file in Unity's persistent data folder under `AmbitionControl`.
9. Test on a **duplicate save** and retain Player.log if the SDK or game fails to load the mod.

This source tree contains no game DLLs, no built asset bundles and no compiled mod. **Neither Unity compilation nor in-game functionality has been verified yet.** The report is explicitly opt-in and contains type/member metadata only.
