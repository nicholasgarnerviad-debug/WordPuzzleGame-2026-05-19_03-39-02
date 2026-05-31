using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using WordPuzzle.UI;

namespace WordPuzzleGame.EditorTools
{
    /// <summary>
    /// scene-builder7 §1 + §2 + §3.1 + §3.2 scene mutations.
    /// Idempotent: re-running re-applies positions without duplicating GameObjects.
    /// </summary>
    public static class SceneBuilder7
    {
        private const string LOG_TAG = "[SceneBuilder7]";

        // ---------- Palette ----------
        private static readonly Color C_BG          = Hex("#0F1217");
        private static readonly Color C_PANEL       = Hex("#1B1F27");
        private static readonly Color C_PANEL_HI    = Hex("#242936");
        private static readonly Color C_GOLD        = Hex("#C9B458");
        private static readonly Color C_GREEN       = Hex("#6AAA64");
        private static readonly Color C_GREEN_HI    = Hex("#7CBA73");
        private static readonly Color C_GREEN_PR    = Hex("#5A9A54");
        private static readonly Color C_GREEN_DIS   = Hex("#3A5A38");
        private static readonly Color C_TEXT        = Hex("#E7E1C4");
        private static readonly Color C_TEXT_DIM    = Hex("#8A93A1");
        private static readonly Color C_TEXT_DARK   = Hex("#1A1B26");
        private static readonly Color C_WHITE_15A   = new Color(1f, 1f, 1f, 0.15f);

        // ============================================================
        //  MENU
        // ============================================================
        [MenuItem("Tools/SceneBuilder7/Run All (§1 + §2 + §3.1 + §3.2)")]
        public static void RunAll()
        {
            var sb = new StringBuilder();
            try
            {
                EnsureGameUIScene();

                var canvas = GameObject.Find("Canvas");
                if (canvas == null) { Debug.LogError($"{LOG_TAG} Canvas not found"); return; }

                Undo.SetCurrentGroupName("SceneBuilder7 mutations");
                int undoGroup = Undo.GetCurrentGroup();

                // §1 — TimeAttackSetupScreen
                var tas = BuildTimeAttackSetupScreen(canvas, sb);

                // Wire into UIManager
                WireUIManagerSlot(tas, sb);

                // §2 — Power-up row reposition + AddTime
                var gameplay = canvas.transform.Find("GameplayScreen");
                if (gameplay == null) { sb.AppendLine("  WARN: Canvas/GameplayScreen not found, skipping §2"); }
                else { BuildPowerupRow(gameplay.gameObject, sb); }

                // §3.1 — Settings value reposition
                var settings = canvas.transform.Find("SettingsScreen");
                if (settings == null) { sb.AppendLine("  WARN: Canvas/SettingsScreen not found, skipping §3.1"); }
                else { RepositionSettingsValues(settings.gameObject, sb); }

                // §3.2 — Results rebalance
                var results = canvas.transform.Find("ResultsScreen");
                if (results == null) { sb.AppendLine("  WARN: Canvas/ResultsScreen not found, skipping §3.2"); }
                else { RebalanceResultsScreen(results.gameObject, sb); }

                Undo.CollapseUndoOperations(undoGroup);

                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
                EditorSceneManager.SaveOpenScenes();

                sb.AppendLine();
                sb.AppendLine("SCENEBUILDER7 COMPLETE");
                Debug.Log($"{LOG_TAG}\n{sb}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"{LOG_TAG} EXCEPTION: {ex.Message}\n{ex.StackTrace}\nLog so far:\n{sb}");
            }
        }

        // ============================================================
        //  §1 TimeAttackSetupScreen
        // ============================================================
        private static GameObject BuildTimeAttackSetupScreen(GameObject canvas, StringBuilder log)
        {
            log.AppendLine("§1 TimeAttackSetupScreen:");

            // Find or create root under Canvas.
            var rootT = canvas.transform.Find("TimeAttackSetupScreen");
            GameObject root;
            if (rootT == null)
            {
                root = new GameObject("TimeAttackSetupScreen", typeof(RectTransform));
                Undo.RegisterCreatedObjectUndo(root, "Create TimeAttackSetupScreen");
                root.transform.SetParent(canvas.transform, false);
                log.AppendLine("  CREATED root");
            }
            else
            {
                root = rootT.gameObject;
                log.AppendLine("  FOUND existing root, refreshing");
            }

            // Root rect — full stretch.
            var rrt = (RectTransform)root.transform;
            rrt.anchorMin = Vector2.zero;
            rrt.anchorMax = Vector2.one;
            rrt.offsetMin = Vector2.zero;
            rrt.offsetMax = Vector2.zero;
            rrt.pivot = new Vector2(0.5f, 0.5f);

            // Background Image.
            var bg = EnsureComponent<Image>(root);
            bg.color = C_BG;
            bg.raycastTarget = true;

            // CanvasGroup.
            var cg = EnsureComponent<CanvasGroup>(root);
            cg.alpha = 1f;
            cg.interactable = true;
            cg.blocksRaycasts = true;

            // TimeAttackSetupScreen script.
            var tasComp = EnsureComponent<TimeAttackSetupScreen>(root);

            // Children.
            var homeButton = BuildHomeButton(root, log);
            var titleText  = BuildTitleText(root, log);
            BuildSubtitleText(root, log);
            var grid       = BuildButtonGrid(root, log);

            var btn60T   = BuildModeButton(grid, "Btn60Timed",     "60s",  "TIMED",    C_GOLD,  log);
            var btn60S   = BuildModeButton(grid, "Btn60Survival",  "60s",  "SURVIVAL", C_GREEN, log);
            var btn120T  = BuildModeButton(grid, "Btn120Timed",    "120s", "TIMED",    C_GOLD,  log);
            var btn120S  = BuildModeButton(grid, "Btn120Survival", "120s", "SURVIVAL", C_GREEN, log);

            // Wire SerializedFields on the script component.
            var so = new SerializedObject(tasComp);
            SetObjRef(so, "btn60Timed",     btn60T.GetComponent<Button>(),  log);
            SetObjRef(so, "btn60Survival",  btn60S.GetComponent<Button>(),  log);
            SetObjRef(so, "btn120Timed",    btn120T.GetComponent<Button>(), log);
            SetObjRef(so, "btn120Survival", btn120S.GetComponent<Button>(), log);
            SetObjRef(so, "backButton",     homeButton.GetComponent<Button>(), log);
            SetObjRef(so, "titleText",      titleText.GetComponent<TextMeshProUGUI>(), log);
            so.ApplyModifiedPropertiesWithoutUndo();

            // Edit-time SetActive(false) per spec.
            root.SetActive(false);
            log.AppendLine("  root SetActive(false)");

            return root;
        }

        private static GameObject BuildHomeButton(GameObject parent, StringBuilder log)
        {
            var go = EnsureChild(parent, "HomeButton");
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(-460f, 890f);
            rt.sizeDelta = new Vector2(120f, 80f);

            var img = EnsureComponent<Image>(go);
            img.color = C_PANEL;

            var btn = EnsureComponent<Button>(go);
            btn.transition = Selectable.Transition.ColorTint;
            btn.targetGraphic = img;
            var colors = btn.colors;
            colors.normalColor = C_PANEL;
            colors.highlightedColor = C_PANEL_HI;
            colors.pressedColor = C_PANEL_HI;
            colors.selectedColor = C_PANEL_HI;
            colors.disabledColor = new Color(C_PANEL.r, C_PANEL.g, C_PANEL.b, 0.5f);
            btn.colors = colors;

            // Label.
            var label = EnsureChild(go, "Label");
            var lrt = (RectTransform)label.transform;
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = Vector2.zero;
            lrt.offsetMax = Vector2.zero;

            var tmp = EnsureComponent<TextMeshProUGUI>(label);
            tmp.text = "HOME";
            tmp.fontSize = 28f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = C_TEXT;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;

            log.AppendLine("  HomeButton (-460,+890) 120x80");
            return go;
        }

        private static GameObject BuildTitleText(GameObject parent, StringBuilder log)
        {
            var go = EnsureChild(parent, "TitleText");
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0f, 780f);
            rt.sizeDelta = new Vector2(800f, 100f);

            var tmp = EnsureComponent<TextMeshProUGUI>(go);
            tmp.text = "TIME ATTACK";
            tmp.fontSize = 56f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = C_GOLD;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;

            log.AppendLine("  TitleText");
            return go;
        }

        private static GameObject BuildSubtitleText(GameObject parent, StringBuilder log)
        {
            var go = EnsureChild(parent, "SubtitleText");
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0f, 680f);
            rt.sizeDelta = new Vector2(800f, 60f);

            var tmp = EnsureComponent<TextMeshProUGUI>(go);
            tmp.text = "Choose your challenge";
            tmp.fontSize = 24f;
            tmp.fontStyle = FontStyles.Italic;
            tmp.color = C_TEXT_DIM;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;

            log.AppendLine("  SubtitleText");
            return go;
        }

        private static GameObject BuildButtonGrid(GameObject parent, StringBuilder log)
        {
            var go = EnsureChild(parent, "ButtonGrid");
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0f, 120f);
            rt.sizeDelta = new Vector2(720f, 720f);

            var glg = EnsureComponent<GridLayoutGroup>(go);
            glg.cellSize = new Vector2(340f, 340f);
            glg.spacing = new Vector2(32f, 32f);
            glg.padding = new RectOffset(0, 0, 0, 0);
            glg.startCorner = GridLayoutGroup.Corner.UpperLeft;
            glg.startAxis = GridLayoutGroup.Axis.Horizontal;
            glg.childAlignment = TextAnchor.UpperLeft;
            glg.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            glg.constraintCount = 2;

            log.AppendLine("  ButtonGrid 720x720 2col");
            return go;
        }

        private static GameObject BuildModeButton(
            GameObject parent, string name,
            string topText, string bottomText, Color outlineAndTopColor,
            StringBuilder log)
        {
            var go = EnsureChild(parent, name);
            // GridLayoutGroup drives size — RectTransform sizing handled by parent.

            var img = EnsureComponent<Image>(go);
            img.color = C_PANEL;

            // Outline (UI.Outline component).
            var outline = EnsureComponent<Outline>(go);
            outline.effectColor = outlineAndTopColor;
            outline.effectDistance = new Vector2(2f, 2f);
            outline.useGraphicAlpha = true;

            var btn = EnsureComponent<Button>(go);
            btn.transition = Selectable.Transition.ColorTint;
            btn.targetGraphic = img;
            var colors = btn.colors;
            colors.normalColor = C_PANEL;
            colors.highlightedColor = C_PANEL_HI;
            colors.pressedColor = C_PANEL_HI;
            colors.selectedColor = C_PANEL_HI;
            colors.disabledColor = new Color(C_PANEL.r, C_PANEL.g, C_PANEL.b, 0.5f);
            btn.colors = colors;

            // LabelTop.
            var lt = EnsureChild(go, "LabelTop");
            var ltrt = (RectTransform)lt.transform;
            ltrt.anchorMin = ltrt.anchorMax = new Vector2(0.5f, 0.5f);
            ltrt.pivot = new Vector2(0.5f, 0.5f);
            ltrt.anchoredPosition = new Vector2(0f, 60f);
            ltrt.sizeDelta = new Vector2(300f, 100f);

            var lttmp = EnsureComponent<TextMeshProUGUI>(lt);
            lttmp.text = topText;
            lttmp.fontSize = 64f;
            lttmp.fontStyle = FontStyles.Bold;
            lttmp.color = outlineAndTopColor;
            lttmp.alignment = TextAlignmentOptions.Center;
            lttmp.raycastTarget = false;

            // LabelBottom.
            var lb = EnsureChild(go, "LabelBottom");
            var lbrt = (RectTransform)lb.transform;
            lbrt.anchorMin = lbrt.anchorMax = new Vector2(0.5f, 0.5f);
            lbrt.pivot = new Vector2(0.5f, 0.5f);
            lbrt.anchoredPosition = new Vector2(0f, -60f);
            lbrt.sizeDelta = new Vector2(300f, 80f);

            var lbtmp = EnsureComponent<TextMeshProUGUI>(lb);
            lbtmp.text = bottomText;
            lbtmp.fontSize = 36f;
            lbtmp.fontStyle = FontStyles.Bold; // SemiBold isn't a TMP_FontStyle flag; Bold matches gold accent.
            lbtmp.color = C_TEXT;
            lbtmp.alignment = TextAlignmentOptions.Center;
            lbtmp.raycastTarget = false;

            log.AppendLine($"  {name} outline=#{ColorUtility.ToHtmlStringRGB(outlineAndTopColor)} '{topText}' / '{bottomText}'");
            return go;
        }

        // ============================================================
        //  UIManager wire
        // ============================================================
        private static void WireUIManagerSlot(GameObject tas, StringBuilder log)
        {
            log.AppendLine("UIManager wire:");
            var bootstrap = GameObject.Find("Bootstrap");
            if (bootstrap == null) { log.AppendLine("  WARN Bootstrap not found"); return; }

            var ui = bootstrap.GetComponent<UIManager>();
            if (ui == null) { log.AppendLine("  WARN UIManager missing on Bootstrap"); return; }

            var so = new SerializedObject(ui);
            SetObjRef(so, "timeAttackSetupScreen", tas.GetComponent<TimeAttackSetupScreen>(), log);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ============================================================
        //  §2 Power-up row + AddTime
        // ============================================================
        private static void BuildPowerupRow(GameObject gameplay, StringBuilder log)
        {
            log.AppendLine("§2 Power-up row:");

            // Reposition existing 3 buttons.
            Reposition(gameplay, "HintButton",   new Vector2(-450f, -310f), new Vector2(240f, 120f), log);
            Reposition(gameplay, "UndoButton",   new Vector2(-150f, -310f), new Vector2(240f, 120f), log);
            Reposition(gameplay, "RevealButton", new Vector2( 150f, -310f), new Vector2(240f, 120f), log);

            // AddTimeButton — create or update.
            var addBtn = EnsureChild(gameplay, "AddTimeButton");
            var rt = (RectTransform)addBtn.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(450f, -310f);
            rt.sizeDelta = new Vector2(240f, 120f);

            var img = EnsureComponent<Image>(addBtn);
            img.color = C_GREEN;

            var outline = EnsureComponent<Outline>(addBtn);
            outline.effectColor = C_WHITE_15A;
            outline.effectDistance = new Vector2(2f, 2f);

            var btn = EnsureComponent<Button>(addBtn);
            btn.transition = Selectable.Transition.ColorTint;
            btn.targetGraphic = img;
            var colors = btn.colors;
            colors.normalColor = C_GREEN;
            colors.highlightedColor = C_GREEN_HI;
            colors.pressedColor = C_GREEN_PR;
            colors.selectedColor = C_GREEN_HI;
            colors.disabledColor = C_GREEN_DIS;
            btn.colors = colors;

            // AddTimeButton label.
            var lbl = EnsureChild(addBtn, "Label");
            var lblRT = (RectTransform)lbl.transform;
            lblRT.anchorMin = Vector2.zero;
            lblRT.anchorMax = Vector2.one;
            lblRT.offsetMin = Vector2.zero;
            lblRT.offsetMax = Vector2.zero;

            var lblTmp = EnsureComponent<TextMeshProUGUI>(lbl);
            lblTmp.text = "+TIME";
            lblTmp.fontSize = 22f;
            lblTmp.fontStyle = FontStyles.Bold;
            lblTmp.color = C_TEXT_DARK;
            lblTmp.alignment = TextAlignmentOptions.Center;
            lblTmp.raycastTarget = false;

            log.AppendLine("  AddTimeButton (+450,-310) 240x120");

            // AddTimeCount TMP — sibling of AddTimeButton (NOT child; ReparentBadge moves it at runtime).
            var count = EnsureChild(gameplay, "AddTimeCount");
            var crt = (RectTransform)count.transform;
            crt.anchorMin = crt.anchorMax = new Vector2(0.5f, 0.5f);
            crt.pivot = new Vector2(0.5f, 0.5f);
            crt.anchoredPosition = new Vector2(0f, 0f);
            crt.sizeDelta = new Vector2(64f, 36f);

            var ctmp = EnsureComponent<TextMeshProUGUI>(count);
            ctmp.text = "1";
            ctmp.fontSize = 22f;
            ctmp.fontStyle = FontStyles.Bold;
            ctmp.color = C_TEXT;
            ctmp.alignment = TextAlignmentOptions.Center;
            ctmp.raycastTarget = false;

            log.AppendLine("  AddTimeCount default 64x36");

            // Wire SerializedFields on GameplayScreen.
            var gs = gameplay.GetComponent<GameplayScreen>();
            if (gs == null) { log.AppendLine("  WARN GameplayScreen component missing"); return; }
            var so = new SerializedObject(gs);
            SetObjRef(so, "addTimeButton", btn, log);
            SetObjRef(so, "addTimeCountText", ctmp, log);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ============================================================
        //  §3.1 Settings volume value reposition
        // ============================================================
        private static void RepositionSettingsValues(GameObject settings, StringBuilder log)
        {
            log.AppendLine("§3.1 Settings values:");

            // Map known spec positions onto either script field references (preferred)
            // or by name-match fallback. Use the script SerializedFields if wired.
            var s = settings.GetComponent<SettingsScreen>();
            TextMeshProUGUI master = null, music = null, sfx = null;
            if (s != null)
            {
                var so = new SerializedObject(s);
                master = so.FindProperty("masterVolumeValueLabel")?.objectReferenceValue as TextMeshProUGUI;
                music  = so.FindProperty("musicVolumeValueLabel")?.objectReferenceValue as TextMeshProUGUI;
                sfx    = so.FindProperty("sfxVolumeValueLabel")?.objectReferenceValue as TextMeshProUGUI;
            }

            // Fallback to name search if SerializedFields aren't wired.
            master = master != null ? master : FindTMPChild(settings, "MasterVolumeValue");
            music  = music  != null ? music  : FindTMPChild(settings, "MusicVolumeValue");
            sfx    = sfx    != null ? sfx    : FindTMPChild(settings, "SFXVolumeValue");

            StyleValueLabel(master, new Vector2(520f, 600f), "MasterVolumeValue", log);
            StyleValueLabel(sfx,    new Vector2(520f, 500f), "SFXVolumeValue",    log);
            StyleValueLabel(music,  new Vector2(520f, 400f), "MusicVolumeValue",  log);
        }

        private static void StyleValueLabel(TextMeshProUGUI label, Vector2 pos, string tag, StringBuilder log)
        {
            if (label == null) { log.AppendLine($"  {tag} NOT FOUND, skip"); return; }
            var rt = label.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(100f, 50f);
            label.alignment = TextAlignmentOptions.Center;
            label.fontSize = 24f;
            label.color = C_TEXT;
            log.AppendLine($"  {tag} → {pos.x},{pos.y}");
        }

        // ============================================================
        //  §3.2 Results rebalance
        // ============================================================
        private static void RebalanceResultsScreen(GameObject results, StringBuilder log)
        {
            log.AppendLine("§3.2 Results rebalance:");

            // ScoreLabel — find existing or create.
            var scoreLabel = EnsureChild(results, "ScoreLabel");
            var slrt = (RectTransform)scoreLabel.transform;
            slrt.anchorMin = slrt.anchorMax = new Vector2(0.5f, 0.5f);
            slrt.pivot = new Vector2(0.5f, 0.5f);
            slrt.anchoredPosition = new Vector2(0f, 500f);
            slrt.sizeDelta = new Vector2(600f, 80f);
            var sltmp = EnsureComponent<TextMeshProUGUI>(scoreLabel);
            sltmp.text = "FINAL SCORE";
            sltmp.fontSize = 32f;
            sltmp.fontStyle = FontStyles.Bold;
            sltmp.color = C_TEXT_DIM;
            sltmp.alignment = TextAlignmentOptions.Center;
            sltmp.raycastTarget = false;
            log.AppendLine("  ScoreLabel (0,+500) 600x80");

            // ScoreValue — prefer existing scoreText if wired.
            GameObject scoreValueGO = null;
            var rs = results.GetComponent<ResultsScreen>();
            if (rs != null)
            {
                var so = new SerializedObject(rs);
                var sv = so.FindProperty("scoreText")?.objectReferenceValue as TextMeshProUGUI;
                if (sv != null) scoreValueGO = sv.gameObject;
            }
            if (scoreValueGO == null)
            {
                var t = results.transform.Find("ScoreValue");
                if (t != null) scoreValueGO = t.gameObject;
            }
            if (scoreValueGO == null) scoreValueGO = EnsureChild(results, "ScoreValue");

            var svrt = (RectTransform)scoreValueGO.transform;
            svrt.anchorMin = svrt.anchorMax = new Vector2(0.5f, 0.5f);
            svrt.pivot = new Vector2(0.5f, 0.5f);
            svrt.anchoredPosition = new Vector2(0f, 400f);
            svrt.sizeDelta = new Vector2(600f, 100f);
            var svtmp = EnsureComponent<TextMeshProUGUI>(scoreValueGO);
            svtmp.fontSize = 64f;
            svtmp.fontStyle = FontStyles.Bold;
            svtmp.color = C_GOLD;
            svtmp.alignment = TextAlignmentOptions.Center;
            svtmp.raycastTarget = false;
            log.AppendLine("  ScoreValue (0,+400) 600x100");

            // StatsContainer.
            var stats = EnsureChild(results, "StatsContainer");
            var srt = (RectTransform)stats.transform;
            srt.anchorMin = srt.anchorMax = new Vector2(0.5f, 0.5f);
            srt.pivot = new Vector2(0.5f, 0.5f);
            srt.anchoredPosition = new Vector2(0f, 0f);
            srt.sizeDelta = new Vector2(800f, 400f);

            var vlg = EnsureComponent<VerticalLayoutGroup>(stats);
            vlg.spacing = 16f;
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            log.AppendLine("  StatsContainer (0,0) 800x400 VLG");

            // Build/refresh stat rows. Only build rows whose script field exists.
            BuildStatRow(stats, "WordsFoundRow", "WORDS FOUND", out var wfValue, log);
            BuildStatRow(stats, "TimeTakenRow",  "TIME",        out var ttValue, log);
            BuildStatRow(stats, "AccuracyRow",   "ACCURACY",    out var acValue, log);
            BuildStatRow(stats, "BestWordRow",   "BEST WORD",   out _,          log);

            // ButtonsContainer.
            var btns = EnsureChild(results, "ButtonsContainer");
            var brt = (RectTransform)btns.transform;
            brt.anchorMin = brt.anchorMax = new Vector2(0.5f, 0.5f);
            brt.pivot = new Vector2(0.5f, 0.5f);
            brt.anchoredPosition = new Vector2(0f, -550f);
            brt.sizeDelta = new Vector2(700f, 130f);

            var hlg = EnsureComponent<HorizontalLayoutGroup>(btns);
            hlg.spacing = 24f;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;
            log.AppendLine("  ButtonsContainer (0,-550) 700x130 HLG");

            // Existing buttons — reparent if not already under ButtonsContainer; size to spec.
            ManageResultsButton(results, btns, "PlayAgainButton", log);
            ManageResultsButton(results, btns, "HomeButton",     log, alsoLabel: "HOME");
        }

        private static void BuildStatRow(
            GameObject statsContainer, string rowName, string labelText,
            out TextMeshProUGUI valueTmp, StringBuilder log)
        {
            valueTmp = null;
            var row = EnsureChild(statsContainer, rowName);
            var rrt = (RectTransform)row.transform;
            rrt.sizeDelta = new Vector2(800f, 60f);

            var rhlg = EnsureComponent<HorizontalLayoutGroup>(row);
            rhlg.spacing = 20f;
            rhlg.childAlignment = TextAnchor.MiddleCenter;
            rhlg.childControlWidth = false;
            rhlg.childControlHeight = false;
            rhlg.childForceExpandWidth = false;
            rhlg.childForceExpandHeight = false;

            var le = EnsureComponent<LayoutElement>(row);
            le.minHeight = 60f;
            le.preferredHeight = 60f;

            // Label.
            var lbl = EnsureChild(row, "Label");
            var lblRT = (RectTransform)lbl.transform;
            lblRT.sizeDelta = new Vector2(300f, 60f);
            var lblTmp = EnsureComponent<TextMeshProUGUI>(lbl);
            lblTmp.text = labelText;
            lblTmp.fontSize = 24f;
            lblTmp.color = C_TEXT_DIM;
            lblTmp.alignment = TextAlignmentOptions.Left;
            lblTmp.raycastTarget = false;
            var lblLE = EnsureComponent<LayoutElement>(lbl);
            lblLE.preferredWidth = 300f;
            lblLE.preferredHeight = 60f;

            // Value.
            var val = EnsureChild(row, "Value");
            var valRT = (RectTransform)val.transform;
            valRT.sizeDelta = new Vector2(300f, 60f);
            var valTmp = EnsureComponent<TextMeshProUGUI>(val);
            if (string.IsNullOrEmpty(valTmp.text)) valTmp.text = "-";
            valTmp.fontSize = 32f;
            valTmp.fontStyle = FontStyles.Bold;
            valTmp.color = C_TEXT;
            valTmp.alignment = TextAlignmentOptions.Right;
            valTmp.raycastTarget = false;
            var valLE = EnsureComponent<LayoutElement>(val);
            valLE.preferredWidth = 300f;
            valLE.preferredHeight = 60f;

            valueTmp = valTmp;
            log.AppendLine($"  Stat row {rowName}");
        }

        private static void ManageResultsButton(
            GameObject results, GameObject container, string name,
            StringBuilder log, string alsoLabel = null)
        {
            // Find existing button anywhere under results; reparent to container if needed.
            Transform t = results.transform.Find(name);
            if (t == null)
            {
                // Search recursively.
                t = FindDeep(results.transform, name);
            }
            if (t == null)
            {
                log.AppendLine($"  WARN {name} not found, skip");
                return;
            }

            if (t.parent != container.transform)
            {
                Undo.SetTransformParent(t, container.transform, $"Reparent {name}");
                t.SetParent(container.transform, false);
            }

            var rt = (RectTransform)t;
            rt.sizeDelta = new Vector2(320f, 110f);
            var le = EnsureComponent<LayoutElement>(t.gameObject);
            le.preferredWidth = 320f;
            le.preferredHeight = 110f;
            le.minWidth = 320f;
            le.minHeight = 110f;

            if (alsoLabel != null)
            {
                var lbl = t.GetComponentInChildren<TextMeshProUGUI>(true);
                if (lbl != null)
                {
                    bool glyph = lbl.font != null && lbl.font.HasCharacter('⌂');
                    lbl.text = glyph ? "⌂ HOME" : "HOME";
                }
            }

            log.AppendLine($"  {name} → ButtonsContainer 320x110");
        }

        // ============================================================
        //  Helpers
        // ============================================================
        private static void EnsureGameUIScene()
        {
            var active = SceneManager.GetActiveScene();
            if (active.IsValid() && active.name == "GameUI") return;
            EditorSceneManager.OpenScene("Assets/Scenes/GameUI.unity", OpenSceneMode.Single);
        }

        private static T EnsureComponent<T>(GameObject go) where T : Component
        {
            var c = go.GetComponent<T>();
            if (c == null) c = Undo.AddComponent<T>(go);
            return c;
        }

        private static GameObject EnsureChild(GameObject parent, string name)
        {
            var t = parent.transform.Find(name);
            if (t != null) return t.gameObject;
            var go = new GameObject(name, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(go, $"Create {name}");
            go.transform.SetParent(parent.transform, false);
            return go;
        }

        private static void Reposition(GameObject parent, string childName, Vector2 pos, Vector2 size, StringBuilder log)
        {
            var t = parent.transform.Find(childName);
            if (t == null)
            {
                log.AppendLine($"  WARN {childName} not found under {parent.name}, skip reposition");
                return;
            }
            var rt = (RectTransform)t;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            log.AppendLine($"  Reposition {childName} → {pos.x},{pos.y} size {size.x}x{size.y}");
        }

        private static TextMeshProUGUI FindTMPChild(GameObject parent, string name)
        {
            var t = FindDeep(parent.transform, name);
            return t == null ? null : t.GetComponent<TextMeshProUGUI>();
        }

        private static Transform FindDeep(Transform root, string name)
        {
            if (root == null) return null;
            if (root.name == name) return root;
            for (int i = 0; i < root.childCount; i++)
            {
                var r = FindDeep(root.GetChild(i), name);
                if (r != null) return r;
            }
            return null;
        }

        private static void SetObjRef(SerializedObject so, string propName, Object value, StringBuilder log)
        {
            var p = so.FindProperty(propName);
            if (p == null) { log.AppendLine($"    WIRE FAIL {propName} (property missing)"); return; }
            p.objectReferenceValue = value;
            log.AppendLine($"    WIRE {propName} → {(value == null ? "<null>" : value.name)}");
        }

        private static Color Hex(string s)
        {
            if (ColorUtility.TryParseHtmlString(s, out var c)) return c;
            return Color.magenta;
        }
    }
}
