using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WordPuzzle.UI;
using WordPuzzle.UI.Components;

// ============================================================
//  Task 46 — design-system ENFORCEMENT. Encodes Tasks 42–45 as
//  mechanical checks so no future swarm task can regress them:
//  (1) every active text on every runtime-built surface is a
//  TypeScale-role font; (2) every active Graphic resolves to a
//  Palette token (alpha-modulation allowed; scan rules below);
//  (3) every screen content root is safe-area'd; (4) exactly ONE
//  filled-hero on the menu, ghosts carry no glow; (5) every
//  button hit target ≥ 88×88 (Apple HIG 44pt @2×); (6) a source
//  lint: no raw hex / Color32 / fontSize outside the token files.
//  EXCEPTION REGISTRIES SHIP EMPTY — a future exception needs a
//  reviewed entry here with a comment. If a UI change fails this
//  suite, fix the change, not the test.
//
//  Colour-scan rules (documented, principled):
//   • alpha ≤ 1/255 ⇒ invisible, passes (the ghost hit targets).
//   • a Graphic carrying UIVerticalGradient is judged by the
//     gradient's Top/Bottom RGB (its base stays white by design).
//   • an Image whose art carries identity (the logotype, the
//     BackgroundLayer still) may be white — ONLY those two.
//   • RawImage is excluded only for the video backdrop surface.
//   • TMP submeshes (fallback glyph carriers) are vertex-driven.
// ============================================================
public class DesignSystemTests
{
    private readonly List<GameObject> spawned = new List<GameObject>();
    private bool savedReduceMotion;

    [SetUp]
    public void SetUp()
    {
        savedReduceMotion = UIAnimations.ReduceMotion;
        UIAnimations.ReduceMotion = true; // surfaces render at rest — scans see end-states
    }

    [TearDown]
    public void TearDown()
    {
        UIAnimations.ReduceMotion = savedReduceMotion;
        foreach (var go in spawned)
            if (go != null) Object.DestroyImmediate(go);
        spawned.Clear();
    }

    // ───────────────────────── the surface harness ─────────────────────────

    private Canvas SpawnCanvas()
    {
        var go = new GameObject("Canvas", typeof(Canvas));
        spawned.Add(go);
        return go.GetComponent<Canvas>();
    }

    private GameObject Spawn(Transform parent, string name)
    {
        var go = new GameObject(name, typeof(RectTransform));
        spawned.Add(go);
        go.transform.SetParent(parent, false);
        return go;
    }

    /// <summary>Every runtime-buildable surface, built the way GameBootstrap drives them.</summary>
    private List<(string name, GameObject root)> BuildAllSurfaces(Transform canvas)
    {
        var surfaces = new List<(string, GameObject)>();

        var settings = Spawn(canvas, "Settings");
        settings.AddComponent<SettingsScreen>().Populate(null);
        surfaces.Add(("SettingsScreen", settings));

        var rewards = Spawn(canvas, "DailyRewards");
        rewards.AddComponent<DailyRewardPopup>().ShowRewards(
            loginAvailable: true, loginCoins: 50, loginDay: 3, claimLogin: grant => grant(50),
            repairAvailable: true, repairCost: 150, repairAffordable: true, adReady: false,
            requestRepair: (useAd, done) => done(true));
        surfaces.Add(("DailyRewardPopup", rewards));

        var tutorial = Spawn(canvas, "Tutorial");
        tutorial.AddComponent<TutorialOverlay>().ShowWelcome(() => { }, () => { });
        surfaces.Add(("TutorialOverlay", tutorial));

        var keyboard = Spawn(canvas, "Keyboard");
        keyboard.AddComponent<OnScreenKeyboard>();
        surfaces.Add(("OnScreenKeyboard", keyboard));

        var results = Spawn(canvas, "Results");
        var resultsScreen = results.AddComponent<ResultsScreen>();
        resultsScreen.ConfigureForDaily();
        resultsScreen.ShowDailyResult(2, 4, 5, false, 10, 3, usedPowerUp: false, animate: false);
        resultsScreen.ShowDailyCoinReward(30);
        resultsScreen.ShowDailyStreak(3, 5, false);
        surfaces.Add(("ResultsScreen", results));

        var stats = Spawn(canvas, "Stats");
        stats.AddComponent<StatsScreen>().Populate(null, null);
        surfaces.Add(("StatsScreen", stats));

        var library = Spawn(canvas, "Library");
        library.AddComponent<PuzzleLibraryScreen>();
        surfaces.Add(("PuzzleLibraryScreen", library));

        var taSetup = Spawn(canvas, "TimeAttackSetup");
        taSetup.AddComponent<TimeAttackSetupScreen>();
        surfaces.Add(("TimeAttackSetupScreen", taSetup));

        var shop = Spawn(canvas, "Shop");
        shop.AddComponent<ShopScreen>().Open();
        surfaces.Add(("ShopScreen", shop));

        surfaces.Add(("MainMenuScreen", BuildMainMenu(canvas)));

        // Drive layout so geometry reads REAL rects: nested LayoutGroups (settings sections,
        // shop rows) need an explicit immediate rebuild — one ForceUpdateCanvases isn't enough
        // in a single headless frame.
        Canvas.ForceUpdateCanvases();
        foreach (var (_, root) in surfaces)
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)root.transform);
        Canvas.ForceUpdateCanvases();
        return surfaces;
    }

    // The menu's buttons are scene refs — inject synthetic ones (reflection) before OnEnable
    // styles them, so the REAL ApplyMenuPolish path runs headlessly.
    private static readonly string[] MenuButtonFields =
    {
        "resumeButton", "dailyButton", "classicModeButton", "puzzleShowButton",
        "timeAttackButton", "libraryButton", "statsButton", "settingsButton",
    };

    private GameObject BuildMainMenu(Transform canvas)
    {
        var go = Spawn(canvas, "MainMenu");
        go.SetActive(false); // defer OnEnable until the refs exist
        var screen = go.AddComponent<MainMenuScreen>();
        foreach (var fieldName in MenuButtonFields)
        {
            var field = typeof(MainMenuScreen).GetField(fieldName,
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(field, $"MainMenuScreen.{fieldName} no longer exists — update the harness");
            field.SetValue(screen, MakeMenuButton(go.transform, fieldName));
        }
        go.SetActive(true); // OnEnable → ApplyMenuPolish styles the tiers
        return go;
    }

    private Button MakeMenuButton(Transform parent, string name)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        ((RectTransform)go.transform).sizeDelta = new Vector2(900f, 130f);
        var labelGo = new GameObject("Label", typeof(RectTransform));
        labelGo.transform.SetParent(go.transform, false);
        var label = labelGo.AddComponent<TextMeshProUGUI>();
        label.text = name;
        return go.GetComponent<Button>();
    }

    private static string GetPath(Transform t)
    {
        string path = t.name;
        while (t.parent != null) { t = t.parent; path = t.name + "/" + path; }
        return path;
    }

    // ───────────────────────── 1 · fonts ─────────────────────────

    [Test]
    public void EveryActiveText_OnEveryBuiltSurface_IsATypeScaleFont()
    {
        var canvas = SpawnCanvas();
        int scanned = 0;
        var violations = new List<string>();
        foreach (var (name, root) in BuildAllSurfaces(canvas.transform))
        {
            foreach (var t in root.GetComponentsInChildren<TMP_Text>(false))
            {
                scanned++;
                if (!ThemeFonts.IsThemeFont(t.font))
                    violations.Add($"{name}: {GetPath(t.transform)} → '{(t.font ? t.font.name : "null")}'");
            }
        }
        Assert.GreaterOrEqual(scanned, 60, "vacuous-pass guard — the sweep must see real texts");
        Assert.IsEmpty(violations, "off-scale fonts:\n" + string.Join("\n", violations));
    }

    // ───────────────────────── 2 · colours ─────────────────────────

    private static bool GraphicPasses(Graphic g)
    {
        if (g is TMP_SubMeshUI) return true;                  // fallback glyph carrier — vertex-driven
        if (g.color.a <= 1f / 255f) return true;              // invisible (ghost hit targets)

        var grad = g.GetComponent<UIVerticalGradient>();
        if (grad != null)                                      // hero fills + the gameplay scrim bands
            return Palette.IsToken(grad.Top) && Palette.IsToken(grad.Bottom);

        bool isWhite = Palette.IsToken(g.color) == false
            && Mathf.Approximately(g.color.r, 1f) && Mathf.Approximately(g.color.g, 1f)
            && Mathf.Approximately(g.color.b, 1f);
        if (isWhite)
        {
            // The ONLY sanctioned white-multiply surfaces: art that carries its own colour.
            if (g is RawImage && g.name == "VideoSurface") return true;       // the video backdrop
            if (g.transform.name == "LogotypeImage") return true;             // gradient baked into the art
            // The backdrop stills (shared layer AND the shop overlay root) — identified by the
            // ART, not the hierarchy: only the Resources/UI background sprites may be white-lit.
            if (g is Image still && still.sprite != null && still.sprite.name.Contains("Background"))
                return true;
            return false;
        }
        return Palette.IsToken(g.color);
    }

    [Test]
    public void EveryActiveGraphic_OnEveryBuiltSurface_ResolvesToAToken()
    {
        var canvas = SpawnCanvas();
        int scanned = 0;
        var violations = new List<string>();
        foreach (var (name, root) in BuildAllSurfaces(canvas.transform))
        {
            foreach (var g in root.GetComponentsInChildren<Graphic>(false))
            {
                scanned++;
                if (!GraphicPasses(g))
                    violations.Add($"{name}: {GetPath(g.transform)} ({g.GetType().Name}) → {g.color}");
            }
        }
        Assert.GreaterOrEqual(scanned, 80, "vacuous-pass guard — the sweep must see real graphics");
        Assert.IsEmpty(violations, "off-token colours:\n" + string.Join("\n", violations));
    }

    // ───────────────────────── 3 · safe area ─────────────────────────

    [Test]
    public void EveryScreenContentRoot_CarriesSafeAreaPanel()
    {
        var canvas = SpawnCanvas();
        // Overlays (DailyRewardPopup, TutorialOverlay) ride above an already safe-area'd screen;
        // the keyboard lives inside the gameplay root. The SCREENS are the safe-area surfaces.
        var overlayNames = new HashSet<string> { "DailyRewardPopup", "TutorialOverlay", "OnScreenKeyboard" };
        var violations = new List<string>();
        foreach (var (name, root) in BuildAllSurfaces(canvas.transform))
        {
            if (overlayNames.Contains(name)) continue;
            bool onRoot = root.GetComponent<SafeAreaPanel>() != null;
            var safeChild = root.transform.Find("__SafeContent"); // the overlay-idiom screens (Shop)
            bool onContent = safeChild != null && safeChild.GetComponent<SafeAreaPanel>() != null;
            if (!onRoot && !onContent)
                violations.Add(name);
        }
        Assert.IsEmpty(violations, "screens without a SafeAreaPanel: " + string.Join(", ", violations));
    }

    // ───────────────────────── 4 · button hierarchy ─────────────────────────

    private static bool IsFilledHero(Button b)
    {
        var img = b.GetComponent<Image>();
        return img != null
            && b.GetComponent<UIVerticalGradient>() != null
            && img.sprite == UIThemeManager.RoundedButtonSprite;
    }

    [Test]
    public void MainMenu_HasExactlyOneFilledHero_AndGhostsCarryNoGlow()
    {
        var canvas = SpawnCanvas();
        var menu = BuildMainMenu(canvas.transform);

        var heroes = new List<string>();
        foreach (var b in menu.GetComponentsInChildren<Button>(true))
            if (IsFilledHero(b)) heroes.Add(b.name);
        Assert.AreEqual(1, heroes.Count,
            $"exactly ONE filled hero answers 'where do I tap' (found: {string.Join(", ", heroes)})");
        Assert.AreEqual("dailyButton", heroes[0], "DAILY is the hero");

        // Tier 3 — Library/Stats (and the deactivated Settings) recede: no ring sprite, no glow.
        foreach (var ghostName in new[] { "libraryButton", "statsButton" })
        {
            var ghost = menu.transform.Find(ghostName);
            Assert.IsNotNull(ghost, $"{ghostName} missing from the harness menu");
            var img = ghost.GetComponent<Image>();
            Assert.IsNull(img.sprite, $"{ghostName}: a ghost renders NO chrome");
            Assert.AreEqual(0f, img.color.a, $"{ghostName}: ghost image must be invisible");
            Assert.IsTrue(img.raycastTarget, $"{ghostName}: the full rect stays tappable");
            Assert.AreEqual(0, ghost.GetComponentsInChildren<Shadow>(true).Length,
                $"{ghostName}: ghosts carry no glow Shadow components");
        }
    }

    // ───────────────────────── 5 · hit-target geometry ─────────────────────────

    [Test]
    public void EveryActiveButton_MeetsTheMinimumHitTarget()
    {
        const float MinSide = 88f - 0.01f; // Apple HIG 44pt @2× (the keyboard keys sit exactly at 88)
        var canvas = SpawnCanvas();
        var violations = new List<string>();
        int scanned = 0;
        foreach (var (name, root) in BuildAllSurfaces(canvas.transform))
        {
            foreach (var b in root.GetComponentsInChildren<Button>(false))
            {
                scanned++;
                var rt = (RectTransform)b.transform;
                var r = rt.rect;
                // A dimension that FILLS its container (flexible in a LayoutGroup, or stretch
                // anchors, or ScrollRect-driven content) reads 0 in a single headless frame —
                // that's a harness artifact, not a hit-target fact. Enforce the dimension the
                // author actually fixed; a fill-driven axis is as wide as the device makes it.
                var le = b.GetComponent<LayoutElement>();
                bool widthFillDriven = r.width <= 0.01f
                    && ((le != null && le.flexibleWidth > 0f)
                        || !Mathf.Approximately(rt.anchorMin.x, rt.anchorMax.x)
                        || b.GetComponentInParent<ScrollRect>() != null);
                bool widthBad  = r.width  < MinSide && !widthFillDriven;
                bool heightBad = r.height < MinSide;
                if (widthBad || heightBad)
                    violations.Add($"{name}: {GetPath(b.transform)} → {r.width:F0}×{r.height:F0}");
            }
        }
        Assert.GreaterOrEqual(scanned, 30, "vacuous-pass guard — the sweep must see real buttons");
        Assert.IsEmpty(violations, "hit targets under 88×88:\n" + string.Join("\n", violations));
    }

    // ───────────────────────── 6 · source lint (the straggler-catcher) ─────────────────────────

    // The token files own the raw values; everything else must reference them.
    private static readonly HashSet<string> LintExemptFiles = new HashSet<string>
    {
        "UITheme.cs",      // Palette / AccessiblePalette — the ONLY raw theme hex
        "UITypeScale.cs",  // TypeScale — the ONLY raw font sizes
    };

    [Test]
    public void UISources_NoRawColorsOrFontSizes_OutsideTheTokenFiles()
    {
        string uiRoot = Path.Combine(Application.dataPath, "Scripts/UI");
        Assert.IsTrue(Directory.Exists(uiRoot), $"UI source root missing: {uiRoot}");

        var rawColor = new Regex(@"new Color32\s*\(|new Color\s*\(\s*0x|""#[0-9A-Fa-f]{6}|<color=#[0-9A-Fa-f]");
        var rawFontSize = new Regex(@"\.fontSize\s*=");
        var violations = new List<string>();
        int files = 0;

        foreach (var file in Directory.GetFiles(uiRoot, "*.cs", SearchOption.AllDirectories))
        {
            if (LintExemptFiles.Contains(Path.GetFileName(file))) continue;
            files++;
            var lines = File.ReadAllLines(file);
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                if (rawColor.IsMatch(line))
                    violations.Add($"{Path.GetFileName(file)}:{i + 1}: {line.Trim()}");
                // fontSize assignments are sanctioned ONLY when the value comes from TypeScale.
                if (rawFontSize.IsMatch(line) && !line.Contains("TypeScale."))
                    violations.Add($"{Path.GetFileName(file)}:{i + 1}: {line.Trim()}");
            }
        }

        Assert.GreaterOrEqual(files, 20, "vacuous-pass guard — the lint must see the UI assembly");
        Assert.IsEmpty(violations,
            "raw colour/size literals (route through UITheme.Palette / TypeScale):\n"
            + string.Join("\n", violations));
    }

    // ───────────────────────── registries ─────────────────────────

    [Test]
    public void TokenRegistries_AreComplete_AndIsTokenBehaves()
    {
        Assert.GreaterOrEqual(Palette.All.Count, 15, "every declared Palette token registers");
        Assert.IsTrue(Palette.All.ContainsKey("SurfaceVoid"));
        Assert.IsTrue(Palette.All.ContainsKey("ModeDaily"));

        Assert.IsTrue(Palette.IsToken(Palette.AccentAqua));
        var faded = Palette.Coins; faded.a = 0.35f;
        Assert.IsTrue(Palette.IsToken(faded), "tokens at reduced alpha still match (ghost identity)");
        Assert.IsFalse(Palette.IsToken(new Color(0.1f, 0.9f, 0.1f)), "off-palette green is rejected");
        foreach (var cb in AccessiblePalette.ColorblindTokens)
            Assert.IsTrue(Palette.IsToken(cb), "colorblind hues are tokens");

        Assert.AreEqual(System.Enum.GetValues(typeof(TypeRole)).Length, TypeScale.All.Count,
            "every role registers with its font + size");
        foreach (var kv in TypeScale.All)
        {
            Assert.IsNotNull(kv.Value.font, $"{kv.Key}: registry font missing");
            Assert.Greater(kv.Value.size, 0f, $"{kv.Key}: registry size missing");
        }
    }
}
