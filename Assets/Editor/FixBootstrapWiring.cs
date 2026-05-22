using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using WordPuzzle.UI;
using WordPuzzle.Modes;

public class FixBootstrapWiring
{
    [MenuItem("Tools/Fix Bootstrap Wiring")]
    public static void FixBootstrap()
    {
        Debug.Log("=== Fixing Bootstrap Wiring ===");

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
        else
        {
            Debug.Log("✓ GameBootstrap already present");
        }

        var uiManager = bootstrap.GetComponent<WordPuzzle.UI.UIManager>();
        if (uiManager == null)
        {
            uiManager = bootstrap.AddComponent<WordPuzzle.UI.UIManager>();
            Debug.Log("✓ Added UIManager component");
        }
        else
        {
            Debug.Log("✓ UIManager already present");
        }

        // Note: ModeController is not a MonoBehaviour, so it's created in GameBootstrap, not here
        // var modeController = bootstrap.GetComponent<WordPuzzle.Modes.ModeController>();
        // if (modeController == null)
        // {
        //     Debug.Log("ModeController will be created by GameBootstrap");
        // }

        // Find screens by searching canvas children more robustly
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

        if (gameplayScreenComp) Debug.Log("✓ GameplayScreen wired");
        else Debug.LogWarning("✗ GameplayScreen not found");

        if (mainMenuScreenComp) Debug.Log("✓ MainMenuScreen wired");
        else Debug.LogWarning("✗ MainMenuScreen not found");

        if (resultsScreenComp) Debug.Log("✓ ResultsScreen wired");
        else Debug.LogWarning("✗ ResultsScreen not found");

        // Wire UIManager
        var uiManagerSO = new SerializedObject(uiManager);
        uiManagerSO.FindProperty("mainMenuScreen").objectReferenceValue = mainMenuScreenComp;
        uiManagerSO.FindProperty("gameplayScreen").objectReferenceValue = gameplayScreenComp;
        uiManagerSO.FindProperty("resultsScreen").objectReferenceValue = resultsScreenComp;
        uiManagerSO.ApplyModifiedProperties();
        Debug.Log("✓ UIManager wired");

        // Note: ModeController is not a MonoBehaviour and is created in GameBootstrap.InitializeGameSystems()
        // var modeControllerSO = new SerializedObject(modeController);
        // modeControllerSO.FindProperty("timerDisplay").objectReferenceValue = timerDisplayComp;
        // modeControllerSO.ApplyModifiedProperties();

        if (timerDisplayComp) Debug.Log("✓ TimerDisplay wired");
        else Debug.LogWarning("✗ TimerDisplay not found");

        EditorSceneManager.SaveOpenScenes();
        Debug.Log("=== Bootstrap Wiring Fixed ===");
    }
}
