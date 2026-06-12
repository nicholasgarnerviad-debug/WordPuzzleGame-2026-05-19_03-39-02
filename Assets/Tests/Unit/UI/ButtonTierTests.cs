using System.Collections.Generic;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WordPuzzle.UI;
using WordPuzzle.UI.Components;

// ============================================================
//  Task 43 — the three-tier button hierarchy + icon pipeline.
//  Pins: (1) ApplyFilledHeroButton fills the shared 9-slice with
//  a vertical gradient over a WHITE base and a soft ~8% press
//  dim; (2) ApplyGhostButton strips chrome, tints the Label-role
//  text, KEEPS the rect tappable and enforces the ≥96px hit
//  target; (3) ApplyButtonIcon is idempotent and never eats
//  taps; (4) every Task 43 PNG resolves through LoadIconSprite
//  (menu modes, gameplay power-ups, logotype); (5) the
//  UIVerticalGradient mesh effect lerps bottom→top.
//  Visual placement on device is the human Simulator gate.
// ============================================================
public class ButtonTierTests
{
    private readonly List<GameObject> spawned = new List<GameObject>();

    [TearDown]
    public void TearDown()
    {
        foreach (var go in spawned)
            if (go != null) Object.DestroyImmediate(go);
        spawned.Clear();
    }

    private GameObject Spawn(string name)
    {
        var go = new GameObject(name, typeof(RectTransform));
        spawned.Add(go);
        return go;
    }

    private Button MakeButton(out Image img, out TextMeshProUGUI label)
    {
        var go = Spawn("Btn");
        img = go.AddComponent<Image>();
        var btn = go.AddComponent<Button>();
        var labelGo = new GameObject("Label", typeof(RectTransform));
        labelGo.transform.SetParent(go.transform, false);
        label = labelGo.AddComponent<TextMeshProUGUI>();
        label.text = "LABEL";
        return btn;
    }

    private static Sprite MakeSprite()
    {
        var tex = new Texture2D(4, 4);
        return Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f));
    }

    // ── Tier 1 — filled hero ─────────────────────────────────

    [Test]
    public void FilledHero_GradientOverWhiteBase_WithSoftPressDim()
    {
        var btn = MakeButton(out var img, out var label);
        var top = new Color(0.8f, 0.4f, 0.9f);
        var bottom = new Color(0.3f, 0.2f, 0.6f);

        UIThemeManager.ApplyFilledHeroButton(btn, top, bottom);

        Assert.AreSame(UIThemeManager.RoundedButtonSprite, img.sprite,
            "hero must reuse the shared bubbly 9-slice as a FILL");
        Assert.AreEqual(Image.Type.Sliced, img.type);
        Assert.AreEqual(Color.white, img.color,
            "base stays white — the gradient multiplies vertex colours");

        var grad = btn.GetComponent<UIVerticalGradient>();
        Assert.IsNotNull(grad, "hero carries the vertical gradient component");
        Assert.AreEqual(top, grad.Top);
        Assert.AreEqual(bottom, grad.Bottom);

        Assert.AreEqual(Color.white, btn.colors.normalColor);
        Assert.AreEqual(0.92f, btn.colors.pressedColor.r, 0.001f,
            "soft ~8% press darken — the punch comes from press feedback");
        Assert.AreEqual(Palette.TextPrimary, label.color,
            "hero label is Label-role TextPrimary on the fill");
    }

    [Test]
    public void FilledHero_Idempotent_OneGradientLatestColoursWin()
    {
        var btn = MakeButton(out _, out _);

        UIThemeManager.ApplyFilledHeroButton(btn, Color.red, Color.blue);
        UIThemeManager.ApplyFilledHeroButton(btn, Color.green, Color.yellow);

        var grads = btn.GetComponents<UIVerticalGradient>();
        Assert.AreEqual(1, grads.Length, "re-styling must reuse the gradient component");
        Assert.AreEqual(Color.green, grads[0].Top);
        Assert.AreEqual(Color.yellow, grads[0].Bottom);
    }

    // ── Tier 3 — ghost ───────────────────────────────────────

    [Test]
    public void Ghost_StripsChrome_TintsLabel_KeepsRectTappable()
    {
        var btn = MakeButton(out var img, out var label);
        img.sprite = MakeSprite();
        var tint = new Color(0.55f, 0.47f, 0.78f);

        UIThemeManager.ApplyGhostButton(btn, tint);

        Assert.IsNull(img.sprite, "ghost renders NO chrome");
        Assert.AreEqual(0f, img.color.a, "ghost image is invisible");
        Assert.IsTrue(img.raycastTarget, "the full rect must stay tappable");
        Assert.AreEqual(tint, label.color, "the tinted text IS the button");
    }

    [Test]
    public void Ghost_SurvivesOutlineConversion_TheShopRestorePath()
    {
        // Shop Restore / library grid Back are built as outlines first, then recede to ghost.
        var btn = MakeButton(out var img, out var label);
        UIThemeManager.ApplyOutlineButton(img, Palette.AccentPeriwinkle);

        UIThemeManager.ApplyGhostButton(btn, Palette.AccentPeriwinkle);

        Assert.IsNull(img.sprite);
        Assert.AreEqual(0f, img.color.a);
        Assert.AreEqual(Palette.AccentPeriwinkle, label.color);
    }

    [Test]
    public void Ghost_GrowsSmallRectsToTheMinimumHitTarget()
    {
        var btn = MakeButton(out _, out _);
        var rt = (RectTransform)btn.transform;
        rt.sizeDelta = new Vector2(160f, 64f);

        UIThemeManager.ApplyGhostButton(btn, Color.white);

        Assert.GreaterOrEqual(rt.sizeDelta.y, UIThemeManager.GhostMinHitHeight);
        Assert.AreEqual(160f, rt.sizeDelta.x, "only the short axis is enforced");
    }

    [Test]
    public void Ghost_RespectsLayoutElement_AndNeverShrinks()
    {
        var small = MakeButton(out _, out _);
        var smallLe = small.gameObject.AddComponent<LayoutElement>();
        smallLe.minHeight = 50f;

        var tall = MakeButton(out _, out _);
        var tallLe = tall.gameObject.AddComponent<LayoutElement>();
        tallLe.minHeight = 130f;

        UIThemeManager.ApplyGhostButton(small, Color.white);
        UIThemeManager.ApplyGhostButton(tall, Color.white);

        Assert.AreEqual(UIThemeManager.GhostMinHitHeight, smallLe.minHeight);
        Assert.AreEqual(130f, tallLe.minHeight, "a taller authored target is never shrunk");
    }

    // ── Phase 3 — the icon slot ──────────────────────────────

    [Test]
    public void ButtonIcon_CreatesOneSlot_Idempotent_NeverEatsTaps()
    {
        var btn = MakeButton(out _, out _);
        var icon = MakeSprite();
        var tint = new Color(0.2f, 0.9f, 0.8f);

        var first = UIThemeManager.ApplyButtonIcon(btn, icon, tint);
        var second = UIThemeManager.ApplyButtonIcon(btn, icon, tint);

        Assert.IsNotNull(first);
        Assert.AreSame(first, second, "re-applying must reuse the slot, not stack icons");
        Assert.IsFalse(first.raycastTarget, "the icon must never eat the button's tap");
        Assert.IsTrue(first.preserveAspect);
        Assert.AreEqual(tint, first.color, "white-stroke art carries the token tint");
        Assert.AreSame(first, UIThemeManager.FindButtonIcon(btn));
    }

    [Test]
    public void ButtonIcon_NullSafe_NoSlotWithoutASprite()
    {
        var btn = MakeButton(out _, out _);

        Assert.IsNull(UIThemeManager.ApplyButtonIcon(btn, null, Color.white));
        Assert.IsNull(UIThemeManager.FindButtonIcon(btn));
        Assert.IsNull(UIThemeManager.ApplyButtonIcon(null, MakeSprite(), Color.white));
    }

    // ── Icon pipeline — Resources/Icons PNGs ─────────────────

    [Test]
    public void LoadIconSprite_ResolvesEveryTask43Asset()
    {
        // Menu modes + gameplay power-ups + the masthead: a rename or a broken import
        // surfaces here instead of as a silently icon-less button.
        string[] names =
        {
            "IconResume", "IconDaily", "IconClassic", "IconPuzzleShow", "IconTimeAttack",
            "IconHint", "IconUndo", "IconReveal", "IconAddTime",
            "StarLadderLogotype",
        };
        foreach (var name in names)
            Assert.IsNotNull(UIThemeManager.LoadIconSprite(name), $"missing Resources/Icons/{name}");
    }

    [Test]
    public void LoadIconSprite_CachesAndIsNullSafe()
    {
        var a = UIThemeManager.LoadIconSprite("IconDaily");
        var b = UIThemeManager.LoadIconSprite("IconDaily");
        Assert.AreSame(a, b, "repeat loads must not mint new sprites");

        Assert.IsNull(UIThemeManager.LoadIconSprite("NoSuchIcon"));
        Assert.IsNull(UIThemeManager.LoadIconSprite(null));
        Assert.IsNull(UIThemeManager.LoadIconSprite(""));
    }

    // ── UIVerticalGradient mesh effect ───────────────────────

    [Test]
    public void Gradient_LerpsVerticesBottomToTop()
    {
        var go = Spawn("Grad");
        go.AddComponent<Image>();
        var grad = go.AddComponent<UIVerticalGradient>();
        grad.SetColors(Color.red, Color.blue); // top red, bottom blue

        var vh = new VertexHelper();
        var v = UIVertex.simpleVert;
        v.color = Color.white; // white base — the gradient multiplies, so lerp shows pure
        v.position = new Vector3(0f, 0f, 0f);   vh.AddVert(v);
        v.position = new Vector3(10f, 0f, 0f);  vh.AddVert(v);
        v.position = new Vector3(10f, 20f, 0f); vh.AddVert(v);
        v.position = new Vector3(0f, 20f, 0f);  vh.AddVert(v);
        vh.AddTriangle(0, 1, 2);
        vh.AddTriangle(2, 3, 0);

        grad.ModifyMesh(vh);

        UIVertex read = default;
        vh.PopulateUIVertex(ref read, 0);
        Assert.AreEqual((Color32)Color.blue, read.color, "lowest vertices take Bottom");
        vh.PopulateUIVertex(ref read, 2);
        Assert.AreEqual((Color32)Color.red, read.color, "highest vertices take Top");
        vh.Dispose();
    }
}
