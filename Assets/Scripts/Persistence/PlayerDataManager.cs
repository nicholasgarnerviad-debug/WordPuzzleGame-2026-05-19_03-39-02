using UnityEngine;

public class PlayerDataManager : MonoBehaviour
{
    public static PlayerDataManager Instance { get; private set; }

    [System.Serializable]
    public class PlayerData
    {
        public int coins;
        public bool isPremium;
        public int classicBestScore;
        public int puzzleShowTierUnlocked;
        public int timeAttackBestStreak;
    }

    private PlayerData playerData;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        LoadPlayerData();
    }

    public void LoadPlayerData()
    {
        playerData = new PlayerData
        {
            coins = PlayerPrefs.GetInt("Coins", 0),
            isPremium = PlayerPrefs.GetInt("Premium", 0) == 1,
            classicBestScore = PlayerPrefs.GetInt("ClassicBestScore", 0),
            puzzleShowTierUnlocked = PlayerPrefs.GetInt("PuzzleShowTier", 1),
            timeAttackBestStreak = PlayerPrefs.GetInt("TimeAttackStreak", 0)
        };
        Logger.Log("Player data loaded");
    }

    public void SavePlayerData()
    {
        PlayerPrefs.SetInt("Coins", playerData.coins);
        PlayerPrefs.SetInt("Premium", playerData.isPremium ? 1 : 0);
        PlayerPrefs.SetInt("ClassicBestScore", playerData.classicBestScore);
        PlayerPrefs.SetInt("PuzzleShowTier", playerData.puzzleShowTierUnlocked);
        PlayerPrefs.SetInt("TimeAttackStreak", playerData.timeAttackBestStreak);
        PlayerPrefs.Save();
        Logger.Log("Player data saved");
    }

    public PlayerData GetData() => playerData;
    public void UpdateData(PlayerData newData) => playerData = newData;
}
