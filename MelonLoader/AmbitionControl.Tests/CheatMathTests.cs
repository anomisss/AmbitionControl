using AmbitionControl.Core;
using Xunit;

public class CheatMathTests
{
    [Theory]
    [InlineData(0L, "5", 5L)]
    [InlineData(12L, "3", 123L)]
    [InlineData(25L, "000", 25_000L)]
    [InlineData(7L, "00", 700L)]
    [InlineData(10_000_000L, "0", 100_000_000L)]   // exactly the $100M cap
    public void Keypad_appends(long start, string keys, long expected)
    {
        Assert.True(CheatMath.TryAppendDigits(start, keys, out long r));
        Assert.Equal(expected, r);
    }

    [Theory]
    [InlineData(100_000_000L, "0")]   // would exceed $100M
    [InlineData(10_000_001L, "0")]
    [InlineData(1_000_000L, "000")]
    [InlineData(5L, "a")]
    [InlineData(5L, "")]
    [InlineData(5L, "0000")]
    [InlineData(-1L, "1")]
    public void Keypad_refuses(long start, string keys)
    {
        Assert.False(CheatMath.TryAppendDigits(start, keys, out long r));
        Assert.Equal(start, r);
    }

    [Fact]
    public void Keypad_backspace()
    {
        Assert.Equal(12L, CheatMath.Backspace(123L));
        Assert.Equal(0L, CheatMath.Backspace(7L));
        Assert.Equal(0L, CheatMath.Backspace(0L));
    }

    [Theory]
    [InlineData(20f, WageStep.PlusOne, 21f)]
    [InlineData(20f, WageStep.MinusOne, 19f)]
    [InlineData(20f, WageStep.PlusTenPercent, 22f)]
    [InlineData(20f, WageStep.MinusTenPercent, 18f)]
    [InlineData(15.55f, WageStep.PlusTenPercent, 17.11f)]  // rounded to cents
    [InlineData(1.5f, WageStep.MinusOne, 1f)]              // clamped to $1
    [InlineData(499.5f, WageStep.PlusOne, 500f)]           // clamped to $500
    public void Wage_steps(float current, WageStep step, float expected)
    {
        Assert.True(CheatMath.TryStepWage(current, step, out float next, out string error), error);
        Assert.Equal(expected, next, 2);
    }

    [Theory]
    [InlineData(1f, WageStep.MinusOne)]
    [InlineData(1f, WageStep.MinusTenPercent)]
    [InlineData(500f, WageStep.PlusOne)]
    [InlineData(float.NaN, WageStep.PlusOne)]
    [InlineData(-3f, WageStep.PlusOne)]
    public void Wage_step_refused(float current, WageStep step)
    {
        Assert.False(CheatMath.TryStepWage(current, step, out _, out string error));
        Assert.False(string.IsNullOrEmpty(error));
    }

    [Fact]
    public void Market_wage_sanity()
    {
        Assert.True(CheatMath.IsUsableMarketWage(18.5f));
        Assert.False(CheatMath.IsUsableMarketWage(0f));
        Assert.False(CheatMath.IsUsableMarketWage(float.NaN));
        Assert.False(CheatMath.IsUsableMarketWage(5000f));
    }

    [Fact]
    public void Revenue_choices_cycle_and_normalize()
    {
        Assert.Equal(1f, CheatMath.NormalizeRevenue(float.NaN));
        Assert.Equal(1f, CheatMath.NormalizeRevenue(7f));
        Assert.Equal(2f, CheatMath.NormalizeRevenue(2f));
        Assert.Equal(1.5f, CheatMath.NextRevenue(1f));
        Assert.Equal(1f, CheatMath.NextRevenue(10f));
        Assert.Equal(1.5f, CheatMath.NextRevenue(123f)); // invalid -> 1x -> next
    }

    [Theory]
    [InlineData(10f, 2f, true, 20f)]
    [InlineData(10f, 1.5f, true, 15f)]
    [InlineData(10f, 1f, false, 10f)]
    [InlineData(-10f, 2f, false, -10f)]     // money going out is never boosted
    [InlineData(0f, 2f, false, 0f)]
    [InlineData(10f, 7f, false, 10f)]       // invalid multiplier -> 1x
    [InlineData(500_000f, 10f, true, 1_500_000f)] // extra capped at $1M per sale
    public void Revenue_boost(float amount, float mult, bool boosted, float expected)
    {
        Assert.Equal(boosted, CheatMath.TryBoost(amount, mult, out float r));
        Assert.Equal(expected, r, 2);
    }
}
