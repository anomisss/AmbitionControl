using System;
using Il2Cpp;
using Il2CppHelpers;
using UnityEngine;

namespace AmbitionControl.Game
{
    internal enum Need { Energy, Hunger, Happiness }

    /// <summary>
    /// Player utilities (Milestone B). All effects are runtime game state that the game itself manages; nothing here
    /// changes save options. APIs (build-3233 interop):
    ///   EnergyHelper.SetCurrentEnergy(float), EnergyHelper.SetCurrentHunger(float)   — the game's own need setters
    ///   HappinessHelper.Happiness (static get/set)
    ///   EnergyHelper.Command_ToggleInvincibility() + static EnergyHelper._invincibility (readback)
    /// Readback uses GameInstance.Energy/Hunger/Happiness, the same values the HUD shows (owner-confirmed 0–100 scale).
    /// Hunger has no "disable" option in build 3233, so "hold hunger full" is a session-only top-up (max once/second).
    /// </summary>
    internal static class PlayerTools
    {
        internal const float Full = 100f;
        private const float HoldThreshold = 99f;
        private const float HoldInterval = 1f;

        internal static string Status { get; private set; }
        internal static bool HoldHunger { get; private set; }

        private static float nextHold;
        private static int holdTopUps;

        internal static string BlockReason(GameSnapshot snap, Settings settings)
        {
            if (!settings.PlayerToolsEnabled) return "Player tools are turned off in preferences (PlayerToolsEnabled).";
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

        private static float Read(GameInstance gi, Need need) =>
            need == Need.Energy ? gi.Energy : need == Need.Hunger ? gi.Hunger : gi.Happiness;

        private static void Write(Need need, float value)
        {
            switch (need)
            {
                case Need.Energy: EnergyHelper.SetCurrentEnergy(value); break;
                case Need.Hunger: EnergyHelper.SetCurrentHunger(value); break;
                default: HappinessHelper.Happiness = value; break;
            }
        }

        /// <summary>Refill one need to full and verify by readback. Returns true if verified.</summary>
        internal static bool Refill(Need need, GameSnapshot snap, Settings settings, bool quiet = false)
        {
            string block = BlockReason(snap, settings);
            if (block != null) { Status = need + " not refilled: " + block; return false; }
            try
            {
                GameInstance gi = SessionInstance(snap);
                if (gi == null) { Status = need + " not refilled: the save changed. Try again."; return false; }

                float before = Read(gi, need);
                Write(need, Full);
                float after = Read(gi, need);
                bool ok = Math.Abs(after - Full) < 0.5f;
                string line = need + " " + before.ToString("0.#") + " -> " + after.ToString("0.#");
                if (ok)
                {
                    if (!quiet) Status = need + " refilled (verified): " + before.ToString("0.#") + " -> " + after.ToString("0.#") + ".";
                    AmbitionControlMod.Log.Msg("Refill verified: " + line);
                }
                else
                {
                    Status = need + " refill did not reach " + Full + " (" + line + ").";
                    AmbitionControlMod.Log.Warning("Refill mismatch: " + line);
                }
                return ok;
            }
            catch (Exception ex)
            {
                Status = need + " refill failed: " + ex.GetBaseException().Message;
                AmbitionControlMod.Log.Error(Status);
                return false;
            }
        }

        internal static void RefillAll(GameSnapshot snap, Settings settings)
        {
            int ok = 0;
            foreach (Need n in new[] { Need.Energy, Need.Hunger, Need.Happiness })
                if (Refill(n, snap, settings, quiet: true)) ok++;
            if (ok == 3) Status = "All needs refilled (verified).";
            else Status = ok + " of 3 needs refilled. " + Status;
        }

        internal static bool ReadInvincible()
        {
            try { return EnergyHelper._invincibility; } catch { return false; }
        }

        internal static void ToggleInvincibility(GameSnapshot snap, Settings settings)
        {
            string block = BlockReason(snap, settings);
            if (block != null) { Status = "Invincibility not changed: " + block; return; }
            try
            {
                bool before = EnergyHelper._invincibility;
                EnergyHelper.Command_ToggleInvincibility();
                bool after = EnergyHelper._invincibility;
                if (after != before)
                {
                    snap.Invincible = after;
                    Status = "Invincibility " + (after ? "ON" : "OFF") + " (verified). Runtime only; not saved.";
                    AmbitionControlMod.Log.Msg("Invincibility " + (after ? "ON" : "OFF") + " verified.");
                }
                else
                {
                    Status = "Invincibility did not change (still " + (after ? "ON" : "OFF") + ").";
                    AmbitionControlMod.Log.Warning(Status);
                }
            }
            catch (Exception ex)
            {
                Status = "Invincibility toggle failed: " + ex.GetBaseException().Message;
                AmbitionControlMod.Log.Error(Status);
            }
        }

        internal static void SetHoldHunger(bool on, GameSnapshot snap, Settings settings)
        {
            if (on)
            {
                string block = BlockReason(snap, settings);
                if (block != null) { Status = "Hold hunger not enabled: " + block; return; }
            }
            HoldHunger = on;
            holdTopUps = 0;
            nextHold = 0f;
            Status = on
                ? "Hold hunger full ON (experimental, this session only): hunger is topped up when it drops."
                : "Hold hunger full OFF.";
            AmbitionControlMod.Log.Msg("Hold hunger full " + (on ? "ON" : "OFF") + ".");
        }

        /// <summary>Turn session-only effects off when the save closes or changes.</summary>
        internal static void ResetSession()
        {
            if (HoldHunger) AmbitionControlMod.Log.Msg("Hold hunger full OFF (save changed; " + holdTopUps + " top-ups this session).");
            HoldHunger = false;
            holdTopUps = 0;
        }

        /// <summary>Main-thread step from OnUpdate; rate-limited to one check per second.</summary>
        internal static void Tick(GameSnapshot snap, Settings settings)
        {
            if (!HoldHunger || Time.unscaledTime < nextHold) return;
            nextHold = Time.unscaledTime + HoldInterval;
            if (BlockReason(snap, settings) != null) return;
            try
            {
                GameInstance gi = SessionInstance(snap);
                if (gi == null || gi.Hunger >= HoldThreshold) return;
                EnergyHelper.SetCurrentHunger(Full);
                holdTopUps++;
            }
            catch (Exception ex)
            {
                HoldHunger = false;
                Status = "Hold hunger stopped after an error: " + ex.GetBaseException().Message;
                AmbitionControlMod.Log.Error(Status);
            }
        }
    }
}
