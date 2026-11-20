using System;
using System.Collections.Generic;
using Il2Cpp;

namespace AmbitionControl.Game
{
    internal sealed class BizRow
    {
        internal string Name;
        internal Address Address;
        internal string AddressText;
        internal bool Rented, Owned;
        internal bool HasSatisfaction;
        internal int Overall, Service, Pricing, Cleanliness, Facility;
    }

    /// <summary>
    /// Business tab (Milestone I). Build-3233 interop evidence:
    ///   GameInstance.BuildingRegistrations : List&lt;BuildingRegistration&gt;
    ///   BuildingRegistration.RentedByPlayer, BuildingOwnedByPlayer, GetDisplayName(), Address {streetName, streetNumber}, HasValidAddress
    ///   BuildingRegistration.satisfaction : Satisfaction {overall, customerService, pricing, cleanliness, facility}
    ///   GameManager.Command_TeleportPlayerToAddress(int, StreetName) (via QuestTools.TeleportToAddress, verified by movement)
    /// Read-only list; the only action is teleporting to a business.
    /// </summary>
    internal static class BusinessTools
    {
        internal const int PageSize = 5;

        internal static string BlockReason(GameSnapshot snap, Settings settings)
        {
            if (!settings.BusinessToolsEnabled) return "Business tools are turned off in preferences (BusinessToolsEnabled).";
            int? build = GameBridge.BuildNumber;
            if (!build.HasValue) return "Game build not detected yet.";
            if (build.Value != GameBridge.TargetBuild && !settings.AllowOtherBuilds)
                return "Game build " + build.Value + " is not the verified build " + GameBridge.TargetBuild + ".";
            if (snap == null || !snap.Loaded) return snap?.NotLoadedReason ?? "No save loaded.";
            if (snap.Saving) return "Game is saving; wait a moment.";
            return null;
        }

        private static bool loggedOnce;

        internal static List<BizRow> ReadRows(GameSnapshot snap, out string error)
        {
            error = null;
            var rows = new List<BizRow>();
            try
            {
                GameInstance gi = SaveGameManager.Current;
                if (gi == null || gi.Pointer != snap.InstancePtr) { error = "No save loaded."; return rows; }
                if (gi.BuildingRegistrations == null) return rows;
                foreach (BuildingRegistration r in gi.BuildingRegistrations)
                {
                    if (r == null) continue;
                    bool rented = r.RentedByPlayer, owned = r.BuildingOwnedByPlayer;
                    if (!rented && !owned) continue;
                    var row = new BizRow { Rented = rented, Owned = owned };
                    try { row.Name = r.GetDisplayName(); } catch { }
                    if (string.IsNullOrEmpty(row.Name)) row.Name = string.IsNullOrEmpty(r.BusinessName) ? "(unnamed building)" : r.BusinessName;
                    row.Name = System.Text.RegularExpressions.Regex.Replace(row.Name, "<.*?>", "");
                    try
                    {
                        if (r.HasValidAddress) { row.Address = r.Address; row.AddressText = row.Address != null ? row.Address.ToString() : ""; }
                    }
                    catch { }
                    try
                    {
                        Satisfaction s = r.satisfaction;
                        if (s != null)
                        {
                            row.HasSatisfaction = true;
                            row.Overall = s.overall; row.Service = s.customerService; row.Pricing = s.pricing;
                            row.Cleanliness = s.cleanliness; row.Facility = s.facility;
                        }
                    }
                    catch { }
                    rows.Add(row);
                }
                rows.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
                if (!loggedOnce)
                {
                    loggedOnce = true;
                    var parts = new List<string>();
                    foreach (BizRow b in rows)
                        parts.Add(b.Name + " @ " + (b.AddressText ?? "?") + (b.HasSatisfaction ? " sat " + b.Overall : ""));
                    AmbitionControlMod.Log.Msg("Businesses: " + rows.Count + " rented/owned — " + string.Join("; ", parts));
                }
            }
            catch (Exception ex)
            {
                error = "Could not read businesses: " + ex.GetBaseException().Message;
            }
            return rows;
        }

        internal static void GoTo(BizRow row, GameSnapshot snap, Settings settings)
        {
            string block = BlockReason(snap, settings);
            if (block != null) { QuestTools.SetStatus("Not teleported: " + block); return; }
            if (row == null || row.Address == null) { QuestTools.SetStatus("Not teleported: this building has no address."); return; }
            QuestTools.TeleportToAddress(row.Address, row.Name);
        }

        internal static void ResetSession() => loggedOnce = false;
    }
}
