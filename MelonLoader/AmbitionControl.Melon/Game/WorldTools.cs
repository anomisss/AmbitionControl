using System;
using System.Linq;
using Il2Cpp;
using Il2CppHelpers;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AmbitionControl.Game
{
    /// <summary>
    /// Vehicle and world tools (Milestone C). Build-3233 interop evidence:
    ///   Vehicles: VehicleHelper.IsInsideMotorVehicle(), GetCurrentVehicle() : VehicleInstance,
    ///             GetCurrentVehicleBase() : VehicleController, VehicleHelper.RepairVehicle(),
    ///             VehicleController.SetFuel(float), VehicleInstance.fuel/damage, VehicleType.maxFuel,
    ///             GameInstance.VehicleInstances (the player's owned vehicles).
    ///   Pause:    GameSpeedController.SetPause(bool, bool), .Paused (game's own pause system).
    ///   Speed:    TimeHelper.Command_SetTimeSpeed(int percent) (game debug command; experimental), readback via
    ///             UnityEngine.Time.timeScale. The game's normal speeds are only Normal and FastForward.
    ///   Traffic:  GameManager.Command_ToggleTraffic(), readback GameManager.spawnTraffic.
    ///   Save:     SaveGameManager.CanSave(), GameManager.SaveGame(string, bool) using the loaded save's name.
    /// Vehicle tools act only on the vehicle the player is in, and only if it is one of the player's own vehicles.
    /// </summary>
    internal static class WorldTools
    {
        internal static readonly int[] SpeedPercents = { 100, 200, 300, 500, 1000 };

        internal static string Status { get; private set; }
        private static bool savePending;
        internal static bool SavePending => savePending;

        // Cached scene objects (FindObjectOfType scans the scene; snapshots run 4x per second).
        private static GameSpeedController cachedSpeed;
        private static GameManager cachedManager;

        private static GameSpeedController SpeedController()
        {
            if (cachedSpeed == null) cachedSpeed = Object.FindObjectOfType<GameSpeedController>();
            return cachedSpeed;
        }

        internal static GameManager Manager()
        {
            if (cachedManager == null) cachedManager = Object.FindObjectOfType<GameManager>();
            return cachedManager;
        }

        /// <summary>Drop cached scene objects and pending confirmations (scene change).</summary>
        internal static void ResetScene()
        {
            cachedSpeed = null;
            cachedManager = null;
            savePending = false;
        }

        internal static string BlockReason(GameSnapshot snap, Settings settings)
        {
            if (!settings.WorldToolsEnabled) return "World tools are turned off in preferences (WorldToolsEnabled).";
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

        /// <summary>Read-only state for the UI (called from GameBridge.Capture; never throws).</summary>
        internal static void ReadState(GameSnapshot snap, GameInstance gi)
        {
            try
            {
                snap.InVehicle = false;
                if (VehicleHelper.IsInsideMotorVehicle())
                {
                    VehicleInstance vi = VehicleHelper.GetCurrentVehicle();
                    if (vi != null)
                    {
                        snap.InVehicle = true;
                        snap.VehicleOwned = IsOwned(gi, vi);
                        snap.VehicleFuel = vi.fuel;
                        snap.VehicleMaxFuel = vi.VehicleType != null ? vi.VehicleType.maxFuel : 0f;
                        snap.VehicleDamage = vi.damage;
                        snap.VehicleDirt = vi.dirtiness;
                    }
                }
            }
            catch { snap.InVehicle = false; }

            try
            {
                GameSpeedController gsc = SpeedController();
                snap.PauseKnown = gsc != null;
                snap.Paused = gsc != null && gsc.Paused;
            }
            catch { snap.PauseKnown = false; }

            try
            {
                GameManager gm = Manager();
                snap.TrafficKnown = gm != null;
                snap.Traffic = gm != null && gm.spawnTraffic;
            }
            catch { snap.TrafficKnown = false; }

            snap.TimeScale = Time.timeScale;
        }

        private static bool IsOwned(GameInstance gi, VehicleInstance vi)
        {
            var owned = gi.VehicleInstances;
            if (owned == null || string.IsNullOrEmpty(vi.id)) return false;
            foreach (VehicleInstance v in owned)
                if (v != null && v.id == vi.id) return true;
            return false;
        }

        private static bool TryOwnedVehicle(GameSnapshot snap, out VehicleInstance vi, out VehicleController vc, out string reason)
        {
            vi = null; vc = null;
            GameInstance gi = SessionInstance(snap);
            if (gi == null) { reason = "the save changed. Try again."; return false; }
            if (!VehicleHelper.IsInsideMotorVehicle()) { reason = "get into one of your own vehicles first."; return false; }
            vi = VehicleHelper.GetCurrentVehicle();
            vc = VehicleHelper.GetCurrentVehicleBase();
            if (vi == null || vc == null) { reason = "no current vehicle found."; return false; }
            if (!IsOwned(gi, vi)) { reason = "this vehicle is not one of your own vehicles."; return false; }
            reason = null;
            return true;
        }

        internal static void RepairVehicle(GameSnapshot snap, Settings settings)
        {
            savePending = false;
            string block = BlockReason(snap, settings);
            if (block != null) { Status = "Vehicle not repaired: " + block; return; }
            try
            {
                if (!TryOwnedVehicle(snap, out VehicleInstance vi, out _, out string reason)) { Status = "Vehicle not repaired: " + reason; return; }
                float before = vi.damage;
                VehicleHelper.RepairVehicle();
                float after = vi.damage;
                snap.VehicleDamage = after;
                if (after <= 0.001f)
                {
                    Status = "Vehicle repaired (verified): damage " + before.ToString("0.##") + " -> " + after.ToString("0.##") + ". No repair cost charged.";
                    AmbitionControlMod.Log.Msg("Vehicle repair verified: damage " + before.ToString("0.###") + " -> " + after.ToString("0.###"));
                }
                else
                {
                    Status = "Repair did not clear damage (" + before.ToString("0.##") + " -> " + after.ToString("0.##") + ").";
                    AmbitionControlMod.Log.Warning(Status);
                }
            }
            catch (Exception ex)
            {
                Status = "Vehicle repair failed: " + ex.GetBaseException().Message;
                AmbitionControlMod.Log.Error(Status);
            }
        }

        internal static void RefuelVehicle(GameSnapshot snap, Settings settings)
        {
            savePending = false;
            string block = BlockReason(snap, settings);
            if (block != null) { Status = "Vehicle not refuelled: " + block; return; }
            try
            {
                if (!TryOwnedVehicle(snap, out VehicleInstance vi, out VehicleController vc, out string reason)) { Status = "Vehicle not refuelled: " + reason; return; }
                float max = vi.VehicleType != null ? vi.VehicleType.maxFuel : 0f;
                if (max <= 0f) { Status = "Vehicle not refuelled: this vehicle has no fuel tank."; return; }
                float before = vi.fuel;
                vc.SetFuel(max);
                float after = vi.fuel;
                snap.VehicleFuel = after;
                if (Math.Abs(after - max) < 0.01f * Math.Max(1f, max))
                {
                    Status = "Vehicle refuelled (verified): fuel " + before.ToString("0.#") + " -> " + after.ToString("0.#") + " of " + max.ToString("0.#") + ". No cost charged.";
                    AmbitionControlMod.Log.Msg("Vehicle refuel verified: fuel " + before.ToString("0.##") + " -> " + after.ToString("0.##") + " (max " + max.ToString("0.##") + ")");
                }
                else
                {
                    Status = "Refuel did not fill the tank (" + before.ToString("0.#") + " -> " + after.ToString("0.#") + " of " + max.ToString("0.#") + ").";
                    AmbitionControlMod.Log.Warning(Status);
                }
            }
            catch (Exception ex)
            {
                Status = "Vehicle refuel failed: " + ex.GetBaseException().Message;
                AmbitionControlMod.Log.Error(Status);
            }
        }

        /// <summary>Clean: VehicleController.SetDirtiness(0) (the game's own setter), readback VehicleInstance.dirtiness.</summary>
        internal static void CleanVehicle(GameSnapshot snap, Settings settings)
        {
            savePending = false;
            string block = BlockReason(snap, settings);
            if (block != null) { Status = "Vehicle not cleaned: " + block; return; }
            try
            {
                if (!TryOwnedVehicle(snap, out VehicleInstance vi, out VehicleController vc, out string reason)) { Status = "Vehicle not cleaned: " + reason; return; }
                if (vi.VehicleType != null && !vi.VehicleType.canGetDirty) { Status = "This vehicle type never gets dirty."; return; }
                float before = vi.dirtiness;
                vc.SetDirtiness(0f);
                float after = vi.dirtiness;
                snap.VehicleDirt = after;
                if (after <= 0.001f)
                {
                    Status = "Vehicle cleaned (verified): dirtiness " + before.ToString("0.##") + " -> 0. No cost charged.";
                    AmbitionControlMod.Log.Msg("Vehicle clean verified: dirtiness " + before.ToString("0.###") + " -> " + after.ToString("0.###"));
                }
                else
                {
                    Status = "Clean did not take effect (dirtiness " + before.ToString("0.##") + " -> " + after.ToString("0.##") + ").";
                    AmbitionControlMod.Log.Warning(Status);
                }
            }
            catch (Exception ex)
            {
                Status = "Vehicle clean failed: " + ex.GetBaseException().Message;
                AmbitionControlMod.Log.Error(Status);
            }
        }

        internal static void TogglePause(GameSnapshot snap, Settings settings)
        {
            savePending = false;
            string block = BlockReason(snap, settings);
            if (block != null) { Status = "Pause not changed: " + block; return; }
            try
            {
                GameSpeedController gsc = SpeedController();
                if (gsc == null) { Status = "Pause not changed: the game's speed controller was not found."; return; }
                bool before = gsc.Paused;
                gsc.SetPause(!before, true);
                bool after = gsc.Paused;
                snap.Paused = after;
                Status = after != before
                    ? "Game " + (after ? "paused" : "resumed") + " (verified)."
                    : "Pause did not change (still " + (after ? "paused" : "running") + ").";
                AmbitionControlMod.Log.Msg("Pause " + before + " -> " + after);
            }
            catch (Exception ex)
            {
                Status = "Pause failed: " + ex.GetBaseException().Message;
                AmbitionControlMod.Log.Error(Status);
            }
        }

        internal static void SetSpeed(int percent, GameSnapshot snap, Settings settings)
        {
            savePending = false;
            string block = BlockReason(snap, settings);
            if (block != null) { Status = "Speed not changed: " + block; return; }
            if (!SpeedPercents.Contains(percent)) { Status = "Speed not changed: unsupported value."; return; }
            try
            {
                float before = Time.timeScale;
                TimeHelper.Command_SetTimeSpeed(percent);
                float after = Time.timeScale;
                snap.TimeScale = after;
                bool matches = Math.Abs(after - percent / 100f) < 0.05f;
                Status = "Game speed " + (percent / 100f).ToString("0.#") + "x requested (experimental): time scale " +
                         before.ToString("0.##") + " -> " + after.ToString("0.##") + (matches ? " (verified)." : " (may still be changing).");
                AmbitionControlMod.Log.Msg("Speed " + percent + "% requested; Time.timeScale " + before.ToString("0.###") + " -> " + after.ToString("0.###"));
            }
            catch (Exception ex)
            {
                Status = "Speed change failed: " + ex.GetBaseException().Message;
                AmbitionControlMod.Log.Error(Status);
            }
        }

        internal static void ToggleTraffic(GameSnapshot snap, Settings settings)
        {
            savePending = false;
            string block = BlockReason(snap, settings);
            if (block != null) { Status = "Traffic not changed: " + block; return; }
            try
            {
                GameManager gm = Manager();
                if (gm == null) { Status = "Traffic not changed: game manager not found."; return; }
                bool before = gm.spawnTraffic;
                GameManager.Command_ToggleTraffic();
                bool after = gm.spawnTraffic;
                snap.Traffic = after;
                Status = after != before
                    ? "Traffic " + (after ? "ON" : "OFF") + " (verified). Runtime only."
                    : "Traffic did not change (still " + (after ? "ON" : "OFF") + ").";
                AmbitionControlMod.Log.Msg("Traffic " + before + " -> " + after);
            }
            catch (Exception ex)
            {
                Status = "Traffic toggle failed: " + ex.GetBaseException().Message;
                AmbitionControlMod.Log.Error(Status);
            }
        }

        /// <summary>First press asks for confirmation; second press runs the game's normal save for the loaded save.</summary>
        internal static void SaveGame(GameSnapshot snap, Settings settings)
        {
            string block = BlockReason(snap, settings);
            if (block != null) { savePending = false; Status = "Not saved: " + block; return; }
            try
            {
                if (!SaveGameManager.CanSave()) { savePending = false; Status = "Not saved: the game says saving is not possible right now."; return; }
                if (!savePending)
                {
                    savePending = true;
                    Status = "Save the game now to \"" + snap.SaveName + "\" (the game's normal save, overwrites that save)? Press Confirm save.";
                    return;
                }
                savePending = false;
                GameManager gm = Manager();
                if (gm == null) { Status = "Not saved: game manager not found."; return; }
                bool ok = gm.SaveGame(snap.SaveName, false);
                Status = ok ? "Save started for \"" + snap.SaveName + "\" (game reported success)." : "The game refused to save.";
                AmbitionControlMod.Log.Msg("SaveGame(\"" + snap.SaveName + "\") returned " + ok);
            }
            catch (Exception ex)
            {
                savePending = false;
                Status = "Save failed: " + ex.GetBaseException().Message;
                AmbitionControlMod.Log.Error(Status);
            }
        }

        internal static void ClearPending() => savePending = false;
    }
}
