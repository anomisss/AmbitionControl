using System;
using System.Collections.Generic;
using System.Reflection;
using Il2Cpp;
using Il2CppHelpers;
using UnityEngine;

namespace AmbitionControl.Game
{
    /// <summary>
    /// Hold customer satisfaction at 100% (Milestone K, Business tab). Build-3233 interop evidence:
    ///   BuildingRegistration.satisfaction : Satisfaction {overall, customerService, pricing, cleanliness, facility} (int)
    ///   BusinessHelper.UpdateSatisfaction(BuildingRegistration) — the game recalculates satisfaction from order history,
    ///   so a one-off write would be overwritten. While the hold is on, a Harmony postfix on UpdateSatisfaction re-applies
    ///   100 right after each recalculation, and a 2-second fallback re-applies it if the hook is ever bypassed.
    /// Session-only toggle (off after a scene change or save switch); only the player's rented buildings are touched.
    /// When turned off, the game's next recalculation restores the real value. Readback after every write.
    /// </summary>
    internal static class SatisfactionHold
    {
        internal const int Max = 100;
        internal static bool On { get; private set; }
        internal static string Status { get; private set; }
        internal static bool Hooked { get; private set; }

        private static int hookHits, fallbackFixes;
        private static float nextFallback;
        private static IntPtr sessionPtr;

        private static HarmonyLib.Harmony harmonyRef;
        private static bool hookAttempted;

        /// <summary>Remember Harmony; the hook itself is installed the first time the hold is turned on.</summary>
        internal static void Apply(HarmonyLib.Harmony harmony) => harmonyRef = harmony;

        private static void EnsureHooked()
        {
            if (hookAttempted) return;
            hookAttempted = true;
            HarmonyLib.Harmony harmony = harmonyRef;
            try
            {
                MethodInfo target = typeof(BusinessHelper).GetMethod(nameof(BusinessHelper.UpdateSatisfaction),
                    BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(BuildingRegistration) }, null);
                if (target == null) throw new MissingMethodException("BusinessHelper", "UpdateSatisfaction");
                harmony.Patch(target, postfix: new HarmonyLib.HarmonyMethod(typeof(SatisfactionHold).GetMethod(nameof(Postfix),
                    BindingFlags.NonPublic | BindingFlags.Static)));
                Hooked = true;
                AmbitionControlMod.Log.Msg("Satisfaction hold: hook on BusinessHelper.UpdateSatisfaction installed.");
            }
            catch (Exception ex)
            {
                AmbitionControlMod.Log.Warning("Satisfaction hold: could not hook BusinessHelper.UpdateSatisfaction (" +
                                               ex.GetBaseException().Message + "); the 2-second fallback will be used.");
            }
        }

        private static bool Eligible(BuildingRegistration r) => r != null && r.RentedByPlayer && r.satisfaction != null;

        private static List<BuildingRegistration> Targets(GameInstance gi)
        {
            var list = new List<BuildingRegistration>();
            if (gi?.BuildingRegistrations == null) return list;
            foreach (BuildingRegistration r in gi.BuildingRegistrations)
                if (Eligible(r)) list.Add(r);
            return list;
        }

        /// <summary>Set all five values to 100; returns true if anything changed. Caller reads back.</summary>
        private static bool Fill(Satisfaction s)
        {
            bool changed = s.overall != Max || s.customerService != Max || s.pricing != Max || s.cleanliness != Max || s.facility != Max;
            if (!changed) return false;
            s.overall = Max; s.customerService = Max; s.pricing = Max; s.cleanliness = Max; s.facility = Max;
            return true;
        }

        private static bool IsFull(Satisfaction s) =>
            s.overall == Max && s.customerService == Max && s.pricing == Max && s.cleanliness == Max && s.facility == Max;

        internal static void Toggle(GameSnapshot snap, Settings settings)
        {
            string block = BusinessTools.BlockReason(snap, settings);
            if (block != null) { Status = "Satisfaction hold not changed: " + block; return; }
            if (On)
            {
                On = false;
                Status = "Satisfaction hold OFF. The game recalculates real satisfaction at its next update.";
                AmbitionControlMod.Log.Msg("Satisfaction hold OFF (hook re-applied " + hookHits + "x, fallback " + fallbackFixes + "x).");
                return;
            }
            try
            {
                GameInstance gi = SaveGameManager.Current;
                if (gi == null || gi.Pointer != snap.InstancePtr) { Status = "Satisfaction hold not started: the save changed."; return; }
                List<BuildingRegistration> targets = Targets(gi);
                if (targets.Count == 0) { Status = "Satisfaction hold not started: you don't rent any business buildings."; return; }
                // Scale check: the game's values must already be on a 0-100 scale before we write 100.
                foreach (BuildingRegistration r in targets)
                {
                    Satisfaction s = r.satisfaction;
                    int hi = Math.Max(Math.Max(s.overall, s.customerService), Math.Max(Math.Max(s.pricing, s.cleanliness), s.facility));
                    int lo = Math.Min(Math.Min(s.overall, s.customerService), Math.Min(Math.Min(s.pricing, s.cleanliness), s.facility));
                    if (hi > Max || lo < 0)
                    {
                        Status = "Satisfaction hold not started: unexpected satisfaction scale (" + lo + ".." + hi + ").";
                        AmbitionControlMod.Log.Warning(Status);
                        return;
                    }
                }
                EnsureHooked();
                int ok = 0;
                foreach (BuildingRegistration r in targets)
                {
                    Fill(r.satisfaction);
                    if (IsFull(r.satisfaction)) ok++;
                }
                On = true;
                sessionPtr = gi.Pointer;
                hookHits = fallbackFixes = 0;
                Status = "Satisfaction hold ON: " + ok + "/" + targets.Count + " businesses at 100% (verified). This session only" +
                         (Hooked ? "." : "; hook unavailable, re-applied every 2 seconds.");
                AmbitionControlMod.Log.Msg("Satisfaction hold ON: " + ok + "/" + targets.Count + " verified; hook " + (Hooked ? "active" : "unavailable"));
            }
            catch (Exception ex)
            {
                On = false;
                Status = "Satisfaction hold failed: " + ex.GetBaseException().Message;
                AmbitionControlMod.Log.Error(Status);
            }
        }

        // Harmony postfix: runs after the game recalculated one building's satisfaction.
        private static void Postfix(BuildingRegistration __0)
        {
            if (!On) return;
            try
            {
                BuildingRegistration registration = __0;
                if (!Eligible(registration)) return;
                GameInstance gi = SaveGameManager.Current;
                if (gi == null || gi.Pointer != sessionPtr || SaveGameManager.SavingGameInProgress) return;
                if (Fill(registration.satisfaction))
                {
                    hookHits++;
                    if (hookHits == 1) AmbitionControlMod.Log.Msg("Satisfaction hold: game recalculated a business; hook re-applied 100% (verified: " + IsFull(registration.satisfaction) + ").");
                }
            }
            catch { /* never break the game's update */ }
        }

        /// <summary>Every frame: 2-second fallback re-apply, and stop if the save changed.</summary>
        internal static void Tick(GameSnapshot snap, Settings settings)
        {
            if (!On || Time.unscaledTime < nextFallback) return;
            nextFallback = Time.unscaledTime + 2f;
            try
            {
                GameInstance gi = SaveGameManager.Current;
                if (gi == null || gi.Pointer != sessionPtr || BusinessTools.BlockReason(snap, settings) != null)
                {
                    if (gi == null || gi.Pointer != sessionPtr) ResetSession();
                    return;
                }
                foreach (BuildingRegistration r in Targets(gi))
                    if (Fill(r.satisfaction))
                    {
                        fallbackFixes++;
                        if (fallbackFixes == 1) AmbitionControlMod.Log.Msg("Satisfaction hold: fallback re-applied 100% (the game changed it outside the hook).");
                    }
            }
            catch (Exception ex)
            {
                On = false;
                Status = "Satisfaction hold stopped: " + ex.GetBaseException().Message;
                AmbitionControlMod.Log.Error(Status);
            }
        }

        internal static void ResetSession()
        {
            if (On) AmbitionControlMod.Log.Msg("Satisfaction hold OFF (scene/save changed; hook " + hookHits + "x, fallback " + fallbackFixes + "x).");
            On = false;
            sessionPtr = IntPtr.Zero;
        }
    }
}
