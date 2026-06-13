#!/usr/bin/env python3
"""Task 43 - Star Ladder visual-identity art builder.

Generates (machine-generated -- re-run this tool, never hand-edit the outputs):
  * 9 stroke icon SVGs -> Assets/ui/icons/Icon*.svg  (24x24, stroke 2, white,
    round caps/joins -- the same Lucide-style language as the existing gear)
  * PNG fallbacks      -> Assets/Resources/Icons/Icon*.png (256px, white, alpha)
  * Logotype           -> Assets/ui/icons/StarLadderLogotype.svg (candidate A,
    wired) + _CandidateB/_CandidateC.svg for the human pick, traced from the
    real Rungo-Bold glyph outlines; aqua->periwinkle vertical gradient; the
    one four-point star in the word gap. (The original "ladder-rung A" motif —
    an extra bar above LADDER's A crossbar — read as a RENDERING GLITCH at
    masthead size, not a motif; retired. The star alone carries the identity.)
  * 4x PNG masthead    -> Assets/Resources/Icons/StarLadderLogotype.png
  * Unity .meta files for every SVG (svgType=1 Textured Sprite, STABLE guids
    derived from the filename so re-runs never churn references).

Requires: fontTools, Pillow.  Run: python Tools/logo_icons_build.py
"""
import hashlib
import math
import os

from fontTools.ttLib import TTFont
from fontTools.pens.svgPathPen import SVGPathPen
from PIL import Image, ImageDraw, ImageFont

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SVG_DIR = os.path.join(ROOT, 'Assets', 'ui', 'icons')
PNG_DIR = os.path.join(ROOT, 'Assets', 'Resources', 'Icons')
FONT_PATH = os.path.join(ROOT, 'Assets', 'Fonts', 'Rungo', 'Rungo-Bold.ttf')

BOARD = 24.0          # icon artboard
STROKE = 2.0          # matches the existing settings-gear / home chrome weight
PNG_SIZE = 256        # icon PNG fallback resolution
AQUA = (84, 168, 180)        # Palette.AccentAqua  #54A8B4
PERIWINKLE = (142, 120, 200) # Palette.AccentPeriwinkle #8E78C8

# ---------------------------------------------------------------- primitives
# Geometry is authored in SCREEN coords (y down) on the 24-unit board.
# Arc angles use math convention about the centre with +angle = upward-CCW on
# screen; both emitters share the same convention so SVG and PNG match.

def _arc_point(cx, cy, r, deg):
    return (cx + r * math.cos(math.radians(deg)), cy - r * math.sin(math.radians(deg)))


def _qcurve_points(p0, c, p1, n=24):
    pts = []
    for i in range(n + 1):
        t = i / n
        x = (1 - t) ** 2 * p0[0] + 2 * (1 - t) * t * c[0] + t ** 2 * p1[0]
        y = (1 - t) ** 2 * p0[1] + 2 * (1 - t) * t * c[1] + t ** 2 * p1[1]
        pts.append((x, y))
    return pts


def _arrowhead(tip, direction, length=4.0, spread_deg=34):
    dx, dy = direction
    mag = math.hypot(dx, dy) or 1.0
    dx, dy = dx / mag, dy / mag
    lines = []
    for s in (+1, -1):
        a = math.radians(spread_deg) * s
        rx = dx * math.cos(a) - dy * math.sin(a)
        ry = dx * math.sin(a) + dy * math.cos(a)
        lines.append(('line', tip[0], tip[1], tip[0] - rx * length, tip[1] - ry * length))
    return lines


def _undo_arc(cx, cy, r, a0, a1):
    """Arc + an arrowhead at the a1 end, aligned to the travel tangent."""
    prims = [('arc', cx, cy, r, a0, a1)]
    tip = _arc_point(cx, cy, r, a1)
    a = math.radians(a1)
    tangent = (-math.sin(a), -math.cos(a))  # CCW travel direction in screen coords
    prims += _arrowhead(tip, tangent)
    return prims


ICONS = {
    # calendar with a star on the date cell
    'IconDaily': [
        ('rrect', 3.5, 5.5, 17.0, 15.0, 2.5),
        ('line', 8.0, 3.0, 8.0, 7.5), ('line', 16.0, 3.0, 16.0, 7.5),
        ('line', 3.5, 10.5, 20.5, 10.5),
        ('fillstar', 12.0, 15.8, 3.0, 1.15),
    ],
    # infinity loop (circles overlap so the pair reads as one figure-eight)
    'IconClassic': [
        ('circle', 8.7, 12.0, 4.6), ('circle', 15.3, 12.0, 4.6),
    ],
    # trophy — rim ends meet the handle tops exactly (no loose joints)
    'IconPuzzleShow': [
        ('line', 5.5, 4.5, 18.5, 4.5),
        ('arc', 5.5, 6.5, 2.0, 90, 270),
        ('arc', 18.5, 6.5, 2.0, -90, 90),
        ('line', 7.5, 4.5, 7.5, 9.5), ('line', 16.5, 4.5, 16.5, 9.5),
        ('arc', 12.0, 9.5, 4.5, 180, 360),
        ('line', 12.0, 14.0, 12.0, 16.5),
        ('line', 8.5, 19.0, 15.5, 19.0),
    ],
    # stopwatch
    'IconTimeAttack': [
        ('circle', 12.0, 13.5, 7.5),
        ('line', 10.0, 2.5, 14.0, 2.5), ('line', 12.0, 4.5, 12.0, 6.0),
        ('line', 12.0, 13.5, 15.2, 10.3),
    ],
    # play-in-circle
    'IconResume': [
        ('circle', 12.0, 12.0, 9.5),
        ('polyline', [(10.0, 8.6), (10.0, 15.4), (15.8, 12.0)], True),
    ],
    # lightbulb
    'IconHint': [
        ('circle', 12.0, 9.5, 5.5),
        ('line', 9.6, 17.4, 14.4, 17.4), ('line', 10.6, 20.2, 13.4, 20.2),
    ],
    # curved undo arrow
    'IconUndo': _undo_arc(12.0, 12.5, 7.0, -75, 152),
    # eye
    'IconReveal': [
        ('qcurve', (3.2, 12.0), (12.0, 4.8), (20.8, 12.0)),
        ('qcurve', (3.2, 12.0), (12.0, 19.2), (20.8, 12.0)),
        ('circle', 12.0, 12.0, 3.0),
    ],
    # clock-plus
    'IconAddTime': [
        ('circle', 10.5, 13.5, 7.0),
        ('line', 10.5, 13.5, 10.5, 9.6), ('line', 10.5, 13.5, 13.6, 13.5),
        ('line', 19.5, 4.0, 19.5, 10.0), ('line', 16.5, 7.0, 22.5, 7.0),
    ],
}


def _star_points(cx, cy, R, r):
    pts = []
    for i in range(8):
        ang = 90 - i * 45
        rad = R if i % 2 == 0 else r
        pts.append(_arc_point(cx, cy, rad, ang))
    return pts


# ---------------------------------------------------------------- SVG emit

def icon_svg(prims):
    parts = []
    for p in prims:
        k = p[0]
        if k == 'circle':
            _, cx, cy, r = p
            parts.append(f'<circle cx="{cx}" cy="{cy}" r="{r}"/>')
        elif k == 'line':
            _, x1, y1, x2, y2 = p
            parts.append(f'<line x1="{x1}" y1="{y1}" x2="{x2}" y2="{y2}"/>')
        elif k == 'arc':
            _, cx, cy, r, a0, a1 = p
            x0, y0 = _arc_point(cx, cy, r, a0)
            x1, y1 = _arc_point(cx, cy, r, a1)
            sweep = a1 - a0
            large = 1 if abs(sweep) > 180 else 0
            sflag = 0 if sweep > 0 else 1
            parts.append(f'<path d="M{x0:.3f},{y0:.3f} A{r},{r} 0 {large} {sflag} {x1:.3f},{y1:.3f}"/>')
        elif k == 'rrect':
            _, x, y, w, h, rx = p
            parts.append(f'<rect x="{x}" y="{y}" width="{w}" height="{h}" rx="{rx}"/>')
        elif k == 'qcurve':
            _, p0, c, p1 = p
            parts.append(f'<path d="M{p0[0]},{p0[1]} Q{c[0]},{c[1]} {p1[0]},{p1[1]}"/>')
        elif k == 'polyline':
            _, pts, closed = p
            coords = ' '.join(f'{x:.3f},{y:.3f}' for x, y in pts)
            tag = 'polygon' if closed else 'polyline'
            parts.append(f'<{tag} points="{coords}"/>')
        elif k == 'fillstar':
            _, cx, cy, R, r = p
            coords = ' '.join(f'{x:.3f},{y:.3f}' for x, y in _star_points(cx, cy, R, r))
            parts.append(f'<polygon points="{coords}" fill="#FFFFFF" stroke="none"/>')
    body = ''.join(parts)
    return ('<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" '
            'fill="none" stroke="#FFFFFF" stroke-width="2" stroke-linecap="round" '
            f'stroke-linejoin="round">{body}</svg>')


# ---------------------------------------------------------------- PNG emit

def icon_png(prims, path):
    S = PNG_SIZE / BOARD
    w = max(2, int(round(STROKE * S)))
    cap_r = w / 2.0
    img = Image.new('RGBA', (PNG_SIZE, PNG_SIZE), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    white = (255, 255, 255, 255)

    def dot(pt):
        x, y = pt[0] * S, pt[1] * S
        d.ellipse([x - cap_r, y - cap_r, x + cap_r, y + cap_r], fill=white)

    def seg(pts, closed=False):
        sp = [(x * S, y * S) for x, y in pts]
        if closed:
            sp = sp + [sp[0]]
        d.line(sp, fill=white, width=w, joint='curve')
        if not closed:
            dot(pts[0]); dot(pts[-1])
        else:
            for pt in pts:
                dot(pt)

    for p in prims:
        k = p[0]
        if k == 'circle':
            _, cx, cy, r = p
            d.ellipse([(cx - r) * S, (cy - r) * S, (cx + r) * S, (cy + r) * S],
                      outline=white, width=w)
        elif k == 'line':
            _, x1, y1, x2, y2 = p
            seg([(x1, y1), (x2, y2)])
        elif k == 'arc':
            _, cx, cy, r, a0, a1 = p
            pts = [_arc_point(cx, cy, r, a0 + (a1 - a0) * i / 40) for i in range(41)]
            seg(pts)
        elif k == 'rrect':
            _, x, y, ww, hh, rx = p
            d.rounded_rectangle([x * S, y * S, (x + ww) * S, (y + hh) * S],
                                radius=rx * S, outline=white, width=w)
        elif k == 'qcurve':
            _, p0, c, p1 = p
            seg(_qcurve_points(p0, c, p1))
        elif k == 'polyline':
            _, pts, closed = p
            seg(pts, closed=closed)
        elif k == 'fillstar':
            _, cx, cy, R, r = p
            d.polygon([(x * S, y * S) for x, y in _star_points(cx, cy, R, r)], fill=white)
    img.save(path)


# ---------------------------------------------------------------- logotype

TEXT = 'STAR LADDER'
TRACKING = 30  # font units


def _layout(font):
    glyphset = font.getGlyphSet()
    cmap = font.getBestCmap()
    x = 0
    glyphs = []  # (x, advance, char)
    for ch in TEXT:
        gname = cmap[ord(ch)]
        adv = glyphset[gname].width
        glyphs.append((x, adv, ch, gname))
        x += adv + TRACKING
    return glyphs, x - TRACKING, glyphset


def _rung_and_star(font, glyphs, cap, candidate):
    """Returns (rung_rect, star) in FONT units (y up, baseline 0)."""
    # The second A = LADDER's A (7th glyph incl. the space).
    a_entries = [g for g in glyphs if g[2] == 'A']
    ax, aw = a_entries[1][0], a_entries[1][1]
    acx = ax + aw / 2.0
    # Poppins-Bold A heuristics (1000 UPM): crossbar band ~y120..260, counter
    # apex ~y610, stem thickness ~110. The extra rung sits above the real
    # crossbar, spanning rail-to-rail at its own height (rails slope inward).
    y0, y1 = 345.0, 455.0
    counter_half = 140.0 * max(0.0, (610.0 - (y0 + y1) / 2.0)) / 350.0
    half = counter_half + 110.0
    rung = (acx - half, y0, 2 * half, y1 - y0)

    star = None
    if candidate == 'A':
        space = [g for g in glyphs if g[2] == ' '][0]
        gap_cx = space[0] + space[1] / 2.0
        star = (gap_cx, cap * 0.55, 120.0, 46.0)
    elif candidate == 'B':
        last = glyphs[-1]
        star = (last[0] + last[1] + 60.0, cap * 0.98, 95.0, 36.0)
    return rung, star


def logotype_svg(candidate):
    font = TTFont(FONT_PATH)
    cap = font['OS/2'].sCapHeight or 700
    glyphs, total, glyphset = _layout(font)
    rung, star = _rung_and_star(font, glyphs, cap, candidate)

    pad = cap * 0.16
    vb_w = total + (220 if candidate == 'B' else 0) + pad
    vb_h = cap + 2 * pad
    baseline = cap + pad  # y-down viewBox; glyphs flipped about the baseline

    parts = [f'<defs><linearGradient id="g" gradientUnits="userSpaceOnUse" '
             f'x1="0" y1="{pad:.0f}" x2="0" y2="{baseline:.0f}">'
             f'<stop offset="0" stop-color="#54A8B4"/>'
             f'<stop offset="1" stop-color="#8E78C8"/></linearGradient></defs>']

    for (x, adv, ch, gname) in glyphs:
        if ch == ' ':
            continue
        pen = SVGPathPen(glyphset)
        glyphset[gname].draw(pen)
        d = pen.getCommands()
        parts.append(f'<path transform="translate({x:.1f},{baseline:.1f}) scale(1,-1)" d="{d}"/>')

    # (The extra A-rung rect is retired — it read as a glitch on the A, not a ladder motif.)

    if star is not None:
        scx, scy, R, r = star
        pts = ' '.join(f'{px:.1f},{baseline - py_local:.1f}' for px, py_local in
                       [(scx + rad * math.cos(math.radians(90 - i * 45)),
                         scy + rad * math.sin(math.radians(90 - i * 45)))
                        for i, rad in ((i, R if i % 2 == 0 else r) for i in range(8))])
        parts.append(f'<polygon points="{pts}"/>')

    body = ''.join(parts)
    return (f'<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 {vb_w:.0f} {vb_h:.0f}" '
            f'fill="url(#g)">{body}</svg>')


def logotype_png(path):
    """Candidate A as the 4x raster masthead (target row 940x130 -> 3760x520)."""
    font_tt = TTFont(FONT_PATH)
    cap = font_tt['OS/2'].sCapHeight or 700
    glyphs, total, _ = _layout(font_tt)
    rung, star = _rung_and_star(font_tt, glyphs, cap, 'A')

    H2 = 1040                      # 2x supersample of the 520px target
    pad = int(H2 * 0.08)
    cap_px = H2 - 2 * pad
    fsize = int(round(cap_px * 1000.0 / cap))
    f = ImageFont.truetype(FONT_PATH, fsize)
    upx = fsize / 1000.0           # font units -> px
    track_px = TRACKING * upx

    widths = [f.getlength(ch) for ch in TEXT]
    W2 = int(math.ceil(sum(widths) + track_px * (len(TEXT) - 1))) + 2 * pad
    baseline = pad + cap_px

    mask = Image.new('L', (W2, H2), 0)
    d = ImageDraw.Draw(mask)
    x = float(pad)
    xs = []
    for ch, wch in zip(TEXT, widths):
        xs.append(x)
        if ch != ' ':
            d.text((x, baseline), ch, font=f, fill=255, anchor='ls')
        x += wch + track_px

    # Star uses the same font-unit geometry, mapped through xs. (The extra A-rung
    # is retired — it read as a glitch on the A at masthead size.)
    sp_idx = TEXT.index(' ')
    scx = xs[sp_idx] + widths[sp_idx] / 2.0
    _, scy_u, R_u, r_u = star
    scy = baseline - scy_u * upx
    pts = []
    for i in range(8):
        rad = (R_u if i % 2 == 0 else r_u) * upx
        ang = math.radians(90 - i * 45)
        pts.append((scx + rad * math.cos(ang), scy - rad * math.sin(ang)))
    d.polygon(pts, fill=255)

    grad = Image.new('RGB', (1, H2))
    for y in range(H2):
        t = min(1.0, max(0.0, (y - pad) / max(1, cap_px)))
        c = tuple(int(round(AQUA[i] + (PERIWINKLE[i] - AQUA[i]) * t)) for i in range(3))
        grad.putpixel((0, y), c)
    out = grad.resize((W2, H2)).convert('RGBA')
    out.putalpha(mask)

    out = out.resize((W2 // 2, H2 // 2), Image.LANCZOS)
    if out.width > 3760:
        out = out.resize((3760, int(out.height * 3760 / out.width)), Image.LANCZOS)
    out.save(path)


# ---------------------------------------------------------------- Unity metas

META_TEMPLATE = """fileFormatVersion: 2
guid: {guid}
ScriptedImporter:
  internalIDToNameTable: []
  externalObjects: {{}}
  serializedVersion: 2
  userData:
  assetBundleName:
  assetBundleVariant:
  script: {{fileID: 12408, guid: 0000000000000000e000000000000000, type: 0}}
  svgType: 1
  texturedSpriteMeshType: 0
  svgPixelsPerUnit: 100
  gradientResolution: 64
  alignment: 0
  customPivot: {{x: 0, y: 0}}
  generatePhysicsShape: 0
  viewportOptions: 0
  preserveViewport: 0
  advancedMode: 0
  tessellationMode: 1
  predefinedResolutionIndex: 1
  targetResolution: 1080
  resolutionMultiplier: 1
  stepDistance: 10
  samplingStepDistance: 100
  maxCordDeviationEnabled: 0
  maxCordDeviation: 1
  maxTangentAngleEnabled: 0
  maxTangentAngle: 5
  keepTextureAspectRatio: 1
  textureSize: 256
  textureWidth: 256
  textureHeight: 256
  wrapMode: 0
  filterMode: 1
  sampleCount: 4
  preserveSVGImageAspect: 0
  useSVGPixelsPerUnit: 0
  spriteData:
    TessellationDetail: 0
    SpriteName: {name}
    SpritePivot: {{x: 0.5, y: 0.5}}
    SpriteAlignment: 0
    SpriteBorder: {{x: 0, y: 0, z: 0, w: 0}}
    SpriteRect:
      serializedVersion: 2
      x: 0
      y: 0
      width: 256
      height: 256
    SpriteID: {sprite_id}
    PhysicsOutlines: []
"""


def stable_hex(seed):
    return hashlib.md5(seed.encode('utf-8')).hexdigest()[:32]


def write_svg(name, svg_text):
    path = os.path.join(SVG_DIR, name + '.svg')
    with open(path, 'w', encoding='utf-8', newline='\n') as fh:
        fh.write(svg_text)
    meta = os.path.join(path + '.meta')
    if not os.path.exists(meta):  # never churn an existing guid
        with open(meta, 'w', encoding='utf-8', newline='\n') as fh:
            fh.write(META_TEMPLATE.format(
                guid=stable_hex('starladder-svg-' + name),
                name=name,
                sprite_id=stable_hex('starladder-sprite-' + name)))
    print('  svg  ' + os.path.relpath(path, ROOT))


def main():
    os.makedirs(SVG_DIR, exist_ok=True)
    os.makedirs(PNG_DIR, exist_ok=True)

    print('icons:')
    for name, prims in ICONS.items():
        write_svg(name, icon_svg(prims))
        png_path = os.path.join(PNG_DIR, name + '.png')
        icon_png(prims, png_path)
        print('  png  ' + os.path.relpath(png_path, ROOT))

    print('logotype:')
    write_svg('StarLadderLogotype', logotype_svg('A'))
    write_svg('StarLadderLogotype_CandidateB', logotype_svg('B'))
    write_svg('StarLadderLogotype_CandidateC', logotype_svg('C'))
    png_path = os.path.join(PNG_DIR, 'StarLadderLogotype.png')
    logotype_png(png_path)
    print('  png  ' + os.path.relpath(png_path, ROOT))
    print('done.')


if __name__ == '__main__':
    main()
