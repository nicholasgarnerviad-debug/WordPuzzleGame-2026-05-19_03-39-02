using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using WordPuzzle.UI;
using WordPuzzle.UI.Components;

/// <summary>
/// One-shot editor tool: adds the on-screen keyboard UI to GameplayScreen.
/// Run via Tools > Setup > Add Keyboard To Scene.
/// </summary>
public static class AddKeyboardToScene
{
    [MenuItem("Tools/Setup/Add Keyboard To Scene")]
    public static void Run()
    {
        var scene = EditorSceneManager.GetActiveScene();

        // Remove stale copies (must search inactive too)
        RemoveIfExists("KeyboardRoot");
        RemoveIfExists("CurrentInputText");

        var gameplayGO = FindInactiveByName("GameplayScreen");
        if (gameplayGO == null)
        {
            Debug.LogError("[AddKeyboard] GameplayScreen not found.");
            return;
        }

        var inputTextGO = CreateCurrentInputText(gameplayGO);
        var kbRootGO = CreateKeyboardRoot(gameplayGO, inputTextGO);
        WireGameplayScreen(gameplayGO, kbRootGO, inputTextGO);

        EditorUtility.SetDirty(gameplayGO);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[AddKeyboard] Done. Scene saved.");
    }

    private static GameObject FindInactiveByName(string name)
    {
        var all = Resources.FindObjectsOfTypeAll<GameObject>();
        foreach (var go in all)
            if (go.name == name && go.scene.IsValid()) return go;
        return null;
    }

    private static void RemoveIfExists(string name)
    {
        var go = FindInactiveByName(name);
        if (go != null) Object.DestroyImmediate(go);
    }

    private static GameObject CreateCurrentInputText(GameObject parent)
    {
        var go = new GameObject("CurrentInputText");
        go.transform.SetParent(parent.transform, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(0f, -160f);
        rt.sizeDelta = new Vector2(600f, 60f);

        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = "";
        tmp.fontSize = 48f;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontStyle = FontStyles.Bold;
        return go;
    }

    private static GameObject CreateKeyboardRoot(GameObject parent, GameObject inputTextGO)
    {
        var go = new GameObject("KeyboardRoot");
        go.transform.SetParent(parent.transform, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(0f, -280f);
        rt.sizeDelta = new Vector2(800f, 300f);

        var kb = go.AddComponent<OnScreenKeyboard>();
        SetPrivateField(kb, "keyboardRoot", go.transform);
        SetPrivateField(kb, "currentInputDisplay", inputTextGO.GetComponent<TextMeshProUGUI>());
        return go;
    }

    private static void WireGameplayScreen(GameObject gameplayGO, GameObject kbRootGO, GameObject inputTextGO)
    {
        var gp = gameplayGO.GetComponent<GameplayScreen>();
        if (gp == null) { Debug.LogError("[AddKeyboard] GameplayScreen component missing."); return; }
        SetPrivateField(gp, "keyboard", kbRootGO.GetComponent<OnScreenKeyboard>());
        SetPrivateField(gp, "currentInputText", inputTextGO.GetComponent<TextMeshProUGUI>());
    }

    private static void SetPrivateField(object obj, string fieldName, object value)
    {
        var field = obj.GetType().GetField(fieldName,
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null)
            field.SetValue(obj, value);
        else
            Debug.LogWarning($"[AddKeyboard] Field '{fieldName}' not found on {obj.GetType().Name}");
    }
}
