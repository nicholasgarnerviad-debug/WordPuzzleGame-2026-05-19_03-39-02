using UnityEngine;

namespace WordPuzzle.UI.Components
{
    /// <summary>
    /// Task 44 — systematized safe-area handling: maps <see cref="Screen.safeArea"/> onto this
    /// RectTransform's anchors (offsets zeroed) so screen content clears the notch and home
    /// indicator on every device. Attached to every screen's content root by
    /// <c>UIThemeManager.ApplyScreenBackground</c> (replacing per-screen hand fixes); the shared
    /// backdrop + gameplay scrim live on the canvas-level BackgroundLayer, so they intentionally
    /// stay full-bleed OUTSIDE the safe panel. Re-applies on enable and whenever the safe area
    /// or resolution changes (orientation, foldables); a no-op when the safe area is the full
    /// screen (editor Game view), where the mapping equals a plain full-stretch rect.
    /// </summary>
    [DisallowMultipleComponent]
    public class SafeAreaPanel : MonoBehaviour
    {
        private Rect appliedSafeArea = new Rect(-1f, -1f, -1f, -1f);
        private int appliedWidth, appliedHeight;

        private void OnEnable() => Apply();

        private void Update()
        {
            if (Screen.safeArea != appliedSafeArea
                || Screen.width != appliedWidth || Screen.height != appliedHeight)
                Apply();
        }

        private void Apply()
        {
            var rt = transform as RectTransform;
            if (rt == null) return;
            float w = Screen.width, h = Screen.height;
            if (w <= 0f || h <= 0f) return;

            var safe = Screen.safeArea;
            rt.anchorMin = new Vector2(safe.xMin / w, safe.yMin / h);
            rt.anchorMax = new Vector2(safe.xMax / w, safe.yMax / h);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            appliedSafeArea = safe;
            appliedWidth = Screen.width;
            appliedHeight = Screen.height;
        }
    }
}
