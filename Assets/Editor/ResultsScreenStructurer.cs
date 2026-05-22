using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class ResultsScreenStructurer : Editor
{
    [MenuItem("Window/Restructure ResultsScreen")]
    public static void Restructure()
    {
        // Open the scene
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/GameUI.unity", OpenSceneMode.Single);

        // Find ResultsScreen
        var resultsScreenGO = GameObject.Find("ResultsScreen");
        if (resultsScreenGO == null)
        {
            Debug.LogError("ResultsScreen not found");
            return;
        }

        Debug.Log("Starting ResultsScreen restructuring...");

        var resultsScreen = resultsScreenGO.transform;

        // Step 1: Find and organize existing elements
        Transform headerText = resultsScreen.Find("HeaderText");
        Transform finalScoreText = resultsScreen.Find("FinalScoreText");
        Transform finalScoreStat = resultsScreen.Find("FinalScoreStat");
        Transform durationStat = resultsScreen.Find("DurationStat");
        Transform playAgainButton = resultsScreen.Find("PlayAgainButton");
        Transform mainMenuButton = resultsScreen.Find("MainMenuButton");

        if (headerText == null || finalScoreText == null)
        {
            Debug.LogError("Required elements not found");
            return;
        }

        // Step 2: Create StatsList ScrollRect
        GameObject statsListGO = new GameObject("StatsList");
        statsListGO.transform.SetParent(resultsScreen, false);
        statsListGO.transform.SetSiblingIndex(2); // After header and score

        RectTransform statsListRect = statsListGO.AddComponent<RectTransform>();
        statsListRect.anchorMin = Vector2.zero;
        statsListRect.anchorMax = Vector2.one;
        statsListRect.offsetMin = Vector2.zero;
        statsListRect.offsetMax = Vector2.zero;

        Image statsListImage = statsListGO.AddComponent<Image>();
        statsListImage.color = new Color(0, 0, 0, 0);

        ScrollRect scrollRect = statsListGO.AddComponent<ScrollRect>();
        LayoutElement statsListLayout = statsListGO.AddComponent<LayoutElement>();
        statsListLayout.preferredHeight = 250;

        // Step 3: Create StatsContent with VerticalLayoutGroup
        GameObject statsContentGO = new GameObject("StatsContent");
        statsContentGO.transform.SetParent(statsListGO.transform, false);

        RectTransform statsContentRect = statsContentGO.AddComponent<RectTransform>();
        statsContentRect.anchorMin = new Vector2(0, 1);
        statsContentRect.anchorMax = new Vector2(1, 1);
        statsContentRect.pivot = new Vector2(0.5f, 1);

        VerticalLayoutGroup vlg = statsContentGO.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 8;
        vlg.padding = new RectOffset(16, 16, 16, 16);

        LayoutElement contentLayout = statsContentGO.AddComponent<LayoutElement>();
        contentLayout.preferredWidth = 400;

        scrollRect.content = statsContentRect;

        // Step 4: Move existing stat items into StatsContent
        if (finalScoreStat != null)
            finalScoreStat.SetParent(statsContentGO.transform, false);

        if (durationStat != null)
            durationStat.SetParent(statsContentGO.transform, false);

        // Step 5: Create 5 new stat items
        string[] newStatNames = { "WordsFoundStat", "AccuracyStat", "BestWordStat", "CurrentStreakStat", "LongestStreakStat" };
        foreach (var statName in newStatNames)
        {
            CreateStatItem(statsContentGO.transform, statName);
        }

        // Step 6: Create ButtonContainer
        GameObject buttonContainerGO = new GameObject("ButtonContainer");
        buttonContainerGO.transform.SetParent(resultsScreen, false);

        RectTransform buttonContainerRect = buttonContainerGO.AddComponent<RectTransform>();
        buttonContainerRect.anchorMin = Vector2.zero;
        buttonContainerRect.anchorMax = Vector2.one;

        HorizontalLayoutGroup hlg = buttonContainerGO.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 16;
        hlg.padding = new RectOffset(16, 16, 16, 16);
        hlg.childForceExpandHeight = false;
        hlg.childForceExpandWidth = false;

        LayoutElement buttonContainerLayout = buttonContainerGO.AddComponent<LayoutElement>();
        buttonContainerLayout.preferredHeight = 60;

        // Step 7: Move buttons into ButtonContainer
        if (playAgainButton != null)
        {
            playAgainButton.SetParent(buttonContainerGO.transform, false);
            RectTransform btn = playAgainButton as RectTransform;
            if (btn != null)
            {
                btn.sizeDelta = new Vector2(100, 60);
            }
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.SetParent(buttonContainerGO.transform, false);
            RectTransform btn = mainMenuButton as RectTransform;
            if (btn != null)
            {
                btn.sizeDelta = new Vector2(100, 60);
            }
        }

        // Save scene
        EditorSceneManager.SaveScene(scene);
        Debug.Log("ResultsScreen restructuring complete!");

        // Select the ResultsScreen to show the changes
        Selection.activeGameObject = resultsScreenGO;
    }

    private static void CreateStatItem(Transform parent, string itemName)
    {
        // Create the stat item container
        GameObject itemGO = new GameObject(itemName);
        itemGO.transform.SetParent(parent, false);

        RectTransform itemRect = itemGO.AddComponent<RectTransform>();
        itemRect.sizeDelta = new Vector2(300, 40);

        HorizontalLayoutGroup hlg = itemGO.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 10;

        LayoutElement itemLayout = itemGO.AddComponent<LayoutElement>();
        itemLayout.preferredHeight = 40;
        itemLayout.preferredWidth = 300;

        // Create label text
        GameObject labelGO = new GameObject("Label");
        labelGO.transform.SetParent(itemGO.transform, false);

        RectTransform labelRect = labelGO.AddComponent<RectTransform>();
        labelRect.sizeDelta = new Vector2(150, 40);

        TextMeshProUGUI labelText = labelGO.AddComponent<TextMeshProUGUI>();
        labelText.text = itemName.Replace("Stat", "");
        labelText.fontSize = 16;
        labelText.color = Color.white;

        LayoutElement labelLayout = labelGO.AddComponent<LayoutElement>();
        labelLayout.preferredWidth = 150;
        labelLayout.preferredHeight = 40;

        // Create value text
        GameObject valueGO = new GameObject("Value");
        valueGO.transform.SetParent(itemGO.transform, false);

        RectTransform valueRect = valueGO.AddComponent<RectTransform>();
        valueRect.sizeDelta = new Vector2(150, 40);

        TextMeshProUGUI valueText = valueGO.AddComponent<TextMeshProUGUI>();
        valueText.text = "0";
        valueText.fontSize = 16;
        valueText.color = Color.white;
        valueText.alignment = TextAlignmentOptions.Right;

        LayoutElement valueLayout = valueGO.AddComponent<LayoutElement>();
        valueLayout.preferredWidth = 150;
        valueLayout.preferredHeight = 40;
    }
}
