using System;
using System.Collections.Generic;

namespace WordPuzzle.Persistence
{
    /// <summary>
    /// Pure, Unity-free update logic for the Library Path View (Task: best solve +
    /// progressively-revealed optimal path). The ONE place the best-solve and
    /// revealed-optimal rules are decided — no Unity, no scoring recompute, fully
    /// unit-testable.
    ///
    /// Rules (locked):
    ///  • (A) Best solve only ever IMPROVES. "Best" = fewest steps, which equals fewest
    ///    detours because par == optimalSteps (detours = playerSteps − par). A strictly
    ///    shorter replay replaces the stored best; an equal-or-worse replay changes nothing.
    ///  • (B) Reveal canonical slot i iff the player's chain has the exact same word at the
    ///    exact same position (playerChain[i] == solution[i], case-insensitive). The revealed
    ///    set UNIONS on every solve and NEVER shrinks.
    ///  • A verified PERFECT/optimal-length solve (playerSteps == optimalSteps) auto-reveals
    ///    the ENTIRE canonical path, regardless of word-match (confirmed design decision) —
    ///    otherwise a perfect solve via a different optimal route would still show blanks.
    /// </summary>
    public static class PuzzlePathProgress
    {
        /// <summary>
        /// Fold a single solve/replay into <paramref name="record"/> (mutated in place and
        /// also returned). <paramref name="playerChain"/> is the full word sequence the player
        /// built (start..end inclusive). <paramref name="solution"/> is the SINGLE stored
        /// canonical optimal path (start..end inclusive); <paramref name="optimalSteps"/> is its
        /// par (== solution.Length − 1). Safe with null/short inputs (no-op).
        /// </summary>
        public static PuzzlePathRecord ApplySolve(
            PuzzlePathRecord record,
            int puzzleId,
            IReadOnlyList<string> playerChain,
            IReadOnlyList<string> solution,
            int optimalSteps)
        {
            if (record == null) record = new PuzzlePathRecord { puzzleId = puzzleId };
            record.puzzleId = puzzleId;
            if (record.revealedOptimalIndices == null) record.revealedOptimalIndices = new List<int>();
            if (record.bestSolvePath == null) record.bestSolvePath = Array.Empty<string>();

            if (playerChain == null || playerChain.Count < 1) return record;

            // (A) Best solve — fewest steps wins; only strictly-better replaces. Steps = edges
            // in the chain (Count − 1). Never let a worse/equal replay overwrite the stored best.
            int steps = playerChain.Count - 1;
            if (steps < record.bestSolveSteps)
            {
                record.bestSolveSteps = steps;
                var copy = new string[playerChain.Count];
                for (int i = 0; i < playerChain.Count; i++) copy[i] = playerChain[i];
                record.bestSolvePath = copy;
            }

            // (B) Reveal matched optimal slots (union into a set; never shrink).
            var revealed = new HashSet<int>(record.revealedOptimalIndices);

            bool isPerfect = solution != null && solution.Count >= 2 && steps == optimalSteps;
            if (isPerfect)
            {
                // Perfect/optimal-length solve auto-reveals the entire canonical path.
                for (int i = 0; i < solution.Count; i++) revealed.Add(i);
            }
            else if (solution != null)
            {
                int n = Math.Min(playerChain.Count, solution.Count);
                for (int i = 0; i < n; i++)
                {
                    if (WordsEqual(playerChain[i], solution[i])) revealed.Add(i);
                }
            }

            // Persist the union as a sorted list (stable JSON, deterministic UI order).
            var sorted = new List<int>(revealed);
            sorted.Sort();
            record.revealedOptimalIndices = sorted;
            return record;
        }

        /// <summary>
        /// True iff the player has revealed every slot of the canonical path (fully uncovered).
        /// </summary>
        public static bool IsFullyRevealed(PuzzlePathRecord record, int solutionLength)
        {
            if (record == null || record.revealedOptimalIndices == null || solutionLength <= 0)
                return false;
            return record.revealedOptimalIndices.Count >= solutionLength;
        }

        /// <summary>Case-insensitive word equality (canonical data is lowercase; player input may vary).</summary>
        private static bool WordsEqual(string a, string b)
        {
            if (a == null || b == null) return false;
            return string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
        }
    }
}
