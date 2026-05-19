using NUnit.Framework;
using UnityEngine;

public class CoinSystemTests
{
    private GameObject gameObject;
    private CoinSystem coinSystem;

    [SetUp]
    public void Setup()
    {
        gameObject = new GameObject();
        coinSystem = gameObject.AddComponent<CoinSystem>();
    }

    [TearDown]
    public void Teardown()
    {
        Object.DestroyImmediate(gameObject);
    }

    [Test]
    public void AddCoins_IncreaseBalance()
    {
        coinSystem.SetBalance(0);
        coinSystem.AddCoins(50);

        Assert.AreEqual(50, coinSystem.GetBalance());
    }

    [Test]
    public void SpendCoins_DecreaseBalance()
    {
        coinSystem.SetBalance(100);
        bool result = coinSystem.SpendCoins(30);

        Assert.IsTrue(result);
        Assert.AreEqual(70, coinSystem.GetBalance());
    }

    [Test]
    public void SpendCoins_InsufficientFunds_ReturnsFalse()
    {
        coinSystem.SetBalance(10);
        bool result = coinSystem.SpendCoins(50);

        Assert.IsFalse(result);
        Assert.AreEqual(10, coinSystem.GetBalance());
    }

    [Test]
    public void AddCoins_NegativeAmount_Ignored()
    {
        coinSystem.SetBalance(50);
        coinSystem.AddCoins(-10);

        Assert.AreEqual(50, coinSystem.GetBalance());
    }

    [Test]
    public void SetBalance_UpdatesBalance()
    {
        coinSystem.SetBalance(200);

        Assert.AreEqual(200, coinSystem.GetBalance());
    }
}
