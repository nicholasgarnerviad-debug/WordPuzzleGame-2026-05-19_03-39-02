#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using WordPuzzle;
using WordPuzzle.Modes;
using WordPuzzle.Puzzle;
using WordPuzzle.State;
using WordPuzzle.UI;

namespace WordPuzzleEditor.Verify
{
    /// <summary>
    /// Editor menu items used by tester6 to drive every verification probe from
    /// the play-mode Unity console (the MCP execute_code path is blocked by a
    /// Windows mono.exe command-line length limit). Each menu item logs a single
    /// "[VERIFY] ..." prefix line per assertion so read_console can scan them.
    /// </summary>
    public static class VerifyModes
    {
        private const string Prefix = "[VERIFY] ";

        private static void Pass(string label) => Debug.Log($"{Prefix}PASS {label}");
        private static void Fail(string label) => Debug.LogError($"{Prefix}FAIL {label}");
        private static void Info(string msg) => Debug.Log($"{Prefix}INFO {msg}");

        private static GameBootstrap FindBootstrap()
        {
            var bs = GameObject.Find("Bootstrap");
            if (bs == null) { Fail("Bootstrap GameObject not in scene"); return null; }
            var gb = bs.GetComponent<GameBootstrap>();
            if (gb == null) { Fail("Bootstrap missing GameBootstrap component"); return null; }
            return gb;
        }

        private static T GetPrivate<T>(object target, string name) where T : class
        {
            if (target == null) return null;
            var f = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            if (f == null) return null;
            return f.GetValue(target) as T;
        }

        private static object GetPrivateRaw(object target, string name)
        {
            if (target == null) return null;
            var f = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            return f?.GetValue(target);
        }

        private static void InvokePrivate(object target, string name, params object[] args)
        {
            if (target == null) return;
            var m = target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic);
            if (m == null) { Fail($"reflection: method {name} not found"); return; }
            m.Invoke(target, args);
        }

        private static GameStateManager GetStateManager(GameBootstrap gb) =>
            GetPrivate<GameStateManager>(gb, "stateManager");

        private static IGameMode GetActiveMode(GameBootstrap gb) =>
            GetPrivate<IGameMode>(gb, "activeMode");

        // ========================================================================
        // 1. CLASSIC MODE PROBES
        // ========================================================================
        [MenuItem("Verify/Step1_StartClassicMode")]
        public static void StartClassicMode()
        {
            if (!Application.isPlaying) { Fail("must be in play mode"); return; }
            var gb = FindBootstrap(); if (gb == null) return;
            InvokePrivate(gb, "StartClassicMode");
            var mode = GetActiveMode(gb);
            if (mode is ClassicMode)
            {
                Pass("activeMode is ClassicMode");
                var state = GetStateManager(gb)?.GetCurrentState();
                if (state != null)
                {
                    int len = state.puzzle.startWord?.Length ?? 0;
                    Info($"classic puzzle startWord='{state.puzzle.startWord}' endWord='{state.puzzle.endWord}' len={len}");
                    if (len >= 3 && len <= 7) Pass($"classic word length in 3-7 range (got {len})");
                    else Fail($"classic word length OUT of 3-7 range (got {len})");
                }
            }
            else Fail($"activeMode is {mode?.GetType().Name ?? "null"} not ClassicMode");
        }

        [MenuItem("Verify/Step2_ClassicTickNoTimer")]
        public static void ClassicTickNoTimer()
        {
            if (!Application.isPlaying) { Fail("must be in play mode"); return; }
            var gb = FindBootstrap(); if (gb == null) return;
            var mode = GetActiveMode(gb);
            if (!(mode is ClassicMode)) { Fail("activeMode is not ClassicMode"); return; }
            var sm = GetStateManager(gb);
            var before = sm.GetCurrentState();
            // Classic mode has no timer; just confirm wordChain is unaffected by Tick.
            mode.Tick(1f);
            var after = sm.GetCurrentState();
            if (before.wordChain.Count == after.wordChain.Count)
                Pass($"classic Tick preserved chain (count={after.wordChain.Count})");
            else
                Fail($"classic Tick changed chain ({before.wordChain.Count} -> {after.wordChain.Count})");
            // GameModeStats has no timer; lives are sentinel 999 -> never decrement on Tick.
            Info($"after Tick: elapsedTime={after.elapsedTime:F2}");
        }

        [MenuItem("Verify/Step3_SubmitBadWord")]
        public static void SubmitBadWord()
        {
            if (!Application.isPlaying) { Fail("must be in play mode"); return; }
            var gb = FindBootstrap(); if (gb == null) return;
            var sm = GetStateManager(gb);
            if (sm == null) { Fail("stateManager null"); return; }

            var before = sm.GetCurrentState();
            int chainBefore = before.wordChain.Count;
            int livesBefore = GetLives(before);
            string bad = new string('z', Mathf.Max(3, before.puzzle.startWord.Length));
            Info($"submitting bad word '{bad}' (chain={chainBefore} lives={livesBefore})");

            bool gotRejectEvent = false;
            string rejectReason = "";
            Action<SubmissionResult> handler = (r) =>
            {
                gotRejectEvent = true;
                rejectReason = r.reason;
                Info($"OnWordSubmissionResult fired: accepted={r.accepted} reason='{r.reason}' rejectReason={r.rejectReason}");
            };
            sm.OnWordSubmissionResult += handler;
            try { sm.Dispatch(new SubmitWordAction(bad)); }
            finally { sm.OnWordSubmissionResult -= handler; }

            var after = sm.GetCurrentState();
            int chainAfter = after.wordChain.Count;
            int livesAfter = GetLives(after);

            if (chainAfter == chainBefore) Pass($"bad word did NOT extend chain (still {chainAfter})");
            else Fail($"bad word changed chain ({chainBefore} -> {chainAfter})");

            if (livesAfter == livesBefore) Pass($"bad word did NOT decrement lives ({livesAfter})");
            else Fail($"bad word decremented lives ({livesBefore} -> {livesAfter})");

            if (gotRejectEvent) Pass($"OnWordSubmissionResult fired with reason='{rejectReason}'");
            else Fail("OnWordSubmissionResult did NOT fire");
        }

        [MenuItem("Verify/Step4_SubmitRealWord")]
        public static void SubmitRealWord()
        {
            if (!Application.isPlaying) { Fail("must be in play mode"); return; }
            var gb = FindBootstrap(); if (gb == null) return;
            var sm = GetStateManager(gb);
            if (sm == null) { Fail("stateManager null"); return; }

            var before = sm.GetCurrentState();
            string tail = before.wordChain[before.wordChain.Count - 1];
            string targetWord = FindHammingOneFromSolution(before.puzzle, tail);
            if (string.IsNullOrEmpty(targetWord))
            {
                Info("no canonical next word available — testing with solution[1] if present");
                if (before.puzzle.solution != null && before.puzzle.solution.Length > 1)
                    targetWord = before.puzzle.solution[1];
            }
            if (string.IsNullOrEmpty(targetWord)) { Fail("no candidate Hamming-1 word"); return; }

            int chainBefore = before.wordChain.Count;
            int livesBefore = GetLives(before);
            Info($"submitting real word '{targetWord}' (chain={chainBefore} tail='{tail}')");

            bool gotAcceptEvent = false;
            Action<SubmissionResult> handler = (r) =>
            {
                if (r.accepted) gotAcceptEvent = true;
                Info($"OnWordSubmissionResult: accepted={r.accepted} reason='{r.reason}'");
            };
            sm.OnWordSubmissionResult += handler;
            try { sm.Dispatch(new SubmitWordAction(targetWord)); }
            finally { sm.OnWordSubmissionResult -= handler; }

            var after = sm.GetCurrentState();
            int chainAfter = after.wordChain.Count;
            int livesAfter = GetLives(after);

            if (chainAfter == chainBefore + 1) Pass($"real word extended chain ({chainBefore} -> {chainAfter})");
            else Fail($"real word did NOT extend chain ({chainBefore} -> {chainAfter})");
            if (livesAfter == livesBefore) Pass($"real word did not change lives ({livesAfter})");
            else Fail($"lives unexpectedly changed ({livesBefore} -> {livesAfter})");
            if (gotAcceptEvent) Pass("OnWordSubmissionResult fired with accepted=true");
            else Fail("OnWordSubmissionResult accept event did NOT fire");
        }

        // ========================================================================
        // 2. PUZZLE SHOW MODE PROBES
        // ========================================================================
        [MenuItem("Verify/Step5_PuzzleShowLibraryTap")]
        public static void PuzzleShowLibraryTap()
        {
            if (!Application.isPlaying) { Fail("must be in play mode"); return; }
            var gb = FindBootstrap(); if (gb == null) return;

            // Drive the same code path the library tap uses.
            InvokePrivate(gb, "OnLibraryPuzzleSelected", 1);

            var mode = GetActiveMode(gb);
            if (mode is PuzzleShowMode) Pass("activeMode is PuzzleShowMode");
            else { Fail($"activeMode is {mode?.GetType().Name ?? "null"}"); return; }

            var sm = GetStateManager(gb);
            var state = sm?.GetCurrentState();
            if (state == null) { Fail("state null after library tap"); return; }
            if (state.puzzle.puzzleId == 1) Pass($"currentPuzzle.puzzleId == 1");
            else Fail($"currentPuzzle.puzzleId == {state.puzzle.puzzleId}, expected 1");
            Info($"puzzleshow start='{state.puzzle.startWord}' end='{state.puzzle.endWord}'");
        }

        // ========================================================================
        // 3. TIME ATTACK MODE PROBES
        // ========================================================================
        [MenuItem("Verify/Step6_StartTimeAttack60")]
        public static void StartTimeAttack60()
        {
            if (!Application.isPlaying) { Fail("must be in play mode"); return; }
            var gb = FindBootstrap(); if (gb == null) return;
            InvokePrivate(gb, "StartTimeAttackModeWithConfig", TimeAttackConfig.Default60());

            var mode = GetActiveMode(gb);
            if (!(mode is TimeAttackMode tam)) { Fail($"activeMode is {mode?.GetType().Name ?? "null"}, expected TimeAttackMode"); return; }
            Pass("activeMode is TimeAttackMode");

            float remaining = tam.GetTimeRemaining();
            if (Mathf.Approximately(remaining, 60f)) Pass($"timeRemaining == 60 (got {remaining})");
            else Fail($"timeRemaining == {remaining}, expected 60");

            var state = GetStateManager(gb).GetCurrentState();
            // addTimeCharges default is 1 in Default60() — verify state.addTimesRemaining matches.
            int addTimes = state.addTimesRemaining;
            if (addTimes == 1) Pass($"addTimesRemaining == 1 (matches Default60.addTimeCharges)");
            else Fail($"addTimesRemaining == {addTimes}, expected 1 from Default60");
            Info($"addTimeGrantSeconds={state.addTimeGrantSeconds}");
        }

        [MenuItem("Verify/Step7_UseAddTimePowerUp")]
        public static void UseAddTimePowerUp()
        {
            if (!Application.isPlaying) { Fail("must be in play mode"); return; }
            var gb = FindBootstrap(); if (gb == null) return;
            var mode = GetActiveMode(gb) as TimeAttackMode;
            if (mode == null) { Fail("activeMode is not TimeAttackMode"); return; }
            var sm = GetStateManager(gb);

            var stateBefore = sm.GetCurrentState();
            float timeBefore = mode.GetTimeRemaining();
            int chargesBefore = stateBefore.addTimesRemaining;
            float grantSec = stateBefore.addTimeGrantSeconds;
            Info($"before AddTime: timeRemaining={timeBefore} charges={chargesBefore} grant={grantSec}");

            if (chargesBefore <= 0) { Fail("no AddTime charges available"); return; }

            sm.Dispatch(new UseAddTimeAction());

            var stateAfter = sm.GetCurrentState();
            float timeAfter = mode.GetTimeRemaining();
            int chargesAfter = stateAfter.addTimesRemaining;

            if (Mathf.Approximately(timeAfter, timeBefore + grantSec))
                Pass($"AddTime added {grantSec}s ({timeBefore} -> {timeAfter})");
            else
                Fail($"AddTime added wrong amount ({timeBefore} -> {timeAfter}, expected +{grantSec})");

            if (chargesAfter == chargesBefore - 1) Pass($"addTimesRemaining decremented ({chargesBefore} -> {chargesAfter})");
            else Fail($"addTimesRemaining did NOT decrement ({chargesBefore} -> {chargesAfter})");
        }

        [MenuItem("Verify/Step8_SurvivalReward")]
        public static void SurvivalReward()
        {
            if (!Application.isPlaying) { Fail("must be in play mode"); return; }
            var gb = FindBootstrap(); if (gb == null) return;

            InvokePrivate(gb, "StartTimeAttackModeWithConfig", TimeAttackConfig.DefaultSurvival());
            var mode = GetActiveMode(gb) as TimeAttackMode;
            if (mode == null) { Fail("activeMode is not TimeAttackMode after Survival start"); return; }
            var sm = GetStateManager(gb);

            float timeBefore = mode.GetTimeRemaining();
            int completedBefore = mode.PuzzlesCompleted;
            Info($"survival start: time={timeBefore} completed={completedBefore} reward={mode.Config.survivalRewardSeconds}s");

            // Force puzzle completion by appending the endWord directly to working state via reflection.
            // We can use SubmitWord with the endWord IF we are already next-to-end — otherwise we just
            // splice the endWord into wordChain and flag isWon. We use the reflection splice path so we
            // don't depend on a valid solution.
            ForceCompletePuzzle(sm);

            // Drive one Tick to let Survival reward logic fire.
            mode.Tick(0.01f);

            float timeAfter = mode.GetTimeRemaining();
            int completedAfter = mode.PuzzlesCompleted;

            float expectedDelta = mode.Config.survivalRewardSeconds - 0.01f; // tick consumed 0.01s
            float actualDelta = timeAfter - timeBefore;
            Info($"survival after: time={timeAfter} completed={completedAfter} delta={actualDelta}");

            if (Mathf.Abs(actualDelta - expectedDelta) < 0.1f)
                Pass($"survival reward added (+{mode.Config.survivalRewardSeconds}s)");
            else
                Fail($"survival reward incorrect (delta={actualDelta}, expected ~{expectedDelta})");
        }

        // ========================================================================
        // 4. UTILITIES
        // ========================================================================
        [MenuItem("Verify/Step9_StopMode")]
        public static void StopMode()
        {
            if (!Application.isPlaying) { Fail("must be in play mode"); return; }
            var gb = FindBootstrap(); if (gb == null) return;
            InvokePrivate(gb, "ShowMainMenu");
            var mode = GetActiveMode(gb);
            if (mode == null) Pass("activeMode is null (back at main menu)");
            else Info($"activeMode still {mode.GetType().Name}");
        }

        // ------- helpers -------
        private static int GetLives(GameState state)
        {
            // Lives field exists on GameState (sentinel 999 after Spec §1).
            var f = state.GetType().GetField("lives", BindingFlags.Instance | BindingFlags.Public);
            if (f == null) return -1;
            try { return (int)f.GetValue(state); } catch { return -1; }
        }

        private static string FindHammingOneFromSolution(WordPuzzle.Puzzle.WordPuzzle puzzle, string tail)
        {
            if (puzzle?.solution == null || string.IsNullOrEmpty(tail)) return null;
            foreach (var cand in puzzle.solution)
            {
                if (string.IsNullOrEmpty(cand)) continue;
                if (cand.Length != tail.Length) continue;
                if (string.Equals(cand, tail, StringComparison.OrdinalIgnoreCase)) continue;
                int diff = 0;
                for (int i = 0; i < cand.Length; i++)
                    if (char.ToLowerInvariant(cand[i]) != char.ToLowerInvariant(tail[i])) diff++;
                if (diff == 1) return cand;
            }
            return null;
        }

        private static void ForceCompletePuzzle(GameStateManager sm)
        {
            // GameState.IsPuzzleComplete is derived from wordChain ending in endWord.
            // We splice endWord into the working chain so the next GetCurrentState()
            // surfaces IsPuzzleComplete = true. We also flip isWon for symmetry.
            var workingField = sm.GetType().GetField("workingState", BindingFlags.Instance | BindingFlags.NonPublic);
            var ws = workingField?.GetValue(sm);
            if (ws == null) { Fail("workingState reflection failed"); return; }

            var puzField = sm.GetType().GetField("currentPuzzle", BindingFlags.Instance | BindingFlags.NonPublic);
            var puz = puzField?.GetValue(sm);
            if (puz == null) { Fail("currentPuzzle reflection failed"); return; }
            var endWordField = puz.GetType().GetField("endWord");
            string endWord = endWordField?.GetValue(puz) as string;
            if (string.IsNullOrEmpty(endWord)) { Fail("endWord null"); return; }

            var chainField = ws.GetType().GetField("wordChain");
            var chain = chainField?.GetValue(ws) as System.Collections.Generic.List<string>;
            if (chain == null) { Fail("wordChain null"); return; }
            chain.Add(endWord);

            var isWonField = ws.GetType().GetField("isWon");
            isWonField?.SetValue(ws, true);
            Info($"ForceCompletePuzzle: spliced '{endWord}' into chain, set isWon=true");
        }
    }
}
#endif
