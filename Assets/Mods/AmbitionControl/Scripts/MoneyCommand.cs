using System;
using System.Globalization;

namespace AmbitionControl
{
    // Pure validation functions for a future game-state adapter.
    public static class MoneyCommand
    {
        public const long MaxBalance = 1000000000000L;

        public static bool TryParse(string text, out long value, out string error)
        {
            value = 0;
            error = null;
            if (string.IsNullOrWhiteSpace(text) ||
                !long.TryParse(text.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out value))
            {
                error = "Enter whole non-negative dollars using digits only.";
                return false;
            }
            if (value > MaxBalance)
            {
                error = "The amount exceeds the one-trillion-dollar limit.";
                return false;
            }
            return true;
        }

        public static bool TryAdd(long current, long amount, out long result)
        {
            result = 0;
            if (current < 0 || current > MaxBalance || amount < 0 || amount > MaxBalance) return false;
            if (current > MaxBalance - amount) return false;
            result = current + amount;
            return true;
        }
    }
}
