using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WordPuzzle.UI;
using WordPuzzle.UI.Components;

// ============================================================
//  Task 43 — the three-tier button hierarchy.
//  Tier 1  ApplyFilledHeroButton — ONE solid gradient-filled hero per surface
//          (DAILY on the menu). Same shared 9-slice geometry; the glow reads
//          as rim light on the solid.
//  Tier 2  ApplyOutlineButton / ApplyPrimaryMenuButton — unchanged (UITheme.cs).
//  Tier 3  ApplyGhostButton — no ring, no glow: tinted Label-role text on a
//          generous invisible hit target. Navigation/utility recedes.
//  Partial of UIThemeManager so the documented seam names stay flat.
// ============================================================
public static partial class UIThemeManager
{
    /// <summary>Ghost buttons keep a comfortable, invisible hit target (canvas units).</summary>
    public const float GhostMinHitHeight = 96f;

    private const string TierIconChildName = "__TierIcon";

    /// <summary>
    /// Tier 1 — the ONE filled hero: the shared bubbly 9-slice as a solid fill carrying a
    /// vertical <paramref name="top"/>→<paramref name="bottom"/> gradient (via
    /// <see cref="UIVerticalGradient"/>), hero-brightness neon glow as rim light, Label-role
    /// text, and a soft ~8% press darken (the press-punch comes from the existing hooks).
    /// </summary>
    public static void ApplyFilledHeroButton(Button btn, Color top, Color bottom)
    {
        if (btn == null) return;
        var img = btn.GetComponent<Image>();
        if (img == null) return;

        img.sprite = RoundedButtonSprite;   // the FILLED bubbly 9-slice (not the outline ring)
        img.type = Image.Type.Sliced;
        img.pixelsPerUnitMultiplier = 1f;
        img.color = Color.white;            // the gradient multiplies vertex colours — keep the base white
        RemoveRingChild(img.transform);

        var grad = img.GetComponent<UIVerticalGradient>();
        if (grad == null) grad = img.gameObject.AddComponent<UIVerticalGradient>();
        grad.SetColors(top, bottom);

        ApplyNeonGlow(img, top, hero: true); // rim light on a solid — the intended hero read

        var label = btn.GetComponentInChildren<TMP_Text>(true);
        if (label != null) TypeScale.Apply(label, TypeRole.Label); // TextPrimary on the fill

        var cb = btn.colors;                 // ~8% darken on press; punch comes from press feedback
        cb.normalColor      = Color.white;
        cb.highlightedColor = new Color(0.97f, 0.97f, 0.97f, 1f);
        cb.pressedColor     = new Color(0.92f, 0.92f, 0.92f, 1f);
        cb.selectedColor    = Color.white;
        cb.disabledColor    = new Color(0.5f, 0.5f, 0.5f, 0.5f);
        cb.colorMultiplier  = 1f;
        btn.colors = cb;
    }

    /// <summary>
    /// Tier 3 — ghost: NO ring sprite, NO glow; just tinted Label-role text over a generous
    /// invisible hit target (≥ <see cref="GhostMinHitHeight"/>). For navigation/utility
    /// (Library/Stats pair, library grid Back, shop Restore, results Home) — they recede.
    /// </summary>
    public static void ApplyGhostButton(Button btn, Color tint)
    {
        if (btn == null) return;
        var img = btn.GetComponent<Image>();
        if (img != null)
        {
            img.sprite = null;
            img.color = new Color(0f, 0f, 0f, 0f); // invisible — alpha does not affect raycast
            img.raycastTarget = true;              // the full rect stays tappable
            RemoveRingChild(img.transform);
            RemoveGlowChild(img.transform);
            var grad = img.GetComponent<UIVerticalGradient>();
            if (grad != null) Object.Destroy(grad);
        }

        var label = btn.GetComponentInChildren<TMP_Text>(true);
        if (label != null)
        {
            TypeScale.Apply(label, TypeRole.Label);
            label.color = tint; // the tinted text IS the button
        }

        // Generous invisible hit target — never shrink a ghost below the comfortable minimum.
        var le = btn.GetComponent<LayoutElement>();
        if (le != null)
        {
            if (le.minHeight < GhostMinHitHeight) le.minHeight = GhostMinHitHeight;
        }
        else
        {
            var rt = btn.transform as RectTransform;
            if (rt != null && rt.sizeDelta.y > 0f && rt.sizeDelta.y < GhostMinHitHeight)
                rt.sizeDelta = new Vector2(rt.sizeDelta.x, GhostMinHitHeight);
        }
    }

    private static void RemoveGlowChild(Transform parent)
    {
        var glow = parent.Find(GlowChildName);
        if (glow != null) Object.Destroy(glow.gameObject);
    }

    /// <summary>
    /// Phase 3 — the optional LEFT-ALIGNED icon slot on the shared button geometry: a tinted
    /// child Image exactly like <c>UIManager.CreateGlobalSettingsButton</c> builds its gear.
    /// Idempotent (reuses the slot); never eats taps. Ghost buttons stay text-only — don't call.
    /// </summary>
    public static Image ApplyButtonIcon(Button btn, Sprite icon, Color tint,
        float size = 48f, float leftInset = 36f)
    {
        if (btn == null || icon == null) return null;

        var t = btn.transform.Find(TierIconChildName);
        Image img;
        if (t != null)
        {
            img = t.GetComponent<Image>();
            if (img == null) img = t.gameObject.AddComponent<Image>();
        }
        else
        {
            var go = new GameObject(TierIconChildName, typeof(RectTransform));
            go.transform.SetParent(btn.transform, false);
            img = go.AddComponent<Image>();
        }

        var rt = img.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 0.5f);
        rt.pivot = new Vector2(0f, 0.5f);
        rt.anchoredPosition = new Vector2(leftInset, 0f);
        rt.sizeDelta = new Vector2(size, size);

        img.sprite = icon;
        img.color = tint;        // white-stroke art × token tint
        img.raycastTarget = false;
        img.preserveAspect = true;
        return img;
    }

    /// <summary>Find a button's tier-icon slot (for state tinting); null when it has none.</summary>
    public static Image FindButtonIcon(Button btn)
    {
        if (btn == null) return null;
        var t = btn.transform.Find(TierIconChildName);
        return t != null ? t.GetComponent<Image>() : null;
    }

    // ── Runtime icon pipeline — PNGs under Resources/Icons (mirrors the HOME/gear pattern). ──
    private static readonly Dictionary<string, Sprite> _iconSprites = new Dictionary<string, Sprite>();

    /// <summary>Load (and cache) an icon sprite from Resources/Icons/&lt;name&gt;.png. Null-safe.</summary>
    public static Sprite LoadIconSprite(string name)
    {
        if (string.IsNullOrEmpty(name)) return null;
        if (_iconSprites.TryGetValue(name, out var cached) && cached != null) return cached;

        var tex = Resources.Load<Texture2D>("Icons/" + name);
        if (tex == null) return null; // not cached — a later-dropped asset resolves next call
        var sprite = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height),
            new Vector2(0.5f, 0.5f), 100f);
        sprite.name = name;
        _iconSprites[name] = sprite;
        return sprite;
    }
}
