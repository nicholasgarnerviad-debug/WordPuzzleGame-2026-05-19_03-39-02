#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using WordPuzzle.UI;
using WordPuzzle.State;

/// <summary>
/// Verification harness for Phase-2 hint/reveal/undo flow.
/// Drives the GameStateManager via the same GameActions used by the UI and
/// asserts hintLetterIndex/revealedNextWord transition correctly.
/// </summary>
public static class VerifyLadder
{
    private const BindingFlags BF = BindingFlags.NonPublic | BindingFlags.Instance;
    private const string SCREENSHOT_PATH = "Assets/Screenshots/ladder_v1.png";

    // ----- helpers -----------------------------------------------------------

    private static IGameStateManager FindStateManager()
    {
        var allMonos = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var m in allMonos)
        {
            if (m.GetType().Name == "GameBootstrap")
            {
                var f = m.GetType().GetField("stateManager", BF) ?? m.GetType().GetField("stateManager", BindingFlags.Public | BindingFlags.Instance);
                var sm = f?.GetValue(m) as IGameStateManager;
                if (sm != null) return sm;
            }
        }
        return null;
    }

    private static GameplayScreen FindGameplayScreen() =>
        UnityEngine.Object.FindObjectsByType<GameplayScreen>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault();

    private static int ComputeChangedIndex(string a, string b)
    {
        if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b)) return -1;
        int sharedLen = Math.Min(a.Length, b.Length);
        for (int k = 0; k < sharedLen; k++)
        {
            if (char.ToLowerInvariant(a[k]) != char.ToLowerInvariant(b[k])) return k;
        }
        if (a.Length == b.Length) return -1;
        return sharedLen;
    }

    private static string SolutionAfter(GameState state)
    {
        var solution = state?.puzzle?.solution;
        if (solution == null || solution.Length < 2) return null;
        var chain = state.wordChain;
        if (chain == null || chain.Count == 0) return null;
        string last = chain[chain.Count - 1];
        for (int i = 0; i < solution.Length - 1; i++)
        {
            if (string.Equals(solution[i], last, StringComparison.OrdinalIgnoreCase))
                return solution[i + 1];
        }
        return null;
    }

    // ----- menu items --------------------------------------------------------

    [MenuItem("Tools/Verify Ladder/1 Hint")]
    public static void VerifyHint()
    {
        var sm = FindStateManager();
        if (sm == null) { Debug.LogError("[LADDER] no GameStateManager"); return; }

        GameState before;
        try { before = sm.GetCurrentState(); }
        catch (Exception ex) { Debug.LogError("[LADDER] no active puzzle: " + ex.Message); return; }

        string nextSolutionWord = SolutionAfter(before);
        if (string.IsNullOrEmpty(nextSolutionWord))
        {
            Debug.LogError("[LADDER] no next solution word available — cannot verify hint");
            return;
        }
        string lastChain = before.wordChain[before.wordChain.Count - 1];
        int expectedIndex = ComputeChangedIndex(lastChain, nextSolutionWord);

        Debug.Log($"[LADDER] hint: last='{lastChain}' next='{nextSolutionWord}' expectedIdx={expectedIndex} hintsBefore={before.hintsRemaining}");

        sm.Dispatch(new UseHintAction(0));
        var after = sm.GetCurrentState();

        bool indexOk = after.hintLetterIndex >= 0 && after.hintLetterIndex == expectedIndex;
        bool counterOk = after.hintsRemaining == before.hintsRemaining - 1;
        Debug.Log($"[LADDER] hint result: hintLetterIndex={after.hintLetterIndex} (expected {expectedIndex}) hintsRemaining={after.hintsRemaining} (expected {before.hintsRemaining - 1})");

        if (indexOk && counterOk) Debug.Log("[LADDER] PASS hint");
        else Debug.LogError($"[LADDER] FAIL hint indexOk={indexOk} counterOk={counterOk}");
    }

    [MenuItem("Tools/Verify Ladder/2 Reveal")]
    public static void VerifyReveal()
    {
        var sm = FindStateManager();
        if (sm == null) { Debug.LogError("[LADDER] no GameStateManager"); return; }

        var before = sm.GetCurrentState();
        string nextSolutionWord = SolutionAfter(before);
        if (string.IsNullOrEmpty(nextSolutionWord))
        {
            Debug.LogError("[LADDER] no next solution word available — cannot verify reveal");
            return;
        }
        string lastChain = before.wordChain[before.wordChain.Count - 1];
        int expectedIndex = ComputeChangedIndex(lastChain, nextSolutionWord);

        Debug.Log($"[LADDER] reveal: last='{lastChain}' next='{nextSolutionWord}' expectedIdx={expectedIndex} revealsBefore={before.revealsRemaining}");

        sm.Dispatch(new UseRevealAction());
        var after = sm.GetCurrentState();

        bool wordOk = string.Equals(after.revealedNextWord, nextSolutionWord, StringComparison.OrdinalIgnoreCase);
        bool indexOk = after.hintLetterIndex == expectedIndex;
        bool counterOk = after.revealsRemaining == before.revealsRemaining - 1;
        Debug.Log($"[LADDER] reveal result: revealedNextWord='{after.revealedNextWord}' hintLetterIndex={after.hintLetterIndex} revealsRemaining={after.revealsRemaining}");

        if (wordOk && indexOk && counterOk) Debug.Log("[LADDER] PASS reveal");
        else Debug.LogError($"[LADDER] FAIL reveal wordOk={wordOk} indexOk={indexOk} counterOk={counterOk}");
    }

    [MenuItem("Tools/Verify Ladder/3 SubmitClearsState")]
    public static void VerifySubmitClearsState()
    {
        var sm = FindStateManager();
        if (sm == null) { Debug.LogError("[LADDER] no GameStateManager"); return; }

        var before = sm.GetCurrentState();
        string nextSolutionWord = SolutionAfter(before);
        if (string.IsNullOrEmpty(nextSolutionWord))
        {
            Debug.LogError("[LADDER] no next solution word available — cannot verify submit");
            return;
        }

        // Ensure a hint is active before submit so we can prove it is cleared.
        if (before.hintLetterIndex < 0 && before.hintsRemaining > 0)
        {
            sm.Dispatch(new UseHintAction(0));
        }

        Debug.Log($"[LADDER] submit: typing '{nextSolutionWord}' and submitting");
        foreach (char c in nextSolutionWord.ToLower())
        {
            sm.Dispatch(new PressLetterAction(c));
        }
        sm.Dispatch(new SubmitWordAction(nextSolutionWord));

        var after = sm.GetCurrentState();
        string newLast = after.wordChain[after.wordChain.Count - 1];

        bool chainGrew = after.wordChain.Count == before.wordChain.Count + 1
                         && string.Equals(newLast, nextSolutionWord, StringComparison.OrdinalIgnoreCase);
        bool hintCleared = after.hintLetterIndex == -1;
        bool revealCleared = string.IsNullOrEmpty(after.revealedNextWord);

        Debug.Log($"[LADDER] submit result: chain.Count={after.wordChain.Count} last='{newLast}' hintLetterIndex={after.hintLetterIndex} revealedNextWord='{after.revealedNextWord}'");

        if (chainGrew && hintCleared && revealCleared) Debug.Log("[LADDER] PASS submit-clears-state");
        else Debug.LogError($"[LADDER] FAIL submit chainGrew={chainGrew} hintCleared={hintCleared} revealCleared={revealCleared}");
    }

    [MenuItem("Tools/Verify Ladder/4 Undo")]
    public static void VerifyUndo()
    {
        var sm = FindStateManager();
        if (sm == null) { Debug.LogError("[LADDER] no GameStateManager"); return; }

        var before = sm.GetCurrentState();
        int chainBefore = before.wordChain.Count;
        if (chainBefore <= 1)
        {
            Debug.LogError("[LADDER] chain too short to undo — submit a word first");
            return;
        }
        Debug.Log($"[LADDER] undo: chain.Count before={chainBefore}");

        sm.Dispatch(new UndoStepAction());
        var after = sm.GetCurrentState();

        bool chainShrank = after.wordChain.Count == chainBefore - 1;
        bool hintCleared = after.hintLetterIndex == -1;
        bool revealCleared = string.IsNullOrEmpty(after.revealedNextWord);

        Debug.Log($"[LADDER] undo result: chain.Count after={after.wordChain.Count} hintLetterIndex={after.hintLetterIndex} revealedNextWord='{after.revealedNextWord}'");

        if (chainShrank && hintCleared && revealCleared) Debug.Log("[LADDER] PASS undo");
        else Debug.LogError($"[LADDER] FAIL undo chainShrank={chainShrank} hintCleared={hintCleared} revealCleared={revealCleared}");
    }

    [MenuItem("Tools/Verify Ladder/5 Screenshot")]
    public static void TakeScreenshot()
    {
        System.IO.Directory.CreateDirectory("Assets/Screenshots");
        ScreenCapture.CaptureScreenshot(SCREENSHOT_PATH);
        Debug.Log("[LADDER] screenshot requested -> " + SCREENSHOT_PATH);
    }

    [MenuItem("Tools/Verify Ladder/6 Dump State")]
    public static void DumpState()
    {
        var sm = FindStateManager();
        if (sm == null) { Debug.LogError("[LADDER] no GameStateManager"); return; }
        var s = sm.GetCurrentState();
        var sol = s.puzzle?.solution;
        var sb = new StringBuilder();
        sb.Append("[LADDER DUMP] start='").Append(s.puzzle.startWord).Append("' end='").Append(s.puzzle.endWord).Append("' ");
        sb.Append("solution=[");
        if (sol != null) sb.Append(string.Join(",", sol));
        sb.Append("] ");
        sb.Append("chain=[").Append(string.Join(",", s.wordChain)).Append("] ");
        sb.Append("hintLetterIndex=").Append(s.hintLetterIndex).Append(" revealedNextWord='").Append(s.revealedNextWord).Append("' ");
        sb.Append("hintsRemaining=").Append(s.hintsRemaining).Append(" revealsRemaining=").Append(s.revealsRemaining);
        Debug.Log(sb.ToString());
    }
}
#endif
