using System;
using System.Collections.Generic;
using System.Linq;
using Il2Cpp;
using Il2CppEntities;
using Il2CppHelpers;

namespace AmbitionControl.Game
{
    internal sealed class EmployeeRow
    {
        internal string Name;
        internal float Wage;
        internal float Satisfaction;
        internal bool Absent;
        internal bool Assigned;
    }

    /// <summary>
    /// Employees (Milestone E). Build-3233 interop evidence:
    ///   GameInstance.EmployeeInstances (hired staff; candidates are a separate list),
    ///   EmployeeInstance.GetEmployeeNameWithInfo(), .hourlyWage, .satisfaction, .isAbsent, .IsAssignedToAnyBusiness(),
    ///   EmployeeInstance.UpdateSatisfaction() — the game recalculates satisfaction itself, so a direct write is temporary.
    ///   EmployeeHelper.LowSatisfactionThreshold (int) — used with observed values to confirm the 0–100 scale.
    /// The list is read-only. "Max satisfaction" needs a confirmed scale, shows the affected count first, and reads back.
    /// </summary>
    internal static class EmployeeTools
    {
        internal const int PageSize = 7;
        internal static string Status { get; private set; }

        private static bool confirmPending;
        private static bool loggedScale;
        internal static bool ConfirmPending => confirmPending;

        internal static string BlockReason(GameSnapshot snap, Settings settings)
        {
            if (!settings.EmployeeToolsEnabled) return "Employee tools are turned off in preferences (EmployeeToolsEnabled).";
            int? build = GameBridge.BuildNumber;
            if (!build.HasValue) return "Game build not detected yet.";
            if (build.Value != GameBridge.TargetBuild && !settings.AllowOtherBuilds)
                return "Game build " + build.Value + " is not the verified build " + GameBridge.TargetBuild + ".";
            if (snap == null || !snap.Loaded) return snap?.NotLoadedReason ?? "No save loaded.";
            if (snap.Saving) return "Game is saving; wait a moment.";
            return null;
        }

        private static GameInstance SessionInstance(GameSnapshot snap)
        {
            GameInstance gi = SaveGameManager.Current;
            return gi != null && gi.Pointer == snap.InstancePtr ? gi : null;
        }

        private static List<EmployeeInstance> Hired(GameInstance gi)
        {
            var result = new List<EmployeeInstance>();
            var list = gi.EmployeeInstances;
            if (list == null) return result;
            foreach (EmployeeInstance e in list)
                if (e != null && !e.IsCandidate) result.Add(e);
            return result;
        }

        /// <summary>Read-only rows for the UI. Called on demand (not every frame).</summary>
        internal static List<EmployeeRow> ReadRows(GameSnapshot snap, out string error)
        {
            error = null;
            var rows = new List<EmployeeRow>();
            try
            {
                GameInstance gi = SessionInstance(snap);
                if (gi == null) { error = "No save loaded."; return rows; }
                foreach (EmployeeInstance e in Hired(gi))
                {
                    string name;
                    try { name = e.GetEmployeeNameWithInfo(); } catch { name = e.id; }
                    bool assigned;
                    try { assigned = e.IsAssignedToAnyBusiness(); } catch { assigned = false; }
                    rows.Add(new EmployeeRow
                    {
                        Name = StripTags(name), Wage = e.hourlyWage, Satisfaction = e.satisfaction,
                        Absent = e.isAbsent, Assigned = assigned
                    });
                }
                LogScaleOnce(rows);
            }
            catch (Exception ex)
            {
                error = "Could not read employees: " + ex.GetBaseException().Message;
            }
            return rows;
        }

        private static string StripTags(string s)
        {
            if (string.IsNullOrEmpty(s)) return "?";
            var sb = new System.Text.StringBuilder(s.Length);
            bool inTag = false;
            foreach (char c in s)
            {
                if (c == '<') inTag = true;
                else if (c == '>') inTag = false;
                else if (!inTag) sb.Append(c);
            }
            return sb.ToString().Trim();
        }

        private static void LogScaleOnce(List<EmployeeRow> rows)
        {
            if (loggedScale || rows.Count == 0) return;
            loggedScale = true;
            int threshold = -1;
            try { threshold = EmployeeHelper.LowSatisfactionThreshold; } catch { }
            AmbitionControlMod.Log.Msg("Employees: " + rows.Count + " hired; satisfaction min " +
                rows.Min(r => r.Satisfaction).ToString("0.###") + ", max " + rows.Max(r => r.Satisfaction).ToString("0.###") +
                "; game LowSatisfactionThreshold " + threshold + ".");
        }

        /// <summary>Scale is confirmed as 0–100 only if the game's threshold is above 1 and no observed value exceeds 100.</summary>
        private static bool TryScale(List<EmployeeInstance> staff, out float max, out string reason)
        {
            max = 0f;
            int threshold;
            try { threshold = EmployeeHelper.LowSatisfactionThreshold; }
            catch (Exception ex) { reason = "satisfaction scale unknown (" + ex.GetBaseException().Message + ")."; return false; }
            float observedMax = staff.Count == 0 ? 0f : staff.Max(e => e.satisfaction);
            if (threshold > 1 && threshold < 100 && observedMax <= 100.0001f) { max = 100f; reason = null; return true; }
            reason = "satisfaction scale could not be confirmed (threshold " + threshold + ", highest value " + observedMax.ToString("0.###") + ").";
            return false;
        }

        /// <summary>First press previews the affected count; second press sets satisfaction to max and reads back.</summary>
        internal static void MaxSatisfaction(GameSnapshot snap, Settings settings)
        {
            string block = BlockReason(snap, settings);
            if (block != null) { confirmPending = false; Status = "Satisfaction not changed: " + block; return; }
            try
            {
                GameInstance gi = SessionInstance(snap);
                if (gi == null) { confirmPending = false; Status = "Satisfaction not changed: the save changed."; return; }
                List<EmployeeInstance> staff = Hired(gi);
                if (staff.Count == 0) { confirmPending = false; Status = "You have no hired employees."; return; }
                if (!TryScale(staff, out float max, out string reason)) { confirmPending = false; Status = "Satisfaction not changed: " + reason; return; }

                int affected = staff.Count(e => e.satisfaction < max - 0.01f);
                if (!confirmPending)
                {
                    confirmPending = true;
                    Status = "Set satisfaction to " + max.ToString("0") + " for " + affected + " of " + staff.Count +
                             " employees? Temporary: the game recalculates satisfaction from wages and conditions. Press Confirm.";
                    return;
                }
                confirmPending = false;
                int ok = 0;
                foreach (EmployeeInstance e in staff)
                {
                    e.satisfaction = max;
                    if (Math.Abs(e.satisfaction - max) < 0.01f) ok++;
                }
                Status = "Satisfaction set to " + max.ToString("0") + " for " + ok + "/" + staff.Count + " employees (verified). " +
                         "Temporary: the game will recalculate it.";
                AmbitionControlMod.Log.Msg("Employee satisfaction set to " + max + ": " + ok + "/" + staff.Count + " verified (" + affected + " were below max).");
            }
            catch (Exception ex)
            {
                confirmPending = false;
                Status = "Satisfaction change failed: " + ex.GetBaseException().Message;
                AmbitionControlMod.Log.Error(Status);
            }
        }

        internal static void ClearPending() => confirmPending = false;

        internal static void ResetSession()
        {
            confirmPending = false;
            loggedScale = false;
        }
    }
}
