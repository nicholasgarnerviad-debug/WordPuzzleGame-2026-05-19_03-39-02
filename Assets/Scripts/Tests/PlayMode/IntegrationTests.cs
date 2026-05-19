using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class IntegrationTests
{
    [UnityTest]
    public IEnumerator MainMenuLoadScene_NoErrors()
    {
        SceneManager.LoadScene("MainMenu");
        yield return new WaitForSeconds(2f);

        Assert.IsNotNull(UIManager.Instance);
    }

    [UnityTest]
    public IEnumerator ClassicModeGameflow_Complete()
    {
        SceneManager.LoadScene("ClassicMode");
        yield return new WaitForSeconds(1f);

        GameController gameController = Object.FindObjectOfType<GameController>();
        ClassicMode classicMode = Object.FindObjectOfType<ClassicMode>();

        Assert.IsNotNull(gameController);
        Assert.IsNotNull(classicMode);

        classicMode.StartGame();
        yield return new WaitForSeconds(1f);

        PuzzleData puzzle = gameController.GetCurrentPuzzle();
        foreach (var word in puzzle.words)
        {
            gameController.SubmitWord(word);
        }

        Assert.IsTrue(gameController.IsCurrentPuzzleComplete());
        Assert.Greater(classicMode.GetCoinsEarned(), 0);
    }

    [UnityTest]
    public IEnumerator CoinSystem_EarnAndSpend()
    {
        SceneManager.LoadScene("MainMenu");
        yield return new WaitForSeconds(1f);

        CoinSystem coinSystem = Object.FindObjectOfType<CoinSystem>();

        coinSystem.SetBalance(0);
        coinSystem.AddCoins(100);
        Assert.AreEqual(100, coinSystem.GetBalance());

        bool spent = coinSystem.SpendCoins(30);
        Assert.IsTrue(spent);
        Assert.AreEqual(70, coinSystem.GetBalance());

        coinSystem.SaveCoinsToStorage();
        yield return null;

        int saved = PlayerPrefs.GetInt("Coins");
        Assert.AreEqual(70, saved);
    }
}
