"""Extract a GIF into a horizontal sprite sheet + sidecar JSON for form locomotion.

Usage:
  python tools/extract_locomotion_gif.py Assets/TEMP_ASSETS/pikachu_idle.gif \\
      Assets/Forms/Locomotion/L04_F01_Idle.png --flip-h
  python tools/extract_locomotion_gif.py Assets/TEMP_ASSETS/pikachu-running.gif \\
      Assets/Forms/Locomotion/L04_F01_Run.png

Writes <out>.png (RGBA horizontal strip) and <out>.json (frame meta; C# uses hardcoded clips).
"""
from __future__ import annotations

import argparse
import json
import os
from pathlib import Path

from PIL import Image


def iter_frames(im: Image.Image) -> list[tuple[Image.Image, int]]:
	frames: list[tuple[Image.Image, int]] = []
	idx = 0
	while True:
		duration = int(im.info.get("duration") or 100)
		# GIF disposal: composite onto transparent canvas of full size
		frame = im.convert("RGBA")
		frames.append((frame.copy(), duration))
		idx += 1
		try:
			im.seek(idx)
		except EOFError:
			break
	return frames


def content_bbox(im: Image.Image, thr: int = 8) -> tuple[int, int, int, int]:
	a = im.split()[-1]
	bbox = a.point(lambda p: 255 if p > thr else 0).getbbox()
	if bbox is None:
		return 0, 0, im.width, im.height
	return bbox


def main() -> None:
	ap = argparse.ArgumentParser(description=__doc__)
	ap.add_argument("gif", type=Path, help="Source GIF path")
	ap.add_argument("out_png", type=Path, help="Output horizontal strip PNG")
	ap.add_argument("--flip-h", action="store_true", help="Mirror each frame horizontally (face right)")
	ap.add_argument("--pad", type=int, default=0, help="Padding around content bbox before sheet pack")
	args = ap.parse_args()

	im = Image.open(args.gif)
	raw = iter_frames(im)
	if not raw:
		raise SystemExit(f"No frames in {args.gif}")

	cropped: list[tuple[Image.Image, int]] = []
	max_w = max_h = 0
	for frame, dur in raw:
		if args.flip_h:
			frame = frame.transpose(Image.Transpose.FLIP_LEFT_RIGHT)
		x0, y0, x1, y1 = content_bbox(frame)
		pad = args.pad
		x0 = max(0, x0 - pad)
		y0 = max(0, y0 - pad)
		x1 = min(frame.width, x1 + pad)
		y1 = min(frame.height, y1 + pad)
		cell = frame.crop((x0, y0, x1, y1))
		cropped.append((cell, dur))
		max_w = max(max_w, cell.width)
		max_h = max(max_h, cell.height)

	# Align all frames to shared canvas (bottom-center for foot anchoring)
	aligned: list[Image.Image] = []
	for cell, _ in cropped:
		canvas = Image.new("RGBA", (max_w, max_h), (0, 0, 0, 0))
		ox = (max_w - cell.width) // 2
		oy = max_h - cell.height  # feet on bottom
		canvas.paste(cell, (ox, oy), cell)
		aligned.append(canvas)

	n = len(aligned)
	sheet = Image.new("RGBA", (max_w * n, max_h), (0, 0, 0, 0))
	for i, cell in enumerate(aligned):
		sheet.paste(cell, (i * max_w, 0), cell)

	args.out_png.parent.mkdir(parents=True, exist_ok=True)
	sheet.save(args.out_png)

	meta = {
		"source": str(args.gif).replace("\\", "/"),
		"flip_h": bool(args.flip_h),
		"frameCount": n,
		"frameWidth": max_w,
		"frameHeight": max_h,
		"durationsMs": [d for _, d in cropped],
		"facesLeft": False,
		"note": "C# FormAnimClip is hardcoded; this JSON is authoring evidence only.",
	}
	out_json = args.out_png.with_suffix(".json")
	out_json.write_text(json.dumps(meta, indent=2) + "\n", encoding="utf-8")
	print(f"Wrote {args.out_png} ({max_w}x{max_h} x {n})")
	print(f"Wrote {out_json}")
	print(f"durationsMs={meta['durationsMs']}")


if __name__ == "__main__":
	main()
