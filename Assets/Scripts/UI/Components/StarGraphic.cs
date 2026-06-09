using UnityEngine;
using UnityEngine.UI;

namespace WordPuzzle.UI.Components
{
    /// <summary>
    /// A 5-pointed star drawn directly as a UI mesh — font-independent (no glyph, no sprite). Used for the
    /// Daily grade rating because the bundled TMP font has no ★ glyph (it rendered as a □ box) and Unity's
    /// built-in font can't be loaded for a runtime fallback. Tint via <see cref="Graphic.color"/>; size via
    /// the RectTransform. Filled solid star; a "dim" colour conveys an unearned star.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public class StarGraphic : MaskableGraphic
    {
        [SerializeField, Range(0.2f, 0.9f)] private float innerRadiusRatio = 0.42f; // dimple of a classic star

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Rect r = GetPixelAdjustedRect();
            float cx = r.x + r.width * 0.5f;
            float cy = r.y + r.height * 0.5f;
            float outer = Mathf.Min(r.width, r.height) * 0.5f;
            float inner = outer * innerRadiusRatio;

            var vert = UIVertex.simpleVert;
            vert.color = color;

            // Centre vertex (index 0).
            vert.position = new Vector3(cx, cy, 0f);
            vh.AddVert(vert);

            // 10 perimeter points, alternating outer/inner radius, starting at the top tip (90°), 36° apart.
            for (int i = 0; i < 10; i++)
            {
                float ang = Mathf.Deg2Rad * (90f - i * 36f);
                float rad = (i % 2 == 0) ? outer : inner;
                vert.position = new Vector3(cx + Mathf.Cos(ang) * rad, cy + Mathf.Sin(ang) * rad, 0f);
                vh.AddVert(vert);
            }

            // Triangle fan from the centre around the 10 perimeter points.
            for (int i = 0; i < 10; i++)
                vh.AddTriangle(0, 1 + i, 1 + (i + 1) % 10);
        }
    }
}
