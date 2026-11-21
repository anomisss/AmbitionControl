using System;
using System.Linq;
using System.Reflection;
using AmbitionControl.Core;
using Il2Cpp;
using MelonLoader;

namespace AmbitionControl.Game
{
    /// <summary>
    /// Revenue multiplier (Milestone K). Build-3233 interop evidence:
    ///   GameManager.ChangeMoney(float, Transaction.TransactionType, Transaction.DataHolder, Nullable&lt;int&gt;, Address)
    ///   GameManager.ChangeMoneySafe(float, Transaction.TransactionType, DataHolder, Nullable&lt;int&gt;, Address, bool, bool)
    ///   Transaction.TransactionType.Revenue — the game's own tag for business sales income (Cheat, Wage, Rent, ItemSold,
    ///   RentRevenue … are separate types and are never touched).
    /// A Harmony prefix on both money entry points multiplies positive Revenue amounts before the game records them, so
    /// the boosted amount shows in the game's own transaction history. A re-entrancy flag stops a double boost if one
    /// entry point calls the other. Readback: balance before/after the call is compared with the boosted amount for the
    /// first sales of each session. The mod's own money changes use TransactionType.Cheat, so they are unaffected.
    /// The multiplier is a mod preference (RevenueMultiplier, default 1x = off), not part of the save.
    /// The hooks are installed only when a multiplier above 1x is first used (at start-up if the saved preference is
    /// above 1x), so players who never use the feature run the game's money code untouched.
    /// </summary>
    internal static class RevenueBoost
    {
        internal static string Status { get; private set; }
        internal static float Multiplier { get; private set; } = 1f;
        internal static int PatchesApplied { get; private set; }
        internal static int SalesSeen { get; private set; }
        internal static int SalesBoosted { get; private set; }
        internal static double ExtraTotal { get; private set; }

        private static MelonPreferences_Entry<float> pref;
        private static Settings settings;
        private static HarmonyLib.Harmony harmonyRef;
        private static bool hookAttempted;
        private static bool active;        // re-entrancy guard (main thread only)
        private static float balanceBefore;
        private static float expectedDelta;
        private static int verifiedLogs;
        private static bool loggedFirstSeen;

        internal static void Init(MelonPreferences_Category prefs, HarmonyLib.Harmony harmony, Settings s)
        {
            settings = s;
            pref = prefs.CreateEntry("RevenueMultiplier", 1f,
                description: "Multiplier for business sales revenue (1 = off; allowed 1, 1.5, 2, 3, 5, 10). Set from the phone app's Economy tab.");
            Multiplier = CheatMath.NormalizeRevenue(pref.Value);
            if (Math.Abs(Multiplier - pref.Value) > 0.001f)
                AmbitionControlMod.Log.Warning("RevenueMultiplier " + pref.Value + " is not an allowed value; using " + Multiplier + "x.");

            harmonyRef = harmony;
            if (Multiplier > 1f && !EnsureHooked())
            {
                Multiplier = 1f;
                AmbitionControlMod.Log.Warning("Revenue multiplier disabled for this run: the money hooks could not be installed.");
            }
            else AmbitionControlMod.Log.Msg("Revenue multiplier: " + Label() + (Multiplier > 1f ? "" : " (hooks not installed until used)") + ".");
        }

        /// <summary>Install both money hooks once. True if at least one is active.</summary>
        private static bool EnsureHooked()
        {
            if (hookAttempted) return PatchesApplied > 0;
            hookAttempted = true;
            HarmonyLib.Harmony harmony = harmonyRef;
            const BindingFlags S = BindingFlags.Public | BindingFlags.Static;
            foreach (string name in new[] { "ChangeMoney", "ChangeMoneySafe" })
            {
                try
                {
                    MethodInfo target = typeof(GameManager).GetMethods(S).FirstOrDefault(m =>
                    {
                        if (m.Name != name) return false;
                        ParameterInfo[] p = m.GetParameters();
                        return p.Length >= 2 && p[0].ParameterType == typeof(float) && p[1].ParameterType == typeof(Transaction.TransactionType);
                    });
                    if (target == null) throw new MissingMethodException("GameManager", name);
                    harmony.Patch(target,
                        prefix: new HarmonyLib.HarmonyMethod(typeof(RevenueBoost).GetMethod(nameof(Prefix), BindingFlags.NonPublic | BindingFlags.Static)),
                        postfix: new HarmonyLib.HarmonyMethod(typeof(RevenueBoost).GetMethod(nameof(Postfix), BindingFlags.NonPublic | BindingFlags.Static)));
                    PatchesApplied++;
                }
                catch (Exception ex)
                {
                    AmbitionControlMod.Log.Warning("Revenue multiplier: could not hook GameManager." + name + ": " + ex.GetBaseException().Message);
                }
            }
            AmbitionControlMod.Log.Msg("Revenue multiplier: " + PatchesApplied + "/2 money hooks installed.");
            return PatchesApplied > 0;
        }

        internal static string Label() => Multiplier <= 1f ? "off (1x)" : "x" + Multiplier.ToString("0.#");

        internal static string BlockReason(GameSnapshot snap, Settings s)
        {
            string b = GameEconomy.BlockReason(snap, s);
            if (b != null) return b;
            if (hookAttempted && PatchesApplied == 0) return "the game's money functions could not be hooked (see log).";
            return null;
        }

        /// <summary>Cycle 1x → 1.5x → 2x → 3x → 5x → 10x → 1x. Saved as a mod preference.</summary>
        internal static void Cycle(GameSnapshot snap, Settings s)
        {
            string block = BlockReason(snap, s);
            if (block != null) { Status = "Revenue multiplier not changed: " + block; return; }
            float before = Multiplier;
            float next = CheatMath.NextRevenue(Multiplier);
            if (next > 1f && !EnsureHooked()) { Status = "Revenue multiplier not changed: the game's money functions could not be hooked (see log)."; return; }
            pref.Value = next;
            MelonPreferences.Save();
            Multiplier = CheatMath.NormalizeRevenue(pref.Value);
            bool ok = Math.Abs(Multiplier - next) < 0.001f;
            Status = ok
                ? "Revenue multiplier: " + (before <= 1f ? "off" : "x" + before.ToString("0.#")) + " -> " + Label() +
                  " (verified). Applies to new sales; " + SalesSeen + " sales seen this session."
                : "Revenue multiplier did not stick (read back " + Multiplier + ").";
            AmbitionControlMod.Log.Msg("Revenue multiplier set to " + Multiplier + " (" + (ok ? "verified" : "NOT as expected") +
                                       "); hooks " + PatchesApplied + "/2, sales seen " + SalesSeen + ", boosted " + SalesBoosted + ".");
        }

        private static bool Allowed()
        {
            if (settings == null || !settings.EconomyControlsEnabled) return false;
            int? build = GameBridge.BuildNumber;
            if (!build.HasValue || (build.Value != GameBridge.TargetBuild && !settings.AllowOtherBuilds)) return false;
            return SaveGameManager.Current != null && !SaveGameManager.SavingGameInProgress;
        }

        // Harmony prefix on both entry points: __0 = amount, __1 = transaction type.
        private static void Prefix(ref float __0, Transaction.TransactionType __1, out bool __state)
        {
            __state = false;
            try
            {
                if (active || __1 != Transaction.TransactionType.Revenue) return;
                active = true;
                __state = true;
                SalesSeen++;
                if (!loggedFirstSeen)
                {
                    loggedFirstSeen = true;
                    AmbitionControlMod.Log.Msg("Revenue hook: first sale seen this session ($" + __0.ToString("0.00") + ", multiplier " + Label() + ").");
                }
                expectedDelta = float.NaN;
                if (!Allowed() || !CheatMath.TryBoost(__0, Multiplier, out float boosted)) return;
                balanceBefore = SaveGameManager.Current.Money;
                expectedDelta = boosted;
                SalesBoosted++;
                ExtraTotal += boosted - __0;
                if (verifiedLogs < 3)
                    AmbitionControlMod.Log.Msg("Revenue x" + Multiplier.ToString("0.#") + ": sale $" + __0.ToString("0.00") + " -> $" + boosted.ToString("0.00"));
                __0 = boosted;
            }
            catch (Exception ex)
            {
                AmbitionControlMod.Log.Warning("Revenue multiplier skipped a sale: " + ex.GetBaseException().Message);
            }
        }

        private static void Postfix(bool __state)
        {
            if (!__state) return;
            active = false;
            try
            {
                if (float.IsNaN(expectedDelta) || verifiedLogs >= 3) return;
                verifiedLogs++;
                float delta = SaveGameManager.Current.Money - balanceBefore;
                bool ok = MoneyCommand.BalanceMatches(delta, expectedDelta);
                AmbitionControlMod.Log.Msg("Revenue readback: balance +" + delta.ToString("0.00") + ", expected +" + expectedDelta.ToString("0.00") +
                                           (ok ? " (verified)" : " (NOT as expected)"));
            }
            catch { /* readback is diagnostic only */ }
        }

        /// <summary>Called every frame: a stuck re-entrancy flag (exception inside the game call) never outlives a frame.</summary>
        internal static void Tick() => active = false;

        internal static void ResetSession()
        {
            active = false;
            SalesSeen = 0;
            SalesBoosted = 0;
            ExtraTotal = 0d;
            verifiedLogs = 0;
            loggedFirstSeen = false;
        }
    }
}
