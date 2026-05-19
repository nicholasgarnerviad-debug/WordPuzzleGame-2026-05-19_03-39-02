using UnityEngine;
using UnityEngine.UI;

public class ResultsScreen : MonoBehaviour
{
    [SerializeField] private Text scoreText;
    [SerializeField] private Text coinsEarnedText;
    [SerializeField] private Button watchAdButton;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button menuButton;

    private CoinSystem coinSystem;
    private int baseCoinsEarned = 0;

    private void Start()
    {
        coinSystem = FindObjectOfType<CoinSystem>();
        watchAdButton.onClick.AddListener(() => WatchAdForCoins());
        nextButton.onClick.AddListener(() => PlayNextPuzzle());
        menuButton.onClick.AddListener(() => ReturnToMenu());
    }

    public void ShowResults(int score, int coinsEarned)
    {
        scoreText.text = $"Score: {score}";
        baseCoinsEarned = coinsEarned;
        coinsEarnedText.text = $"Coins: +{coinsEarned}";
        coinSystem.AddCoins(coinsEarned);
    }

    private void WatchAdForCoins()
    {
        AdManager.Instance.ShowRewardedAd(() => {
            int bonusCoins = 10;
            coinSystem.AddCoins(bonusCoins);
            coinsEarnedText.text += $" (+{bonusCoins} bonus)";
            watchAdButton.interactable = false;
            Logger.Log("Rewarded ad completed");
        });
    }

    private void PlayNextPuzzle()
    {
        Logger.Log("Loading next puzzle");
    }

    private void ReturnToMenu()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }
}
