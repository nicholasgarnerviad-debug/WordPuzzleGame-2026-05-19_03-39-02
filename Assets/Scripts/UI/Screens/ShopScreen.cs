using UnityEngine;
using UnityEngine.UI;

public class ShopScreen : MonoBehaviour
{
    [SerializeField] private Button coinPack50Button;
    [SerializeField] private Button coinPack150Button;
    [SerializeField] private Button coinPack500Button;
    [SerializeField] private Button premiumButton;
    [SerializeField] private Text coinBalanceText;
    [SerializeField] private Button closeButton;

    private CoinSystem coinSystem;
    private IAPManager iapManager;

    private void Start()
    {
        coinSystem = FindObjectOfType<CoinSystem>();
        iapManager = IAPManager.Instance;

        coinPack50Button.onClick.AddListener(() => BuyCoinPack("com.yourname.wordpuzzle.coins_50"));
        coinPack150Button.onClick.AddListener(() => BuyCoinPack("com.yourname.wordpuzzle.coins_150"));
        coinPack500Button.onClick.AddListener(() => BuyCoinPack("com.yourname.wordpuzzle.coins_500"));
        premiumButton.onClick.AddListener(() => BuyPremium());
        closeButton.onClick.AddListener(() => CloseShop());

        coinSystem.CoinsChanged += UpdateCoinDisplay;
        UpdateCoinDisplay(coinSystem.GetBalance());
    }

    private void BuyCoinPack(string productId)
    {
        iapManager.BuyProduct(productId);
    }

    private void BuyPremium()
    {
        iapManager.BuyProduct("com.yourname.wordpuzzle.premium");
    }

    private void UpdateCoinDisplay(int balance)
    {
        coinBalanceText.text = $"Coins: {balance}";
    }

    private void CloseShop()
    {
        gameObject.SetActive(false);
    }
}
