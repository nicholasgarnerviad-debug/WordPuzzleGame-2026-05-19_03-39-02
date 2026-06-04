using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace WordPuzzle.State
{
    /// <summary>Real-money product kind. Coin bundles are consumable; RemoveAds and the one-time
    /// StarterPack are non-consumables (restorable, never re-granted as coins).</summary>
    public enum StoreProductType { Coins, RemoveAds, StarterPack }

    /// <summary>Outcome of a real-money purchase attempt. A product is granted ONLY on Success.</summary>
    public enum PurchaseOutcome { Success, Cancelled, Failed, AlreadyOwned, NotFound }

    /// <summary>A purchasable real-money product (defined in Resources/Data/coin_shop.json).</summary>
    [Serializable]
    public class StoreProduct
    {
        public string id;
        public StoreProductType type;
        public int coins;          // Coins / StarterPack: how many coins this product grants
        public float priceUsd;
        public string displayName;
        public string badge;       // Coins: optional merchandising tag ("MOST POPULAR" / "BEST VALUE"); "" = none
        public int powerUpsEach;   // StarterPack: count of EACH power-up (hint/undo/reveal/time) granted
        public int adFreeDays;     // StarterPack: length of the temporary ad-free window, in days
    }

    /// <summary>
    /// Task 33D — abstraction over REAL-MONEY purchases (coin bundles + remove-ads). The Editor/tests
    /// use <see cref="MockStoreService"/> (grants immediately, no billing) so the shop FLOW is testable;
    /// the real platform implementation (<see cref="PlatformStoreServiceStub"/>) is a clearly-marked TODO
    /// that requires Unity IAP + store-console product setup + a device. Coin-priced power-up bundles do
    /// NOT go through this — those are direct EconomyManager.SpendCoinsAsync calls.
    /// </summary>
    public interface IStoreService
    {
        Task InitializeAsync();
        IReadOnlyList<StoreProduct> Products { get; }
        /// <summary>True for an owned non-consumable (remove-ads). Always false for consumable coin bundles.</summary>
        bool IsOwned(string productId);
        Task<PurchaseOutcome> PurchaseAsync(string productId);

        /// <summary>
        /// Re-establish entitlements for owned NON-consumables (remove-ads, starter-pack) after a
        /// reinstall or device change. Never re-grants consumables (coins). The Editor mock re-applies
        /// the side effects of the persisted ownership flags; the platform impl queries the store's
        /// restore API. Required by app-store policy whenever non-consumables are sold.
        /// </summary>
        Task RestorePurchasesAsync();
    }

    /// <summary>Loads the real-money product catalog from Resources/Data/coin_shop.json (single source of truth).</summary>
    public static class ShopCatalog
    {
        [Serializable] private class CoinShopData { public CoinPackData[] coinPacks; public PremiumData premium; public StarterPackData starterPack; }
        [Serializable] private class CoinPackData { public string id; public int coins; public float price; public string currency; public string name; public string badge; }
        [Serializable] private class PremiumData  { public string id; public string name; public float price; public string currency; }
        [Serializable] private class StarterPackData { public string id; public string name; public float price; public string currency; public int coins; public int powerUpsEach; public int adFreeDays; }

        public static List<StoreProduct> Load()
        {
            var list = new List<StoreProduct>();
            var ta = Resources.Load<TextAsset>("Data/coin_shop");
            if (ta == null)
            {
                Debug.LogWarning("[ShopCatalog] Resources/Data/coin_shop.json not found.");
                return list;
            }

            CoinShopData data = null;
            try { data = JsonUtility.FromJson<CoinShopData>(ta.text); }
            catch (Exception e) { Debug.LogWarning($"[ShopCatalog] parse failed: {e.Message}"); }
            if (data == null) return list;

            if (data.coinPacks != null)
            {
                foreach (var p in data.coinPacks)
                {
                    if (p == null || string.IsNullOrEmpty(p.id)) continue;
                    list.Add(new StoreProduct
                    {
                        id = p.id,
                        type = StoreProductType.Coins,
                        coins = p.coins,
                        priceUsd = p.price,
                        displayName = string.IsNullOrEmpty(p.name) ? $"{p.coins} Coins" : p.name,
                        badge = p.badge
                    });
                }
            }

            if (data.premium != null && !string.IsNullOrEmpty(data.premium.id))
            {
                list.Add(new StoreProduct
                {
                    id = data.premium.id,
                    type = StoreProductType.RemoveAds,
                    coins = 0,
                    priceUsd = data.premium.price,
                    displayName = string.IsNullOrEmpty(data.premium.name) ? "Remove Ads" : data.premium.name
                });
            }

            if (data.starterPack != null && !string.IsNullOrEmpty(data.starterPack.id))
            {
                list.Add(new StoreProduct
                {
                    id = data.starterPack.id,
                    type = StoreProductType.StarterPack,
                    coins = data.starterPack.coins,
                    priceUsd = data.starterPack.price,
                    displayName = string.IsNullOrEmpty(data.starterPack.name) ? "Starter Pack" : data.starterPack.name,
                    powerUpsEach = data.starterPack.powerUpsEach,
                    adFreeDays = data.starterPack.adFreeDays
                });
            }

            return list;
        }
    }
}
