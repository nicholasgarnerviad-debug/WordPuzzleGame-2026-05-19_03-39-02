using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

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
        var modeController = bootstrap.GetComponent<ModeController>();
        var uiManager = bootstrap.GetComponent<UIManager>();

        // Wire GameBootstrap
        var bootstrapComp = bootstrap.GetComponent<GameBootstrap>();
        var bootstrapSO = new SerializedObject(bootstrapComp);
        bootstrapSO.FindProperty("modeController").objectReferenceValue = modeController;
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

        // Wire ModeController timer display
        var modeControllerSO = new SerializedObject(modeController);
        modeControllerSO.FindProperty("timerDisplay").objectReferenceValue = timerDisplayComp;
        modeControllerSO.ApplyModifiedProperties();

        if (timerDisplayComp) Debug.Log("✓ TimerDisplay wired");
        else Debug.LogWarning("✗ TimerDisplay not found");

        EditorSceneManager.SaveOpenScenes();
        Debug.Log("=== Bootstrap Wiring Fixed ===");
    }
}
