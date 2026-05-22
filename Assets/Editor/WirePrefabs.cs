using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using WordPuzzle.UI;

public class WirePrefabs
{
    [MenuItem("Tools/Wire Prefabs & Cleanup")]
    public static void WireAndCleanup()
    {
        Debug.Log("=== Wiring Prefabs & Cleanup ===");

        // Load prefabs
        var letterTilePrefab = AssetDatabase.LoadAssetAtPath<LetterTile>("Assets/Prefabs/LetterTile.prefab");
        var wordLabelPrefab = AssetDatabase.LoadAssetAtPath<TextMeshProUGUI>("Assets/Prefabs/WordLabel.prefab");

        if (!letterTilePrefab) { Debug.LogError("LetterTile prefab not found"); return; }
        if (!wordLabelPrefab) { Debug.LogError("WordLabel prefab not found"); return; }

        // Wire GameplayScreen
        var gameplayScreen = GameObject.Find("Canvas/GameplayScreen");
        if (gameplayScreen)
        {
            var gs = gameplayScreen.GetComponent<GameplayScreen>();
            if (gs)
            {
                var gsSO = new SerializedObject(gs);
                gsSO.FindProperty("letterTilePrefab").objectReferenceValue = letterTilePrefab;
                gsSO.ApplyModifiedProperties();
                Debug.Log("GameplayScreen letterTilePrefab wired");
            }
        }

        // Wire WordChainDisplay
        var wordChainDisplay = GameObject.Find("Canvas/GameplayScreen/WordChainDisplay");
        if (wordChainDisplay)
        {
            var wcd = wordChainDisplay.GetComponent<WordChainDisplay>();
            if (wcd)
            {
                var wcdSO = new SerializedObject(wcd);
                wcdSO.FindProperty("wordPrefab").objectReferenceValue = wordLabelPrefab;
                wcdSO.ApplyModifiedProperties();
                Debug.Log("WordChainDisplay wordPrefab wired");
            }
        }

        // Clean up duplicate MainMenuScreen (keep the first one, delete the second)
        var allObjects = GameObject.FindObjectsByType<GameObject>(FindObjectsInactive.Include);
        int menuScreenCount = 0;
        GameObject secondMenuScreen = null;
        var canvasTransform = GameObject.Find("Canvas").transform;

        foreach (var obj in allObjects)
        {
            if (obj.name == "MainMenuScreen" && obj.transform.parent == canvasTransform)
            {
                menuScreenCount++;
                if (menuScreenCount == 2)
                {
                    secondMenuScreen = obj;
                    break;
                }
            }
        }

        if (secondMenuScreen)
        {
            GameObject.DestroyImmediate(secondMenuScreen);
            Debug.Log("Removed duplicate MainMenuScreen");
        }

        EditorSceneManager.SaveOpenScenes();
        Debug.Log("=== Wiring & Cleanup Complete ===");
    }
}
