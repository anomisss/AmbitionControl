using System;
using AmbitionControl.Core;
using Il2Cpp;
using MelonLoader;
using UnityEngine;

namespace AmbitionControl.Game
{
    /// <summary>
    /// Sales revenue multiplier (Milestone K, reworked in alpha.16). Build-3233 interop evidence:
    ///   GameInstance.Transactions : Queue&lt;Transaction&gt; — the game's own transaction history (newest last)
    ///   Transaction.amount, .transactionType (Transaction.TransactionType.Revenue = business sales income)
    ///   GameManager.ChangeMoneySafe(…, TransactionType.Cheat, …) — the same path the Money tab uses (runtime-verified)
    /// alpha.15 hooked GameManager.ChangeMoney with Harmony; in-game that crashed Il2CppInterop's native->managed
    /// trampoline (Nullable&lt;int&gt; parameter) and broke every money change while installed. This version installs NO
    /// hooks: once a second it reads the new Revenue entries in the game's history and credits the extra
    /// (amount × (multiplier − 1), capped per sale) as one combined "Cheat" transaction, without a popup.
    /// Credits wait while the game is saving or a Money-tab change is being verified, and are read back.
    /// The first scan after load or after turning it on only sets a baseline, so past sales are never boosted.
    /// </summary>
    internal static class RevenueBoost
    {
        internal static string Status { get; private set; }
        internal static float Multiplier { get; private set; } = 1f;
        internal static int SalesSeen { get; private set; }
        internal static int SalesBoosted { get; private set; }
        internal static double ExtraTotal { get; private set; }

        private const float MaxCreditPerBatch = MoneyCommand.MaxAdjustment;
        private static MelonPreferences_Entry<float> pref;
        private static IntPtr sessionPtr;
        private static IntPtr lastSeenPtr;
        private static bool baselineSet;
        private static double pendingExtra;
        private static float nextScan;
        private static int creditsLogged;
        private static bool broken;

        internal static void Init(MelonPreferences_Category prefs, Settings s)
        {
            pref = prefs.CreateEntry("RevenueMultiplier", 1f,
                description: "Multiplier for business sales revenue (1 = off; allowed 1, 1.5, 2, 3, 5, 10). Set from the phone app's Economy tab.");
            Multiplier = CheatMath.NormalizeRevenue(pref.Value);
            if (Math.Abs(Multiplier - pref.Value) > 0.001f)
                AmbitionControlMod.Log.Warning("RevenueMultiplier " + pref.Value + " is not an allowed value; using " + Multiplier + "x.");
            AmbitionControlMod.Log.Msg("Revenue multiplier: " + Label() + " (reads the game's transaction history; no hooks).");
        }

        internal static string Label() => Multiplier <= 1f ? "off (1x)" : "x" + Multiplier.ToString("0.#");

        internal static string BlockReason(GameSnapshot snap, Settings s)
        {
            string b = GameEconomy.BlockReason(snap, s);
            if (b != null) return b;
            if (broken) return "stopped after an error this session (see log).";
            return null;
        }

        /// <summary>Cycle 1x → 1.5x → 2x → 3x → 5x → 10x → 1x. Saved as a mod preference.</summary>
        internal static void Cycle(GameSnapshot snap, Settings s)
        {
            string block = BlockReason(snap, s);
            if (block != null) { Status = "Revenue multiplier not changed: " + block; return; }
            float before = Multiplier;
            float next = CheatMath.NextRevenue(Multiplier);
            pref.Value = next;
            MelonPreferences.Save();
            Multiplier = CheatMath.NormalizeRevenue(pref.Value);
            if (before <= 1f && Multiplier > 1f) baselineSet = false; // sales before turning it on are never boosted
            if (Multiplier <= 1f) pendingExtra = 0d;
            bool ok = Math.Abs(Multiplier - next) < 0.001f;
            Status = ok
                ? "Sales revenue: " + (before <= 1f ? "off" : "x" + before.ToString("0.#")) + " -> " + Label() +
                  " (verified). New business sales get the extra as a separate credit."
                : "Revenue multiplier did not stick (read back " + Multiplier + ").";
            AmbitionControlMod.Log.Msg("Revenue multiplier set to " + Multiplier + " (" + (ok ? "verified" : "NOT as expected") + ").");
        }

        /// <summary>Every frame from OnUpdate; scans once a second.</summary>
        internal static void Tick(GameSnapshot snap, Settings settings, bool moneyIdle)
        {
            if (broken || Multiplier <= 1f || Time.unscaledTime < nextScan) return;
            nextScan = Time.unscaledTime + 1f;
            if (snap == null || !snap.Loaded || GameEconomy.BlockReason(snap, settings) != null) return;
            try
            {
                GameInstance gi = SaveGameManager.Current;
                if (gi == null || gi.Pointer != snap.InstancePtr) return;
                if (gi.Pointer != sessionPtr) { ResetSession(); sessionPtr = gi.Pointer; }
                Scan(gi);
                if (pendingExtra >= 0.01d && moneyIdle && !SaveGameManager.SavingGameInProgress) Credit(gi);
            }
            catch (Exception ex)
            {
                broken = true;
                Status = "Sales revenue multiplier stopped: " + ex.GetBaseException().Message;
                AmbitionControlMod.Log.Error(Status);
            }
        }

        private static void Scan(GameInstance gi)
        {
            var queue = gi.Transactions;
            if (queue == null || queue.Count == 0) return;
            var arr = queue.ToArray();
            int n = arr.Length;
            if (n == 0) return;
            Transaction newest = arr[n - 1];
            if (newest == null) return;
            if (!baselineSet)
            {
                baselineSet = true;
                lastSeenPtr = newest.Pointer;
                AmbitionControlMod.Log.Msg("Revenue multiplier: baseline set (" + n + " past transactions not boosted).");
                return;
            }
            if (newest.Pointer == lastSeenPtr) return;
            int start = -1;
            for (int i = n - 1; i >= 0; i--)
                if (arr[i] != null && arr[i].Pointer == lastSeenPtr) { start = i + 1; break; }
            if (start < 0)
            {
                // Our marker fell out of the game's history (very many transactions at once); skip rather than guess.
                AmbitionControlMod.Log.Warning("Revenue multiplier: history moved past the last seen entry; new baseline set.");
                lastSeenPtr = newest.Pointer;
                return;
            }
            for (int i = start; i < n; i++)
            {
                Transaction t = arr[i];
                if (t == null || t.transactionType != Transaction.TransactionType.Revenue) continue;
                SalesSeen++;
                if (!CheatMath.TryBoost(t.amount, Multiplier, out float boosted)) continue;
                SalesBoosted++;
                pendingExtra += boosted - t.amount;
                if (SalesBoosted <= 3)
                    AmbitionControlMod.Log.Msg("Revenue x" + Multiplier.ToString("0.#") + ": sale $" + t.amount.ToString("0.00") +
                                               " -> extra $" + (boosted - t.amount).ToString("0.00"));
            }
            lastSeenPtr = newest.Pointer;
        }

        private static void Credit(GameInstance gi)
        {
            float extra = (float)Math.Min(pendingExtra, MaxCreditPerBatch);
            float before = gi.Money;
            if ((double)before + extra > MoneyCommand.MaxBalance)
            {
                pendingExtra = 0d;
                Status = "Sales revenue extra skipped: balance would exceed $100M.";
                return;
            }
            bool accepted = GameManager.ChangeMoneySafe(extra, Transaction.TransactionType.Cheat, new Transaction.DataHolder(),
                new Il2CppSystem.Nullable<int>(), null, false, false);
            float after = gi.Money;
            bool ok = accepted && MoneyCommand.BalanceMatches(after, (double)before + extra);
            if (!ok)
            {
                broken = true;
                Status = "Sales revenue multiplier stopped: the extra $" + extra.ToString("0.00") + " was not credited (balance $" +
                         before.ToString("0.00") + " -> $" + after.ToString("0.00") + ").";
                AmbitionControlMod.Log.Error(Status);
                return;
            }
            pendingExtra -= extra;
            ExtraTotal += extra;
            if (creditsLogged++ < 3)
                AmbitionControlMod.Log.Msg("Revenue extra credited (verified): +$" + extra.ToString("0.00") + ", balance $" +
                                           before.ToString("0.00") + " -> $" + after.ToString("0.00"));
        }

        internal static void ResetSession()
        {
            sessionPtr = IntPtr.Zero;
            lastSeenPtr = IntPtr.Zero;
            baselineSet = false;
            pendingExtra = 0d;
            SalesSeen = SalesBoosted = 0;
            ExtraTotal = 0d;
            creditsLogged = 0;
            broken = false;
        }
    }
}
