"""Move effects. Hi-res textures from Assets/Fx are pixelized so they sit in the 2x-pixel world."""
from __future__ import annotations

import math
from functools import lru_cache

from PIL import Image, ImageDraw

from pixel import ASSETS, H, W, SPARK, add_glow, clamp01, draw, dust_burst, lerp_color, paste, pixel_circle, pixelize, rng

FIRE_RAMP = [(255, 255, 220), (255, 230, 90), (255, 150, 40), (220, 70, 30), (90, 50, 50)]


def ramp(colors, k):
    k = clamp01(k) * (len(colors) - 1)
    i = min(int(k), len(colors) - 2)
    return lerp_color(colors[i], colors[i + 1], k - i)


# ---------------------------------------------------------------- lightning

def _jagged(p0, p1, seed, segs=10, amp=10.0):
    r = rng("bolt", seed)
    (x0, y0), (x1, y1) = p0, p1
    dx, dy = x1 - x0, y1 - y0
    ln = math.hypot(dx, dy) or 1
    nx, ny = -dy / ln, dx / ln
    pts = [p0]
    for i in range(1, segs):
        k = i / segs
        off = r.uniform(-amp, amp) * math.sin(k * math.pi) ** 0.5
        pts.append((x0 + dx * k + nx * off, y0 + dy * k + ny * off))
    pts.append(p1)
    return [(int(x) // 2 * 2, int(y) // 2 * 2) for x, y in pts]


def lightning(canvas, p0, p1, seed, color=SPARK, core=(255, 250, 220), width=2, glow=True, amp=10.0, segs=10,
              branches=2):
    pts = _jagged(p0, p1, seed, segs, amp)
    if glow:
        layer = Image.new("RGBA", (W, H), (0, 0, 0, 0))
        gd = ImageDraw.Draw(layer)
        gd.line(pts, fill=color + (70,), width=width * 5)
        gd.line(pts, fill=color + (140,), width=width * 3)
        add_glow(canvas, layer, 0.9)
    d = draw(canvas)
    d.line(pts, fill=color + (255,), width=width + 2)
    d.line(pts, fill=core + (255,), width=width)
    r = rng("branch", seed)
    for b in range(branches):
        i = r.randrange(1, len(pts) - 1)
        a = r.uniform(0, math.tau)
        ln = r.uniform(10, 24)
        q = (pts[i][0] + math.cos(a) * ln, pts[i][1] + math.sin(a) * ln)
        bp = _jagged(pts[i], q, (seed, b), 4, 4)
        d.line(bp, fill=color + (230,), width=2)


def sky_strike(canvas, x, ground, age, seed, color=SPARK):
    """Lightning from the top of the screen into ground; age in seconds."""
    if age < 0 or age > 0.6:
        return
    if age < 0.22 and int(age * 30) % 3 != 2:
        lightning(canvas, (x + rng("sx", seed).uniform(-30, 30), -10), (x, ground), (seed, int(age * 30) // 2),
                  color, width=4, amp=14, segs=12, branches=3)
    if age < 0.35:
        k = age / 0.35
        layer = Image.new("RGBA", (W, H), (0, 0, 0, 0))
        ImageDraw.Draw(layer).ellipse((x - 40, ground - 20, x + 40, ground + 20), fill=color + (int(160 * (1 - k)),))
        add_glow(canvas, layer)
    dust_burst(canvas, x, ground - 4, age, ("strike", seed), n=18, speed=110, colors=(SPARK, (255, 255, 255)),
               life=0.5, gravity=260)


# ---------------------------------------------------------------- textures

@lru_cache(maxsize=None)
def _fire_frame(idx: int, size: int, color: tuple) -> Image.Image:
    sheet = Image.open(ASSETS / "Fx" / "Fire.png").convert("RGBA")
    cell = sheet.width // 4
    f = sheet.crop(((idx % 4) * cell, (idx // 4) * cell, (idx % 4 + 1) * cell, (idx // 4 + 1) * cell))
    lum = f.convert("L")
    tinted = Image.new("RGBA", f.size, color + (0,))
    tinted.putalpha(lum)
    return pixelize(tinted, (size, size), 3)


@lru_cache(maxsize=None)
def _explosion_frame(idx: int, size: int) -> Image.Image:
    sheet = Image.open(ASSETS / "Fx" / "Kenney" / "ExplosionStrip.png").convert("RGBA")
    n = sheet.width // sheet.height
    fw = sheet.width // n
    return pixelize(sheet.crop((idx * fw, 0, (idx + 1) * fw, sheet.height)), (size, size), 3)


def explosion(canvas, x, y, age, size=64, seed=0):
    if age < 0 or age > 0.6:
        return
    n = 9
    idx = min(n - 1, int(age / 0.6 * n))
    paste(canvas, _explosion_frame(idx, size), x, y, "mm")
    if age < 0.12:
        layer = Image.new("RGBA", (W, H), (0, 0, 0, 0))
        r = size * 0.7
        ImageDraw.Draw(layer).ellipse((x - r, y - r, x + r, y + r), fill=(255, 220, 140, 150))
        add_glow(canvas, layer)
    dust_burst(canvas, x, y, age, ("exp", seed), n=16, speed=size * 2.2, colors=FIRE_RAMP[:4], life=0.55, gravity=160)


def fire_stream(canvas, p0, p1, t, seed=0, strength=1.0, rate=2.6, n=26):
    """Flamethrower: puffs travel p0->p1, growing and cooling; uses Fire.png frames."""
    (x0, y0), (x1, y1) = p0, p1
    r = rng("fire", seed)
    jit = [(r.uniform(-1, 1), r.uniform(-1, 1), r.randrange(16)) for _ in range(n)]
    layer = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    blobs = Image.new("RGBA", (W // 2, H // 2), (0, 0, 0, 0))
    bd = ImageDraw.Draw(blobs)
    puffs = []
    for i in range(n):
        ph = (t * rate + i / n) % 1.0
        if ph > strength:
            continue
        jx, jy, fi = jit[i]
        px = x0 + (x1 - x0) * ph + jx * 6 * ph
        py = y0 + (y1 - y0) * ph + jy * 18 * ph - 10 * ph * ph
        puffs.append((ph, px, py, fi))
    for ph, px, py, fi in sorted(puffs, reverse=True):
        rr = (3 + 13 * ph) / 2
        col = ramp(FIRE_RAMP, ph * 0.95 + 0.05)
        bd.ellipse((px / 2 - rr, py / 2 - rr, px / 2 + rr, py / 2 + rr), fill=col + (int(255 * (1 - ph ** 3)),))
        size = int(16 + 36 * ph) // 2 * 2
        img = _fire_frame((fi + int(t * 20)) % 16, size, ramp(FIRE_RAMP, ph * 0.6))
        paste(layer, img, px, py, "mm")
    comp_blobs = blobs.resize((W, H), Image.NEAREST)
    canvas.paste(comp_blobs, (0, 0), comp_blobs)
    add_glow(canvas, layer, 1.0)
    d = draw(canvas)
    for i in range(14):
        ph = (t * 3.1 + i / 14) % 1.0
        px = x0 + (x1 - x0) * ph + math.sin(i * 7 + t * 9) * 10 * ph
        py = y0 + (y1 - y0) * ph + math.cos(i * 5 + t * 7) * 16 * ph
        c = ramp(FIRE_RAMP, ph)
        d.rectangle((int(px), int(py), int(px) + 1, int(py) + 1), fill=c + (255,))


def water_beam(canvas, x0, y, x1, t, width=22, k_open=1.0):
    """Hydro Pump style beam with rippling bands and a frothy head."""
    if k_open <= 0:
        return
    w = width * k_open
    d = draw(canvas)
    layer = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    ld = ImageDraw.Draw(layer)
    ld.rectangle((x0, y - w * 0.9, x1, y + w * 0.9), fill=(60, 140, 255, 90))
    add_glow(canvas, layer, 0.8)
    for x in range(int(x0), int(x1), 2):
        wob = math.sin(x * 0.12 - t * 30) * 2 + math.sin(x * 0.05 - t * 12) * 2
        hw = max(2.0, w / 2 + wob)
        d.rectangle((x, int(y - hw) // 2 * 2, x + 1, int(y + hw) // 2 * 2), fill=(40, 110, 230, 255))
        hw2 = hw * 0.62
        d.rectangle((x, int(y - hw2) // 2 * 2, x + 1, int(y + hw2) // 2 * 2), fill=(110, 190, 255, 255))
        hw3 = hw * 0.25
        d.rectangle((x, int(y - hw3) // 2 * 2, x + 1, int(y + hw3) // 2 * 2), fill=(235, 250, 255, 255))
    for i in range(10):
        ph = (t * 2.5 + i / 10) % 1.0
        bx = x0 + (x1 - x0) * ph
        by = y + math.sin(i * 3.3) * w * 0.4
        d.rectangle((int(bx), int(by), int(bx) + 3, int(by) + 1), fill=(255, 255, 255, 200))
    for i in range(12):
        a = i / 12 * math.tau + t * 6
        rr = w * 0.7 + math.sin(t * 25 + i) * 4
        d.rectangle((int(x1 + math.cos(a) * rr * 0.6), int(y + math.sin(a) * rr), int(x1 + math.cos(a) * rr * 0.6) + 3,
                     int(y + math.sin(a) * rr) + 3), fill=(220, 245, 255, 230))


def charge_orb(canvas, x, y, k, t, color=(110, 200, 255), seed=0):
    """Particles converging into a growing orb (k: 0..1 charge)."""
    if k <= 0:
        return
    d = draw(canvas)
    r = rng("charge", seed)
    for i in range(18):
        a = r.uniform(0, math.tau)
        ph = (t * 2.0 + r.random()) % 1.0
        dist = 40 * (1 - ph)
        px, py = x + math.cos(a) * dist, y + math.sin(a) * dist
        d.rectangle((int(px), int(py), int(px) + 1, int(py) + 1), fill=color + (int(255 * ph),))
    rr = 3 + 9 * k + math.sin(t * 40) * 1.5
    layer = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    ImageDraw.Draw(layer).ellipse((x - rr * 2, y - rr * 2, x + rr * 2, y + rr * 2), fill=color + (90,))
    add_glow(canvas, layer)
    d.ellipse((x - rr, y - rr, x + rr, y + rr), fill=color + (255,))
    d.ellipse((x - rr * 0.5, y - rr * 0.5, x + rr * 0.5, y + rr * 0.5), fill=(255, 255, 255, 255))


def shockwave(canvas, x, y, age, color=(140, 255, 160), max_r=180, life=0.5, width=4):
    if age < 0 or age > life:
        return
    k = age / life
    r = max_r * (1 - (1 - k) ** 2)
    pixel_circle(canvas, x, y, r, color, width, int(255 * (1 - k)))
    if k < 0.6:
        pixel_circle(canvas, x, y, r * 0.75, (255, 255, 255), 2, int(200 * (1 - k / 0.6)))


def afterimages(canvas, img: Image.Image, path, t, n=6, spacing=0.035, color=(80, 255, 140)):
    """Draws tinted ghost copies along a time-parameterized path(t)->(x,y)."""
    from actors import silhouette
    ghost = silhouette(img, color)
    for i in range(n, 0, -1):
        tt = t - i * spacing
        if tt < 0:
            continue
        x, y = path(tt)
        paste(canvas, ghost, x, y, "mm", 0.55 * (1 - i / (n + 1)))


def psy_beam(canvas, p0, p1, t, color=(220, 120, 255), width=6):
    layer = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    ld = ImageDraw.Draw(layer)
    ld.line((p0, p1), fill=color + (110,), width=width * 3)
    add_glow(canvas, layer)
    d = draw(canvas)
    d.line((p0, p1), fill=color + (255,), width=width)
    d.line((p0, p1), fill=(255, 240, 255, 255), width=max(2, width // 3))
    (x0, y0), (x1, y1) = p0, p1
    for i in range(4):
        ph = (t * 3 + i / 4) % 1.0
        px, py = x0 + (x1 - x0) * ph, y0 + (y1 - y0) * ph
        pixel_circle(canvas, px, py, width * 0.8, (255, 255, 255), 2, 140)
