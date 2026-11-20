using System;
using System.Collections.Generic;
using System.Linq;
using Il2Cpp;
using Il2CppBigAmbitions.Neighborhoods;
using Il2CppBigAmbitions.Rivals;

namespace AmbitionControl.Game
{
    internal sealed class RivalRow
    {
        internal Neighbourhood Neighbourhood;
        internal string Id;
        internal string Name;
        internal float WeeklyIncome;
        internal bool Met;        // has a SpecialRivalState in this save
        internal bool Active;
        internal bool Defeated;
    }

    /// <summary>
    /// Rivals (Milestone F part 1). Build-3233 interop evidence:
    ///   RivalsHelper.IsFeatureEnabled, RivalsHelper.GetSpecialRivalByNeighborhood(Neighbourhood) : SpecialRival
    ///   SpecialRival.rivalData : RivalData { id, rivalName, WeeklyIncome }
    ///   GameInstance.specialRivalStates : List&lt;SpecialRivalState { rivalId, isActive, isDefeated }&gt;
    ///   RivalTimeline.DefeatRival(Neighbourhood, bool showMessage)   — the game's own defeat path
    ///   RivalsHelper.IsRivalDefeated(string)                        — readback
    /// Defeat is PERMANENT (the game's OnRivalDefeat shuts down the rival's businesses and sells their real estate),
    /// so it always needs a second press, and only rivals already present in this save can be defeated.
    /// </summary>
    internal static class RivalTools
    {
        internal static readonly Neighbourhood[] Areas =
        {
            Neighbourhood.GarmentDistrict, Neighbourhood.HellsKitchen, Neighbourhood.MurrayHill,
            Neighbourhood.Midtown, Neighbourhood.LowerManhattan, Neighbourhood.IndustryCity
        };

        internal static string Status { get; private set; }
        internal static Neighbourhood? PendingArea { get; private set; }
        internal static bool PendingAll { get; private set; }

        internal static string AreaName(Neighbourhood n)
        {
            switch (n)
            {
                case Neighbourhood.GarmentDistrict: return "Garment District";
                case Neighbourhood.HellsKitchen: return "Hell's Kitchen";
                case Neighbourhood.MurrayHill: return "Murray Hill";
                case Neighbourhood.LowerManhattan: return "Lower Manhattan";
                case Neighbourhood.IndustryCity: return "Industry City";
                default: return n.ToString();
            }
        }

        internal static string BlockReason(GameSnapshot snap, Settings settings)
        {
            if (!settings.RivalToolsEnabled) return "Rival tools are turned off in preferences (RivalToolsEnabled).";
            int? build = GameBridge.BuildNumber;
            if (!build.HasValue) return "Game build not detected yet.";
            if (build.Value != GameBridge.TargetBuild && !settings.AllowOtherBuilds)
                return "Game build " + build.Value + " is not the verified build " + GameBridge.TargetBuild + ".";
            if (snap == null || !snap.Loaded) return snap?.NotLoadedReason ?? "No save loaded.";
            if (snap.Saving) return "Game is saving; wait a moment.";
            try { if (!RivalsHelper.IsFeatureEnabled) return "Rivals are not enabled in this game."; }
            catch (Exception ex) { return "Rival system unavailable: " + ex.GetBaseException().Message; }
            return null;
        }

        private static GameInstance SessionInstance(GameSnapshot snap)
        {
            GameInstance gi = SaveGameManager.Current;
            return gi != null && gi.Pointer == snap.InstancePtr ? gi : null;
        }

        /// <summary>Read-only rows for the UI (called at most once per second while the tab is open).</summary>
        internal static List<RivalRow> ReadRows(GameSnapshot snap, out string error)
        {
            error = null;
            var rows = new List<RivalRow>();
            try
            {
                GameInstance gi = SessionInstance(snap);
                if (gi == null) { error = "No save loaded."; return rows; }
                var states = new Dictionary<string, SpecialRivalState>();
                if (gi.specialRivalStates != null)
                    foreach (SpecialRivalState s in gi.specialRivalStates)
                        if (s != null && !string.IsNullOrEmpty(s.rivalId)) states[s.rivalId] = s;

                foreach (Neighbourhood n in Areas)
                {
                    SpecialRival sr = RivalsHelper.GetSpecialRivalByNeighborhood(n);
                    if (sr == null || sr.rivalData == null) continue;
                    RivalData d = sr.rivalData;
                    var row = new RivalRow { Neighbourhood = n, Id = d.id, Name = d.rivalName ?? d.id };
                    try { row.WeeklyIncome = d.WeeklyIncome; } catch { }
                    if (d.id != null && states.TryGetValue(d.id, out SpecialRivalState st))
                    {
                        row.Met = true;
                        row.Active = st.isActive;
                        row.Defeated = st.isDefeated;
                    }
                    try { if (d.id != null && RivalsHelper.IsRivalDefeated(d.id)) row.Defeated = true; } catch { }
                    rows.Add(row);
                }
            }
            catch (Exception ex)
            {
                error = "Could not read rivals: " + ex.GetBaseException().Message;
            }
            return rows;
        }

        internal static void DefeatPressed(Neighbourhood area, GameSnapshot snap, Settings settings)
        {
            PendingAll = false;
            string block = BlockReason(snap, settings);
            if (block != null) { PendingArea = null; Status = "Rival not defeated: " + block; return; }
            RivalRow row = ReadRows(snap, out string err).FirstOrDefault(r => r.Neighbourhood == area);
            if (row == null) { PendingArea = null; Status = "Rival not defeated: " + (err ?? "no rival for " + AreaName(area) + "."); return; }
            if (row.Defeated) { PendingArea = null; Status = row.Name + " is already defeated."; return; }
            if (!row.Met || !row.Active) { PendingArea = null; Status = row.Name + " is not an active rival in this save yet."; return; }

            if (PendingArea != area)
            {
                PendingArea = area;
                Status = "Defeat " + row.Name + " (" + AreaName(area) + ")? PERMANENT: the game closes all their businesses " +
                         "and sells their buildings. Cannot be undone. Press Confirm.";
                return;
            }
            PendingArea = null;
            Status = Defeat(row);
        }

        internal static void DefeatAllPressed(GameSnapshot snap, Settings settings)
        {
            PendingArea = null;
            string block = BlockReason(snap, settings);
            if (block != null) { PendingAll = false; Status = "Rivals not defeated: " + block; return; }
            List<RivalRow> targets = ReadRows(snap, out _).Where(r => r.Met && r.Active && !r.Defeated).ToList();
            if (targets.Count == 0) { PendingAll = false; Status = "No active, undefeated rivals to defeat."; return; }

            if (!PendingAll)
            {
                PendingAll = true;
                Status = "Defeat ALL " + targets.Count + " active rivals (" + string.Join(", ", targets.Select(t => t.Name)) +
                         ")? PERMANENT and cannot be undone. Press Confirm all.";
                return;
            }
            PendingAll = false;
            int ok = targets.Count(t => Defeat(t).Contains("(verified)"));
            Status = "Defeated " + ok + " of " + targets.Count + " rivals (verified by the game).";
        }

        private static string Defeat(RivalRow row)
        {
            try
            {
                RivalTimeline.DefeatRival(row.Neighbourhood, true);
                bool after = RivalsHelper.IsRivalDefeated(row.Id);
                string msg = after
                    ? row.Name + " defeated (verified). Their businesses close per the game's rules."
                    : row.Name + ": the game did not report the rival as defeated.";
                if (after) AmbitionControlMod.Log.Msg("Rival defeated (verified): " + row.Name + " [" + row.Id + "] in " + row.Neighbourhood);
                else AmbitionControlMod.Log.Warning("Rival defeat not confirmed: " + row.Name + " [" + row.Id + "]");
                return msg;
            }
            catch (Exception ex)
            {
                string msg = "Defeating " + row.Name + " failed: " + ex.GetBaseException().Message;
                AmbitionControlMod.Log.Error(msg);
                return msg;
            }
        }

        internal static void ClearPending()
        {
            PendingArea = null;
            PendingAll = false;
        }
    }
}
