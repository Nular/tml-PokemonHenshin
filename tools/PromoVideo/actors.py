"""Characters: mod form sprites, a code-drawn Terraria player, slimes, Eye of Cthulhu and a giant boss silhouette."""
from __future__ import annotations

import json
import math
from functools import lru_cache

import numpy as np
from PIL import Image, ImageDraw, ImageOps

from pixel import ASSETS, BEAT, WHITE, clamp01, draw, draw_text, paste

# ---------------------------------------------------------------- form sprites

NAMES = {
    "L01_F01": "小火龙", "L01_F02": "火恐龙", "L01_F03": "喷火龙",
    "L02_F01": "杰尼龟", "L02_F02": "卡咪龟", "L02_F03": "水箭龟",
    "L03_F01": "妙蛙种子", "L03_F02": "妙蛙草", "L03_F03": "妙蛙花",
    "L04_F01": "皮卡丘", "L04_F02": "雷丘",
    "L14_F01": "超梦", "L16_F01": "洛奇亚", "L17_F01": "烈空坐",
}

ALL_FORMS = sorted(p.stem for p in (ASSETS / "Forms").glob("L??_F??.png"))


def shrink(img: Image.Image, k: float) -> Image.Image:
    """Downscale pixel art cleanly: high-quality resample then hard alpha threshold."""
    if k == 1:
        return img
    w, h = img.size
    out = img.resize((max(1, round(w * k)), max(1, round(h * k))), Image.LANCZOS if k < 1 else Image.NEAREST)
    a = np.asarray(out.getchannel("A"))
    out.putalpha(Image.fromarray(np.where(a > 110, 255, 0).astype(np.uint8)))
    return out


@lru_cache(maxsize=None)
def form(code: str, face_right: bool = True, k: float = 1.0) -> Image.Image:
    img = Image.open(ASSETS / "Forms" / f"{code}.png").convert("RGBA")
    img = img.crop(img.getbbox())
    if face_right:
        img = ImageOps.mirror(img)
    return shrink(img, k)


def silhouette(img: Image.Image, color=(255, 255, 255)) -> Image.Image:
    out = Image.new("RGBA", img.size, color + (0,))
    out.putalpha(img.getchannel("A"))
    return out


def outlined(img: Image.Image, color=(255, 255, 255)) -> Image.Image:
    """Sprite with a 1px colored outline (used for highlight / selection)."""
    a = np.asarray(img.getchannel("A")) > 0
    pad = np.pad(a, 1)
    o = np.zeros_like(pad)
    for dy in (-1, 0, 1):
        for dx in (-1, 0, 1):
            o |= np.roll(np.roll(pad, dy, 0), dx, 1)
    rgba = np.zeros(pad.shape + (4,), np.uint8)
    rgba[o] = color + (255,)
    base = Image.fromarray(rgba, "RGBA")
    base.alpha_composite(img, (1, 1))
    return base


@lru_cache(maxsize=None)
def icon(code: str, size: int = 22) -> Image.Image:
    img = form(code, False)
    k = min(1.0, size / max(img.size))
    return shrink(img, k)


@lru_cache(maxsize=None)
def _clip_frames(name: str, k: float, face_right: bool) -> tuple[list[Image.Image], list[float]]:
    meta = json.loads((ASSETS / "Forms" / "Locomotion" / f"L04_F01_{name}.json").read_text("utf-8"))
    sheet = Image.open(ASSETS / "Forms" / "Locomotion" / f"L04_F01_{name}.png").convert("RGBA")
    fw, fh = meta["frameWidth"], meta["frameHeight"]
    frames = []
    for i in range(meta["frameCount"]):
        f = sheet.crop((i * fw, 0, (i + 1) * fw, fh))
        if not face_right:
            f = ImageOps.mirror(f)
        frames.append(shrink(f, k))
    return frames, [d / 1000 for d in meta["durationsMs"]]


def pikachu_frame(state: str, t: float, face_right: bool = True) -> Image.Image:
    k = {"Idle": 0.62, "Run": 0.36}[state]
    frames, durs = _clip_frames(state, k, face_right)
    total = sum(durs)
    tt = t % total
    for f, d in zip(frames, durs):
        if tt < d:
            return f
        tt -= d
    return frames[-1]


def draw_form(canvas, code, x, feet_y, t=0.0, face_right=True, k=1.0, bob=True, alpha=1.0, sil=None, anim=None):
    """Draw a form standing on feet_y (bottom-center anchor) with Terraria-like idle bob."""
    if code == "L04_F01" and anim:
        img = pikachu_frame(anim, t, face_right)
    else:
        img = form(code, face_right, k)
    if sil is not None:
        img = silhouette(img, sil)
    dy = -2 * round((math.sin(t * math.tau / (BEAT * 2)) + 1) / 2) if bob and not anim else 0
    paste(canvas, img, x, feet_y + dy, "mb", alpha)
    return img


def name_tag(canvas, text, x, y, color=WHITE, alpha=1.0):
    draw_text(canvas, text, x, y, color, 1, "mb", alpha)


# ---------------------------------------------------------------- Terraria player

PLAYER_PAL = {
    "H": (130, 78, 40), "h": (96, 56, 28), "S": (240, 182, 140), "s": (206, 142, 108),
    "W": (255, 255, 255), "K": (40, 50, 110), "T": (72, 140, 222), "t": (48, 98, 170),
    "P": (70, 64, 120), "p": (50, 46, 92), "B": (96, 62, 38),
}
_HEAD = [
    "...hHHHh..",
    "..hHHHHHH.",
    ".hHHHHHHHH",
    ".hHHSSSSSH",
    ".hHSSSSWKS",
    ".hHSSSSWKS",
    "..hSSSSSSS",
    "...sSSSSs.",
    "....sss...",
    "...TTTTT..",
    "..tTTTTTT.",
    "..tTTTTTT.",
    "..tTTTTTT.",
    "..tTTTTTT.",
    "...PPPPP..",
    "...PPPPP..",
]
_LEGS = {
    "stand": ["...Pp.PP..", "...Pp.PP..", "...Pp.PP..", "..BBB.BBB.", "..BBB.BBB."],
    "stride": ["..PPp.PP..", ".PP...PPP.", ".PP....PP.", "BBB....BBB", "BB......BB"],
    "jump": ["..PPp.PP..", "..PP..PP..", ".BBB...PP.", ".BB...BBB.", "......BB.."],
}


@lru_cache(maxsize=None)
def _player_body(legs: str, palette_key: tuple) -> Image.Image:
    pal = dict(PLAYER_PAL)
    pal.update(dict(palette_key))
    rows = _HEAD + _LEGS[legs]
    img = Image.new("RGBA", (10, len(rows)), (0, 0, 0, 0))
    px = img.load()
    for y, row in enumerate(rows):
        for x, ch in enumerate(row):
            if ch in pal:
                px[x, y] = pal[ch] + (255,)
    return img.resize((20, len(rows) * 2), Image.NEAREST)


def _arm(d: ImageDraw.ImageDraw, sx, sy, angle, sleeve, skin, length=5):
    pts = []
    for i in range(length):
        px = sx + math.cos(angle) * i * 2
        py = sy + math.sin(angle) * i * 2
        pts.append((int(px) // 2 * 2, int(py) // 2 * 2))
    for i, (px, py) in enumerate(pts):
        c = sleeve if i < 2 else skin
        d.rectangle((px, py, px + 1, py + 1), fill=c + (255,))
    return pts[-1]


@lru_cache(maxsize=None)
def item_sprite(kind: str) -> Image.Image:
    """Tiny pixel items drawn pointing up-right (art 2x)."""
    grids = {
        "sword": [
            "..........WW",
            ".........WLW",
            "........WLW.",
            ".......WLW..",
            "......WLW...",
            ".....WLW....",
            "..G.WLW.....",
            "..GGLW......",
            "...GG.......",
            "..BrGG......",
            ".Br..G......",
            "Br..........",
        ],
        "pickaxe": [
            "....LLLL....",
            "..LL....LL..",
            ".L....B...L.",
            "L....B.....L",
            ".....B......",
            "....B.......",
            "...B........",
            "..B.........",
            ".B..........",
            "B...........",
        ],
        "torch": ["..Y.", ".YOY", ".OYO", "..B.", "..B.", "..B.", "..B."],
        "potion": ["..cc..", "..WW..", ".WRRW.", "WRRRRW", "WRRRRW", ".WRRW."],
        "ball": ["..kkkk..", ".kRRRRk.", "kRRhRRRk", "kkkWWkkk", "kWkWWkWk", "kWWkkWWk", ".kWWWWk.", "..kkkk.."],
    }
    pal = {"W": (230, 230, 240), "L": (170, 180, 200), "G": (220, 170, 60), "B": (120, 80, 50), "r": (90, 60, 40),
           "Y": (255, 240, 120), "O": (255, 150, 40), "R": (230, 40, 70), "c": (140, 100, 70),
           "k": (30, 30, 40), "h": (255, 170, 170)}
    rows = grids[kind]
    img = Image.new("RGBA", (len(rows[0]), len(rows)), (0, 0, 0, 0))
    px = img.load()
    for y, row in enumerate(rows):
        for x, ch in enumerate(row):
            if ch in pal:
                px[x, y] = pal[ch] + (255,)
    return img.resize((img.width * 2, img.height * 2), Image.NEAREST)


def draw_player(canvas, x, feet_y, t=0.0, face=1, pose="stand", item: Image.Image | None = None,
                swing_k=0.0, palette: dict | None = None, alpha=1.0, sil=None):
    """Terraria-like player 20x42 logical. pose: stand | walk | raise | swing | jump."""
    pal_key = tuple(sorted((palette or {}).items()))
    legs = "stand"
    if pose == "walk":
        legs = "stride" if int(t / (BEAT / 2)) % 2 else "stand"
    elif pose == "jump":
        legs = "jump"
    body = _player_body(legs, pal_key)
    pal = dict(PLAYER_PAL)
    pal.update(palette or {})
    layer = Image.new("RGBA", (64, 64), (0, 0, 0, 0))
    d = ImageDraw.Draw(layer)
    ox, oy = 22, 64 - body.height
    sx, sy = ox + 10, oy + 19
    if pose == "raise":
        fa = -math.pi / 2 - 0.15
        ba = math.pi / 2 + 0.25
    elif pose == "swing":
        fa = -math.pi * 0.75 + swing_k * math.pi * 1.1
        ba = math.pi / 2 + 0.3
    else:
        sw = math.sin(t * math.tau / BEAT) * 0.35 if pose == "walk" else 0.0
        fa = math.pi / 2 - 0.25 + sw
        ba = math.pi / 2 + 0.25 - sw
    _arm(d, sx - 4, sy, ba, pal["t"], pal["s"])
    layer.alpha_composite(body, (ox, oy))
    d = ImageDraw.Draw(layer)
    if pose == "raise":
        hand = _arm(d, sx + 4, sy, -math.pi / 2 + 0.4, pal["T"], pal["S"], 9)
    else:
        hand = _arm(d, sx, sy, fa, pal["T"], pal["S"])
    if item is not None and pose in ("raise", "swing", "stand", "walk"):
        if pose == "swing":
            ang = -math.degrees(fa) - 45
            it = item.rotate(ang, Image.NEAREST, expand=True)
            layer.alpha_composite(it, (hand[0] - it.width // 2 + int(math.cos(fa) * 8),
                                       hand[1] - it.height // 2 + int(math.sin(fa) * 8)))
        elif pose == "raise":
            layer.alpha_composite(item, (hand[0] - item.width // 2 + 1, max(0, hand[1] - item.height + 2)))
        else:
            layer.alpha_composite(item, (hand[0] - 2, hand[1] - item.height + 4))
    if face < 0:
        layer = ImageOps.mirror(layer)
    if sil is not None:
        layer = silhouette(layer, sil)
    paste(canvas, layer, x, feet_y, "mb", alpha)


# ---------------------------------------------------------------- slime

_SLIME = [
    "...OOOO...",
    "..OLLLLO..",
    ".OLWWLLLO.",
    "OLLWLLLLLO",
    "OLLLLDDLLO",
    "OLLLDDDDLO",
    "OLLLLDDLLO",
    ".OOOOOOOO.",
]
SLIME_COLORS = {
    "blue": ((60, 120, 255), (30, 60, 170), (150, 200, 255), (40, 80, 200)),
    "green": ((80, 210, 80), (30, 120, 40), (190, 255, 190), (50, 160, 60)),
    "purple": ((170, 90, 230), (90, 40, 140), (230, 190, 255), (130, 60, 190)),
    "red": ((230, 70, 70), (130, 30, 30), (255, 190, 190), (180, 40, 40)),
}


@lru_cache(maxsize=None)
def _slime_img(color: str) -> Image.Image:
    L, O, Wc, D = SLIME_COLORS[color]
    pal = {"L": L + (210,), "O": O + (255,), "W": Wc + (235,), "D": D + (220,)}
    img = Image.new("RGBA", (10, 8), (0, 0, 0, 0))
    px = img.load()
    for y, row in enumerate(_SLIME):
        for x, ch in enumerate(row):
            if ch in pal:
                px[x, y] = pal[ch]
    return img.resize((20, 16), Image.NEAREST)


def slime_pos(x0, feet, t, seed=0, hop_period=1.2, hop_dx=14.0, hop_h=18.0):
    """Returns (x, y_feet, squash) for a hopping slime at time t."""
    ph = (t / hop_period + (seed * 0.37) % 1.0)
    n, f = int(ph), ph % 1.0
    air = 0.45
    if f < air:
        k = f / air
        return x0 + (n + k) * hop_dx, feet - hop_h * 4 * k * (1 - k), 1.0 + 0.15 * math.sin(k * math.pi)
    k = (f - air) / (1 - air)
    sq = 1.0 - 0.18 * math.sin(k * math.pi)
    return x0 + (n + 1) * hop_dx, feet, sq


def draw_slime(canvas, x, feet_y, squash=1.0, color="blue", alpha=1.0, sil=None, k=1.0):
    img = _slime_img(color)
    if k != 1.0:
        img = img.resize((round(img.width * k), round(img.height * k)), Image.NEAREST)
    h = max(4, int(round(img.height * squash / 2) * 2))
    w = max(4, int(round(img.width * (2 - squash) / 2) * 2))
    img = img.resize((w, h), Image.NEAREST)
    if sil is not None:
        img = silhouette(img, sil)
    paste(canvas, img, x, feet_y, "mb", alpha)


# ---------------------------------------------------------------- Eye of Cthulhu

@lru_cache(maxsize=None)
def _eoc_art() -> Image.Image:
    a = Image.new("RGBA", (44, 30), (0, 0, 0, 0))
    d = ImageDraw.Draw(a)
    for i, (dy, ln) in enumerate(((-6, 14), (-2, 18), (3, 16), (7, 12))):
        y = 15 + dy
        for k in range(ln):
            yy = y + int(math.sin(k * 0.6 + i) * 1.5)
            d.rectangle((k, yy, k + 1, yy), fill=(170, 30, 40, 255) if k % 3 else (120, 20, 30, 255))
    d.ellipse((14, 2, 42, 28), fill=(235, 225, 220, 255), outline=(150, 40, 50, 255))
    d.ellipse((17, 5, 39, 21), fill=(250, 245, 240, 255))
    for (x0, y0, x1, y1) in ((18, 8, 24, 12), (20, 22, 26, 18), (34, 6, 30, 10), (36, 24, 32, 20)):
        d.line((x0, y0, x1, y1), fill=(200, 50, 60, 255))
    d.ellipse((28, 8, 40, 22), fill=(40, 90, 150, 255))
    d.ellipse((31, 11, 38, 19), fill=(15, 15, 25, 255))
    d.rectangle((32, 12, 33, 13), fill=(255, 255, 255, 255))
    return a.resize((88, 60), Image.NEAREST)


def draw_eoc(canvas, x, y, t=0.0, angle_deg=0.0, alpha=1.0, sil=None, hurt=0.0, face_left=True, k=1.0):
    img = _eoc_art()
    if face_left:
        img = ImageOps.mirror(img)
    if k != 1.0:
        img = img.resize((round(img.width * k) // 2 * 2, round(img.height * k) // 2 * 2), Image.NEAREST)
    if hurt > 0:
        ov = Image.new("RGBA", img.size, (255, 60, 60, 0))
        ov.putalpha(img.getchannel("A").point(lambda v: int(v * 0.6 * clamp01(hurt))))
        img = img.copy()
        img.alpha_composite(ov)
    img = img.rotate(angle_deg + math.sin(t * 3) * 6, Image.NEAREST, expand=True)
    if sil is not None:
        img = silhouette(img, sil)
    paste(canvas, img, x, y + math.sin(t * 2.2) * 4, "mm", alpha)


# ---------------------------------------------------------------- giant boss silhouette

@lru_cache(maxsize=None)
def _boss_art() -> Image.Image:
    """Moon-Lord-like silhouette (art res), dark with glowing eyes."""
    aw, ah = 150, 125
    m = Image.new("L", (aw, ah), 0)
    d = ImageDraw.Draw(m)
    cx = aw // 2
    # torso + shoulders
    d.polygon([(cx - 34, 72), (cx + 34, 72), (cx + 26, ah), (cx - 26, ah)], fill=255)
    d.ellipse((cx - 44, 62, cx - 18, 86), fill=255)
    d.ellipse((cx + 18, 62, cx + 44, 86), fill=255)
    # head dome + face tentacles
    d.ellipse((cx - 20, 6, cx + 20, 58), fill=255)
    for i in range(9):
        x = cx - 16 + i * 4
        ln = 14 + (4 - abs(i - 4)) * 4
        d.rectangle((x, 46, x + 2, 46 + ln), fill=255)
    # arms raised: upper arm out, forearm up, palm + fingers
    for s in (-1, 1):
        sx, sy = cx + s * 36, 72
        ex, ey = cx + s * 60, 58
        hx, hy = cx + s * 62, 22
        d.line((sx, sy, ex, ey), fill=255, width=11)
        d.line((ex, ey, hx, hy), fill=255, width=9)
        d.ellipse((ex - 6, ey - 6, ex + 6, ey + 6), fill=255)
        d.ellipse((hx - 11, hy - 11, hx + 11, hy + 11), fill=255)
        for f in range(4):
            fx = hx - 9 + f * 6
            d.rectangle((fx, hy - 20 + abs(f - 1.5) * 3, fx + 3, hy - 6), fill=255)
        d.rectangle((hx - s * 12 - 2, hy - 4, hx - s * 12 + 2 + (-s * 4), hy + 2), fill=255)
    rgba = np.zeros((ah, aw, 4), np.uint8)
    mask = np.asarray(m) > 0
    ys = np.linspace(0, 1, ah)[:, None]
    top, bot = np.array([58, 30, 70]), np.array([18, 8, 24])
    col = (top + (bot - top) * ys[..., None]).astype(np.uint8)
    rgba[..., :3] = np.broadcast_to(col, (ah, aw, 3))
    rgba[..., 3] = np.where(mask, 255, 0)
    edge = mask & ~(np.roll(mask, 1, 0) & np.roll(mask, -1, 1) & np.roll(mask, 1, 1))
    rgba[edge] = (200, 80, 150, 255)
    return Image.fromarray(rgba, "RGBA").resize((aw * 2, ah * 2), Image.NEAREST)


BOSS_EYES = [(0, -79, 9), (-124, -81, 8), (124, -81, 8), (0, 52, 10)]


def draw_boss(canvas, x, y, t=0.0, alpha=1.0, eye_glow=1.0, hurt=0.0):
    img = _boss_art()
    aura = Image.new("RGBA", canvas.size, (0, 0, 0, 0))
    ad = ImageDraw.Draw(aura)
    pulse = 1 + 0.06 * math.sin(t * 5)
    for rr, al in ((175, 60), (135, 90), (100, 110)):
        r2 = rr * pulse
        ad.ellipse((x - r2 * 1.2, y - 40 - r2, x + r2 * 1.2, y - 40 + r2), fill=(200, 30, 60, al))
    from pixel import add_glow
    add_glow(canvas, aura)
    paste(canvas, img, x, y + math.sin(t * 1.4) * 5, "mm", alpha)
    d = draw(canvas)
    cx, cy = x, y + math.sin(t * 1.4) * 5
    for ex, ey, r in BOSS_EYES:
        px, py = int(cx + ex), int(cy + ey)
        g = int(120 * eye_glow)
        d.ellipse((px - r - 6, py - r - 6, px + r + 6, py + r + 6), fill=(80, 255, 220, g // 2))
        d.ellipse((px - r, py - r, px + r, py + r), fill=(140, 255, 230, 255))
        d.ellipse((px - r // 2, py - r // 2, px + r // 2, py + r // 2), fill=(20, 60, 60, 255))
    if hurt > 0:
        ov = silhouette(img, (255, 80, 120))
        paste(canvas, ov, x, cy, "mm", 0.35 * clamp01(hurt))
