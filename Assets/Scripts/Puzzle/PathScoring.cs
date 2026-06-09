namespace WordPuzzle.Puzzle
{
    /// <summary>
    /// Par-grade for a completed (or failed) daily run. Ordered so the integer value
    /// equals the star count (Failed=0★ … Perfect=3★) and so "more detours never improves
    /// the grade" is a simple monotonic comparison. Failed is reserved for running out of
    /// the mistake budget. (Task 36 Phase 1.)
    /// </summary>
    public enum PathGrade
    {
        Failed  = 0,
        Solved  = 1,
        Good    = 2,
        Perfect = 3
    }

    /// <summary>
    /// Immutable result of scoring a daily run against par. Pure data — no Unity types,
    /// no BFS. Produced exclusively by <see cref="PathScoring.Score"/>.
    /// </summary>
    public struct PathScoreResult
    {
        public readonly int par;          // shortest-path distance start->end (WordGraph)
        public readonly int playerSteps;  // accepted moves the player actually made
        public readonly int detours;      // accepted moves that did NOT reduce distance-to-target
        public readonly int mistakesUsed; // invalid guesses spent this run
        public readonly PathGrade grade;
        public readonly int stars;        // == (int)grade by construction
        public readonly bool failed;      // true iff the run ended by exhausting the mistake budget
        public readonly bool usedPowerUp; // true when any power-up was spent; caps the grade (Task 40A)

        public PathScoreResult(int par, int playerSteps, int detours, int mistakesUsed,
                               PathGrade grade, int stars, bool failed, bool usedPowerUp)
        {
            this.par = par;
            this.playerSteps = playerSteps;
            this.detours = detours;
            this.mistakesUsed = mistakesUsed;
            this.grade = grade;
            this.stars = stars;
            this.failed = failed;
            this.usedPowerUp = usedPowerUp;
        }
    }

    /// <summary>
    /// Pure par-scoring for the daily puzzle (Task 36 Phase 1). The ONE place daily grades
    /// are decided — no scoring logic may live elsewhere. The grade is decided on DETOURS
    /// (accepted moves that did not get strictly closer to the target); an optimal-length
    /// path — via ANY route — is Perfect. Running out of mistakes is a hard Fail that
    /// overrides everything. Cutoffs live in <see cref="BalanceConfig"/> (no magic numbers);
    /// a power-up-assisted solve is CAPPED at <see cref="BalanceConfig.PowerUpMaxGrade"/>
    /// (Task 40A — Perfect is unassisted-only). No Unity / State / SDK dependency.
    /// </summary>
    public static class PathScoring
    {
        /// <param name="par">Shortest-path distance start->end (WordGraph distance).</param>
        /// <param name="playerSteps">Accepted moves the player made to reach the target.</param>
        /// <param name="detours">Accepted moves that did NOT reduce distance-to-target.</param>
        /// <param name="mistakesUsed">Invalid submissions spent this run.</param>
        /// <param name="ranOutOfMistakes">True when the run ended by exhausting the mistake budget.</param>
        /// <param name="usedPowerUp">True when any power-up was spent; caps a solve's grade at
        /// <see cref="BalanceConfig.PowerUpMaxGrade"/> (Task 40A). Failed is unaffected.</param>
        public static PathScoreResult Score(int par, int playerSteps, int detours, int mistakesUsed,
                                            bool ranOutOfMistakes, bool usedPowerUp)
        {
            PathGrade grade;
            if (ranOutOfMistakes)
            {
                grade = PathGrade.Failed;                       // hard fail — overrides detours
            }
            else if (detours <= BalanceConfig.PerfectMaxDetours)
            {
                grade = PathGrade.Perfect;                      // optimal-length path (any route)
            }
            else if (detours <= BalanceConfig.GoodMaxDetours)
            {
                grade = PathGrade.Good;
            }
            else
            {
                grade = PathGrade.Solved;                       // solved but wandered — never 0★ on a solve
            }

            // Task 40A — assistance caps the grade (cap level lives in BalanceConfig).
            // Solves only: Failed stays Failed, and the cap never RAISES a grade.
            if (usedPowerUp && !ranOutOfMistakes && grade > BalanceConfig.PowerUpMaxGrade)
                grade = BalanceConfig.PowerUpMaxGrade;

            int stars = (int)grade;                             // enum value == star count by construction
            return new PathScoreResult(par, playerSteps, detours, mistakesUsed,
                                       grade, stars, ranOutOfMistakes, usedPowerUp);
        }
    }
}
