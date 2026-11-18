"""Procedural Terraria-like surface: banded sky, parallax hills, 16px tiles drawn with 2x art pixels."""
from __future__ import annotations

import math
from functools import lru_cache

from PIL import Image, ImageDraw

from pixel import H, W, comp, draw, rng, sparkle

TILE = 16
GROUND_Y = 208
STRIP_W = 960

THEMES = {
    "day": dict(
        sky=((70, 130, 230), (160, 210, 255)), hills=((120, 170, 210), (70, 140, 120), (40, 100, 70)),
        grass=((40, 190, 60), (90, 225, 90), (25, 140, 45)), dirt=((151, 107, 75), (120, 84, 58), (178, 130, 92)),
        stone=((128, 128, 128), (100, 100, 105), (150, 150, 155)), leaves=((40, 150, 50), (70, 190, 70), (25, 105, 40)),
        trunk=((120, 82, 50), (90, 60, 36)), sun=(255, 240, 150), moon=False, stars=False, shade=1.0, cloud=(255, 255, 255),
    ),
    "night": dict(
        sky=((6, 8, 30), (30, 40, 90)), hills=((30, 40, 80), (20, 40, 50), (12, 28, 30)),
        grass=((20, 100, 50), (40, 130, 70), (12, 70, 35)), dirt=((80, 60, 55), (62, 46, 42), (98, 74, 66)),
        stone=((70, 72, 85), (55, 56, 68), (85, 88, 100)), leaves=((18, 80, 45), (30, 105, 60), (10, 55, 32)),
        trunk=((70, 52, 40), (50, 36, 28)), sun=None, moon=True, stars=True, shade=0.6, cloud=(70, 80, 120),
    ),
    "dusk": dict(
        sky=((60, 40, 120), (255, 150, 90)), hills=((150, 90, 130), (90, 60, 100), (50, 40, 70)),
        grass=((60, 160, 70), (110, 200, 90), (35, 115, 50)), dirt=((140, 92, 70), (110, 72, 56), (165, 112, 84)),
        stone=((118, 110, 120), (92, 86, 98), (140, 132, 142)), leaves=((50, 130, 60), (90, 170, 80), (30, 90, 45)),
        trunk=((110, 72, 48), (80, 52, 34)), sun=(255, 200, 120), moon=False, stars=False, shade=0.9, cloud=(255, 190, 170),
    ),
    "corrupt": dict(
        sky=((20, 6, 24), (120, 30, 50)), hills=((80, 30, 60), (50, 20, 45), (30, 12, 30)),
        grass=((130, 110, 200), (170, 150, 235), (90, 70, 150)), dirt=((90, 70, 90), (70, 54, 72), (110, 88, 110)),
        stone=((90, 80, 110), (70, 62, 90), (110, 100, 130)), leaves=((110, 70, 140), (140, 100, 170), (80, 50, 100)),
        trunk=((80, 60, 70), (60, 44, 52)), sun=None, moon=True, stars=True, shade=0.75, cloud=(110, 50, 80),
    ),
}


def _shade(c, k):
    return tuple(max(0, min(255, int(v * k))) for v in c)


@lru_cache(maxsize=None)
def _sky(theme: str) -> Image.Image:
    th = THEMES[theme]
    top, bot = th["sky"]
    img = Image.new("RGBA", (W, H))
    d = ImageDraw.Draw(img)
    band = 6
    for y in range(0, H, band):
        k = min(1.0, y / (H * 0.85))
        k = round(k * 14) / 14
        c = tuple(int(a + (b - a) * k) for a, b in zip(top, bot))
        d.rectangle((0, y, W, y + band - 1), fill=c + (255,))
    if th["sun"]:
        _blocky_disc(img, 390, 52, 18, th["sun"], halo=True)
    if th["moon"]:
        _blocky_disc(img, 392, 46, 14, (235, 235, 210), halo=False)
        d.rectangle((384, 38, 388, 42), fill=(205, 205, 185, 255))
        d.rectangle((396, 48, 400, 54), fill=(205, 205, 185, 255))
    return img


def _blocky_disc(img: Image.Image, cx, cy, r, color, halo):
    rings = [(r + 10, 50), (r + 5, 90)] if halo else []
    for rr, a in rings + [(r, 255)]:
        layer = Image.new("RGBA", img.size, (0, 0, 0, 0))
        d = ImageDraw.Draw(layer)
        for y in range(-rr, rr + 1, 2):
            hw = int(math.sqrt(max(0, rr * rr - y * y))) // 2 * 2
            if hw > 0:
                d.rectangle((cx - hw, cy + y, cx + hw - 1, cy + y + 1), fill=color + (a,))
        img.alpha_composite(layer)


@lru_cache(maxsize=None)
def _hill_layer(theme: str, idx: int) -> Image.Image:
    """Art-res (half) silhouette layer, wraps horizontally, upscaled 2x."""
    th = THEMES[theme]
    color = th["hills"][idx]
    aw, ah = STRIP_W // 2, H // 2
    img = Image.new("RGBA", (aw, ah), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    r = rng("hill", theme, idx)
    phases = [r.uniform(0, math.tau) for _ in range(3)]
    base = [62, 76, 88][idx]
    amp = [16, 11, 7][idx]
    for x in range(aw):
        u = x / aw * math.tau
        h = base - amp * (0.55 * math.sin(u * 2 + phases[0]) + 0.3 * math.sin(u * 5 + phases[1])
                          + 0.15 * math.sin(u * 11 + phases[2]))
        d.line((x, int(h), x, ah), fill=color + (255,))
    if idx >= 1:
        tr = rng("bgtree", theme, idx)
        for _ in range(9 if idx == 1 else 6):
            tx = tr.randrange(0, aw)
            u = tx / aw * math.tau
            gy = base - amp * (0.55 * math.sin(u * 2 + phases[0]) + 0.3 * math.sin(u * 5 + phases[1])
                               + 0.15 * math.sin(u * 11 + phases[2]))
            th_ = tr.randint(14, 26) if idx == 1 else tr.randint(10, 18)
            tw = 2 if idx == 1 else 3
            d.rectangle((tx, gy - th_, tx + tw - 1, gy), fill=color + (255,))
            cr = tr.randint(5, 8)
            d.ellipse((tx - cr + 1, gy - th_ - cr, tx + cr, gy - th_ + cr // 2), fill=color + (255,))
    return img.resize((STRIP_W, H), Image.NEAREST)


def _tile_art(kind: str, th: dict, r) -> Image.Image:
    """8x8 art-pixel tile."""
    t = Image.new("RGBA", (8, 8))
    px = t.load()
    pal = th["dirt"] if kind != "stone" else th["stone"]
    for y in range(8):
        for x in range(8):
            v = r.random()
            c = pal[0] if v < 0.72 else pal[1] if v < 0.9 else pal[2]
            px[x, y] = c + (255,)
    if kind == "grass":
        g = th["grass"]
        for x in range(8):
            depth = 2 + (1 if r.random() < 0.35 else 0)
            for y in range(depth):
                px[x, y] = (g[1] if y == 0 and r.random() < 0.5 else g[0]) + (255,)
            px[x, depth] = g[2] + (255,) if r.random() < 0.5 else px[x, depth]
    return t


def column_height(col: int) -> int:
    """Surface tile row for a world column (flat stage in the middle, gentle bumps outside)."""
    r = rng("col", col // 3)
    x = col * TILE
    if 48 <= (x % STRIP_W) <= 432:
        return GROUND_Y // TILE
    return GROUND_Y // TILE - (1 if r.random() < 0.45 else 0)


@lru_cache(maxsize=None)
def _ground(theme: str, trees: bool = True) -> Image.Image:
    th = THEMES[theme]
    aw, ah = STRIP_W // 2, H // 2
    art = Image.new("RGBA", (aw, ah), (0, 0, 0, 0))
    r = rng("ground", theme)
    d = ImageDraw.Draw(art)
    cols = STRIP_W // TILE
    # trees behind the tile layer
    for col in range(cols):
        if r.random() < 0.13 and not (5 <= col % cols <= 24 and r.random() < 0.7) and trees:
            _tree(d, col * 8 + 4, column_height(col) * 8, th, rng("tree", theme, col))
    for col in range(cols):
        top = column_height(col)
        for row in range(top, H // TILE + 1):
            kind = "grass" if row == top else ("stone" if row >= top + 3 and r.random() < 0.55 + (row - top) * 0.1 else "dirt")
            art.alpha_composite(_tile_art(kind, th, r), (col * 8, row * 8))
        g = th["grass"]
        for gx in range(col * 8, col * 8 + 8):
            if r.random() < 0.35:
                hgt = r.randint(1, 3)
                d.line((gx, top * 8 - hgt, gx, top * 8 - 1), fill=g[r.randint(0, 1)] + (255,))
        if r.random() < 0.12:
            fx = col * 8 + r.randint(1, 6)
            fc = [(255, 90, 90), (255, 230, 90), (180, 140, 255), (255, 255, 255)][r.randint(0, 3)]
            d.line((fx, top * 8 - 3, fx, top * 8 - 1), fill=g[2] + (255,))
            d.point((fx, top * 8 - 4), fill=fc + (255,))
    big = art.resize((STRIP_W, H), Image.NEAREST)
    if th["shade"] != 1.0:
        k = th["shade"]
        rgb = big.convert("RGB").point(lambda v: int(v * k))
        rgb.putalpha(big.getchannel("A"))
        big = rgb
    return big


def _tree(d: ImageDraw.ImageDraw, x: int, ground: int, th: dict, r):
    hgt = r.randint(22, 38)
    tc, td = th["trunk"]
    d.rectangle((x - 2, ground - hgt, x + 1, ground - 1), fill=tc + (255,))
    d.rectangle((x + 1, ground - hgt, x + 1, ground - 1), fill=td + (255,))
    d.rectangle((x - 4, ground - 2, x + 3, ground - 1), fill=tc + (255,))
    for _ in range(2):
        by = ground - r.randint(8, hgt - 6)
        side = r.choice((-1, 1))
        d.rectangle((x + side * 3 - 1, by, x + side * 3, by + 1), fill=tc + (255,))
        d.ellipse((x + side * 5 - 2, by - 3, x + side * 5 + 2, by + 1), fill=th["leaves"][0] + (255,))
    lc, lh, ld = th["leaves"]
    cy = ground - hgt
    for i in range(7):
        ox, oy = r.randint(-8, 8), r.randint(-9, 2)
        rr = r.randint(5, 8)
        d.ellipse((x + ox - rr, cy + oy - rr, x + ox + rr, cy + oy + rr), fill=ld + (255,))
    for i in range(6):
        ox, oy = r.randint(-6, 6), r.randint(-10, 0)
        rr = r.randint(4, 6)
        d.ellipse((x + ox - rr, cy + oy - rr - 1, x + ox + rr, cy + oy + rr - 1), fill=lc + (255,))
    for i in range(5):
        ox, oy = r.randint(-6, 5), r.randint(-12, -3)
        d.rectangle((x + ox, cy + oy, x + ox + 1, cy + oy), fill=lh + (255,))


def _clouds(canvas: Image.Image, theme: str, t: float):
    th = THEMES[theme]
    d = draw(canvas)
    r = rng("clouds", theme)
    for i in range(5):
        y = r.randint(16, 90)
        speed = r.uniform(4, 10)
        w = r.randint(30, 60)
        x = (r.uniform(0, W + 120) + t * speed) % (W + 120) - 60
        x, y = int(x) // 2 * 2, y // 2 * 2
        c = th["cloud"] + (220,)
        d.rectangle((x, y, x + w, y + 8), fill=c)
        d.rectangle((x + 6, y - 6, x + w // 2 + 6, y), fill=c)
        d.rectangle((x + w // 3, y - 10, x + w // 3 + 14, y - 4), fill=c)


def draw_world(canvas: Image.Image, theme: str = "day", cam_x: float = 0.0, t: float = 0.0, ground: bool = True):
    canvas.paste(_sky(theme).convert("RGB"))
    th = THEMES[theme]
    if th["stars"]:
        r = rng("stars", theme)
        for i in range(40):
            sparkle(canvas, r.randint(0, W), r.randint(0, 130), (t * r.uniform(0.3, 0.8) + r.random()) % 1.0,
                    (255, 255, 220), 1 + (i % 3 == 0))
    _clouds(canvas, theme, t)
    for idx, fac in enumerate((0.15, 0.3, 0.5)):
        layer = _hill_layer(theme, idx)
        _blit_wrap(canvas, layer, cam_x * fac)
    if ground:
        draw_ground(canvas, theme, cam_x)


def draw_ground(canvas: Image.Image, theme: str = "day", cam_x: float = 0.0, trees: bool = True):
    _blit_wrap(canvas, _ground(theme, trees), cam_x)


def _blit_wrap(canvas: Image.Image, strip: Image.Image, off: float):
    off = int(off) % STRIP_W
    part = strip.crop((off, 0, min(STRIP_W, off + W), H))
    comp(canvas, part, (0, 0))
    if off + W > STRIP_W:
        rest = strip.crop((0, 0, off + W - STRIP_W, H))
        comp(canvas, rest, (STRIP_W - off, 0))


def ground_y(screen_x: float, cam_x: float = 0.0) -> int:
    col = int((screen_x + cam_x) // TILE) % (STRIP_W // TILE)
    return column_height(col) * TILE
