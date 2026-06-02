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
        public static readonly Color DailyFill       = Hex("#DD7E2A"); // amber — warm hero (also larger font)
        public static readonly Color DailyLabel      = Hex("#F5F7FA");
        public static readonly Color ClassicFill     = Hex("#3D9E54"); // green
        public static readonly Color ClassicLabel    = Hex("#F5F7FA");
        public static readonly Color PuzzleShowFill  = Hex("#7B5FD4"); // violet
        public static readonly Color PuzzleShowLabel = Hex("#F5F7FA");
        public static readonly Color TimeAttackFill  = Hex("#D23F58"); // rose-red — urgency suits the timer
        public static readonly Color TimeAttackLabel = Hex("#F5F7FA");
        public static readonly Color SecondaryFill   = Hex("#39435A"); // calm slate family member — Library/Stats
        public static readonly Color SecondaryLabel  = Hex("#E7E1C4"); // cream — legible, slightly calmer
        public static readonly Color TitleColor      = Hex("#F5F7FA"); // flat, bright, non-gold title

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
}
