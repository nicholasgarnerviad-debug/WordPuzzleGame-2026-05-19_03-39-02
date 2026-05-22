using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using WordPuzzle.UI;
using WordPuzzle.Modes;

public class AutoFixBootstrap
{
    [InitializeOnLoadMethod]
    private static void AutoFixOnLoad()
    {
        EditorApplication.update += CheckAndFixBootstrap;
    }

    private static bool hasRun = false;

    private static void CheckAndFixBootstrap()
    {
        if (hasRun) return;

        // Only run in editor, not in play mode
        if (EditorApplication.isPlaying) return;

        // Check if we need to fix
        var bootstrap = GameObject.Find("Bootstrap");
        if (bootstrap == null) return;

        var gameBootstrap = bootstrap.GetComponent<WordPuzzle.GameBootstrap>();
        var uiManager = bootstrap.GetComponent<WordPuzzle.UI.UIManager>();

        // If all components exist, we're done (ModeController is created by GameBootstrap, not a component)
        if (gameBootstrap != null && uiManager != null)
        {
            hasRun = true;
            EditorApplication.update -= CheckAndFixBootstrap;
            return;
        }

        // Otherwise run the fix
        FixBootstrap();
        hasRun = true;
        EditorApplication.update -= CheckAndFixBootstrap;
    }

    private static void FixBootstrap()
    {
        Debug.Log("=== AutoFixBootstrap: Fixing Bootstrap Wiring ===");

        var bootstrap = GameObject.Find("Bootstrap");
        if (!bootstrap) { Debug.LogError("Bootstrap not found"); return; }

        var canvas = GameObject.Find("Canvas");
        if (!canvas) { Debug.LogError("Canvas not found"); return; }

        // ADD COMPONENTS IF MISSING
        Debug.Log("Adding missing components to Bootstrap...");

        var gameBootstrap = bootstrap.GetComponent<WordPuzzle.GameBootstrap>();
        if (gameBootstrap == null)
        {
            gameBootstrap = bootstrap.AddComponent<WordPuzzle.GameBootstrap>();
            Debug.Log("✓ Added GameBootstrap component");
        }

        var uiManager = bootstrap.GetComponent<WordPuzzle.UI.UIManager>();
        if (uiManager == null)
        {
            uiManager = bootstrap.AddComponent<WordPuzzle.UI.UIManager>();
            Debug.Log("✓ Added UIManager component");
        }

        // Note: ModeController is not a MonoBehaviour, so it's created in GameBootstrap, not here
        // var modeController = bootstrap.GetComponent<WordPuzzle.Modes.ModeController>();
        // if (modeController == null)
        // {
        //     Debug.Log("ModeController will be created by GameBootstrap");
        // }

        // Find screens
        GameplayScreen gameplayScreenComp = null;
        MainMenuScreen mainMenuScreenComp = null;
        ResultsScreen resultsScreenComp = null;
        TimerDisplay timerDisplayComp = null;

        foreach (Transform child in canvas.transform)
        {
            if (child.name == "GameplayScreen")
                gameplayScreenComp = child.GetComponent<GameplayScreen>();
            else if (child.name == "MainMenuScreen")
                mainMenuScreenComp = child.GetComponent<MainMenuScreen>();
            else if (child.name == "ResultsScreen")
                resultsScreenComp = child.GetComponent<ResultsScreen>();
            else if (child.name == "TimerDisplay")
                timerDisplayComp = child.GetComponent<TimerDisplay>();
        }

        // Wire GameBootstrap
        var bootstrapSO = new SerializedObject(gameBootstrap);
        // modeController is created in GameBootstrap.InitializeGameSystems(), not wired here
        bootstrapSO.FindProperty("uiManager").objectReferenceValue = uiManager;
        bootstrapSO.FindProperty("gameplayScreen").objectReferenceValue = gameplayScreenComp;
        bootstrapSO.FindProperty("mainMenuScreen").objectReferenceValue = mainMenuScreenComp;
        bootstrapSO.FindProperty("resultsScreen").objectReferenceValue = resultsScreenComp;
        bootstrapSO.ApplyModifiedProperties();

        // Wire UIManager
        var uiManagerSO = new SerializedObject(uiManager);
        uiManagerSO.FindProperty("mainMenuScreen").objectReferenceValue = mainMenuScreenComp;
        uiManagerSO.FindProperty("gameplayScreen").objectReferenceValue = gameplayScreenComp;
        uiManagerSO.FindProperty("resultsScreen").objectReferenceValue = resultsScreenComp;
        uiManagerSO.ApplyModifiedProperties();

        // Note: ModeController is not a MonoBehaviour and is created in GameBootstrap.InitializeGameSystems()
        // var modeControllerSO = new SerializedObject(modeController);
        // modeControllerSO.FindProperty("timerDisplay").objectReferenceValue = timerDisplayComp;
        // modeControllerSO.ApplyModifiedProperties();

        EditorSceneManager.SaveOpenScenes();
        Debug.Log("=== AutoFixBootstrap: Complete ===");
    }
}
