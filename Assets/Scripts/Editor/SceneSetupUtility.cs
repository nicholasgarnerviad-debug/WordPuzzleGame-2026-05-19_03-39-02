#if UNITY_EDITOR

using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;

public class SceneSetupUtility : MonoBehaviour
{
    [MenuItem("Tools/Word Puzzle Game/Setup All Scenes")]
    public static void SetupAllScenes()
    {
        SetupMainMenuScene();
        SetupClassicModeScene();
        SetupPuzzleShowModeScene();
        SetupTimeAttackModeScene();
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        Logger.Log("All scenes set up successfully!");
    }

    [MenuItem("Tools/Word Puzzle Game/Setup Main Menu")]
    public static void SetupMainMenuScene()
    {
        // Create new scene
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Create Canvas
        var canvasGO = new GameObject("Canvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var canvasScaler = canvasGO.AddComponent<CanvasScaler>();
        canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasScaler.referenceResolution = new Vector2(1080, 1920);

        // Create Title
        CreateText(canvasGO.transform, "TitleText", "Word Puzzle Game", new Vector3(0, 300, 0), 48);

        // Create Buttons
        CreateButton(canvasGO.transform, "ClassicModeButton", "Classic Mode", new Vector3(0, 100, 0), new Vector2(300, 80));
        CreateButton(canvasGO.transform, "PuzzleShowButton", "Puzzle Show", new Vector3(0, 0, 0), new Vector2(300, 80));
        CreateButton(canvasGO.transform, "TimeAttackButton", "Time Attack", new Vector3(0, -100, 0), new Vector2(300, 80));
        CreateButton(canvasGO.transform, "ShopButton", "Shop", new Vector3(-300, -250, 0), new Vector2(200, 60));
        CreateButton(canvasGO.transform, "SettingsButton", "Settings", new Vector3(300, -250, 0), new Vector2(200, 60));

        // Add UIManager
        var uiManagerGO = new GameObject("UIManager");
        uiManagerGO.AddComponent<UIManager>();

        // Add persistent managers
        CreatePersistentManager("CoinSystem", typeof(CoinSystem));
        CreatePersistentManager("AdManager", typeof(AdManager));
        CreatePersistentManager("IAPManager", typeof(IAPManager));
        CreatePersistentManager("PlayerDataManager", typeof(PlayerDataManager));

        // Add MainMenuScreen to Canvas
        var mainMenuScreen = canvasGO.AddComponent<MainMenuScreen>();
        AssignButtonReferences(mainMenuScreen, canvasGO.transform);

        // Save scene
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/MainMenu.unity");
        Logger.Log("MainMenu scene created!");
    }

    [MenuItem("Tools/Word Puzzle Game/Setup Classic Mode")]
    public static void SetupClassicModeScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Create GameManager
        var gameManagerGO = new GameObject("GameManager");
        gameManagerGO.AddComponent<GameController>();
        gameManagerGO.AddComponent<ClassicMode>();

        // Create Canvas
        var canvasGO = new GameObject("Canvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var canvasScaler = canvasGO.AddComponent<CanvasScaler>();
        canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasScaler.referenceResolution = new Vector2(1080, 1920);

        // Create UI elements
        CreateText(canvasGO.transform, "ScoreText", "Score: 0", new Vector3(0, 400, 0), 36);
        CreateText(canvasGO.transform, "WordsText", "Words: ", new Vector3(0, 300, 0), 28);
        CreateInputField(canvasGO.transform, "WordInput", "Enter word...", new Vector3(0, 100, 0), new Vector2(400, 60));
        CreateButton(canvasGO.transform, "SubmitButton", "Submit", new Vector3(0, -20, 0), new Vector2(200, 60));
        CreateButton(canvasGO.transform, "HintButton", "Hint", new Vector3(-150, -100, 0), new Vector2(150, 50));
        CreateButton(canvasGO.transform, "UndoButton", "Undo", new Vector3(150, -100, 0), new Vector2(150, 50));

        // Add GameplayScreen
        var gameplayScreen = canvasGO.AddComponent<GameplayScreen>();
        AssignGameplayScreenReferences(gameplayScreen, canvasGO.transform, gameManagerGO);

        // Add persistent managers
        CreatePersistentManager("CoinSystem", typeof(CoinSystem));
        CreatePersistentManager("AdManager", typeof(AdManager));
        CreatePersistentManager("IAPManager", typeof(IAPManager));
        CreatePersistentManager("PlayerDataManager", typeof(PlayerDataManager));

        EditorSceneManager.SaveScene(scene, "Assets/Scenes/ClassicMode.unity");
        Logger.Log("ClassicMode scene created!");
    }

    [MenuItem("Tools/Word Puzzle Game/Setup Puzzle Show")]
    public static void SetupPuzzleShowModeScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Create GameManager
        var gameManagerGO = new GameObject("GameManager");
        gameManagerGO.AddComponent<GameController>();
        gameManagerGO.AddComponent<PuzzleShowMode>();

        // Create Canvas
        var canvasGO = new GameObject("Canvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var canvasScaler = canvasGO.AddComponent<CanvasScaler>();
        canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasScaler.referenceResolution = new Vector2(1080, 1920);

        // Create UI elements
        CreateText(canvasGO.transform, "TierText", "Tier: 1", new Vector3(0, 450, 0), 40);
        CreateText(canvasGO.transform, "ScoreText", "Score: 0", new Vector3(0, 350, 0), 36);
        CreateText(canvasGO.transform, "WordsText", "Words: ", new Vector3(0, 250, 0), 28);
        CreateInputField(canvasGO.transform, "WordInput", "Enter word...", new Vector3(0, 100, 0), new Vector2(400, 60));
        CreateButton(canvasGO.transform, "SubmitButton", "Submit", new Vector3(0, -20, 0), new Vector2(200, 60));
        CreateButton(canvasGO.transform, "HintButton", "Hint", new Vector3(-150, -100, 0), new Vector2(150, 50));
        CreateButton(canvasGO.transform, "UndoButton", "Undo", new Vector3(150, -100, 0), new Vector2(150, 50));

        // Add GameplayScreen
        var gameplayScreen = canvasGO.AddComponent<GameplayScreen>();
        AssignGameplayScreenReferences(gameplayScreen, canvasGO.transform, gameManagerGO);

        // Add persistent managers
        CreatePersistentManager("CoinSystem", typeof(CoinSystem));
        CreatePersistentManager("AdManager", typeof(AdManager));
        CreatePersistentManager("IAPManager", typeof(IAPManager));
        CreatePersistentManager("PlayerDataManager", typeof(PlayerDataManager));

        EditorSceneManager.SaveScene(scene, "Assets/Scenes/PuzzleShowMode.unity");
        Logger.Log("PuzzleShowMode scene created!");
    }

    [MenuItem("Tools/Word Puzzle Game/Setup Time Attack")]
    public static void SetupTimeAttackModeScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Create GameManager
        var gameManagerGO = new GameObject("GameManager");
        gameManagerGO.AddComponent<GameController>();
        gameManagerGO.AddComponent<TimeAttackMode>();

        // Create Canvas
        var canvasGO = new GameObject("Canvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var canvasScaler = canvasGO.AddComponent<CanvasScaler>();
        canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasScaler.referenceResolution = new Vector2(1080, 1920);

        // Create UI elements
        var timerText = CreateText(canvasGO.transform, "TimerText", "Time: 90s", new Vector3(0, 450, 0), 48);
        timerText.GetComponent<Text>().color = Color.red;

        CreateText(canvasGO.transform, "RoundText", "Round: 1", new Vector3(0, 350, 0), 36);
        CreateText(canvasGO.transform, "ScoreText", "Score: 0", new Vector3(0, 250, 0), 36);
        CreateText(canvasGO.transform, "WordsText", "Words: ", new Vector3(-200, 150, 0), 24);
        CreateInputField(canvasGO.transform, "WordInput", "Enter word...", new Vector3(0, 100, 0), new Vector2(400, 60));
        CreateButton(canvasGO.transform, "SubmitButton", "Submit", new Vector3(0, -20, 0), new Vector2(200, 60));
        CreateButton(canvasGO.transform, "HintButton", "Hint", new Vector3(-150, -100, 0), new Vector2(150, 50));
        CreateButton(canvasGO.transform, "UndoButton", "Undo", new Vector3(150, -100, 0), new Vector2(150, 50));

        // Add GameplayScreen
        var gameplayScreen = canvasGO.AddComponent<GameplayScreen>();
        AssignGameplayScreenReferences(gameplayScreen, canvasGO.transform, gameManagerGO);

        // Add persistent managers
        CreatePersistentManager("CoinSystem", typeof(CoinSystem));
        CreatePersistentManager("AdManager", typeof(AdManager));
        CreatePersistentManager("IAPManager", typeof(IAPManager));
        CreatePersistentManager("PlayerDataManager", typeof(PlayerDataManager));

        EditorSceneManager.SaveScene(scene, "Assets/Scenes/TimeAttackMode.unity");
        Logger.Log("TimeAttackMode scene created!");
    }

    // Helper methods
    private static GameObject CreateText(Transform parent, string name, string text, Vector3 position, int fontSize)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent);
        go.transform.localPosition = position;

        var rectTransform = go.AddComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(600, 100);

        var textComponent = go.AddComponent<Text>();
        textComponent.text = text;
        textComponent.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        textComponent.fontSize = fontSize;
        textComponent.fontStyle = FontStyle.Normal;
        textComponent.alignment = TextAnchor.MiddleCenter;

        return go;
    }

    private static GameObject CreateButton(Transform parent, string name, string text, Vector3 position, Vector2 size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent);
        go.transform.localPosition = position;

        var rectTransform = go.AddComponent<RectTransform>();
        rectTransform.sizeDelta = size;

        var image = go.AddComponent<Image>();
        image.color = new Color(0.2f, 0.2f, 0.8f);

        var button = go.AddComponent<Button>();

        // Create text child
        var textGO = new GameObject("Text");
        textGO.transform.SetParent(go.transform);
        textGO.transform.localPosition = Vector3.zero;

        var textRect = textGO.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        var textComponent = textGO.AddComponent<Text>();
        textComponent.text = text;
        textComponent.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        textComponent.fontSize = 24;
        textComponent.fontStyle = FontStyle.Normal;
        textComponent.alignment = TextAnchor.MiddleCenter;
        textComponent.color = Color.white;

        button.targetGraphic = image;

        return go;
    }

    private static GameObject CreateInputField(Transform parent, string name, string placeholder, Vector3 position, Vector2 size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent);
        go.transform.localPosition = position;

        var rectTransform = go.AddComponent<RectTransform>();
        rectTransform.sizeDelta = size;

        var image = go.AddComponent<Image>();
        image.color = Color.white;

        var inputField = go.AddComponent<InputField>();

        // Create text child for input display
        var textGO = new GameObject("Text");
        textGO.transform.SetParent(go.transform);
        textGO.transform.localPosition = Vector3.zero;

        var textRect = textGO.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(10, 0);
        textRect.offsetMax = new Vector2(-10, 0);

        var textComponent = textGO.AddComponent<Text>();
        textComponent.text = "";
        textComponent.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        textComponent.fontSize = 24;
        textComponent.fontStyle = FontStyle.Normal;
        textComponent.alignment = TextAnchor.MiddleLeft;
        textComponent.color = Color.black;

        // Create placeholder text
        var placeholderGO = new GameObject("Placeholder");
        placeholderGO.transform.SetParent(go.transform);
        placeholderGO.transform.localPosition = Vector3.zero;

        var placeholderRect = placeholderGO.AddComponent<RectTransform>();
        placeholderRect.anchorMin = Vector2.zero;
        placeholderRect.anchorMax = Vector2.one;
        placeholderRect.offsetMin = new Vector2(10, 0);
        placeholderRect.offsetMax = new Vector2(-10, 0);

        var placeholderComponent = placeholderGO.AddComponent<Text>();
        placeholderComponent.text = placeholder;
        placeholderComponent.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        placeholderComponent.fontSize = 24;
        placeholderComponent.fontStyle = FontStyle.Italic;
        placeholderComponent.alignment = TextAnchor.MiddleLeft;
        placeholderComponent.color = new Color(1, 1, 1, 0.5f);

        inputField.textComponent = textComponent;
        inputField.placeholder = placeholderComponent;

        return go;
    }

    private static void CreatePersistentManager(string name, System.Type componentType)
    {
        var go = new GameObject(name);
        go.AddComponent(componentType);
    }

    private static void AssignButtonReferences(MainMenuScreen screen, Transform canvas)
    {
        var classicBtn = canvas.Find("ClassicModeButton")?.GetComponent<Button>();
        var puzzleBtn = canvas.Find("PuzzleShowButton")?.GetComponent<Button>();
        var timeBtn = canvas.Find("TimeAttackButton")?.GetComponent<Button>();
        var shopBtn = canvas.Find("ShopButton")?.GetComponent<Button>();
        var settingsBtn = canvas.Find("SettingsButton")?.GetComponent<Button>();

        var serializedObject = new SerializedObject(screen);
        serializedObject.FindProperty("classicModeButton").objectReferenceValue = classicBtn;
        serializedObject.FindProperty("puzzleShowButton").objectReferenceValue = puzzleBtn;
        serializedObject.FindProperty("timeAttackButton").objectReferenceValue = timeBtn;
        serializedObject.FindProperty("shopButton").objectReferenceValue = shopBtn;
        serializedObject.FindProperty("settingsButton").objectReferenceValue = settingsBtn;
        serializedObject.ApplyModifiedProperties();
    }

    private static void AssignGameplayScreenReferences(GameplayScreen screen, Transform canvas, GameObject gameManager)
    {
        var serializedObject = new SerializedObject(screen);
        serializedObject.FindProperty("gameController").objectReferenceValue = gameManager.GetComponent<GameController>();
        serializedObject.FindProperty("submitButton").objectReferenceValue = canvas.Find("SubmitButton")?.GetComponent<Button>();
        serializedObject.FindProperty("hintButton").objectReferenceValue = canvas.Find("HintButton")?.GetComponent<Button>();
        serializedObject.FindProperty("undoButton").objectReferenceValue = canvas.Find("UndoButton")?.GetComponent<Button>();
        serializedObject.FindProperty("scoreText").objectReferenceValue = canvas.Find("ScoreText")?.GetComponent<Text>();
        serializedObject.FindProperty("wordsText").objectReferenceValue = canvas.Find("WordsText")?.GetComponent<Text>();
        serializedObject.FindProperty("wordInput").objectReferenceValue = canvas.Find("WordInput")?.GetComponent<InputField>();
        serializedObject.ApplyModifiedProperties();
    }
}

#endif
