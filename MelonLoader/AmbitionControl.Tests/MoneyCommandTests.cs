using AmbitionControl.Core;
using Xunit;

public class MoneyCommandTests
{
    [Theory]
    [InlineData(MoneyAction.Add, 1000f, 500L, 500f)]
    [InlineData(MoneyAction.Remove, 1000f, 400L, -400f)]
    [InlineData(MoneyAction.Set, 1000f, 250L, -750f)]
    [InlineData(MoneyAction.Set, -5000f, 1000L, 6000f)]   // debt -> positive is allowed
    public void Prepares_expected_delta(MoneyAction action, float balance, long amount, float expected)
    {
        Assert.True(MoneyCommand.TryPrepare(action, balance, amount, out float delta, out string error), error);
        Assert.Equal(expected, delta, 3);
    }

    [Theory]
    [InlineData(MoneyAction.Remove, 100f, 200L)]                // would go negative
    [InlineData(MoneyAction.Add, 99_999_000f, 10_000L)]        // exceeds $100M
    [InlineData(MoneyAction.Add, 0f, 10_000_001L)]              // exceeds per-change limit
    [InlineData(MoneyAction.Set, 500f, 500L)]                   // no change
    [InlineData(MoneyAction.Add, float.NaN, 1L)]
    [InlineData(MoneyAction.Add, 0f, -1L)]
    public void Rejects_invalid(MoneyAction action, float balance, long amount)
    {
        Assert.False(MoneyCommand.TryPrepare(action, balance, amount, out _, out string error));
        Assert.False(string.IsNullOrEmpty(error));
    }

    [Fact]
    public void Balance_match_tolerates_float_rounding_at_scale()
    {
        Assert.True(MoneyCommand.BalanceMatches(50_000_004f, 50_000_000d));
        Assert.False(MoneyCommand.BalanceMatches(50_000_100f, 50_000_000d));
        Assert.True(MoneyCommand.BalanceMatches(100.5f, 100d));
        Assert.False(MoneyCommand.BalanceMatches(102f, 100d));
    }

    [Fact]
    public void Clamp_amount()
    {
        Assert.Equal(0L, MoneyCommand.ClampAmount(-5L));
        Assert.Equal(MoneyCommand.MaxBalance, MoneyCommand.ClampAmount(MoneyCommand.MaxBalance + 1));
    }
}
