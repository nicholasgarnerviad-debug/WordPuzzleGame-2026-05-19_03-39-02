using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using WordPuzzle.State;

namespace WordPuzzle.UI
{
    /// <summary>
    /// Task 33C — the Shop. A runtime-built, theme-fitting full-screen overlay (no scene edit): black
    /// background, cyan title, gold coin balance, outline/rounded buttons. Three sections:
    ///   • Power-Ups (bought with COINS via EconomyManager.SpendCoins + Add*),
    ///   • Coins (real money via IStoreService — MockStoreService in-editor),
    ///   • Remove Ads (real money, one-time, via IStoreService).
    /// Rebuilds its content from live economy state after every purchase. Injected via Configure().
    /// </summary>
    public class ShopScreen : MonoBehaviour
    {
        private IEconomyManager economy;
        private IStoreService store;
        private Action onClosed;

        private RectTransform contentRoot;   // the scroll content that gets rebuilt
        private TMP_Text balanceText;
        private TMP_Text feedbackText;
        private bool built;

        // Task 33 — shop pricing, injected via Configure. Lives in BalanceConfig but is passed in because
        // the UI assembly doesn't reference the Puzzle assembly directly. Defaults mirror BalanceConfig.
        private int[] bundleSizes = { 5, 15, 40 };
        private int[] hintPrices = { 50, 135, 320 }, undoPrices = { 50, 135, 320 },
                      revealPrices = { 120, 320, 800 }, timePrices = { 60, 160, 400 };

        private static readonly Color C_BLACK   = new Color(0x0A / 255f, 0x0A / 255f, 0x0A / 255f, 1f);
        private static readonly Color C_GOLD     = new Color(0xC9 / 255f, 0xB4 / 255f, 0x58 / 255f, 1f);
        private static readonly Color C_CREAM    = new Color(0xE7 / 255f, 0xE1 / 255f, 0xC4 / 255f, 1f);
        private static readonly Color C_MUTED    = new Color(0x8A / 255f, 0x93 / 255f, 0xA1 / 255f, 1f);
        private static readonly Color C_SECTION  = new Color(0x39 / 255f, 0x43 / 255f, 0x5A / 255f, 1f);

        public void Configure(IEconomyManager economy, IStoreService store, Action onClosed,
                              int[] bundleSizes, int[] hintPrices, int[] undoPrices, int[] revealPrices, int[] timePrices)
        {
            this.economy = economy;
            this.store = store;
            this.onClosed = onClosed;
            if (bundleSizes != null && bundleSizes.Length > 0) this.bundleSizes = bundleSizes;
            if (hintPrices != null) this.hintPrices = hintPrices;
            if (undoPrices != null) this.undoPrices = undoPrices;
            if (revealPrices != null) this.revealPrices = revealPrices;
            if (timePrices != null) this.timePrices = timePrices;
        }

        /// <summary>Show the shop on top of everything and (re)build it from current state.</summary>
        public void Open()
        {
            gameObject.SetActive(true);
            transform.SetAsLastSibling();      // cover the gear + coin pill + menu behind it
            EnsureBuilt();
            Rebuild();
        }

        public void Close()
        {
            gameObject.SetActive(false);
            onClosed?.Invoke();
        }

        // ── Chrome (built once) ──────────────────────────────────────────────
        private void EnsureBuilt()
        {
            if (built) return;
            built = true;

            var rt = GetComponent<RectTransform>();
            if (rt == null) rt = gameObject.AddComponent<RectTransform>();
            Stretch(rt);

            var bg = gameObject.GetComponent<Image>();
            if (bg == null) bg = gameObject.AddComponent<Image>();
            bg.raycastTarget = true;            // swallow taps so they don't fall through to the menu
            // Task 34A — paint the shared pixel-space backdrop (opaque) so the Shop matches every other
            // screen while fully covering the menu/gear behind this overlay. Falls back to flat black.
            var spaceSprite = Resources.Load<Sprite>("UI/SpaceBackground");
            if (spaceSprite != null)
            {
                bg.sprite = spaceSprite;
                bg.type = Image.Type.Simple;
                bg.preserveAspect = false;      // stretch to fill — no gaps on any aspect ratio
                bg.color = Color.white;         // untinted so the image shows its true colours
            }
            else
            {
                bg.color = C_BLACK;
            }

            // Title — cyan, matches the menu header.
            var title = MakeText(transform, "SHOP", 56f, MenuPalette.TitleColor, FontStyles.Bold, TextAlignmentOptions.Center);
            Anchor(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -120f), new Vector2(600f, 70f));

            // Coin balance — gold, top centre under the title.
            balanceText = MakeText(transform, "0 coins", 34f, C_GOLD, FontStyles.Bold, TextAlignmentOptions.Center);
            Anchor(balanceText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -196f), new Vector2(600f, 48f));

            // Back button — outline, top-left.
            var back = MakeButton(transform, "Back", 30f, MenuPalette.SecondaryBorder, C_CREAM);
            Anchor(((RectTransform)back.transform), new Vector2(0f, 1f), new Vector2(40f, -110f), new Vector2(190f, 80f));
            back.onClick.AddListener(Close);

            // Restore Purchases — outline, top-right (mirrors Back). Re-establishes owned non-consumables
            // (remove-ads / starter-pack) after a reinstall; store policy requires this when selling them.
            var restore = MakeButton(transform, "Restore", 26f, MenuPalette.SecondaryBorder, C_CREAM);
            Anchor(((RectTransform)restore.transform), new Vector2(1f, 1f), new Vector2(-40f, -110f), new Vector2(230f, 80f));
            restore.onClick.AddListener(RestorePurchases);

            // Feedback line near the bottom.
            feedbackText = MakeText(transform, "", 26f, C_CREAM, FontStyles.Normal, TextAlignmentOptions.Center);
            Anchor(feedbackText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 60f), new Vector2(900f, 40f));

            // Scroll view for the (tall) content.
            var scrollGO = new GameObject("ShopScroll", typeof(RectTransform), typeof(Image), typeof(ScrollRect), typeof(RectMask2D));
            scrollGO.transform.SetParent(transform, false);
            var srt = (RectTransform)scrollGO.transform;
            // Task 34B — fill the area BETWEEN the header (title+balance) and the feedback line, with 60px
            // side margins. Stretch anchors + offsets give a correct viewport width (no horizontal overflow)
            // and remove the big empty gap under the title.
            srt.anchorMin = new Vector2(0f, 0f);
            srt.anchorMax = new Vector2(1f, 1f);
            srt.pivot = new Vector2(0.5f, 0.5f);
            srt.offsetMin = new Vector2(60f, 110f);     // left margin, bottom (above feedback)
            srt.offsetMax = new Vector2(-60f, -260f);   // right margin, top (below the balance)
            scrollGO.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0f); // transparent viewport
            var scroll = scrollGO.GetComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;

            var contentGO = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            contentGO.transform.SetParent(scrollGO.transform, false);
            contentRoot = (RectTransform)contentGO.transform;
            contentRoot.anchorMin = new Vector2(0f, 1f);
            contentRoot.anchorMax = new Vector2(1f, 1f);
            contentRoot.pivot = new Vector2(0.5f, 1f);
            // Task 34B — pin the content to the viewport's FULL width (sizeDelta.x = 0 with stretched-x
            // anchors) and centre it (anchoredPosition.x = 0), so labels aren't clipped left and the Buy
            // buttons don't overflow right. Height is driven by the ContentSizeFitter below.
            contentRoot.sizeDelta = new Vector2(0f, contentRoot.sizeDelta.y);
            contentRoot.anchoredPosition = new Vector2(0f, contentRoot.anchoredPosition.y);
            var vlg = contentGO.GetComponent<VerticalLayoutGroup>();
            vlg.childControlWidth = true; vlg.childForceExpandWidth = true;
            vlg.childControlHeight = true; vlg.childForceExpandHeight = false;
            vlg.spacing = 18f;
            vlg.padding = new RectOffset(10, 10, 10, 10);
            var csf = contentGO.GetComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = contentRoot;
            scroll.viewport = srt;
        }

        // ── Content (rebuilt from state) ─────────────────────────────────────
        private void Rebuild()
        {
            if (contentRoot == null) return;
            var prog = economy?.GetCurrentProgress();
            int coins = prog != null ? prog.totalCoins : 0;
            if (balanceText != null) balanceText.text = $"{coins} coins";

            for (int i = contentRoot.childCount - 1; i >= 0; i--)
                Destroy(contentRoot.GetChild(i).gameObject);

            // STARTER PACK (real money, one-time) — pinned at the very top for new players.
            StoreProduct starter = null;
            if (store != null)
                foreach (var p in store.Products) { if (p != null && p.type == StoreProductType.StarterPack) { starter = p; break; } }
            if (starter != null)
            {
                SectionHeader("STARTER PACK  ·  one-time  ·  best value");
                StarterPackRow(starter);
            }

            // POWER-UPS (coins)
            SectionHeader("POWER-UPS  ·  buy with coins");
            PowerUpRow("Hint",   prog != null ? prog.totalHintsEarned   : 0, hintPrices,   AddKind.Hint,   coins);
            PowerUpRow("Undo",   prog != null ? prog.totalUndosEarned   : 0, undoPrices,   AddKind.Undo,   coins);
            PowerUpRow("Reveal", prog != null ? prog.totalRevealsEarned : 0, revealPrices, AddKind.Reveal, coins);
            PowerUpRow("Time",   prog != null ? prog.totalTimeEarned    : 0, timePrices,   AddKind.Time,   coins);

            // COINS (real money)
            SectionHeader("COINS  ·  buy with money");
            if (store != null)
            {
                foreach (var p in store.Products)
                {
                    if (p == null || p.type != StoreProductType.Coins) continue;
                    CoinBundleRow(p);
                }
            }

            // REMOVE ADS (real money)
            SectionHeader("REMOVE ADS");
            RemoveAdsRow();
        }

        private enum AddKind { Hint, Undo, Reveal, Time }

        private void SectionHeader(string label)
        {
            var t = MakeText(contentRoot, label, 26f, C_MUTED, FontStyles.Bold, TextAlignmentOptions.Left);
            var le = t.gameObject.AddComponent<LayoutElement>();
            le.minHeight = 44f; le.preferredHeight = 44f;
            t.margin = new Vector4(8f, 0f, 0f, 0f);
        }

        private void PowerUpRow(string name, int owned, int[] bundlePrices, AddKind kind, int coins)
        {
            var row = MakeRow(150f);
            var label = MakeText(row, $"{name}\n<size=22><color=#8A93A1>owned {owned}</color></size>", 30f, C_CREAM, FontStyles.Bold, TextAlignmentOptions.Left);
            var lle = label.gameObject.AddComponent<LayoutElement>(); lle.preferredWidth = 220f; lle.minWidth = 200f; lle.flexibleWidth = 1f;
            label.richText = true;

            int[] sizes = bundleSizes;
            for (int i = 0; i < sizes.Length; i++)
            {
                int size = sizes[i];
                int price = (bundlePrices != null && i < bundlePrices.Length) ? bundlePrices[i] : size;
                bool canAfford = coins >= price;
                var btn = MakeButton(row, $"x{size}\n<size=20>{price}c</size>", 24f, canAfford ? MenuPalette.ClassicFill : C_SECTION, canAfford ? C_CREAM : C_MUTED);
                var ble = btn.gameObject.AddComponent<LayoutElement>(); ble.preferredWidth = 150f; ble.flexibleWidth = 0f;
                btn.interactable = canAfford;
                var label2 = btn.GetComponentInChildren<TMP_Text>(); if (label2 != null) label2.richText = true;
                int capturedSize = size; int capturedPrice = price; AddKind capturedKind = kind;
                btn.onClick.AddListener(() => BuyPowerUp(capturedKind, capturedSize, capturedPrice));
            }
        }

        private void CoinBundleRow(StoreProduct p)
        {
            var row = MakeRow(120f);
            // Title = pack name (Pouch/Stack/Chest/Vault/Hoard) + optional merchandising badge;
            // subtitle = coin count; price lives on the Buy button (matches the Starter Pack row).
            string title = string.IsNullOrEmpty(p.displayName) ? $"{p.coins} Coins" : p.displayName;
            string badge = string.IsNullOrEmpty(p.badge) ? "" : $"  <size=18><color=#C9B458>[ {p.badge} ]</color></size>";
            var label = MakeText(row, $"{title}{badge}\n<size=22><color=#8A93A1>{p.coins} coins</color></size>",
                                  30f, C_GOLD, FontStyles.Bold, TextAlignmentOptions.Left);
            label.richText = true;
            var lle = label.gameObject.AddComponent<LayoutElement>(); lle.flexibleWidth = 1f;

            var btn = MakeButton(row, $"${p.priceUsd:0.00}", 26f, MenuPalette.DailyFill, C_CREAM);
            var ble = btn.gameObject.AddComponent<LayoutElement>(); ble.preferredWidth = 200f; ble.flexibleWidth = 0f;
            string id = p.id;
            btn.onClick.AddListener(() => BuyRealMoney(id, "Coins added!"));
        }

        private void RemoveAdsRow()
        {
            StoreProduct ads = null;
            if (store != null)
                foreach (var p in store.Products) { if (p != null && p.type == StoreProductType.RemoveAds) { ads = p; break; } }

            var row = MakeRow(120f);
            bool owned = ads != null && store != null && store.IsOwned(ads.id);
            string price = ads != null ? $"${ads.priceUsd:0.00}" : "";
            var label = MakeText(row, owned ? "Remove Ads\n<size=22><color=#6AAA64>Owned</color></size>"
                                            : $"Remove Ads\n<size=22><color=#8A93A1>{price}</color></size>",
                                  30f, C_CREAM, FontStyles.Bold, TextAlignmentOptions.Left);
            label.richText = true;
            var lle = label.gameObject.AddComponent<LayoutElement>(); lle.flexibleWidth = 1f;

            var btn = MakeButton(row, owned ? "Owned" : "Buy", 28f, owned ? C_SECTION : MenuPalette.TimeAttackFill, owned ? C_MUTED : C_CREAM);
            var ble = btn.gameObject.AddComponent<LayoutElement>(); ble.preferredWidth = 200f; ble.flexibleWidth = 0f;
            btn.interactable = !owned && ads != null;
            if (ads != null) { string id = ads.id; btn.onClick.AddListener(() => BuyRealMoney(id, "Ads removed!")); }
        }

        private void StarterPackRow(StoreProduct p)
        {
            bool owned = store != null && store.IsOwned(p.id);
            var row = MakeRow(150f);
            string contents = $"{p.coins} coins  ·  {p.powerUpsEach} of each power-up  ·  {p.adFreeDays} days ad-free";
            var label = MakeText(row, $"{p.displayName}\n<size=20><color=#8A93A1>{contents}</color></size>",
                                  30f, C_GOLD, FontStyles.Bold, TextAlignmentOptions.Left);
            label.richText = true;
            var lle = label.gameObject.AddComponent<LayoutElement>(); lle.flexibleWidth = 1f;

            var btn = MakeButton(row, owned ? "Owned" : $"${p.priceUsd:0.00}", 26f,
                                 owned ? C_SECTION : MenuPalette.DailyFill, owned ? C_MUTED : C_CREAM);
            var ble = btn.gameObject.AddComponent<LayoutElement>(); ble.preferredWidth = 220f; ble.flexibleWidth = 0f;
            btn.interactable = !owned;
            if (!owned) { string id = p.id; btn.onClick.AddListener(() => BuyRealMoney(id, "Starter Pack unlocked!")); }
        }

        // ── Purchase handlers ────────────────────────────────────────────────
        private async void BuyPowerUp(AddKind kind, int size, int price)
        {
            if (economy == null) return;
            bool ok = await economy.SpendCoinsAsync(price, "shop_powerup");
            if (!ok) { Feedback("Not enough coins"); Rebuild(); return; }
            switch (kind)
            {
                case AddKind.Hint:   await economy.AddHintsAsync(size, "shop"); break;
                case AddKind.Undo:   await economy.AddUndosAsync(size, "shop"); break;
                case AddKind.Reveal: await economy.AddRevealsAsync(size, "shop"); break;
                case AddKind.Time:   await economy.AddTimePowerUpsAsync(size, "shop"); break;
            }
            Feedback($"+{size} {kind} added!");
            Rebuild();
        }

        private async void BuyRealMoney(string productId, string successMsg)
        {
            if (store == null) return;
            var outcome = await store.PurchaseAsync(productId);
            switch (outcome)
            {
                case PurchaseOutcome.Success:      Feedback(successMsg); break;
                case PurchaseOutcome.AlreadyOwned: Feedback("Already owned"); break;
                case PurchaseOutcome.Cancelled:    Feedback("Cancelled"); break;
                default:                           Feedback("Purchase unavailable (mock store)"); break;
            }
            Rebuild();
        }

        private async void RestorePurchases()
        {
            if (store == null) return;
            await store.RestorePurchasesAsync();
            Feedback("Purchases restored");
            Rebuild();
        }

        private void Feedback(string msg)
        {
            if (feedbackText != null) feedbackText.text = msg;
        }

        // ── Tiny UI builders ─────────────────────────────────────────────────
        private RectTransform MakeRow(float height)
        {
            var go = new GameObject("Row", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            go.transform.SetParent(contentRoot, false);
            var hlg = go.GetComponent<HorizontalLayoutGroup>();
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlWidth = true; hlg.childForceExpandWidth = false;
            hlg.childControlHeight = true; hlg.childForceExpandHeight = true;
            hlg.spacing = 12f;
            var le = go.GetComponent<LayoutElement>();
            le.minHeight = height; le.preferredHeight = height;
            return (RectTransform)go.transform;
        }

        private TMP_Text MakeText(Transform parent, string text, float size, Color color, FontStyles style, TextAlignmentOptions align)
        {
            var go = new GameObject("Text", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<TextMeshProUGUI>();
            t.text = text; t.fontSize = size; t.color = color; t.fontStyle = style; t.alignment = align;
            t.raycastTarget = false; t.enableWordWrapping = false;
            return t;
        }

        private Button MakeButton(Transform parent, string label, float fontSize, Color outline, Color labelColor)
        {
            var go = new GameObject("Button", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.raycastTarget = true;
            // Coloured rounded OUTLINE button — matches the menu's ghost-button language.
            UIThemeManager.ApplyOutlineButton(img, outline);
            var btn = go.GetComponent<Button>();
            btn.targetGraphic = img;

            var t = MakeText(go.transform, label, fontSize, labelColor, FontStyles.Bold, TextAlignmentOptions.Center);
            Stretch(t.rectTransform);
            return btn;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        }

        private static void Anchor(RectTransform rt, Vector2 anchor, Vector2 anchoredPos, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = anchor;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;
        }
    }
}
