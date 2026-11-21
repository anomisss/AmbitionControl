using System;
using Il2Cpp;
using Il2CppHelpers;
using Il2CppVehicles.VehicleTypes;

namespace AmbitionControl.Game
{
    /// <summary>
    /// Get a vehicle (Milestone K, World tab). Build-3233 interop evidence:
    ///   VehicleHelper.Command_GetCar(VehicleTypeName) — the game's own command that gives the player a car
    ///   GameInstance.VehicleInstances (List&lt;VehicleInstance&gt;), VehicleInstance.vehicleTypeName/.id — readback
    ///   GameInstance.Money — checked before/after so a charge (if the command charges) is reported, not hidden
    /// Two-press confirm; on foot and outdoors only (the car is placed near the player). Work-only and non-car types
    /// (hand truck, job delivery van, flatbed, freight truck, scooter) are not offered.
    /// </summary>
    internal static class VehicleSpawn
    {
        internal static readonly VehicleTypeName[] Types =
        {
            VehicleTypeName.VordPony, VehicleTypeName.HonzaMimic, VehicleTypeName.MissamVillian, VehicleTypeName.Bima320,
            VehicleTypeName.VordTiaraVic, VehicleTypeName.VordV150, VehicleTypeName.UMCNunavut, VehicleTypeName.UMCDesert,
            VehicleTypeName.MersaidiDash, VehicleTypeName.MersaidiS500, VehicleTypeName.MersaidiMGAGT, VehicleTypeName.Ferdinand112,
            VehicleTypeName.AnselmoAF90, VehicleTypeName.PetrollsFanton, VehicleTypeName.DeliveryTruck
        };

        internal static string Status { get; private set; }
        internal static int TypeIndex { get; private set; }
        internal static bool Pending { get; private set; }

        internal static string TypeLabel => GameText.Words(Types[TypeIndex].ToString());

        internal static void NextType()
        {
            Pending = false;
            TypeIndex = (TypeIndex + 1) % Types.Length;
        }

        internal static string Block(GameSnapshot snap, Settings settings)
        {
            string b = WorldTools.BlockReason(snap, settings);
            if (b != null) return b;
            try
            {
                if (PlayerHelper.IsUsingVehicle) return "Get out of the vehicle first.";
                if (BuildingManager.IsInsideBuilding) return "Go outside first (the car is placed next to you).";
            }
            catch (Exception ex) { return "Unavailable: " + ex.GetBaseException().Message; }
            return null;
        }

        internal static void GetPressed(GameSnapshot snap, Settings settings)
        {
            string block = Block(snap, settings);
            if (block != null) { Pending = false; Status = "No vehicle added: " + block; return; }
            VehicleTypeName type = Types[TypeIndex];
            if (!Pending)
            {
                Pending = true;
                Status = "Get a free " + TypeLabel + "? The game adds it to your vehicles next to you (it keeps a resale value). Press Confirm.";
                return;
            }
            Pending = false;
            try
            {
                GameInstance gi = SaveGameManager.Current;
                if (gi == null || gi.Pointer != snap.InstancePtr) { Status = "No vehicle added: the save changed."; return; }
                int before = gi.VehicleInstances != null ? gi.VehicleInstances.Count : 0;
                float moneyBefore = gi.Money;
                VehicleHelper.Command_GetCar(type);
                int after = gi.VehicleInstances != null ? gi.VehicleInstances.Count : 0;
                float charged = moneyBefore - gi.Money;
                VehicleInstance added = after > before ? gi.VehicleInstances[after - 1] : null;
                if (added != null && added.vehicleTypeName == type)
                {
                    Status = "Vehicle added (verified): " + TypeLabel + ". Your vehicles " + before + " -> " + after +
                             (Math.Abs(charged) >= 0.01f ? "; balance changed by $" + (-charged).ToString("N2") + "." : "; no charge.");
                    AmbitionControlMod.Log.Msg("Vehicle added (verified): " + type + " id " + added.id + "; vehicles " + before + " -> " + after +
                                               "; balance change " + (-charged).ToString("0.00"));
                }
                else
                {
                    Status = "The game did not add a " + TypeLabel + " to your vehicles (vehicles " + before + " -> " + after + ").";
                    AmbitionControlMod.Log.Warning(Status + (added != null ? " Last vehicle type: " + added.vehicleTypeName : ""));
                }
            }
            catch (Exception ex)
            {
                Status = "Getting a vehicle failed: " + ex.GetBaseException().Message;
                AmbitionControlMod.Log.Error(Status);
            }
        }

        internal static void ClearPending() => Pending = false;
    }
}
