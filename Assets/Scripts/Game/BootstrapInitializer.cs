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
                Debug.Log("Added GameBootstrap component");
            }

            // Add UIManager if missing
            var uiManager = bootstrap.GetComponent<UIManager>();
            if (uiManager == null)
            {
                uiManager = bootstrap.AddComponent<UIManager>();
                Debug.Log("Added UIManager component");
            }

            // Add ModeController if missing
            var modeController = bootstrap.GetComponent<ModeController>();
            if (modeController == null)
            {
                modeController = bootstrap.AddComponent<ModeController>();
                Debug.Log("Added ModeController component");
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
                Debug.Log("Wired UIManager references");
            }

            // Wire GameBootstrap using reflection
            if (gameBootstrap != null)
            {
                SetFieldValue(gameBootstrap, "modeController", modeController);
                SetFieldValue(gameBootstrap, "uiManager", uiManager);
                SetFieldValue(gameBootstrap, "gameplayScreen", gameplayScreen);
                SetFieldValue(gameBootstrap, "mainMenuScreen", mainMenuScreen);
                SetFieldValue(gameBootstrap, "resultsScreen", resultsScreen);
                Debug.Log("Wired GameBootstrap references");
            }

            // Wire ModeController using reflection
            if (modeController != null && timerDisplay != null)
            {
                SetFieldValue(modeController, "timerDisplay", timerDisplay);
                Debug.Log("Wired ModeController references");
            }

            Debug.Log("Bootstrap initialization complete");

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
