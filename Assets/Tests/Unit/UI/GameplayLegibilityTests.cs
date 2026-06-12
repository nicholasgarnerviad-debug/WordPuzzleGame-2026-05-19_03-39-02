using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using WordPuzzle.UI;
using WordPuzzle.UI.Components;

// ============================================================
//  Task 44 — gameplay legibility: scrim, keyboard tokens,
//  backdrop gating, safe area.
//  Pins: (1) ResolveBackdrop's full gate matrix (video only when
//  ungated; any gate ⇒ still; no still ⇒ flat); (2) the gameplay
//  scrim exists ONLY when gameplay requests its background —
//  three SurfaceVoid bands realizing 0.00→0.28→0.38, raycast-
//  transparent, idempotent; (3) keyboard fills stay tokened and
//  ≥0.88 alpha; (4) ApplyScreenBackground attaches exactly one
//  SafeAreaPanel and the panel maps Screen.safeArea onto anchors.
//  Visual verdicts (scrim weight, start-row white) are the human
//  Simulator gate.
// ============================================================
public class GameplayLegibilityTests
{
    private readonly List<GameObject> spawned = new List<GameObject>();

    [TearDown]
    public void TearDown()
    {
        foreach (var go in spawned)
            if (go != null) Object.DestroyImmediate(go);
        spawned.Clear();
    }

    private GameObject Spawn(string name)
    {
        var go = new GameObject(name, typeof(RectTransform));
        spawned.Add(go);
        return go;
    }

    private Canvas SpawnCanvas()
    {
        var go = new GameObject("TestCanvas", typeof(Canvas));
        spawned.Add(go);
        return go.GetComponent<Canvas>();
    }

    // ── Phase 4 — backdrop gating ────────────────────────────

    [Test]
    public void ResolveBackdrop_FullGateMatrix()
    {
        // Video wins only when present AND ungated.
        Assert.AreEqual(BackdropKind.Video, UIThemeManager.ResolveBackdrop(true, true, false, false));
        Assert.AreEqual(BackdropKind.Video, UIThemeManager.ResolveBackdrop(true, false, false, false));

        // Any gate (ReduceMotion or low power) falls back to the still.
        Assert.AreEqual(BackdropKind.Still, UIThemeManager.ResolveBackdrop(true, true, true, false));
        Assert.AreEqual(BackdropKind.Still, UIThemeManager.ResolveBackdrop(true, true, false, true));
        Assert.AreEqual(BackdropKind.Still, UIThemeManager.ResolveBackdrop(true, true, true, true));
        Assert.AreEqual(BackdropKind.Still, UIThemeManager.ResolveBackdrop(false, true, false, false));

        // No still to fall back to ⇒ flat SurfaceVoid.
        Assert.AreEqual(BackdropKind.Flat, UIThemeManager.ResolveBackdrop(true, false, true, false));
        Assert.AreEqual(BackdropKind.Flat, UIThemeManager.ResolveBackdrop(true, false, false, true));
        Assert.AreEqual(BackdropKind.Flat, UIThemeManager.ResolveBackdrop(false, false, false, false));
    }

    [Test]
    public void PowerModeSeam_NullAndStubNeverGate()
    {
        Assert.IsFalse(new NullPowerModeService().LowPowerActive, "the shipped stub must never gate");

        var prev = UIThemeManager.PowerMode;
        try
        {
            var stub = new NullPowerModeService();
            UIThemeManager.PowerMode = stub;
            Assert.AreSame(stub, UIThemeManager.PowerMode);
            UIThemeManager.PowerMode = null; // pre-boot / tests — must be a safe state
        }
        finally { UIThemeManager.PowerMode = prev; }
    }

    // ── Phase 2 — keyboard tokens ────────────────────────────

    [Test]
    public void Keyboard_FillsAreTokened_AndKeysOpaqueEnough()
    {
        Assert.AreEqual(Palette.Panel, KeyboardPalette.KeyFill, "keys carry the Panel token");
        Assert.GreaterOrEqual(KeyboardPalette.KeyFill.a, 0.88f,
            "key fill must guarantee letter contrast over the brightest backdrop frame");
        Assert.AreEqual(Palette.Alert,      KeyboardPalette.DelFill, "DEL = Alert token");
        Assert.AreEqual(Palette.AccentAqua, KeyboardPalette.GoFill,  "GO = AccentAqua (affirmative)");
        Assert.AreEqual(Palette.TextPrimary, KeyboardPalette.KeyText);
    }

    // ── Phase 1 — the gameplay scrim ─────────────────────────

    [Test]
    public void GameplayScrim_OnlyWhenGameplayRequests_AndIdempotent()
    {
        var canvas = SpawnCanvas();
        var gameplay = Spawn("GameplayRoot");
        gameplay.transform.SetParent(canvas.transform, false);
        var menu = Spawn("MenuRoot");
        menu.transform.SetParent(canvas.transform, false);

        UIThemeManager.ApplyScreenBackground(gameplay, gameplayScrim: true);

        var layer = canvas.transform.Find("BackgroundLayer");
        Assert.IsNotNull(layer, "shared background layer exists");
        var scrim = layer.Find("__GameplayScrim");
        Assert.IsNotNull(scrim, "gameplay request builds the scrim");
        Assert.IsTrue(scrim.gameObject.activeSelf);
        Assert.AreEqual(3, scrim.childCount, "three bands realize the 0.00→0.28→0.38 curve");

        // Every band: SurfaceVoid tint, raycast-transparent, gradient-driven.
        foreach (Transform band in scrim)
        {
            var img = band.GetComponent<Image>();
            Assert.IsNotNull(img);
            Assert.IsFalse(img.raycastTarget, $"{band.name} must never eat taps");
            Assert.IsNotNull(band.GetComponent<UIVerticalGradient>(), $"{band.name} uses Task 43's gradient");
        }

        var fade  = scrim.Find("__ScrimFade").GetComponent<UIVerticalGradient>();
        var board = scrim.Find("__ScrimBoard").GetComponent<UIVerticalGradient>();
        var keys  = scrim.Find("__ScrimKeys").GetComponent<UIVerticalGradient>();
        Assert.AreEqual(0f,                 fade.Top.a,     1e-4f);
        Assert.AreEqual(Scrim.BoardAlpha,   fade.Bottom.a,  1e-4f);
        Assert.AreEqual(Scrim.BoardAlpha,   board.Top.a,    1e-4f);
        Assert.AreEqual(Scrim.BoardAlpha,   board.Bottom.a, 1e-4f);
        Assert.AreEqual(Scrim.BoardAlpha,   keys.Top.a,     1e-4f);
        Assert.AreEqual(Scrim.KeyboardAlpha, keys.Bottom.a, 1e-4f);
        var tint = Scrim.Tint;
        Assert.AreEqual(tint.r, keys.Bottom.r, 1e-4f, "scrim tint is SurfaceVoid");

        // Zone geometry comes from the constants, not magic numbers.
        var keysRt = (RectTransform)keys.transform;
        Assert.AreEqual(Scrim.KeyboardZoneFraction, keysRt.anchorMax.y, 1e-4f);
        var fadeRt = (RectTransform)fade.transform;
        Assert.AreEqual(1f - Scrim.TopClearFraction, fadeRt.anchorMax.y, 1e-4f,
            "the top clear zone stays scrim-free");

        // A non-gameplay screen's request turns the scrim off (next transition semantics)…
        UIThemeManager.ApplyScreenBackground(menu);
        Assert.IsFalse(scrim.gameObject.activeSelf, "menu request deactivates the scrim");

        // …and re-requesting gameplay reuses the SAME scrim (no stacking).
        UIThemeManager.ApplyScreenBackground(gameplay, gameplayScrim: true);
        Assert.IsTrue(scrim.gameObject.activeSelf);
        int scrimCount = 0;
        foreach (Transform child in layer)
            if (child.name == "__GameplayScrim") scrimCount++;
        Assert.AreEqual(1, scrimCount, "re-apply must reuse, not stack");
    }

    // ── Phase 5 — safe area ──────────────────────────────────

    [Test]
    public void ApplyScreenBackground_AttachesExactlyOneSafeAreaPanel()
    {
        var canvas = SpawnCanvas();
        var root = Spawn("ScreenRoot");
        root.transform.SetParent(canvas.transform, false);

        UIThemeManager.ApplyScreenBackground(root);
        UIThemeManager.ApplyScreenBackground(root); // idempotent

        Assert.AreEqual(1, root.GetComponents<SafeAreaPanel>().Length);
    }

    [Test]
    public void SafeAreaPanel_MapsScreenSafeAreaOntoAnchors()
    {
        var canvas = SpawnCanvas();
        var root = Spawn("SafeRoot");
        root.transform.SetParent(canvas.transform, false);
        root.AddComponent<SafeAreaPanel>(); // OnEnable applies immediately

        var rt = (RectTransform)root.transform;
        var safe = Screen.safeArea;
        Assert.AreEqual(safe.xMin / Screen.width,  rt.anchorMin.x, 1e-4f);
        Assert.AreEqual(safe.yMin / Screen.height, rt.anchorMin.y, 1e-4f);
        Assert.AreEqual(safe.xMax / Screen.width,  rt.anchorMax.x, 1e-4f);
        Assert.AreEqual(safe.yMax / Screen.height, rt.anchorMax.y, 1e-4f);
        Assert.AreEqual(Vector2.zero, rt.offsetMin);
        Assert.AreEqual(Vector2.zero, rt.offsetMax);
    }
}
