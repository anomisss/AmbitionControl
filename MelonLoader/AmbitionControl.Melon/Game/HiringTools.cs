using System;
using System.Collections.Generic;
using System.Linq;
using AmbitionControl.Core;
using Il2Cpp;
using Il2CppBigAmbitions.Characters.Skills;
using Il2CppEntities;
using Il2CppHelpers;

namespace AmbitionControl.Game
{
    /// <summary>
    /// Wages + candidates (Milestone H, Employees part 2). Build-3233 interop evidence:
    ///   EmployeeInstance.hourlyWage (float), GetPrimarySkill(), GetSkillValue(SkillName), IsCandidate, static MaxSkillValue
    ///   EmployeeHelper.CalculateHourlyWageForSkill(SkillName, float) — the game's own wage formula for a skill level
    ///   RecruitmentHelper.Command_GenerateCandidate(SkillName, int) — the game's own candidate generator
    ///   GameInstance.CandidateEmployeeInstances — readback (count grows by one)
    /// Market wages need a second press and show the payroll change first; each wage is read back.
    /// </summary>
    internal static class HiringTools
    {
        internal static string Status { get; private set; }
        internal static bool WagePending { get; private set; }

        internal static readonly SkillName[] Skills =
        {
            SkillName.CustomerService, SkillName.Cleaning, SkillName.PurchasingAgent, SkillName.LogisticsManager,
            SkillName.DeliveryDriver, SkillName.Programmer, SkillName.HRManager, SkillName.GraphicDesigner,
            SkillName.Lawyer, SkillName.Negotiation, SkillName.DJ, SkillName.HairStylist, SkillName.SecurityGuard,
            SkillName.Headhunter, SkillName.FactoryWorker, SkillName.GymTrainer
        };
        internal static readonly int[] LevelPercents = { 25, 50, 75, 100 };
        internal static int SkillIndex { get; private set; }
        internal static int LevelIndex { get; private set; } = 3;

        internal static string SkillLabel => Pretty(Skills[SkillIndex].ToString());
        internal static string LevelLabel => LevelPercents[LevelIndex] + "%";

        private static string Pretty(string s)
        {
            var sb = new System.Text.StringBuilder();
            foreach (char c in s) { if (char.IsUpper(c) && sb.Length > 0) sb.Append(' '); sb.Append(c); }
            return sb.ToString().Replace("H R", "HR").Replace("D J", "DJ");
        }

        internal static void NextSkill() { SkillIndex = (SkillIndex + 1) % Skills.Length; }
        internal static void NextLevel() { LevelIndex = (LevelIndex + 1) % LevelPercents.Length; }

        private static GameInstance SessionInstance(GameSnapshot snap)
        {
            GameInstance gi = SaveGameManager.Current;
            return gi != null && gi.Pointer == snap.InstancePtr ? gi : null;
        }

        private static List<EmployeeInstance> Hired(GameInstance gi)
        {
            var result = new List<EmployeeInstance>();
            if (gi.EmployeeInstances == null) return result;
            foreach (EmployeeInstance e in gi.EmployeeInstances)
                if (e != null && !e.IsCandidate) result.Add(e);
            return result;
        }

        private static float MarketWage(EmployeeInstance e)
        {
            SkillName primary = e.GetPrimarySkill();
            if (primary == SkillName.None) return -1f;
            return EmployeeHelper.CalculateHourlyWageForSkill(primary, e.GetSkillValue(primary));
        }

        internal static void MarketWagesPressed(GameSnapshot snap, Settings settings)
        {
            string block = EmployeeTools.BlockReason(snap, settings);
            if (block != null) { WagePending = false; Status = "Wages not changed: " + block; return; }
            try
            {
                GameInstance gi = SessionInstance(snap);
                if (gi == null) { WagePending = false; Status = "Wages not changed: the save changed."; return; }
                List<EmployeeInstance> staff = Hired(gi);
                if (staff.Count == 0) { WagePending = false; Status = "You have no hired employees."; return; }

                var plan = new List<KeyValuePair<EmployeeInstance, float>>();
                foreach (EmployeeInstance e in staff)
                {
                    float w = MarketWage(e);
                    if (w > 0f && w < 10000f && Math.Abs(w - e.hourlyWage) >= 0.01f) plan.Add(new KeyValuePair<EmployeeInstance, float>(e, w));
                }
                if (plan.Count == 0) { WagePending = false; Status = "All wages already match the game's market rate."; return; }
                float before = staff.Sum(e => e.hourlyWage);
                float after = before + plan.Sum(p => p.Value - p.Key.hourlyWage);

                if (!WagePending)
                {
                    WagePending = true;
                    Status = "Set " + plan.Count + " of " + staff.Count + " wages to the game's market rate? Payroll $" +
                             before.ToString("N0") + "/h -> $" + after.ToString("N0") + "/h. May change satisfaction. Press Confirm.";
                    return;
                }
                WagePending = false;
                int ok = 0;
                foreach (var p in plan)
                {
                    p.Key.hourlyWage = p.Value;
                    if (Math.Abs(p.Key.hourlyWage - p.Value) < 0.01f) ok++;
                }
                float now = staff.Sum(e => e.hourlyWage);
                Status = "Wages set to market rate: " + ok + "/" + plan.Count + " verified. Payroll $" + before.ToString("N0") +
                         "/h -> $" + now.ToString("N0") + "/h.";
                AmbitionControlMod.Log.Msg("Market wages: " + ok + "/" + plan.Count + " verified; payroll " + before.ToString("0.##") +
                                           " -> " + now.ToString("0.##") + " per hour");
            }
            catch (Exception ex)
            {
                WagePending = false;
                Status = "Wage change failed: " + ex.GetBaseException().Message;
                AmbitionControlMod.Log.Error(Status);
            }
        }

        /// <summary>Market wage for the UI; -1 when the game can't give a sane value.</summary>
        internal static float TryMarketWage(EmployeeInstance e)
        {
            try { float w = MarketWage(e); return CheatMath.IsUsableMarketWage(w) ? w : -1f; }
            catch { return -1f; }
        }

        private static EmployeeInstance FindHired(GameInstance gi, string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            foreach (EmployeeInstance e in Hired(gi))
                if (e.id == id) return e;
            return null;
        }

        /// <summary>
        /// Individual wage edit (Milestone K): one step (-10%, -$1, +$1, +10%) or the game's market rate, for one
        /// employee. Bounded to $1-$500/h (CheatMath), single press (small, reversible), read back.
        /// </summary>
        internal static void AdjustWage(string id, WageStep? step, GameSnapshot snap, Settings settings)
        {
            WagePending = false;
            string block = EmployeeTools.BlockReason(snap, settings);
            if (block != null) { Status = "Wage not changed: " + block; return; }
            try
            {
                GameInstance gi = SessionInstance(snap);
                if (gi == null) { Status = "Wage not changed: the save changed."; return; }
                EmployeeInstance e = FindHired(gi, id);
                if (e == null) { Status = "Wage not changed: select an employee in the list first."; return; }
                string name = StripTags(e.GetEmployeeNameWithInfo());
                float before = e.hourlyWage;
                float target;
                if (step.HasValue)
                {
                    if (!CheatMath.TryStepWage(before, step.Value, out target, out string err)) { Status = "Wage not changed for " + name + ": " + err; return; }
                }
                else
                {
                    target = TryMarketWage(e);
                    if (target < 0f) { Status = "Wage not changed for " + name + ": the game gave no usable market rate."; return; }
                    if (Math.Abs(target - before) < 0.01f) { Status = name + " already earns the market rate ($" + before.ToString("0.00") + "/h)."; return; }
                }
                e.hourlyWage = target;
                float after = e.hourlyWage;
                bool ok = Math.Abs(after - target) < 0.01f;
                Status = ok
                    ? "Wage (verified): " + name + " $" + before.ToString("0.00") + " -> $" + after.ToString("0.00") + "/h."
                    : "Wage for " + name + " did not stick (read back $" + after.ToString("0.00") + ").";
                AmbitionControlMod.Log.Msg("Wage " + (step.HasValue ? step.Value.ToString() : "MarketRate") + ": " + name + " " +
                                           before.ToString("0.00") + " -> " + after.ToString("0.00") + (ok ? " (verified)" : " (NOT as expected)"));
            }
            catch (Exception ex)
            {
                Status = "Wage change failed: " + ex.GetBaseException().Message;
                AmbitionControlMod.Log.Error(Status);
            }
        }

        internal static void GenerateCandidate(GameSnapshot snap, Settings settings)
        {
            WagePending = false;
            string block = EmployeeTools.BlockReason(snap, settings);
            if (block != null) { Status = "No candidate generated: " + block; return; }
            try
            {
                GameInstance gi = SessionInstance(snap);
                if (gi == null) { Status = "No candidate generated: the save changed."; return; }
                float max = EmployeeInstance.MaxSkillValue;
                if (max <= 0f || max > 1000f) { Status = "No candidate generated: unexpected skill scale (" + max + ")."; return; }
                int value = (int)Math.Round(max * LevelPercents[LevelIndex] / 100f);
                SkillName skill = Skills[SkillIndex];
                int before = gi.CandidateEmployeeInstances != null ? gi.CandidateEmployeeInstances.Count : 0;
                RecruitmentHelper.Command_GenerateCandidate(skill, value);
                int after = gi.CandidateEmployeeInstances != null ? gi.CandidateEmployeeInstances.Count : 0;
                if (after > before)
                {
                    EmployeeInstance c = gi.CandidateEmployeeInstances[after - 1];
                    string name = c != null ? StripTags(c.GetEmployeeNameWithInfo()) : "candidate";
                    Status = "Candidate generated (verified): " + name + " — " + SkillLabel + " " + LevelLabel +
                             ". Find them in the MyEmployees app.";
                    AmbitionControlMod.Log.Msg("Candidate generated (verified): " + name + ", " + skill + " " + value + "/" + max +
                                               "; candidates " + before + " -> " + after);
                }
                else
                {
                    Status = "The game did not add a candidate (candidates " + before + " -> " + after + ").";
                    AmbitionControlMod.Log.Warning(Status);
                }
            }
            catch (Exception ex)
            {
                Status = "Generating a candidate failed: " + ex.GetBaseException().Message;
                AmbitionControlMod.Log.Error(Status);
            }
        }

        private static string StripTags(string s) => s == null ? "" : System.Text.RegularExpressions.Regex.Replace(s, "<.*?>", "");

        internal static void ClearPending() => WagePending = false;
    }
}
