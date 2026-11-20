using System;
using System.Collections.Generic;
using System.Linq;
using Il2Cpp;

namespace AmbitionControl.Game
{
    internal enum EconSetting
    {
        Tax, MarketPrices, Salaries, RivalDifficulty, BankInterest, BankNegativeRate,
        CustomerPromotion, WholesaleUrgentFee, ImporterUrgentFee
    }

    /// <summary>
    /// Economy and difficulty controls (Milestone C/D). Values live on GameInstance.gameVariables (part of the save).
    /// Safe ranges come from the game itself: the only values offered are those used by the game's own difficulty
    /// presets, read at runtime via DifficultySetting.GetDifficultySettings(Difficulty.Easy/Normal/Hard)
    /// (build-3233 interop; Difficulty = Custom, Easy, Normal, Hard — there is no "Brutal" in this build).
    /// Every write is read back. Applying a preset also sets gameVariables.difficulty.
    /// Not touched: startingAge, startingMoney, daysPerYear (start-of-game values; changing daysPerYear mid-game could
    /// disturb aging/calendar logic).
    /// </summary>
    internal static class GameEconomy
    {
        internal static readonly EconSetting[] All =
            (EconSetting[])Enum.GetValues(typeof(EconSetting));
        internal static readonly Difficulty[] Presets = { Difficulty.Easy, Difficulty.Normal, Difficulty.Hard };

        internal static string Status { get; private set; }

        private static float[][] presetValues;   // [preset index][setting]
        private static float[][] choices;        // [setting] distinct sorted preset values
        private static string loadError;
        private static Difficulty? pendingPreset;

        internal static Difficulty? PendingPreset => pendingPreset;

        internal static string Label(EconSetting s)
        {
            switch (s)
            {
                case EconSetting.Tax: return "Tax";
                case EconSetting.MarketPrices: return "Market prices";
                case EconSetting.Salaries: return "Staff salaries";
                case EconSetting.RivalDifficulty: return "Rival difficulty";
                case EconSetting.BankInterest: return "Bank interest";
                case EconSetting.BankNegativeRate: return "Bank negative rate";
                case EconSetting.CustomerPromotion: return "Customer promotion";
                case EconSetting.WholesaleUrgentFee: return "Wholesale urgent fee";
                default: return "Importer urgent fee";
            }
        }

        internal static string Format(EconSetting s, float v) =>
            s == EconSetting.Tax ? ((int)Math.Round(v)) + "%" :
            s == EconSetting.BankNegativeRate ? v.ToString("0.###") : "x" + v.ToString("0.##");

        internal static float Read(GameVariables gv, EconSetting s)
        {
            switch (s)
            {
                case EconSetting.Tax: return gv.taxPercentage;
                case EconSetting.MarketPrices: return gv.marketPriceMultiplier;
                case EconSetting.Salaries: return gv.employeeHourlySalaryMultiplier;
                case EconSetting.RivalDifficulty: return gv.rivalsDifficultyMultiplier;
                case EconSetting.BankInterest: return gv.bankInterestMultiplier;
                case EconSetting.BankNegativeRate: return gv.bankInterestRate;
                case EconSetting.CustomerPromotion: return gv.baseCustomerPromotionMultiplier;
                case EconSetting.WholesaleUrgentFee: return gv.wholesaleUrgentFeeMultiplier;
                default: return gv.importerUrgentFeeMultiplier;
            }
        }

        private static void Write(GameVariables gv, EconSetting s, float v)
        {
            switch (s)
            {
                case EconSetting.Tax: gv.taxPercentage = (int)Math.Round(v); break;
                case EconSetting.MarketPrices: gv.marketPriceMultiplier = v; break;
                case EconSetting.Salaries: gv.employeeHourlySalaryMultiplier = v; break;
                case EconSetting.RivalDifficulty: gv.rivalsDifficultyMultiplier = v; break;
                case EconSetting.BankInterest: gv.bankInterestMultiplier = v; break;
                case EconSetting.BankNegativeRate: gv.bankInterestRate = v; break;
                case EconSetting.CustomerPromotion: gv.baseCustomerPromotionMultiplier = v; break;
                case EconSetting.WholesaleUrgentFee: gv.wholesaleUrgentFeeMultiplier = v; break;
                default: gv.importerUrgentFeeMultiplier = v; break;
            }
        }

        private static float ReadPreset(DifficultySetting d, EconSetting s)
        {
            switch (s)
            {
                case EconSetting.Tax: return d.taxPercentage;
                case EconSetting.MarketPrices: return d.marketPriceMultiplier;
                case EconSetting.Salaries: return d.employeeHourlySalaryMultiplier;
                case EconSetting.RivalDifficulty: return d.rivalsDifficultyMultiplier;
                case EconSetting.BankInterest: return d.bankInterestMultiplier;
                case EconSetting.BankNegativeRate: return d.bankInterestRate;
                case EconSetting.CustomerPromotion: return d.baseCustomerPromotionMultiplier;
                case EconSetting.WholesaleUrgentFee: return d.wholesaleUrgentFeeMultiplier;
                default: return d.importerUrgentFeeMultiplier;
            }
        }

        private static bool Same(float a, float b) => Math.Abs(a - b) < 0.0005f;

        /// <summary>Load the game's preset values once (main thread, game running).</summary>
        internal static bool EnsurePresets()
        {
            if (presetValues != null) return true;
            try
            {
                var values = new float[Presets.Length][];
                for (int p = 0; p < Presets.Length; p++)
                {
                    DifficultySetting d = DifficultySetting.GetDifficultySettings(Presets[p]);
                    if (d == null) { loadError = "Difficulty preset " + Presets[p] + " not found."; return false; }
                    values[p] = All.Select(s => ReadPreset(d, s)).ToArray();
                }
                choices = All.Select(s => values.Select(v => v[(int)s]).Distinct().OrderBy(v => v).ToArray()).ToArray();
                presetValues = values;
                for (int p = 0; p < Presets.Length; p++)
                    AmbitionControlMod.Log.Msg("Game preset " + Presets[p] + ": " +
                        string.Join(", ", All.Select(s => Label(s) + " " + Format(s, values[p][(int)s]))));
                return true;
            }
            catch (Exception ex)
            {
                loadError = "Could not read the game's difficulty presets: " + ex.GetBaseException().Message;
                AmbitionControlMod.Log.Error(loadError);
                return false;
            }
        }

        internal static string BlockReason(GameSnapshot snap, Settings settings)
        {
            if (!settings.EconomyControlsEnabled) return "Economy controls are turned off in preferences (EconomyControlsEnabled).";
            int? build = GameBridge.BuildNumber;
            if (!build.HasValue) return "Game build not detected yet.";
            if (build.Value != GameBridge.TargetBuild && !settings.AllowOtherBuilds)
                return "Game build " + build.Value + " is not the verified build " + GameBridge.TargetBuild + ".";
            if (snap == null || !snap.Loaded) return snap?.NotLoadedReason ?? "No save loaded.";
            if (snap.Saving) return "Game is saving; wait a moment.";
            if (!snap.OptionsKnown) return "Game options not available for this save.";
            if (presetValues == null && !EnsurePresets()) return loadError;
            return null;
        }

        /// <summary>Which presets use this value, e.g. "Normal" or "Easy/Hard"; null if none.</summary>
        internal static string PresetNames(EconSetting s, float v)
        {
            if (presetValues == null) return null;
            var names = new List<string>();
            for (int p = 0; p < Presets.Length; p++)
                if (Same(presetValues[p][(int)s], v)) names.Add(Presets[p].ToString());
            return names.Count == 0 ? null : string.Join("/", names);
        }

        private static GameVariables SessionVariables(GameSnapshot snap)
        {
            GameInstance gi = SaveGameManager.Current;
            return gi != null && gi.Pointer == snap.InstancePtr ? gi.gameVariables : null;
        }

        /// <summary>Step one setting to the next preset value (wraps around), with readback.</summary>
        internal static void Cycle(EconSetting s, GameSnapshot snap, Settings settings)
        {
            pendingPreset = null;
            string block = BlockReason(snap, settings);
            if (block != null) { Status = Label(s) + " not changed: " + block; return; }
            try
            {
                GameVariables gv = SessionVariables(snap);
                if (gv == null) { Status = Label(s) + " not changed: the save changed. Try again."; return; }
                float before = Read(gv, s);
                float[] c = choices[(int)s];
                float next = c.FirstOrDefault(v => v > before + 0.0005f);
                if (!c.Any(v => v > before + 0.0005f)) next = c[0];
                if (Same(next, before)) { Status = Label(s) + ": the game's presets offer only " + Format(s, before) + "."; return; }

                Write(gv, s, next);
                float after = Read(gv, s);
                if (Same(after, next))
                {
                    snap.Econ[(int)s] = after;
                    Status = Label(s) + ": " + Format(s, before) + " -> " + Format(s, after) + " (verified). Stored in your save when the game saves.";
                    AmbitionControlMod.Log.Msg(Label(s) + " " + Format(s, before) + " -> " + Format(s, after) + " verified.");
                }
                else
                {
                    Status = Label(s) + " did not stick (read back " + Format(s, after) + ").";
                    AmbitionControlMod.Log.Error(Status);
                }
            }
            catch (Exception ex)
            {
                Status = Label(s) + " change failed: " + ex.GetBaseException().Message;
                AmbitionControlMod.Log.Error(Status);
            }
        }

        /// <summary>First press stages the preset (shows what changes); second press of the same preset applies it.</summary>
        internal static void PresetPressed(Difficulty preset, GameSnapshot snap, Settings settings)
        {
            string block = BlockReason(snap, settings);
            if (block != null) { pendingPreset = null; Status = preset + " preset not applied: " + block; return; }

            int p = Array.IndexOf(Presets, preset);
            int changes = All.Count(s => !Same(snap.Econ[(int)s], presetValues[p][(int)s]));
            if (pendingPreset != preset)
            {
                pendingPreset = preset;
                Status = "Apply the game's " + preset + " economy preset? " + changes + " of " + All.Length +
                         " settings would change (stored in your save). Press " + preset + " again to confirm.";
                return;
            }

            pendingPreset = null;
            try
            {
                GameVariables gv = SessionVariables(snap);
                if (gv == null) { Status = preset + " preset not applied: the save changed. Try again."; return; }
                int ok = 0;
                foreach (EconSetting s in All)
                {
                    Write(gv, s, presetValues[p][(int)s]);
                    float after = Read(gv, s);
                    snap.Econ[(int)s] = after;
                    if (Same(after, presetValues[p][(int)s])) ok++;
                }
                gv.difficulty = preset;
                bool diffOk = gv.difficulty == preset;
                snap.CurrentDifficulty = gv.difficulty;
                if (ok == All.Length && diffOk)
                {
                    Status = preset + " economy preset applied (" + ok + "/" + All.Length + " verified). Stored in your save when the game saves.";
                    AmbitionControlMod.Log.Msg(preset + " preset applied and verified (" + changes + " values changed).");
                }
                else
                {
                    Status = preset + " preset partly applied: " + ok + "/" + All.Length + " values verified" + (diffOk ? "" : ", difficulty label not set") + ".";
                    AmbitionControlMod.Log.Error(Status);
                }
            }
            catch (Exception ex)
            {
                Status = preset + " preset failed: " + ex.GetBaseException().Message;
                AmbitionControlMod.Log.Error(Status);
            }
        }

        internal static void ClearPending() => pendingPreset = null;
    }
}
