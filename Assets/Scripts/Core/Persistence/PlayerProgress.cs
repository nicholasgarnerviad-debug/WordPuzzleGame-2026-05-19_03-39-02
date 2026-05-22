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

        // Cross-mode totals
        public int totalHintsEarned;
        public int totalRevealsEarned;
        public int totalUndosEarned;

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
