"""Pokemon Henshin promo video renderer.

  python tools/PromoVideo/render.py --preview          # keyframe contact sheets -> out/preview_*.png
  python tools/PromoVideo/render.py --at 1.5 12.0      # single frames (seconds) at 1080p
  python tools/PromoVideo/render.py                    # full video -> out/pokemon_henshin_promo.mp4
"""
from __future__ import annotations

import argparse
import subprocess
import sys
import time
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))

from PIL import Image  # noqa: E402

import pixel as P  # noqa: E402
import timeline as TL  # noqa: E402
from pixel import BEAT, FPS, H, OUT_DIR, SCALE, W  # noqa: E402


def preview():
    OUT_DIR.mkdir(parents=True, exist_ok=True)
    tiles = []
    for start, sc in zip(TL.STARTS, TL.SCENES):
        for k in (0.2, 0.5, 0.8):
            t = (start + sc.beats * k) * BEAT
            tiles.append((f"{sc.name} {t:05.2f}s", TL.render_frame(t)))
    cols = 3
    per_sheet = 12
    for si in range(0, len(tiles), per_sheet):
        chunk = tiles[si:si + per_sheet]
        rows = (len(chunk) + cols - 1) // cols
        sheet = Image.new("RGB", (cols * W * 2, rows * H * 2), (0, 0, 0))
        for i, (label, img) in enumerate(chunk):
            big = img.resize((W * 2, H * 2), Image.NEAREST)
            P.draw_text(big, label, 6, 10, (255, 255, 0), 1, "lm")
            sheet.paste(big, ((i % cols) * W * 2, (i // cols) * H * 2))
        path = OUT_DIR / f"preview_{si // per_sheet + 1}.png"
        sheet.save(path)
        print(path)


def stills(times):
    OUT_DIR.mkdir(parents=True, exist_ok=True)
    for t in times:
        path = OUT_DIR / f"frame_{t:06.2f}.png"
        TL.render_frame(t).resize((W * SCALE, H * SCALE), Image.NEAREST).save(path)
        print(path)


def full(out: Path, t0: float, t1: float):
    import imageio_ffmpeg

    import audio

    OUT_DIR.mkdir(parents=True, exist_ok=True)
    wav = OUT_DIR / "audio.wav"
    audio.build(TL.sfx_events(), TL.DURATION, wav)
    ffmpeg = imageio_ffmpeg.get_ffmpeg_exe()
    cmd = [ffmpeg, "-y", "-loglevel", "error",
           "-f", "rawvideo", "-pix_fmt", "rgb24", "-s", f"{W}x{H}", "-r", str(FPS), "-i", "-",
           "-ss", f"{t0:.3f}", "-i", str(wav),
           "-vf", f"scale={W * SCALE}:{H * SCALE}:flags=neighbor,format=yuv420p",
           "-c:v", "libx264", "-preset", "medium", "-crf", "16", "-tune", "animation",
           "-c:a", "aac", "-b:a", "192k", "-shortest", "-movflags", "+faststart", str(out)]
    proc = subprocess.Popen(cmd, stdin=subprocess.PIPE)
    n0, n1 = int(round(t0 * FPS)), int(round(t1 * FPS))
    start = time.time()
    for f in range(n0, n1):
        proc.stdin.write(TL.render_frame(f / FPS).tobytes())
        if (f - n0) % 150 == 0:
            el = time.time() - start
            print(f"frame {f - n0}/{n1 - n0}  {el:.0f}s", flush=True)
    proc.stdin.close()
    proc.wait()
    print(f"done: {out}  ({time.time() - start:.0f}s)")


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--preview", action="store_true")
    ap.add_argument("--at", type=float, nargs="*")
    ap.add_argument("--out", type=Path, default=OUT_DIR / "pokemon_henshin_promo.mp4")
    ap.add_argument("--start", type=float, default=0.0)
    ap.add_argument("--end", type=float, default=None)
    args = ap.parse_args()
    if args.preview:
        preview()
    elif args.at:
        stills(args.at)
    else:
        full(args.out, args.start, args.end if args.end is not None else TL.DURATION)


if __name__ == "__main__":
    main()
