using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace WordPuzzle.UI
{
    /// <summary>
    /// Non-modal tutorial overlay (no full-screen raycast blocker).
    /// Design tokens: bg-surface #1B1F27, accent-gold #C9B458, text-primary #E7E1C4.
    ///
    /// Step flow:
    ///   step 1 — "Change ONE letter to make a new word."   (on Begin)
    ///   step 3 — "Now reach TO — one step at a time."      (after accepted, not yet done)
    ///   step 4 — "You've got it — here's your first puzzle." + 1.1s beat -> OnSuccessBeatFinished
    ///
    /// GameBootstrap subscribes:
    ///   OnSkipRequested       -> CompleteTutorial(skipped:true)
    ///   OnSuccessBeatFinished -> CompleteTutorial(skipped:false)
    /// </summary>
    public class TutorialOverlay : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI calloutText;
        [SerializeField] private Button skipButton;
        [SerializeField] private RectTransform highlightFrame;
        [SerializeField] private RectTransform startRowAnchor;
        [SerializeField] private RectTransform endRowAnchor;

        public event System.Action OnSkipRequested;
        public event System.Action OnSuccessBeatFinished;

        private static readonly Color32 AccentGold    = new Color32(0xC9, 0xB4, 0x58, 0xFF);
        private static readonly Color32 TextPrimary   = new Color32(0xE7, 0xE1, 0xC4, 0xFF);

        private const string Step1Text = "Change ONE letter to make a new word.";
        private const string Step3Text = "Now reach the end word — one step at a time.";
        private const string Step4Text = "You've got it — here's your first puzzle.";
        private const float  SuccessBeatDelay = 1.1f;

        private bool skipSubscribed;
        private Coroutine successCoroutine;

        private void Awake()
        {
            SubscribeSkip();
        }

        private void OnEnable()
        {
            SubscribeSkip();
        }

        private void OnDisable()
        {
            UnsubscribeSkip();
            if (successCoroutine != null)
            {
                StopCoroutine(successCoroutine);
                successCoroutine = null;
            }
        }

        private void SubscribeSkip()
        {
            if (skipSubscribed || skipButton == null) return;
            skipButton.onClick.AddListener(HandleSkipClicked);
            ApplySkipButtonStyle();
            skipSubscribed = true;
        }

        private void UnsubscribeSkip()
        {
            if (!skipSubscribed || skipButton == null) return;
            skipButton.onClick.RemoveListener(HandleSkipClicked);
            skipSubscribed = false;
        }

        private void ApplySkipButtonStyle()
        {
            if (skipButton == null) return;
            var outline = skipButton.GetComponent<UnityEngine.UI.Outline>();
            if (outline == null) outline = skipButton.gameObject.AddComponent<UnityEngine.UI.Outline>();
            outline.effectColor = AccentGold;
            var lbl = skipButton.GetComponentInChildren<TMP_Text>(true);
            if (lbl != null)
            {
                lbl.color = AccentGold;
                lbl.text  = "SKIP";
            }
        }

        // --- Public contract ---------------------------------------------------------

        /// <summary>Activate the overlay and show step 1, highlighting the start row.</summary>
        public void Begin()
        {
            gameObject.SetActive(true);
            SetCallout(Step1Text);
            PositionHighlight(startRowAnchor);
        }

        /// <summary>
        /// Called by GameBootstrap each time the player submits a word.
        /// accepted && !reachedEnd -> step 3 (highlight end row).
        /// reachedEnd -> step 4 success beat coroutine.
        /// </summary>
        public void OnSubmission(bool accepted, bool reachedEnd)
        {
            if (reachedEnd)
            {
                SetCallout(Step4Text);
                PositionHighlight(null);
                if (successCoroutine != null) StopCoroutine(successCoroutine);
                successCoroutine = StartCoroutine(SuccessBeatRoutine());
                return;
            }
            if (accepted)
            {
                SetCallout(Step3Text);
                PositionHighlight(endRowAnchor);
            }
        }

        /// <summary>Deactivate the overlay.</summary>
        public void Hide()
        {
            if (successCoroutine != null)
            {
                StopCoroutine(successCoroutine);
                successCoroutine = null;
            }
            gameObject.SetActive(false);
        }

        // --- Private helpers ---------------------------------------------------------

        private void SetCallout(string text)
        {
            if (calloutText == null) return;
            calloutText.text  = text;
            calloutText.color = TextPrimary;
        }

        private void PositionHighlight(RectTransform anchor)
        {
            if (highlightFrame == null) return;
            highlightFrame.gameObject.SetActive(anchor != null);
            if (anchor == null) return;
            highlightFrame.position = anchor.position;
        }

        private IEnumerator SuccessBeatRoutine()
        {
            yield return new WaitForSecondsRealtime(SuccessBeatDelay);
            successCoroutine = null;
            OnSuccessBeatFinished?.Invoke();
        }

        private void HandleSkipClicked()
        {
            OnSkipRequested?.Invoke();
        }
    }
}
