"""Download 52poke HGSS sprites + bag item icons; crop alpha and save."""
import hashlib
import io
import json
import os
import re
import urllib.parse
import urllib.request

from PIL import Image

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
FORMS = os.path.join(ROOT, "Assets", "Forms")
ACCS = os.path.join(ROOT, "Assets", "Accessories")
os.makedirs(FORMS, exist_ok=True)
os.makedirs(ACCS, exist_ok=True)

# FormId -> national dex
FORM_DEX = {
    "L01_F01": 4, "L01_F02": 5, "L01_F03": 6,
    "L02_F01": 7, "L02_F02": 8, "L02_F03": 9,
    "L03_F01": 1, "L03_F02": 2, "L03_F03": 3,
    "L04_F01": 25, "L04_F02": 26,
    "L05_F01": 66, "L05_F02": 67, "L05_F03": 68,
    "L06_F01": 93, "L06_F02": 94,
    "L07_F01": 147, "L07_F02": 148, "L07_F03": 149,
    "L08_F01": 374, "L08_F02": 376,
    "L09_F01": 129, "L09_F02": 130,
    "L10_F01": 50, "L10_F02": 51,
    "L11_F01": 16, "L11_F02": 18,
    "L12_F01": 63, "L12_F02": 65,
    "L13_F01": 95, "L13_F02": 208,
    "L14_F01": 150,
    "L15_F01": 443, "L15_F02": 445,
    "L16_F01": 249,
    "L17_F01": 384,
}

# Accessory wiki file titles (prefer SV bag sprites)
ACC_FILES = {
    "A01": "Bag 特性胶囊 SV Sprite.png",
    "A02": "Bag 力量头带 SV Sprite.png",
    "A03": "Bag 心之水滴 SV Sprite.png",
    "A04": "Bag 轻石 SV Sprite.png",
    "A05": "Bag 气势头带 SV Sprite.png",
    "A06": "Bag 达人带 SV Sprite.png",
    "A07": "Bag 木炭 SV Sprite.png",
    "A08": "Bag 神秘水滴 SV Sprite.png",
    "A09": "Bag 磁铁 SV Sprite.png",
    "A10": "Bag 锐利鸟嘴 SV Sprite.png",
    "A11": "Bag 诅咒之符 SV Sprite.png",
    "A12": "Bag 龙之牙 SV Sprite.png",
    "A13": "Bag 广角镜 SV Sprite.png",
    "A14": "Bag 讲究头带 SV Sprite.png",
    "A15": "Bag 焦点镜 SV Sprite.png",
    "A16": "Bag 生命宝珠 SV Sprite.png",
    "A17": "Bag 贝壳之铃 SV Sprite.png",
    "A18": "Bag 凸凸头盔 SV Sprite.png",
    "A19": "Bag 充电电池 SV Sprite.png",
    "A20": "Bag 光之黏土 SV Sprite.png",
    "A21": "Bag 弱点保险 SV Sprite.png",
    "A22": "Bag 学习装置 SV Sprite.png",
    "A23": "Bag 幸运蛋 SV Sprite.png",
    "A24": "Bag 不变之石 SV Sprite.png",
    "A25": "Bag 黑带 SV Sprite.png",
    "A26": "Bag 吃剩的东西 SV Sprite.png",
    "A27": "Bag 气势披带 SV Sprite.png",
    "A28": "Bag 进化奇石 SV Sprite.png",
    "A29": "Bag 电气球 SV Sprite.png",
}

UA = {"User-Agent": "PokemonHenshinModBot/1.0 (dev asset fetch)"}


def api_image_url(title: str) -> str | None:
    q = urllib.parse.urlencode({
        "action": "query",
        "titles": f"File:{title}",
        "prop": "imageinfo",
        "iiprop": "url",
        "format": "json",
    })
    url = f"https://wiki.52poke.com/api.php?{q}"
    req = urllib.request.Request(url, headers=UA)
    with urllib.request.urlopen(req, timeout=60) as resp:
        data = json.load(resp)
    pages = data.get("query", {}).get("pages", {})
    for page in pages.values():
        infos = page.get("imageinfo")
        if infos:
            return infos[0]["url"]
    return None


def download(url: str) -> bytes:
    req = urllib.request.Request(url, headers=UA)
    with urllib.request.urlopen(req, timeout=60) as resp:
        return resp.read()


def crop_alpha(im: Image.Image) -> Image.Image:
    if im.mode != "RGBA":
        im = im.convert("RGBA")
    # APNG/GIF: take first frame
    try:
        im.seek(0)
    except Exception:
        pass
    im = im.convert("RGBA")
    bbox = im.getbbox()
    if bbox:
        im = im.crop(bbox)
    return im


def save_form(form_id: str, dex: int):
    out = os.path.join(FORMS, f"{form_id}.png")
    if form_id == "L01_F01" and os.path.exists(out):
        print(f"skip existing {form_id}")
        return
    title = f"Spr 4h {dex:03d}.png"
    url = api_image_url(title)
    if not url:
        # fallback media guess
        fname = f"Spr_4h_{dex:03d}.png"
        md5 = hashlib.md5(fname.replace("_", " ").encode()).hexdigest()  # may fail; try api only
        print(f"FAIL form {form_id} title={title}")
        return
    raw = download(url)
    im = Image.open(io.BytesIO(raw))
    im = crop_alpha(im)
    im.save(out, "PNG")
    print(f"OK form {form_id} <- {title} {im.size}")


def save_acc(aid: str, title: str):
    out = os.path.join(ACCS, f"{aid}.png")
    if os.path.exists(out):
        print(f"skip existing {aid}")
        return
    url = api_image_url(title)
    if not url:
        # try without SV
        alt = title.replace(" SV Sprite", " Sprite")
        url = api_image_url(alt)
        title = alt
    if not url:
        print(f"FAIL acc {aid} {title}")
        return
    raw = download(url)
    im = Image.open(io.BytesIO(raw))
    im = crop_alpha(im)
    im.save(out, "PNG")
    print(f"OK acc {aid} <- {title} {im.size}")


def main():
    import sys
    acc_only = "--acc-only" in sys.argv
    if not acc_only:
        for fid, dex in FORM_DEX.items():
            try:
                save_form(fid, dex)
            except Exception as e:
                print(f"ERR form {fid}: {e}")
    for aid, title in ACC_FILES.items():
        try:
            save_acc(aid, title)
        except Exception as e:
            print(f"ERR acc {aid}: {e}")


if __name__ == "__main__":
    main()
