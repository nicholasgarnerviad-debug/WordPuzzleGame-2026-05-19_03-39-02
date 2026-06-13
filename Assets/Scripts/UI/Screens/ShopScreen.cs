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
        private Func<int> watchCoinsRemaining;        // Task 36 36K — watches left today (for the row label)
        private Action<Action<int>> watchForCoins;    // Task 36 36K — play rewarded ad then grant; cb(coinsGranted)
        private Func<bool> watchAdReady;              // v1.0 audit C4 — rewarded ad loaded? (null = assume ready)
        private Coroutine watchReadyPoll;             // refreshes the Watch row once the ad arrives

        // v1.0 audit Track 3 — real-money commerce is OFF for v1.0: the only IStoreService is a
        // mock, so selling Starter Pack / coin packs / Remove Ads would be fake purchases. Flip
        // to true when a real store integration lands (planned 1.1). readonly (not const) so the
        // gated blocks don't trip CS0162 unreachable-code warnings.
        private static readonly bool RealMoneyEnabled = false;

        private RectTransform contentRoot;   // the scroll content that gets rebuilt
        private TMP_Text balanceText;
        private TMP_Text feedbackText;
        private bool built;

        // Task 33 — shop pricing, injected via Configure. Lives in BalanceConfig but is passed in because
        // the UI assembly doesn't reference the Puzzle assembly directly. Defaults mirror BalanceConfig.
        private int[] bundleSizes = { 5, 15, 40 };
        private int[] hintPrices = { 50, 135, 320 }, undoPrices = { 50, 135, 320 },
                      revealPrices = { 120, 320, 800 }, timePrices = { 60, 160, 400 };

        // Direction B — forward to the canonical Palette (no raw hex).
        private static readonly Color C_GOLD     = Palette.Coins;
        private static readonly Color C_CREAM    = Palette.TextPrimary;
        private static readonly Color C_MUTED    = Palette.TextMuted;
        private static readonly Color C_SECTION  = Palette.Panel;

        // Rich-text colour tags below derive their hex from the tokens above, so no raw theme hex is scattered.
        private static string Hx(Color c) => ColorUtility.ToHtmlStringRGB(c);

        // Task 41B — analytics seams. The UI assembly can't see IAnalytics (no Puzzle reference),
        // so the shop raises plain events and GameBootstrap forwards them to the reporter.
        public event Action<string> OnPurchaseAttempt;          // real-money product id
        public event Action<string, string> OnPurchaseResult;   // product id, outcome (snake-ish lowercase)
        public event Action<string, int> OnBundleBought;        // power-up kind, bundle size (coin spend)

        public void Configure(IEconomyManager economy, IStoreService store, Action onClosed,
                              int[] bundleSizes, int[] hintPrices, int[] undoPrices, int[] revealPrices, int[] timePrices,
                              Func<int> watchCoinsRemaining = null, Action<Action<int>> watchForCoins = null,
                              Func<bool> watchAdReady = null)
        {
            this.economy = economy;
            this.store = store;
            this.onClosed = onClosed;
            this.watchAdReady = watchAdReady;
            if (bundleSizes != null && bundleSizes.Length > 0) this.bundleSizes = bundleSizes;
            if (hintPrices != null) this.hintPrices = hintPrices;
            if (undoPrices != null) this.undoPrices = undoPrices;
            if (revealPrices != null) this.revealPrices = revealPrices;
            if (timePrices != null) this.timePrices = timePrices;
            this.watchCoinsRemaining = watchCoinsRemaining;
            this.watchForCoins = watchForCoins;
        }

        /// <summary>Show the shop on top of everything and (re)build it from current state.</summary>
        public void Open()
        {
            gameObject.SetActive(true);
            transform.SetAsLastSibling();      // cover the gear + coin pill + menu behind it
            EnsureBuilt();
            Rebuild();
            UIAnimations.PlayScreenEntrance(this); // modern feel — gentle fade-in on open (ReduceMotion-gated)
        }

        public void Close()
        {
            StopWatchReadyPoll();
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

            // Direction B — give the Shop overlay the SAME animated app backdrop as every other screen
            // (opaque, since it covers the menu behind it). Routed through the shared helper so it can't
            // drift back onto a stale still image.
            UIThemeManager.ApplyOverlayBackground(gameObject);

            // Task 46 — the overlay SafeArea fix deferred from Task 44: the opaque backdrop must
            // stay full-bleed on the ROOT (insetting the root would expose the screen behind at
            // the notch), so the CONTENT gets its own safe-area'd wrapper instead. Anchors below
            // are unchanged — they were authored against a full-stretch rect, which this is.
            var safeGo = new GameObject("__SafeContent", typeof(RectTransform));
            safeGo.transform.SetParent(transform, false);
            Stretch((RectTransform)safeGo.transform);
            safeGo.AddComponent<WordPuzzle.UI.Components.SafeAreaPanel>();
            Transform content = safeGo.transform;

            // Title — cyan, matches the menu header.
            var title = MakeText(content, "SHOP", TypeRole.Headline, MenuPalette.TitleColor, TextAlignmentOptions.Center);
            Anchor(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -120f), new Vector2(600f, 70f));

            // Coin balance — the shared gold coin pill (matches menu/stats), centred under the title.
            var pill = new GameObject("CoinPill", typeof(RectTransform), typeof(Image),
                                      typeof(HorizontalLayoutGroup), typeof(ContentSizeFitter));
            pill.transform.SetParent(content, false);
            var prt = (RectTransform)pill.transform;
            prt.anchorMin = prt.anchorMax = new Vector2(0.5f, 1f);
            prt.pivot = new Vector2(0.5f, 1f);
            prt.anchoredPosition = new Vector2(0f, -196f);
            var pimg = pill.GetComponent<Image>(); pimg.raycastTarget = false;
            UIThemeManager.ApplyOutlineButton(pimg, Palette.AccentPeriwinkle);
            var phlg = pill.GetComponent<HorizontalLayoutGroup>();
            phlg.childControlWidth = true; phlg.childForceExpandWidth = false;
            phlg.childControlHeight = true; phlg.childForceExpandHeight = false;
            phlg.childAlignment = TextAnchor.MiddleCenter;
            phlg.spacing = 10f; phlg.padding = new RectOffset(22, 24, 8, 8);
            var pcsf = pill.GetComponent<ContentSizeFitter>();
            pcsf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            pcsf.verticalFit   = ContentSizeFitter.FitMode.PreferredSize;
            var token = new GameObject("Token", typeof(RectTransform), typeof(Image));
            token.transform.SetParent(pill.transform, false);
            var timg = token.GetComponent<Image>(); timg.color = C_GOLD; timg.raycastTarget = false;
            UIThemeManager.ApplyRoundedButton(timg);
            var tle = token.AddComponent<LayoutElement>(); tle.preferredWidth = 26f; tle.preferredHeight = 26f;
            balanceText = MakeText(pill.transform, "0", TypeRole.Body, C_GOLD, TextAlignmentOptions.Center);

            // Back button — Task 43 tier 3: navigation recedes to a ghost (same as library Back).
            var back = MakeButton(content, "Back", MenuPalette.SecondaryBorder, C_CREAM);
            Anchor(((RectTransform)back.transform), new Vector2(0f, 1f), new Vector2(40f, -110f), new Vector2(190f, 96f));
            UIThemeManager.ApplyGhostButton(back, MenuPalette.SecondaryBorder);
            back.onClick.AddListener(Close);

            // Restore Purchases — Task 43 ghost tier, top-right (mirrors Back). Rare store-policy
            // utility, so it recedes to tinted text. Re-establishes owned non-consumables
            // (remove-ads / starter-pack) after a reinstall; store policy requires this when selling
            // them — and ONLY when selling them, so it hides with the money sections (Track 3).
            if (RealMoneyEnabled)
            {
                var restore = MakeButton(content, "Restore", MenuPalette.SecondaryBorder, C_CREAM);
                Anchor(((RectTransform)restore.transform), new Vector2(1f, 1f), new Vector2(-40f, -110f), new Vector2(230f, 80f));
                UIThemeManager.ApplyGhostButton(restore, MenuPalette.SecondaryBorder);
                restore.onClick.AddListener(RestorePurchases);
            }

            // Feedback line near the bottom.
            feedbackText = MakeText(content, "", TypeRole.Caption, C_CREAM, TextAlignmentOptions.Center);
            Anchor(feedbackText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 60f), new Vector2(900f, 40f));

            // Scroll view for the (tall) content.
            var scrollGO = new GameObject("ShopScroll", typeof(RectTransform), typeof(Image), typeof(ScrollRect), typeof(RectMask2D));
            scrollGO.transform.SetParent(content, false);
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
            if (balanceText != null) balanceText.text = coins.ToString("N0"); // the pill's gold token carries the unit

            for (int i = contentRoot.childCount - 1; i >= 0; i--)
                Destroy(contentRoot.GetChild(i).gameObject);

            // STARTER PACK (real money, one-time) — pinned at the very top for new players.
            StoreProduct starter = null;
            if (RealMoneyEnabled && store != null)
                foreach (var p in store.Products) { if (p != null && p.type == StoreProductType.StarterPack) { starter = p; break; } }
            if (starter != null)
            {
                SectionHeader("STARTER PACK  ·  best value"); // short enough to never truncate at Title size
                StarterPackRow(starter);
            }

            // POWER-UPS (coins)
            SectionHeader("POWER-UPS  ·  buy with coins");
            PowerUpRow("Hint",   prog != null ? prog.totalHintsEarned   : 0, hintPrices,   AddKind.Hint,   coins);
            PowerUpRow("Undo",   prog != null ? prog.totalUndosEarned   : 0, undoPrices,   AddKind.Undo,   coins);
            PowerUpRow("Reveal", prog != null ? prog.totalRevealsEarned : 0, revealPrices, AddKind.Reveal, coins);
            PowerUpRow("Time",   prog != null ? prog.totalTimeEarned    : 0, timePrices,   AddKind.Time,   coins);

            // FREE COINS (watch a rewarded ad) — daily-capped faucet, shown above the paid packs.
            if (watchForCoins != null)
            {
                SectionHeader("FREE COINS  ·  watch an ad");
                WatchForCoinsRow();
            }

            // COINS + REMOVE ADS (real money) — hidden for v1.0 (Track 3, RealMoneyEnabled).
            if (RealMoneyEnabled)
            {
                SectionHeader("COINS  ·  buy with money");
                if (store != null)
                {
                    foreach (var p in store.Products)
                    {
                        if (p == null || p.type != StoreProductType.Coins) continue;
                        CoinBundleRow(p);
                    }
                }

                SectionHeader("REMOVE ADS");
                RemoveAdsRow();
            }
        }

        private enum AddKind { Hint, Undo, Reveal, Time }

        private void SectionHeader(string label)
        {
            var t = MakeText(contentRoot, label, TypeRole.Title, C_MUTED, TextAlignmentOptions.Left);
            var le = t.gameObject.AddComponent<LayoutElement>();
            le.minHeight = 56f; le.preferredHeight = 56f; // Title (44) needs more than the old 26pt row
            t.margin = new Vector4(8f, 0f, 0f, 0f);
        }

        private void PowerUpRow(string name, int owned, int[] bundlePrices, AddKind kind, int coins)
        {
            var row = MakeRow(150f);
            var label = MakeText(row, $"{name}\n<size=24><color=#{Hx(C_MUTED)}>owned {owned}</color></size>", TypeRole.Body, C_CREAM, TextAlignmentOptions.Left);
            var lle = label.gameObject.AddComponent<LayoutElement>(); lle.preferredWidth = 220f; lle.minWidth = 200f; lle.flexibleWidth = 1f;
            label.richText = true;

            int[] sizes = bundleSizes;
            for (int i = 0; i < sizes.Length; i++)
            {
                int size = sizes[i];
                int price = (bundlePrices != null && i < bundlePrices.Length) ? bundlePrices[i] : size;
                bool canAfford = coins >= price;
                var btn = MakeButton(row, $"x{size}\n<size=22>{price}c</size>", canAfford ? MenuPalette.ClassicFill : C_SECTION, canAfford ? C_CREAM : C_MUTED);
                var ble = btn.gameObject.AddComponent<LayoutElement>(); ble.preferredWidth = 150f; ble.flexibleWidth = 0f;
                btn.interactable = canAfford;
                var label2 = btn.GetComponentInChildren<TMP_Text>(); if (label2 != null) label2.richText = true;
                int capturedSize = size; int capturedPrice = price; AddKind capturedKind = kind;
                btn.onClick.AddListener(() => BuyPowerUp(capturedKind, capturedSize, capturedPrice));
            }
        }

        private void WatchForCoinsRow()
        {
            int remaining = watchCoinsRemaining != null ? watchCoinsRemaining() : 0;
            bool canWatch = remaining > 0;
            // v1.0 audit C4 — a tappable "Watch" while the rewarded ad is still loading was a
            // silent no-op; show a disabled "Loading…" instead and refresh when the ad arrives.
            bool adReady = watchAdReady == null || watchAdReady();
            var row = MakeRow(120f);
            string sub = canWatch ? $"{remaining} left today" : "back tomorrow";
            var label = MakeText(row, $"Watch for Coins\n<size=24><color=#{Hx(C_MUTED)}>Free  ·  {sub}</color></size>",
                                  TypeRole.Body, C_CREAM, TextAlignmentOptions.Left);
            label.richText = true;
            var lle = label.gameObject.AddComponent<LayoutElement>(); lle.flexibleWidth = 1f;

            var btn = MakeButton(row, !canWatch ? "Done" : adReady ? "Watch" : "Loading…",
                                 canWatch && adReady ? MenuPalette.TimeAttackFill : C_SECTION,
                                 canWatch && adReady ? C_CREAM : C_MUTED);
            var ble = btn.gameObject.AddComponent<LayoutElement>(); ble.preferredWidth = 200f; ble.flexibleWidth = 0f;
            btn.interactable = canWatch && adReady;
            if (canWatch && adReady)
                btn.onClick.AddListener(() => watchForCoins(coins =>
                {
                    Feedback(coins > 0 ? $"+{coins} coins!" : "Ads aren't available yet");
                    Rebuild();
                }));
            if (canWatch && !adReady) StartWatchReadyPoll();
        }

        // Poll the rewarded-ad readiness while the "Loading…" row is showing; one Rebuild when
        // it flips. Dedicated handle (never StopAllCoroutines — see the ShowToast trap).
        private void StartWatchReadyPoll()
        {
            if (watchReadyPoll != null || watchAdReady == null || !isActiveAndEnabled) return;
            watchReadyPoll = StartCoroutine(WatchReadyPollRoutine());
        }

        private void StopWatchReadyPoll()
        {
            if (watchReadyPoll == null) return;
            StopCoroutine(watchReadyPoll);
            watchReadyPoll = null;
        }

        private System.Collections.IEnumerator WatchReadyPollRoutine()
        {
            // Bounded: ~60s of 1s checks; the retry backoff caps near this horizon anyway,
            // and reopening the shop restarts the poll.
            for (int i = 0; i < 60; i++)
            {
                yield return new WaitForSecondsRealtime(1f);
                if (watchAdReady != null && watchAdReady())
                {
                    watchReadyPoll = null;
                    Rebuild();
                    yield break;
                }
            }
            watchReadyPoll = null;
        }

        private void CoinBundleRow(StoreProduct p)
        {
            var row = MakeRow(120f);
            // Title = pack name (Pouch/Stack/Chest/Vault/Hoard) + optional merchandising badge;
            // subtitle = coin count; price lives on the Buy button (matches the Starter Pack row).
            string title = string.IsNullOrEmpty(p.displayName) ? $"{p.coins} Coins" : p.displayName;
            string badge = string.IsNullOrEmpty(p.badge) ? "" : $"  <size=22><color=#{Hx(C_GOLD)}>[ {p.badge} ]</color></size>";
            var label = MakeText(row, $"{title}{badge}\n<size=24><color=#{Hx(C_MUTED)}>{p.coins} coins</color></size>",
                                  TypeRole.Body, C_GOLD, TextAlignmentOptions.Left);
            label.richText = true;
            var lle = label.gameObject.AddComponent<LayoutElement>(); lle.flexibleWidth = 1f;

            var btn = MakeButton(row, $"${p.priceUsd:0.00}", MenuPalette.DailyFill, C_CREAM);
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
            var label = MakeText(row, owned ? $"Remove Ads\n<size=24><color=#{Hx(Palette.AccentAqua)}>Owned</color></size>"
                                            : $"Remove Ads\n<size=24><color=#{Hx(C_MUTED)}>{price}</color></size>",
                                  TypeRole.Body, C_CREAM, TextAlignmentOptions.Left);
            label.richText = true;
            var lle = label.gameObject.AddComponent<LayoutElement>(); lle.flexibleWidth = 1f;

            var btn = MakeButton(row, owned ? "Owned" : "Buy", owned ? C_SECTION : MenuPalette.TimeAttackFill, owned ? C_MUTED : C_CREAM);
            var ble = btn.gameObject.AddComponent<LayoutElement>(); ble.preferredWidth = 200f; ble.flexibleWidth = 0f;
            btn.interactable = !owned && ads != null;
            if (ads != null) { string id = ads.id; btn.onClick.AddListener(() => BuyRealMoney(id, "Ads removed!")); }
        }

        private void StarterPackRow(StoreProduct p)
        {
            bool owned = store != null && store.IsOwned(p.id);
            var row = MakeRow(150f, C_GOLD); // gold ring — the merchandised lead offer
            string contents = $"{p.coins} coins  ·  {p.powerUpsEach} of each power-up  ·  {p.adFreeDays} days ad-free";
            var label = MakeText(row, $"{p.displayName}\n<size=24><color=#{Hx(C_MUTED)}>{contents}</color></size>",
                                  TypeRole.Body, C_GOLD, TextAlignmentOptions.Left);
            label.richText = true;
            var lle = label.gameObject.AddComponent<LayoutElement>(); lle.flexibleWidth = 1f;

            var btn = MakeButton(row, owned ? "Owned" : $"${p.priceUsd:0.00}",
                                 owned ? C_SECTION : MenuPalette.DailyFill, owned ? C_MUTED : C_CREAM);
            var ble = btn.gameObject.AddComponent<LayoutElement>(); ble.preferredWidth = 220f; ble.flexibleWidth = 0f;
            btn.interactable = !owned;
            // Task 43 — the Starter Pack price is the shop's ONE filled hero (the DAILY gradient).
            if (!owned)
            {
                UIThemeManager.ApplyFilledHeroButton(btn, Palette.ModeDaily, Palette.ModePuzzleShow);
                string id = p.id; btn.onClick.AddListener(() => BuyRealMoney(id, "Starter Pack unlocked!"));
            }
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
            OnBundleBought?.Invoke(kind.ToString().ToLowerInvariant(), size);   // Task 41B
            Rebuild();
        }

        private async void BuyRealMoney(string productId, string successMsg)
        {
            if (store == null) return;
            OnPurchaseAttempt?.Invoke(productId);   // Task 41B
            var outcome = await store.PurchaseAsync(productId);
            OnPurchaseResult?.Invoke(productId, outcome.ToString().ToLowerInvariant());   // Task 41B
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
        private RectTransform MakeRow(float height) => MakeRow(height, GameAccents.CardOutline);

        // Each row is a SOLID card (shared modern-card seam) so labels and prices sit on readable
        // ground instead of colliding with the backdrop planets; `accent` tints the ring.
        private RectTransform MakeRow(float height, Color accent)
        {
            var go = new GameObject("Row", typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            go.transform.SetParent(contentRoot, false);
            var img = go.GetComponent<Image>(); img.raycastTarget = false;
            UIThemeManager.ApplySolidCard(img, accent);
            var hlg = go.GetComponent<HorizontalLayoutGroup>();
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlWidth = true; hlg.childForceExpandWidth = false;
            hlg.childControlHeight = true; hlg.childForceExpandHeight = true;
            hlg.spacing = 12f;
            hlg.padding = new RectOffset(28, 18, 14, 14);
            var le = go.GetComponent<LayoutElement>();
            le.minHeight = height; le.preferredHeight = height;
            return (RectTransform)go.transform;
        }

        private TMP_Text MakeText(Transform parent, string text, TypeRole role, Color color, TextAlignmentOptions align)
        {
            var go = new GameObject("Text", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<TextMeshProUGUI>();
            t.text = text;
            TypeScale.Apply(t, role); // Task 42
            t.color = color; t.alignment = align;
            t.raycastTarget = false; t.enableWordWrapping = false;
            return t;
        }

        private Button MakeButton(Transform parent, string label, Color outline, Color labelColor)
        {
            var go = new GameObject("Button", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.raycastTarget = true;
            // Coloured rounded OUTLINE button — matches the menu's ghost-button language.
            UIThemeManager.ApplyOutlineButton(img, outline);
            var btn = go.GetComponent<Button>();
            btn.targetGraphic = img;

            var t = MakeText(go.transform, label, TypeRole.Label, labelColor, TextAlignmentOptions.Center);
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
