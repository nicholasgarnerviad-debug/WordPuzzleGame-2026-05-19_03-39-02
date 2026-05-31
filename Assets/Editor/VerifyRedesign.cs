#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using WordPuzzle;
using WordPuzzle.UI;
using WordPuzzle.Persistence;

/// <summary>
/// Verification probes for the v1 redesign / settings work
/// (tester4 — verification-4 plan).
/// </summary>
public static class VerifyRedesign
{
    private const BindingFlags BF_INST = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public;
    private const BindingFlags BF_STATIC = BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Public;

    // -------------------------------------------------------------------
    //  Helpers
    // -------------------------------------------------------------------

    private static T FindActiveOrInactive<T>() where T : Component
        => Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault();

    private static object GetPrivateField(object target, string name)
    {
        if (target == null) return null;
        var f = target.GetType().GetField(name, BF_INST);
        return f?.GetValue(target);
    }

    private static System.Delegate GetEventDelegate(object target, string name)
    {
        if (target == null) return null;
        var t = target.GetType();
        // event field's backing field has the same name as the event in C#
        var f = t.GetField(name, BF_INST);
        return f?.GetValue(target) as System.Delegate;
    }

    private static bool ApproxZero(float a) => Mathf.Abs(a) < 0.0001f;

    private static string Yes(bool b) => b ? "PASS" : "FAIL";

    // -------------------------------------------------------------------
    //  Drivers (allow tester to start each mode)
    // -------------------------------------------------------------------

    [MenuItem("Tools/Verify Redesign/0 Start Classic Mode")]
    public static void StartClassicMode() => InvokeMainMenuEvent("OnClassicModeSelected");

    [MenuItem("Tools/Verify Redesign/0 Start TimeAttack Mode")]
    public static void StartTimeAttackMode() => InvokeMainMenuEvent("OnTimeAttackSelected");

    [MenuItem("Tools/Verify Redesign/0 Start PuzzleShow Mode")]
    public static void StartPuzzleShowMode() => InvokeMainMenuEvent("OnPuzzleShowSelected");

    [MenuItem("Tools/Verify Redesign/0 Open Settings")]
    public static void OpenSettings() => InvokeMainMenuEvent("OnSettingsSelected");

    [MenuItem("Tools/Verify Redesign/0 Back To MainMenu")]
    public static void GameplayBackToMenu()
    {
        var gs = FindActiveOrInactive<GameplayScreen>();
        if (gs == null) { Debug.LogError("[VERIFY-R] GameplayScreen not found"); return; }
        var del = GetEventDelegate(gs, "OnBackToMenu");
        if (del == null) { Debug.LogError("[VERIFY-R] OnBackToMenu has no subscribers"); return; }
        try { del.DynamicInvoke(); Debug.Log("[VERIFY-R] gameplay back to menu fired"); }
        catch (System.Exception ex) { Debug.LogError("[VERIFY-R] back to menu threw: " + ex.Message); }
    }

    private static void InvokeMainMenuEvent(string eventName)
    {
        var mainMenu = FindActiveOrInactive<MainMenuScreen>();
        if (mainMenu == null) { Debug.LogError("[VERIFY-R] MainMenuScreen not found"); return; }
        var del = GetEventDelegate(mainMenu, eventName);
        if (del == null) { Debug.LogError($"[VERIFY-R] {eventName} has no subscribers"); return; }
        Debug.Log($"[VERIFY-R] firing {eventName} ({del.GetInvocationList().Length} subscribers)");
        try { del.DynamicInvoke(); Debug.Log($"[VERIFY-R] {eventName} fired ok"); }
        catch (System.Exception ex) { Debug.LogError($"[VERIFY-R] {eventName} threw: " + ex.Message + "\n" + ex.StackTrace); }
    }

    // -------------------------------------------------------------------
    //  Probe 1 — Badge reparent + legacy text hidden + HOME labels
    // -------------------------------------------------------------------
    [MenuItem("Tools/Verify Redesign/1 Probe Gameplay")]
    public static void ProbeGameplay()
    {
        var sb = new StringBuilder();
        var gs = FindActiveOrInactive<GameplayScreen>();
        if (gs == null) { Debug.LogError("[VERIFY-R] GameplayScreen not found"); return; }

        var hintBtn   = GetPrivateField(gs, "hintButton")   as Button;
        var revealBtn = GetPrivateField(gs, "revealButton") as Button;
        var hintCnt   = GetPrivateField(gs, "hintCountText")   as TextMeshProUGUI;
        var revealCnt = GetPrivateField(gs, "revealCountText") as TextMeshProUGUI;
        var wordChain = GetPrivateField(gs, "wordChainText")     as TextMeshProUGUI;
        var puzzleDsp = GetPrivateField(gs, "puzzleDisplayText") as TextMeshProUGUI;
        var backBtn   = GetPrivateField(gs, "backButton")        as Button;
        var content   = GetPrivateField(gs, "chainScrollContent") as RectTransform;

        bool hintReparented   = (hintBtn != null && hintCnt != null && hintCnt.transform.parent == hintBtn.transform);
        bool revealReparented = (revealBtn != null && revealCnt != null && revealCnt.transform.parent == revealBtn.transform);

        bool wordChainHidden = true;
        if (wordChain != null)
            wordChainHidden = (!wordChain.gameObject.activeSelf) || ApproxZero(wordChain.color.a);

        bool puzzleDspHidden = true;
        if (puzzleDsp != null)
            puzzleDspHidden = (!puzzleDsp.gameObject.activeSelf) || ApproxZero(puzzleDsp.color.a);

        string homeLabel = ResolveButtonLabel(backBtn);
        bool homeOk = (homeLabel != null && homeLabel.ToUpperInvariant().Contains("HOME"));

        int chainChildCount = content != null ? content.childCount : -1;

        sb.Append("[VERIFY-R][gameplay] ")
          .Append("hintBadgeReparented=").Append(Yes(hintReparented)).Append(" ")
          .Append("revealBadgeReparented=").Append(Yes(revealReparented)).Append(" ")
          .Append("wordChainHidden=").Append(Yes(wordChainHidden)).Append(" ")
          .Append("puzzleDispHidden=").Append(Yes(puzzleDspHidden)).Append(" ")
          .Append("backLabelHasHOME=").Append(Yes(homeOk)).Append("(\"").Append(homeLabel ?? "<null>").Append("\") ")
          .Append("chainContent.childCount=").Append(chainChildCount);

        Debug.Log(sb.ToString());

        if (hintCnt != null)
            Debug.Log($"[VERIFY-R][gameplay] hintCount parent={(hintCnt.transform.parent != null ? hintCnt.transform.parent.name : "<null>")}");
        if (revealCnt != null)
            Debug.Log($"[VERIFY-R][gameplay] revealCount parent={(revealCnt.transform.parent != null ? revealCnt.transform.parent.name : "<null>")}");
    }

    // -------------------------------------------------------------------
    //  Probe 2 — Submit several words, ensure full chain visible
    // -------------------------------------------------------------------
    [MenuItem("Tools/Verify Redesign/2 Submit 4 valid chain words")]
    public static void SubmitChainWords()
    {
        var gs = FindActiveOrInactive<GameplayScreen>();
        if (gs == null) { Debug.LogError("[VERIFY-R] GameplayScreen not found"); return; }

        // We need the actual current puzzle's words from GameStateManager.
        // Easiest: pull the active puzzle's "solution" via GameBootstrap → stateManager.
        var bootstrap = Object.FindObjectsByType<GameBootstrap>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault();
        if (bootstrap == null) { Debug.LogError("[VERIFY-R] GameBootstrap not found"); return; }

        var stateMgr = GetPrivateField(bootstrap, "stateManager");
        if (stateMgr == null) { Debug.LogError("[VERIFY-R] stateManager null"); return; }

        var getStateMi = stateMgr.GetType().GetMethod("GetCurrentState", BF_INST);
        if (getStateMi == null) { Debug.LogError("[VERIFY-R] GetCurrentState missing"); return; }

        var state = getStateMi.Invoke(stateMgr, null);
        if (state == null) { Debug.LogError("[VERIFY-R] state null"); return; }

        var puzzleObj = state.GetType().GetField("puzzle")?.GetValue(state);
        if (puzzleObj == null) { Debug.LogError("[VERIFY-R] puzzle null"); return; }

        var solutionField = puzzleObj.GetType().GetField("solution");
        if (solutionField == null) { Debug.LogError("[VERIFY-R] puzzle.solution missing"); return; }

        var solutionArr = solutionField.GetValue(puzzleObj) as System.Collections.Generic.IList<string>;
        if (solutionArr == null)
        {
            // fall back to string[]
            var raw = solutionField.GetValue(puzzleObj);
            if (raw is string[] s) solutionArr = s;
        }
        if (solutionArr == null || solutionArr.Count < 2) { Debug.LogError("[VERIFY-R] solution unavailable"); return; }

        // Get current chain length so we know where we are in the solution.
        var chainField = state.GetType().GetField("wordChain");
        var chain = chainField?.GetValue(state) as System.Collections.Generic.IList<string>;
        int chainCount = chain != null ? chain.Count : 1;

        var evDel = GetEventDelegate(gs, "OnWordSubmitted");
        if (evDel == null) { Debug.LogError("[VERIFY-R] OnWordSubmitted has no subscribers"); return; }

        int submitted = 0;
        for (int i = chainCount; i < solutionArr.Count && submitted < 4; i++)
        {
            var word = solutionArr[i];
            if (string.IsNullOrEmpty(word)) continue;
            try
            {
                evDel.DynamicInvoke(word.ToLowerInvariant());
                submitted++;
                Debug.Log($"[VERIFY-R][submit] '{word}' submitted ({submitted}/4)");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[VERIFY-R][submit] '{word}' threw: {ex.Message}");
                break;
            }
        }

        // Re-probe chain child count
        var content = GetPrivateField(gs, "chainScrollContent") as RectTransform;
        int chainChildCount = content != null ? content.childCount : -1;
        var refreshState = getStateMi.Invoke(stateMgr, null);
        var refreshChain = refreshState.GetType().GetField("wordChain")?.GetValue(refreshState) as System.Collections.Generic.IList<string>;
        int finalChain = refreshChain != null ? refreshChain.Count : -1;

        Debug.Log($"[VERIFY-R][submit] final wordChain.Count={finalChain} chainScrollContent.childCount={chainChildCount} (expected equal)");
    }

    // -------------------------------------------------------------------
    //  Probe 3 — HOME labels on PuzzleLibrary and Results
    // -------------------------------------------------------------------
    [MenuItem("Tools/Verify Redesign/3 Probe HOME labels")]
    public static void ProbeHomeLabels()
    {
        var lib = FindActiveOrInactive<PuzzleLibraryScreen>();
        var res = FindActiveOrInactive<ResultsScreen>();
        string libLabel = null, resLabel = null;
        if (lib != null)
        {
            var btn = GetPrivateField(lib, "backButton") as Button;
            libLabel = ResolveButtonLabel(btn);
        }
        if (res != null)
        {
            var btn = GetPrivateField(res, "mainMenuButton") as Button;
            resLabel = ResolveButtonLabel(btn);
        }
        bool libOk = libLabel != null && libLabel.ToUpperInvariant().Contains("HOME");
        bool resOk = resLabel != null && resLabel.ToUpperInvariant().Contains("HOME");
        Debug.Log($"[VERIFY-R][home] library='{libLabel}' {Yes(libOk)} results='{resLabel}' {Yes(resOk)}");
    }

    // -------------------------------------------------------------------
    //  Probe 4 — Settings mute changes AudioListener.volume
    // -------------------------------------------------------------------
    [MenuItem("Tools/Verify Redesign/4 Probe Settings Mute")]
    public static void ProbeSettingsMute()
    {
        var settings = FindActiveOrInactive<SettingsScreen>();
        if (settings == null) { Debug.LogError("[VERIFY-R] SettingsScreen not found"); return; }

        var toggle = GetPrivateField(settings, "muteToggle") as Toggle;
        var master = GetPrivateField(settings, "masterVolumeSlider") as Slider;
        if (toggle == null) { Debug.LogError("[VERIFY-R] muteToggle null"); return; }

        // make sure screen is showing so OnEnable hook listeners run
        settings.Show();

        // Snapshot, then assert mute → AudioListener.volume == 0
        if (master != null && master.value <= 0f) master.value = 0.75f;
        toggle.isOn = false; // unmute first
        float vUnmuted = AudioListener.volume;

        toggle.isOn = true; // mute
        float vMuted = AudioListener.volume;

        toggle.isOn = false; // restore
        float vAfter = AudioListener.volume;

        bool muteWorks = ApproxZero(vMuted) && !ApproxZero(vUnmuted);
        Debug.Log($"[VERIFY-R][settings] vUnmuted={vUnmuted:F2} vMuted={vMuted:F2} vAfter={vAfter:F2} -> {Yes(muteWorks)}");
    }

    // -------------------------------------------------------------------
    //  Probe 5 — Settings persistence round-trip
    // -------------------------------------------------------------------
    [MenuItem("Tools/Verify Redesign/5 Probe Settings Persistence")]
    public static void ProbeSettingsPersistence()
    {
        var bootstrap = Object.FindObjectsByType<GameBootstrap>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault();
        if (bootstrap == null) { Debug.LogError("[VERIFY-R] GameBootstrap not found"); return; }
        var dataManager = GetPrivateField(bootstrap, "dataManagerRef") as IDataManager;
        if (dataManager == null) { Debug.LogError("[VERIFY-R] dataManagerRef null"); return; }

        var write = new SettingsData
        {
            masterVolume = 0.42f,
            sfxVolume = 0.31f,
            musicVolume = 0.19f,
            muted = true,
            version = 1
        };

        var saveT = dataManager.SaveSettingsAsync(write);
        saveT.GetAwaiter().GetResult();

        var loadT = dataManager.LoadSettingsAsync();
        var read = loadT.GetAwaiter().GetResult();

        bool ok =
            read != null &&
            Mathf.Abs(read.masterVolume - 0.42f) < 0.01f &&
            Mathf.Abs(read.sfxVolume - 0.31f)    < 0.01f &&
            Mathf.Abs(read.musicVolume - 0.19f)  < 0.01f &&
            read.muted == true;

        Debug.Log($"[VERIFY-R][persist] read=({(read==null ? "<null>" : $"m={read.masterVolume:F2} s={read.sfxVolume:F2} mu={read.musicVolume:F2} muted={read.muted}")}) -> {Yes(ok)}");

        // Restore: clear mute so test doesn't leave audio off forever
        write.muted = false;
        write.masterVolume = 0.75f;
        write.sfxVolume = 0.75f;
        write.musicVolume = 0.50f;
        dataManager.SaveSettingsAsync(write).GetAwaiter().GetResult();
    }

    // -------------------------------------------------------------------
    //  Probe 6 — Reset Progress confirm action wipes puzzle progress
    // -------------------------------------------------------------------
    [MenuItem("Tools/Verify Redesign/6 Probe Reset Progress")]
    public static void ProbeResetProgress()
    {
        var settings = FindActiveOrInactive<SettingsScreen>();
        if (settings == null) { Debug.LogError("[VERIFY-R] SettingsScreen not found"); return; }

        var bootstrap = Object.FindObjectsByType<GameBootstrap>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault();
        if (bootstrap == null) { Debug.LogError("[VERIFY-R] GameBootstrap not found"); return; }
        var dataManager = GetPrivateField(bootstrap, "dataManagerRef") as IDataManager;
        if (dataManager == null) { Debug.LogError("[VERIFY-R] dataManagerRef null"); return; }

        // Seed some progress
        var seed = new PuzzleProgressData
        {
            currentTier = 3,
            completedPuzzleIds = new List<int> { 1, 2, 3, 4 },
            inProgressPuzzleIds = new List<int> { 5 },
            lastUpdated = System.DateTime.UtcNow.Ticks
        };
        dataManager.SavePuzzleProgressAsync(seed).GetAwaiter().GetResult();

        // Fire confirmed event on the screen
        var del = GetEventDelegate(settings, "OnResetProgressConfirmed");
        if (del == null) { Debug.LogError("[VERIFY-R] OnResetProgressConfirmed has no subscribers"); return; }
        try { del.DynamicInvoke(); }
        catch (System.Exception ex) { Debug.LogError("[VERIFY-R] reset confirm threw: " + ex.Message); }

        // Give the async reset a frame
        System.Threading.Thread.Sleep(150);

        var after = dataManager.LoadPuzzleProgressAsync().GetAwaiter().GetResult();
        bool cleared = after == null
            || (after.completedPuzzleIds == null || after.completedPuzzleIds.Count == 0)
            && (after.inProgressPuzzleIds == null || after.inProgressPuzzleIds.Count == 0)
            && after.currentTier <= 1;

        Debug.Log($"[VERIFY-R][reset] after: currentTier={(after?.currentTier ?? -1)} completed={(after?.completedPuzzleIds?.Count ?? -1)} inProg={(after?.inProgressPuzzleIds?.Count ?? -1)} -> {Yes(cleared)}");
    }

    // -------------------------------------------------------------------
    //  Screenshot
    // -------------------------------------------------------------------
    [MenuItem("Tools/Verify Redesign/9 Take Screenshot - Classic")]
    public static void ShotClassic() => TakeShot("Assets/Screenshots/redesign_v1_classic.png");
    [MenuItem("Tools/Verify Redesign/9 Take Screenshot - TimeAttack")]
    public static void ShotTimeAttack() => TakeShot("Assets/Screenshots/redesign_v1_timeattack.png");
    [MenuItem("Tools/Verify Redesign/9 Take Screenshot - PuzzleShow")]
    public static void ShotPuzzleShow() => TakeShot("Assets/Screenshots/redesign_v1_puzzleshow.png");
    [MenuItem("Tools/Verify Redesign/9 Take Screenshot - Settings")]
    public static void ShotSettings() => TakeShot("Assets/Screenshots/redesign_v1_settings.png");

    public static void TakeShot(string assetRelativePath)
    {
        string full = Path.Combine(Path.GetDirectoryName(Application.dataPath) ?? "", assetRelativePath.Replace('/', Path.DirectorySeparatorChar));
        var dir = Path.GetDirectoryName(full);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
        ScreenCapture.CaptureScreenshot(full);
        Debug.Log($"[VERIFY-R][shot] captured -> {assetRelativePath}");
    }

    // -------------------------------------------------------------------
    //  Helpers
    // -------------------------------------------------------------------
    private static string ResolveButtonLabel(Button btn)
    {
        if (btn == null) return null;
        var tmp = btn.GetComponentInChildren<TextMeshProUGUI>(true);
        if (tmp != null) return tmp.text;
        var legacy = btn.GetComponentInChildren<Text>(true);
        return legacy != null ? legacy.text : null;
    }
}
#endif
