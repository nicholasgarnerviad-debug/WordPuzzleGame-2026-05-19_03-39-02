using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace WordPuzzle.UI
{
    /// <summary>
    /// Task 45 — the two retention-beat celebration modals (Tier Unlocked / Streak Milestone),
    /// built at runtime ABOVE the results screen in the DailyRewardPopup overlay idiom: a dim
    /// full-screen blocker that swallows taps (Continue is the ONLY dismiss — these are 2-second
    /// moments, not interrupts), a compact rounded ghost-card (near-black centre + outline ring +
    /// the shared neon glow), and one Continue button. Entrance is the standard screen fade + a
    /// gentle settle; ReduceMotion ⇒ a static card. The milestone's "+N coins" counts up through
    /// <see cref="UIAnimations.CountUpInt"/> (which snaps under ReduceMotion).
    /// </summary>
    public class CelebrationModal : MonoBehaviour
    {
        private Action onContinue;
        private RectTransform card;
        private TMP_Text countUpLabel;
        private int countUpTo = -1;
        private string countUpFormat;
        private bool played;

        /// <summary>
        /// Pure once-ever decision for the tier celebration (unit-tested): celebrate only when the
        /// results surface offers a next tier AND that tier has never been celebrated before.
        /// </summary>
        public static bool ShouldCelebrateTier(List<int> celebratedTiers, bool hasNextTier, int tier)
        {
            if (!hasNextTier || tier <= 0) return false;
            return celebratedTiers == null || !celebratedTiers.Contains(tier);
        }

        /// <summary>"TIER N UNLOCKED" — shown once ever per tier, above the Puzzle Show results.</summary>
        public static CelebrationModal ShowTierUnlocked(Transform parent, int tier, string themeLine, Action onContinue)
        {
            var m = Build(parent, onContinue);
            m.BuildCard($"SHELF {tier} UNLOCKED", TypeRole.Headline, Palette.ModePuzzleShow,
                themeLine, Palette.ModePuzzleShow);
            return m;
        }

        /// <summary>Streak milestone (7/30/100) — the day count huge in gold; the coin bonus counts up.</summary>
        public static CelebrationModal ShowStreakMilestone(Transform parent, int days, int coins, Action onContinue)
        {
            var m = Build(parent, onContinue);
            m.BuildCard(days.ToString(), TypeRole.Display, Palette.Coins,
                $"{days}-DAY STREAK", Palette.Coins);
            m.AddCountUpLine(coins, "+{0} stars");
            return m;
        }

        private static CelebrationModal Build(Transform parent, Action onContinue)
        {
            var go = new GameObject("CelebrationModal", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            go.transform.SetAsLastSibling(); // above the results content (and the doubler)
            var m = go.AddComponent<CelebrationModal>();
            m.onContinue = onContinue;
            return m;
        }

        private void BuildCard(string headline, TypeRole headlineRole, Color headlineColor,
            string line, Color accent)
        {
            // Dim blocker — swallows every tap that isn't Continue (no tap-outside dismiss).
            var blocker = gameObject.AddComponent<Image>();
            var dim = Palette.SurfaceVoid;
            dim.a = UIThemeManager.ReadabilityScrimAlpha; // token alpha-modulation, no new constant
            blocker.color = dim;
            blocker.raycastTarget = true;

            // The card: rounded near-black centre (§15 SurfaceVoid ghost-card centre) + outline ring.
            var cardGo = new GameObject("Card", typeof(RectTransform), typeof(Image));
            cardGo.transform.SetParent(transform, false);
            card = (RectTransform)cardGo.transform;
            card.anchorMin = card.anchorMax = new Vector2(0.5f, 0.5f);
            card.pivot = new Vector2(0.5f, 0.5f);
            card.sizeDelta = new Vector2(780f, 600f);
            var back = cardGo.GetComponent<Image>();
            back.sprite = UIThemeManager.RoundedButtonSprite;
            back.type = Image.Type.Sliced;
            back.color = Palette.SurfaceVoid;
            back.raycastTarget = true; // part of the blocker surface

            var ringGo = new GameObject("Ring", typeof(RectTransform), typeof(Image));
            ringGo.transform.SetParent(card, false);
            var ringRt = (RectTransform)ringGo.transform;
            ringRt.anchorMin = Vector2.zero;
            ringRt.anchorMax = Vector2.one;
            ringRt.offsetMin = Vector2.zero;
            ringRt.offsetMax = Vector2.zero;
            var ring = ringGo.GetComponent<Image>();
            ring.raycastTarget = false;
            UIThemeManager.ApplyOutlineButton(ring, accent); // ring + the shared neon glow

            MakeText("Headline", headline, headlineRole, headlineColor,
                new Vector2(0f, 110f), new Vector2(720f, 200f));
            if (!string.IsNullOrEmpty(line))
                MakeText("ThemeLine", line, TypeRole.Caption, Palette.TextMuted,
                    new Vector2(0f, -10f), new Vector2(720f, 60f));

            // Continue — the obvious single action (outline, the celebration's token colour).
            var btnGo = new GameObject("ContinueButton", typeof(RectTransform), typeof(Image), typeof(Button));
            btnGo.transform.SetParent(card, false);
            var brt = (RectTransform)btnGo.transform;
            brt.anchorMin = brt.anchorMax = new Vector2(0.5f, 0f);
            brt.pivot = new Vector2(0.5f, 0f);
            brt.anchoredPosition = new Vector2(0f, 56f);
            brt.sizeDelta = new Vector2(420f, 110f);
            var bimg = btnGo.GetComponent<Image>();
            bimg.raycastTarget = true;
            UIThemeManager.ApplyOutlineButton(bimg, accent);
            var btn = btnGo.GetComponent<Button>();
            btn.targetGraphic = bimg;
            btn.onClick.AddListener(Continue);
            var blabelGo = new GameObject("Label", typeof(RectTransform));
            blabelGo.transform.SetParent(btnGo.transform, false);
            var blrt = (RectTransform)blabelGo.transform;
            blrt.anchorMin = Vector2.zero;
            blrt.anchorMax = Vector2.one;
            blrt.offsetMin = Vector2.zero;
            blrt.offsetMax = Vector2.zero;
            var blabel = blabelGo.AddComponent<TextMeshProUGUI>();
            blabel.text = "CONTINUE";
            TypeScale.Apply(blabel, TypeRole.Label);
            blabel.alignment = TextAlignmentOptions.Center;
            blabel.raycastTarget = false;
        }

        private void AddCountUpLine(int to, string format)
        {
            countUpLabel = MakeText("CoinLine", string.Format(format, 0), TypeRole.Title, GameAccents.Gold,
                new Vector2(0f, -90f), new Vector2(720f, 70f));
            countUpTo = to;
            countUpFormat = format;
        }

        private TMP_Text MakeText(string name, string text, TypeRole role, Color color,
            Vector2 anchoredPos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(card, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;
            var t = go.AddComponent<TextMeshProUGUI>();
            t.text = text;
            TypeScale.Apply(t, role);
            t.color = color;
            t.alignment = TextAlignmentOptions.Center;
            t.raycastTarget = false;
            return t;
        }

        private void OnEnable()
        {
            if (played) return; // re-enables render at rest — the entrance is a one-time beat
            played = true;

            var cg = gameObject.GetComponent<CanvasGroup>();
            if (cg == null) cg = gameObject.AddComponent<CanvasGroup>();
            if (UIAnimations.ReduceMotion)
            {
                cg.alpha = 1f;
            }
            else
            {
                StartCoroutine(UIAnimations.FadeTransition(cg, true));
                if (card != null)
                {
                    card.localScale = Vector3.one * 0.97f; // settle from a hair small — no bounce
                    StartCoroutine(UIAnimations.RowAcceptSettle(card));
                }
            }

            // The milestone coin bonus counts up (snaps instantly under ReduceMotion).
            if (countUpLabel != null && countUpTo >= 0)
                StartCoroutine(UIAnimations.CountUpInt(countUpLabel, 0, countUpTo,
                    UIAnimations.STANDARD, countUpFormat));
        }

        private void Continue()
        {
            var cb = onContinue;
            onContinue = null;
            Destroy(gameObject);
            cb?.Invoke();
        }
    }
}
