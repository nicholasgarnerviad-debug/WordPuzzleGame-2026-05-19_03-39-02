using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using WordPuzzle.UI;

[InitializeOnLoad]
public class InitializeScene
{
    static InitializeScene()
    {
        EditorApplication.delayCall += SetupScene;
    }

    private static void SetupScene()
    {
        // Load GameUI scene if not already loaded
        var scene = EditorSceneManager.GetSceneByName("GameUI");
        if (!scene.isLoaded)
        {
            scene = EditorSceneManager.OpenScene("Assets/Scenes/GameUI.unity", OpenSceneMode.Single);
        }

        // Now run the fix
        RunFixBootstrap();
    }

    private static void RunFixBootstrap()
    {
        Debug.Log("=== InitializeScene: Setting up Bootstrap ===");

        var bootstrap = GameObject.Find("Bootstrap");
        if (bootstrap == null)
        {
            Debug.LogError("Bootstrap GameObject not found in scene");
            return;
        }

        var canvas = GameObject.Find("Canvas");
        if (canvas == null)
        {
            Debug.LogError("Canvas GameObject not found in scene");
            return;
        }

        try
        {
            // Add GameBootstrap
            if (bootstrap.GetComponent("WordPuzzle.GameBootstrap") == null)
            {
                var asm = System.Reflection.Assembly.Load("Game.Bootstrap");
                var type = asm.GetType("WordPuzzle.GameBootstrap");
                bootstrap.AddComponent(type);
                Debug.Log("✓ Added GameBootstrap");
            }

            // Add UIManager
            if (bootstrap.GetComponent("WordPuzzle.UI.UIManager") == null)
            {
                var asm = System.Reflection.Assembly.Load("Game.UI");
                var type = asm.GetType("WordPuzzle.UI.UIManager");
                bootstrap.AddComponent(type);
                Debug.Log("✓ Added UIManager");
            }

            // Find screen components
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

            // Get components
            var gameBootstrap = bootstrap.GetComponent("WordPuzzle.GameBootstrap") as MonoBehaviour;
            var uiManager = bootstrap.GetComponent("WordPuzzle.UI.UIManager") as MonoBehaviour;

            // ModeController is created by GameBootstrap, not added as a component
            if (gameBootstrap != null && uiManager != null)
            {
                // Wire GameBootstrap
                var bootstrapSO = new SerializedObject(gameBootstrap);
                bootstrapSO.FindProperty("uiManager").objectReferenceValue = uiManager;
                bootstrapSO.FindProperty("gameplayScreen").objectReferenceValue = gameplayScreenComp;
                bootstrapSO.FindProperty("mainMenuScreen").objectReferenceValue = mainMenuScreenComp;
                bootstrapSO.FindProperty("resultsScreen").objectReferenceValue = resultsScreenComp;
                bootstrapSO.ApplyModifiedProperties();
                Debug.Log("✓ Wired GameBootstrap");

                // Wire UIManager
                var uiManagerSO = new SerializedObject(uiManager);
                uiManagerSO.FindProperty("mainMenuScreen").objectReferenceValue = mainMenuScreenComp;
                uiManagerSO.FindProperty("gameplayScreen").objectReferenceValue = gameplayScreenComp;
                uiManagerSO.FindProperty("resultsScreen").objectReferenceValue = resultsScreenComp;
                uiManagerSO.ApplyModifiedProperties();
                Debug.Log("✓ Wired UIManager");

                EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
                Debug.Log("=== InitializeScene: Setup Complete ===");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error during initialization: {ex.Message}\n{ex.StackTrace}");
        }
    }
}
