using UnityEngine;
using System.Collections.Generic;
using WordPuzzle.Modes;
using WordPuzzle.Persistence;

// ============================================================
//  AccessiblePalette — Task 9E
//  Central source of truth for gameplay tile colors that adapt
//  to colorblind modes. Lives here because UITheme.cs is the
//  established color-theme file; LetterTile reads these accessors
//  instead of its private hardcoded constants for state-dependent hues.
//
//  Off (default):
//    Correct  #6AAA64  (green)
//    Error    #D9534F  (red)
//    Hint     #C9B458  (gold)
//
//  Deuteranopia / highContrast:
//    Correct  #3A7CA5  (blue  — no red/green reliance)
//    Error    #E08214  (orange — distinct from blue in all deuteranopia sims)
//    Hint     #C9B458  (gold  — yellow-range, distinct from both)
//
//  Seeded at boot (and on every settings change) via Apply(SettingsData).
//  Callers: LetterTile.ApplyStateVisuals.
// ============================================================
namespace WordPuzzle.UI
{
    public static class AccessiblePalette
    {
        // --- Default hues (Off mode) ---
        private static readonly Color DefaultCorrect = HexToColor("#6AAA64");
        private static readonly Color DefaultError   = HexToColor("#D9534F");
        private static readonly Color DefaultHint    = HexToColor("#C9B458");

        // --- Colorblind-safe hues (Deuteranopia / highContrast) ---
        private static readonly Color CbCorrect = HexToColor("#3A7CA5");
        private static readonly Color CbError   = HexToColor("#E08214");
        private static readonly Color CbHint    = HexToColor("#C9B458"); // gold stays

        private static ColorBlindMode _mode = ColorBlindMode.Off;
        private static bool _highContrast   = false;
        private static float _textScale     = 1.0f;

        /// <summary>
        /// Seed palette from settings once at boot, and again whenever settings change.
        /// Main-thread only (Unity).
        /// </summary>
        public static void Apply(SettingsData settings)
        {
            if (settings == null) return;
            _mode         = settings.colorBlindMode;
            _highContrast = settings.highContrast;
            _textScale    = Mathf.Clamp(settings.textScale, 1.0f, 1.5f);
        }

        /// <summary>Text-size multiplier in [1.0, 1.5]. Multiply TMP fontSize by this.</summary>
        public static float TextScale => _textScale;

        private static bool UseAlt => _mode != ColorBlindMode.Off || _highContrast;

        /// <summary>Fill color for CorrectInChain tile state.</summary>
        public static Color Correct => UseAlt ? CbCorrect : DefaultCorrect;

        /// <summary>Fill color for InvalidFlash tile state.</summary>
        public static Color Error   => UseAlt ? CbError   : DefaultError;

        /// <summary>Fill color for RevealedByHint tile state.</summary>
        public static Color Hint    => UseAlt ? CbHint    : DefaultHint;

        private static Color HexToColor(string hex)
        {
            if (ColorUtility.TryParseHtmlString(hex, out var c)) return c;
            return Color.magenta;
        }
    }

    // ============================================================
    //  MenuPalette — Task 23
    //  Main-menu button palette. NO gold on the menu; each primary button gets its own distinct,
    //  bright on-brand fill (tuned for contrast on bg-base #0F1217), paired with a per-fill label
    //  colour chosen for legibility. Library/Stats stay a muted slate (secondary chrome). Button
    //  MEANING is carried by the text LABELS, so these fills are decorative and colorblind-safe
    //  (e.g. green Classic vs red Time Attack are still distinguished by their words).
    //  Centralized here so the menu has one source of truth — no scattered inline hex.
    //  NOTE: gold (#C9B458) is still the IN-GAME accent; this set intentionally only governs the menu.
    // ============================================================
    public static class MenuPalette
    {
        // Task 24 — ONE cohesive jewel-tone family: medium-deep fills at similar saturation/lightness,
        // evenly-spaced hues, so the buttons read as a designed set (not random). Every fill is deep
        // enough that a LIGHT label (#F5F7FA) sits on it with strong contrast — no dark-on-dark.
        public static readonly Color ResumeFill      = Hex("#1B9E8F"); // teal — "continue"
        public static readonly Color ResumeLabel     = Hex("#F5F7FA");
        public static readonly Color DailyFill       = Hex("#FF8A2E"); // Task 26 — bright clear orange hero border (thicker ring, no fill)
        public static readonly Color DailyLabel      = Hex("#F5F7FA");
        public static readonly Color ClassicFill     = Hex("#3D9E54"); // green
        public static readonly Color ClassicLabel    = Hex("#F5F7FA");
        public static readonly Color PuzzleShowFill  = Hex("#7B5FD4"); // violet
        public static readonly Color PuzzleShowLabel = Hex("#F5F7FA");
        public static readonly Color TimeAttackFill  = Hex("#D23F58"); // rose-red — urgency suits the timer
        public static readonly Color TimeAttackLabel = Hex("#F5F7FA");
        public static readonly Color SecondaryFill   = Hex("#39435A"); // calm slate family member — Library/Stats
        public static readonly Color SecondaryBorder = Hex("#8A93A1"); // Task 25 — visible muted ring for outline Library/Stats
        public static readonly Color SecondaryLabel  = Hex("#E7E1C4"); // cream — legible, slightly calmer
        public static readonly Color TitleColor      = Hex("#45E0E0"); // Task 28 — cyan WORD LADDER header

        private static Color Hex(string h) => ColorUtility.TryParseHtmlString(h, out var c) ? c : Color.magenta;
    }
}

/// <summary>
/// Centralized color theme system for Word Puzzle Game.
/// Stores all UI aesthetic colors and mode-specific colors.
/// NOTE: The gameplay tile palette (Correct/Error/Hint) is NOT here —
/// it lives in WordPuzzle.UI.AccessiblePalette (same file, above) so it
/// can adapt to colorblind modes at runtime.
/// </summary>
public class UITheme : ScriptableObject
{
    [System.Serializable]
    public struct ModeColorPalette
    {
        public ModeType modeType;
        public Color primaryColor;
        public Color accentColor;
    }

    [Header("Backgrounds")]
    [SerializeField] private Color darkBackground = new Color(0.1f, 0.1f, 0.18f); // #1a1a2e

    [Header("Game Mode Colors")]
    [SerializeField] private ModeColorPalette[] modeColors = new ModeColorPalette[3];

    [Header("Text Colors")]
    [SerializeField] private Color lightText = Color.white; // #ffffff
    [SerializeField] private Color subtleText = new Color(0.8f, 0.8f, 0.8f); // #cccccc

    [Header("Accent & Feedback")]
    [SerializeField] private Color accentGold = new Color(1f, 0.84f, 0f); // #ffd700
    [SerializeField] private Color errorRed = new Color(1f, 0.32f, 0.32f); // #ff5252

    private Dictionary<ModeType, ModeColorPalette> modeColorLookup;

    private void OnEnable()
    {
        InitializeModeColorLookup();
    }

    private void InitializeModeColorLookup()
    {
        modeColorLookup = new Dictionary<ModeType, ModeColorPalette>();
        foreach (var palette in modeColors)
        {
            if (!modeColorLookup.ContainsKey(palette.modeType))
                modeColorLookup[palette.modeType] = palette;
        }
    }

    /// <summary>Gets the primary color for a specific game mode. O(1) lookup.</summary>
    public Color GetModeColor(ModeType modeType)
    {
        if (modeColorLookup == null) InitializeModeColorLookup();
        if (modeColorLookup.TryGetValue(modeType, out var palette)) return palette.primaryColor;
        Logger.LogWarning($"Color not found for mode {modeType}, returning default");
        return accentGold;
    }

    /// <summary>Gets the accent color for a specific game mode. O(1) lookup.</summary>
    public Color GetModeAccentColor(ModeType modeType)
    {
        if (modeColorLookup == null) InitializeModeColorLookup();
        if (modeColorLookup.TryGetValue(modeType, out var palette)) return palette.accentColor;
        Logger.LogWarning($"Accent color not found for mode {modeType}, returning default");
        return accentGold;
    }

    public Color DarkBackground => darkBackground;
    public Color LightText      => lightText;
    public Color SubtleText     => subtleText;
    public Color AccentGold     => accentGold;
    public Color ErrorRed       => errorRed;
}

/// <summary>
/// Static accessor for the default UI theme.
/// Loads the theme from Resources/Themes/DefaultTheme.asset on first access.
/// NOTE: Not thread-safe — main Unity thread only.
/// </summary>
public static class UIThemeManager
{
    private static UITheme instance;

    public static UITheme Current
    {
        get
        {
            if (instance == null)
            {
                instance = Resources.Load<UITheme>("Themes/DefaultTheme");
                if (instance == null)
                    Logger.LogError("Failed to load default UI theme from Resources/Themes/DefaultTheme.asset");
            }
            return instance;
        }
    }

    /// <summary>Manually set a theme instance. Useful for testing or runtime theme switching.</summary>
    public static void SetTheme(UITheme theme) => instance = theme;

    // ── Task 22A — one shared, generously-rounded button background (9-slice). ──
    // A dedicated rounded-rect sprite (Assets/Resources/UI/RoundedButtonBubbly.png, 128px texture
    // with a baked 44px corner 9-slice border) replaces the timid built-in UISprite. Routing every
    // button background through here gives a consistent, crisp "bubbly" corner at any size — the
    // small power-up button and the wide menu button alike — without stretching the corner. Falls
    // back to Unity's built-in UISprite only if the dedicated asset is missing.
    private static UnityEngine.Sprite _roundedButton;
    private static bool _roundedButtonLoaded;
    public static UnityEngine.Sprite RoundedButtonSprite
    {
        get
        {
            if (!_roundedButtonLoaded)
            {
                _roundedButton = UnityEngine.Resources.Load<UnityEngine.Sprite>("UI/RoundedButtonBubbly");
                if (_roundedButton == null)
                    _roundedButton = UnityEngine.Resources.GetBuiltinResource<UnityEngine.Sprite>("UI/Skin/UISprite.psd");
                _roundedButtonLoaded = true;
            }
            return _roundedButton;
        }
    }

    /// <summary>
    /// Apply the shared generously-rounded background to a button/panel Image without changing its
    /// colour, raycast, or children (badges/icons/labels). The corner radius is baked into the
    /// 9-slice sprite, so pixelsPerUnitMultiplier stays at 1 to honour the designed (bubbly) radius.
    /// Safe to call repeatedly; no-op if the Image or sprite is unavailable.
    /// </summary>
    public static void ApplyRoundedButton(UnityEngine.UI.Image img, float pixelsPerUnitMultiplier = 1f)
    {
        if (img == null) return;
        var sprite = RoundedButtonSprite;
        if (sprite == null) return;
        img.sprite = sprite;
        img.type = UnityEngine.UI.Image.Type.Sliced;
        img.pixelsPerUnitMultiplier = pixelsPerUnitMultiplier;
    }

    // ============================================================
    //  Task 25 — true-black background + outline ("ghost") buttons.
    // ============================================================

    /// <summary>App background — neutral near-black #0A0A0A (Task 26: no blue/teal cast; reads as genuine black).</summary>
    public static readonly UnityEngine.Color AppBackground =
        new UnityEngine.Color(0x0A / 255f, 0x0A / 255f, 0x0A / 255f, 1f);

    private const string BackgroundLayerName = "BackgroundLayer";

    /// <summary>
    /// Task 26 — make a screen render over the one shared full-screen BackgroundLayer: the screen root
    /// Image is made transparent (so the layer shows through) and the layer is ensured behind all UI.
    /// Also forces the main camera's clear colour to black. Runtime only — no scene edit.
    /// </summary>
    public static void ApplyScreenBackground(UnityEngine.GameObject root)
    {
        if (root != null)
        {
            var img = root.GetComponent<UnityEngine.UI.Image>();
            if (img != null) img.color = new UnityEngine.Color(0f, 0f, 0f, 0f); // transparent — shared layer shows through
            EnsureBackgroundLayer(root.transform);
        }
        var cam = UnityEngine.Camera.main;
        if (cam != null)
        {
            cam.clearFlags = UnityEngine.CameraClearFlags.SolidColor;
            cam.backgroundColor = AppBackground;
        }
    }

    /// <summary>
    /// Task 26 — ensure ONE reusable full-screen "BackgroundLayer" Image exists as the first child of the
    /// root Canvas (so it renders behind every screen). It is filled with the flat app black now, and will
    /// auto-display a space backdrop the moment a sprite is dropped at Resources/UI/SpaceBackground.png
    /// (no scene edit, no restructuring). Stretches to fill the Canvas on every device; never eats taps.
    /// </summary>
    public static void EnsureBackgroundLayer(UnityEngine.Transform underCanvas)
    {
        if (underCanvas == null) return;
        var canvas = underCanvas.GetComponentInParent<UnityEngine.Canvas>();
        if (canvas == null) return;
        canvas = canvas.rootCanvas;

        var existing = canvas.transform.Find(BackgroundLayerName);
        UnityEngine.UI.Image img;
        if (existing != null)
        {
            img = existing.GetComponent<UnityEngine.UI.Image>();
            if (img == null) img = existing.gameObject.AddComponent<UnityEngine.UI.Image>();
        }
        else
        {
            var go = new UnityEngine.GameObject(BackgroundLayerName,
                typeof(UnityEngine.RectTransform), typeof(UnityEngine.CanvasRenderer), typeof(UnityEngine.UI.Image));
            go.transform.SetParent(canvas.transform, false);
            img = go.GetComponent<UnityEngine.UI.Image>();
        }

        var rt = img.rectTransform;
        rt.anchorMin = UnityEngine.Vector2.zero;
        rt.anchorMax = UnityEngine.Vector2.one;
        rt.offsetMin = UnityEngine.Vector2.zero;
        rt.offsetMax = UnityEngine.Vector2.zero;
        rt.localScale = UnityEngine.Vector3.one;
        img.raycastTarget = false; // never block taps to the buttons in front

        // Backdrop priority: a LOOPING VIDEO (Resources/UI/SpaceBackground.mp4) > a still sprite
        // (SpaceBackground.png) > flat black. All swappable by just dropping the file in — no scene edit.
        var clip = UnityEngine.Resources.Load<UnityEngine.Video.VideoClip>("UI/SpaceBackground");
        if (clip != null)
        {
            img.sprite = null;
            img.color = AppBackground;        // black base behind the video while it warms up
            EnsureVideoBackground(img.transform, clip);
        }
        else
        {
            RemoveVideoBackground(img.transform);
            var space = UnityEngine.Resources.Load<UnityEngine.Sprite>("UI/SpaceBackground");
            if (space != null)
            {
                img.sprite = space;
                img.type = UnityEngine.UI.Image.Type.Simple;
                img.preserveAspect = false;   // stretch to fill — no gaps on any aspect ratio
                img.color = UnityEngine.Color.white; // untinted so the image shows its true colours
            }
            else
            {
                img.sprite = null;
                img.color = AppBackground;     // flat neutral black for now
            }
        }

        img.transform.SetAsFirstSibling(); // behind every screen
    }

    private const string VideoSurfaceName = "VideoSurface";

    /// <summary>
    /// Task 27.1 — set up (once) a full-screen RawImage child driven by a LOOPING, MUTED VideoPlayer that
    /// renders the clip into a RenderTexture, so a video backdrop sits behind every screen and loops
    /// forever. Idempotent: reuses the existing surface; only (re)configures when the clip changes.
    /// </summary>
    private static void EnsureVideoBackground(UnityEngine.Transform layer, UnityEngine.Video.VideoClip clip)
    {
        if (layer == null || clip == null) return;

        var t = layer.Find(VideoSurfaceName);
        UnityEngine.UI.RawImage raw;
        UnityEngine.Video.VideoPlayer vp;
        if (t != null)
        {
            raw = t.GetComponent<UnityEngine.UI.RawImage>();
            vp  = t.GetComponent<UnityEngine.Video.VideoPlayer>();
        }
        else
        {
            var go = new UnityEngine.GameObject(VideoSurfaceName,
                typeof(UnityEngine.RectTransform), typeof(UnityEngine.CanvasRenderer),
                typeof(UnityEngine.UI.RawImage), typeof(UnityEngine.Video.VideoPlayer));
            go.transform.SetParent(layer, false);
            raw = go.GetComponent<UnityEngine.UI.RawImage>();
            vp  = go.GetComponent<UnityEngine.Video.VideoPlayer>();
        }
        if (raw == null || vp == null) return;

        var rrt = raw.rectTransform;
        rrt.anchorMin = UnityEngine.Vector2.zero;
        rrt.anchorMax = UnityEngine.Vector2.one;
        rrt.offsetMin = UnityEngine.Vector2.zero;
        rrt.offsetMax = UnityEngine.Vector2.zero;
        rrt.localScale = UnityEngine.Vector3.one;
        raw.raycastTarget = false;
        raw.color = UnityEngine.Color.white;

        // Configure the player + render target once (or when the clip changes).
        if (vp.clip != clip || vp.targetTexture == null)
        {
            if (vp.targetTexture == null)
            {
                var renderTex = new UnityEngine.RenderTexture(1080, 1920, 0) { name = "SpaceBackgroundRT" };
                renderTex.filterMode = UnityEngine.FilterMode.Point; // keep pixel art crisp
                vp.targetTexture = renderTex;
                raw.texture = renderTex;
            }
            vp.source            = UnityEngine.Video.VideoSource.VideoClip;
            vp.clip              = clip;
            vp.renderMode        = UnityEngine.Video.VideoRenderMode.RenderTexture;
            vp.aspectRatio       = UnityEngine.Video.VideoAspectRatio.FitOutside; // cover — fill, no distortion
            vp.isLooping         = true;
            vp.playOnAwake       = true;
            vp.waitForFirstFrame = true;
            vp.skipOnDrop        = true;
            vp.audioOutputMode   = UnityEngine.Video.VideoAudioOutputMode.None;   // muted
            vp.Play();
        }
        else if (!vp.isPlaying)
        {
            vp.Play();
        }

        raw.transform.SetAsLastSibling(); // cover the layer's black base
    }

    private static void RemoveVideoBackground(UnityEngine.Transform layer)
    {
        if (layer == null) return;
        var t = layer.Find(VideoSurfaceName);
        if (t != null) UnityEngine.Object.Destroy(t.gameObject);
    }

    // Border-only rounded 9-slice ring (transparent centre) — Assets/Resources/UI/RoundedButtonOutline.png.
    // Same 44px corner border as the solid bubbly sprite, so the rounded corner matches Task 22.
    private static UnityEngine.Sprite _outlineButton;
    private static bool _outlineButtonLoaded;
    public static UnityEngine.Sprite OutlineButtonSprite
    {
        get
        {
            if (!_outlineButtonLoaded)
            {
                _outlineButton = UnityEngine.Resources.Load<UnityEngine.Sprite>("UI/RoundedButtonOutline");
                _outlineButtonLoaded = true;
            }
            return _outlineButton;
        }
    }

    // Task 26 — thicker hero ring (RoundedButtonOutlineHero.png) for the primary action (Daily).
    private static UnityEngine.Sprite _heroOutlineButton;
    private static bool _heroOutlineButtonLoaded;
    public static UnityEngine.Sprite HeroOutlineButtonSprite
    {
        get
        {
            if (!_heroOutlineButtonLoaded)
            {
                _heroOutlineButton = UnityEngine.Resources.Load<UnityEngine.Sprite>("UI/RoundedButtonOutlineHero");
                _heroOutlineButtonLoaded = true;
            }
            return _heroOutlineButton;
        }
    }

    private const string RingChildName = "__OutlineBorder";

    /// <summary>
    /// Render a button/card as a coloured rounded OUTLINE with a transparent centre (the black bg shows
    /// through). The border takes <paramref name="borderColor"/>. Falls back to the filled bubbly sprite
    /// (tinted) only if the outline asset is missing, so a button is never invisible. Visual only — leaves
    /// raycast and children (labels/icons/badges) untouched. Clears any prior hero faint-fill ring child.
    /// </summary>
    public static void ApplyOutlineButton(UnityEngine.UI.Image img, UnityEngine.Color borderColor)
    {
        if (img == null) return;
        var ring = OutlineButtonSprite;
        img.sprite = ring != null ? ring : RoundedButtonSprite;
        img.type = UnityEngine.UI.Image.Type.Sliced;
        img.pixelsPerUnitMultiplier = 1f;
        img.color = borderColor;
        RemoveRingChild(img.transform);
    }

    /// <summary>Convenience: render a Button as a coloured outline and set its label colour in one call.</summary>
    public static void ApplyOutlineButton(UnityEngine.UI.Button btn, UnityEngine.Color borderColor, UnityEngine.Color labelColor)
    {
        if (btn == null) return;
        ApplyOutlineButton(btn.GetComponent<UnityEngine.UI.Image>(), borderColor);
        var label = btn.GetComponentInChildren<TMPro.TMP_Text>(true);
        if (label != null) label.color = labelColor;
    }

    /// <summary>
    /// Task 26 — Hero outline: a THICKER, brighter ring with NO fill, so a primary action (e.g. Daily)
    /// stands out while staying a clean transparent outline like its peers (the old faint fill read as a
    /// muddy brown over black). Uses the dedicated thicker ring sprite (RoundedButtonOutlineHero), falling
    /// back to the normal ring. Visual only; removes any leftover faint-fill ring child from the old style.
    /// </summary>
    public static void ApplyHeroOutlineButton(UnityEngine.UI.Image img, UnityEngine.Color borderColor)
    {
        if (img == null) return;
        var ring = HeroOutlineButtonSprite != null ? HeroOutlineButtonSprite : OutlineButtonSprite;
        img.sprite = ring != null ? ring : RoundedButtonSprite;
        img.type = UnityEngine.UI.Image.Type.Sliced;
        img.pixelsPerUnitMultiplier = 1f;
        img.color = borderColor;
        RemoveRingChild(img.transform); // drop the old faint-fill ring child if it exists
    }

    private static void RemoveRingChild(UnityEngine.Transform parent)
    {
        var existing = parent.Find(RingChildName);
        if (existing != null) UnityEngine.Object.Destroy(existing.gameObject);
    }
}
