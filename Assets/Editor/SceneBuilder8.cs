using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using WordPuzzle.UI;

namespace WordPuzzleGame.EditorTools
{
    /// <summary>
    /// Adds the DAILY button + label under Canvas/MainMenuScreen and wires the two
    /// SerializedFields on the MainMenuScreen component. Idempotent.
    /// </summary>
    public static class SceneBuilder8
    {
        private const string LOG = "[SceneBuilder8]";

        private static readonly Color C_PANEL    = Hex("#1B1F27");
        private static readonly Color C_PANEL_HI = Hex("#242936");
        private static readonly Color C_GREEN    = Hex("#6AAA64");
        private static readonly Color C_TEXT     = Hex("#E7E1C4");

        [MenuItem("Tools/SceneBuilder8/Add DAILY button to MainMenu")]
        public static void AddDailyButton()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError($"{LOG} Cannot run during play mode.");
                return;
            }

            var canvas = GameObject.Find("Canvas");
            if (canvas == null) { Debug.LogError($"{LOG} Canvas not found."); return; }

            // Find MainMenuScreen even if inactive.
            MainMenuScreen menu = null;
            foreach (var m in Object.FindObjectsByType<MainMenuScreen>(
                FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                menu = m; break;
            }
            if (menu == null) { Debug.LogError($"{LOG} MainMenuScreen not found in scene."); return; }

            // Locate or create DailyButton under MainMenuScreen.
            Transform parent = menu.transform;
            var existing = parent.Find("DailyButton");
            GameObject btnGO = existing != null ? existing.gameObject : null;

            if (btnGO == null)
            {
                btnGO = new GameObject("DailyButton",
                    typeof(RectTransform), typeof(Image), typeof(Button), typeof(Outline));
                btnGO.transform.SetParent(parent, false);
                Undo.RegisterCreatedObjectUndo(btnGO, "Create DailyButton");
            }

            var rt = btnGO.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0f, 460f);  // headline slot above other modes
            rt.sizeDelta = new Vector2(640f, 130f);

            var img = btnGO.GetComponent<Image>();
            img.color = C_PANEL;
            img.raycastTarget = true;

            var outline = btnGO.GetComponent<Outline>();
            outline.effectColor = C_GREEN;
            outline.effectDistance = new Vector2(3f, 3f);

            var btn = btnGO.GetComponent<Button>();
            var colors = btn.colors;
            colors.normalColor = C_PANEL;
            colors.highlightedColor = C_PANEL_HI;
            colors.pressedColor = C_PANEL_HI;
            btn.colors = colors;
            btn.transition = Selectable.Transition.ColorTint;

            // Label child.
            var labelTrans = btnGO.transform.Find("Label");
            GameObject labelGO = labelTrans != null ? labelTrans.gameObject : null;
            if (labelGO == null)
            {
                labelGO = new GameObject("Label",
                    typeof(RectTransform), typeof(TextMeshProUGUI));
                labelGO.transform.SetParent(btnGO.transform, false);
                Undo.RegisterCreatedObjectUndo(labelGO, "Create DailyButton Label");
            }
            var lrt = labelGO.GetComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = Vector2.zero;
            lrt.offsetMax = Vector2.zero;

            var tmp = labelGO.GetComponent<TextMeshProUGUI>();
            tmp.text = "DAILY";
            tmp.fontSize = 44f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = C_TEXT;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;

            // Wire SerializedFields on MainMenuScreen.
            var so = new SerializedObject(menu);
            var pBtn = so.FindProperty("dailyButton");
            var pLbl = so.FindProperty("dailyButtonLabel");
            if (pBtn == null || pLbl == null)
            {
                Debug.LogError($"{LOG} MainMenuScreen does not expose dailyButton/dailyButtonLabel SerializedFields.");
            }
            else
            {
                pBtn.objectReferenceValue = btn;
                pLbl.objectReferenceValue = tmp;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(menu);
            }

            EditorSceneManager.MarkSceneDirty(menu.gameObject.scene);
            EditorSceneManager.SaveScene(menu.gameObject.scene);
            Debug.Log($"{LOG} DAILY button placed at (0, +460) under MainMenuScreen and wired. Scene saved.");
        }

        private static Color Hex(string h)
        {
            ColorUtility.TryParseHtmlString(h, out var c);
            return c;
        }
    }
}
