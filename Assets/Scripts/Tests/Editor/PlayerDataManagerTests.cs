using NUnit.Framework;
using UnityEngine;

public class PlayerDataManagerTests
{
    [SetUp]
    public void Setup()
    {
        PlayerPrefs.DeleteAll();
    }

    [Test]
    public void LoadPlayerData_DefaultValues()
    {
        var manager = ScriptableObject.CreateInstance<PlayerDataManager>();
        manager.LoadPlayerData();

        var data = manager.GetData();
        Assert.AreEqual(0, data.coins);
        Assert.AreEqual(false, data.isPremium);
    }

    [Test]
    public void SavePlayerData_PersistsToPrefs()
    {
        var manager = ScriptableObject.CreateInstance<PlayerDataManager>();
        var data = manager.GetData();
        data.coins = 500;
        data.isPremium = true;
        manager.SavePlayerData();

        Assert.AreEqual(500, PlayerPrefs.GetInt("Coins"));
        Assert.AreEqual(1, PlayerPrefs.GetInt("Premium"));
    }

    [Test]
    public void LoadPlayerData_RestoresFromPrefs()
    {
        PlayerPrefs.SetInt("Coins", 250);
        PlayerPrefs.SetInt("Premium", 1);

        var manager = ScriptableObject.CreateInstance<PlayerDataManager>();
        manager.LoadPlayerData();
        var data = manager.GetData();

        Assert.AreEqual(250, data.coins);
        Assert.AreEqual(true, data.isPremium);
    }
}
