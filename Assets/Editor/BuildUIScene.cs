using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using WordPuzzle;
using WordPuzzle.UI;
using WordPuzzle.Modes;

public class BuildUIScene {
    [MenuItem("Tools/Build Complete UI")]
    public static void BuildUI() {
        Debug.Log("=== Starting Complete UI Build ===");
        var canvas = GameObject.Find("Canvas");
        if (!canvas) { Debug.LogError("Canvas not found"); return; }
        var canvasT = canvas.transform;

        // MAIN MENU SCREEN
        BuildMainMenuScreen(canvasT);
        // GAMEPLAY SCREEN
        BuildGameplayScreen(canvasT);
        // RESULTS SCREEN
        BuildResultsScreen(canvasT);
        // TIMER DISPLAY
        BuildTimerDisplay(canvasT);

        // Wire Bootstrap
        WireBootstrap();

        Debug.Log("=== Complete UI Build Finished ===");
        EditorSceneManager.SaveOpenScenes();
    }

    static void BuildMainMenuScreen(Transform canvasT) {
        var mms = new GameObject("MainMenuScreen");
        mms.transform.SetParent(canvasT);
        SetStretch(mms.AddComponent<RectTransform>());
        var mmsImg = mms.AddComponent<Image>();
        mmsImg.color = new Color(20/255f, 30/255f, 60/255f, 1f);
        var mmsComp = mms.AddComponent<MainMenuScreen>();

        var title = new GameObject("TitleText");
        title.transform.SetParent(mms.transform);
        var titleRT = title.AddComponent<RectTransform>();
        SetRect(titleRT, 0, 700, 800, 120);
        var titleTMP = title.AddComponent<TextMeshProUGUI>();
        titleTMP.text = "Word Puzzle Game";
        titleTMP.fontSize = 80;
        titleTMP.alignment = TextAlignmentOptions.Center;

        var classicBtn = CreateButton("ClassicModeButton", mms.transform, new Color(70/255f, 130/255f, 200/255f), "Classic Mode", 50, 0, 250, 500, 100);
        var puzzleBtn = CreateButton("PuzzleShowButton", mms.transform, new Color(130/255f, 80/255f, 200/255f), "Puzzle Show", 50, 0, 110, 500, 100);
        var timeBtn = CreateButton("TimeAttackButton", mms.transform, new Color(200/255f, 80/255f, 60/255f), "Time Attack", 50, 0, -30, 500, 100);

        var so = new SerializedObject(mmsComp);
        so.FindProperty("classicModeButton").objectReferenceValue = classicBtn.GetComponent<Button>();
        so.FindProperty("puzzleShowButton").objectReferenceValue = puzzleBtn.GetComponent<Button>();
        so.FindProperty("timeAttackButton").objectReferenceValue = timeBtn.GetComponent<Button>();
        so.ApplyModifiedProperties();

        Debug.Log("MainMenuScreen built");
    }

    static void BuildGameplayScreen(Transform canvasT) {
        var gs = new GameObject("GameplayScreen");
        gs.transform.SetParent(canvasT);
        SetStretch(gs.AddComponent<RectTransform>());
        var gsImg = gs.AddComponent<Image>();
        gsImg.color = new Color(15/255f, 15/255f, 25/255f, 1f);
        gs.AddComponent<GameplayScreen>();

        // Lives & Score text
        var livesText = new GameObject("LivesText");
        livesText.transform.SetParent(gs.transform);
        var livesRT = livesText.AddComponent<RectTransform>();
        SetRect(livesRT, -350, 850, 250, 60);
        var livesTMP = livesText.AddComponent<TextMeshProUGUI>();
        livesTMP.text = "Lives: 3";
        livesTMP.fontSize = 45;

        var scoreText = new GameObject("ScoreText");
        scoreText.transform.SetParent(gs.transform);
        var scoreRT = scoreText.AddComponent<RectTransform>();
        SetRect(scoreRT, 350, 850, 250, 60);
        var scoreTMP = scoreText.AddComponent<TextMeshProUGUI>();
        scoreTMP.text = "Steps: 0";
        scoreTMP.fontSize = 45;
        scoreTMP.alignment = TextAlignmentOptions.Right;

        // WordChainDisplay
        var wcd = new GameObject("WordChainDisplay");
        wcd.transform.SetParent(gs.transform);
        SetRect(wcd.AddComponent<RectTransform>(), 0, 600, 900, 200);
        var wcdComp = wcd.AddComponent<WordChainDisplay>();

        var container = new GameObject("Container");
        container.transform.SetParent(wcd.transform);
        SetStretch(container.AddComponent<RectTransform>());
        container.AddComponent<VerticalLayoutGroup>().childAlignment = TextAnchor.UpperCenter;

        var wcdSO = new SerializedObject(wcdComp);
        wcdSO.FindProperty("container").objectReferenceValue = container.transform;
        wcdSO.ApplyModifiedProperties();

        // CurrentWordInput
        var cwi = new GameObject("CurrentWordInput");
        cwi.transform.SetParent(gs.transform);
        SetRect(cwi.AddComponent<RectTransform>(), 0, 350, 900, 150);
        var cwiComp = cwi.AddComponent<CurrentWordInput>();

        var inputText = new GameObject("InputText");
        inputText.transform.SetParent(cwi.transform);
        var inputRT = inputText.AddComponent<RectTransform>();
        SetRect(inputRT, 0, 35, 880, 75);
        var inputTMP = inputText.AddComponent<TextMeshProUGUI>();
        inputTMP.fontSize = 65;
        inputTMP.alignment = TextAlignmentOptions.Center;

        var targetText = new GameObject("TargetText");
        targetText.transform.SetParent(cwi.transform);
        var targetRT = targetText.AddComponent<RectTransform>();
        SetRect(targetRT, 0, -45, 880, 60);
        var targetTMP = targetText.AddComponent<TextMeshProUGUI>();
        targetTMP.text = "Target: ?";
        targetTMP.fontSize = 40;
        targetTMP.alignment = TextAlignmentOptions.Center;

        var cwiSO = new SerializedObject(cwiComp);
        cwiSO.FindProperty("inputText").objectReferenceValue = inputTMP;
        cwiSO.FindProperty("targetText").objectReferenceValue = targetTMP;
        cwiSO.ApplyModifiedProperties();

        // Buttons
        var hintBtn = CreateButton("HintButton", gs.transform, new Color(200/255f, 160/255f, 30/255f), "Hint", 40, -290, 150, 200, 80);
        var revealBtn = CreateButton("RevealButton", gs.transform, new Color(60/255f, 160/255f, 60/255f), "Reveal", 40, 0, 150, 200, 80);
        var undoBtn = CreateButton("UndoButton", gs.transform, new Color(100/255f, 100/255f, 100/255f), "Undo", 40, 290, 150, 200, 80);

        // Keyboard
        var keyboardContainer = new GameObject("KeyboardContainer");
        keyboardContainer.transform.SetParent(gs.transform);
        var keyboardRT = keyboardContainer.AddComponent<RectTransform>();
        SetRect(keyboardRT, 0, -170, 1000, 320);
        var glg = keyboardContainer.AddComponent<GridLayoutGroup>();
        glg.cellSize = new Vector2(100, 100);
        glg.spacing = new Vector2(2, 2);
        glg.startCorner = GridLayoutGroup.Corner.UpperLeft;
        glg.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        glg.constraintCount = 10;

        // Submit & Delete
        var submitBtn = CreateButton("SubmitButton", gs.transform, new Color(50/255f, 180/255f, 100/255f), "Submit", 48, -200, -560, 340, 100);
        var deleteBtn = CreateButton("DeleteButton", gs.transform, new Color(180/255f, 60/255f, 60/255f), "Delete ←", 48, 200, -560, 340, 100);

        // Overlays
        var winOverlay = new GameObject("WinOverlay");
        winOverlay.transform.SetParent(gs.transform);
        SetStretch(winOverlay.AddComponent<RectTransform>());
        winOverlay.AddComponent<Image>().color = new Color(0, 180/255f, 80/255f, 210/255f);
        winOverlay.SetActive(false);

        var winText = new GameObject("WinText");
        winText.transform.SetParent(winOverlay.transform);
        var winRT = winText.AddComponent<RectTransform>();
        SetRect(winRT, 0, 100, 800, 150);
        var winTMP = winText.AddComponent<TextMeshProUGUI>();
        winTMP.text = "YOU WIN!";
        winTMP.fontSize = 90;
        winTMP.alignment = TextAlignmentOptions.Center;
        winTMP.color = Color.white;

        var lossOverlay = new GameObject("LossOverlay");
        lossOverlay.transform.SetParent(gs.transform);
        SetStretch(lossOverlay.AddComponent<RectTransform>());
        lossOverlay.AddComponent<Image>().color = new Color(180/255f, 30/255f, 30/255f, 210/255f);
        lossOverlay.SetActive(false);

        var lossText = new GameObject("LossText");
        lossText.transform.SetParent(lossOverlay.transform);
        var lossRT = lossText.AddComponent<RectTransform>();
        SetRect(lossRT, 0, 100, 800, 150);
        var lossTMP = lossText.AddComponent<TextMeshProUGUI>();
        lossTMP.text = "GAME OVER";
        lossTMP.fontSize = 90;
        lossTMP.alignment = TextAlignmentOptions.Center;
        lossTMP.color = Color.white;

        // Wire GameplayScreen
        var gsSO = new SerializedObject(gs.GetComponent<GameplayScreen>());
        gsSO.FindProperty("wordChainDisplay").objectReferenceValue = wcdComp;
        gsSO.FindProperty("currentWordInput").objectReferenceValue = cwiComp;
        gsSO.FindProperty("keyboardContainer").objectReferenceValue = keyboardContainer.transform;
        gsSO.FindProperty("livesText").objectReferenceValue = livesTMP;
        gsSO.FindProperty("scoreText").objectReferenceValue = scoreTMP;
        gsSO.FindProperty("submitButton").objectReferenceValue = submitBtn.GetComponent<Button>();
        gsSO.FindProperty("hintButton").objectReferenceValue = hintBtn.GetComponent<Button>();
        gsSO.FindProperty("revealButton").objectReferenceValue = revealBtn.GetComponent<Button>();
        gsSO.FindProperty("undoButton").objectReferenceValue = undoBtn.GetComponent<Button>();
        gsSO.FindProperty("deleteButton").objectReferenceValue = deleteBtn.GetComponent<Button>();
        gsSO.FindProperty("winOverlay").objectReferenceValue = winOverlay;
        gsSO.FindProperty("lossOverlay").objectReferenceValue = lossOverlay;
        gsSO.ApplyModifiedProperties();

        Debug.Log("GameplayScreen built");
    }

    static void BuildResultsScreen(Transform canvasT) {
        var rs = new GameObject("ResultsScreen");
        rs.transform.SetParent(canvasT);
        SetStretch(rs.AddComponent<RectTransform>());
        rs.AddComponent<Image>().color = new Color(20/255f, 20/255f, 40/255f, 1f);
        var rsComp = rs.AddComponent<ResultsScreen>();
        rs.SetActive(false);

        var modeNameText = CreateText("ModeNameText", rs.transform, "Classic Mode", 70, 0, 600);
        var scoreText = CreateText("ScoreText", rs.transform, "Puzzles: 0", 55, 0, 400);
        var coinsText = CreateText("CoinsEarnedText", rs.transform, "Coins: +0", 55, 0, 300);
        var timeText = CreateText("TimeText", rs.transform, "Time: 0s", 55, 0, 200);

        var nextBtn = CreateButton("NextButton", rs.transform, new Color(50/255f, 180/255f, 100/255f), "Play Again", 50, -210, -500, 380, 100);
        var menuBtn = CreateButton("MenuButton", rs.transform, new Color(70/255f, 130/255f, 200/255f), "Main Menu", 50, 210, -500, 380, 100);

        var rsSO = new SerializedObject(rsComp);
        rsSO.FindProperty("modeNameText").objectReferenceValue = modeNameText;
        rsSO.FindProperty("scoreText").objectReferenceValue = scoreText;
        rsSO.FindProperty("coinsEarnedText").objectReferenceValue = coinsText;
        rsSO.FindProperty("timeText").objectReferenceValue = timeText;
        rsSO.FindProperty("nextButton").objectReferenceValue = nextBtn.GetComponent<Button>();
        rsSO.FindProperty("menuButton").objectReferenceValue = menuBtn.GetComponent<Button>();
        rsSO.ApplyModifiedProperties();

        Debug.Log("ResultsScreen built");
    }

    static void BuildTimerDisplay(Transform canvasT) {
        var td = new GameObject("TimerDisplay");
        td.transform.SetParent(canvasT);
        SetRect(td.AddComponent<RectTransform>(), 0, 850, 350, 70);
        var tdComp = td.AddComponent<TimerDisplay>();
        td.SetActive(false);

        var timerText = new GameObject("TimerText");
        timerText.transform.SetParent(td.transform);
        SetStretch(timerText.AddComponent<RectTransform>());
        var timerTMP = timerText.AddComponent<TextMeshProUGUI>();
        timerTMP.text = "Time: 60.0s";
        timerTMP.fontSize = 55;
        timerTMP.alignment = TextAlignmentOptions.Center;
        timerTMP.color = new Color(100/255f, 220/255f, 100/255f, 1f);

        var tdSO = new SerializedObject(tdComp);
        tdSO.FindProperty("timerText").objectReferenceValue = timerTMP;
        tdSO.ApplyModifiedProperties();

        Debug.Log("TimerDisplay built");
    }

    static void WireBootstrap() {
        var bootstrap = GameObject.Find("Bootstrap");
        if (!bootstrap) return;

        var gameplayScreen = GameObject.Find("Canvas/GameplayScreen");
        var mainMenuScreen = GameObject.Find("Canvas/MainMenuScreen");
        var resultsScreen = GameObject.Find("Canvas/ResultsScreen");
        var timerDisplay = GameObject.Find("Canvas/TimerDisplay");

        var bootstrapComp = bootstrap.GetComponent<GameBootstrap>();
        var bootstrapSO = new SerializedObject(bootstrapComp);
        bootstrapSO.FindProperty("uiManager").objectReferenceValue = bootstrap.GetComponent<UIManager>();
        bootstrapSO.FindProperty("gameplayScreen").objectReferenceValue = gameplayScreen ? gameplayScreen.GetComponent<GameplayScreen>() : null;
        bootstrapSO.FindProperty("mainMenuScreen").objectReferenceValue = mainMenuScreen ? mainMenuScreen.GetComponent<MainMenuScreen>() : null;
        bootstrapSO.FindProperty("resultsScreen").objectReferenceValue = resultsScreen ? resultsScreen.GetComponent<ResultsScreen>() : null;
        bootstrapSO.ApplyModifiedProperties();

        Debug.Log("Bootstrap wired");
    }

    static void SetRect(RectTransform rt, float x, float y, float w, float h) {
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(x, y);
        rt.sizeDelta = new Vector2(w, h);
    }

    static void SetStretch(RectTransform rt) {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = Vector2.zero;
    }

    static GameObject CreateText(string name, Transform parent, string text, int fontSize, float x, float y) {
        var go = new GameObject(name);
        go.transform.SetParent(parent);
        var rt = go.AddComponent<RectTransform>();
        SetRect(rt, x, y, 700, 80);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        return go;
    }

    static GameObject CreateButton(string name, Transform parent, Color color, string text, int fontSize, float x, float y, float w, float h) {
        var btn = new GameObject(name);
        btn.transform.SetParent(parent);
        var rt = btn.AddComponent<RectTransform>();
        SetRect(rt, x, y, w, h);
        var img = btn.AddComponent<Image>();
        img.color = color;
        btn.AddComponent<Button>();

        var txtGO = new GameObject("Text (TMP)");
        txtGO.transform.SetParent(btn.transform);
        var txtRT = txtGO.AddComponent<RectTransform>();
        SetStretch(txtRT);
        var txt = txtGO.AddComponent<TextMeshProUGUI>();
        txt.text = text;
        txt.fontSize = fontSize;
        txt.alignment = TextAlignmentOptions.Center;

        return btn;
    }
}
