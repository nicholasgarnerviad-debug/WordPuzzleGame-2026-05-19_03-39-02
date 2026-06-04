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
        private readonly Action onStarterPackGranted;   // fired when the starter pack's ad-free window opens
        private readonly List<StoreProduct> products;

        // Test hooks — set before a PurchaseAsync call to simulate a cancel / billing failure (grants NOTHING).
        public bool nextPurchaseCancelled = false;
        public bool nextPurchaseFails = false;

        public MockStoreService(IEconomyManager economy, IReadOnlyList<StoreProduct> products,
                                Action onRemoveAdsGranted = null, Action onStarterPackGranted = null)
        {
            this.economy = economy;
            this.onRemoveAdsGranted = onRemoveAdsGranted;
            this.onStarterPackGranted = onStarterPackGranted;
            this.products = products != null ? new List<StoreProduct>(products) : new List<StoreProduct>();
        }

        public Task InitializeAsync() => Task.CompletedTask;

        public IReadOnlyList<StoreProduct> Products => products;

        public bool IsOwned(string productId)
        {
            var p = Find(productId);
            if (p == null) return false;
            var prog = economy?.GetCurrentProgress();
            if (prog == null) return false;
            if (p.type == StoreProductType.RemoveAds)   return prog.removeAds;
            if (p.type == StoreProductType.StarterPack) return prog.starterPackOwned;
            return false; // consumable coin bundles are never "owned"
        }

        public async Task<PurchaseOutcome> PurchaseAsync(string productId)
        {
            var p = Find(productId);
            if (p == null) return PurchaseOutcome.NotFound;

            // Non-consumable already owned — never double-grant remove-ads or the starter pack.
            if ((p.type == StoreProductType.RemoveAds || p.type == StoreProductType.StarterPack) && IsOwned(productId))
                return PurchaseOutcome.AlreadyOwned;

            // Simulated user cancel / billing failure — grant NOTHING.
            if (nextPurchaseCancelled) { nextPurchaseCancelled = false; return PurchaseOutcome.Cancelled; }
            if (nextPurchaseFails)     { nextPurchaseFails = false;     return PurchaseOutcome.Failed; }

            // Success — grant exactly once.
            switch (p.type)
            {
                case StoreProductType.Coins:
                    if (economy != null) await economy.AddCoinsAsync(p.coins, "iap:" + p.id);
                    break;
                case StoreProductType.RemoveAds:
                    if (economy != null) await economy.SetRemoveAdsAsync(true);
                    onRemoveAdsGranted?.Invoke();
                    break;
                case StoreProductType.StarterPack:
                    // The ad-free window runs from "now" for the product's configured number of days.
                    long adFreeUntil = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + (long)p.adFreeDays * 86400L;
                    if (economy != null) await economy.GrantStarterPackAsync(p.coins, p.powerUpsEach, adFreeUntil);
                    onStarterPackGranted?.Invoke();
                    break;
            }

            return PurchaseOutcome.Success;
        }

        public Task RestorePurchasesAsync()
        {
            // Editor mock: entitlements live in the local save, so re-apply only the SIDE EFFECTS of any
            // owned non-consumables (suppress ads). Consumable coins are never re-granted.
            var prog = economy?.GetCurrentProgress();
            if (prog != null)
            {
                if (prog.removeAds)        onRemoveAdsGranted?.Invoke();
                if (prog.starterPackOwned) onStarterPackGranted?.Invoke();
            }
            return Task.CompletedTask;
        }

        private StoreProduct Find(string productId)
        {
            for (int i = 0; i < products.Count; i++)
                if (products[i] != null && products[i].id == productId) return products[i];
            return null;
        }
    }
}
