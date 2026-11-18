"""Canvas, pixel text, Terraria-style UI widgets and shared helpers.

Every draw call is a pure function of time so any frame can be rendered in isolation.
"""
from __future__ import annotations

import io
import math
import random
import urllib.request
import zipfile
import zlib
from functools import lru_cache
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFont

W, H = 480, 270
SCALE = 4
FPS = 30
BPM = 150
BEAT = 60.0 / BPM

HERE = Path(__file__).resolve().parent
REPO = HERE.parent.parent
ASSETS = REPO / "Assets"
FONT_DIR = HERE / "fonts"
OUT_DIR = HERE / "out"

FONT_VERSION = "2026.11.10"
FONT_ZIP_URL = (
    "https://github.com/TakWolf/fusion-pixel-font/releases/download/"
    f"{FONT_VERSION}/fusion-pixel-font-12px-proportional-ttf-v{FONT_VERSION}.zip"
)

GOLD = (255, 214, 64)
GOLD_DARK = (232, 140, 24)
WHITE = (255, 255, 255)
BLACK = (0, 0, 0)
YELLOW = (255, 236, 92)
SPARK = (255, 206, 64)
DMG = (255, 140, 40)
CRIT = (255, 70, 40)
GREEN_TXT = (120, 255, 120)
PANEL = (52, 70, 150)
PANEL_EDGE = (22, 28, 72)


# ---------------------------------------------------------------- fonts

def _find_font_file() -> Path | None:
    for p in sorted(FONT_DIR.glob("**/*12px*proportional*zh_hans*.ttf")):
        return p
    for p in sorted(FONT_DIR.glob("**/*.ttf")):
        return p
    return None


def ensure_font() -> Path | None:
    found = _find_font_file()
    if found:
        return found
    FONT_DIR.mkdir(parents=True, exist_ok=True)
    try:
        print(f"[font] downloading Fusion Pixel {FONT_VERSION} ...")
        data = urllib.request.urlopen(FONT_ZIP_URL, timeout=60).read()
        zipfile.ZipFile(io.BytesIO(data)).extractall(FONT_DIR)
    except Exception as exc:  # noqa: BLE001
        print(f"[font] download failed ({exc}); falling back to SimSun")
        return None
    return _find_font_file()


@lru_cache(maxsize=None)
def font() -> tuple[ImageFont.FreeTypeFont, int]:
    """Returns (font, glyph_height). Pixel font renders crisp at its native size."""
    path = ensure_font()
    if path:
        return ImageFont.truetype(str(path), 12), 12
    return ImageFont.truetype("C:/Windows/Fonts/simsun.ttc", 12), 12


# ---------------------------------------------------------------- canvas helpers

def new_canvas(color=(0, 0, 0)) -> Image.Image:
    """Main frame buffer is opaque RGB; RGBA sprites/ink are blended onto it."""
    return Image.new("RGB", (W, H), color)


def draw(canvas: Image.Image) -> ImageDraw.ImageDraw:
    """ImageDraw that alpha-blends RGBA ink onto the RGB frame."""
    return ImageDraw.Draw(canvas, "RGBA")


def comp(canvas: Image.Image, img: Image.Image, pos=(0, 0)):
    canvas.paste(img, pos, img)


def lerp(a, b, k):
    return a + (b - a) * k


def clamp01(x):
    return 0.0 if x < 0 else 1.0 if x > 1 else x


def ease_out(k):
    k = clamp01(k)
    return 1 - (1 - k) ** 3


def ease_in(k):
    k = clamp01(k)
    return k ** 3


def ease_out_back(k, s=1.9):
    k = clamp01(k) - 1
    return k * k * ((s + 1) * k + s) + 1


def lerp_color(c1, c2, k):
    k = clamp01(k)
    return tuple(int(round(lerp(a, b, k))) for a, b in zip(c1, c2))


def rng(*seed) -> random.Random:
    return random.Random(zlib.crc32(repr(seed).encode()))


def paste(canvas: Image.Image, img: Image.Image, x: float, y: float, anchor: str = "mm", alpha: float = 1.0):
    """Alpha-composites img onto canvas. anchor: [lmr][tmb] (horizontal, vertical)."""
    if img is None:
        return
    w, h = img.size
    ax = {"l": 0, "m": w // 2, "r": w}[anchor[0]]
    ay = {"t": 0, "m": h // 2, "b": h}[anchor[1]]
    if alpha < 1.0:
        if alpha <= 0:
            return
        img = img.copy()
        a = img.getchannel("A").point(lambda v: int(v * alpha))
        img.putalpha(a)
    px, py = int(round(x)) - ax, int(round(y)) - ay
    if px >= W or py >= H or px + w <= 0 or py + h <= 0:
        return
    canvas.paste(img, (px, py), img)


def add_glow(canvas: Image.Image, layer: Image.Image, strength: float = 1.0):
    """Additive blend of an RGBA layer (premultiplied by its alpha)."""
    base = np.asarray(canvas, dtype=np.float32)
    lay = np.asarray(layer, dtype=np.float32)
    rgb = lay[..., :3] * (lay[..., 3:4] / 255.0) * strength
    out = np.clip(base + rgb, 0, 255).astype(np.uint8)
    canvas.paste(Image.fromarray(out, "RGB"))


def flash(canvas: Image.Image, alpha: float, color=(255, 255, 255)):
    if alpha <= 0:
        return
    draw(canvas).rectangle((0, 0, W, H), fill=color + (int(255 * clamp01(alpha)),))


def darken(canvas: Image.Image, alpha: float, color=(0, 0, 0)):
    flash(canvas, alpha, color)


def shake_offset(t: float, t0: float, dur: float, amp: float) -> tuple[int, int]:
    k = (t - t0) / dur
    if k < 0 or k > 1:
        return 0, 0
    a = amp * (1 - k) ** 2
    f = int(t * FPS)
    r = rng("shake", f)
    return int(round(r.uniform(-a, a))), int(round(r.uniform(-a, a)))


def apply_shake(canvas: Image.Image, dx: int, dy: int) -> Image.Image:
    if dx == 0 and dy == 0:
        return canvas
    out = Image.new("RGB", (W, H), (0, 0, 0))
    out.paste(canvas, (dx, dy))
    return out


def scale_nearest(img: Image.Image, s: float) -> Image.Image:
    w, h = img.size
    return img.resize((max(1, int(round(w * s))), max(1, int(round(h * s)))), Image.NEAREST)


def pixelize(img: Image.Image, size: tuple[int, int], alpha_levels: int = 4) -> Image.Image:
    """Downsample a hi-res FX texture into chunky pixels with posterized alpha."""
    small = img.convert("RGBA").resize(size, Image.BOX)
    arr = np.asarray(small).astype(np.float32)
    a = arr[..., 3] / 255.0
    a = np.floor(a * alpha_levels + 0.35) / alpha_levels
    arr[..., 3] = np.clip(a, 0, 1) * 255
    arr[..., :3] = np.round(arr[..., :3] / 32) * 32
    return Image.fromarray(np.clip(arr, 0, 255).astype(np.uint8), "RGBA")


def tint_mask(mask: Image.Image, color, alpha_mul: float = 1.0) -> Image.Image:
    """mask: L or LA/RGBA luminance texture -> colored RGBA."""
    m = mask.convert("L")
    if alpha_mul != 1.0:
        m = m.point(lambda v: int(min(255, v * alpha_mul)))
    out = Image.new("RGBA", m.size, color + (0,))
    out.putalpha(m)
    return out


# ---------------------------------------------------------------- pixel text

def _dilate(mask: np.ndarray, r: int) -> np.ndarray:
    out = mask.copy()
    for dy in range(-r, r + 1):
        for dx in range(-r, r + 1):
            if dx == 0 and dy == 0:
                continue
            out |= np.roll(np.roll(mask, dy, 0), dx, 1)
    return out


@lru_cache(maxsize=2048)
def text_img(text: str, color=WHITE, scale: int = 1, outline=BLACK, color2=None, shadow: bool = True) -> Image.Image:
    """Crisp pixel text with Terraria-like 1px dark outline, optional vertical gradient, then integer upscale."""
    fnt, gh = font()
    lines = text.split("\n")
    widths = [int(math.ceil(fnt.getlength(ln))) for ln in lines]
    lh = gh + 2
    pad = 2
    tw, th = max(widths) + pad * 2 + 1, lh * len(lines) + pad * 2 + 1
    mask_img = Image.new("L", (tw, th), 0)
    d = ImageDraw.Draw(mask_img)
    d.fontmode = "1"
    for i, ln in enumerate(lines):
        x = pad + (max(widths) - widths[i]) // 2
        d.text((x, pad + i * lh - 1), ln, font=fnt, fill=255)
    m = np.asarray(mask_img) > 127
    o = _dilate(m, 1)
    rgba = np.zeros((th, tw, 4), np.uint8)
    if shadow and outline is not None:
        sh = np.roll(np.roll(o, 1, 0), 1, 1) & ~o
        rgba[sh] = (0, 0, 0, 150)
    if outline is not None:
        rgba[o] = outline + (255,)
    if color2 is None:
        rgba[m] = color + (255,)
    else:
        ys = np.arange(th)[:, None].repeat(tw, 1)
        k = np.clip((ys - pad) / max(1, lh * len(lines) - 2), 0, 1)
        for c in range(3):
            ch = (color[c] + (color2[c] - color[c]) * k).astype(np.uint8)
            rgba[..., c] = np.where(m, ch, rgba[..., c])
        rgba[..., 3] = np.where(m, 255, rgba[..., 3])
    img = Image.fromarray(rgba, "RGBA")
    if scale != 1:
        img = img.resize((tw * scale, th * scale), Image.NEAREST)
    return img


def draw_text(canvas, text, x, y, color=WHITE, scale=1, anchor="mm", alpha=1.0, outline=BLACK, color2=None):
    paste(canvas, text_img(text, color, scale, outline, color2), x, y, anchor, alpha)


def pop_text(canvas, text, x, y, age_beats: float, color=WHITE, scale=3, color2=None, outline=BLACK,
             life: float | None = None, anchor="mm"):
    """Short-video style pop: overshoot one scale step, settle, optional fade-out at end of life."""
    if age_beats < 0:
        return
    s = scale
    if age_beats < 0.12:
        s = scale + 1
    dy = -int(6 * (1 - ease_out(age_beats / 0.4)))
    alpha = 1.0
    if life is not None:
        if age_beats > life:
            return
        alpha = clamp01((life - age_beats) / 0.3)
    draw_text(canvas, text, x, y + dy, color, s, anchor, alpha, outline, color2)


def typewriter(canvas, text, x, y, age_beats: float, chars_per_beat: float, **kw):
    n = int(max(0, age_beats) * chars_per_beat)
    if n <= 0:
        return
    shown = text[:n]
    img = text_img(text, kw.get("color", WHITE), kw.get("scale", 2), kw.get("outline", BLACK), kw.get("color2"))
    part = text_img(shown, kw.get("color", WHITE), kw.get("scale", 2), kw.get("outline", BLACK), kw.get("color2"))
    paste(canvas, part, x - img.width // 2, y, "lm")


# ---------------------------------------------------------------- UI widgets

def rounded_box(canvas, box, fill, edge=None, alpha=255, radius=3, edge_w=2):
    x0, y0, x1, y1 = [int(v) for v in box]
    d = draw(canvas)
    d.rounded_rectangle((x0, y0, x1, y1), radius, fill=fill + (alpha,))
    if edge:
        d.rounded_rectangle((x0, y0, x1, y1), radius, outline=edge + (255,), width=edge_w)


def slot(canvas, x, y, size=28, selected=False, icon: Image.Image | None = None, label: str | None = None,
         fill=(63, 82, 151), alpha=200):
    """Terraria inventory slot (blue translucent, darker border, gold border when selected)."""
    edge = (255, 230, 90) if selected else (30, 38, 90)
    rounded_box(canvas, (x, y, x + size, y + size), fill if not selected else (90, 110, 190), edge,
                alpha=alpha if not selected else 235, radius=4, edge_w=2 if selected else 1)
    if icon is not None:
        paste(canvas, icon, x + size / 2, y + size / 2, "mm")
    if label:
        draw_text(canvas, label, x + 2, y + 1, WHITE, 1, "lt")


def hotbar(canvas, x, y, icons: list, selected: int, size=28, gap=2, title: str | None = None):
    if title:
        draw_text(canvas, title, x + 2, y - 3, WHITE, 1, "lb")
    for i in range(10):
        ic = icons[i] if i < len(icons) else None
        sx = x + i * (size + gap)
        sel = i == selected
        s = size + (4 if sel else 0)
        oy = -2 if sel else 0
        slot(canvas, sx - (2 if sel else 0), y + oy, s, sel, ic, str((i + 1) % 10))


def bar(canvas, x, y, w, h, k, fg, bg=(40, 20, 20), edge=(10, 10, 10), shine=True):
    x, y, w, h = int(x), int(y), int(w), int(h)
    d = draw(canvas)
    d.rectangle((x - 1, y - 1, x + w, y + h), fill=edge + (255,))
    d.rectangle((x, y, x + w - 1, y + h - 1), fill=bg + (255,))
    fw = int(round(w * clamp01(k)))
    if fw > 0:
        d.rectangle((x, y, x + fw - 1, y + h - 1), fill=fg + (255,))
        if shine and h >= 4:
            hi = lerp_color(fg, WHITE, 0.45)
            d.rectangle((x, y, x + fw - 1, y + max(1, h // 4) - 1), fill=hi + (255,))


def chip(canvas, text, x, y, color=(255, 255, 255), fill=(40, 46, 110), edge=(255, 214, 64), anchor="lt",
         scale=1, alpha=1.0):
    img = text_img(text, color, scale)
    w, h = img.width + 8, img.height + 2
    ax = {"l": 0, "m": w // 2, "r": w}[anchor[0]]
    ay = {"t": 0, "m": h // 2, "b": h}[anchor[1]]
    x0, y0 = int(x) - ax, int(y) - ay
    if alpha > 0:
        rounded_box(canvas, (x0, y0, x0 + w, y0 + h), fill, edge, int(220 * alpha), 3, 1)
        paste(canvas, img, x0 + w / 2, y0 + h / 2 + 1, "mm", alpha)


def damage_number(canvas, value, x, y, age_beats: float, crit=False, color=None, scale=None):
    """Terraria combat text: pops up, drifts upward, fades."""
    life = 2.2
    if age_beats < 0 or age_beats > life:
        return
    col = color or (CRIT if crit else DMG)
    s = scale or (2 if crit else 1)
    rise = 18 * ease_out(age_beats / life)
    if age_beats < 0.1:
        s += 1
    alpha = clamp01((life - age_beats) / 0.6)
    draw_text(canvas, str(value), x, y - rise, col, s, "mm", alpha)


def chat_line(canvas, x, y, name, msg, age_beats):
    if age_beats < 0:
        return
    alpha = clamp01((6 - age_beats) / 0.5)
    img_n = text_img(f"<{name}>", (120, 200, 255))
    img_m = text_img(" " + msg, WHITE)
    paste(canvas, img_n, x, y, "lm", alpha)
    paste(canvas, img_m, x + img_n.width - 4, y, "lm", alpha)


# ---------------------------------------------------------------- particles (stateless)

def dust_burst(canvas, x, y, age: float, seed, n=14, speed=60.0, colors=((255, 255, 255),), life=0.7,
               gravity=120.0, size=2, spread=math.tau, angle=-math.pi / 2, drag=1.5):
    """Square pixel dust, Terraria-style. age in seconds."""
    if age < 0 or age > life * 1.4:
        return
    r = rng("dust", seed)
    d = draw(canvas)
    for i in range(n):
        a = angle + r.uniform(-spread / 2, spread / 2)
        v = speed * r.uniform(0.35, 1.0)
        l = life * r.uniform(0.6, 1.4)
        if age > l:
            continue
        k = age / l
        tt = (1 - math.exp(-drag * age)) / drag
        px = x + math.cos(a) * v * tt
        py = y + math.sin(a) * v * tt + 0.5 * gravity * age * age
        c = colors[i % len(colors)]
        sz = max(1, int(round(size * (1 - 0.5 * k))))
        al = int(255 * (1 - k * k))
        d.rectangle((int(px), int(py), int(px) + sz - 1, int(py) + sz - 1), fill=c + (al,))


def sparkle(canvas, x, y, phase: float, color=(255, 255, 220), size=3):
    """4-point pixel twinkle; phase in [0,1)."""
    k = math.sin(phase * math.pi)
    if k <= 0.05:
        return
    s = max(1, int(round(size * k)))
    d = draw(canvas)
    x, y = int(x), int(y)
    c = color + (255,)
    d.rectangle((x - s, y, x + s, y), fill=c)
    d.rectangle((x, y - s, x, y + s), fill=c)
    if s >= 2:
        d.point((x, y), fill=(255, 255, 255, 255))


def block_wipe(canvas, k: float, seed=0, block=16, color=(0, 0, 0), direction=1):
    """Diagonal tile wipe used for scene transitions. k 0..1 coverage."""
    if k <= 0:
        return
    d = draw(canvas)
    cols, rows = W // block + 1, H // block + 1
    for gy in range(rows):
        for gx in range(cols):
            order = (gx + gy * 0.6) / (cols + rows * 0.6)
            if direction < 0:
                order = 1 - order
            if k * 1.25 - 0.25 > order:
                d.rectangle((gx * block, gy * block, gx * block + block - 1, gy * block + block - 1), fill=color + (255,))
            elif k * 1.25 > order:
                s = int(block * ((k * 1.25 - order) / 0.25)) // 2 * 2
                if s < 2:
                    continue
                cx, cy = gx * block + block // 2, gy * block + block // 2
                d.rectangle((cx - s // 2, cy - s // 2, cx + s // 2 - 1, cy + s // 2 - 1), fill=color + (255,))


def pixel_circle(canvas, x, y, r, color, width=2, alpha=255):
    if r <= 0:
        return
    d = draw(canvas)
    steps = max(24, int(r * 3))
    pts = set()
    for i in range(steps):
        a = i / steps * math.tau
        pts.add((int(x + math.cos(a) * r) // 2 * 2, int(y + math.sin(a) * r) // 2 * 2))
    for px, py in pts:
        d.rectangle((px, py, px + width - 1, py + width - 1), fill=color + (alpha,))
