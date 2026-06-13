using UnityEngine;
using System.Reflection;
using WordPuzzle.UI;
using WordPuzzle.Modes;

namespace WordPuzzle
{
    /// <summary>
    /// Temporary initializer to set up Bootstrap since YAML deserialization isn't working.
    /// This ensures all components are present and wired before GameBootstrap.Start() runs.
    /// </summary>
    public class BootstrapInitializer : MonoBehaviour
    {
        private void Awake()
        {
            // Find the Bootstrap GameObject
            var bootstrap = GameObject.Find("Bootstrap");
            if (bootstrap == null)
            {
                Debug.LogError("Bootstrap GameObject not found!");
                return;
            }

            // Add GameBootstrap if missing
            var gameBootstrap = bootstrap.GetComponent<GameBootstrap>();
            if (gameBootstrap == null)
            {
                gameBootstrap = bootstrap.AddComponent<GameBootstrap>();
                GameLog.Log("Added GameBootstrap component");
            }

            // Add UIManager if missing
            var uiManager = bootstrap.GetComponent<UIManager>();
            if (uiManager == null)
            {
                uiManager = bootstrap.AddComponent<UIManager>();
                GameLog.Log("Added UIManager component");
            }

            // v1.0 audit Track 1 - the production ad stack was never attached ANYWHERE, so
            // GameBootstrap's GetComponent<IAdService>() always fell back to NullAdService and
            // ads were dead code. Ensure it here like the other components (unit IDs load from
            // Resources/Config/ad_units.json inside AdService; editor stays ad-inert).
            if (bootstrap.GetComponent<AdService>() == null)
            {
                bootstrap.AddComponent<AdService>();
                GameLog.Log("Added AdService component");
            }

            // Find UI elements
            var canvas = GameObject.Find("Canvas");
            if (canvas == null)
            {
                Debug.LogError("Canvas not found!");
                Destroy(this.gameObject);
                return;
            }

            MainMenuScreen mainMenuScreen = null;
            GameplayScreen gameplayScreen = null;
            ResultsScreen resultsScreen = null;
            TimerDisplay timerDisplay = null;

            foreach (Transform child in canvas.transform)
            {
                if (child.name == "MainMenuScreen")
                    mainMenuScreen = child.GetComponent<MainMenuScreen>();
                else if (child.name == "GameplayScreen")
                    gameplayScreen = child.GetComponent<GameplayScreen>();
                else if (child.name == "ResultsScreen")
                    resultsScreen = child.GetComponent<ResultsScreen>();
                else if (child.name == "TimerDisplay")
                    timerDisplay = child.GetComponent<TimerDisplay>();
            }

            // Wire UIManager using reflection
            if (uiManager != null)
            {
                SetFieldValue(uiManager, "mainMenuScreen", mainMenuScreen);
                SetFieldValue(uiManager, "gameplayScreen", gameplayScreen);
                SetFieldValue(uiManager, "resultsScreen", resultsScreen);
                GameLog.Log("Wired UIManager references");
            }

            // Wire GameBootstrap using reflection
            if (gameBootstrap != null)
            {
                SetFieldValue(gameBootstrap, "uiManager", uiManager);
                SetFieldValue(gameBootstrap, "gameplayScreen", gameplayScreen);
                SetFieldValue(gameBootstrap, "mainMenuScreen", mainMenuScreen);
                SetFieldValue(gameBootstrap, "resultsScreen", resultsScreen);
                GameLog.Log("Wired GameBootstrap references");
            }

            GameLog.Log("Bootstrap initialization complete");

            // Destroy this initializer - its job is done
            Destroy(this.gameObject);
        }

        private void SetFieldValue(object obj, string fieldName, object value)
        {
            var fieldInfo = obj.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            if (fieldInfo != null)
            {
                fieldInfo.SetValue(obj, value);
            }
            else
            {
                Debug.LogWarning($"Field '{fieldName}' not found on {obj.GetType().Name}");
            }
        }
    }
}
