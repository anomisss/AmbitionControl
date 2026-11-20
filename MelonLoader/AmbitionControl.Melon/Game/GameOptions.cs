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
        WholesaleImportLimits,  // disableWholesaleAndImportLimits
        AllCoursesUnlocked,     // allCoursesUnlocked   (positive flag: ON = unlocked)
        AllContactsUnlocked,    // allContactsUnlocked  (positive flag)
        AllImporterProducts,    // allProductsAvailableFromImporters (positive flag)
        Tutorial                // tutorialEnabled      (positive flag)
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
            GameOption.VehicleDamage, GameOption.VehicleFuel, GameOption.WholesaleImportLimits,
            GameOption.AllCoursesUnlocked, GameOption.AllContactsUnlocked, GameOption.AllImporterProducts, GameOption.Tutorial
        };

        /// <summary>True for "disableX" flags, where the displayed ON (normal behaviour) means the flag is false.</summary>
        internal static bool IsDisableFlag(GameOption o) => o <= GameOption.WholesaleImportLimits;

        /// <summary>What the button shows: ON/OFF of the behaviour named by Label().</summary>
        internal static bool DisplayOn(GameOption o, bool flag) => IsDisableFlag(o) ? !flag : flag;

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
                case GameOption.WholesaleImportLimits: return "Wholesale/import limits";
                case GameOption.AllCoursesUnlocked: return "All courses unlocked";
                case GameOption.AllContactsUnlocked: return "All contacts unlocked";
                case GameOption.AllImporterProducts: return "All importer products";
                default: return "Tutorial";
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
                case GameOption.WholesaleImportLimits: return gv.disableWholesaleAndImportLimits;
                case GameOption.AllCoursesUnlocked: return gv.allCoursesUnlocked;
                case GameOption.AllContactsUnlocked: return gv.allContactsUnlocked;
                case GameOption.AllImporterProducts: return gv.allProductsAvailableFromImporters;
                default: return gv.tutorialEnabled;
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
                case GameOption.WholesaleImportLimits: gv.disableWholesaleAndImportLimits = value; break;
                case GameOption.AllCoursesUnlocked: gv.allCoursesUnlocked = value; break;
                case GameOption.AllContactsUnlocked: gv.allContactsUnlocked = value; break;
                case GameOption.AllImporterProducts: gv.allProductsAvailableFromImporters = value; break;
                default: gv.tutorialEnabled = value; break;
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

            bool newFlag = !snap.OptionDisabled[(int)option];
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

                WriteDisabled(gv, option, newFlag);
                bool readBack = ReadDisabled(gv, option);
                if (readBack == newFlag)
                {
                    snap.OptionDisabled[(int)option] = readBack;
                    Status = name + " " + (DisplayOn(option, readBack) ? "ON" : "OFF") + " (verified). Stored in your save when the game saves.";
                    AmbitionControlMod.Log.Msg(name + " " + (DisplayOn(option, readBack) ? "ON" : "OFF") + " verified.");
                }
                else
                {
                    Status = name + " change did not stick (read back " + (DisplayOn(option, readBack) ? "ON" : "OFF") + ").";
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
