using System;
using Il2Cpp;
using Il2CppBigAmbitions.DayNightCycle;
using Il2CppHelpers;
using Il2CppTimemachine;
using Object = UnityEngine.Object;

namespace AmbitionControl.Game
{
    /// <summary>
    /// Skip hours (Milestone H). Build-3233 interop evidence:
    ///   TimeMachine.StartTimeMachine(Timestamp goal) — the game's own fast-forward (used for sleeping/waiting); it plays
    ///   time forward quickly rather than jumping, so the normal simulation keeps running. TimeMachine.isRunning.
    ///   TimeHelper.Now() : Timestamp (a class); new Timestamp(float totalMinutes), GetTotalMinutes().
    ///   alpha.16: the goal is built as a NEW Timestamp from total minutes. alpha.11-15 called now.AddHours(h), and the
    ///   alpha.15 log ("Day 11 04:02 -> Day 11 04:02") showed that call changes the object it is called on, which may be
    ///   the game's live clock. The game's current time is now re-read after building the goal to prove it was untouched.
    /// Two-press confirm; on foot only. Readback: isRunning right after starting, and the time reached when it stops.
    /// </summary>
    internal static class TimeSkip
    {
        internal static readonly int[] Hours = { 1, 4, 8 };
        internal static string Status { get; private set; }
        internal static int PendingHours { get; private set; }

        private static TimeMachine cached;
        private static bool watching;
        private static string watchGoal;
        private static float watchGoalMinutes;
        private static float watchStartReal;

        private static TimeMachine Machine()
        {
            if (cached == null) cached = Object.FindObjectOfType<TimeMachine>();
            return cached;
        }

        internal static bool Running
        {
            get { try { TimeMachine tm = Machine(); return tm != null && tm.isRunning; } catch { return false; } }
        }

        internal static string Block(GameSnapshot snap, Settings settings)
        {
            string b = WorldTools.BlockReason(snap, settings);
            if (b != null) return b;
            try
            {
                if (Machine() == null) return "The game's time machine was not found.";
                if (Running) return "Time is already being skipped.";
                if (PlayerHelper.IsUsingVehicle) return "Get out of the vehicle first.";
            }
            catch (Exception ex) { return "Time skip unavailable: " + ex.GetBaseException().Message; }
            return null;
        }

        internal static void SkipPressed(int hours, GameSnapshot snap, Settings settings)
        {
            string block = Block(snap, settings);
            if (block != null) { PendingHours = 0; Status = "Time not skipped: " + block; return; }
            if (PendingHours != hours)
            {
                PendingHours = hours;
                Status = "Skip " + hours + " hour" + (hours == 1 ? "" : "s") + "? The game fast-forwards like sleeping: wages, rent, " +
                         "customers and your needs all run. Press Confirm.";
                return;
            }
            PendingHours = 0;
            try
            {
                Timestamp now = TimeHelper.Now();
                float nowMinutes = now.GetTotalMinutes();
                string nowText = GameText.Of(now);
                var goal = new Timestamp(nowMinutes + hours * 60f);
                float recheck = TimeHelper.Now().GetTotalMinutes();
                if (Math.Abs(recheck - nowMinutes) > 0.5f)
                {
                    Status = "Time skip refused: the game clock changed while preparing (" + nowMinutes + " -> " + recheck + " min).";
                    AmbitionControlMod.Log.Warning(Status);
                    return;
                }
                watchGoal = GameText.Of(goal);
                watchGoalMinutes = goal.GetTotalMinutes();
                watchStartReal = UnityEngine.Time.unscaledTime;
                Machine().StartTimeMachine(goal);
                if (Running)
                {
                    watching = true;
                    Status = "Skipping " + hours + "h to " + watchGoal + "...";
                    AmbitionControlMod.Log.Msg("Time skip started (verified running): " + nowText + " -> " + watchGoal + " (" + hours * 60 + " game minutes)");
                }
                else
                {
                    Status = "The game did not start the time skip.";
                    AmbitionControlMod.Log.Warning(Status);
                }
            }
            catch (Exception ex)
            {
                watching = false;
                Status = "Time skip failed: " + ex.GetBaseException().Message;
                AmbitionControlMod.Log.Error(Status);
            }
        }

        /// <summary>Called every frame: reports where the time machine stopped.</summary>
        internal static void Tick()
        {
            if (!watching) return;
            try
            {
                if (Running) return;
                watching = false;
                Timestamp end = TimeHelper.Now();
                float diff = watchGoalMinutes - end.GetTotalMinutes();
                bool reached = diff <= 1f;
                Status = reached ? "Time skipped (verified): now " + GameText.Of(end) + "." : "Time skip stopped early at " + GameText.Of(end) + " (goal " + watchGoal + ").";
                AmbitionControlMod.Log.Msg("Time skip finished: now " + GameText.Of(end) + ", goal " + watchGoal + (reached ? " (reached)" : " (stopped early)") +
                                           " after " + (UnityEngine.Time.unscaledTime - watchStartReal).ToString("0.0") + " s real time");
            }
            catch (Exception ex)
            {
                watching = false;
                Status = "Time skip check failed: " + ex.GetBaseException().Message;
            }
        }

        internal static void ClearPending() => PendingHours = 0;

        internal static void ResetScene()
        {
            cached = null;
            PendingHours = 0;
            watching = false;
        }
    }
}
