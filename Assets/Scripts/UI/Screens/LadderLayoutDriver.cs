using UnityEngine;
using UnityEngine.UI;

namespace WordPuzzle.UI
{
    /// <summary>
    /// Drives the contiguous ladder layout so the chain area collapses to zero
    /// height when empty and grows as rungs are added, pushing CurrentInputRow
    /// and the TO block downward. All rows remain direct children of GameplayScreen
    /// (preserving SerializeField wiring). This script is attached to GameplayScreen.
    ///
    /// Tight empty state (no chain words):
    ///   StartWordLabel → StartWordRow → [0px chain] → CurrentInputRow → EndWordLabel → EndWordRow
    ///
    /// As words are accepted the chain grows, and the input/TO block shifts down.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class LadderLayoutDriver : MonoBehaviour
    {
        [Header("Ladder rows — assign in Inspector")]
        [SerializeField] private RectTransform startWordLabel;
        [SerializeField] private RectTransform startWordRow;
        [SerializeField] private RectTransform chainScrollView;
        [SerializeField] private RectTransform chainScrollContent;
        [SerializeField] private RectTransform currentInputRow;
        [SerializeField] private RectTransform endWordLabel;
        [SerializeField] private RectTransform endWordRow;

        [Header("Layout constants")]
        [SerializeField] private float labelHeight    = 35f;
        [SerializeField] private float tileRowHeight  = 120f;
        [SerializeField] private float gapLabelToRow  = 4f;
        [SerializeField] private float gapRowToChain  = 12f;
        [SerializeField] private float gapChainToInput = 12f;
        [SerializeField] private float gapInputToLabel = 8f;
        [SerializeField] private float gapLabelToEnd  = 4f;

        // The top of the ladder column in GameplayScreen-local space (anchor y=0.5).
        // Canvas height=1920, centre=960. Header takes ~130px below top → header bottom ~210px.
        // Ladder top starts ~30px below header bottom → ladderTopY = 960 - 240 = 720 (from centre, positive=up).
        [SerializeField] private float ladderTopY = 720f;

        // Cap chain area so fully-grown ladder's bottom (EndWordRow) clears the toolbar top.
        // Toolbar sits ~30px above keyboard top edge at 457px from screen bottom.
        // Toolbar top ≈ 572px from screen bottom = 960-572 = 388px from centre (downward = -388).
        // EndWordRow bottom must stay above toolbar top:
        //   ladder bottom from centre = ladderTopY - (labelH+gap + tileH+gap + chainMaxH+gap + tileH+gap + labelH+gap + tileH)
        //   ≈ 720 - (35+4+120+12 + chainMaxH + 12+120+8+35+4+120) = 720 - 470 - chainMaxH
        // For EndWordRow bottom > -388: 720 - 470 - chainMaxH > -388 → chainMaxH < 638.
        // Use 340px as a comfortable cap leaving ~60px clearance above toolbar.
        [SerializeField] private float chainMaxHeight = 340f;

        private void LateUpdate()
        {
            if (!AllRefsValid()) return;

            float chainContentH = ResolvedChainHeight();

            PositionLadder(chainContentH);
        }

        // ------------------------------------------------------------------
        // VLG padding (8+8=16) is present even when chain is empty.
        // Treat anything at or below this threshold as "empty" (no gap).
        private const float EmptyContentThreshold = 20f;

        private float ResolvedChainHeight()
        {
            if (chainScrollContent == null) return 0f;
            // ContentSizeFitter drives sizeDelta.y; rect.height reflects this after layout.
            float h = chainScrollContent.rect.height;
            if (h <= EmptyContentThreshold) return 0f;
            return Mathf.Clamp(h, 0f, chainMaxHeight);
        }

        /// <summary>
        /// Lays out all rows top-down from ladderTopY.
        /// Labels (startWordLabel, endWordLabel) are skipped when their GameObject is inactive
        /// so the rows sit cleanly without a label gap.
        /// All RectTransforms use anchor = (0, 0.5)/(1, 0.5) (stretch-X, centre-Y).
        /// We write anchoredPosition.y and sizeDelta.y; pivot must be (0.5, 1) for top-referenced rows.
        /// </summary>
        private void PositionLadder(float chainH)
        {
            float cursor = ladderTopY; // top of next element, positive = up from center

            // --- StartWordLabel (skip when inactive — Issue 3: FROM label hidden) ---
            bool startLabelActive = startWordLabel != null && startWordLabel.gameObject.activeInHierarchy;
            if (startLabelActive)
            {
                SetRow(startWordLabel, cursor, labelHeight, pivotTop: true);
                cursor -= labelHeight + gapLabelToRow;
            }

            // --- StartWordRow (pivot top) ---
            SetRow(startWordRow, cursor, tileRowHeight, pivotTop: true);
            cursor -= tileRowHeight + gapRowToChain;

            // --- ChainScrollView: height = chainContentH, capped ---
            float scrollH = chainH; // 0 when empty
            SetRow(chainScrollView, cursor, scrollH, pivotTop: true);
            if (scrollH > 0f) cursor -= scrollH + gapChainToInput;
            // When empty, no gap — next item is directly below.

            // --- CurrentInputRow (pivot top) ---
            SetRow(currentInputRow, cursor, tileRowHeight, pivotTop: true);
            cursor -= tileRowHeight + gapInputToLabel;

            // --- EndWordLabel (skip when inactive — Issue 3: TO label hidden) ---
            bool endLabelActive = endWordLabel != null && endWordLabel.gameObject.activeInHierarchy;
            if (endLabelActive)
            {
                SetRow(endWordLabel, cursor, labelHeight, pivotTop: true);
                cursor -= labelHeight + gapLabelToEnd;
            }

            // --- EndWordRow (pivot top) ---
            SetRow(endWordRow, cursor, tileRowHeight, pivotTop: true);
        }

        /// <summary>
        /// Positions a row so its top edge is at localY (in GameplayScreen local space,
        /// anchor = centre). Sets pivot.y = 1 (top) and anchoredPosition.y = localY.
        /// </summary>
        private static void SetRow(RectTransform rt, float localY, float height, bool pivotTop)
        {
            if (rt == null) return;

            // Normalize anchor to stretch-X, centre-Y
            rt.anchorMin = new Vector2(0f, 0.5f);
            rt.anchorMax = new Vector2(1f, 0.5f);

            // Pivot y=1 means anchoredPosition.y is the top of the rect
            var piv = rt.pivot;
            piv.y = pivotTop ? 1f : 0.5f;
            rt.pivot = piv;

            var ap = rt.anchoredPosition;
            ap.y = localY;
            rt.anchoredPosition = ap;

            var sd = rt.sizeDelta;
            sd.y = height;
            rt.sizeDelta = sd;
        }

        private bool AllRefsValid()
        {
            return startWordLabel   != null
                && startWordRow     != null
                && chainScrollView  != null
                && chainScrollContent != null
                && currentInputRow  != null
                && endWordLabel     != null
                && endWordRow       != null;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Preview in editor without play mode when values change.
            if (!Application.isPlaying && AllRefsValid())
            {
                float chainH = Mathf.Clamp(chainScrollContent.rect.height, 0f, chainMaxHeight);
                PositionLadder(chainH);
            }
        }
#endif
    }
}
