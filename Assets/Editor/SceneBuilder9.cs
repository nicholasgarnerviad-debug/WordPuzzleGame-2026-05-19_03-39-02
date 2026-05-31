using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using WordPuzzle.UI;

namespace WordPuzzleGame.EditorTools
{
    /// <summary>
    /// Adds the "Share Result" button + toast TMP under Canvas/ResultsScreen and
    /// wires the SerializedFields on ResultsScreen. Idempotent.
    /// </summary>
    public static class SceneBuilder9
    {
        private const string LOG = "[SceneBuilder9]";

        private static readonly Color C_PANEL  = Hex("#1B1F27");
        private static readonly Color C_PANEL_HI = Hex("#242936");
        private static readonly Color C_GOLD   = Hex("#C9B458");
        private static readonly Color C_TEXT   = Hex("#E7E1C4");

        [MenuItem("Tools/SceneBuilder9/Add Share button to Results")]
        public static void AddShare()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError($"{LOG} Cannot run during play mode."); return;
            }

            var canvas = GameObject.Find("Canvas");
            if (canvas == null) { Debug.LogError($"{LOG} Canvas not found."); return; }

            ResultsScreen results = null;
            foreach (var r in Object.FindObjectsByType<ResultsScreen>(
                FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                results = r; break;
            }
            if (results == null) { Debug.LogError($"{LOG} ResultsScreen not found."); return; }
            Transform parent = results.transform;

            // ---- Share button ----
            var existingBtn = parent.Find("ShareButton");
            GameObject btnGO = existingBtn != null ? existingBtn.gameObject : null;
            if (btnGO == null)
            {
                btnGO = new GameObject("ShareButton",
                    typeof(RectTransform), typeof(Image), typeof(Button), typeof(Outline));
                btnGO.transform.SetParent(parent, false);
                Undo.RegisterCreatedObjectUndo(btnGO, "Create ShareButton");
            }
            var brt = btnGO.GetComponent<RectTransform>();
            brt.anchorMin = brt.anchorMax = new Vector2(0.5f, 0.5f);
            brt.pivot = new Vector2(0.5f, 0.5f);
            brt.anchoredPosition = new Vector2(0f, -380f);
            brt.sizeDelta = new Vector2(560f, 110f);
            var bimg = btnGO.GetComponent<Image>();
            bimg.color = C_PANEL;
            bimg.raycastTarget = true;
            var bout = btnGO.GetComponent<Outline>();
            bout.effectColor = C_GOLD;
            bout.effectDistance = new Vector2(2f, 2f);
            var btn = btnGO.GetComponent<Button>();
            var cols = btn.colors;
            cols.normalColor = C_PANEL;
            cols.highlightedColor = C_PANEL_HI;
            cols.pressedColor = C_PANEL_HI;
            btn.colors = cols;

            var labelTrans = btnGO.transform.Find("Label");
            GameObject labelGO = labelTrans != null ? labelTrans.gameObject : null;
            if (labelGO == null)
            {
                labelGO = new GameObject("Label",
                    typeof(RectTransform), typeof(TextMeshProUGUI));
                labelGO.transform.SetParent(btnGO.transform, false);
                Undo.RegisterCreatedObjectUndo(labelGO, "Create ShareButton Label");
            }
            var lrt = labelGO.GetComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = Vector2.zero;
            lrt.offsetMax = Vector2.zero;
            var ltmp = labelGO.GetComponent<TextMeshProUGUI>();
            ltmp.text = "SHARE RESULT";
            ltmp.fontSize = 36f;
            ltmp.fontStyle = FontStyles.Bold;
            ltmp.color = C_TEXT;
            ltmp.alignment = TextAlignmentOptions.Center;
            ltmp.raycastTarget = false;

            // ---- Toast TMP ----
            var toastTrans = parent.Find("ToastText");
            GameObject toastGO = toastTrans != null ? toastTrans.gameObject : null;
            if (toastGO == null)
            {
                toastGO = new GameObject("ToastText",
                    typeof(RectTransform), typeof(TextMeshProUGUI));
                toastGO.transform.SetParent(parent, false);
                Undo.RegisterCreatedObjectUndo(toastGO, "Create ToastText");
            }
            var trt = toastGO.GetComponent<RectTransform>();
            trt.anchorMin = trt.anchorMax = new Vector2(0.5f, 0.5f);
            trt.pivot = new Vector2(0.5f, 0.5f);
            trt.anchoredPosition = new Vector2(0f, -270f);
            trt.sizeDelta = new Vector2(600f, 60f);
            var ttmp = toastGO.GetComponent<TextMeshProUGUI>();
            ttmp.text = "";
            ttmp.fontSize = 32f;
            ttmp.fontStyle = FontStyles.Italic | FontStyles.Bold;
            ttmp.color = C_GOLD;
            ttmp.alignment = TextAlignmentOptions.Center;
            ttmp.raycastTarget = false;
            toastGO.SetActive(false);

            // Wire SerializedFields.
            var so = new SerializedObject(results);
            var pBtn = so.FindProperty("shareButton");
            var pToast = so.FindProperty("toastText");
            if (pBtn == null || pToast == null)
            {
                Debug.LogError($"{LOG} ResultsScreen does not expose shareButton/toastText SerializedFields.");
            }
            else
            {
                pBtn.objectReferenceValue = btn;
                pToast.objectReferenceValue = ttmp;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(results);
            }

            EditorSceneManager.MarkSceneDirty(results.gameObject.scene);
            EditorSceneManager.SaveScene(results.gameObject.scene);
            Debug.Log($"{LOG} ShareButton + ToastText placed under ResultsScreen and wired. Scene saved.");
        }

        private static Color Hex(string h) { ColorUtility.TryParseHtmlString(h, out var c); return c; }
    }
}
