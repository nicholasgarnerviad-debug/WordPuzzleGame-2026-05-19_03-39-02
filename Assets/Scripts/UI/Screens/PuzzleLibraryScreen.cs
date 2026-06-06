using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using WordPuzzle.Modes;
using WordPuzzle.Persistence;

namespace WordPuzzle.UI
{
    /// <summary>
    /// Puzzle Library (Task 15; tiers expanded to 100/tier) — two-level Puzzle Show navigation.
    ///   Level 1 (Tier Select): 7 tier cards with theme, progress (X/100) and lock state.
    ///   Level 2 (Puzzle Grid):  the selected tier's 100 puzzle cards + a Back to tier-select.
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

        // --- Design tokens — Direction B: forward to the canonical Palette (no raw hex) ---
        private static readonly Color C_LOCKED_BG       = Palette.SurfaceVoid;     // near-black ghost centre
        private static readonly Color C_LOCKED_BORDER   = Palette.Amethyst;        // dim but visible ring
        private static readonly Color C_LOCKED_TEXT     = Palette.TextMuted;

        private static readonly Color C_UNPLAYED_BG     = Palette.SurfaceVoid;     // near-black ghost centre
        private static readonly Color C_UNPLAYED_BORDER = Palette.AccentPeriwinkle; // clearly visible ring
        private static readonly Color C_UNPLAYED_TEXT   = Palette.TextPrimary;
        private static readonly Color C_UNPLAYED_ICON   = Palette.TextMuted;

        private static readonly Color C_INPROGRESS_BG     = Palette.SurfaceVoid;   // near-black ghost centre
        private static readonly Color C_INPROGRESS_BORDER = Palette.ModeDaily;     // current tier — hero accent
        private static readonly Color C_INPROGRESS_TEXT   = Palette.TextPrimary;
        private static readonly Color C_INPROGRESS_ICON   = Palette.ModeDaily;

        private static readonly Color C_COMPLETED_BG     = Palette.SurfaceVoid;    // near-black ghost centre
        private static readonly Color C_COMPLETED_BORDER = Palette.AccentAqua;     // completed — aqua (retired green)
        private static readonly Color C_COMPLETED_TEXT   = Palette.TextPrimary;
        private static readonly Color C_COMPLETED_ICON   = Palette.AccentAqua;

        private static readonly Color C_HEADER_TIER     = Palette.TextPrimary;
        private static readonly Color C_HEADER_TIER_LK  = Palette.TextMuted;
        private static readonly Color C_HEADER_COUNT    = Palette.TextMuted;
        private static readonly Color C_SUBTITLE        = Palette.TextMuted;
        private static readonly Color C_PUZZLE_ID       = Palette.TextMuted;
        private static readonly Color C_GOLD            = Palette.ModeDaily;       // current/next tier accent

        // --- View state ---
        private enum ViewMode { TierSelect, PuzzleGrid }
        private ViewMode viewMode = ViewMode.TierSelect;
        private int selectedTierId = 1;

        // --- Injected progress (set by GameBootstrap before Show; read-only here) ---
        private readonly HashSet<int> completedIds = new HashSet<int>();
        private readonly HashSet<int> inProgressIds = new HashSet<int>();
        private int highestUnlockedTier = 1;

        // Library Path View — per-puzzle best-solve + revealed-optimal records, keyed by puzzleId.
        private readonly Dictionary<int, PuzzlePathRecord> pathRecords = new Dictionary<int, PuzzlePathRecord>();

        private TierDefinitionsWrapper tierData;

        // --- Path View detail overlay (built lazily) ---
        private GameObject detailOverlay;

        // Path View palette (on-brand: black + outline, no gold accents on the path itself).
        private static readonly Color C_DETAIL_SCRIM   = new Color(0f, 0f, 0f, 0.82f);
        private static readonly Color C_PANEL_BG       = Palette.SurfaceVoid;
        private static readonly Color C_PANEL_BORDER   = Palette.AccentPeriwinkle;
        private static readonly Color C_SLOT_BG        = Palette.Surface;
        private static readonly Color C_SLOT_BORDER    = Palette.Amethyst;     // blank slot ring
        private static readonly Color C_SLOT_REVEALED  = Palette.AccentAqua;   // matched optimal word — aqua (retired green)
        private static readonly Color C_SLOT_BLANK_TXT = Palette.TextMuted;
        private static readonly Color C_BEST_WORD      = Palette.TextPrimary;
        private static readonly Color C_PERFECT_TXT    = Palette.AccentAqua;

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

        /// <summary>
        /// Library Path View — orchestrator injects the per-puzzle path records (best solve +
        /// revealed-optimal slots) before Show(). Keyed by puzzleId for O(1) lookup when a beaten
        /// puzzle is tapped. Null/empty (old saves) is fine — those puzzles just show no path data.
        /// </summary>
        public void SetPathRecords(IEnumerable<PuzzlePathRecord> records)
        {
            pathRecords.Clear();
            if (records == null) return;
            foreach (var r in records)
                if (r != null) pathRecords[r.puzzleId] = r;
        }

        private void OnEnable()
        {
            UIThemeManager.ApplyScreenBackground(gameObject, UIThemeManager.ReadabilityScrimAlpha); // Task 25 backdrop + readability scrim
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
            UIAnimations.PlayScreenEntrance(this); // modern feel — gentle fade-in on open (ReduceMotion-gated)
        }

        public void Hide()
        {
            ClosePathDetail();
            gameObject.SetActive(false);
        }

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

            int index = 0;
            foreach (var tier in tierData.tiers)
            {
                if (tier == null) continue;
                bool unlocked = tier.tierId <= highestUnlockedTier;
                bool isCurrent = tier.tierId == highestUnlockedTier;
                int total = tier.puzzles != null ? tier.puzzles.Length : 0;
                int completed = CountCompleted(tier);
                CreateTierSelectCard(tier, unlocked, isCurrent, completed, total, index++);
            }
        }

        private void CreateTierSelectCard(TierData tier, bool unlocked, bool isCurrent, int completed, int total, int animIndex = 0)
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
                var capturedRt = (RectTransform)go.transform;
                btn.onClick.AddListener(() =>
                {
                    if (!UIAnimations.ReduceMotion && isActiveAndEnabled)
                        StartCoroutine(UIAnimations.ScaleButtonTap(capturedRt));
                    OpenTier(captured);
                });
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
                // Polish — was C_SUBTITLE @18 (flagged low-contrast/tiny); brighter + larger for legibility.
                CreateAnchored(fill.transform, "UnlockHint",
                    $"Clear {need} in Tier {tier.tierId - 1} to unlock", 19,
                    TextAlignmentOptions.BottomRight, C_HEADER_COUNT, FontStyles.Normal,
                    new Vector2(1f, 0f), new Vector2(1f, 0f),
                    new Vector2(-24f, 18f), new Vector2(360f, 28f));
            }

            // Modern feel — staggered cascade reveal (slide-up + fade), ReduceMotion-gated.
            PlayCardCascade(go.transform, animIndex);
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
            int cardIndex = 0;
            foreach (var puzzle in tier.puzzles)
            {
                if (puzzle == null) continue;
                var state = PuzzleShowMode.ResolveState(puzzle.puzzleId, unlocked, completedIds, inProgressIds);
                CreateLevelCard(grid.transform, puzzle, state, cardIndex++);
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
            backBtn.onClick.AddListener(() =>
            {
                if (!UIAnimations.ReduceMotion && isActiveAndEnabled)
                    StartCoroutine(UIAnimations.ScaleButtonTap(brt));
                BackToTierSelect();
            });
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
        private void CreateLevelCard(Transform parent, PuzzleDefinition puzzle, PuzzleState state, int animIndex = 0)
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
            var capturedPuzzle = puzzle;
            btn.interactable = (state != PuzzleState.Locked);
            var cardRt = (RectTransform)go.transform;
            // Library Path View — a BEATEN puzzle opens the detail panel (best solve + partial optimal),
            // gated by completion (no spoilers before beating). Unbeaten-unlocked launches as before.
            // Polish — a subtle press-squish on tap (ReduceMotion-gated) precedes the action for tactile feel.
            if (state == PuzzleState.Completed)
                btn.onClick.AddListener(() =>
                {
                    if (!UIAnimations.ReduceMotion && isActiveAndEnabled)
                        StartCoroutine(UIAnimations.ScaleButtonTap(cardRt));
                    ShowPathDetail(capturedPuzzle);
                });
            else
                btn.onClick.AddListener(() =>
                {
                    if (!UIAnimations.ReduceMotion && isActiveAndEnabled)
                        StartCoroutine(UIAnimations.ScaleButtonTap(cardRt));
                    OnPuzzleSelected?.Invoke(capturedId);
                });

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

            // Modern feel — staggered cascade reveal (slide-up + fade), ReduceMotion-gated.
            // Wrapped by column so the 3-wide grid ripples diagonally rather than row-by-row.
            PlayCardCascade(go.transform, animIndex % 12);
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
        //  Library Path View — detail overlay (best solve + partial optimal)
        // ================================================================

        // Build (or rebuild) and show the full-screen detail overlay for a BEATEN puzzle:
        //   (A) the player's BEST solve path, and
        //   (B) the canonical optimal path as word-slots — matched words revealed, the rest blank.
        // On-brand (black + outline, no gold on the path), Safe-Area inset, Large-Text safe (wraps),
        // ReduceMotion-gated reveal (blanks animate a subtle fade/scale-in, or snap when ReduceMotion).
        private void ShowPathDetail(PuzzleDefinition puzzle)
        {
            if (puzzle == null) return;
            pathRecords.TryGetValue(puzzle.puzzleId, out var record);

            if (detailOverlay != null) Destroy(detailOverlay);

            // Full-screen scrim (also a tap-to-close target).
            detailOverlay = new GameObject($"PathDetail_{puzzle.puzzleId}", typeof(RectTransform));
            detailOverlay.transform.SetParent(transform, false);
            var ort = (RectTransform)detailOverlay.transform;
            ort.anchorMin = Vector2.zero; ort.anchorMax = Vector2.one;
            ort.offsetMin = Vector2.zero; ort.offsetMax = Vector2.zero;
            var scrim = detailOverlay.AddComponent<Image>();
            scrim.color = C_DETAIL_SCRIM;
            var scrimBtn = detailOverlay.AddComponent<Button>();
            scrimBtn.transition = Selectable.Transition.None;
            scrimBtn.onClick.AddListener(ClosePathDetail);

            // Compact, content-sized panel: stretches horizontally (Safe-Area inset) but its HEIGHT
            // is driven by ContentSizeFitter so it hugs the content and sits centred — no dead space.
            var safe = GetSafeAreaInsets();
            var panelGo = new GameObject("Panel", typeof(RectTransform));
            panelGo.transform.SetParent(detailOverlay.transform, false);
            var prt = (RectTransform)panelGo.transform;
            prt.anchorMin = new Vector2(0f, 0.5f); prt.anchorMax = new Vector2(1f, 0.5f);
            prt.pivot = new Vector2(0.5f, 0.5f);
            prt.anchoredPosition = Vector2.zero;
            prt.offsetMin = new Vector2(24f + safe.x, 0f);
            prt.offsetMax = new Vector2(-24f - safe.z, 0f);
            var panelImg = panelGo.AddComponent<Image>();
            UIThemeManager.ApplyRoundedButton(panelImg);
            panelImg.color = C_PANEL_BORDER;
            // Eat taps so clicking the panel doesn't close it (only the scrim does).
            panelGo.AddComponent<Button>().transition = Selectable.Transition.None;
            // Outer layout = the 5px ring; ContentSizeFitter sizes the panel to its single child.
            var panelVlg = panelGo.AddComponent<VerticalLayoutGroup>();
            panelVlg.padding = new RectOffset(5, 5, 5, 5);
            panelVlg.childControlWidth = true; panelVlg.childControlHeight = true;
            panelVlg.childForceExpandWidth = true; panelVlg.childForceExpandHeight = false;
            var panelCsf = panelGo.AddComponent<ContentSizeFitter>();
            panelCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            panelCsf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            // Inner fill card holds the actual content stack (also content-sized).
            var content = new GameObject("DetailContent", typeof(RectTransform));
            content.transform.SetParent(panelGo.transform, false);
            var panelFillImg = content.AddComponent<Image>();
            UIThemeManager.ApplyRoundedButton(panelFillImg);
            panelFillImg.color = C_PANEL_BG;
            panelFillImg.raycastTarget = false;
            var vlg = content.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(20, 20, 18, 18);
            vlg.spacing = 9f;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true; vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;
            var contentCsf = content.AddComponent<ContentSizeFitter>();
            contentCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            contentCsf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            // Title: the word pair.
            string pair = $"{(puzzle.startWord ?? "").ToUpper()}  →  {(puzzle.endWord ?? "").ToUpper()}";
            AddDetailLabel(content.transform, pair, 28, C_HEADER_TIER, FontStyles.Bold, 38f);
            AddDetailLabel(content.transform, $"#{puzzle.puzzleId:000} · {puzzle.optimalSteps} steps optimal",
                15, C_SUBTITLE, FontStyles.Normal, 20f);

            // (A) Best solve.
            AddDetailLabel(content.transform, "YOUR BEST SOLVE", 15, C_GOLD, FontStyles.Bold, 20f);
            string[] best = record != null ? record.bestSolvePath : null;
            bool isPerfect = best != null && best.Length > 0 && (best.Length - 1) == puzzle.optimalSteps;
            if (best != null && best.Length > 0)
            {
                AddWordRow(content.transform, best, _ => C_BEST_WORD, null, animate: false);
                int steps = best.Length - 1;
                string sub = isPerfect ? "Perfect — optimal length!" : $"{steps} steps";
                AddDetailLabel(content.transform, sub, 15,
                    isPerfect ? C_PERFECT_TXT : C_SUBTITLE, FontStyles.Italic, 20f);
            }
            else
            {
                // Graceful degrade: beaten on an OLD save (pre-Path-View) with no stored best.
                AddDetailLabel(content.transform, "Replay to record your best route.",
                    15, C_SUBTITLE, FontStyles.Italic, 20f);
            }

            // (B) Optimal path — revealed slots vs blanks.
            AddDetailLabel(content.transform, "OPTIMAL PATH", 15, C_GOLD, FontStyles.Bold, 20f);
            string[] solution = puzzle.solution ?? Array.Empty<string>();
            var revealed = new HashSet<int>();
            if (record != null && record.revealedOptimalIndices != null)
                foreach (var i in record.revealedOptimalIndices) revealed.Add(i);
            AddWordRow(content.transform, solution,
                i => revealed.Contains(i) ? C_SLOT_REVEALED : C_SLOT_BLANK_TXT,
                i => revealed.Contains(i),
                animate: true);
            int revealedCount = 0;
            for (int i = 0; i < solution.Length; i++) if (revealed.Contains(i)) revealedCount++;
            AddDetailLabel(content.transform, $"{revealedCount}/{solution.Length} revealed",
                15, C_SUBTITLE, FontStyles.Normal, 20f);

            // Replay button (reuses the existing launch path — no spoiler concern; already beaten).
            AddReplayButton(content.transform, puzzle.puzzleId);

            // Entrance: gentle fade-in (ReduceMotion → instant inside PlayScreenEntrance).
            UIAnimations.PlayScreenEntrance(this);
        }

        private void ClosePathDetail()
        {
            if (detailOverlay != null) { Destroy(detailOverlay); detailOverlay = null; }
        }

        // A single row of word "slots". Each slot is a bordered box; revealed/known words show text,
        // blanks show "_ _ _" sized to the word length. ReduceMotion-gated subtle reveal animation.
        private void AddWordRow(Transform parent, string[] words, Func<int, Color> textColor,
            Func<int, bool> isRevealed, bool animate)
        {
            var rowGo = new GameObject("WordRow", typeof(RectTransform));
            rowGo.transform.SetParent(parent, false);
            var le = rowGo.AddComponent<LayoutElement>();
            le.minHeight = 52f; le.flexibleWidth = 1f;
            var hlg = rowGo.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 8f;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = true; hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false; hlg.childForceExpandHeight = false;
            var fitter = rowGo.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            if (words == null) return;
            for (int i = 0; i < words.Length; i++)
            {
                bool revealed = isRevealed == null || isRevealed(i);
                string w = words[i] ?? "";
                string shown = revealed
                    ? w.ToUpper()
                    : MakeBlank(w.Length);

                var slot = new GameObject($"Slot_{i}", typeof(RectTransform));
                slot.transform.SetParent(rowGo.transform, false);
                var sle = slot.AddComponent<LayoutElement>();
                sle.minHeight = 46f; sle.preferredHeight = 46f;
                sle.minWidth = 44f;
                var sImg = slot.AddComponent<Image>();
                UIThemeManager.ApplyRoundedButton(sImg);
                sImg.color = revealed ? C_SLOT_REVEALED : C_SLOT_BORDER;
                sImg.raycastTarget = false;

                var sFill = MakeFill(slot.transform, 3f);
                var sFillImg = sFill.GetComponent<Image>();
                UIThemeManager.ApplyRoundedButton(sFillImg);
                sFillImg.color = C_SLOT_BG;

                var txt = CreateText(sFill.transform, "Word", shown, 22,
                    TextAlignmentOptions.Center, textColor != null ? textColor(i) : C_BEST_WORD,
                    FontStyles.Bold);
                txt.enableWordWrapping = false;
                var padded = (RectTransform)txt.transform;
                padded.offsetMin = new Vector2(8f, 2f); padded.offsetMax = new Vector2(-8f, -2f);

                // ReduceMotion-gated reveal: revealed slots pop subtly in; blanks/static snap.
                if (animate && revealed && !UIAnimations.ReduceMotion)
                    StartCoroutine(AnimateSlotIn(slot.transform, i * 0.05f));
            }
        }

        private IEnumerator AnimateSlotIn(Transform slot, float delay)
        {
            if (slot == null) yield break;
            var rt = slot as RectTransform;
            if (rt == null) yield break;
            rt.localScale = new Vector3(0.6f, 0.6f, 1f);
            float t = -delay;
            const float dur = 0.18f;
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                if (slot == null) yield break;
                float p = Mathf.Clamp01(t / dur);
                float e = 1f - (1f - p) * (1f - p); // ease-out-quad
                rt.localScale = new Vector3(Mathf.Lerp(0.6f, 1f, e), Mathf.Lerp(0.6f, 1f, e), 1f);
                yield return null;
            }
            rt.localScale = Vector3.one;
        }

        // ================================================================
        //  Modern feel — staggered cascade entrance for cards/rows.
        //  Each card fades + slides up a few px into place, delayed by its
        //  index so the list/grid ripples in. ReduceMotion ⇒ instant (no
        //  CanvasGroup left over). Coroutine/Mathf-only — no per-frame GC.
        // ================================================================
        private void PlayCardCascade(Transform card, int index)
        {
            if (card == null) return;
            if (UIAnimations.ReduceMotion || !isActiveAndEnabled) return;
            var cg = card.GetComponent<CanvasGroup>();
            if (cg == null) cg = card.gameObject.AddComponent<CanvasGroup>();
            StartCoroutine(CascadeIn(card as RectTransform, cg, index * 0.045f));
        }

        // Layout-safe entrance: animate ALPHA + a gentle scale rise only. Cards live inside
        // Vertical/Grid LayoutGroups which own anchoredPosition, so we never touch position
        // (that would fight the layout); localScale is untouched by layout, so it's safe.
        private IEnumerator CascadeIn(RectTransform rt, CanvasGroup cg, float delay)
        {
            if (rt == null || cg == null) yield break;
            cg.alpha = 0f;
            rt.localScale = new Vector3(0.96f, 0.96f, 1f);

            float t = -delay;
            const float dur = 0.26f;
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                if (rt == null || cg == null) yield break;
                if (t < 0f) { yield return null; continue; }
                float p = UIAnimations.EaseOutCubic(Mathf.Clamp01(t / dur));
                cg.alpha = p;
                float s = Mathf.Lerp(0.96f, 1f, p);
                rt.localScale = new Vector3(s, s, 1f);
                yield return null;
            }
            rt.localScale = Vector3.one;
            cg.alpha = 1f;
        }

        private static string MakeBlank(int len)
        {
            if (len <= 0) len = 3;
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < len; i++) { if (i > 0) sb.Append(' '); sb.Append('_'); }
            return sb.ToString();
        }

        private void AddDetailLabel(Transform parent, string text, float size, Color color,
            FontStyles style, float height)
        {
            var go = new GameObject("Label", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var le = go.AddComponent<LayoutElement>();
            le.minHeight = height; le.flexibleWidth = 1f;
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text; tmp.fontSize = size; tmp.color = color;
            tmp.alignment = TextAlignmentOptions.Center; tmp.fontStyle = style;
            tmp.raycastTarget = false;
            tmp.enableWordWrapping = true;       // Large-Text safe — wraps rather than clipping.
            tmp.overflowMode = TextOverflowModes.Overflow;
        }

        private void AddReplayButton(Transform parent, int puzzleId)
        {
            var go = new GameObject("ReplayBtn", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var le = go.AddComponent<LayoutElement>();
            le.minHeight = 56f; le.preferredHeight = 56f; le.flexibleWidth = 1f;
            var img = go.AddComponent<Image>();
            UIThemeManager.ApplyOutlineButton(img, new Color32(0x8A, 0x93, 0xA1, 0xFF));
            var btn = go.AddComponent<Button>(); btn.transition = Selectable.Transition.None;
            int captured = puzzleId;
            btn.onClick.AddListener(() => { ClosePathDetail(); OnPuzzleSelected?.Invoke(captured); });
            CreateText(go.transform, "ReplayLabel", "REPLAY", 22,
                TextAlignmentOptions.Center, C_UNPLAYED_TEXT, FontStyles.Bold);
        }

        // Safe-area insets (left,bottom,right,top) in UI-space px relative to this Canvas. Returns
        // zero when there's no inset or no canvas (Editor/Simulator without a notch).
        private Vector4 GetSafeAreaInsets()
        {
            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null) return Vector4.zero;
            var sa = Screen.safeArea;
            float scale = canvas.scaleFactor <= 0f ? 1f : canvas.scaleFactor;
            float left = sa.xMin / scale;
            float bottom = sa.yMin / scale;
            float right = (Screen.width - sa.xMax) / scale;
            float top = (Screen.height - sa.yMax) / scale;
            return new Vector4(Mathf.Max(0f, left), Mathf.Max(0f, bottom),
                               Mathf.Max(0f, right), Mathf.Max(0f, top));
        }

        // ================================================================
        //  JSON wrapper types (mirror GameBootstrap's tier types)
        // ================================================================
        [Serializable] private class TierDefinitionsWrapper { public TierData[] tiers; }
        [Serializable] private class TierData { public int tierId; public bool isUnlocked; public PuzzleDefinition[] puzzles; }
        [Serializable] private class PuzzleDefinition { public int puzzleId; public string startWord; public string endWord; public int optimalSteps; public string[] solution; }
    }
}
