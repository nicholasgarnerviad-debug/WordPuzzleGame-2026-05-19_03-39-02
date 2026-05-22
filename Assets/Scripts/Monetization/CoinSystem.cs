using UnityEngine;

public class CoinSystem : MonoBehaviour
{
    private int coinBalance = 0;

    public delegate void OnCoinsChanged(int newBalance);
    public event OnCoinsChanged CoinsChanged;

    private void Start()
    {
        LoadCoinsFromStorage();
    }

    public void AddCoins(int amount)
    {
        if (amount < 0)
        {
            Debug.LogError("Cannot add negative coins");
            return;
        }

        coinBalance += amount;
        Debug.Log($"Added {amount} coins. Balance: {coinBalance}");
        CoinsChanged?.Invoke(coinBalance);
    }

    public bool SpendCoins(int amount)
    {
        if (amount < 0)
        {
            Debug.LogError("Cannot spend negative coins");
            return false;
        }

        if (coinBalance < amount)
        {
            Debug.LogWarning($"Insufficient coins. Have {coinBalance}, need {amount}");
            return false;
        }

        coinBalance -= amount;
        Debug.Log($"Spent {amount} coins. Balance: {coinBalance}");
        CoinsChanged?.Invoke(coinBalance);
        return true;
    }

    public int GetBalance() => coinBalance;

    public void SetBalance(int amount)
    {
        coinBalance = Mathf.Max(0, amount);
        CoinsChanged?.Invoke(coinBalance);
    }

    private void LoadCoinsFromStorage()
    {
        coinBalance = PlayerPrefs.GetInt("Coins", 0);
    }

    public void SaveCoinsToStorage()
    {
        PlayerPrefs.SetInt("Coins", coinBalance);
        PlayerPrefs.Save();
    }
}
