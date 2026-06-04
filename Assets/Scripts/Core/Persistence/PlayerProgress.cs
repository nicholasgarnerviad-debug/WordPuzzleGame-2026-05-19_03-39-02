using System.Collections.Generic;
using UnityEngine;
using WordPuzzle.Persistence;

namespace WordPuzzle.Persistence
{
    public class PlayerProgress
    {
        public int totalCoins;
        public int totalPuzzlesCompleted;

        // Puzzle Show tracking
        public int highestTierUnlocked;
        public Dictionary<int, TierProgress> tierProgress;

        // Cross-mode totals (owned power-up inventory; decremented on use).
        public int totalHintsEarned;
        public int totalRevealsEarned;
        public int totalUndosEarned;
        public int totalTimeEarned;          // Task 33 — owned +TIME power-up inventory

        // Task 33 — shop economy: remove-ads flag + starting/daily power-up grant tracking.
        public bool removeAds;
        public bool startingGrantApplied;    // 5-each starting inventory granted once
        public string lastDailyGrantDate;    // ISO yyyy-MM-dd of the last daily grant ("" = never)

        // Task 36 Phase 5 (36J) — one-time Starter Pack + temporary ad-free window.
        public bool starterPackOwned;        // one-time non-consumable (restore must NOT re-grant coins)
        public long adFreeUntilUnix;          // Unix seconds; ads suppressed while now < this (0 = none)

        // Task 36 Phase 5 (36K) — faucet/sink claim state (all idempotent per local day).
        public string lastLoginRewardDate;        // ISO; "" = never claimed
        public int loginRewardIndex;              // 0..6 position in the escalating 7-day login cycle
        public string lastWatchCoinsDate;         // ISO; the day watchCoinsCountToday applies to
        public int watchCoinsCountToday;          // rewarded-video watches used today (cap = WatchCoinsDailyCap)
        public int highestStreakMilestoneAwarded; // largest streak milestone already paid (0 = none)

        // Mode-specific stats
        public ClassicModeStats classicStats;
        public TimeAttackModeStats timeAttackStats;

        public PlayerProgress()
        {
            totalCoins = 0;
            totalPuzzlesCompleted = 0;
            highestTierUnlocked = 1;
            tierProgress = new Dictionary<int, TierProgress>();
            totalHintsEarned = 0;
            totalRevealsEarned = 0;
            totalUndosEarned = 0;
            totalTimeEarned = 0;
            removeAds = false;
            startingGrantApplied = false;
            lastDailyGrantDate = "";
            starterPackOwned = false;
            adFreeUntilUnix = 0L;
            lastLoginRewardDate = "";
            loginRewardIndex = 0;
            lastWatchCoinsDate = "";
            watchCoinsCountToday = 0;
            highestStreakMilestoneAwarded = 0;
            classicStats = new ClassicModeStats();
            timeAttackStats = new TimeAttackModeStats();
        }
    }

    public class TierProgress
    {
        public int tierId;
        public int completedPuzzles;
        public bool isUnlocked;
        public long unlockedTimestamp;
    }

    public class ClassicModeStats
    {
        public int gamesPlayed;
        public int gamesWon;
        public int totalCoinsEarned;
        public int totalPuzzlesCompleted;
    }

    public class TimeAttackModeStats
    {
        public int gamesPlayed;
        public int bestRoundReached;
        public int totalCoinsEarned;
        public float sessionStartTime;
    }
}
