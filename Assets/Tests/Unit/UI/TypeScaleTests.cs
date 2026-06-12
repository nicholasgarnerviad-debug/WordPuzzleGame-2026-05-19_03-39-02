using System.Collections.Generic;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using WordPuzzle.UI;
using WordPuzzle.UI.Components;

// ============================================================
//  Task 42 — Rungo typography + the role-based type scale.
//  Pins: (1) the four dynamic Rungo SDF assets build and are
//  distinct; (2) ★ ☆ ✓ ▸ · resolve through the symbols fallback
//  (zero tofu); (3) TypeScale.Apply sets the role's font, size,
//  style and default colour; (4) the screens that build their UI
//  in code produce ONLY theme-font texts (exception list: empty).
//  Full-scene reflection over every bootstrapped screen is the
//  Task 46 enforcement suite; this is the Task 42 smoke.
// ============================================================
public class TypeScaleTests
{
    private readonly List<GameObject> spawned = new List<GameObject>();

    private GameObject Spawn(string name)
    {
        var go = new GameObject(name, typeof(RectTransform));
        spawned.Add(go);
        return go;
    }

    [TearDown]
    public void TearDown()
    {
        foreach (var go in spawned)
            if (go != null) Object.DestroyImmediate(go);
        spawned.Clear();
    }

    private static TMP_FontAsset[] AllWeights() => new[]
        { ThemeFonts.Regular, ThemeFonts.Medium, ThemeFonts.SemiBold, ThemeFonts.Bold };

    // ── Font assets ──────────────────────────────────────────

    [Test]
    public void ThemeFonts_FourWeights_BuildAndAreDistinctDynamicAssets()
    {
        var fonts = AllWeights();
        foreach (var f in fonts)
            Assert.IsNotNull(f, "every Rungo weight must build from Resources/Fonts/Rungo");
        Assert.AreEqual(4, new HashSet<TMP_FontAsset>(fonts).Count,
            "the four weights must be four distinct assets (a missing TTF falls back to the TMP default)");
        foreach (var f in fonts)
        {
            Assert.AreEqual(AtlasPopulationMode.Dynamic, f.atlasPopulationMode,
                $"{f.name} must be a dynamic SDF asset (Task 42 spec)");
            Assert.IsTrue(ThemeFonts.IsThemeFont(f), $"{f.name} must self-identify as a theme font");
        }
    }

    [Test]
    public void ThemeFonts_SymbolGlyphs_ResolveThroughFallback_NoTofu()
    {
        const string symbols = "★☆✓▸·";
        foreach (var f in AllWeights())
        {
            bool ok = f.HasCharacters(symbols, out uint[] missing,
                searchFallbacks: true, tryAddCharacter: true);
            string missingList = missing == null ? "(none)" : string.Join(", ", missing);
            Assert.IsTrue(ok, $"{f.name} cannot render '{symbols}' — missing codepoints: {missingList}");
        }
    }

    [Test]
    public void Palette_TextMuted_IsTheRaisedAccessibleValue()
    {
        // Task 42 Phase 4 — #9A8FBE → #ABA0CE (WCAG AA at Caption sizes on Panel).
        Assert.IsTrue(ColorUtility.TryParseHtmlString("#ABA0CE", out var expected));
        Assert.AreEqual(expected.r, Palette.TextMuted.r, 1f / 255f);
        Assert.AreEqual(expected.g, Palette.TextMuted.g, 1f / 255f);
        Assert.AreEqual(expected.b, Palette.TextMuted.b, 1f / 255f);
    }

    // ── Role application ─────────────────────────────────────

    [Test]
    public void TypeScale_Apply_SetsRoleFontSizeStyleAndDefaultColor()
    {
        var go = Spawn("Text");
        var text = go.AddComponent<TextMeshProUGUI>();

        foreach (TypeRole role in System.Enum.GetValues(typeof(TypeRole)))
        {
            TypeScale.Apply(text, role);
            Assert.AreEqual(TypeScale.FontFor(role), text.font, $"{role}: wrong font asset");
            Assert.AreEqual(TypeScale.Size(role), text.fontSize, 0.01f, $"{role}: wrong size");
            Assert.AreEqual(FontStyles.Normal, text.fontStyle,
                $"{role}: weight must come from the asset, never the Bold flag");
            Assert.AreEqual(TypeScale.DefaultColor(role), text.color, $"{role}: wrong default colour");
        }

        Assert.AreEqual(Palette.TextMuted, TypeScale.DefaultColor(TypeRole.Caption));
        Assert.AreEqual(Palette.TextPrimary, TypeScale.DefaultColor(TypeRole.Body));
    }

    [Test]
    public void TypeScale_TileLetter_TracksTileSizeAndTextScale()
    {
        // Responsive: a 7-letter board's small tiles and a 3-letter board's large tiles both fit.
        Assert.AreEqual(TypeScale.TileLetterMinSize, TypeScale.TileLetterFontSize(0f), 0.01f);
        Assert.AreEqual(90f * TypeScale.TileLetterSizeRatio * AccessiblePalette.TextScale,
            TypeScale.TileLetterFontSize(90f), 0.01f);
        Assert.Greater(TypeScale.TileLetterFontSize(140f), TypeScale.TileLetterFontSize(90f));
    }

    // ── Screen smoke (code-built surfaces; exception list ships EMPTY) ──

    private static void AssertAllTextsThemed(GameObject root, int minTexts, string surface)
    {
        var texts = root.GetComponentsInChildren<TMP_Text>(true);
        Assert.GreaterOrEqual(texts.Length, minTexts,
            $"{surface}: expected at least {minTexts} built texts (vacuous-pass guard)");
        foreach (var t in texts)
            Assert.IsTrue(ThemeFonts.IsThemeFont(t.font),
                $"{surface}: '{GetPath(t.transform)}' uses '{(t.font != null ? t.font.name : "null")}' — every text must be a TypeScale role font");
    }

    private static string GetPath(Transform t)
    {
        string path = t.name;
        while (t.parent != null) { t = t.parent; path = t.name + "/" + path; }
        return path;
    }

    [Test]
    public void SettingsScreen_RuntimeBuild_UsesOnlyThemeFonts()
    {
        var go = Spawn("Settings");
        var screen = go.AddComponent<WordPuzzle.UI.SettingsScreen>();
        screen.Populate(null); // builds the full runtime layout (title, sections, sliders, toggles, buttons)
        AssertAllTextsThemed(go, 10, "SettingsScreen");
    }

    [Test]
    public void DailyRewardPopup_RuntimeBuild_UsesOnlyThemeFonts()
    {
        var go = Spawn("DailyRewards");
        var popup = go.AddComponent<WordPuzzle.UI.DailyRewardPopup>();
        popup.ShowRewards(
            loginAvailable: true, loginCoins: 50, loginDay: 3, claimLogin: grant => grant(50),
            repairAvailable: true, repairCost: 150, repairAffordable: true, adReady: false,
            requestRepair: (useAd, done) => done(true));
        AssertAllTextsThemed(go, 4, "DailyRewardPopup");
    }

    [Test]
    public void TutorialOverlay_WelcomeCard_UsesOnlyThemeFonts()
    {
        var go = Spawn("Tutorial");
        var overlay = go.AddComponent<WordPuzzle.UI.TutorialOverlay>();
        overlay.ShowWelcome(() => { }, () => { });
        AssertAllTextsThemed(go, 2, "TutorialOverlay welcome");
    }

    [Test]
    public void OnScreenKeyboard_Keys_AreLabelRoleThemeFonts()
    {
        var go = Spawn("Keyboard");
        go.AddComponent<OnScreenKeyboard>(); // Awake builds 26 letter keys + DEL + GO
        AssertAllTextsThemed(go, 28, "OnScreenKeyboard");
        foreach (var t in go.GetComponentsInChildren<TMP_Text>(true))
            Assert.AreEqual(TypeScale.LabelSize, t.fontSize, 0.01f, "key letters are the Label role size");
    }

    [Test]
    public void LetterTile_LetterAndStateGlyph_AreThemeFonts()
    {
        var go = Spawn("Tile");
        var tile = go.AddComponent<LetterTile>(); // Awake creates the letter + state-glyph labels
        tile.SetSize(120f);
        AssertAllTextsThemed(go, 2, "LetterTile");
        var letter = go.transform.Find("Letter");
        Assert.IsNotNull(letter, "LetterTile must create its Letter label");
        var letterText = letter.GetComponent<TMP_Text>();
        Assert.AreEqual(ThemeFonts.SemiBold, letterText.font, "tile letters carry the SemiBold weight");
        Assert.AreEqual(TypeScale.TileLetterFontSize(120f), letterText.fontSize, 0.01f,
            "tile letters stay responsive to the tile rect (Large Text keeps working)");
    }
}
