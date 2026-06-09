using NUnit.Framework;
using WordPuzzle.Puzzle;

/// <summary>
/// Task 39B — exponential backoff curve for ad load retries.
///
/// Acceptance criteria:
/// • Attempt 0 returns the BalanceConfig base delay.
/// • Each attempt doubles the previous delay (standard AdMob backoff).
/// • The curve is capped at BalanceConfig.AdRetryMaxDelaySeconds.
/// • Defensive: negative / huge attempts never under- or overflow.
/// </summary>
[TestFixture]
public class AdRetryPolicyTests
{
    [Test]
    public void Attempt0_ReturnsBaseDelay()
    {
        Assert.AreEqual(BalanceConfig.AdRetryBaseDelaySeconds,
            AdRetryPolicy.NextDelaySeconds(0),
            "Attempt 0 must wait exactly the base delay.");
    }

    [Test]
    public void EachAttempt_DoublesUntilCap()
    {
        float previous = AdRetryPolicy.NextDelaySeconds(0);
        for (int attempt = 1; attempt < 20; attempt++)
        {
            float current = AdRetryPolicy.NextDelaySeconds(attempt);
            float expected = System.Math.Min(
                BalanceConfig.AdRetryBaseDelaySeconds * (float)System.Math.Pow(2, attempt),
                BalanceConfig.AdRetryMaxDelaySeconds);
            Assert.AreEqual(expected, current,
                $"Attempt {attempt} must be min(base * 2^attempt, max).");
            Assert.GreaterOrEqual(current, previous,
                "The curve is monotonically non-decreasing.");
            previous = current;
        }
    }

    [Test]
    public void LargeAttempt_ClampedAtMaxDelay()
    {
        Assert.AreEqual(BalanceConfig.AdRetryMaxDelaySeconds,
            AdRetryPolicy.NextDelaySeconds(50),
            "Far past the cap the delay holds at AdRetryMaxDelaySeconds.");
        Assert.AreEqual(BalanceConfig.AdRetryMaxDelaySeconds,
            AdRetryPolicy.NextDelaySeconds(int.MaxValue),
            "Even int.MaxValue attempts must not overflow past the cap.");
    }

    [Test]
    public void NegativeAttempt_TreatedAsAttempt0()
    {
        Assert.AreEqual(BalanceConfig.AdRetryBaseDelaySeconds,
            AdRetryPolicy.NextDelaySeconds(-3),
            "Negative attempts are defensive-clamped to the base delay.");
    }
}
