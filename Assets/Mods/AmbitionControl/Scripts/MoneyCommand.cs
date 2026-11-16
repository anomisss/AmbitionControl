using System;
using System.Globalization;

namespace AmbitionControl
{
    // Pure, deterministic input guards, independent of game APIs.
    public static class MoneyCommand
    {
        public const long MaxBalance = 100000000L;
        public const long MaxAdjustment = 10000000L;

        public static bool TryParse(string text, out long value, out string error)
        {
            value = 0L;
            error = null;
            if (string.IsNullOrWhiteSpace(text) ||
                !long.TryParse(text.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out value))
            {
                error = "Enter a whole, non-negative amount using digits only.";
                return false;
            }
            if (value > MaxBalance)
            {
                error = "Balance limits: maximum $100,000,000.";
                return false;
            }
            return true;
        }

        public static bool TryPrepare(MoneyAction action, float balance, long amount,
            out float delta, out string error)
        {
            delta = 0f;
            error = null;
            if (float.IsNaN(balance) || float.IsInfinity(balance) || balance < 0f || balance > MaxBalance)
            {
                error = "Current balance is outside the supported range (0 to $100M).";
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
                case MoneyAction.Add: desired = balance + amount; break;
                case MoneyAction.Remove: desired = balance - amount; break;
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
                error = "No significant change; amounts are rounded by the game's float balance.";
                return false;
            }
            if (Math.Abs(diff) > MaxAdjustment)
            {
                error = "Maximum change per command: $10,000,000.";
                return false;
            }
            delta = (float)diff;
            return true;
        }
    }

    public enum MoneyAction
    {
        Add,
        Remove,
        Set
    }
}
