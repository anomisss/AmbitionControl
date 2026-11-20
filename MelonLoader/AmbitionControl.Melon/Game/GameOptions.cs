using System;
using Il2Cpp;

namespace AmbitionControl.Game
{
    /// <summary>
    /// The game's own custom-game options, each a bool on GameInstance.gameVariables (build 3233) and each offered
    /// by the game as a checkbox in CustomGameOptionsHandler. Order is used as an index into GameSnapshot.OptionDisabled.
    /// </summary>
    internal enum GameOption
    {
        EnergyDrain,            // disableEnergy
        HappinessDrain,         // disableHappiness
        Aging,                  // disableAging
        VehicleDamage,          // disableVehicleDamage
        VehicleFuel,            // disableVehicleFuel
        WholesaleImportLimits   // disableWholesaleAndImportLimits
    }

    /// <summary>
    /// Switch game options on/off with readback. The flags are part of the save, so a change persists once the game
    /// saves; switching back ON restores the normal behaviour. Verified in-game for EnergyDrain/HappinessDrain (the game
    /// reads them live); the other four use the identical mechanism.
    /// </summary>
    internal static class GameOptions
    {
        internal static readonly GameOption[] All =
        {
            GameOption.EnergyDrain, GameOption.HappinessDrain, GameOption.Aging,
            GameOption.VehicleDamage, GameOption.VehicleFuel, GameOption.WholesaleImportLimits
        };

        internal static string Status { get; private set; }

        /// <summary>Short name of the behaviour that the option turns off ("ON" = normal game behaviour).</summary>
        internal static string Label(GameOption o)
        {
            switch (o)
            {
                case GameOption.EnergyDrain: return "Energy drain";
                case GameOption.HappinessDrain: return "Happiness drain";
                case GameOption.Aging: return "Aging";
                case GameOption.VehicleDamage: return "Vehicle damage";
                case GameOption.VehicleFuel: return "Vehicle fuel use";
                default: return "Wholesale/import limits";
            }
        }

        internal static bool ReadDisabled(GameVariables gv, GameOption o)
        {
            switch (o)
            {
                case GameOption.EnergyDrain: return gv.disableEnergy;
                case GameOption.HappinessDrain: return gv.disableHappiness;
                case GameOption.Aging: return gv.disableAging;
                case GameOption.VehicleDamage: return gv.disableVehicleDamage;
                case GameOption.VehicleFuel: return gv.disableVehicleFuel;
                default: return gv.disableWholesaleAndImportLimits;
            }
        }

        private static void WriteDisabled(GameVariables gv, GameOption o, bool value)
        {
            switch (o)
            {
                case GameOption.EnergyDrain: gv.disableEnergy = value; break;
                case GameOption.HappinessDrain: gv.disableHappiness = value; break;
                case GameOption.Aging: gv.disableAging = value; break;
                case GameOption.VehicleDamage: gv.disableVehicleDamage = value; break;
                case GameOption.VehicleFuel: gv.disableVehicleFuel = value; break;
                default: gv.disableWholesaleAndImportLimits = value; break;
            }
        }

        internal static string BlockReason(GameSnapshot snap, Settings settings)
        {
            if (!settings.PlayerTogglesEnabled) return "Game option switches are turned off in preferences (PlayerTogglesEnabled).";
            int? build = GameBridge.BuildNumber;
            if (!build.HasValue) return "Game build not detected yet.";
            if (build.Value != GameBridge.TargetBuild && !settings.AllowOtherBuilds)
                return "Game build " + build.Value + " is not the verified build " + GameBridge.TargetBuild + ".";
            if (snap == null || !snap.Loaded) return snap?.NotLoadedReason ?? "No save loaded.";
            if (snap.Saving) return "Game is saving; wait a moment.";
            if (!snap.OptionsKnown) return "Game options not available for this save.";
            return null;
        }

        /// <summary>Flip one option (normal ⇄ disabled), read it back, and update the snapshot on success.</summary>
        internal static void Toggle(GameOption option, GameSnapshot snap, Settings settings)
        {
            string name = Label(option);
            string block = BlockReason(snap, settings);
            if (block != null) { Status = name + " not changed: " + block; return; }

            bool disabled = !snap.OptionDisabled[(int)option];
            try
            {
                GameInstance gi = SaveGameManager.Current;
                if (gi == null || gi.Pointer != snap.InstancePtr)
                {
                    Status = name + " not changed: the save changed. Try again.";
                    return;
                }
                GameVariables gv = gi.gameVariables;
                if (gv == null) { Status = name + " not changed: game options unavailable."; return; }

                WriteDisabled(gv, option, disabled);
                bool readBack = ReadDisabled(gv, option);
                if (readBack == disabled)
                {
                    snap.OptionDisabled[(int)option] = readBack;
                    Status = name + " " + (disabled ? "OFF" : "ON") + " (verified). Stored in your save when the game saves.";
                    AmbitionControlMod.Log.Msg(name + " " + (disabled ? "OFF" : "ON") + " verified.");
                }
                else
                {
                    Status = name + " change did not stick (read back " + (readBack ? "OFF" : "ON") + ").";
                    AmbitionControlMod.Log.Error(Status);
                }
            }
            catch (Exception ex)
            {
                Status = name + " change failed: " + ex.GetBaseException().Message;
                AmbitionControlMod.Log.Error(Status);
            }
        }
    }
}
