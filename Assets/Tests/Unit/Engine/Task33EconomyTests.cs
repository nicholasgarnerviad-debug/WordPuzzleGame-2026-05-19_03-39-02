using NUnit.Framework;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using WordPuzzle.State;
using WordPuzzle.Persistence;

// Task 33E — regression tests for the shop economy: starting inventory, daily grant, coin->power-up
// buy, can't-afford guard, mock store (coin bundle + remove-ads), and save migration.
[TestFixture]
public class Task33EconomyTests
{
    private EconomyManager manager;
    private MockDataManager mockDataManager;

    [SetUp]
    public void Setup()
    {
        mockDataManager = new MockDataManager();
        manager = new EconomyManager(mockDataManager);
    }

    private static List<StoreProduct> SampleProducts() => new List<StoreProduct>
    {
        new StoreProduct { id = "coins_50",       type = StoreProductType.Coins,    coins = 50, priceUsd = 0.99f, displayName = "50 Coins" },
        new StoreProduct { id = "premium_no_ads", type = StoreProductType.RemoveAds, coins = 0,  priceUsd = 2.99f, displayName = "Remove Ads" },
    };

    // ── Starting inventory ────────────────────────────────────────────────────
    [Test]
    public async Task StartingInventory_GrantsFiveEachOnce()
    {
        await manager.InitializeAsync();
        await manager.ApplyStartingInventoryIfNeeded();

        int g = BalanceConfig.StartingPowerUpGrant;
        Assert.AreEqual(g, await manager.GetHintsAsync());
        Assert.AreEqual(g, await manager.GetRevealsAsync());
        Assert.AreEqual(g, await manager.GetUndosAsync());
        Assert.AreEqual(g, await manager.GetTimePowerUpsAsync());
        Assert.IsTrue(manager.GetCurrentProgress().startingGrantApplied);
    }

    [Test]
    public async Task StartingInventory_NotReappliedOnSecondCall()
    {
        await manager.InitializeAsync();
        await manager.ApplyStartingInventoryIfNeeded(); // 5 each
        await manager.UseHintAsync();                   // 4 hints

        await manager.ApplyStartingInventoryIfNeeded(); // must be a no-op (already applied)

        Assert.AreEqual(BalanceConfig.StartingPowerUpGrant - 1, await manager.GetHintsAsync(),
            "Second starting-grant call must not top hints back up.");
    }

    [Test]
    public async Task StartingInventory_TopsUpButNeverReducesARicherSave()
    {
        await manager.InitializeAsync();
        await manager.AddHintsAsync(10, "pre-existing");   // simulate an existing save with extra hints

        await manager.ApplyStartingInventoryIfNeeded();

        Assert.AreEqual(10, await manager.GetHintsAsync(), "Richer counts must not be reduced.");
        Assert.AreEqual(BalanceConfig.StartingPowerUpGrant, await manager.GetRevealsAsync(),
            "Empty counts must be topped up to the starting amount.");
    }

    // ── Daily grant ───────────────────────────────────────────────────────────
    [Test]
    public async Task DailyGrant_AddsTwoEachPerNewDayAndIsIdempotentSameDay()
    {
        await manager.InitializeAsync();
        int baseHints = await manager.GetHintsAsync();
        int g = BalanceConfig.DailyPowerUpGrant;

        await manager.GrantDailyIfDue("2026-06-03");
        Assert.AreEqual(baseHints + g, await manager.GetHintsAsync());
        Assert.AreEqual(baseHints + g, await manager.GetTimePowerUpsAsync());

        await manager.GrantDailyIfDue("2026-06-03"); // same day — no-op
        Assert.AreEqual(baseHints + g, await manager.GetHintsAsync(), "Daily grant must not stack same day.");

        await manager.GrantDailyIfDue("2026-06-04"); // new day — grants again
        Assert.AreEqual(baseHints + 2 * g, await manager.GetHintsAsync());
    }

    // ── Time power-up round-trip ───────────────────────────────────────────────
    [Test]
    public async Task TimePowerUp_AddThenUse_RoundTrips()
    {
        await manager.InitializeAsync();
        await manager.AddTimePowerUpsAsync(3, "test");
        Assert.AreEqual(3, await manager.GetTimePowerUpsAsync());

        await manager.UseTimePowerUpAsync();
        Assert.AreEqual(2, await manager.GetTimePowerUpsAsync());
    }

    // ── Coin -> power-up buy (the shop's coin transaction) ─────────────────────
    [Test]
    public async Task BuyPowerUp_SpendsCoinsAndAddsHints()
    {
        await manager.InitializeAsync();
        await manager.AddCoinsAsync(100, "test");

        bool ok = await manager.SpendCoinsAsync(25, "buy_hints");
        Assert.IsTrue(ok);
        await manager.AddHintsAsync(5, "buy_hints");

        Assert.AreEqual(75, await manager.GetCoinsAsync());
        Assert.AreEqual(5, await manager.GetHintsAsync());
    }

    [Test]
    public async Task BuyPowerUp_CantAfford_IsBlockedAndCoinsUnchanged()
    {
        await manager.InitializeAsync();
        await manager.AddCoinsAsync(10, "test");

        bool ok = await manager.SpendCoinsAsync(25, "buy_reveals");

        Assert.IsFalse(ok, "Spend must fail when the balance is insufficient.");
        Assert.AreEqual(10, await manager.GetCoinsAsync(), "Coins must be unchanged on a failed spend (never negative).");
    }

    // ── Mock store (real-money path, no billing) ───────────────────────────────
    [Test]
    public async Task MockStore_CoinBundle_AddsCoinsOnceOnSuccess()
    {
        await manager.InitializeAsync();
        var store = new MockStoreService(manager, SampleProducts());

        var outcome = await store.PurchaseAsync("coins_50");

        Assert.AreEqual(PurchaseOutcome.Success, outcome);
        Assert.AreEqual(50, await manager.GetCoinsAsync());
    }

    [Test]
    public async Task MockStore_CoinBundle_CancelledGrantsNothing()
    {
        await manager.InitializeAsync();
        var store = new MockStoreService(manager, SampleProducts());
        store.nextPurchaseCancelled = true;

        var outcome = await store.PurchaseAsync("coins_50");

        Assert.AreEqual(PurchaseOutcome.Cancelled, outcome);
        Assert.AreEqual(0, await manager.GetCoinsAsync(), "A cancelled purchase must grant nothing.");
    }

    [Test]
    public async Task MockStore_RemoveAds_SetsFlag_NotifiesPolicy_AndReportsOwned()
    {
        await manager.InitializeAsync();
        bool policyNotified = false;
        var store = new MockStoreService(manager, SampleProducts(), () => policyNotified = true);

        var outcome = await store.PurchaseAsync("premium_no_ads");

        Assert.AreEqual(PurchaseOutcome.Success, outcome);
        Assert.IsTrue(manager.GetCurrentProgress().removeAds, "remove-ads flag must be set.");
        Assert.IsTrue(policyNotified, "ad policy must be notified so interstitials are disabled.");
        Assert.IsTrue(store.IsOwned("premium_no_ads"));
    }

    [Test]
    public async Task MockStore_RemoveAds_SecondPurchaseIsAlreadyOwned()
    {
        await manager.InitializeAsync();
        var store = new MockStoreService(manager, SampleProducts());

        await store.PurchaseAsync("premium_no_ads");
        var outcome = await store.PurchaseAsync("premium_no_ads");

        Assert.AreEqual(PurchaseOutcome.AlreadyOwned, outcome, "remove-ads must never be double-granted.");
    }

    // ── Save migration ─────────────────────────────────────────────────────────
    [Test]
    public void Migration_OldSaveJson_DefaultsNewFieldsSafely()
    {
        // A PlayerProgressData written before Task 33 — no totalTimeEarned / removeAds /
        // startingGrantApplied / lastDailyGrantDate fields.
        const string oldJson = "{\"totalCoins\":120,\"totalHintsEarned\":3,\"totalRevealsEarned\":1,\"totalUndosEarned\":0}";

        var data = JsonUtility.FromJson<PlayerProgressData>(oldJson);

        Assert.AreEqual(120, data.totalCoins);
        Assert.AreEqual(0, data.totalTimeEarned, "missing time inventory defaults to 0");
        Assert.IsFalse(data.removeAds, "missing remove-ads defaults to false");
        Assert.IsFalse(data.startingGrantApplied,
            "missing startingGrantApplied defaults to false so the 5-each grant applies once on next boot");
    }
}
