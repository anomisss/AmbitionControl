using System;
using Il2Cpp;

namespace AmbitionControl.Game
{
    internal enum NeedKind { Energy, Happiness }

    /// <summary>
    /// Energy/happiness drain switches using the game's own custom-game options
    /// (GameInstance.gameVariables.disableEnergy / disableHappiness, verified in build-3233 interop and exposed by the
    /// game's CustomGameOptionsHandler). The flags are part of the save, so they persist once the game saves; turning a
    /// switch back on restores normal drain. Every change is read back before it is reported as done.
    /// </summary>
    internal static class NeedToggles
    {
        internal static string Status { get; private set; }

        internal static string BlockReason(GameSnapshot snap, Settings settings)
        {
            if (!settings.PlayerTogglesEnabled) return "Player toggles are turned off in preferences (PlayerTogglesEnabled).";
            int? build = GameBridge.BuildNumber;
            if (!build.HasValue) return "Game build not detected yet.";
            if (build.Value != GameBridge.TargetBuild && !settings.AllowOtherBuilds)
                return "Game build " + build.Value + " is not the verified build " + GameBridge.TargetBuild + ".";
            if (snap == null || !snap.Loaded) return snap?.NotLoadedReason ?? "No save loaded.";
            if (snap.Saving) return "Game is saving; wait a moment.";
            if (!snap.TogglesKnown) return "Game options not available for this save.";
            return null;
        }

        internal static void Set(NeedKind kind, bool disabled, GameSnapshot snap, Settings settings)
        {
            string name = kind == NeedKind.Energy ? "Energy" : "Happiness";
            string block = BlockReason(snap, settings);
            if (block != null) { Status = name + " drain not changed: " + block; return; }

            try
            {
                GameInstance gi = SaveGameManager.Current;
                if (gi == null || gi.Pointer != snap.InstancePtr)
                {
                    Status = name + " drain not changed: the save changed. Try again.";
                    return;
                }
                GameVariables gv = gi.gameVariables;
                if (gv == null) { Status = name + " drain not changed: game options unavailable."; return; }

                if (kind == NeedKind.Energy) gv.disableEnergy = disabled;
                else gv.disableHappiness = disabled;

                bool readBack = kind == NeedKind.Energy ? gv.disableEnergy : gv.disableHappiness;
                if (readBack == disabled)
                {
                    if (kind == NeedKind.Energy) snap.EnergyDrainOff = readBack;
                    else snap.HappinessDrainOff = readBack;
                    Status = name + " drain " + (disabled ? "OFF" : "ON") + " (verified). Stored in your save when the game saves.";
                    AmbitionControlMod.Log.Msg(name + " drain " + (disabled ? "OFF" : "ON") + " verified.");
                }
                else
                {
                    Status = name + " drain change did not stick (read back " + (readBack ? "OFF" : "ON") + ").";
                    AmbitionControlMod.Log.Error(Status);
                }
            }
            catch (Exception ex)
            {
                Status = name + " drain change failed: " + ex.GetBaseException().Message;
                AmbitionControlMod.Log.Error(Status);
            }
        }
    }
}
