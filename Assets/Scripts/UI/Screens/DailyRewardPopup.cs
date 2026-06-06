using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace WordPuzzle.UI
{
    /// <summary>
    /// Task 36 36K — runtime "Daily Rewards" overlay shown over the main menu when a login reward is
    /// claimable and/or a slipped streak can be repaired. Self-contained (no scene edit, no surgery on the
    /// finely-tuned MainMenuScreen layout): GameBootstrap owns the economy/streak logic and injects
    /// primitives + callbacks, so this is dumb UI. Mirrors the ShopScreen overlay pattern.
    /// </summary>
    public class DailyRewardPopup : MonoBehaviour
    {
        private Action onClosed;

        // Per-show state + callbacks (supplied by GameBootstrap each time it is shown).
        private bool loginAvailable; private int loginCoins; private int loginDay; private bool loginClaimed;
        private Action<Action<int>> claimLogin;                 // claimLogin(cb): grants, then cb(coinsGranted)
        private bool repairAvailable; private int repairCost; private bool repairAffordable; private bool adReady;
        private Action<bool, Action<bool>> requestRepair;       // requestRepair(useAd, cb(success))
        private string repairMsg;

        private RectTransform contentRoot;
        private bool built;

        // Direction B — forward to the canonical Palette (no raw hex).
        private static readonly Color C_BLACK   = new Color(Palette.SurfaceVoid.r, Palette.SurfaceVoid.g, Palette.SurfaceVoid.b, 0.97f);
        private static readonly Color C_GOLD     = Palette.Coins;
        private static readonly Color C_GREEN    = Palette.AccentAqua; // retired green → aqua
        private static readonly Color C_CREAM    = Palette.TextPrimary;
        private static readonly Color C_MUTED    = Palette.TextMuted;
        private static readonly Color C_SECTION  = Palette.Panel;
        private static readonly Color C_SCRIM    = new Color(0f, 0f, 0f, 0.86f);

        // Rich-text colour tags derive their hex from the tokens above — no raw theme hex scattered.
        private static string Hx(Color c) => ColorUtility.ToHtmlStringRGB(c);

        public void Configure(Action onClosed) { this.onClosed = onClosed; }

        /// <summary>Show the overlay for the supplied login/repair state. Caller decides when there is
        /// anything worth showing (login available OR repair available).</summary>
        public void ShowRewards(bool loginAvailable, int loginCoins, int loginDay, Action<Action<int>> claimLogin,
                                bool repairAvailable, int repairCost, bool repairAffordable, bool adReady,
                                Action<bool, Action<bool>> requestRepair)
        {
            this.loginAvailable = loginAvailable; this.loginCoins = loginCoins; this.loginDay = loginDay;
            this.claimLogin = claimLogin; this.loginClaimed = false;
            this.repairAvailable = repairAvailable; this.repairCost = repairCost;
            this.repairAffordable = repairAffordable; this.adReady = adReady; this.requestRepair = requestRepair;
            this.repairMsg = null;

            gameObject.SetActive(true);
            transform.SetAsLastSibling();   // above the menu, gear, and coin pill
            EnsureBuilt();
            Rebuild();
        }

        public void Close()
        {
            gameObject.SetActive(false);
            onClosed?.Invoke();
        }

        // ── Chrome (built once): full-screen scrim + centred auto-sizing card ──
        private void EnsureBuilt()
        {
            if (built) return;
            built = true;

            var rt = GetComponent<RectTransform>();
            if (rt == null) rt = gameObject.AddComponent<RectTransform>();
            Stretch(rt);

            var bg = gameObject.GetComponent<Image>();
            if (bg == null) bg = gameObject.AddComponent<Image>();
            bg.color = C_SCRIM;          // dim the menu behind
            bg.raycastTarget = true;     // modal — swallow taps so they don't fall through

            var card = new GameObject("Card", typeof(RectTransform), typeof(Image),
                                      typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            card.transform.SetParent(transform, false);
            var crt = (RectTransform)card.transform;
            crt.anchorMin = crt.anchorMax = new Vector2(0.5f, 0.5f);
            crt.pivot = new Vector2(0.5f, 0.5f);
            crt.sizeDelta = new Vector2(840f, 0f);
            card.GetComponent<Image>().color = C_BLACK;

            var vlg = card.GetComponent<VerticalLayoutGroup>();
            vlg.childControlWidth = true; vlg.childForceExpandWidth = true;
            vlg.childControlHeight = true; vlg.childForceExpandHeight = false;
            vlg.spacing = 16f; vlg.padding = new RectOffset(44, 44, 40, 40);
            vlg.childAlignment = TextAnchor.UpperCenter;
            var csf = card.GetComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            contentRoot = crt;
        }

        // ── Content (rebuilt after every action) ──
        private void Rebuild()
        {
            if (contentRoot == null) return;
            for (int i = contentRoot.childCount - 1; i >= 0; i--)
                Destroy(contentRoot.GetChild(i).gameObject);

            var title = Label("DAILY REWARDS", MenuPalette.TitleColor, 46f);
            title.fontStyle = FontStyles.Bold;

            // Login reward.
            if (loginAvailable)
            {
                if (!loginClaimed)
                {
                    Label($"Day {loginDay} reward", C_CREAM, 30f);
                    Label($"<color=#{Hx(C_GOLD)}>+{loginCoins} coins</color>", C_CREAM, 42f);
                    Btn("CLAIM", C_GOLD, true, () =>
                    {
                        if (claimLogin == null) return;
                        claimLogin(_ => { loginClaimed = true; Rebuild(); });
                    });
                }
                else
                {
                    Label($"<color=#{Hx(C_GREEN)}>Claimed  ·  +{loginCoins} coins</color>", C_CREAM, 32f);
                }
            }

            // Streak repair.
            if (repairAvailable)
            {
                Spacer(8f);
                Label("Your streak slipped", C_CREAM, 30f);
                Label("Repair yesterday to keep it alive", C_MUTED, 24f);

                if (!string.IsNullOrEmpty(repairMsg))
                {
                    Label($"<color=#{Hx(C_GREEN)}>{repairMsg}</color>", C_CREAM, 28f);
                }
                else
                {
                    Btn(repairAffordable ? $"REPAIR  ·  {repairCost} coins" : $"Need {repairCost} coins",
                        repairAffordable ? C_GOLD : C_SECTION, repairAffordable, () =>
                        {
                            if (requestRepair == null) return;
                            requestRepair(false, ok =>
                            {
                                if (ok) { repairMsg = "Streak repaired!"; }
                                else { repairMsg = "Couldn't repair"; }
                                Rebuild();
                            });
                        });
                    Btn("REPAIR  ·  Watch ad", MenuPalette.TimeAttackFill, true, () =>
                    {
                        if (requestRepair == null) return;
                        requestRepair(true, ok =>
                        {
                            repairMsg = ok ? "Streak repaired!" : "Ads aren't available yet";
                            Rebuild();
                        });
                    });
                }
            }

            Spacer(8f);
            Btn("CLOSE", C_MUTED, true, Close);
        }

        // ── tiny builders (mirror ShopScreen) ──
        private TMP_Text Label(string text, Color color, float size)
        {
            var go = new GameObject("Line", typeof(RectTransform));
            go.transform.SetParent(contentRoot, false);
            var t = go.AddComponent<TextMeshProUGUI>();
            t.text = text; t.fontSize = size; t.color = color;
            t.alignment = TextAlignmentOptions.Center; t.richText = true;
            t.raycastTarget = false; t.enableWordWrapping = true;
            var le = go.AddComponent<LayoutElement>(); le.minHeight = size + 16f;
            return t;
        }

        private void Spacer(float h)
        {
            var go = new GameObject("Spacer", typeof(RectTransform));
            go.transform.SetParent(contentRoot, false);
            go.AddComponent<LayoutElement>().minHeight = h;
        }

        private void Btn(string label, Color outline, bool interactable, Action onClick)
        {
            var go = new GameObject("Button", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            go.transform.SetParent(contentRoot, false);
            var img = go.GetComponent<Image>();
            img.raycastTarget = true;
            UIThemeManager.ApplyOutlineButton(img, outline);
            var le = go.GetComponent<LayoutElement>(); le.minHeight = 86f; le.preferredHeight = 86f;
            var btn = go.GetComponent<Button>();
            btn.targetGraphic = img; btn.interactable = interactable;
            if (onClick != null) btn.onClick.AddListener(() => onClick());

            var tgo = new GameObject("Label", typeof(RectTransform));
            tgo.transform.SetParent(go.transform, false);
            var t = tgo.AddComponent<TextMeshProUGUI>();
            Stretch(t.rectTransform);
            t.text = label; t.fontSize = 28f; t.color = interactable ? C_CREAM : C_MUTED;
            t.fontStyle = FontStyles.Bold; t.alignment = TextAlignmentOptions.Center;
            t.raycastTarget = false; t.richText = true;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        }
    }
}
