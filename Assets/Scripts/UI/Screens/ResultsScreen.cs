using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ResultsScreen : MonoBehaviour
{
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text coinsEarnedText;
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
        // AdManager.Instance.ShowRewardedAd disabled - re-enable when AdMob is set up
        int bonusCoins = 10;
        coinSystem.AddCoins(bonusCoins);
        coinsEarnedText.text += $" (+{bonusCoins} bonus)";
        watchAdButton.interactable = false;
        Debug.Log("Rewarded ad completed");
    }

    private void PlayNextPuzzle()
    {
        Debug.Log("Loading next puzzle");
    }

    private void ReturnToMenu()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }
}