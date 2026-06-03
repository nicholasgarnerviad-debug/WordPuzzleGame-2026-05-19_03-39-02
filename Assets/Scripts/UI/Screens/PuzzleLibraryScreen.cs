using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using WordPuzzle.Modes;

namespace WordPuzzle.UI
{
    /// <summary>
    /// Puzzle Library (Task 15) — two-level Puzzle Show navigation.
    ///   Level 1 (Tier Select): 7 tier cards with theme, progress (X/50) and lock state.
    ///   Level 2 (Puzzle Grid):  the selected tier's 50 puzzle cards + a Back to tier-select.
    /// Card colour reflects real saved progress (completed = green + check, in-progress = gold,
    /// unlocked = surface grey, locked = padlock), resolved via PuzzleShowMode.ResolveState so
    /// it matches gameplay state exactly. Only the active tier's cards are rendered (perf).
    /// Tapping a puzzle fires OnPuzzleSelected(puzzleId) — the existing launch path, unchanged.
    /// </summary>
    public class PuzzleLibraryScreen : MonoBehaviour
    {
        [SerializeField] private Transform contentRoot;
        [SerializeField] private Button backButton;

        public event Action OnBackToMenu;
        public event Action<int> OnPuzzleSelected;

        // --- Design tokens (README §14) ---
        private static readonly Color C_LOCKED_BG       = HexC("#060709"); // Task 25 — near-black ghost centre
        private static readonly Color C_LOCKED_BORDER   = HexC("#454B59"); // Task 25 — dim but visible ring
        private static readonly Color C_LOCKED_TEXT     = HexC("#5A6270");

        private static readonly Color C_UNPLAYED_BG     = HexC("#060709"); // Task 25 — near-black ghost centre
        private static readonly Color C_UNPLAYED_BORDER = HexC("#6B7689"); // Task 25 — clearly visible ring
        private static readonly Color C_UNPLAYED_TEXT   = HexC("#E7E1C4");
        private static readonly Color C_UNPLAYED_ICON   = HexC("#7A828F");

        private static readonly Color C_INPROGRESS_BG     = HexC("#060709"); // Task 25 — near-black ghost centre
        private static readonly Color C_INPROGRESS_BORDER = HexC("#C9B458"); // gold
        private static readonly Color C_INPROGRESS_TEXT   = HexC("#F5F7FA");
        private static readonly Color C_INPROGRESS_ICON   = HexC("#C9B458");

        private static readonly Color C_COMPLETED_BG     = HexC("#0C140C"); // Task 25 — near-black, faint-green ghost centre
        private static readonly Color C_COMPLETED_BORDER = HexC("#6AAA64"); // green
        private static readonly Color C_COMPLETED_TEXT   = HexC("#F5F7FA");
        private static readonly Color C_COMPLETED_ICON   = HexC("#6AAA64");

        private static readonly Color C_HEADER_TIER     = HexC("#F5F7FA");
        private static readonly Color C_HEADER_TIER_LK  = HexC("#5A6270");
        private static readonly Color C_HEADER_COUNT    = HexC("#8A93A1");
        private static readonly Color C_SUBTITLE        = HexC("#8A93A1");
        private static readonly Color C_PUZZLE_ID       = HexC("#7A828F");
        private static readonly Color C_GOLD            = HexC("#C9B458"); // current/next tier accent

        // --- View state ---
        private enum ViewMode { TierSelect, PuzzleGrid }
        private ViewMode viewMode = ViewMode.TierSelect;
        private int selectedTierId = 1;

        // --- Injected progress (set by GameBootstrap before Show; read-only here) ---
        private readonly HashSet<int> completedIds = new HashSet<int>();
        private readonly HashSet<int> inProgressIds = new HashSet<int>();
        private int highestUnlockedTier = 1;

        private TierDefinitionsWrapper tierData;

        /// <summary>
        /// Task 15C — orchestrator injects the saved Puzzle Show progress before Show().
        /// Reads the existing PuzzleProgressData store (no new store invented).
        /// </summary>
        public void SetProgress(IEnumerable<int> completed, IEnumerable<int> inProgress, int highestUnlocked)
        {
            completedIds.Clear();
            inProgressIds.Clear();
            if (completed != null) foreach (var id in completed) completedIds.Add(id);
            if (inProgress != null) foreach (var id in inProgress) inProgressIds.Add(id);
            highestUnlockedTier = Mathf.Max(1, highestUnlocked);
        }

        private void OnEnable()
        {
            UIThemeManager.ApplyScreenBackground(gameObject); // Task 25 — true-black background
            if (backButton != null)
            {
                backButton.onClick.AddListener(HandleTopBack);
                UIThemeManager.ApplyOutlineButton(backButton.GetComponent<Image>(),
                    new Color32(0x8A, 0x93, 0xA1, 0xFF)); // Task 25 — ghost HOME pill
                // Preserve the existing HOME pill look (label styled in-code as before).
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

        // Top HOME pill always returns to the main menu (both levels).
        private void HandleTopBack() => OnBackToMenu?.Invoke();

        public void Show()
        {
            gameObject.SetActive(true);
            viewMode = ViewMode.TierSelect;   // always enter at the tier-select level
            tierData = LoadTierDefinitions();
            PopulateContent();
        }

        public void Hide() => gameObject.SetActive(false);

        // ================================================================
        //  Population — branches on the active view
        // ================================================================
        private void PopulateContent()
        {
            if (contentRoot == null) return;
            ClearContent();
            EnsureRootVerticalLayout();

            if (tierData == null || tierData.tiers == null) return;

            if (viewMode == ViewMode.TierSelect) PopulateTierSelect();
            else PopulatePuzzleGrid(selectedTierId);
        }

        // ---------------- Level 1: Tier Select ----------------
        private void PopulateTierSelect()
        {
            CreateScreenTitle("PUZZLE SHOW", "Pick a tier");

            foreach (var tier in tierData.tiers)
            {
                if (tier == null) continue;
                bool unlocked = tier.tierId <= highestUnlockedTier;
                bool isCurrent = tier.tierId == highestUnlockedTier;
                int total = tier.puzzles != null ? tier.puzzles.Length : 0;
                int completed = CountCompleted(tier);
                CreateTierSelectCard(tier, unlocked, isCurrent, completed, total);
            }
        }

        private void CreateTierSelectCard(TierData tier, bool unlocked, bool isCurrent, int completed, int total)
        {
            var go = new GameObject($"TierCard_{tier.tierId}", typeof(RectTransform));
            go.transform.SetParent(contentRoot, false);
            var le = go.AddComponent<LayoutElement>();
            le.minHeight = 104f; le.preferredHeight = 104f; le.flexibleWidth = 1f;

            bool tierComplete = unlocked && total > 0 && completed >= total;

            var border = go.AddComponent<Image>();
            ApplyRounded(border);
            border.color = tierComplete ? C_COMPLETED_BORDER
                         : !unlocked    ? C_LOCKED_BORDER
                         : isCurrent    ? C_GOLD
                                        : C_UNPLAYED_BORDER;

            if (unlocked)
            {
                var btn = go.AddComponent<Button>();
                btn.transition = Selectable.Transition.None;
                int captured = tier.tierId;
                btn.onClick.AddListener(() => OpenTier(captured));
            }

            var fill = MakeFill(go.transform, 6f); // Task 25 — wider ring for the ghost look
            ApplyRounded(fill.GetComponent<Image>());
            fill.GetComponent<Image>().color = tierComplete ? C_COMPLETED_BG
                                             : unlocked     ? C_UNPLAYED_BG
                                                            : C_LOCKED_BG; // all near-black centres now

            // Title row: "Tier N"  +  progress / lock on the right
            CreateAnchored(fill.transform, "TierName", $"Tier {tier.tierId}", 30,
                TextAlignmentOptions.TopLeft, unlocked ? C_HEADER_TIER : C_HEADER_TIER_LK,
                FontStyles.Bold, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(24f, -16f), new Vector2(300f, 38f));

            CreateAnchored(fill.transform, "TierTheme", TierTheme(tier.tierId), 18,
                TextAlignmentOptions.BottomLeft, C_SUBTITLE, FontStyles.Normal,
                new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(24f, 18f), new Vector2(360f, 26f));

            if (unlocked)
            {
                string progressText = tierComplete ? $"✓ {completed}/{total}" : $"{completed}/{total}";
                Color progressColor = tierComplete ? C_COMPLETED_ICON : isCurrent ? C_GOLD : C_HEADER_COUNT;
                CreateAnchored(fill.transform, "TierProgress", progressText, 24,
                    TextAlignmentOptions.Right, progressColor, FontStyles.Bold,
                    new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                    new Vector2(-24f, 0f), new Vector2(190f, 40f));
            }
            else
            {
                int need = PuzzleShowMode.PuzzlesRequiredToAdvance(tier.tierId - 1);
                CreateAnchored(fill.transform, "LockLabel", "□", 26,
                    TextAlignmentOptions.Right, C_LOCKED_TEXT, FontStyles.Bold,
                    new Vector2(1f, 1f), new Vector2(1f, 1f),
                    new Vector2(-24f, -16f), new Vector2(40f, 32f));
                CreateAnchored(fill.transform, "UnlockHint",
                    $"Clear {need} in Tier {tier.tierId - 1} to unlock", 18,
                    TextAlignmentOptions.BottomRight, C_SUBTITLE, FontStyles.Normal,
                    new Vector2(1f, 0f), new Vector2(1f, 0f),
                    new Vector2(-24f, 18f), new Vector2(360f, 28f));
            }
        }

        private void OpenTier(int tierId)
        {
            selectedTierId = tierId;
            viewMode = ViewMode.PuzzleGrid;
            PopulateContent();
        }

        // ---------------- Level 2: Puzzle Grid ----------------
        private void PopulatePuzzleGrid(int tierId)
        {
            TierData tier = null;
            foreach (var t in tierData.tiers)
                if (t != null && t.tierId == tierId) { tier = t; break; }
            if (tier == null) { viewMode = ViewMode.TierSelect; PopulateTierSelect(); return; }

            bool unlocked = tier.tierId <= highestUnlockedTier;
            int total = tier.puzzles != null ? tier.puzzles.Length : 0;
            int completed = CountCompleted(tier);

            CreateGridHeader(tier, completed, total);

            var grid = CreateTierGridContainer(tier.tierId);
            if (tier.puzzles == null) return;
            foreach (var puzzle in tier.puzzles)
            {
                if (puzzle == null) continue;
                var state = PuzzleShowMode.ResolveState(puzzle.puzzleId, unlocked, completedIds, inProgressIds);
                CreateLevelCard(grid.transform, puzzle, state);
            }
        }

        // Header for the grid view: a Back chip (→ tier select) + tier title/theme/progress.
        private void CreateGridHeader(TierData tier, int completed, int total)
        {
            var go = new GameObject("GridHeader", typeof(RectTransform));
            go.transform.SetParent(contentRoot, false);
            var le = go.AddComponent<LayoutElement>();
            le.minHeight = 84f; le.preferredHeight = 84f; le.flexibleWidth = 1f;

            // Back chip
            var backGo = new GameObject("BackToTiers", typeof(RectTransform));
            backGo.transform.SetParent(go.transform, false);
            var brt = (RectTransform)backGo.transform;
            brt.anchorMin = new Vector2(0f, 0.5f); brt.anchorMax = new Vector2(0f, 0.5f);
            brt.pivot = new Vector2(0f, 0.5f);
            brt.anchoredPosition = new Vector2(16f, 0f);
            brt.sizeDelta = new Vector2(96f, 52f);
            var backImg = backGo.AddComponent<Image>();
            UIThemeManager.ApplyOutlineButton(backImg, new Color32(0x8A, 0x93, 0xA1, 0xFF)); // Task 25 — ghost back chip
            var backBtn = backGo.AddComponent<Button>(); backBtn.transition = Selectable.Transition.None;
            backBtn.onClick.AddListener(BackToTierSelect);
            CreateText(backGo.transform, "BackLabel", "‹ Back", 20,
                TextAlignmentOptions.Center, C_UNPLAYED_TEXT, FontStyles.Bold);

            CreateAnchored(go.transform, "GridTitle", $"Tier {tier.tierId}", 28,
                TextAlignmentOptions.Top, C_HEADER_TIER, FontStyles.Bold,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -8f), new Vector2(360f, 34f));
            CreateAnchored(go.transform, "GridTheme", $"{TierTheme(tier.tierId)}   ·   {completed}/{total}", 16,
                TextAlignmentOptions.Bottom, C_SUBTITLE, FontStyles.Normal,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 12f), new Vector2(420f, 24f));
        }

        private void BackToTierSelect()
        {
            viewMode = ViewMode.TierSelect;
            PopulateContent();
        }

        private GameObject CreateTierGridContainer(int tierId)
        {
            var go = new GameObject($"TierGrid_{tierId}", typeof(RectTransform));
            go.transform.SetParent(contentRoot, false);

            var grid = go.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(210f, 150f);
            grid.spacing = new Vector2(16f, 16f);
            grid.padding = new RectOffset(24, 24, 16, 32);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 3;
            grid.childAlignment = TextAnchor.UpperCenter;

            var csf = go.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            return go;
        }

        // ---------------- Puzzle card ----------------
        private void CreateLevelCard(Transform parent, PuzzleDefinition puzzle, PuzzleState state)
        {
            var go = new GameObject($"Card_{puzzle.puzzleId}", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.sizeDelta = new Vector2(210f, 150f);

            var border = go.AddComponent<Image>();
            ApplyRounded(border);
            border.color = StateBorder(state);

            var btn = go.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;
            int capturedId = puzzle.puzzleId;
            btn.interactable = (state != PuzzleState.Locked);
            btn.onClick.AddListener(() => OnPuzzleSelected?.Invoke(capturedId));

            var fillGo = MakeFill(go.transform, state == PuzzleState.Locked ? 4f : 6f); // Task 25 — wider ghost ring
            var fillImg = fillGo.GetComponent<Image>();
            ApplyRounded(fillImg);
            fillImg.color = StateBg(state); // near-black centre; state read from the border ring

            // Row 1 — id + state icon (shape-coded, legible in grayscale / colorblind).
            CreateAnchored(fillGo.transform, "PuzzleId", $"#{puzzle.puzzleId:000}", 14,
                TextAlignmentOptions.TopLeft,
                state == PuzzleState.Locked ? C_LOCKED_TEXT : C_PUZZLE_ID,
                FontStyles.Bold | FontStyles.Italic,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(10f, -8f), new Vector2(60f, 18f));

            string icon = state switch
            {
                PuzzleState.Locked            => "□", // □ padlock-ish
                PuzzleState.UnlockedUnplayed  => "○", // ○ not started
                PuzzleState.InProgress        => "◑", // ◑ in progress
                PuzzleState.Completed         => "✓", // ✓ done (non-color cue)
                _                             => string.Empty
            };
            CreateAnchored(fillGo.transform, "StateIcon", icon, 18,
                TextAlignmentOptions.TopRight, StateIconColor(state), FontStyles.Bold,
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-10f, -8f), new Vector2(30f, 22f));

            // Row 2 — word pair.
            string pair = state == PuzzleState.Locked
                ? "??? → ???"
                : $"{(puzzle.startWord ?? "").ToUpper()} → {(puzzle.endWord ?? "").ToUpper()}";
            CreateAnchored(fillGo.transform, "WordPair", pair, 24,
                TextAlignmentOptions.Center, StateLetterColor(state), FontStyles.Bold,
                new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, 6f), Vector2.zero,
                fillSize: true, fillHeight: 52f);

            // Row 3 — steps subtitle.
            CreateAnchored(fillGo.transform, "Subtitle",
                state == PuzzleState.Locked ? "" : $"{puzzle.optimalSteps} steps", 14,
                TextAlignmentOptions.Center, C_SUBTITLE, FontStyles.Normal,
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 14f), Vector2.zero,
                fillSize: true, fillHeight: 22f);
        }

        // ================================================================
        //  Helpers
        // ================================================================
        private void CreateScreenTitle(string title, string subtitle)
        {
            var go = new GameObject("ScreenTitle", typeof(RectTransform));
            go.transform.SetParent(contentRoot, false);
            var le = go.AddComponent<LayoutElement>();
            le.minHeight = 70f; le.preferredHeight = 70f; le.flexibleWidth = 1f;
            CreateAnchored(go.transform, "Title", title, 26, TextAlignmentOptions.Top,
                C_GOLD, FontStyles.Bold, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -2f), new Vector2(420f, 34f));
            CreateAnchored(go.transform, "Sub", subtitle, 16, TextAlignmentOptions.Bottom,
                C_SUBTITLE, FontStyles.Normal, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 6f), new Vector2(420f, 24f));
        }

        private int CountCompleted(TierData tier)
        {
            if (tier?.puzzles == null) return 0;
            int n = 0;
            foreach (var p in tier.puzzles)
                if (p != null && completedIds.Contains(p.puzzleId)) n++;
            return n;
        }

        private static string TierTheme(int tierId) => tierId switch
        {
            1 => "3-letter words",
            2 => "4-letter words",
            3 => "5-letter words",
            4 => "5–6 letter words",
            5 => "6-letter words",
            6 => "6–7 letter words",
            7 => "7-letter words",
            _ => ""
        };

        private void ClearContent()
        {
            for (int i = contentRoot.childCount - 1; i >= 0; i--)
                Destroy(contentRoot.GetChild(i).gameObject);
        }

        private void EnsureRootVerticalLayout()
        {
            var rt = contentRoot as RectTransform;
            if (rt == null) return;
            var vlg = rt.GetComponent<VerticalLayoutGroup>();
            if (vlg == null) vlg = rt.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 12f;
            vlg.padding = new RectOffset(16, 16, 8, 32);
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

        // Inset child Image that fills its parent, leaving a `border` px ring.
        private static GameObject MakeFill(Transform parent, float border)
        {
            var fillGo = new GameObject("Fill", typeof(RectTransform));
            fillGo.transform.SetParent(parent, false);
            var rt = (RectTransform)fillGo.transform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(border, border);
            rt.offsetMax = new Vector2(-border, -border);
            var img = fillGo.AddComponent<Image>();
            img.raycastTarget = false;
            return fillGo;
        }

        // §15D — rounded corners via Unity's built-in sliced UISprite (matches the app's soft radius).
        // Task 22B — route the library's tier rows + puzzle cards (and their borders/back button)
        // through the one shared, generously-rounded button background so corners match every
        // other screen. Colour/raycast/children are untouched.
        private static void ApplyRounded(Image img)
        {
            UIThemeManager.ApplyRoundedButton(img);
        }

        private static Color StateBg(PuzzleState s) => s switch
        {
            PuzzleState.Locked => C_LOCKED_BG,
            PuzzleState.InProgress => C_INPROGRESS_BG,
            PuzzleState.Completed => C_COMPLETED_BG,
            _ => C_UNPLAYED_BG
        };
        private static Color StateBorder(PuzzleState s) => s switch
        {
            PuzzleState.Locked => C_LOCKED_BORDER,
            PuzzleState.InProgress => C_INPROGRESS_BORDER,
            PuzzleState.Completed => C_COMPLETED_BORDER,
            _ => C_UNPLAYED_BORDER
        };
        private static Color StateLetterColor(PuzzleState s) => s switch
        {
            PuzzleState.Locked => C_LOCKED_TEXT,
            PuzzleState.InProgress => C_INPROGRESS_TEXT,
            PuzzleState.Completed => C_COMPLETED_TEXT,
            _ => C_UNPLAYED_TEXT
        };
        private static Color StateIconColor(PuzzleState s) => s switch
        {
            PuzzleState.Locked => C_LOCKED_TEXT,
            PuzzleState.InProgress => C_INPROGRESS_ICON,
            PuzzleState.Completed => C_COMPLETED_ICON,
            _ => C_UNPLAYED_ICON
        };

        private static TextMeshProUGUI CreateText(Transform parent, string name,
            string text, float fontSize, TextAlignmentOptions align, Color color, FontStyles style)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text; tmp.fontSize = fontSize; tmp.color = color;
            tmp.alignment = align; tmp.fontStyle = style;
            tmp.raycastTarget = false; tmp.enableWordWrapping = false;
            return tmp;
        }

        private static TextMeshProUGUI CreateAnchored(Transform parent, string name,
            string text, float fontSize, TextAlignmentOptions align, Color color, FontStyles style,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 sizeDelta,
            bool fillSize = false, float fillHeight = 0f)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
            rt.pivot = new Vector2(
                Mathf.Approximately(anchorMin.x, anchorMax.x) ? anchorMin.x : 0.5f,
                Mathf.Approximately(anchorMin.y, anchorMax.y) ? anchorMin.y : 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = fillSize ? new Vector2(0f, fillHeight) : sizeDelta;
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text; tmp.fontSize = fontSize; tmp.color = color;
            tmp.alignment = align; tmp.fontStyle = style;
            tmp.raycastTarget = false; tmp.enableWordWrapping = false;
            return tmp;
        }

        private static Color HexC(string hex)
            => ColorUtility.TryParseHtmlString(hex, out var c) ? c : Color.magenta;

        // ================================================================
        //  JSON wrapper types (mirror GameBootstrap's tier types)
        // ================================================================
        [Serializable] private class TierDefinitionsWrapper { public TierData[] tiers; }
        [Serializable] private class TierData { public int tierId; public bool isUnlocked; public PuzzleDefinition[] puzzles; }
        [Serializable] private class PuzzleDefinition { public int puzzleId; public string startWord; public string endWord; public int optimalSteps; }
    }
}
