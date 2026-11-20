using System;

namespace AmbitionControl.Core
{
    public enum MoneyAction
    {
        Add,
        Remove,
        Set
    }

    /// <summary>
    /// Pure, deterministic money guards. No game or Unity types, so it is unit-testable.
    /// Ported from the v0.3.0 SDK MoneyCommand, with one change: the current balance may be
    /// negative (debt is possible in Big Ambitions); the resulting balance still may not be.
    /// </summary>
    public static class MoneyCommand
    {
        public const long MaxBalance = 100_000_000L;
        public const long MaxAdjustment = 10_000_000L;

        public static bool TryPrepare(MoneyAction action, float balance, long amount,
            out float delta, out string error)
        {
            delta = 0f;
            error = null;
            if (float.IsNaN(balance) || float.IsInfinity(balance) || Math.Abs(balance) > MaxBalance)
            {
                error = "Current balance is outside the supported range (-$100M to $100M).";
                return false;
            }
            if (amount < 0L || amount > MaxBalance)
            {
                error = "Requested amount is outside the supported range.";
                return false;
            }

            double desired;
            switch (action)
            {
                case MoneyAction.Add: desired = (double)balance + amount; break;
                case MoneyAction.Remove: desired = (double)balance - amount; break;
                case MoneyAction.Set: desired = amount; break;
                default:
                    error = "Unrecognized money command.";
                    return false;
            }

            if (desired < 0d || desired > MaxBalance)
            {
                error = "Result would be negative or exceed $100M.";
                return false;
            }

            double diff = desired - balance;
            if (Math.Abs(diff) < 0.5d)
            {
                error = "No significant change.";
                return false;
            }
            if (Math.Abs(diff) > MaxAdjustment)
            {
                error = "Maximum change per action is $10,000,000.";
                return false;
            }

            delta = (float)diff;
            return true;
        }

        /// <summary>Balance comparison tolerant of float rounding at large magnitudes.</summary>
        public static bool BalanceMatches(float actual, double expected)
        {
            double tolerance = Math.Max(1.0d, Math.Abs(expected) * 1e-6d);
            return Math.Abs(actual - expected) <= tolerance;
        }

        /// <summary>Clamp helper for the amount picker.</summary>
        public static long ClampAmount(long amount) =>
            amount < 0L ? 0L : (amount > MaxBalance ? MaxBalance : amount);
    }
}
