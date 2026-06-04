using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using WordPuzzle.Persistence;
using WordPuzzle.State;

/// <summary>
/// Task 36 Phase 5 (36J) — one-time Starter Pack + temporary ad-free window + Restore.
/// Verifies the grant is idempotent (no double coins/power-ups on a repeat buy or a restore) and
/// that the ad-free window is honored relative to an injected "now". Mirrors the async + MockDataManager
/// harness used by Task33EconomyTests.
/// </summary>
[TestFixture]
public class StarterPackTests
{
    private MockDataManager data;
    private EconomyManager economy;

    [SetUp]
    public void Setup()
    {
        data = new MockDataManager();
        economy = new EconomyManager(data);
    }

    private static List<StoreProduct> StarterCatalog() => new List<StoreProduct>
    {
        new StoreProduct
        {
            id = "starter_pack", type = StoreProductType.StarterPack,
            coins = 1000, powerUpsEach = 5, adFreeDays = 3, priceUsd = 1.99f, displayName = "Starter Pack"
        },
    };

    // ── EconomyManager grant logic ────────────────────────────────────────────
    [Test]
    public async Task GrantStarterPack_AddsCoinsPowerUpsAndAdFreeWindow()
    {
        await economy.InitializeAsync();
        const long until = 1_000_000L;
        await economy.GrantStarterPackAsync(1000, 5, until);

        var prog = economy.GetCurrentProgress();
        Assert.IsTrue(prog.starterPackOwned);
        Assert.AreEqual(1000, prog.totalCoins);
        Assert.AreEqual(5, prog.totalHintsEarned);
        Assert.AreEqual(5, prog.totalRevealsEarned);
        Assert.AreEqual(5, prog.totalUndosEarned);
        Assert.AreEqual(5, prog.totalTimeEarned);
        Assert.AreEqual(until, prog.adFreeUntilUnix);

        Assert.IsTrue(economy.IsAdFreeActive(until - 1), "window is active before its expiry");
        Assert.IsFalse(economy.IsAdFreeActive(until),     "window is expired AT its boundary (strictly-after)");
        Assert.IsFalse(economy.IsAdFreeActive(until + 1));
    }

    [Test]
    public async Task GrantStarterPack_IsIdempotent_NeverDoubleGrants()
    {
        await economy.InitializeAsync();
        await economy.GrantStarterPackAsync(1000, 5, 1000L);
        await economy.GrantStarterPackAsync(1000, 5, 9999L); // second call must be a no-op

        var prog = economy.GetCurrentProgress();
        Assert.AreEqual(1000, prog.totalCoins, "coins must not be granted twice");
        Assert.AreEqual(5, prog.totalHintsEarned, "power-ups must not be granted twice");
        Assert.AreEqual(1000L, prog.adFreeUntilUnix, "an owned pack must not extend the window again");
        Assert.IsTrue(await economy.GetStarterPackOwnedAsync());
    }

    // ── MockStoreService purchase + restore flow ──────────────────────────────
    [Test]
    public async Task MockStore_StarterPack_GrantsOnceThenAlreadyOwned()
    {
        await economy.InitializeAsync();
        bool adSuppressFired = false;
        var store = new MockStoreService(economy, StarterCatalog(),
            onRemoveAdsGranted: null, onStarterPackGranted: () => adSuppressFired = true);

        var first = await store.PurchaseAsync("starter_pack");
        Assert.AreEqual(PurchaseOutcome.Success, first);
        Assert.IsTrue(adSuppressFired, "ad-suppress callback should fire when the window opens");
        Assert.IsTrue(store.IsOwned("starter_pack"));

        var second = await store.PurchaseAsync("starter_pack");
        Assert.AreEqual(PurchaseOutcome.AlreadyOwned, second);

        Assert.AreEqual(1000, economy.GetCurrentProgress().totalCoins,
            "coins granted exactly once across two purchase attempts");
    }

    [Test]
    public async Task Restore_DoesNotRegrantCoins_ButReappliesAdSuppress()
    {
        await economy.InitializeAsync();
        int adSuppressCount = 0;
        var store = new MockStoreService(economy, StarterCatalog(),
            onRemoveAdsGranted: null, onStarterPackGranted: () => adSuppressCount++);

        await store.PurchaseAsync("starter_pack");
        int coinsAfterBuy = economy.GetCurrentProgress().totalCoins;

        await store.RestorePurchasesAsync();
        Assert.AreEqual(coinsAfterBuy, economy.GetCurrentProgress().totalCoins,
            "restore must NOT re-grant consumable coins");
        Assert.AreEqual(2, adSuppressCount,
            "ad-suppress fires once on purchase and once again on restore for the owned pack");
    }

    [Test]
    public async Task AdFree_InactiveByDefault_OnAFreshSave()
    {
        await economy.InitializeAsync();
        Assert.IsFalse(economy.IsAdFreeActive(System.DateTimeOffset.UtcNow.ToUnixTimeSeconds()));
        Assert.IsFalse(await economy.GetStarterPackOwnedAsync());
    }
}
