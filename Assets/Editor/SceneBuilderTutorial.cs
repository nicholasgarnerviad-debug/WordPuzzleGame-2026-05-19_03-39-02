using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using WordPuzzle.UI;

namespace WordPuzzleGame.EditorTools
{
    /// <summary>
    /// Builds the TutorialOverlay UI under the gameplay Canvas, assigns all
    /// SerializedFields on TutorialOverlay, GameBootstrap.tutorialOverlay, and
    /// SettingsScreen.replayTutorialButton. Idempotent.
    /// MenuItem: Tools/WordPuzzle/Build Tutorial UI
    /// </summary>
    public static class SceneBuilderTutorial
    {
        private const string LOG = "[SceneBuilderTutorial]";

        private static readonly Color C_PANEL  = Hex("#1B1F27");
        private static readonly Color C_GOLD   = Hex("#C9B458");
        private static readonly Color C_TEXT   = Hex("#E7E1C4");

        [MenuItem("Tools/WordPuzzle/Build Tutorial UI")]
        public static void BuildTutorialUI()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError($"{LOG} Cannot run during play mode."); return;
            }

            var canvas = GameObject.Find("Canvas");
            if (canvas == null) { Debug.LogError($"{LOG} Canvas not found."); return; }

            // --- Locate required scene components ---
            var bootstrap = FindFirst<WordPuzzle.GameBootstrap>();
            if (bootstrap == null) { Debug.LogError($"{LOG} GameBootstrap not found."); return; }

            var settings = FindFirst<WordPuzzle.UI.SettingsScreen>();
            if (settings == null) { Debug.LogError($"{LOG} SettingsScreen not found."); return; }

            // --- Build or reuse TutorialOverlay root (inactive, non-modal) ---
            TutorialOverlay overlay = FindFirst<TutorialOverlay>();
            GameObject rootGO;
            if (overlay != null)
            {
                rootGO = overlay.gameObject;
                Debug.Log($"{LOG} Reusing existing TutorialOverlay root.");
            }
            else
            {
                rootGO = new GameObject("TutorialOverlay", typeof(RectTransform));
                rootGO.transform.SetParent(canvas.transform, false);
                Undo.RegisterCreatedObjectUndo(rootGO, "Create TutorialOverlay");
            }

            // Root must start INACTIVE — Begin() activates it
            rootGO.SetActive(false);

            // Stretch root to fill canvas but NO raycast-blocking Image
            var rootRt = rootGO.GetComponent<RectTransform>();
            rootRt.anchorMin = Vector2.zero;
            rootRt.anchorMax = Vector2.one;
            rootRt.offsetMin = Vector2.zero;
            rootRt.offsetMax = Vector2.zero;

            // --- Callout panel (bg-surface + accent-gold outline) ---
            var calloutPanel = GetOrCreate(rootGO.transform, "CalloutPanel",
                typeof(RectTransform), typeof(Image), typeof(Outline));
            var panelRt = calloutPanel.GetComponent<RectTransform>();
            panelRt.anchorMin = panelRt.anchorMax = new Vector2(0.5f, 0.85f);
            panelRt.pivot = new Vector2(0.5f, 0.5f);
            panelRt.anchoredPosition = Vector2.zero;
            panelRt.sizeDelta = new Vector2(900f, 180f);
            var panelImg = calloutPanel.GetComponent<Image>();
            panelImg.color = C_PANEL;
            panelImg.raycastTarget = false;
            var panelOutline = calloutPanel.GetComponent<Outline>();
            panelOutline.effectColor = C_GOLD;
            panelOutline.effectDistance = new Vector2(2f, 2f);

            // --- Callout text label ---
            var textGO = GetOrCreate(calloutPanel.transform, "CalloutText",
                typeof(RectTransform), typeof(TextMeshProUGUI));
            var textRt = textGO.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = new Vector2(20f, 10f);
            textRt.offsetMax = new Vector2(-20f, -10f);
            var tmp = textGO.GetComponent<TextMeshProUGUI>();
            tmp.text = "Change ONE letter to make a new word.";
            tmp.fontSize = 38f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = C_TEXT;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;

            // --- Skip button (accent-gold outline, no fill) ---
            var skipGO = GetOrCreate(rootGO.transform, "SkipButton",
                typeof(RectTransform), typeof(Image), typeof(Button), typeof(Outline));
            var skipRt = skipGO.GetComponent<RectTransform>();
            skipRt.anchorMin = skipRt.anchorMax = new Vector2(0.5f, 0.75f);
            skipRt.pivot = new Vector2(0.5f, 0.5f);
            skipRt.anchoredPosition = Vector2.zero;
            skipRt.sizeDelta = new Vector2(260f, 90f);
            var skipImg = skipGO.GetComponent<Image>();
            skipImg.color = C_PANEL;
            skipImg.raycastTarget = true;
            var skipOutline = skipGO.GetComponent<Outline>();
            skipOutline.effectColor = C_GOLD;
            skipOutline.effectDistance = new Vector2(2f, 2f);
            var skipBtn = skipGO.GetComponent<Button>();
            var skipCols = skipBtn.colors;
            skipCols.normalColor    = C_PANEL;
            skipCols.highlightedColor = Hex("#242936");
            skipCols.pressedColor   = Hex("#242936");
            skipBtn.colors = skipCols;

            // Skip button label
            var skipLabelGO = GetOrCreate(skipGO.transform, "Label",
                typeof(RectTransform), typeof(TextMeshProUGUI));
            var skipLabelRt = skipLabelGO.GetComponent<RectTransform>();
            skipLabelRt.anchorMin = Vector2.zero;
            skipLabelRt.anchorMax = Vector2.one;
            skipLabelRt.offsetMin = Vector2.zero;
            skipLabelRt.offsetMax = Vector2.zero;
            var skipTmp = skipLabelGO.GetComponent<TextMeshProUGUI>();
            skipTmp.text = "SKIP";
            skipTmp.fontSize = 34f;
            skipTmp.fontStyle = FontStyles.Bold;
            skipTmp.color = C_GOLD;
            skipTmp.alignment = TextAlignmentOptions.Center;
            skipTmp.raycastTarget = false;

            // --- Highlight frame (accent-gold outline, transparent fill) ---
            var frameGO = GetOrCreate(rootGO.transform, "HighlightFrame",
                typeof(RectTransform), typeof(Image), typeof(Outline));
            var frameRt = frameGO.GetComponent<RectTransform>();
            frameRt.anchorMin = frameRt.anchorMax = new Vector2(0.5f, 0.5f);
            frameRt.pivot = new Vector2(0.5f, 0.5f);
            frameRt.anchoredPosition = Vector2.zero;
            frameRt.sizeDelta = new Vector2(920f, 120f);
            var frameImg = frameGO.GetComponent<Image>();
            frameImg.color = new Color(0f, 0f, 0f, 0f);  // transparent — no fill
            frameImg.raycastTarget = false;
            var frameOutline = frameGO.GetComponent<Outline>();
            frameOutline.effectColor = C_GOLD;
            frameOutline.effectDistance = new Vector2(3f, 3f);

            // --- Add TutorialOverlay component and wire SerializedFields ---
            overlay = rootGO.GetComponent<TutorialOverlay>();
            if (overlay == null)
                overlay = Undo.AddComponent<TutorialOverlay>(rootGO);

            var overlaySO = new SerializedObject(overlay);
            SetRef(overlaySO, "calloutText",    tmp);
            SetRef(overlaySO, "skipButton",     skipBtn);
            SetRef(overlaySO, "highlightFrame", frameRt);
            overlaySO.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(overlay);

            // --- Wire GameBootstrap.tutorialOverlay ---
            var bootstrapSO = new SerializedObject(bootstrap);
            SetRef(bootstrapSO, "tutorialOverlay", overlay);
            bootstrapSO.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(bootstrap);

            // --- Create / reuse SettingsScreen.replayTutorialButton ---
            Button replayBtn = BuildReplayTutorialButton(settings);

            var settingsSO = new SerializedObject(settings);
            SetRef(settingsSO, "replayTutorialButton", replayBtn);
            settingsSO.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(settings);

            // --- Persist ---
            EditorSceneManager.MarkSceneDirty(rootGO.scene);
            EditorSceneManager.SaveScene(rootGO.scene);

            Debug.Log(
                $"{LOG} TutorialOverlay built + wired. " +
                $"Root inactive={!rootGO.activeSelf}. " +
                $"ReplayTutorialButton={replayBtn.gameObject.name}. Scene saved.");
        }

        // -----------------------------------------------------------------------
        // Build or reuse a "REPLAY TUTORIAL" button under SettingsScreen,
        // positioned near the Reset Progress button.
        // -----------------------------------------------------------------------
        private static Button BuildReplayTutorialButton(SettingsScreen settings)
        {
            // Reuse if already created
            var existing = settings.transform.Find("ReplayTutorialButton");
            if (existing != null) return existing.GetComponent<Button>();

            var go = new GameObject("ReplayTutorialButton",
                typeof(RectTransform), typeof(Image), typeof(Button), typeof(Outline));
            go.transform.SetParent(settings.transform, false);
            Undo.RegisterCreatedObjectUndo(go, "Create ReplayTutorialButton");

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            // Placed 120px below Reset Progress button's typical position (-340)
            rt.anchoredPosition = new Vector2(0f, -460f);
            rt.sizeDelta = new Vector2(560f, 110f);

            var img = go.GetComponent<Image>();
            img.color = C_PANEL;
            img.raycastTarget = true;

            var outline = go.GetComponent<Outline>();
            outline.effectColor = C_GOLD;
            outline.effectDistance = new Vector2(2f, 2f);

            var btn = go.GetComponent<Button>();
            var cols = btn.colors;
            cols.normalColor      = C_PANEL;
            cols.highlightedColor = Hex("#242936");
            cols.pressedColor     = Hex("#242936");
            btn.colors = cols;

            // Label
            var labelGO = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelGO.transform.SetParent(go.transform, false);
            var lrt = labelGO.GetComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = Vector2.zero;
            lrt.offsetMax = Vector2.zero;
            var ltmp = labelGO.GetComponent<TextMeshProUGUI>();
            ltmp.text = "REPLAY TUTORIAL";
            ltmp.fontSize = 34f;
            ltmp.fontStyle = FontStyles.Bold;
            ltmp.color = C_TEXT;
            ltmp.alignment = TextAlignmentOptions.Center;
            ltmp.raycastTarget = false;

            return btn;
        }

        // -----------------------------------------------------------------------
        // Helpers
        // -----------------------------------------------------------------------

        private static T FindFirst<T>() where T : Object =>
            Object.FindAnyObjectByType<T>(FindObjectsInactive.Include);

        private static GameObject GetOrCreate(Transform parent, string childName, params System.Type[] types)
        {
            var existing = parent.Find(childName);
            if (existing != null) return existing.gameObject;

            var go = new GameObject(childName, types);
            go.transform.SetParent(parent, false);
            Undo.RegisterCreatedObjectUndo(go, $"Create {childName}");
            return go;
        }

        private static void SetRef(SerializedObject so, string propName, Object value)
        {
            var prop = so.FindProperty(propName);
            if (prop == null)
            {
                Debug.LogError($"{LOG} SerializedProperty '{propName}' not found on {so.targetObject.GetType().Name}");
                return;
            }
            prop.objectReferenceValue = value;
        }

        private static Color Hex(string h) { ColorUtility.TryParseHtmlString(h, out var c); return c; }
    }
}
