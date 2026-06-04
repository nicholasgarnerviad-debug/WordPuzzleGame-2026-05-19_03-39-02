using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace WordPuzzle.State
{
    /// <summary>
    /// Task 33D — placeholder for the REAL platform billing implementation. NOT IMPLEMENTED.
    ///
    /// Wiring real-money purchases requires:
    ///   • Unity IAP (com.unity.purchasing) added to the project,
    ///   • products configured in the App Store Connect / Google Play console matching the ids in
    ///     coin_shop.json (coins_50, coins_150, coins_500, premium_no_ads),
    ///   • receipt validation, and a device build (IAP cannot be exercised in the Editor).
    ///
    /// Until that work is done this stub always returns Failed, so nothing is ever granted from a
    /// non-functional path. The app deliberately uses <see cref="MockStoreService"/> in the Editor.
    /// Swap this in (behind the same IStoreService) when real billing is ready — the shop UI/flow are
    /// already written against the interface, so no UI changes are needed.
    /// </summary>
    public class PlatformStoreServiceStub : IStoreService
    {
        private readonly List<StoreProduct> products;

        public PlatformStoreServiceStub(IReadOnlyList<StoreProduct> products)
        {
            this.products = products != null ? new List<StoreProduct>(products) : new List<StoreProduct>();
        }

        public Task InitializeAsync()
        {
            Debug.LogWarning("[PlatformStoreServiceStub] Real billing is NOT implemented — the Editor uses " +
                             "MockStoreService. TODO: integrate Unity IAP + store-console products + receipt validation.");
            return Task.CompletedTask;
        }

        public IReadOnlyList<StoreProduct> Products => products;

        public bool IsOwned(string productId) => false; // TODO: query platform entitlements / restore purchases

        public Task<PurchaseOutcome> PurchaseAsync(string productId)
            => Task.FromResult(PurchaseOutcome.Failed); // TODO: real platform purchase + receipt validation

        public Task RestorePurchasesAsync()
        {
            Debug.LogWarning("[PlatformStoreServiceStub] RestorePurchases is NOT implemented — " +
                             "TODO: query platform entitlements and re-grant owned non-consumables.");
            return Task.CompletedTask; // TODO: real platform restore API
        }
    }
}
