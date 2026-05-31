#if UNITY_EDITOR
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using WordPuzzle.UI;

public static class VerifyPolish
{
    private const BindingFlags BF = BindingFlags.NonPublic | BindingFlags.Instance;

    [MenuItem("Tools/Verify Polish/0 Start Classic Mode")]
    public static void StartClassicMode()
    {
        var mainMenu = Object.FindObjectsByType<MainMenuScreen>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault();
        if (mainMenu == null) { Debug.LogError("[VERIFY] MainMenuScreen not found"); return; }
        var ev = typeof(MainMenuScreen).GetField("OnClassicModeSelected", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
        if (ev == null) { Debug.LogError("[VERIFY] event field missing"); return; }
        var del = ev.GetValue(mainMenu) as System.Delegate;
        if (del == null) { Debug.LogError("[VERIFY] OnClassicModeSelected has no subscribers"); return; }
        Debug.Log("[VERIFY] firing OnClassicModeSelected (" + del.GetInvocationList().Length + " subscribers)");
        try { del.DynamicInvoke(); Debug.Log("[VERIFY] classic mode started"); }
        catch (System.Exception ex) { Debug.LogError("[VERIFY] classic start threw: " + ex.GetType().Name + ": " + ex.Message + "\n" + ex.StackTrace); }
    }

    [MenuItem("Tools/Verify Polish/1 Probe Tile Rows")]
    public static void ProbeTileRows()
    {
        var sb = new StringBuilder();
        var gs = Object.FindObjectsByType<GameplayScreen>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault();
        if (gs == null)
        {
            Debug.LogError("[VERIFY] GameplayScreen not found in scene.");
            return;
        }
        sb.Append("[VERIFY] GameplayScreen go='").Append(gs.gameObject.name)
          .Append("' active=").Append(gs.gameObject.activeInHierarchy)
          .Append(" inHierarchy=").Append(gs.isActiveAndEnabled);
        Debug.Log(sb.ToString());

        var t = typeof(GameplayScreen);
        ProbeRect(t, gs, "startWordRow");
        ProbeRect(t, gs, "endWordRow");
        ProbeRect(t, gs, "currentInputRow");
        ProbeRect(t, gs, "chainScrollContent");

        ProbeButton(t, gs, "hintButton");
        ProbeButton(t, gs, "revealButton");
        ProbeButton(t, gs, "undoButton");
        ProbeButton(t, gs, "submitButton");
        ProbeButton(t, gs, "backButton");
    }

    [MenuItem("Tools/Verify Polish/2 Click Hint")]
    public static void ClickHint() => Click("hintButton");

    [MenuItem("Tools/Verify Polish/3 Click Reveal")]
    public static void ClickReveal() => Click("revealButton");

    [MenuItem("Tools/Verify Polish/4 Click Undo")]
    public static void ClickUndo() => Click("undoButton");

    [MenuItem("Tools/Verify Polish/5 Screenshot")]
    public static void Screenshot()
    {
        string path = "Assets/Screenshots/polish_verify_play.png";
        System.IO.Directory.CreateDirectory("Assets/Screenshots");
        ScreenCapture.CaptureScreenshot(path);
        Debug.Log("[VERIFY] screenshot requested -> " + path);
    }

    [MenuItem("Tools/Verify Polish/6 EndWordRow State")]
    public static void EndWordRowState()
    {
        var gs = Object.FindObjectsByType<GameplayScreen>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault();
        if (gs == null) { Debug.LogError("[VERIFY] no GameplayScreen"); return; }
        var f = typeof(GameplayScreen).GetField("endWordRow", BF);
        var rt = f?.GetValue(gs) as RectTransform;
        if (rt == null) { Debug.LogError("[VERIFY] endWordRow null"); return; }
        var sb = new StringBuilder();
        sb.Append("[VERIFY] endWordRow children=").Append(rt.childCount).Append(" => ");
        for (int i = 0; i < rt.childCount; i++)
        {
            var child = rt.GetChild(i);
            var tmp = child.GetComponentInChildren<TMPro.TMP_Text>(true);
            sb.Append("[").Append(i).Append("]=").Append(tmp != null ? "'" + tmp.text + "'" : "no-tmp").Append(" ");
        }
        Debug.Log(sb.ToString());
    }

    private static object FindStateManager()
    {
        var allMonos = Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var m in allMonos)
        {
            if (m.GetType().Name == "GameBootstrap")
            {
                var f = m.GetType().GetField("stateManager", BF);
                return f?.GetValue(m);
            }
        }
        return null;
    }

    [MenuItem("Tools/Verify Polish/8 Probe Puzzle Words")]
    public static void ProbePuzzleWords()
    {
        var m = FindStateManager();
        if (m != null)
        {
            {
                var miGet = m.GetType().GetMethod("GetCurrentState");
                var state = miGet?.Invoke(m, null);
                if (state == null) { Debug.Log("[VERIFY] state null"); return; }
                var pf = state.GetType().GetField("puzzle");
                var puzzle = pf?.GetValue(state);
                var startF = puzzle?.GetType().GetField("startWord");
                var endF = puzzle?.GetType().GetField("endWord");
                var optF = puzzle?.GetType().GetField("optimalPath") ?? puzzle?.GetType().GetField("solution");
                var sw = startF?.GetValue(puzzle) as string;
                var ew = endF?.GetValue(puzzle) as string;
                var path = optF?.GetValue(puzzle) as System.Collections.IEnumerable;
                string pathStr = "";
                if (path != null) foreach (var w in path) pathStr += w + ",";
                Debug.Log("[VERIFY] puzzle start='" + sw + "' end='" + ew + "' path=" + pathStr);

                // Also dump chain
                var cf = state.GetType().GetField("wordChain");
                var chain = cf?.GetValue(state) as System.Collections.IEnumerable;
                string chainStr = "";
                if (chain != null) foreach (var w in chain) chainStr += w + ",";
                Debug.Log("[VERIFY] wordChain=" + chainStr);
                return;
            }
            return;
        }
        Debug.LogError("[VERIFY] no GameStateManager found");
    }

    [MenuItem("Tools/Verify Polish/9 Type Word And Submit")]
    public static void TypeWordAndSubmit()
    {
        var gs = Object.FindObjectsByType<GameplayScreen>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault();
        if (gs == null) { Debug.LogError("[VERIFY] no GameplayScreen"); return; }

        // Find puzzle path next word
        string nextWord = null;
        var m = FindStateManager();
        if (m != null)
        {
            try {
                var state = m.GetType().GetMethod("GetCurrentState").Invoke(m, null);
                var puzzle = state.GetType().GetField("puzzle").GetValue(state);
                var pathObj = puzzle.GetType().GetField("optimalPath")?.GetValue(puzzle) ?? puzzle.GetType().GetField("solution")?.GetValue(puzzle);
                if (pathObj is string[] arr && arr.Length >= 2) nextWord = arr[1];
                else if (pathObj is System.Collections.Generic.List<string> lst && lst.Count >= 2) nextWord = lst[1];
            } catch (System.Exception ex) { Debug.LogError("[VERIFY] state probe threw: " + ex); }
        }
        if (string.IsNullOrEmpty(nextWord)) { Debug.LogError("[VERIFY] no next word in path"); return; }
        Debug.Log("[VERIFY] typing next word '" + nextWord + "'");

        // Find OnScreenKeyboard
        var kb = Object.FindObjectsByType<WordPuzzle.UI.Components.OnScreenKeyboard>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault();
        if (kb == null) { Debug.LogError("[VERIFY] no OnScreenKeyboard"); return; }
        var ev = typeof(WordPuzzle.UI.Components.OnScreenKeyboard).GetField("OnLetterPressed", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
        var del = ev?.GetValue(kb) as System.Delegate;
        if (del == null) { Debug.LogError("[VERIFY] OnLetterPressed has no subscribers"); return; }

        foreach (char c in nextWord.ToUpper())
        {
            del.DynamicInvoke(c);
        }
        Debug.Log("[VERIFY] letters typed");

        // Click submit
        var sb = typeof(GameplayScreen).GetField("submitButton", BF);
        var sbtn = sb?.GetValue(gs) as Button;
        if (sbtn != null) { try { sbtn.onClick.Invoke(); Debug.Log("[VERIFY] submitButton invoked"); } catch (System.Exception ex) { Debug.LogError("[VERIFY] submit threw: " + ex); } }
    }

    [MenuItem("Tools/Verify Polish/7 ChainScroll State")]
    public static void ChainScrollState()
    {
        var gs = Object.FindObjectsByType<GameplayScreen>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault();
        if (gs == null) { Debug.LogError("[VERIFY] no GameplayScreen"); return; }
        var f = typeof(GameplayScreen).GetField("chainScrollContent", BF);
        var rt = f?.GetValue(gs) as RectTransform;
        if (rt == null) { Debug.LogError("[VERIFY] chainScrollContent null"); return; }
        Debug.Log("[VERIFY] chainScrollContent children=" + rt.childCount);
    }

    private static void ProbeRect(System.Type t, GameplayScreen gs, string field)
    {
        var f = t.GetField(field, BF);
        if (f == null) { Debug.LogError("[VERIFY] FIELD MISSING: " + field); return; }
        var rt = f.GetValue(gs) as RectTransform;
        if (rt == null) { Debug.LogError("[VERIFY] " + field + " = NULL ref"); return; }
        Debug.Log("[VERIFY] " + field + " childCount=" + rt.childCount + " active=" + rt.gameObject.activeInHierarchy);
    }

    private static void ProbeButton(System.Type t, GameplayScreen gs, string field)
    {
        var f = t.GetField(field, BF);
        if (f == null) { Debug.LogError("[VERIFY] FIELD MISSING: " + field); return; }
        var b = f.GetValue(gs) as Button;
        if (b == null) { Debug.LogError("[VERIFY] " + field + " = NULL ref"); return; }
        Debug.Log("[VERIFY] " + field + " interactable=" + b.interactable + " listeners=" + b.onClick.GetPersistentEventCount());
    }

    private static void Click(string field)
    {
        var gs = Object.FindObjectsByType<GameplayScreen>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault();
        if (gs == null) { Debug.LogError("[VERIFY] no GameplayScreen"); return; }
        var f = typeof(GameplayScreen).GetField(field, BF);
        var b = f?.GetValue(gs) as Button;
        if (b == null) { Debug.LogError("[VERIFY] " + field + " null"); return; }
        Debug.Log("[VERIFY] invoking " + field + " (interactable=" + b.interactable + ")");
        try { b.onClick.Invoke(); Debug.Log("[VERIFY] " + field + " invoked OK"); }
        catch (System.Exception ex) { Debug.LogError("[VERIFY] " + field + " threw: " + ex.GetType().Name + ": " + ex.Message + "\n" + ex.StackTrace); }
    }
}
#endif
