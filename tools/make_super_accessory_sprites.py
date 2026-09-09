"""DEPRECATED: Super rims are now rebuilt by tools/pixelize_accessories.py.

Prefer:
  python tools/pixelize_accessories.py

That script pixelizes A01–A28 to 64x64 (pixeloe), rebuilds thicker gold+bling Super
icons, and cuts Axx_Shard.png fragments from Assets/Accessories/_src_hires/.
"""
from __future__ import annotations

import runpy
import sys

if __name__ == "__main__":
	print("make_super_accessory_sprites.py is deprecated; running pixelize_accessories.py")
	sys.argv = [sys.argv[0]]
	runpy.run_path(
		__file__.replace("make_super_accessory_sprites.py", "pixelize_accessories.py"),
		run_name="__main__",
	)
