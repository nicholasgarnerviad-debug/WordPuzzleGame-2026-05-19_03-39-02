using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Security;

public class IAPManager : MonoBehaviour, IDetailedStoreListener
{
    public static IAPManager Instance { get; private set; }

    private IStoreController controller;
    private IExtensionProvider extensions;

    private const string PREMIUM_PRODUCT_ID = "com.yourname.wordpuzzle.premium";
    private const string COINS_50_ID = "com.yourname.wordpuzzle.coins_50";
    private const string COINS_150_ID = "com.yourname.wordpuzzle.coins_150";
    private const string COINS_500_ID = "com.yourname.wordpuzzle.coins_500";

    public delegate void OnPurchaseComplete(string productId);
    public event OnPurchaseComplete PurchaseComplete;

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
        if (controller == null)
        {
            InitializeIAP();
        }
    }

    private void InitializeIAP()
    {
        var builder = ConfigurationBuilder.Instance(StandardPricingStrategy.RetrievalStrategy());

        builder.AddProduct(PREMIUM_PRODUCT_ID, ProductType.NonConsumable);
        builder.AddProduct(COINS_50_ID, ProductType.Consumable);
        builder.AddProduct(COINS_150_ID, ProductType.Consumable);
        builder.AddProduct(COINS_500_ID, ProductType.Consumable);

        UnityPurchasing.Initialize(this, builder);
    }

    public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
    {
        Logger.Log("IAP initialized successfully");
        this.controller = controller;
        this.extensions = extensions;
    }

    public void OnInitializeFailed(InitializationFailureReason reason)
    {
        Logger.LogError($"IAP initialization failed: {reason}");
    }

    public void OnInitializeFailed(InitializationFailureReason reason, string message)
    {
        Logger.LogError($"IAP initialization failed: {reason} - {message}");
    }

    public void BuyProduct(string productId)
    {
        if (controller == null)
        {
            Logger.LogError("IAP not initialized");
            return;
        }

        Product product = controller.products.WithID(productId);
        if (product == null)
        {
            Logger.LogError($"Product not found: {productId}");
            return;
        }

        controller.InitiatePurchase(product);
        Logger.Log($"Initiating purchase: {productId}");
    }

    public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs e)
    {
        Logger.Log($"Purchase successful: {e.purchasedProduct.definition.id}");

        string productId = e.purchasedProduct.definition.id;
        HandleSuccessfulPurchase(productId);

        PurchaseComplete?.Invoke(productId);
        return PurchaseProcessingResult.Complete;
    }

    public void OnPurchaseFailed(Product product, PurchaseFailureReason failureReason)
    {
        Logger.LogError($"Purchase failed: {product.definition.id} - {failureReason}");
    }

    public void OnPurchaseFailed(Product product, PurchaseFailureDescription failureDescription)
    {
        Logger.LogError($"Purchase failed: {product.definition.id} - {failureDescription.reason}");
    }

    private void HandleSuccessfulPurchase(string productId)
    {
        CoinSystem coinSystem = FindObjectOfType<CoinSystem>();

        switch (productId)
        {
            case PREMIUM_PRODUCT_ID:
                PlayerPrefs.SetInt("Premium", 1);
                PlayerPrefs.Save();
                Logger.Log("Premium activated");
                break;
            case COINS_50_ID:
                coinSystem?.AddCoins(50);
                break;
            case COINS_150_ID:
                coinSystem?.AddCoins(150);
                break;
            case COINS_500_ID:
                coinSystem?.AddCoins(500);
                break;
        }
    }

    public bool IsPremium()
    {
        return PlayerPrefs.GetInt("Premium", 0) == 1;
    }
}
