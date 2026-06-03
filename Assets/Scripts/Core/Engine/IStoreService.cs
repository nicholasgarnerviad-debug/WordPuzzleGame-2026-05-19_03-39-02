using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace WordPuzzle.State
{
    /// <summary>Real-money product kind. Coin bundles are consumable; RemoveAds is a one-time non-consumable.</summary>
    public enum StoreProductType { Coins, RemoveAds }

    /// <summary>Outcome of a real-money purchase attempt. A product is granted ONLY on Success.</summary>
    public enum PurchaseOutcome { Success, Cancelled, Failed, AlreadyOwned, NotFound }

    /// <summary>A purchasable real-money product (defined in Resources/Data/coin_shop.json).</summary>
    [Serializable]
    public class StoreProduct
    {
        public string id;
        public StoreProductType type;
        public int coins;          // Coins type: how many coins this bundle grants
        public float priceUsd;
        public string displayName;
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
    }

    /// <summary>Loads the real-money product catalog from Resources/Data/coin_shop.json (single source of truth).</summary>
    public static class ShopCatalog
    {
        [Serializable] private class CoinShopData { public CoinPackData[] coinPacks; public PremiumData premium; }
        [Serializable] private class CoinPackData { public string id; public int coins; public float price; public string currency; }
        [Serializable] private class PremiumData  { public string id; public string name; public float price; public string currency; }

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
                        displayName = $"{p.coins} Coins"
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

            return list;
        }
    }
}
