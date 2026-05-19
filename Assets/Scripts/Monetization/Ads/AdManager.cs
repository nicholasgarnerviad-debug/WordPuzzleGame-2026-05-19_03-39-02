using UnityEngine;
using GoogleMobileAds.Client;
using GoogleMobileAds.Common;

public class AdManager : MonoBehaviour
{
    public static AdManager Instance { get; private set; }

    [SerializeField] private string adMobAppId = "ca-app-pub-xxxxxxxxxxxxxxxx";
    [SerializeField] private string bannerAdId = "ca-app-pub-3940256099942544/6300978111"; // Test ID
    [SerializeField] private string interstitialAdId = "ca-app-pub-3940256099942544/1033173712"; // Test ID
    [SerializeField] private string rewardedAdId = "ca-app-pub-3940256099942544/5224354917"; // Test ID

    private BannerView bannerView;
    private InterstitialAd interstitialAd;
    private RewardedAd rewardedAd;
    private int gamesSinceLastInterstitial = 0;

    public delegate void OnRewardEarned(int rewardAmount);
    public event OnRewardEarned RewardEarned;

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
        InitializeAds();
        LoadBannerAd();
        LoadInterstitialAd();
    }

    private void InitializeAds()
    {
        MobileAds.Initialize((InitializationStatus initStatus) => {
            Logger.Log("Google Mobile Ads initialized");
        });
    }

    private void LoadBannerAd()
    {
        if (bannerView != null)
        {
            bannerView.Destroy();
        }

        var adRequest = new AdRequest();
        adRequest.Keywords.Add("unity-admob-sample");

        bannerView = new BannerView(bannerAdId, AdSize.Banner, AdPosition.Bottom);
        bannerView.LoadAd(adRequest);

        bannerView.OnAdLoaded += HandleBannerLoaded;
        bannerView.OnAdFailedToLoad += HandleBannerFailedToLoad;

        Logger.Log("Banner ad request created");
    }

    private void HandleBannerLoaded()
    {
        Logger.Log("Banner ad loaded");
    }

    private void HandleBannerFailedToLoad(LoadAdError error)
    {
        Logger.LogError($"Banner failed to load: {error}");
    }

    public void ShowInterstitialAd()
    {
        gamesSinceLastInterstitial++;

        if (gamesSinceLastInterstitial < 3)
            return; // Show every 3 games

        if (interstitialAd != null && interstitialAd.CanShowAd())
        {
            interstitialAd.Show();
            gamesSinceLastInterstitial = 0;
        }
        else
        {
            Logger.LogWarning("Interstitial ad not ready");
            LoadInterstitialAd();
        }
    }

    private void LoadInterstitialAd()
    {
        var adRequest = new AdRequest();

        InterstitialAd.Load(interstitialAdId, adRequest,
            (InterstitialAd ad, LoadAdError error) => {
                if (error != null || ad == null)
                {
                    Logger.LogError($"Interstitial failed to load: {error}");
                    return;
                }

                interstitialAd = ad;
                Logger.Log("Interstitial ad loaded");
            });
    }

    public void ShowRewardedAd(System.Action onRewardEarned)
    {
        if (rewardedAd != null && rewardedAd.CanShowAd())
        {
            rewardedAd.Show((Reward reward) => {
                Logger.Log($"Reward earned: {reward.Amount} {reward.Type}");
                onRewardEarned?.Invoke();
            });
        }
        else
        {
            Logger.LogWarning("Rewarded ad not ready");
            LoadRewardedAd();
        }
    }

    private void LoadRewardedAd()
    {
        var adRequest = new AdRequest();

        RewardedAd.Load(rewardedAdId, adRequest,
            (RewardedAd ad, LoadAdError error) => {
                if (error != null || ad == null)
                {
                    Logger.LogError($"Rewarded ad failed to load: {error}");
                    return;
                }

                rewardedAd = ad;
                Logger.Log("Rewarded ad loaded");
            });
    }

    private void OnDestroy()
    {
        bannerView?.Destroy();
    }
}
