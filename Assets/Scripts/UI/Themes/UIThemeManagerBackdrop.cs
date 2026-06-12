using UnityEngine;
using UnityEngine.UI;
using WordPuzzle.UI;
using WordPuzzle.UI.Components;

namespace WordPuzzle.UI
{
    /// <summary>
    /// Task 44 — gameplay readability-scrim constants (token-style, like <see cref="KeyboardPalette"/>).
    /// The scrim is a static SurfaceVoid vertical gradient over the shared backdrop, UNDER screen
    /// content: clear across the top <see cref="TopClearFraction"/> (the backdrop spectacle survives),
    /// ramping to <see cref="BoardAlpha"/> across <see cref="FadeBandFraction"/>, flat over the board,
    /// then deepening to <see cref="KeyboardAlpha"/> across the bottom <see cref="KeyboardZoneFraction"/>
    /// so worst-case video frames can never drop tile/key contrast below readable.
    /// </summary>
    public static class Scrim
    {
        public const float TopClearFraction     = 0.15f; // top 15% — scrim-free
        public const float FadeBandFraction     = 0.15f; // 0 → BoardAlpha ramp directly below the clear zone
        public const float KeyboardZoneFraction = 0.20f; // bottom 20% — the keyboard sits here
        public const float BoardAlpha           = 0.28f;
        public const float KeyboardAlpha        = 0.38f;

        /// <summary>Scrim tint — always the app base, alpha-modulated per zone.</summary>
        public static Color Tint => Palette.SurfaceVoid;
    }

    /// <summary>
    /// Task 44 — HUD layout constants that became safe-area-RELATIVE once <see cref="Components.SafeAreaPanel"/>
    /// took over notch clearance (the old hand-tuned 130px physical-top offsets are retired).
    /// </summary>
    public static class HudLayout
    {
        /// <summary>Gameplay header padding below the SAFE-AREA top edge (not the physical screen top).</summary>
        public const float HeaderTopPadding = 32f;

        // ── Task 47 — the composed gameplay header + the centred board zone ──
        /// <summary>The steps/par subtitle's top, measured below <see cref="HeaderTopPadding"/>.</summary>
        public const float HeaderSubtitleDrop = 64f;
        /// <summary>Extra drop when Puzzle Show's tier line already owns the first subtitle slot.</summary>
        public const float HeaderSubtitleSpacing = 44f;
        /// <summary>Total header band reserved above the board (pad + score + subtitle rows).</summary>
        public const float GameplayHeaderBlock = 176f;
        /// <summary>Breathing room between the board block and the zone edges.</summary>
        public const float BoardZoneMargin = 24f;
    }
}

/// <summary>Which backdrop the background layer should mount (Task 44 — gating is testable math).</summary>
public enum BackdropKind { Video, Still, Flat }

// ============================================================
//  Task 44 — backdrop gating + the gameplay scrim.
//  Partial of UIThemeManager so the seams stay flat next to
//  ApplyScreenBackground / EnsureBackgroundLayer (UITheme.cs).
// ============================================================
public static partial class UIThemeManager
{
    private const string GameplayScrimName = "__GameplayScrim";

    /// <summary>
    /// Task 44 — power-mode seam (set by GameBootstrap, mirroring the ad/store stubs).
    /// Null (tests, pre-boot) reads as "not low power" so the backdrop never over-gates.
    /// </summary>
    public static IPowerModeService PowerMode { get; set; }

    private static bool LowPowerGated => PowerMode != null && PowerMode.LowPowerActive;

    /// <summary>
    /// Pure resolution priority for the shared backdrop: the looping video wins only when present
    /// AND ungated (ReduceMotion off, not low-power); any gate falls back to the still; no still ⇒
    /// flat SurfaceVoid. Re-evaluated on every screen transition (EnsureBackgroundLayer), which is
    /// how a mid-session setting change swaps the layer — no hot-swap mid-frame.
    /// </summary>
    public static BackdropKind ResolveBackdrop(bool hasVideo, bool hasStill, bool reduceMotion, bool lowPower)
    {
        if (hasVideo && !reduceMotion && !lowPower) return BackdropKind.Video;
        if (hasStill) return BackdropKind.Still;
        return BackdropKind.Flat;
    }

    /// <summary>
    /// Toggle the gameplay readability scrim on the shared BackgroundLayer: built once (three
    /// gradient bands realizing the 0.00 / 0.28 / 0.38 curve in <see cref="Scrim"/>), then just
    /// (de)activated. Lives INSIDE the canvas-level layer — between the backdrop RawImage and every
    /// screen's content, full-bleed outside the safe panel — and is static (no animation) and
    /// raycast-transparent. Enabled only when the gameplay screen requests its background; an
    /// overlay above gameplay (e.g. the tutorial) never re-requests, so the scrim persists under it.
    /// </summary>
    private static void SetGameplayScrim(Transform underCanvas, bool enabled)
    {
        if (underCanvas == null) return;
        var canvas = underCanvas.GetComponentInParent<Canvas>();
        if (canvas == null) return;
        var layer = canvas.rootCanvas.transform.Find(BackgroundLayerName);
        if (layer == null) return;

        var scrim = layer.Find(GameplayScrimName);
        if (!enabled)
        {
            if (scrim != null) scrim.gameObject.SetActive(false);
            return;
        }

        if (scrim == null)
        {
            var go = new GameObject(GameplayScrimName, typeof(RectTransform));
            go.transform.SetParent(layer, false);
            scrim = go.transform;
            var rt = (RectTransform)scrim;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            float fadeTop  = 1f - Scrim.TopClearFraction;            // clear above this line
            float boardTop = fadeTop - Scrim.FadeBandFraction;        // flat board band starts here
            BuildScrimBand(scrim, "__ScrimFade",  boardTop, fadeTop,
                topAlpha: 0f,               bottomAlpha: Scrim.BoardAlpha);
            BuildScrimBand(scrim, "__ScrimBoard", Scrim.KeyboardZoneFraction, boardTop,
                topAlpha: Scrim.BoardAlpha, bottomAlpha: Scrim.BoardAlpha);
            BuildScrimBand(scrim, "__ScrimKeys",  0f, Scrim.KeyboardZoneFraction,
                topAlpha: Scrim.BoardAlpha, bottomAlpha: Scrim.KeyboardAlpha);
        }

        scrim.gameObject.SetActive(true);
        scrim.SetAsLastSibling(); // above the video surface within the layer; the layer stays behind screens
    }

    private static void BuildScrimBand(Transform parent, string name, float yMin, float yMax,
        float topAlpha, float bottomAlpha)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = new Vector2(0f, yMin);
        rt.anchorMax = new Vector2(1f, yMax);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        var img = go.AddComponent<Image>();
        img.color = Color.white;     // the gradient multiplies vertex colours — keep the base white
        img.raycastTarget = false;   // a veil must never eat taps

        var tint = Scrim.Tint;
        var grad = go.AddComponent<UIVerticalGradient>();
        grad.SetColors(new Color(tint.r, tint.g, tint.b, topAlpha),
                       new Color(tint.r, tint.g, tint.b, bottomAlpha));
    }
}
