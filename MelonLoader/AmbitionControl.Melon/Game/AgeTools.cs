using System;
using Il2Cpp;
using Il2CppHelpers;

namespace AmbitionControl.Game
{
    /// <summary>
    /// Player age (Milestone J). Build-3233 interop evidence:
    ///   GameManager.Command_ChangeAge(float amount) — the game's own command (unit not documented; measured at runtime)
    ///   PlayerHelper.CharacterData.ageInDays (int) — readback; GameInstance.gameVariables.daysPerYear — days per game year
    ///   AppearanceAgeHelper.MinAge, PlayerHelper.DeathAge, PlayerHelper.PermanentZombieWalkingAge — safety bounds
    /// One year per press, two-press confirm, never below the game's minimum age or within a year of the
    /// zombie-walking / death ages.
    /// </summary>
    internal static class AgeTools
    {
        internal static string Status { get; private set; }
        internal static int PendingDelta { get; private set; }

        private static bool TryRead(out int days, out int perYear)
        {
            days = 0; perYear = 0;
            CharacterData cd = PlayerHelper.CharacterData;
            GameInstance gi = SaveGameManager.Current;
            if (cd == null || gi == null || gi.gameVariables == null) return false;
            days = cd.ageInDays;
            perYear = gi.gameVariables.daysPerYear;
            return perYear > 0;
        }

        internal static string AgeText()
        {
            try
            {
                if (!TryRead(out int d, out int py)) return "Age: unavailable";
                return "Age " + (d / py) + " (" + d + " days, " + py + " days per year)";
            }
            catch (Exception ex) { return "Age: unavailable (" + ex.GetBaseException().Message + ")"; }
        }

        private static string Bounds(int years, int delta)
        {
            int target = years + delta;
            int min = Math.Max(1, AppearanceAgeHelper.MinAge);
            int max = Math.Min(PlayerHelper.DeathAge, PlayerHelper.PermanentZombieWalkingAge) - 2;
            if (target < min) return "the game's minimum age is " + min + ".";
            if (target > max) return "ages above " + max + " are blocked (the game's frailty/death ages start at " + (max + 2) + ").";
            return null;
        }

        internal static void ChangePressed(int delta, GameSnapshot snap, Settings settings)
        {
            string block = PlayerTools.BlockReason(snap, settings);
            if (block != null) { PendingDelta = 0; Status = "Age not changed: " + block; return; }
            try
            {
                if (!TryRead(out int before, out int perYear)) { PendingDelta = 0; Status = "Age not changed: age could not be read."; return; }
                int years = before / perYear;
                string bound = Bounds(years, delta);
                if (bound != null) { PendingDelta = 0; Status = "Age not changed: " + bound; return; }
                if (PendingDelta != delta)
                {
                    PendingDelta = delta;
                    Status = "Change age " + years + " -> " + (years + delta) + "? Your character's look may change with age. Press Confirm.";
                    return;
                }
                PendingDelta = 0;

                // The command's unit isn't documented: apply 1 and measure. If it moved by about a day, top up to a full year.
                GameManager.Command_ChangeAge(delta);
                TryRead(out int mid, out _);
                int moved = mid - before;
                string unit = Math.Abs(moved - delta * perYear) <= 1 ? "years" : moved == delta ? "days" : "unknown";
                if (unit == "unknown")
                {
                    Status = "Age: the game's command moved " + moved + " days (expected 1 day or 1 year); stopped without further changes.";
                    AmbitionControlMod.Log.Warning("Age change stopped: first call moved " + moved + " days (" + before + " -> " + mid + "), " + perYear + " days/year");
                    return;
                }
                if (unit == "days") GameManager.Command_ChangeAge(delta * (perYear - 1));
                TryRead(out int after, out _);
                int diff = after - before;
                bool ok = Math.Abs(diff - delta * perYear) <= 1;
                Status = ok
                    ? "Age changed (verified): " + years + " -> " + (after / perYear) + " (" + before + " -> " + after + " days)."
                    : "Age changed by " + diff + " days instead of " + (delta * perYear) + ".";
                AmbitionControlMod.Log.Msg("Age change: command unit measured as " + unit + " (first call moved " + moved + " days); " +
                                           before + " -> " + after + " days, " + perYear + " days/year" + (ok ? " (verified)" : " (NOT as expected)"));
            }
            catch (Exception ex)
            {
                PendingDelta = 0;
                Status = "Age change failed: " + ex.GetBaseException().Message;
                AmbitionControlMod.Log.Error(Status);
            }
        }

        internal static void ClearPending() => PendingDelta = 0;
    }
}
