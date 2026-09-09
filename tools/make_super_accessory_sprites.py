"""Brighten + gold rim + sparkles for Super accessory icons."""
from __future__ import annotations

import hashlib
import os

from PIL import Image, ImageEnhance, ImageDraw, ImageFilter

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
ACCS = os.path.join(ROOT, "Assets", "Accessories")


def sparkles(im: Image.Image, seed: str) -> Image.Image:
	h = hashlib.md5(seed.encode()).digest()
	overlay = Image.new("RGBA", im.size, (0, 0, 0, 0))
	draw = ImageDraw.Draw(overlay)
	w, ht = im.size
	for i in range(8):
		x = (h[i] * 17 + i * 13) % max(1, w)
		y = (h[(i + 8) % 16] * 11 + i * 7) % max(1, ht)
		r = 1 + (h[i] % 2)
		draw.ellipse((x - r, y - r, x + r, y + r), fill=(255, 230, 120, 200))
	return Image.alpha_composite(im, overlay)


def gold_rim(im: Image.Image) -> Image.Image:
	alpha = im.split()[-1]
	ring = alpha.filter(ImageFilter.MaxFilter(3))
	edge = ImageChopsSubtract(ring, alpha)
	gold = Image.new("RGBA", im.size, (255, 196, 64, 0))
	gold.putalpha(edge.point(lambda p: min(255, p * 3)))
	return Image.alpha_composite(im, gold)


def ImageChopsSubtract(a: Image.Image, b: Image.Image) -> Image.Image:
	from PIL import ImageChops

	return ImageChops.subtract(a, b)


def process(src: str, dst: str) -> None:
	im = Image.open(src).convert("RGBA")
	im = ImageEnhance.Brightness(im).enhance(1.18)
	im = ImageEnhance.Color(im).enhance(1.25)
	im = ImageEnhance.Contrast(im).enhance(1.08)
	im = gold_rim(im)
	im = sparkles(im, os.path.basename(src))
	im.save(dst, "PNG")
	print(f"OK super {os.path.basename(dst)} {im.size}")


def main() -> None:
	os.makedirs(ACCS, exist_ok=True)
	for i in range(1, 29):
		aid = f"A{i:02d}"
		src = os.path.join(ACCS, f"{aid}.png")
		dst = os.path.join(ACCS, f"{aid}_Super.png")
		if not os.path.exists(src):
			print(f"SKIP missing {aid}.png")
			continue
		process(src, dst)


if __name__ == "__main__":
	main()
