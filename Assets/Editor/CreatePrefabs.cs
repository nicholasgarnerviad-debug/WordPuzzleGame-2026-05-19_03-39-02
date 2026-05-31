using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using WordPuzzle.UI;
using WordPuzzle.UI.Components;

public class CreatePrefabs
{
    [MenuItem("Tools/Create Prefabs")]
    public static void CreateAllPrefabs()
    {
        Debug.Log("=== Creating Prefabs ===");

        // Ensure Prefabs folder exists
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            AssetDatabase.CreateFolder("Assets", "Prefabs");

        CreateLetterTilePrefab();
        CreateWordLabelPrefab();

        Debug.Log("=== Prefabs Created ===");
        EditorSceneManager.SaveOpenScenes();
    }

    static void CreateLetterTilePrefab()
    {
        var canvas = GameObject.Find("Canvas");
        if (!canvas) { Debug.LogError("Canvas not found"); return; }

        var tileGO = new GameObject("LetterTile");
        tileGO.transform.SetParent(canvas.transform, false);
        tileGO.layer = LayerMask.NameToLayer("UI");

        var rt = tileGO.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(96, 96);

        var bg = tileGO.AddComponent<Image>();
        bg.color = Color.white;

        tileGO.AddComponent<Button>();
        var letterTile = tileGO.AddComponent<LetterTile>();

        // Add LetterText child
        var textGO = new GameObject("LetterText");
        textGO.transform.SetParent(tileGO.transform, false);
        textGO.layer = LayerMask.NameToLayer("UI");

        var textRT = textGO.AddComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.sizeDelta = Vector2.zero;

        var tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.text = "A";
        tmp.fontSize = 50;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.black;

        // Wire LetterTile component
        var so = new SerializedObject(letterTile);
        so.FindProperty("button").objectReferenceValue = tileGO.GetComponent<Button>();
        so.FindProperty("letterText").objectReferenceValue = tmp;
        so.FindProperty("background").objectReferenceValue = bg;
        so.ApplyModifiedProperties();

        // Save as prefab
        var prefab = PrefabUtility.SaveAsPrefabAsset(tileGO, "Assets/Prefabs/LetterTile.prefab");
        GameObject.DestroyImmediate(tileGO);

        if (prefab) Debug.Log("LetterTile prefab created");
        else Debug.LogError("Failed to create LetterTile prefab");
    }

    static void CreateWordLabelPrefab()
    {
        var canvas = GameObject.Find("Canvas");
        if (!canvas) { Debug.LogError("Canvas not found"); return; }

        var labelGO = new GameObject("WordLabel");
        labelGO.transform.SetParent(canvas.transform, false);
        labelGO.layer = LayerMask.NameToLayer("UI");

        var rt = labelGO.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(800, 70);

        var tmp = labelGO.AddComponent<TextMeshProUGUI>();
        tmp.text = "WORD";
        tmp.fontSize = 55;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;

        // Save as prefab
        var prefab = PrefabUtility.SaveAsPrefabAsset(labelGO, "Assets/Prefabs/WordLabel.prefab");
        GameObject.DestroyImmediate(labelGO);

        if (prefab) Debug.Log("WordLabel prefab created");
        else Debug.LogError("Failed to create WordLabel prefab");
    }
}
