/// <summary>
/// Single authoritative source for all balance-tuning constants.
/// All other systems MUST read from here; never hardcode these values elsewhere.
/// Placed in the Game.Puzzle assembly (no dependencies) so every assembly can reference it.
/// </summary>
public static class BalanceConfig
{
    // ─── Power-ups (Task 5B) ──────────────────────────────────────────────────

    /// <summary>Hint charges granted at the start of every new puzzle. Generous by design.</summary>
    public const int DefaultHintsPerPuzzle = 3;

    /// <summary>
    /// Reveal charges granted per puzzle. Scarce — Reveal is far stronger than Hint
    /// (it shows the entire next word, not just one letter).
    /// </summary>
    public const int DefaultRevealsPerPuzzle = 1;

    /// <summary>Coin cost to use one Hint charge. Free — encourages use over frustration.</summary>
    public const int HintCost = 0;

    /// <summary>
    /// Coin cost to use one Reveal charge. Premium power-up.
    /// INVARIANT: RevealCost MUST stay > HintCost.
    /// </summary>
    public const int RevealCost = 25;

    /// <summary>Coin cost to undo one ladder step.</summary>
    public const int UndoCost = 0;

    // ─── Time Attack (Task 5D) ────────────────────────────────────────────────

    /// <summary>Base countdown for the short (60 s) Time Attack variant.</summary>
    public const float TimeAttackBaseSecondsShort = 60f;

    /// <summary>Base countdown for the long (120 s) Time Attack variant.</summary>
    public const float TimeAttackBaseSecondsLong = 120f;

    /// <summary>AddTime charges seeded for the short variant.</summary>
    public const int AddTimeChargesShort = 1;

    /// <summary>AddTime charges seeded for the long variant.</summary>
    public const int AddTimeChargesLong = 2;

    /// <summary>Seconds credited by each AddTime power-up activation.</summary>
    public const float AddTimeGrantSeconds = 10f;

    /// <summary>
    /// PACING: 60 s base + 10 s/solve => skilled players approach endless;
    /// +15 s Survival reward; average players cannot trivially sustain.
    /// Survival-only: seconds added to the clock on each completed puzzle.
    /// </summary>
    public const float SurvivalRewardSeconds = 15f;

    // ─── Generation (Task 5C) ─────────────────────────────────────────────────

    /// <summary>Maximum BFS edge depth when searching for a ladder path.</summary>
    public const int MaxBfsDepth = 10;

    /// <summary>Maximum random-generation attempts before falling back.</summary>
    public const int MaxGenerationAttempts = 20;

    /// <summary>Target word length for Easy difficulty puzzles.</summary>
    public const int EasyWordLength = 3;

    /// <summary>Target word length for Medium difficulty puzzles.</summary>
    public const int MediumWordLength = 4;

    /// <summary>Target word length for Hard difficulty puzzles.</summary>
    public const int HardWordLength = 5;

    /// <summary>Target ladder distance (steps) for Easy puzzles.</summary>
    public const int EasyTargetDistance = 2;

    /// <summary>Target ladder distance for Medium puzzles.</summary>
    public const int MediumTargetDistance = 4;

    /// <summary>Target ladder distance for Hard puzzles.</summary>
    public const int HardTargetDistance = 6;

    // ─── Minimum move floor (Task 17) ─────────────────────────────────────────

    /// <summary>Absolute hard floor: no puzzle may be solvable in fewer than this many moves.</summary>
    public const int AbsoluteMinMoves = 2;

    /// <summary>
    /// Task 17 — minimum moves (shortest-path edits) a puzzle of the given word length must
    /// require. Single source of truth for the length→min-moves curve; always &gt;= AbsoluteMinMoves.
    /// Curve: 3→2, 4→2, 5→3, 6→3, 7→4 (longer words generally demand longer ladders).
    /// </summary>
    public static int MinMovesForLength(int wordLength)
    {
        int min;
        switch (wordLength)
        {
            case 3: min = 2; break;
            case 4: min = 2; break;
            case 5: min = 3; break;
            case 6: min = 3; break;
            case 7: min = 4; break;
            default: min = wordLength >= 7 ? 4 : 2; break;
        }
        return min < AbsoluteMinMoves ? AbsoluteMinMoves : min;
    }

    // ─── Tier pacing (Task 5A; Task 15: 7 tiers × 50) ─────────────────────────

    /// <summary>Total number of tiers in the game (Puzzle Show mode).</summary>
    public const int MaxTier = 7;

    /// <summary>Curated puzzles authored per tier (Task 15).</summary>
    public const int PuzzlesPerTier = 50;

    /// <summary>
    /// Base gate (Tier 1) — puzzles a player must complete to advance to the next tier.
    /// Tier-1 value; deeper tiers require progressively more, see PuzzlesRequiredToAdvance.
    /// </summary>
    public const int PuzzlesRequiredToAdvanceTier = 10;

    /// <summary>
    /// Task 15 — progressive unlock: how many puzzles to clear in <paramref name="tier"/>
    /// to unlock the next tier. Rises with depth (10/15/20/25/30/35), capped at PuzzlesPerTier.
    /// Already-unlocked tiers stay open, so players can return to complete the rest.
    /// </summary>
    public static int PuzzlesRequiredToAdvance(int tier)
    {
        if (tier < 1) tier = 1;
        int required = PuzzlesRequiredToAdvanceTier + (tier - 1) * 5;
        if (required > PuzzlesPerTier) required = PuzzlesPerTier;
        return required;
    }

    // ─── Economy faucets (Task 6A) ────────────────────────────────────────────

    /// <summary>
    /// Coins awarded on any puzzle completion (Classic, PuzzleShow, TimeAttack).
    /// Anti-deadlock: 3 completions fund one Reveal (RevealCost=25). No gates behind coins.
    /// </summary>
    public const int PuzzleCompletionReward = 10;

    /// <summary>Bonus coins for completing today's daily puzzle (stacks with PuzzleCompletionReward).</summary>
    public const int DailyBonusReward = 25;

    /// <summary>Hint charges granted by one fully-watched rewarded video.</summary>
    public const int RewardedAdHintGrant = 1;

    // ─── Ad policy (Task 6B) ─────────────────────────────────────────────────

    /// <summary>Minimum real-time seconds that must elapse between interstitial impressions.</summary>
    public const int InterstitialCooldownSeconds = 300;   // 5 minutes

    /// <summary>Minimum completed puzzles between interstitial impressions.</summary>
    public const int InterstitialPuzzleCap = 5;
}
