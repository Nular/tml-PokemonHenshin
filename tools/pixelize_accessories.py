"""Pixelize accessory icons to 64x64 via pixeloe; rebuild Super gold/bling; cut Shard fragments.

Requires: pip install pixeloe pillow numpy opencv-python-headless
Source: Assets/Accessories/_src_hires/Axx.png (falls back to Accessories/Axx.png).
Writes: Assets/Accessories/Axx.png | Axx_Super.png | Axx_Shard.png
"""
from __future__ import annotations

import hashlib
import os
import random

import cv2
import numpy as np
from PIL import Image, ImageDraw, ImageEnhance, ImageFilter, ImageChops

from pixeloe.legacy.pixelize import pixelize

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
ACCS = os.path.join(ROOT, "Assets", "Accessories")
SRC_DIR = os.path.join(ACCS, "_src_hires")
SIZE = 64
TARGET = 56  # content short-side before pad-to-64
PATCH = 4
THICKNESS = 2
COLORS = 48


def content_bbox(rgba: np.ndarray, thr: int = 8) -> tuple[int, int, int, int]:
	a = rgba[:, :, 3]
	ys, xs = np.where(a > thr)
	if len(xs) == 0:
		h, w = a.shape
		return 0, 0, w, h
	return int(xs.min()), int(ys.min()), int(xs.max()) + 1, int(ys.max()) + 1


def crop_pad(rgba: np.ndarray, pad: int = 4) -> np.ndarray:
	x0, y0, x1, y1 = content_bbox(rgba)
	h, w = rgba.shape[:2]
	x0 = max(0, x0 - pad)
	y0 = max(0, y0 - pad)
	x1 = min(w, x1 + pad)
	y1 = min(h, y1 + pad)
	return rgba[y0:y1, x0:x1].copy()


def pixelize_rgba(rgba: np.ndarray) -> Image.Image:
	"""Contrast-aware pixelize RGB; nearest-resize alpha; fit into SIZE square."""
	rgba = crop_pad(rgba, pad=6)
	rgb = rgba[:, :, :3]
	alpha = rgba[:, :, 3].astype(np.float32) / 255.0

	# Composite on mid-gray so outline contrast stays usable (not washed on white).
	bg = np.full_like(rgb, 48)
	comp = (rgb.astype(np.float32) * alpha[..., None] + bg.astype(np.float32) * (1.0 - alpha[..., None]))
	comp = np.clip(comp, 0, 255).astype(np.uint8)
	bgr = cv2.cvtColor(comp, cv2.COLOR_RGB2BGR)

	# NOTE: do NOT pass contrast=/saturation= — legacy pixeloe leaves those in 0..1 and
	# writing to uint8 collapses the image to near-black.
	pix_bgr = pixelize(
		bgr,
		mode="contrast",
		target_size=TARGET,
		patch_size=PATCH,
		thickness=THICKNESS,
		color_matching=True,
		colors=COLORS,
		no_upscale=True,
	)
	pix_rgb = cv2.cvtColor(pix_bgr, cv2.COLOR_BGR2RGB)
	ph, pw = pix_rgb.shape[:2]

	# Alpha: max-pool style via PIL NEAREST after slight dilate so silhouette stays solid.
	alpha_img = Image.fromarray((alpha * 255).astype(np.uint8), mode="L")
	alpha_img = alpha_img.filter(ImageFilter.MaxFilter(3))
	alpha_pix = alpha_img.resize((pw, ph), Image.Resampling.NEAREST)
	alpha_arr = np.array(alpha_pix)

	# Knock out near-bg gray leftovers where alpha is low.
	out = np.dstack([pix_rgb, alpha_arr])
	out[alpha_arr < 24, 3] = 0

	im = Image.fromarray(out, "RGBA")
	# Mild post punch (PIL-safe; avoid pixeloe contrast/sat kwargs).
	rgb = im.convert("RGB")
	rgb = ImageEnhance.Color(rgb).enhance(1.12)
	rgb = ImageEnhance.Contrast(rgb).enhance(1.08)
	im = Image.merge("RGBA", (*rgb.split(), im.split()[-1]))
	return fit_square(im, SIZE)


def fit_square(im: Image.Image, size: int) -> Image.Image:
	im = im.convert("RGBA")
	w, h = im.size
	scale = min(size / max(w, 1), size / max(h, 1))
	nw = max(1, int(round(w * scale)))
	nh = max(1, int(round(h * scale)))
	# Keep crisp pixels: NEAREST after pixeloe.
	resized = im.resize((nw, nh), Image.Resampling.NEAREST)
	canvas = Image.new("RGBA", (size, size), (0, 0, 0, 0))
	ox = (size - nw) // 2
	oy = (size - nh) // 2
	canvas.paste(resized, (ox, oy), resized)
	return canvas


def thicker_gold_rim(im: Image.Image) -> Image.Image:
	"""Bold outer gold (≈3px) + bright mid rim + soft glow + top sheen."""
	# Shrink content 1px so gold has room inside 64 canvas without clipping.
	w, h = im.size
	shrunk = Image.new("RGBA", (w, h), (0, 0, 0, 0))
	core = im.resize((max(1, w - 4), max(1, h - 4)), Image.Resampling.NEAREST)
	shrunk.paste(core, (2, 2), core)
	im = shrunk

	alpha = im.split()[-1]
	# Soft outer glow
	glow_a = alpha.filter(ImageFilter.MaxFilter(9))
	glow_a = ImageChops.subtract(glow_a, alpha)
	glow = Image.new("RGBA", im.size, (255, 200, 40, 0))
	glow.putalpha(glow_a.point(lambda p: 110 if p > 10 else 0))

	# Deep gold outer ring (~3px via MaxFilter 7 vs content)
	outer = alpha.filter(ImageFilter.MaxFilter(7))
	ring_outer = ImageChops.subtract(outer, alpha)
	gold = Image.new("RGBA", im.size, (255, 175, 20, 0))
	gold.putalpha(ring_outer.point(lambda p: 255 if p > 20 else 0))

	# Bright mid gold
	mid = alpha.filter(ImageFilter.MaxFilter(5))
	ring_mid = ImageChops.subtract(mid, alpha)
	gold2 = Image.new("RGBA", im.size, (255, 220, 80, 0))
	gold2.putalpha(ring_mid.point(lambda p: 255 if p > 20 else 0))

	# Hot inner rim
	inner = alpha.filter(ImageFilter.MaxFilter(3))
	ring_in = ImageChops.subtract(inner, alpha)
	gold3 = Image.new("RGBA", im.size, (255, 245, 170, 0))
	gold3.putalpha(ring_in.point(lambda p: 240 if p > 20 else 0))

	base = Image.alpha_composite(glow, im)
	base = Image.alpha_composite(base, gold)
	base = Image.alpha_composite(base, gold2)
	base = Image.alpha_composite(base, gold3)

	# Top-edge inner sheen on the item itself
	pixels = base.load()
	a = alpha.load()
	for y in range(h):
		for x in range(w):
			if a[x, y] < 180:
				continue
			up = a[x, y - 1] if y > 0 else 0
			if up < 80:
				r, g, b, aa = pixels[x, y]
				pixels[x, y] = (
					min(255, r + 50),
					min(255, g + 42),
					min(255, b + 12),
					aa,
				)
	return base


def draw_sparkle(draw: ImageDraw.ImageDraw, cx: int, cy: int, arm: int, color: tuple[int, int, int, int]) -> None:
	"""Plus + diagonal diamond sparkle."""
	for d in range(-arm, arm + 1):
		draw.point((cx + d, cy), fill=color)
		draw.point((cx, cy + d), fill=color)
	if arm >= 2:
		half = arm - 1
		for d in range(-half, half + 1):
			draw.point((cx + d, cy + d), fill=color)
			draw.point((cx + d, cy - d), fill=color)
	# core
	draw.point((cx, cy), fill=(255, 255, 255, 255))


def bling_sparkles(im: Image.Image, seed: str) -> Image.Image:
	rng = random.Random(int(hashlib.md5(seed.encode()).hexdigest()[:8], 16))
	overlay = Image.new("RGBA", im.size, (0, 0, 0, 0))
	draw = ImageDraw.Draw(overlay)
	alpha = im.split()[-1]
	bbox = alpha.getbbox()
	if not bbox:
		return im
	x0, y0, x1, y1 = bbox
	spots = []
	for _ in range(22):
		t = rng.random()
		if t < 0.75:
			side = rng.randrange(4)
			if side == 0:
				x, y = rng.randint(x0, max(x0, x1 - 1)), max(0, y0 + rng.randint(-2, 3))
			elif side == 1:
				x, y = rng.randint(x0, max(x0, x1 - 1)), min(im.size[1] - 1, y1 - 1 + rng.randint(-3, 2))
			elif side == 2:
				x, y = max(0, x0 + rng.randint(-2, 3)), rng.randint(y0, max(y0, y1 - 1))
			else:
				x, y = min(im.size[0] - 1, x1 - 1 + rng.randint(-3, 2)), rng.randint(y0, max(y0, y1 - 1))
		else:
			x = rng.randint(x0 + 2, max(x0 + 3, x1 - 3))
			y = rng.randint(y0 + 2, max(y0 + 3, y1 - 3))
		spots.append((x, y))

	palette = [
		(255, 255, 255, 255),
		(255, 255, 220, 250),
		(255, 235, 140, 240),
		(255, 200, 60, 230),
		(255, 248, 200, 220),
	]
	for i, (x, y) in enumerate(spots):
		arm = 1 + (i % 3)
		col = palette[i % len(palette)]
		draw_sparkle(draw, x, y, arm, col)
		if i % 2 == 0:
			draw.point((min(im.size[0] - 1, x + arm + 1), max(0, y - 1)), fill=(255, 250, 200, 210))
		if i % 3 == 0:
			# small diamond companion
			draw.point((x, max(0, y - arm - 1)), fill=(255, 255, 255, 200))

	return Image.alpha_composite(im, overlay)


def make_super(base: Image.Image, seed: str) -> Image.Image:
	im = ImageEnhance.Brightness(base).enhance(1.12)
	im = ImageEnhance.Color(im).enhance(1.18)
	im = ImageEnhance.Contrast(im).enhance(1.06)
	im = thicker_gold_rim(im)
	im = bling_sparkles(im, seed)
	# ensure still SIZE (glow can expand — refit if needed)
	if im.size != (SIZE, SIZE):
		im = fit_square(im, SIZE)
	# If glow went outside, canvas may clip — rebuild on clear canvas if bbox touches edge heavily
	return im.resize((SIZE, SIZE), Image.Resampling.NEAREST) if im.size != (SIZE, SIZE) else im


def shard_polygon(size: int, seed: str) -> list[tuple[int, int]]:
	"""Jagged crystal / broken-shard silhouette, deterministic per id."""
	rng = random.Random(int(hashlib.md5(("shard:" + seed).encode()).hexdigest()[:8], 16))
	cx = cy = size / 2
	# Irregular star-like crystal: 6–8 vertices with varying radius
	n = rng.choice([6, 7, 8])
	pts = []
	base_r = size * 0.38
	for i in range(n):
		ang = (2 * np.pi * i / n) - np.pi / 2 + rng.uniform(-0.18, 0.18)
		# alternate long/short for shard facets
		r = base_r * (rng.uniform(0.72, 1.08) if i % 2 == 0 else rng.uniform(0.42, 0.70))
		# slight stretch vertical for "broken piece" feel
		x = cx + r * np.cos(ang) * 0.92
		y = cy + r * np.sin(ang) * 1.05
		pts.append((int(round(x)), int(round(y))))
	return pts


def make_shard(base: Image.Image, seed: str) -> Image.Image:
	"""Cut a fragment-shaped piece from the pixel accessory; slight desat + crack edge."""
	w, h = base.size
	mask = Image.new("L", (w, h), 0)
	draw = ImageDraw.Draw(mask)
	poly = shard_polygon(w, seed)
	draw.polygon(poly, fill=255)

	# soft crack notch on one edge (subtract a triangle)
	rng = random.Random(int(hashlib.md5(("crack:" + seed).encode()).hexdigest()[:8], 16))
	i = rng.randrange(len(poly))
	p0 = poly[i]
	p1 = poly[(i + 1) % len(poly)]
	mx = (p0[0] + p1[0]) // 2
	my = (p0[1] + p1[1]) // 2
	inx = int(w / 2 + (mx - w / 2) * 0.35)
	iny = int(h / 2 + (my - h / 2) * 0.35)
	crack = Image.new("L", (w, h), 0)
	ImageDraw.Draw(crack).polygon([p0, p1, (inx, iny)], fill=255)
	mask = ImageChops.subtract(mask, crack)

	# Keep only fragment; darken rim 1px for broken-stone read
	frag = Image.new("RGBA", (w, h), (0, 0, 0, 0))
	frag.paste(base, (0, 0), mask)

	# Edge darken
	m = np.array(mask)
	edge = cv2.morphologyEx(m, cv2.MORPH_GRADIENT, np.ones((3, 3), np.uint8))
	arr = np.array(frag)
	edge_bool = edge > 0
	arr[edge_bool, 0] = (arr[edge_bool, 0].astype(np.int16) * 0.55).clip(0, 255).astype(np.uint8)
	arr[edge_bool, 1] = (arr[edge_bool, 1].astype(np.int16) * 0.55).clip(0, 255).astype(np.uint8)
	arr[edge_bool, 2] = (arr[edge_bool, 2].astype(np.int16) * 0.55).clip(0, 255).astype(np.uint8)
	frag = Image.fromarray(arr, "RGBA")

	# Slight desaturation so shards read as "pieces" vs full item
	rgb = frag.convert("RGB")
	rgb = ImageEnhance.Color(rgb).enhance(0.88)
	rgb = ImageEnhance.Brightness(rgb).enhance(0.96)
	frag = Image.merge("RGBA", (*rgb.split(), frag.split()[-1]))

	# Recenter by bbox
	bbox = frag.getbbox()
	if bbox:
		cropped = frag.crop(bbox)
		return fit_square(cropped, SIZE)
	return frag


def resolve_src(aid: str) -> str | None:
	hi = os.path.join(SRC_DIR, f"{aid}.png")
	lo = os.path.join(ACCS, f"{aid}.png")
	# Prefer hires backup; if missing Super-only, still need base.
	if os.path.isfile(hi):
		return hi
	if os.path.isfile(lo):
		return lo
	return None


def process_one(aid: str) -> None:
	src = resolve_src(aid)
	if not src:
		print(f"SKIP missing {aid}")
		return
	rgba = np.array(Image.open(src).convert("RGBA"))
	base = pixelize_rgba(rgba)
	base_path = os.path.join(ACCS, f"{aid}.png")
	base.save(base_path, "PNG")
	print(f"OK  {aid}.png {base.size}")

	super_im = make_super(base, aid)
	# Super glow may need room — if gold rim ate into edges, ok on 64 canvas
	super_path = os.path.join(ACCS, f"{aid}_Super.png")
	super_im.save(super_path, "PNG")
	print(f"OK  {aid}_Super.png")

	shard_im = make_shard(base, aid)
	shard_path = os.path.join(ACCS, f"{aid}_Shard.png")
	shard_im.save(shard_path, "PNG")
	print(f"OK  {aid}_Shard.png")


def main() -> None:
	os.makedirs(ACCS, exist_ok=True)
	if not os.path.isdir(SRC_DIR):
		print(f"NOTE: no {SRC_DIR}; using current Accessories/*.png as source")
	for i in range(1, 29):
		process_one(f"A{i:02d}")


if __name__ == "__main__":
	main()
