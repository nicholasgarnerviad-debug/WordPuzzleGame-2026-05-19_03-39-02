#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;
using TMPro;

/// <summary>
/// Editor probe for the puzzles-v1 verification (tester3).
/// Drives play-mode introspection: tier-gate constant, FROM/TO labels, library card count.
/// </summary>
public static class VerifyPuzzles
{
    private const string SummaryKey = "VerifyPuzzles.Summary";

    [MenuItem("Tools/Verify Puzzles/Run All")]
    public static void RunAll()
    {
        var sb = new StringBuilder();
        sb.AppendLine("[VerifyPuzzles] === Run All ===");

        // 1. PuzzleShowMode.PuzzlesRequiredToAdvanceTier == 10 (reflection — runs even outside play)
        try
        {
            var asm = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => a.GetName().Name == "Game.Modes");
            Type t = asm?.GetType("WordPuzzle.Modes.PuzzleShowMode");
            if (t == null)
            {
                // fallback — scan all
                foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
                {
                    t = a.GetType("WordPuzzle.Modes.PuzzleShowMode");
                    if (t != null) break;
                }
            }
            if (t == null)
            {
                sb.AppendLine("[FAIL] PuzzleShowMode type not found");
            }
            else
            {
                var f = t.GetField("PuzzlesRequiredToAdvanceTier",
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                if (f == null)
                {
                    sb.AppendLine("[FAIL] PuzzlesRequiredToAdvanceTier field not found on PuzzleShowMode");
                }
                else
                {
                    var val = (int)f.GetRawConstantValue();
                    if (val == 10)
                        sb.AppendLine($"[PASS] PuzzleShowMode.PuzzlesRequiredToAdvanceTier == 10");
                    else
                        sb.AppendLine($"[FAIL] PuzzleShowMode.PuzzlesRequiredToAdvanceTier == {val} (expected 10)");
                }
            }
        }
        catch (Exception ex)
        {
            sb.AppendLine($"[FAIL] Reflection error: {ex.Message}");
        }

        // 2. GameplayScreen FROM/TO/steps labels present in scene (active or inactive)
        try
        {
            var gpType = FindType("WordPuzzle.UI.GameplayScreen");
            if (gpType == null)
            {
                sb.AppendLine("[FAIL] GameplayScreen type not found");
            }
            else
            {
                var gp = UnityEngine.Object.FindObjectsByType(gpType,
                    FindObjectsInactive.Include, FindObjectsSortMode.None);
                if (gp == null || gp.Length == 0)
                {
                    sb.AppendLine("[FAIL] No GameplayScreen instance found in scene");
                }
                else
                {
                    var inst = gp[0] as MonoBehaviour;
                    var startLbl = GetTmpField(gpType, inst, "startWordLabel");
                    var endLbl   = GetTmpField(gpType, inst, "endWordLabel");
                    var stepsLbl = GetTmpField(gpType, inst, "stepsRemainingText");

                    sb.AppendLine($"  startWordLabel = {Describe(startLbl)}");
                    sb.AppendLine($"  endWordLabel   = {Describe(endLbl)}");
                    sb.AppendLine($"  stepsRemainingText = {Describe(stepsLbl)}");

                    bool startOK = startLbl != null && !string.IsNullOrEmpty(startLbl.text);
                    bool endOK   = endLbl   != null && !string.IsNullOrEmpty(endLbl.text);
                    bool stepsOK = stepsLbl != null;

                    if (startOK && endOK && stepsOK)
                        sb.AppendLine("[PASS] GameplayScreen has FROM/TO/steps labels wired with text");
                    else
                        sb.AppendLine("[FAIL] GameplayScreen labels missing or empty");
                }
            }
        }
        catch (Exception ex)
        {
            sb.AppendLine($"[FAIL] GameplayScreen probe error: {ex.Message}");
        }

        // 3. PuzzleLibraryScreen card count > 0 after Show()
        try
        {
            var libType = FindType("WordPuzzle.UI.PuzzleLibraryScreen");
            if (libType == null)
            {
                sb.AppendLine("[FAIL] PuzzleLibraryScreen type not found");
            }
            else
            {
                var libArr = UnityEngine.Object.FindObjectsByType(libType,
                    FindObjectsInactive.Include, FindObjectsSortMode.None);
                if (libArr == null || libArr.Length == 0)
                {
                    sb.AppendLine("[FAIL] PuzzleLibraryScreen not found in scene");
                }
                else
                {
                    var lib = libArr[0] as MonoBehaviour;
                    var contentField = libType.GetField("contentRoot",
                        BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                    var content = contentField?.GetValue(lib) as Transform;

                    // Try to call Show() to populate cards
                    var showMethod = libType.GetMethod("Show",
                        BindingFlags.Instance | BindingFlags.Public, null, Type.EmptyTypes, null);
                    if (showMethod != null)
                    {
                        try { lib.gameObject.SetActive(true); showMethod.Invoke(lib, null); }
                        catch (Exception ex) { sb.AppendLine($"  Show() threw: {ex.InnerException?.Message ?? ex.Message}"); }
                    }

                    int cardCount = content != null ? content.childCount : -1;
                    sb.AppendLine($"  contentRoot = {(content == null ? "NULL" : content.name)}, childCount = {cardCount}");
                    if (cardCount > 0)
                        sb.AppendLine($"[PASS] PuzzleLibraryScreen rendered {cardCount} cards");
                    else
                        sb.AppendLine("[FAIL] PuzzleLibraryScreen has 0 cards");
                }
            }
        }
        catch (Exception ex)
        {
            sb.AppendLine($"[FAIL] PuzzleLibraryScreen probe error: {ex.Message}");
        }

        var s = sb.ToString().TrimEnd();
        EditorPrefs.SetString(SummaryKey, s);
        // Log line-by-line so Unity's per-message UI doesn't truncate
        foreach (var line in s.Split('\n'))
        {
            Debug.Log("[VerifyPuzzles] " + line.TrimEnd('\r'));
        }
    }

    [MenuItem("Tools/Verify Puzzles/Show Library Screen")]
    public static void ShowLibraryScreen()
    {
        var libType = FindType("WordPuzzle.UI.PuzzleLibraryScreen");
        if (libType == null) { Debug.LogError("PuzzleLibraryScreen type not found"); return; }
        var libArr = UnityEngine.Object.FindObjectsByType(libType,
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (libArr.Length == 0) { Debug.LogError("PuzzleLibraryScreen not in scene"); return; }
        var lib = libArr[0] as MonoBehaviour;

        // Hide all other screens
        foreach (var go in UnityEngine.Object.FindObjectsByType<Canvas>(
            FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            // Don't disable canvases — but disable sibling screen GameObjects
        }
        // Disable common screen siblings if they share a parent
        var parent = lib.transform.parent;
        if (parent != null)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i);
                if (child != lib.transform) child.gameObject.SetActive(false);
            }
        }

        lib.gameObject.SetActive(true);
        var showMethod = libType.GetMethod("Show",
            BindingFlags.Instance | BindingFlags.Public, null, Type.EmptyTypes, null);
        if (showMethod != null)
        {
            try { showMethod.Invoke(lib, null); Debug.Log("[VerifyPuzzles] Library Show() invoked"); }
            catch (Exception ex) { Debug.LogError($"Show() failed: {ex.InnerException?.Message ?? ex.Message}"); }
        }
    }

    [MenuItem("Tools/Verify Puzzles/Show Gameplay Screen")]
    public static void ShowGameplayScreen()
    {
        var gpType = FindType("WordPuzzle.UI.GameplayScreen");
        if (gpType == null) { Debug.LogError("GameplayScreen type not found"); return; }
        var arr = UnityEngine.Object.FindObjectsByType(gpType,
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (arr.Length == 0) { Debug.LogError("GameplayScreen not in scene"); return; }
        var gp = arr[0] as MonoBehaviour;

        var parent = gp.transform.parent;
        if (parent != null)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i);
                if (child != gp.transform) child.gameObject.SetActive(false);
            }
        }
        gp.gameObject.SetActive(true);

        // Spec: startWordLabel must say "FROM", endWordLabel "TO".
        // Set the row tiles via SetPuzzleDisplay if available so FROM/TO words show below labels.
        try
        {
            // Default label call (no args) keeps spec-exact "FROM"/"TO" headers.
            var setLabels = gpType.GetMethod("SetWordLabels",
                BindingFlags.Instance | BindingFlags.Public);
            setLabels?.Invoke(gp, new object[] { "FROM", "TO" });

            var setSteps = gpType.GetMethod("SetStepsRemaining",
                BindingFlags.Instance | BindingFlags.Public);
            setSteps?.Invoke(gp, new object[] { 3, 4 });

            // Try to render the puzzle tiles via SetPuzzleDisplay
            var setPuzzle = gpType.GetMethod("SetPuzzleDisplay",
                BindingFlags.Instance | BindingFlags.Public);
            if (setPuzzle != null)
            {
                try { setPuzzle.Invoke(gp, new object[] { "stone", "money" }); }
                catch { /* signature may differ */ }
            }
            Debug.Log("[VerifyPuzzles] Gameplay labels set to FROM/TO with stone→money tiles");
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"Could not set labels: {ex.InnerException?.Message ?? ex.Message}");
        }
    }

    [MenuItem("Tools/Verify Puzzles/Print Summary")]
    public static void PrintSummary()
    {
        Debug.Log("[VerifyPuzzles] Last summary:\n" + EditorPrefs.GetString(SummaryKey, "(none)"));
    }

    // ---------- helpers ----------

    private static Type FindType(string fullName)
    {
        foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
        {
            var t = a.GetType(fullName);
            if (t != null) return t;
        }
        return null;
    }

    private static TextMeshProUGUI GetTmpField(Type t, object inst, string name)
    {
        var f = t.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        return f?.GetValue(inst) as TextMeshProUGUI;
    }

    private static string Describe(TextMeshProUGUI tmp)
    {
        if (tmp == null) return "<null>";
        return $"name='{tmp.gameObject.name}', text='{tmp.text}', active={tmp.gameObject.activeInHierarchy}";
    }
}
#endif
