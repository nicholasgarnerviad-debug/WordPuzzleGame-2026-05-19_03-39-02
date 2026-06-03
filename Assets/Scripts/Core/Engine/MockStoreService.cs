using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace WordPuzzle.State
{
    /// <summary>
    /// Task 33D — Editor/test store. "Purchases" succeed immediately and grant the product through the
    /// economy (coin bundle -> AddCoinsAsync; remove-ads -> SetRemoveAdsAsync(true) + onRemoveAdsGranted).
    /// This is NOT real billing — it exists so the shop flow is fully testable WITHOUT store setup or a
    /// device. The test hooks (nextPurchaseCancelled / nextPurchaseFails) exercise the no-grant paths.
    /// </summary>
    public class MockStoreService : IStoreService
    {
        private readonly IEconomyManager economy;
        private readonly Action onRemoveAdsGranted;
        private readonly List<StoreProduct> products;

        // Test hooks — set before a PurchaseAsync call to simulate a cancel / billing failure (grants NOTHING).
        public bool nextPurchaseCancelled = false;
        public bool nextPurchaseFails = false;

        public MockStoreService(IEconomyManager economy, IReadOnlyList<StoreProduct> products, Action onRemoveAdsGranted = null)
        {
            this.economy = economy;
            this.onRemoveAdsGranted = onRemoveAdsGranted;
            this.products = products != null ? new List<StoreProduct>(products) : new List<StoreProduct>();
        }

        public Task InitializeAsync() => Task.CompletedTask;

        public IReadOnlyList<StoreProduct> Products => products;

        public bool IsOwned(string productId)
        {
            var p = Find(productId);
            if (p == null || p.type != StoreProductType.RemoveAds) return false;
            var prog = economy?.GetCurrentProgress();
            return prog != null && prog.removeAds;
        }

        public async Task<PurchaseOutcome> PurchaseAsync(string productId)
        {
            var p = Find(productId);
            if (p == null) return PurchaseOutcome.NotFound;

            // Non-consumable already owned — never double-grant remove-ads.
            if (p.type == StoreProductType.RemoveAds && IsOwned(productId))
                return PurchaseOutcome.AlreadyOwned;

            // Simulated user cancel / billing failure — grant NOTHING.
            if (nextPurchaseCancelled) { nextPurchaseCancelled = false; return PurchaseOutcome.Cancelled; }
            if (nextPurchaseFails)     { nextPurchaseFails = false;     return PurchaseOutcome.Failed; }

            // Success — grant exactly once.
            if (p.type == StoreProductType.Coins)
            {
                if (economy != null) await economy.AddCoinsAsync(p.coins, "iap:" + p.id);
            }
            else // RemoveAds
            {
                if (economy != null) await economy.SetRemoveAdsAsync(true);
                onRemoveAdsGranted?.Invoke();
            }

            return PurchaseOutcome.Success;
        }

        private StoreProduct Find(string productId)
        {
            for (int i = 0; i < products.Count; i++)
                if (products[i] != null && products[i].id == productId) return products[i];
            return null;
        }
    }
}
