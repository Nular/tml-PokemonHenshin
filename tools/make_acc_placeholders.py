"""Write 32x32 placeholder accessory icons when wiki fetch fails."""
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[1]
ACCS = ROOT / "Assets" / "Accessories"
ACCS.mkdir(parents=True, exist_ok=True)

PALETTE = [
	(220, 80, 80), (80, 160, 220), (80, 200, 120), (240, 200, 80),
	(180, 100, 220), (240, 140, 60), (60, 180, 180), (200, 80, 140),
]


def placeholder(aid: str, idx: int) -> None:
	color = PALETTE[idx % len(PALETTE)]
	im = Image.new("RGBA", (32, 32), (0, 0, 0, 0))
	d = ImageDraw.Draw(im)
	d.rounded_rectangle((1, 1, 30, 30), radius=6, fill=color + (230,), outline=(255, 255, 255, 220))
	d.text((6, 10), aid[1:], fill=(255, 255, 255, 255))
	im.save(ACCS / f"{aid}.png")


def main() -> None:
	for i in range(1, 30):
		aid = f"A{i:02d}"
		path = ACCS / f"{aid}.png"
		if not path.exists():
			placeholder(aid, i)
			print("placeholder", aid)


if __name__ == "__main__":
	main()
