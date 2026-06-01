using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace WordPuzzle.UI
{
    /// <summary>
    /// Puzzle Library screen rewrite per UI Spec §2 — level cards grid.
    /// Renders tier headers ("Tier N — X/Y completed") and a 3-column grid of
    /// styled level cards (locked / unlocked-unplayed / in-progress / completed).
    /// Each card is a Button → OnPuzzleSelected(puzzleId).
    /// </summary>
    public class PuzzleLibraryScreen : MonoBehaviour
    {
        [SerializeField] private Transform contentRoot;
        [SerializeField] private Button backButton;

        public event Action OnBackToMenu;

        // §2.1 New event: fires when a level card is tapped.
        public event Action<int> OnPuzzleSelected;

        // --- §2.3 Palette ---
        private static readonly Color C_LOCKED_BG       = HexC("#1B1F27");
        private static readonly Color C_LOCKED_BORDER   = HexC("#2A2F3A");
        private static readonly Color C_LOCKED_TEXT     = HexC("#5A6270");

        private static readonly Color C_UNPLAYED_BG     = HexC("#252A33");
        private static readonly Color C_UNPLAYED_BORDER = HexC("#3A4150");
        private static readonly Color C_UNPLAYED_TEXT   = HexC("#E8EAF0");
        private static readonly Color C_UNPLAYED_ICON   = HexC("#7A828F");

        private static readonly Color C_INPROGRESS_BG     = HexC("#252A33");
        private static readonly Color C_INPROGRESS_BORDER = HexC("#C9B458");
        private static readonly Color C_INPROGRESS_TEXT   = HexC("#E8EAF0");
        private static readonly Color C_INPROGRESS_ICON   = HexC("#C9B458");

        private static readonly Color C_COMPLETED_BG     = HexC("#1F2A1F");
        private static readonly Color C_COMPLETED_BORDER = HexC("#6AAA64");
        private static readonly Color C_COMPLETED_TEXT   = HexC("#FFFFFF");
        private static readonly Color C_COMPLETED_ICON   = HexC("#6AAA64");

        private static readonly Color C_HEADER_BG       = HexC("#1B1F27");
        private static readonly Color C_HEADER_TIER     = HexC("#FFFFFF");
        private static readonly Color C_HEADER_TIER_LK  = HexC("#5A6270");
        // Task 8A: demoted from gold #C9B458 to text-muted #8A93A1. The tier count is
        // secondary info; gold is reserved for the in-progress current-item indicator.
        private static readonly Color C_HEADER_COUNT    = HexC("#8A93A1");
        private static readonly Color C_HEADER_COUNT_LK = HexC("#5A6270");

        private static readonly Color C_BADGE_OPTIMAL_BG = HexC("#2A2F3A");
        // Task 8A: optimal-steps badge FG demoted from gold #C9B458 to text-muted #8A93A1.
        // Badge is informational, not the focal element; in-progress border/icon keep gold.
        private static readonly Color C_BADGE_OPTIMAL_FG = HexC("#8A93A1");
        private static readonly Color C_PROGRESS_BG      = HexC("#2A2F3A");
        private static readonly Color C_SUBTITLE         = HexC("#8A93A1");
        private static readonly Color C_PUZZLE_ID        = HexC("#7A828F");

        // §2.3 Level state — local mirror of mode-coder's pending PuzzleState enum.
        // TODO: switch to WordPuzzle.Puzzle.PuzzleState once merged.
        private enum LocalPuzzleState
        {
            Locked,
            UnlockedUnplayed,
            InProgress,
            Completed
        }

        private void OnEnable()
        {
            if (backButton != null)
            {
                backButton.onClick.AddListener(() => OnBackToMenu?.Invoke());
                // §2 Back → Home conversion: keep serialized name, update visual label only.
                var label = backButton.GetComponentInChildren<TMP_Text>(true);
                if (label != null)
                {
                    label.text = "HOME";
                    label.fontStyle = FontStyles.Bold;
                    label.fontSize = 28f;
                    label.color = new Color32(0xE7, 0xE1, 0xC4, 0xFF);
                    label.alignment = TextAlignmentOptions.Center;
                }
            }
        }

        private void OnDisable()
        {
            if (backButton != null)
                backButton.onClick.RemoveAllListeners();
        }

        public void Show()
        {
            gameObject.SetActive(true);
            PopulateContent();
        }

        public void Hide() => gameObject.SetActive(false);

        // ================================================================
        //  Population
        // ================================================================
        private void PopulateContent()
        {
            if (contentRoot == null) return;

            ClearContent();
            EnsureRootVerticalLayout();

            var tierData = LoadTierDefinitions();
            if (tierData == null || tierData.tiers == null) return;

            foreach (var tier in tierData.tiers)
            {
                if (tier == null) continue;
                int total = tier.puzzles != null ? tier.puzzles.Length : 0;
                int completed = CountCompleted(tier);
                CreateTierHeader(tier, completed, total);

                var grid = CreateTierGridContainer(tier.tierId);
                if (tier.puzzles == null) continue;
                foreach (var puzzle in tier.puzzles)
                {
                    if (puzzle == null) continue;
                    var state = GetPuzzleState(puzzle.puzzleId, tier.isUnlocked);
                    CreateLevelCard(grid.transform, puzzle, state);
                }
            }
        }

        /// <summary>
        /// Stub for mode-coder's GetPuzzleState API. Until merged, assume all unlocked
        /// puzzles are UnlockedUnplayed and all locked tiers' puzzles are Locked.
        /// </summary>
        private LocalPuzzleState GetPuzzleState(int puzzleId, bool tierUnlocked)
        {
            if (!tierUnlocked) return LocalPuzzleState.Locked;
            return LocalPuzzleState.UnlockedUnplayed;
        }

        /// <summary>Count completed puzzles in tier (placeholder — returns 0 until persistence wires it).</summary>
        private int CountCompleted(TierData tier)
        {
            // TODO: pull from PlayerProgress.tierProgress[tier.tierId].completedPuzzles once available here.
            return 0;
        }

        private void ClearContent()
        {
            for (int i = contentRoot.childCount - 1; i >= 0; i--)
                Destroy(contentRoot.GetChild(i).gameObject);
        }

        // §2.6 contentRoot VerticalLayoutGroup config.
        private void EnsureRootVerticalLayout()
        {
            var rt = contentRoot as RectTransform;
            if (rt == null) return;
            var vlg = rt.GetComponent<VerticalLayoutGroup>();
            if (vlg == null) vlg = rt.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 8f;
            vlg.padding = new RectOffset(0, 0, 0, 32);
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            var csf = rt.GetComponent<ContentSizeFitter>();
            if (csf == null) csf = rt.gameObject.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        }

        private TierDefinitionsWrapper LoadTierDefinitions()
        {
            var asset = Resources.Load<TextAsset>("Data/tier_definitions");
            if (asset == null)
            {
                Debug.LogError("PuzzleLibraryScreen: tier_definitions.json not found");
                return null;
            }
            return JsonUtility.FromJson<TierDefinitionsWrapper>(asset.text);
        }

        // ================================================================
        //  §2.2 Tier header bar
        // ================================================================
        private void CreateTierHeader(TierData tier, int completed, int total)
        {
            var go = new GameObject($"TierHeader_{tier.tierId}", typeof(RectTransform));
            go.transform.SetParent(contentRoot, false);

            var rt = (RectTransform)go.transform;
            rt.sizeDelta = new Vector2(0f, 72f);

            var le = go.AddComponent<LayoutElement>();
            le.minHeight = 72f;
            le.preferredHeight = 72f;
            le.flexibleWidth = 1f;

            var img = go.AddComponent<Image>();
            img.color = C_HEADER_BG;

            // 24px padding L/R via HorizontalLayoutGroup
            var hlg = go.AddComponent<HorizontalLayoutGroup>();
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = true;
            hlg.padding = new RectOffset(24, 24, 0, 0);
            hlg.spacing = 12f;

            // Left: Tier {n}
            var leftText = CreateText(go.transform, "TierName",
                $"Tier {tier.tierId}", 30, TextAlignmentOptions.MidlineLeft,
                tier.isUnlocked ? C_HEADER_TIER : C_HEADER_TIER_LK,
                FontStyles.Bold);
            var leftLE = leftText.gameObject.AddComponent<LayoutElement>();
            leftLE.flexibleWidth = 1f;

            // Right: {completed}/{total}
            var rightText = CreateText(go.transform, "TierCount",
                $"{completed}/{total}", 22, TextAlignmentOptions.MidlineRight,
                tier.isUnlocked ? C_HEADER_COUNT : C_HEADER_COUNT_LK,
                FontStyles.Bold);
            var rightLE = rightText.gameObject.AddComponent<LayoutElement>();
            rightLE.minWidth = 80f;
            rightLE.preferredWidth = 100f;

            // Locked chip
            if (!tier.isUnlocked)
            {
                var chipGo = new GameObject("LockedChip", typeof(RectTransform));
                chipGo.transform.SetParent(go.transform, false);
                var chipImg = chipGo.AddComponent<Image>();
                chipImg.color = HexC("#2A2F3A");
                var chipLE = chipGo.AddComponent<LayoutElement>();
                chipLE.minWidth = 80f;
                chipLE.preferredWidth = 80f;
                chipLE.minHeight = 28f;
                chipLE.preferredHeight = 28f;

                CreateText(chipGo.transform, "ChipLabel", "LOCKED", 14,
                    TextAlignmentOptions.Center, C_LOCKED_TEXT, FontStyles.Bold);
            }
        }

        // §2.2 Grid container under header.
        private GameObject CreateTierGridContainer(int tierId)
        {
            var go = new GameObject($"TierGrid_{tierId}", typeof(RectTransform));
            go.transform.SetParent(contentRoot, false);

            var grid = go.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(210f, 160f);
            grid.spacing = new Vector2(16f, 16f);
            grid.padding = new RectOffset(24, 24, 16, 24);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 3;
            grid.childAlignment = TextAnchor.UpperCenter;

            var csf = go.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            return go;
        }

        // ================================================================
        //  §2.3/§2.4/§2.5 Level card
        // ================================================================
        private void CreateLevelCard(Transform parent, PuzzleDefinition puzzle, LocalPuzzleState state)
        {
            var go = new GameObject($"Card_{puzzle.puzzleId}", typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var rt = (RectTransform)go.transform;
            rt.sizeDelta = new Vector2(210f, 160f);

            // Border layer (full bleed)
            var borderImg = go.AddComponent<Image>();
            borderImg.color = StateBorder(state);

            // §2.5 Button covers full card
            var btn = go.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;
            int capturedId = puzzle.puzzleId;
            btn.interactable = (state != LocalPuzzleState.Locked);
            btn.onClick.AddListener(() => OnPuzzleSelected?.Invoke(capturedId));

            // Inner fill (inset 1 or 2px from border per state)
            var fillGo = new GameObject("Fill", typeof(RectTransform));
            fillGo.transform.SetParent(go.transform, false);
            var fillRt = (RectTransform)fillGo.transform;
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            float borderPx = (state == LocalPuzzleState.Locked || state == LocalPuzzleState.UnlockedUnplayed) ? 1f : 2f;
            fillRt.offsetMin = new Vector2(borderPx, borderPx);
            fillRt.offsetMax = new Vector2(-borderPx, -borderPx);
            var fillImg = fillGo.AddComponent<Image>();
            fillImg.color = StateBg(state);
            fillImg.raycastTarget = false;

            BuildRow1(fillGo.transform, puzzle, state);
            BuildRow2(fillGo.transform, puzzle, state);
            BuildRow3(fillGo.transform, puzzle);

            if (state == LocalPuzzleState.InProgress || state == LocalPuzzleState.Completed)
                BuildRow4ProgressBar(fillGo.transform, state);
        }

        private void BuildRow1(Transform fill, PuzzleDefinition puzzle, LocalPuzzleState state)
        {
            // PuzzleId label "#03" top-left at (+8,-8), 14pt BoldItalic, #7A828F.
            CreateAnchored(fill, "PuzzleId",
                $"#{puzzle.puzzleId:00}", 14,
                TextAlignmentOptions.TopLeft,
                state == LocalPuzzleState.Locked ? C_LOCKED_TEXT : C_PUZZLE_ID,
                FontStyles.Bold | FontStyles.Italic,
                new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(8f, -8f), new Vector2(48f, 18f));

            // Optimal badge pill next to id.
            var badgeGo = new GameObject("OptimalBadge", typeof(RectTransform));
            badgeGo.transform.SetParent(fill, false);
            var badgeRt = (RectTransform)badgeGo.transform;
            badgeRt.anchorMin = new Vector2(0f, 1f);
            badgeRt.anchorMax = new Vector2(0f, 1f);
            badgeRt.pivot = new Vector2(0f, 1f);
            badgeRt.anchoredPosition = new Vector2(54f, -8f);
            badgeRt.sizeDelta = new Vector2(36f, 20f);
            var badgeImg = badgeGo.AddComponent<Image>();
            badgeImg.color = state == LocalPuzzleState.Locked ? HexC("#1F2330") : C_BADGE_OPTIMAL_BG;
            badgeImg.raycastTarget = false;

            CreateText(badgeGo.transform, "BadgeText",
                puzzle.optimalSteps.ToString(), 12,
                TextAlignmentOptions.Center,
                state == LocalPuzzleState.Locked ? C_LOCKED_TEXT : C_BADGE_OPTIMAL_FG,
                FontStyles.Bold);

            // State icon top-right — shape-coded, legible in grayscale (Task 9E non-color cue).
            // Locked:           🔒-style  "[ ]"  padlock shape (bracket + gap)
            // UnlockedUnplayed: "○"        hollow circle — not started
            // InProgress:       "◑"        half-filled circle — in progress
            // Completed:        "✓"        checkmark — done
            string iconText = state switch
            {
                LocalPuzzleState.Locked          => "[ ]",
                LocalPuzzleState.UnlockedUnplayed => "○",
                LocalPuzzleState.InProgress       => "◑",
                LocalPuzzleState.Completed        => "✓",
                _                                 => string.Empty
            };
            int iconSize = 18;
            FontStyles iconStyle = FontStyles.Bold;
            float iconWidth = 30f;

            CreateAnchored(fill, "StateIcon",
                iconText, iconSize,
                TextAlignmentOptions.TopRight,
                StateIconColor(state), iconStyle,
                new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-8f, -6f),
                new Vector2(iconWidth, 22f));
        }

        private void BuildRow2(Transform fill, PuzzleDefinition puzzle, LocalPuzzleState state)
        {
            Color letterColor = StateLetterColor(state);
            string text = state == LocalPuzzleState.Locked
                ? "??? -> ???"
                : $"{(puzzle.startWord ?? string.Empty).ToUpper()} -> {(puzzle.endWord ?? string.Empty).ToUpper()}";

            CreateAnchored(fill, "WordPair",
                text, 28,
                TextAlignmentOptions.Center,
                letterColor, FontStyles.Bold,
                new Vector2(0f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(0f, 8f), Vector2.zero,
                fillSize: true, fillHeight: 60f);
        }

        private void BuildRow3(Transform fill, PuzzleDefinition puzzle)
        {
            CreateAnchored(fill, "Subtitle",
                $"{puzzle.optimalSteps} steps", 14,
                TextAlignmentOptions.Center,
                C_SUBTITLE, FontStyles.Normal,
                new Vector2(0f, 0f), new Vector2(1f, 0f),
                new Vector2(0f, 24f), Vector2.zero,
                fillSize: true, fillHeight: 22f);
        }

        // §2.4 Row 4 progress bar (height 6).
        private void BuildRow4ProgressBar(Transform fill, LocalPuzzleState state)
        {
            var bgGo = new GameObject("ProgressBg", typeof(RectTransform));
            bgGo.transform.SetParent(fill, false);
            var bgRt = (RectTransform)bgGo.transform;
            bgRt.anchorMin = new Vector2(0f, 0f);
            bgRt.anchorMax = new Vector2(1f, 0f);
            bgRt.pivot = new Vector2(0.5f, 0f);
            bgRt.anchoredPosition = new Vector2(0f, 8f);
            bgRt.sizeDelta = new Vector2(-16f, 6f);
            var bgImg = bgGo.AddComponent<Image>();
            bgImg.color = C_PROGRESS_BG;
            bgImg.raycastTarget = false;

            float fillFrac = state == LocalPuzzleState.Completed ? 1f : 0.5f;
            var fillGo = new GameObject("ProgressFill", typeof(RectTransform));
            fillGo.transform.SetParent(bgGo.transform, false);
            var fillRt = (RectTransform)fillGo.transform;
            fillRt.anchorMin = new Vector2(0f, 0f);
            fillRt.anchorMax = new Vector2(fillFrac, 1f);
            fillRt.offsetMin = Vector2.zero;
            fillRt.offsetMax = Vector2.zero;
            var fillImg = fillGo.AddComponent<Image>();
            fillImg.color = state == LocalPuzzleState.Completed ? C_COMPLETED_ICON : C_INPROGRESS_ICON;
            fillImg.raycastTarget = false;
        }

        // ================================================================
        //  State → palette helpers
        // ================================================================
        private static Color StateBg(LocalPuzzleState s)
        {
            switch (s)
            {
                case LocalPuzzleState.Locked: return C_LOCKED_BG;
                case LocalPuzzleState.UnlockedUnplayed: return C_UNPLAYED_BG;
                case LocalPuzzleState.InProgress: return C_INPROGRESS_BG;
                case LocalPuzzleState.Completed: return C_COMPLETED_BG;
                default: return C_UNPLAYED_BG;
            }
        }

        private static Color StateBorder(LocalPuzzleState s)
        {
            switch (s)
            {
                case LocalPuzzleState.Locked: return C_LOCKED_BORDER;
                case LocalPuzzleState.UnlockedUnplayed: return C_UNPLAYED_BORDER;
                case LocalPuzzleState.InProgress: return C_INPROGRESS_BORDER;
                case LocalPuzzleState.Completed: return C_COMPLETED_BORDER;
                default: return C_UNPLAYED_BORDER;
            }
        }

        private static Color StateLetterColor(LocalPuzzleState s)
        {
            switch (s)
            {
                case LocalPuzzleState.Locked: return C_LOCKED_TEXT;
                case LocalPuzzleState.UnlockedUnplayed: return C_UNPLAYED_TEXT;
                case LocalPuzzleState.InProgress: return C_INPROGRESS_TEXT;
                case LocalPuzzleState.Completed: return C_COMPLETED_TEXT;
                default: return C_UNPLAYED_TEXT;
            }
        }

        private static Color StateIconColor(LocalPuzzleState s)
        {
            switch (s)
            {
                case LocalPuzzleState.Locked: return C_LOCKED_TEXT;
                case LocalPuzzleState.UnlockedUnplayed: return C_UNPLAYED_ICON;
                case LocalPuzzleState.InProgress: return C_INPROGRESS_ICON;
                case LocalPuzzleState.Completed: return C_COMPLETED_ICON;
                default: return C_UNPLAYED_ICON;
            }
        }

        // ================================================================
        //  Text helpers
        // ================================================================
        private static TextMeshProUGUI CreateText(Transform parent, string name,
            string text, float fontSize, TextAlignmentOptions align,
            Color color, FontStyles style)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.alignment = align;
            tmp.fontStyle = style;
            tmp.raycastTarget = false;
            tmp.enableWordWrapping = false;
            return tmp;
        }

        private static TextMeshProUGUI CreateAnchored(Transform parent, string name,
            string text, float fontSize, TextAlignmentOptions align,
            Color color, FontStyles style,
            Vector2 anchorMin, Vector2 anchorMax,
            Vector2 anchoredPos, Vector2 sizeDelta,
            bool fillSize = false, float fillHeight = 0f)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = new Vector2(
                Mathf.Approximately(anchorMin.x, anchorMax.x) ? anchorMin.x : 0.5f,
                Mathf.Approximately(anchorMin.y, anchorMax.y) ? anchorMin.y : 0.5f);
            rt.anchoredPosition = anchoredPos;
            if (fillSize)
            {
                rt.sizeDelta = new Vector2(0f, fillHeight);
            }
            else
            {
                rt.sizeDelta = sizeDelta;
            }

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.alignment = align;
            tmp.fontStyle = style;
            tmp.raycastTarget = false;
            tmp.enableWordWrapping = false;
            return tmp;
        }

        private static Color HexC(string hex)
        {
            if (ColorUtility.TryParseHtmlString(hex, out var c)) return c;
            return Color.magenta;
        }

        // ================================================================
        //  JSON wrapper types (mirrors GameBootstrap private types)
        // ================================================================
        [Serializable]
        private class TierDefinitionsWrapper
        {
            public TierData[] tiers;
        }

        [Serializable]
        private class TierData
        {
            public int tierId;
            public bool isUnlocked;
            public PuzzleDefinition[] puzzles;
        }

        [Serializable]
        private class PuzzleDefinition
        {
            public int puzzleId;
            public string startWord;
            public string endWord;
            public int optimalSteps;
        }
    }
}
