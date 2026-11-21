using System;

namespace AmbitionControl.Core
{
    public enum WageStep { MinusTenPercent, MinusOne, PlusOne, PlusTenPercent }

    /// <summary>
    /// Pure, deterministic helpers for the v0.7.0-alpha.15 features (no game or Unity types, so they are unit-tested):
    /// the money keypad, individual wage steps and the revenue multiplier.
    /// </summary>
    public static class CheatMath
    {
        // ---------------------------------------------------------------- money keypad

        /// <summary>
        /// Append typed digits ("0".."9", "00", "000") to an amount. Returns false and leaves the amount unchanged
        /// if the key is not digits or the result would exceed <see cref="MoneyCommand.MaxBalance"/>.
        /// </summary>
        public static bool TryAppendDigits(long amount, string digits, out long result)
        {
            result = amount;
            if (string.IsNullOrEmpty(digits) || digits.Length > 3 || amount < 0L) return false;
            long value = amount;
            foreach (char c in digits)
            {
                if (c < '0' || c > '9') return false;
                if (value > (MoneyCommand.MaxBalance - (c - '0')) / 10L) return false;
                value = value * 10L + (c - '0');
            }
            if (value > MoneyCommand.MaxBalance) return false;
            result = value;
            return true;
        }

        /// <summary>Remove the last typed digit.</summary>
        public static long Backspace(long amount) => amount <= 0L ? 0L : amount / 10L;

        // ---------------------------------------------------------------- wages

        public const float MinWage = 1f;
        public const float MaxWage = 500f;

        /// <summary>
        /// Next hourly wage for one step, rounded to cents and kept within [$1, $500]/h.
        /// Returns false (with a reason) when the wage would not change or the input is not a valid wage.
        /// </summary>
        public static bool TryStepWage(float current, WageStep step, out float next, out string error)
        {
            next = current;
            error = null;
            if (float.IsNaN(current) || float.IsInfinity(current) || current < 0f || current > 100_000f)
            {
                error = "the current wage is not a valid value.";
                return false;
            }
            double v;
            switch (step)
            {
                case WageStep.MinusTenPercent: v = current * 0.9d; break;
                case WageStep.MinusOne: v = current - 1d; break;
                case WageStep.PlusOne: v = current + 1d; break;
                case WageStep.PlusTenPercent: v = current * 1.1d; break;
                default: error = "unknown step."; return false;
            }
            v = Math.Round(v, 2, MidpointRounding.AwayFromZero);
            if (v < MinWage) v = MinWage;
            if (v > MaxWage) v = MaxWage;
            if (Math.Abs(v - current) < 0.005d)
            {
                error = current <= MinWage + 0.005f ? "the wage is already at the minimum ($1/h)." :
                        current >= MaxWage - 0.005f ? "the wage is already at the maximum ($500/h)." : "no change.";
                return false;
            }
            next = (float)v;
            return true;
        }

        /// <summary>A market wage from the game is accepted only when it is a sane value inside the same bounds.</summary>
        public static bool IsUsableMarketWage(float w) =>
            !float.IsNaN(w) && !float.IsInfinity(w) && w >= MinWage && w <= MaxWage;

        // ---------------------------------------------------------------- revenue multiplier

        public static readonly float[] RevenueChoices = { 1f, 1.5f, 2f, 3f, 5f, 10f };

        /// <summary>Largest extra amount added to a single sale (a guard against absurd values).</summary>
        public const float MaxExtraPerSale = 1_000_000f;

        /// <summary>Snap a stored preference to the nearest allowed choice (invalid values become 1x).</summary>
        public static float NormalizeRevenue(float stored)
        {
            if (float.IsNaN(stored) || float.IsInfinity(stored)) return 1f;
            foreach (float c in RevenueChoices)
                if (Math.Abs(c - stored) < 0.001f) return c;
            return 1f;
        }

        public static float NextRevenue(float current)
        {
            float c = NormalizeRevenue(current);
            int i = Array.IndexOf(RevenueChoices, c);
            return RevenueChoices[(i + 1) % RevenueChoices.Length];
        }

        /// <summary>
        /// Boosted amount for one revenue credit. Only positive, finite amounts are boosted; the extra is capped.
        /// Returns false when the amount should be left alone.
        /// </summary>
        public static bool TryBoost(float amount, float multiplier, out float boosted)
        {
            boosted = amount;
            if (float.IsNaN(amount) || float.IsInfinity(amount) || amount <= 0f) return false;
            float m = NormalizeRevenue(multiplier);
            if (m <= 1f) return false;
            double extra = Math.Min((double)amount * (m - 1d), MaxExtraPerSale);
            boosted = (float)(amount + extra);
            return true;
        }
    }
}
