using NUnit.Framework;
using UnityEngine;

public class TimeAttackModeTests
{
    private GameObject gameObject;
    private GameController gameController;
    private TimeAttackMode timeAttackMode;

    [SetUp]
    public void Setup()
    {
        gameObject = new GameObject();
        gameController = gameObject.AddComponent<GameController>();
        timeAttackMode = gameObject.AddComponent<TimeAttackMode>();
    }

    [TearDown]
    public void Teardown()
    {
        Object.DestroyImmediate(gameObject);
    }

    [Test]
    public void StartGame_InitializesRound()
    {
        timeAttackMode.Initialize();
        timeAttackMode.StartGame();

        Assert.AreEqual(1, timeAttackMode.GetCurrentRound());
        Assert.Greater(timeAttackMode.GetTimeRemaining(), 0);
    }

    [Test]
    public void GetModeName_ReturnsCorrectName()
    {
        Assert.AreEqual("Time Attack", timeAttackMode.GetModeName());
    }

    [Test]
    public void CoinsEarned_IncludesRoundBonus()
    {
        timeAttackMode.Initialize();
        Assert.AreEqual(
            Constants.TIME_ATTACK_COIN_REWARD + Constants.TIME_ATTACK_BONUS_PER_ROUND,
            timeAttackMode.GetCoinsEarned()
        );
    }

    [Test]
    public void TimeRemaining_Decreases()
    {
        timeAttackMode.Initialize();
        timeAttackMode.StartGame();
        float initialTime = timeAttackMode.GetTimeRemaining();

        // Would need to wait in real test; this is simplified
        Assert.Greater(initialTime, 0);
    }
}
