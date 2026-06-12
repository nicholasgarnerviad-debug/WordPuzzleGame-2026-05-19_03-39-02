using UnityEngine;
using UnityEngine.UI;

namespace WordPuzzle.UI.Components
{
    /// <summary>
    /// Task 43 — a two-stop VERTICAL gradient for uGUI Graphics (Image can't gradient natively).
    /// Multiplies vertex colours from <see cref="Bottom"/> (lowest vertex) to <see cref="Top"/>
    /// (highest), so it composes with the Graphic's own tint (keep the Image white for a pure
    /// gradient). Owned by Task 43 (filled-hero buttons); Task 44's gameplay scrim CONSUMES this
    /// component — do not duplicate it.
    /// </summary>
    [RequireComponent(typeof(Graphic))]
    [DisallowMultipleComponent]
    public class UIVerticalGradient : BaseMeshEffect
    {
        [SerializeField] private Color top = Color.white;
        [SerializeField] private Color bottom = Color.white;

        public Color Top => top;
        public Color Bottom => bottom;

        public void SetColors(Color topColor, Color bottomColor)
        {
            top = topColor;
            bottom = bottomColor;
            if (graphic != null) graphic.SetVerticesDirty();
        }

        public override void ModifyMesh(VertexHelper vh)
        {
            if (!IsActive() || vh == null || vh.currentVertCount == 0) return;

            UIVertex v = default;
            float minY = float.MaxValue, maxY = float.MinValue;
            for (int i = 0; i < vh.currentVertCount; i++)
            {
                vh.PopulateUIVertex(ref v, i);
                if (v.position.y < minY) minY = v.position.y;
                if (v.position.y > maxY) maxY = v.position.y;
            }

            float height = Mathf.Max(0.0001f, maxY - minY);
            for (int i = 0; i < vh.currentVertCount; i++)
            {
                vh.PopulateUIVertex(ref v, i);
                float t = (v.position.y - minY) / height;
                v.color = (Color)v.color * Color.Lerp(bottom, top, t);
                vh.SetUIVertex(v, i);
            }
        }
    }
}
