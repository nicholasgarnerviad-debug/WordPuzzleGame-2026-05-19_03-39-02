using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

// ============================================================
//  Task 42 — Rungo typography + the role-based type scale.
//  ONE source of truth for every font asset and font size in the
//  app: screens call TypeScale.Apply(text, role) (or the
//  UITheme.ApplyType forwarder) and never set a raw fontSize or
//  font asset again. Sibling file to UITheme.cs (kept <500 lines).
// ============================================================
namespace WordPuzzle.UI
{
    /// <summary>
    /// Typography roles. Sizes are 1080×1920 canvas units; weight is carried by the
    /// FONT ASSET (Rungo Regular/Medium/SemiBold/Bold), never by the TMP Bold flag.
    /// </summary>
    public enum TypeRole
    {
        /// <summary>Bold 96 — the STAR LADDER masthead (until the logotype sprite lands).</summary>
        Display,
        /// <summary>Bold 64 — screen titles (RESULTS, STATS, SETTINGS, SHOP, tier-select header).</summary>
        Headline,
        /// <summary>SemiBold 44 — card headers, tier-row names, shop section headers, the Daily grade word.</summary>
        Title,
        /// <summary>SemiBold 38 — every button label and keyboard key letter.</summary>
        Label,
        /// <summary>SemiBold 56 nominal — gameplay tile letters (sized responsively via ApplyTileLetter).</summary>
        TileLetter,
        /// <summary>Medium 32 — body copy, settings row labels, shop item descriptions.</summary>
        Body,
        /// <summary>Regular 26 — stat captions, subtitles, "Par N · Mistakes left M", footers, build version.</summary>
        Caption
    }

    /// <summary>
    /// The four Rungo TMP font assets (Poppins-derived, OFL 1.1 — see Assets/Fonts/Rungo/OFL.txt),
    /// built ONCE as dynamic SDF assets from Resources/Fonts/Rungo TTFs, each with the OFL symbols
    /// fallback (Resources/Fonts/Symbols.ttf — ★ ☆ ✓ ▸ ·) appended so no glyph ever tofus.
    /// Dynamic atlases are fine for a word game's glyph set; baking static atlases in-Editor before
    /// the production build is a logged §13 tech-debt note. Main Unity thread only.
    /// </summary>
    public static class ThemeFonts
    {
        // Dynamic-SDF build parameters (Task 42 spec).
        private const int SamplingPointSize = 90;
        private const int AtlasPadding      = 9;
        private const int AtlasSize         = 1024;

        private static TMP_FontAsset _regular, _medium, _semiBold, _bold, _symbols;

        public static TMP_FontAsset Regular  => GetOrBuild(ref _regular,  "Fonts/Rungo/Rungo-Regular");
        public static TMP_FontAsset Medium   => GetOrBuild(ref _medium,   "Fonts/Rungo/Rungo-Medium");
        public static TMP_FontAsset SemiBold => GetOrBuild(ref _semiBold, "Fonts/Rungo/Rungo-SemiBold");
        public static TMP_FontAsset Bold     => GetOrBuild(ref _bold,     "Fonts/Rungo/Rungo-Bold");

        /// <summary>True iff <paramref name="font"/> is one of the four theme weights (smoke-test seam).</summary>
        public static bool IsThemeFont(TMP_FontAsset font)
            => font != null && (font == _regular || font == _medium || font == _semiBold || font == _bold);

        private static TMP_FontAsset GetOrBuild(ref TMP_FontAsset cache, string resourcePath)
        {
            // Unity-aware null check (not ??=): a play-mode exit destroys runtime-created
            // assets, leaving a fake-null reference that must rebuild on next access.
            if (cache == null) cache = Build(resourcePath, withSymbolsFallback: true);
            return cache;
        }

        private static TMP_FontAsset Symbols
        {
            get
            {
                if (_symbols == null) _symbols = Build("Fonts/Symbols", withSymbolsFallback: false);
                return _symbols;
            }
        }

        private static TMP_FontAsset Build(string resourcePath, bool withSymbolsFallback)
        {
            var font = Resources.Load<Font>(resourcePath);
            if (font == null)
            {
                // Never strand text fontless — fall back to the TMP default so the app still
                // renders; the type-scale smoke test fails loudly on IsThemeFont instead.
                Logger.LogError($"ThemeFonts: missing font resource '{resourcePath}' — falling back to the TMP default.");
                return TMP_Settings.defaultFontAsset;
            }

            var asset = TMP_FontAsset.CreateFontAsset(
                font, SamplingPointSize, AtlasPadding, GlyphRenderMode.SDFAA,
                AtlasSize, AtlasSize, AtlasPopulationMode.Dynamic);
            asset.name = System.IO.Path.GetFileName(resourcePath) + " SDF (runtime)";

            if (withSymbolsFallback)
            {
                var symbols = Symbols;
                if (symbols != null && symbols != asset)
                {
                    if (asset.fallbackFontAssetTable == null)
                        asset.fallbackFontAssetTable = new List<TMP_FontAsset>();
                    asset.fallbackFontAssetTable.Add(symbols);
                }
            }
            return asset;
        }
    }

    /// <summary>
    /// The role-based type scale (Task 42). <see cref="Apply"/> is the ONE application seam:
    /// it sets font, fontSize, fontStyle and the role's default colour
    /// (TextPrimary, Caption → TextMuted). Callers may override the COLOUR after the call
    /// with a Palette token only — never the font or size.
    /// </summary>
    public static class TypeScale
    {
        public const float DisplaySize    = 96f;
        public const float HeadlineSize   = 64f;
        public const float TitleSize      = 44f;
        public const float LabelSize      = 38f;
        public const float TileLetterSize = 56f;
        public const float BodySize       = 32f;
        public const float CaptionSize    = 26f;

        // Gameplay-tile responsive sizing (the ONLY size math outside the table — kept here so
        // LetterTile carries no font constants): letters track the tile rect so 3- and 7-letter
        // boards both fit, scaled by the accessibility TextScale, floored for legibility.
        public const float TileLetterSizeRatio = 0.55f;
        public const float TileLetterMinSize   = 12f;
        // Tile state glyph (the colorblind ✓/✕ cue in the tile corner) — proportional to the tile.
        public const float TileStateGlyphRatio = 0.16f;
        public const float TileStateGlyphMinSize = 14f;

        /// <summary>Responsive tile-letter size — pure, so layout passes can resize without re-styling.</summary>
        public static float TileLetterFontSize(float tilePixelSize)
            => Mathf.Max(TileLetterMinSize, tilePixelSize * TileLetterSizeRatio * AccessiblePalette.TextScale);

        /// <summary>Responsive tile state-glyph size — pure.</summary>
        public static float TileStateGlyphFontSize(float tilePixelSize)
            => Mathf.Max(TileStateGlyphMinSize, tilePixelSize * TileStateGlyphRatio);

        public static float Size(TypeRole role)
        {
            switch (role)
            {
                case TypeRole.Display:    return DisplaySize;
                case TypeRole.Headline:   return HeadlineSize;
                case TypeRole.Title:      return TitleSize;
                case TypeRole.Label:      return LabelSize;
                case TypeRole.TileLetter: return TileLetterSize;
                case TypeRole.Caption:    return CaptionSize;
                default:                  return BodySize;
            }
        }

        public static TMP_FontAsset FontFor(TypeRole role)
        {
            switch (role)
            {
                case TypeRole.Display:
                case TypeRole.Headline:   return ThemeFonts.Bold;
                case TypeRole.Title:
                case TypeRole.Label:
                case TypeRole.TileLetter: return ThemeFonts.SemiBold;
                case TypeRole.Caption:    return ThemeFonts.Regular;
                default:                  return ThemeFonts.Medium;
            }
        }

        public static Color DefaultColor(TypeRole role)
            => role == TypeRole.Caption ? Palette.TextMuted : Palette.TextPrimary;

        /// <summary>
        /// Apply a type role: font asset (weight), size, style and default colour. Weight comes
        /// from the asset, so the TMP Bold flag is cleared — emphasis is a ROLE choice, not a flag.
        /// </summary>
        public static void Apply(TMP_Text text, TypeRole role)
        {
            if (text == null) return;
            text.font      = FontFor(role);
            text.fontSize  = Size(role);
            text.fontStyle = FontStyles.Normal;
            text.color     = DefaultColor(role);
        }

        /// <summary>
        /// Gameplay tile letter: TileLetter role weight/colour with the responsive size —
        /// tracks the tile rect and the accessibility TextScale (Large Text keeps working).
        /// </summary>
        public static void ApplyTileLetter(TMP_Text text, float tilePixelSize)
        {
            if (text == null) return;
            Apply(text, TypeRole.TileLetter);
            text.fontSize = TileLetterFontSize(tilePixelSize);
        }

        /// <summary>Tile corner state glyph (✓/✕ colorblind cue) — proportional to the tile rect.</summary>
        public static void ApplyTileStateGlyph(TMP_Text text, float tilePixelSize)
        {
            if (text == null) return;
            Apply(text, TypeRole.Caption);
            text.fontSize = TileStateGlyphFontSize(tilePixelSize);
        }
    }
}
