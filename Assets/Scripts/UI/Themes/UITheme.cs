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
//  Off (default) — Direction B (forwards to Palette):
//    Correct  Palette.AccentAqua  (#54A8B4 aqua-spark — retired green)
//    Error    Palette.Alert       (#E08A8A warm red)
//    Hint     Palette.Coins       (#E9C98C warm gold)
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
    /// <summary>
    /// Direction B — the SINGLE source of truth for the app's purple-dominant palette, sampled from the
    /// cloud-scape backdrop (Assets/ui/back grounds/lucid pixel art.jpg). Tokens are named by ROLE so screens
    /// reference meaning, not hex. This is the ONLY place raw theme hex literals live; every other palette
    /// class and screen forwards to these. Modes differ by hue-spacing + brightness (NOT temperature) and
    /// walk one cool family; Daily is the hero by brightness + a heavier stroke. Aqua-spark is the one cool
    /// accent; warm Coins + Alert are the only warm notes — kept minimal so the purple reads rich, not flat.
    /// Depth-floor rule: outlines may go as deep as the foundation, but LABELS use a bright token
    /// (<see cref="ModeLabel"/>/<see cref="TextPrimary"/>), never the deep outline colour.
    /// </summary>
    public static class Palette
    {
        // Foundation (surfaces) — purple-black void up through amethyst.
        public static readonly Color SurfaceVoid = Hex("#0D0A1F");
        public static readonly Color Surface     = Hex("#1C1640");
        public static readonly Color Panel       = Hex("#2E2560");
        public static readonly Color Amethyst    = Hex("#473A7E");

        // Accents (deep jewel). Aqua is the ONE cool non-purple highlight.
        public static readonly Color AccentLavender   = Hex("#9F7ED6");
        public static readonly Color AccentPeriwinkle = Hex("#8E78C8");
        public static readonly Color AccentOrchid     = Hex("#B072BC");
        public static readonly Color AccentAqua       = Hex("#54A8B4");

        // Mode buttons — one cool family, hue-spaced; Daily is the hero (brightest + heavier stroke).
        public static readonly Color ModeDaily      = Hex("#BE84E2"); // orchid hero
        public static readonly Color ModeClassic    = Hex("#6E84D6"); // blue-violet
        public static readonly Color ModePuzzleShow = Hex("#8160D2"); // deep violet
        public static readonly Color ModeTimeAttack = Hex("#B25EB8"); // magenta-violet

        // Text & semantic. Coins + Alert are WARM — the only warm notes; keep.
        public static readonly Color TextPrimary = Hex("#EFEAF8");
        public static readonly Color TextMuted   = Hex("#ABA0CE"); // Task 42 — raised from #9A8FBE: clears WCAG AA on Panel at Caption sizes
        public static readonly Color Coins       = Hex("#E9C98C"); // warm gold — in-game accent, hints, streak
        public static readonly Color Alert       = Hex("#E08A8A"); // warm red — errors, destructive actions

        // Derived role helpers (no new hex) — keep call-sites semantic.
        public static Color OutlineMuted => AccentPeriwinkle; // calm purple ring (secondary chrome)
        public static Color CardOutline  => Amethyst;          // subtle card-grouping ring
        public static Color ModeLabel    => TextPrimary;       // button labels — always bright (depth-floor rule)

        private static Color Hex(string h) => ColorUtility.TryParseHtmlString(h, out var c) ? c : Color.magenta;
    }

    public static class AccessiblePalette
    {
        // --- Default hues (Off mode) — Direction B: forward to the canonical Palette ---
        private static readonly Color DefaultCorrect = Palette.AccentAqua; // retired green → aqua-spark
        private static readonly Color DefaultError   = Palette.Alert;
        private static readonly Color DefaultHint    = Palette.Coins;

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
        // Direction B — forwards to the canonical Palette (one source of truth). Mode fills are the cool
        // family; Daily is the hero (brightest, heavier ring). Labels are always TextPrimary (bright) per the
        // depth-floor rule. Library/Stats stay secondary chrome (panel fill + periwinkle ring).
        public static readonly Color ResumeFill      = Palette.AccentAqua;     // "continue" — calm cool highlight
        public static readonly Color ResumeLabel     = Palette.TextPrimary;
        public static readonly Color DailyFill       = Palette.ModeDaily;      // orchid hero (retired warm orange)
        public static readonly Color DailyLabel      = Palette.TextPrimary;
        public static readonly Color ClassicFill     = Palette.ModeClassic;    // blue-violet
        public static readonly Color ClassicLabel    = Palette.TextPrimary;
        public static readonly Color PuzzleShowFill  = Palette.ModePuzzleShow; // deep violet
        public static readonly Color PuzzleShowLabel = Palette.TextPrimary;
        public static readonly Color TimeAttackFill  = Palette.ModeTimeAttack; // magenta-violet
        public static readonly Color TimeAttackLabel = Palette.TextPrimary;
        public static readonly Color SecondaryFill   = Palette.Panel;
        public static readonly Color SecondaryBorder = Palette.AccentPeriwinkle;
        public static readonly Color SecondaryLabel  = Palette.TextPrimary;
        public static readonly Color TitleColor      = Palette.AccentAqua;     // hero header ties to the bright star
        public static readonly Color ChainOutline    = Palette.AccentPeriwinkle; // played chain rows — on-purple
    }

    /// <summary>
    /// Non-menu / in-game accent tokens, centralized so screens stop re-declaring the same hex
    /// (Task 38). <see cref="Gold"/> is the documented IN-GAME accent (#C9B458) — hints, active input,
    /// the streak headline, win/tier accents (NOT a menu button colour). <see cref="CardOutline"/> is a
    /// subtle slate ring for grouping cards on the black+outline screens (e.g. the Stats sections).
    /// </summary>
    public static class GameAccents
    {
        // Direction B — forward to the canonical Palette. Gold is the warm in-game accent (kept).
        public static readonly Color Gold        = Palette.Coins;
        public static readonly Color CardOutline = Palette.CardOutline;
        public static readonly Color Danger      = Palette.Alert; // warm red — destructive actions (Reset Progress ring)
    }

    /// <summary>
    /// On-screen keyboard key palette (Classic polish pass). Centralizes the keyboard's colours so
    /// OnScreenKeyboard stops carrying inline hex. <see cref="KeyFill"/> is the darker deep-indigo key
    /// background that ties to the nebula/space palette; the cream <see cref="KeyText"/> letters stay
    /// crisp on it (~9:1 contrast). DEL keeps the accent red, GO the accent green; <see cref="KeyFlash"/>
    /// is the brief gold highlight pulse.
    /// </summary>
    public static class KeyboardPalette
    {
        // Direction B — forward to the canonical Palette. Deep-indigo keys, bright letters, warm flash.
        public static readonly Color KeyFill  = Palette.Panel;       // deep indigo-purple key background
        public static readonly Color KeyText  = Palette.TextPrimary; // bright letters
        public static readonly Color DelFill  = Palette.Alert;       // warm red (DEL)
        public static readonly Color GoFill   = Palette.AccentAqua;  // aqua (GO) — matches new correct=aqua
        public static readonly Color KeyFlash = Palette.Coins;       // warm gold highlight pulse
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

    // ── Task 42 — typography seam. The canonical type system lives in WordPuzzle.UI
    //    (TypeScale / ThemeFonts in Themes/UITypeScale.cs — sibling file, <500-line rule);
    //    these statics keep the documented UITheme.* seam names callable from anywhere.
    public static void ApplyType(TMPro.TMP_Text text, WordPuzzle.UI.TypeRole role)
        => WordPuzzle.UI.TypeScale.Apply(text, role);

    public static class Fonts
    {
        public static TMPro.TMP_FontAsset Regular  => WordPuzzle.UI.ThemeFonts.Regular;
        public static TMPro.TMP_FontAsset Medium   => WordPuzzle.UI.ThemeFonts.Medium;
        public static TMPro.TMP_FontAsset SemiBold => WordPuzzle.UI.ThemeFonts.SemiBold;
        public static TMPro.TMP_FontAsset Bold     => WordPuzzle.UI.ThemeFonts.Bold;
    }
}

/// <summary>
/// Static accessor for the default UI theme.
/// Loads the theme from Resources/Themes/DefaultTheme.asset on first access.
/// NOTE: Not thread-safe — main Unity thread only.
/// Partial: the Task 43 button-tier hierarchy lives in Themes/UIThemeManagerTiers.cs.
/// </summary>
public static partial class UIThemeManager
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

    /// <summary>App background — Direction B purple-black void (Palette.SurfaceVoid #0D0A1F), the base behind
    /// the cloud-scape backdrop video.</summary>
    public static readonly UnityEngine.Color AppBackground = WordPuzzle.UI.Palette.SurfaceVoid;

    private const string BackgroundLayerName = "BackgroundLayer";

    /// <summary>
    /// Task 26 — make a screen render over the one shared full-screen BackgroundLayer: the screen root
    /// Image is made transparent (so the layer shows through) and the layer is ensured behind all UI.
    /// Also forces the main camera's clear colour to black. Runtime only — no scene edit.
    /// </summary>
    /// <summary>Default readability-scrim alpha for text-heavy menu screens — a soft black veil over the
    /// shared backdrop video so its art (e.g. the space-scene rocket) stops competing with foreground text.</summary>
    public const float ReadabilityScrimAlpha = 0.4f;

    public static void ApplyScreenBackground(UnityEngine.GameObject root, float scrimAlpha = 0f, bool gameplayScrim = false)
    {
        if (root != null)
        {
            var img = root.GetComponent<UnityEngine.UI.Image>();
            // scrimAlpha 0  -> fully transparent: the shared backdrop video shows through (original behaviour).
            // scrimAlpha >0 -> a dark readability scrim painted OVER the shared video but UNDER this screen's
            //                  content (a GameObject's own Image renders behind its children), so busy backdrop
            //                  art no longer overlaps text. Ensure an Image exists when a scrim is requested.
            if (img == null && scrimAlpha > 0f)
            {
                img = root.AddComponent<UnityEngine.UI.Image>();
                img.raycastTarget = false; // visual only — do not change tap behaviour
            }
            if (img != null) img.color = new UnityEngine.Color(0f, 0f, 0f, UnityEngine.Mathf.Clamp01(scrimAlpha));
            EnsureBackgroundLayer(root.transform);
            SetGameplayScrim(root.transform, gameplayScrim); // Task 44 — gradient scrim, gameplay-only

            // Task 44 — systematized safe-area: every screen that requests its background through this
            // seam gets its content root safe-area'd; the shared backdrop + gameplay scrim live on the
            // canvas-level BackgroundLayer, so they intentionally stay full-bleed outside the panel.
            if (root.GetComponent<WordPuzzle.UI.Components.SafeAreaPanel>() == null)
                root.AddComponent<WordPuzzle.UI.Components.SafeAreaPanel>();
        }
        var cam = UnityEngine.Camera.main;
        if (cam != null)
        {
            cam.clearFlags = UnityEngine.CameraClearFlags.SolidColor;
            cam.backgroundColor = AppBackground;
        }
    }

    /// <summary>
    /// Full-screen OVERLAY background (Shop, full-screen popups). Unlike <see cref="ApplyScreenBackground"/>
    /// (which is transparent and relies on the one shared backdrop layer behind every screen), an overlay is
    /// drawn ON TOP of another screen, so it must be OPAQUE or the screen behind would show through. This
    /// paints the root with the opaque app base, then mounts the SAME app backdrop — the looping video
    /// (&gt; still sprite &gt; flat base) — as a BACKMOST child (behind the overlay's content), so the overlay
    /// matches every other screen instead of a stale still image. Routed through here so it can't drift again.
    /// </summary>
    public static void ApplyOverlayBackground(UnityEngine.GameObject root)
    {
        if (root == null) return;
        var img = root.GetComponent<UnityEngine.UI.Image>();
        if (img == null) img = root.AddComponent<UnityEngine.UI.Image>();
        img.sprite = null;
        img.type = UnityEngine.UI.Image.Type.Simple;
        img.color = AppBackground;   // opaque purple-black base — occludes the screen behind this overlay
        img.raycastTarget = true;    // swallow taps so they don't fall through

        // Task 44 — the overlay's backdrop obeys the same video gating as the shared layer.
        var clip = UnityEngine.Resources.Load<UnityEngine.Video.VideoClip>("UI/CloudBackground")
                ?? UnityEngine.Resources.Load<UnityEngine.Video.VideoClip>("UI/SpaceBackground");
        var sprite = UnityEngine.Resources.Load<UnityEngine.Sprite>("UI/CloudBackground")
                  ?? UnityEngine.Resources.Load<UnityEngine.Sprite>("UI/SpaceBackground");
        switch (ResolveBackdrop(clip != null, sprite != null,
                    WordPuzzle.UI.UIAnimations.ReduceMotion, LowPowerGated))
        {
            case BackdropKind.Video:
                EnsureVideoBackground(root.transform, clip, sendToBack: true);
                break;
            case BackdropKind.Still:
                RemoveVideoBackground(root.transform);
                img.sprite = sprite;
                img.preserveAspect = false;        // stretch to fill — no gaps on any aspect ratio
                img.color = UnityEngine.Color.white;
                break;
            default:
                RemoveVideoBackground(root.transform);
                break; // the opaque AppBackground base painted above stands
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
        // (SpaceBackground.png) > flat black — all swappable by just dropping the file in. Task 44:
        // the video is GATED (ReduceMotion / OS low-power ⇒ still) through the pure ResolveBackdrop;
        // this runs on every screen's background request, so a mid-session settings change swaps the
        // layer on the next screen transition — no hot-swap mid-frame.
        var clip  = UnityEngine.Resources.Load<UnityEngine.Video.VideoClip>("UI/SpaceBackground");
        var space = UnityEngine.Resources.Load<UnityEngine.Sprite>("UI/SpaceBackground");
        switch (ResolveBackdrop(clip != null, space != null,
                    WordPuzzle.UI.UIAnimations.ReduceMotion, LowPowerGated))
        {
            case BackdropKind.Video:
                img.sprite = null;
                img.color = AppBackground;    // black base behind the video while it warms up
                EnsureVideoBackground(img.transform, clip);
                break;
            case BackdropKind.Still:
                RemoveVideoBackground(img.transform);
                img.sprite = space;
                img.type = UnityEngine.UI.Image.Type.Simple;
                img.preserveAspect = false;   // stretch to fill — no gaps on any aspect ratio
                img.color = UnityEngine.Color.white; // untinted so the image shows its true colours
                break;
            default:
                RemoveVideoBackground(img.transform);
                img.sprite = null;
                img.color = AppBackground;     // flat neutral base
                break;
        }

        img.transform.SetAsFirstSibling(); // behind every screen
    }

    private const string VideoSurfaceName = "VideoSurface";

    /// <summary>
    /// Task 27.1 — set up (once) a full-screen RawImage child driven by a LOOPING, MUTED VideoPlayer that
    /// renders the clip into a RenderTexture, so a video backdrop sits behind every screen and loops
    /// forever. Idempotent: reuses the existing surface; only (re)configures when the clip changes.
    /// </summary>
    private static void EnsureVideoBackground(UnityEngine.Transform layer, UnityEngine.Video.VideoClip clip, bool sendToBack = false)
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

        // Task 44 — battery hygiene: pause the loop while the app is backgrounded / unfocused.
        if (vp.GetComponent<WordPuzzle.UI.Components.VideoBackdropPauser>() == null)
            vp.gameObject.AddComponent<WordPuzzle.UI.Components.VideoBackdropPauser>();

        if (sendToBack) raw.transform.SetAsFirstSibling(); // behind an overlay's own content
        else            raw.transform.SetAsLastSibling();  // cover the shared layer's black base
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
        ApplyNeonGlow(img, borderColor, hero: false); // Polish — subtle neon halo matching the border colour.
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
        => ApplyPrimaryMenuButton(img, borderColor, heroGlow: true);

    /// <summary>
    /// Shared PRIMARY menu-button style — the ONE outline geometry every primary-stack button (Daily,
    /// Classic, Puzzle Show, Time Attack, Resume) uses, so they read as one consistent set and cannot drift
    /// apart: the SAME thicker hero ring sprite (stroke + 44px corner) over a transparent centre. The only
    /// hero distinction is the GLOW — <paramref name="heroGlow"/> true = the brighter Daily glow, false = the
    /// standard glow. SIZE/position come from the menu layout (ArrangeMenu), not here; this only sets the
    /// sprite/stroke/colour/glow. Visual only; removes any leftover faint-fill ring child; leaves raycast +
    /// children (labels/icons) untouched.
    /// </summary>
    public static void ApplyPrimaryMenuButton(UnityEngine.UI.Image img, UnityEngine.Color borderColor, bool heroGlow)
    {
        if (img == null) return;
        var ring = HeroOutlineButtonSprite != null ? HeroOutlineButtonSprite : OutlineButtonSprite;
        img.sprite = ring != null ? ring : RoundedButtonSprite;
        img.type = UnityEngine.UI.Image.Type.Sliced;
        img.pixelsPerUnitMultiplier = 1f;
        img.color = borderColor;
        RemoveRingChild(img.transform); // drop the old faint-fill ring child if it exists
        ApplyNeonGlow(img, borderColor, hero: heroGlow);
    }

    private static void RemoveRingChild(UnityEngine.Transform parent)
    {
        var existing = parent.Find(RingChildName);
        if (existing != null) UnityEngine.Object.Destroy(existing.gameObject);
    }

    // ============================================================
    //  Polish — TIGHT NEON-TUBE glow on outline buttons.
    //  The outline must read as a luminous LINE, not a fuzzy cloud: a
    //  small glow radius that HUGS the stroke with minimal outer bleed.
    //  A dedicated child Image sits BEHIND the button using the same
    //  rounded-ring sprite at the SAME size (no upscale, so the glow does
    //  not balloon past the outline), and several faint Shadow copies are
    //  spread over a TIGHT radius (~2px) for an even, crisp tube glow.
    //  Daily (hero) is slightly BRIGHTER (higher alpha) but stays just as
    //  tight — a brighter line, never a bigger halo. Static (no pulsing).
    // ============================================================

    private const string GlowChildName = "__NeonGlow";

    // Outward radius of the glow, in UI px. Kept SMALL so the light hugs the
    // stroke as a crisp tube — larger values bloom into a bulky halo.
    private const float GlowRadiusNormal = 2f;
    private const float GlowRadiusHero   = 2f; // hero stays tight — brighter, not wider.
    // Glow ring size vs the button (1.0 = exactly on the stroke). NO upscale:
    // an upscaled ring pushes light out past the outline into the background.
    private const float GlowScaleNormal = 1f;
    private const float GlowScaleHero   = 1f;
    // Per-copy alpha. MANY faint copies (GlowSamples) build the perceived
    // glow. At the tight radius the copies sit right on the stroke, so the
    // line reads luminous. EVERY menu/mode button now carries the standard
    // glow (tinted to its own outline token); Daily is the HERO — a notch
    // brighter so it clearly leads while the rest stay present-but-gentler.
    // Two named intensities, tunable; raising these does NOT widen the glow
    // (radius/scale stay fixed), so it stays a TIGHT soft glow, not a halo.
    private const float GlowAlphaNormal = 0.22f; // standard — clearly present soft glow (was 0.14, too faint to read)
    private const float GlowAlphaHero   = 0.32f; // Daily hero — a notch brighter than standard (was 0.22)

    // Eight directions (cardinals + diagonals) at the tight radius give an
    // even, ring-shaped tube glow rather than a one-sided shadow.
    private const int GlowSamples = 8;

    /// <summary>
    /// Polish — give an outline button/card a soft, static NEON GLOW halo tinted to its border colour.
    /// Builds a dedicated child Image BEHIND the button (the same rounded-ring sprite, slightly upscaled)
    /// and spreads several very-faint <see cref="UnityEngine.UI.Shadow"/> copies of it over a wide radius,
    /// so the result reads as a soft light halo — NOT a thicker border. Idempotent: re-applying reuses the
    /// existing glow child and re-tints it to the CURRENT border colour. The glow child never eats taps and
    /// leaves the crisp outline, labels, icons, and raycast untouched. No external art assets required.
    /// </summary>
    public static void ApplyNeonGlow(UnityEngine.UI.Image img, UnityEngine.Color borderColor, bool hero)
    {
        if (img == null) return;

        float radius = hero ? GlowRadiusHero : GlowRadiusNormal;
        float scale  = hero ? GlowScaleHero  : GlowScaleNormal;
        float alpha  = hero ? GlowAlphaHero   : GlowAlphaNormal;

        // ── Find or create the dedicated glow child (rendered first => behind siblings). ──
        var existing = img.transform.Find(GlowChildName);
        UnityEngine.UI.Image glow;
        if (existing != null)
        {
            glow = existing.GetComponent<UnityEngine.UI.Image>();
            if (glow == null) glow = existing.gameObject.AddComponent<UnityEngine.UI.Image>();
        }
        else
        {
            var go = new UnityEngine.GameObject(GlowChildName,
                typeof(UnityEngine.RectTransform), typeof(UnityEngine.CanvasRenderer), typeof(UnityEngine.UI.Image));
            go.transform.SetParent(img.transform, false);
            glow = go.GetComponent<UnityEngine.UI.Image>();
        }

        // Match the button's rounded-ring sprite so the halo follows the same shape.
        glow.sprite = img.sprite;
        glow.type = UnityEngine.UI.Image.Type.Sliced;
        glow.pixelsPerUnitMultiplier = img.pixelsPerUnitMultiplier;
        glow.raycastTarget = false;                 // never eats taps
        var glowColor = borderColor; glowColor.a = alpha;
        glow.color = glowColor;

        // Stretch to the button's rect, then upscale slightly so the halo sits just outside the border.
        var grt = glow.rectTransform;
        grt.anchorMin = UnityEngine.Vector2.zero;
        grt.anchorMax = UnityEngine.Vector2.one;
        grt.offsetMin = UnityEngine.Vector2.zero;
        grt.offsetMax = UnityEngine.Vector2.zero;
        grt.localScale = new UnityEngine.Vector3(scale, scale, 1f);
        grt.SetAsFirstSibling(); // render behind the crisp outline + label

        // Spread faint Shadow copies in 8 directions at the radius for a soft, even falloff.
        var glowShadows = new System.Collections.Generic.List<UnityEngine.UI.Shadow>(GlowSamples);
        foreach (var s in glow.GetComponents<UnityEngine.UI.Shadow>())
        {
            if (s.GetType() == typeof(UnityEngine.UI.Shadow)) glowShadows.Add(s);
        }
        for (int i = 0; i < GlowSamples; i++)
        {
            float ang = (UnityEngine.Mathf.PI * 2f) * i / GlowSamples;
            var offset = new UnityEngine.Vector2(
                UnityEngine.Mathf.Cos(ang) * radius,
                UnityEngine.Mathf.Sin(ang) * radius);

            UnityEngine.UI.Shadow s = i < glowShadows.Count
                ? glowShadows[i]
                : glow.gameObject.AddComponent<UnityEngine.UI.Shadow>();
            s.enabled         = true;
            s.effectColor     = glowColor;
            s.effectDistance  = offset;
            s.useGraphicAlpha = true; // follow the ring's own shape for a clean halo edge
        }
    }
}
