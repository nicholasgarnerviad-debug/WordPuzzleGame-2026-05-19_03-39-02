using System.Collections.Generic;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using WordPuzzle.Persistence;
using WordPuzzle.UI;

// ============================================================
//  Task 45 — results & celebration juice.
//  Pins: (1) the pure value curves (CountUpValue / PopScale) —
//  exact endpoints, monotonic, EaseOutCubic-shaped; (2) the
//  ReduceMotion snap paths render final state immediately with
//  no running coroutine; (3) the Daily show-path driven twice:
//  fresh show animates (payout coroutine live), the re-show
//  renders AT REST with no label stacking (the BUG-2 class);
//  (4) celebratedTiers round-trips through JsonUtility and
//  defaults EMPTY for old saves; the once-ever tier decision is
//  pure. Feel (pacing, haptic landing) is the human gate.
// ============================================================
public class CelebrationJuiceTests
{
    private readonly List<GameObject> spawned = new List<GameObject>();
    private bool savedReduceMotion;

    [SetUp]
    public void SetUp() => savedReduceMotion = UIAnimations.ReduceMotion;

    [TearDown]
    public void TearDown()
    {
        UIAnimations.ReduceMotion = savedReduceMotion;
        foreach (var go in spawned)
            if (go != null) Object.DestroyImmediate(go);
        spawned.Clear();
    }

    private GameObject Spawn(string name)
    {
        var go = new GameObject(name, typeof(RectTransform));
        spawned.Add(go);
        return go;
    }

    // ── Pure value curves ────────────────────────────────────

    [Test]
    public void CountUpValue_EndpointsExact_MonotonicAndEasedOut()
    {
        Assert.AreEqual(0,   UIAnimations.CountUpValue(0, 120, 0f));
        Assert.AreEqual(120, UIAnimations.CountUpValue(0, 120, 1f));
        Assert.AreEqual(120, UIAnimations.CountUpValue(0, 120, 2f), "clamps past the end");

        int prev = -1;
        for (float t = 0f; t <= 1f; t += 0.05f)
        {
            int v = UIAnimations.CountUpValue(0, 120, t);
            Assert.GreaterOrEqual(v, prev, "count-up never goes backwards");
            prev = v;
        }

        // EaseOutCubic: the value leads linear time (fast start, decelerating).
        Assert.Greater(UIAnimations.CountUpValue(0, 120, 0.5f), 60,
            "ease-OUT means the midpoint value is past the linear midpoint");
    }

    [Test]
    public void PopScale_TroughToOne_NoOvershoot()
    {
        Assert.AreEqual(0.6f, UIAnimations.PopScale(0f), 1e-4f);
        Assert.AreEqual(1f,   UIAnimations.PopScale(1f), 1e-4f);
        Assert.AreEqual(0.6f, UIAnimations.PopScale(-1f), 1e-4f, "pre-start items wait at the trough");
        for (float t = 0f; t <= 1.5f; t += 0.05f)
            Assert.LessOrEqual(UIAnimations.PopScale(t), 1f + 1e-4f, "deliberate and weighted — no bounce");
    }

    // ── ReduceMotion snap paths ──────────────────────────────

    [Test]
    public void CountUpInt_ReduceMotion_SnapsToFormattedFinal()
    {
        UIAnimations.ReduceMotion = true;
        var label = Spawn("Label").AddComponent<TextMeshProUGUI>();
        label.text = "";

        var routine = UIAnimations.CountUpInt(label, 0, 100, format: "+{0} coins");
        Assert.IsFalse(routine.MoveNext(), "ReduceMotion returns immediately — nothing to iterate");
        Assert.AreEqual("+100 coins", label.text);
    }

    [Test]
    public void StaggeredPop_ReduceMotion_AllAtRestImmediately()
    {
        UIAnimations.ReduceMotion = true;
        var items = new RectTransform[3];
        for (int i = 0; i < 3; i++)
        {
            items[i] = (RectTransform)Spawn($"Item{i}").transform;
            items[i].localScale = Vector3.zero;
        }

        var routine = UIAnimations.StaggeredPop(items);
        Assert.IsFalse(routine.MoveNext());
        foreach (var rt in items)
            Assert.AreEqual(Vector3.one, rt.localScale, "everything visible at rest");
    }

    // ── The Daily show-path, twice (fresh animates / re-show at rest) ──

    [Test]
    public void DailyShowPath_FreshAnimates_ReShowRendersAtRest_NoTextStacking()
    {
        UIAnimations.ReduceMotion = false;
        var host = Spawn("Results");
        host.SetActive(true);
        var results = host.AddComponent<ResultsScreen>();

        // Fresh payout — the GameBootstrap chain shape: result → coins → doubler → streak.
        results.ShowDailyResult(2, 4, 5, false, 10, 3, usedPowerUp: false, animate: true);
        results.ShowDailyCoinReward(30);
        results.ConfigureDailyDoubler(false);
        results.ShowDailyStreak(3, 5, false);
        Assert.IsTrue(results.PayoutAnimating, "fresh show starts the payout coroutine");

        // Re-show (Task 38 recall) — must render at rest with no stacked text.
        results.ShowDailyResult(2, 4, 5, false, 10, 3, usedPowerUp: false, animate: false);
        results.ShowDailyCoinReward(30);
        results.ShowDailyStreak(3, 5, true);
        Assert.IsFalse(results.PayoutAnimating, "a recall is not a payout — no coroutine");

        // No label stacking on the dedicated lines (the BUG-2 regression class): the streak and
        // coin lines are SET each view, so a double show yields exactly one value, not appends.
        var streakLine = host.transform.Find("DailyStreakLine");
        Assert.IsNotNull(streakLine);
        var streakText = streakLine.GetComponent<TextMeshProUGUI>().text;
        Assert.AreEqual(streakText.IndexOf("Streak"), streakText.LastIndexOf("Streak"),
            "exactly one streak line per view");

        var coinLine = host.transform.Find("DailyCoinLine");
        Assert.IsNotNull(coinLine);
        Assert.AreEqual("+30 coins", coinLine.GetComponent<TextMeshProUGUI>().text,
            "the re-show renders the final coin value at rest");
    }

    [Test]
    public void DailyShowPath_ReduceMotion_NeverAnimates()
    {
        UIAnimations.ReduceMotion = true;
        var host = Spawn("Results");
        var results = host.AddComponent<ResultsScreen>();

        results.ShowDailyResult(3, 4, 4, false, 11, 7, usedPowerUp: false, animate: true);
        results.ShowDailyCoinReward(60);
        results.ShowDailyStreak(7, 9, false);

        Assert.IsFalse(results.PayoutAnimating, "ReduceMotion renders the payout at rest");
        var coinLine = host.transform.Find("DailyCoinLine");
        Assert.AreEqual("+60 coins", coinLine.GetComponent<TextMeshProUGUI>().text);
    }

    // ── celebratedTiers persistence + the once-ever decision ──

    [Test]
    public void CelebratedTiers_RoundTripsThroughJson_AndDefaultsEmptyForOldSaves()
    {
        var data = new PuzzleProgressData { currentTier = 3 };
        data.celebratedTiers.Add(2);
        data.celebratedTiers.Add(3);

        var clone = JsonUtility.FromJson<PuzzleProgressData>(JsonUtility.ToJson(data));
        CollectionAssert.AreEqual(new[] { 2, 3 }, clone.celebratedTiers, "the set persists");

        // An OLD save (no celebratedTiers field) must deserialize to an EMPTY list — no migration.
        var old = JsonUtility.FromJson<PuzzleProgressData>("{\"currentTier\":2,\"completedPuzzleIds\":[1]}");
        Assert.IsNotNull(old.celebratedTiers);
        Assert.IsEmpty(old.celebratedTiers);
    }

    [Test]
    public void TierCelebration_FiresOncePerTierEver()
    {
        var celebrated = new List<int>();

        Assert.IsTrue(CelebrationModal.ShouldCelebrateTier(celebrated, hasNextTier: true, tier: 2));
        celebrated.Add(2); // what CelebrateTierUnlocked records before showing

        Assert.IsFalse(CelebrationModal.ShouldCelebrateTier(celebrated, true, 2),
            "a second results visit must not re-celebrate");
        Assert.IsTrue(CelebrationModal.ShouldCelebrateTier(celebrated, true, 3), "the NEXT tier still can");
        Assert.IsFalse(CelebrationModal.ShouldCelebrateTier(celebrated, false, 3), "no affordance ⇒ no modal");
        Assert.IsFalse(CelebrationModal.ShouldCelebrateTier(null, true, 0), "null-safe, tier 0 invalid");
    }
}
