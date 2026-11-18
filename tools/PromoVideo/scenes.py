"""The eight storyboard shots. Each scene draws itself from its local beat `lb` and declares sfx / shake cues
on the same beats so picture and sound stay in sync."""
from __future__ import annotations

import math
from dataclasses import dataclass, field
from functools import lru_cache

from PIL import Image, ImageDraw

import actors as A
import fx as F
import pixel as P
from pixel import BEAT, GOLD, GOLD_DARK, H, SPARK, W, WHITE, YELLOW
from world import GROUND_Y as G
from world import draw_ground, draw_world

BLUE_TXT = (130, 210, 255)
FIRE_TXT = (255, 170, 60)
GREEN = (120, 255, 140)
RED_EDGE = (230, 70, 70)
SLIME_DUST = {k: (v[0], v[2]) for k, v in A.SLIME_COLORS.items()}


@dataclass
class Scene:
    name: str
    beats: float
    draw: callable
    sfx: list = field(default_factory=list)      # (lb, name, gain, pitch)
    shakes: list = field(default_factory=list)   # (lb, dur_beats, amp)
    wipe_out: bool = False
    overlay: callable = None                     # drawn after zoom/shake (screen-space UI)
    zoom: callable = None                        # lb -> (integer zoom, cx, cy) or None


# ---------------------------------------------------------------- shared helpers

def age(lb, at):
    return lb - at


def caption(c, text, lb, at, life=None, y=236, scale=3, color=WHITE, color2=None, band=True):
    a = lb - at
    if a < 0 or (life is not None and a > life):
        return
    if band:
        k = P.clamp01(a / 0.15)
        if life is not None:
            k *= P.clamp01((life - a) / 0.3)
        P.draw(c).rectangle((0, y - 22, W, y + 22), fill=(0, 0, 0, int(120 * k)))
    P.pop_text(c, text, W / 2, y, a, color, scale, color2, life=life)


def sub(c, text, lb, at, life=None, y=260, color=(220, 220, 230), scale=1):
    a = lb - at
    if a < 0 or (life is not None and a > life):
        return
    alpha = P.clamp01(a / 0.2) * (P.clamp01((life - a) / 0.3) if life else 1)
    P.draw_text(c, text, W / 2, y, color, scale, "mm", alpha)


def skill_chip(c, text, lb, at, edge=GOLD):
    a = lb - at
    if a < 0:
        return
    x = 8 - int(60 * (1 - P.ease_out(a / 0.3)))
    P.chip(c, text, x, 8, WHITE, (30, 34, 80), edge, "lt", 1)


def transform_fx(c, x, y, a_b, color=(255, 255, 230)):
    """Henshin burst: light pillar, expanding ring, sparks. a_b = beats since transform."""
    if a_b < 0 or a_b > 2:
        return
    t = a_b * BEAT
    if a_b < 0.6:
        k = a_b / 0.6
        layer = Image.new("RGBA", (W, H), (0, 0, 0, 0))
        w = int(26 * (1 - k)) + 2
        ImageDraw.Draw(layer).rectangle((x - w, 0, x + w, G), fill=color + (int(200 * (1 - k)),))
        P.add_glow(c, layer)
    F.shockwave(c, x, y, t, color, 90, 0.45, 2)
    P.dust_burst(c, x, y, t, ("tf", x, y), n=24, speed=130, colors=(color, SPARK, WHITE), life=0.6, gravity=60)


def hits(c, lb, items):
    """items: (at, value, x, y, crit)"""
    for at, val, x, y, crit in items:
        P.damage_number(c, val, x, y, lb - at, crit)


def boss_bar(c, name, k, alpha=1.0, color=(220, 40, 50)):
    if alpha <= 0:
        return
    x, y, w = 140, 252, 200
    P.rounded_box(c, (x - 6, y - 14, x + w + 6, y + 12), (20, 20, 30), (90, 90, 110), int(200 * alpha), 3, 1)
    P.draw_text(c, name, W / 2, y - 6, WHITE, 1, "mm", alpha)
    P.bar(c, x, y + 2, w, 6, k, color, (50, 20, 25))


def light_rays(c, x, y, t, color=(255, 255, 220), n=10, alpha=70):
    layer = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    d = ImageDraw.Draw(layer)
    for i in range(n):
        a = i / n * math.tau + t * 0.8
        a2 = a + 0.12
        d.polygon([(x, y), (x + math.cos(a) * 400, y + math.sin(a) * 400), (x + math.cos(a2) * 400, y + math.sin(a2) * 400)],
                  fill=color + (alpha,))
    P.add_glow(c, layer)


def speed_lines(c, t, y0, y1, color=(255, 255, 255), n=22, seed=0):
    d = P.draw(c)
    r = P.rng("speed", seed)
    for i in range(n):
        y = r.randint(y0, y1) // 2 * 2
        ln = r.randint(30, 120)
        x = (r.uniform(0, W + 200) - t * r.uniform(900, 1500)) % (W + 200) - 100
        d.rectangle((int(x), y, int(x) + ln, y + 1), fill=color + (r.randint(120, 220),))


def ball_icon():
    return A.item_sprite("ball")


# ---------------------------------------------------------------- 1. hook (0-3.2s)

HOOK_SLIMES = [(70, "green", 1, 4.0), (128, "blue", 1, 4.5), (414, "blue", -1, 5.0), (356, "purple", -1, 5.5)]


def hook(c, lb):
    t = lb * BEAT
    draw_world(c, "night", 0, t)
    for i, (x0, col, dirn, die) in enumerate(HOOK_SLIMES):
        tt = min(t, die * BEAT)
        dx, sy, sq = A.slime_pos(0, G, tt, seed=i, hop_period=0.8, hop_dx=9)
        x = x0 + dirn * dx
        if lb < die:
            A.draw_slime(c, x, sy, sq, col, k=1.4, sil=(255, 255, 160) if die - 0.12 < lb else None)
        else:
            P.dust_burst(c, x, G - 10, (lb - die) * BEAT, ("gel", i), n=16, speed=90, colors=SLIME_DUST[col], life=0.8)
        F.sky_strike(c, x, G, (lb - die) * BEAT, i)
        P.damage_number(c, [312, 298, 655, 307][i], x, G - 34, lb - die, crit=i == 2)
    if lb < 3:
        pose = "stand" if lb < 1.5 else "raise"
        A.draw_player(c, 240, G, t, 1, pose, ball_icon())
        if lb >= 1.5:
            F.charge_orb(c, 252, G - 50, (lb - 1.5) / 1.5, t, SPARK, 1)
    else:
        A.draw_form(c, "L04_F01", 240, G, t, anim="Idle")
        transform_fx(c, 240, G - 24, lb - 3, (255, 240, 120))


def hook_overlay(c, lb):
    if lb < 3:
        P.typewriter(c, "泰拉瑞亚，但是——", W / 2, 54, lb, 6, color=WHITE, scale=3)
    P.pop_text(c, "变身！", W / 2, 84, lb - 3, YELLOW, 7, GOLD_DARK)
    if 3 <= lb < 3.7:
        P.flash(c, 1 - (lb - 3) / 0.7)


def hook_zoom(lb):
    if 1.5 <= lb < 3:
        return 2, 240, G - 44
    return None


HOOK_SFX = ([(i / 6, "tick", 0.35, 1.0 + i * 0.04) for i in range(8)]
            + [(1.5, "grab", 0.6, 1.0), (1.55, "charge", 0.55, 1.0), (3, "transform", 1.0, 1.0), (3, "thunder", 0.6, 1.2)]
            + [(d, "thunder", 0.7, 1.0 + i * 0.05) for i, (_, _, _, d) in enumerate(HOOK_SLIMES)]
            + [(d + 0.05, "kill", 0.55, 1.0) for (_, _, _, d) in HOOK_SLIMES])


# ---------------------------------------------------------------- 2. title (3.2-7.2s)

TITLE_TEAM = [("L01_F01", 150, 4), ("L02_F01", 200, 4.5), ("L03_F01", 280, 5), ("L04_F01", 330, 5.5)]


def title(c, lb):
    t = lb * BEAT
    draw_world(c, "day", t * 18, t)
    if lb < 1:
        y = -50 + (78 + 50) * P.ease_in(lb)
    else:
        y = 78
    P.draw_text(c, "宝可梦之力", W / 2, y, GOLD, 6, "mm", 1, P.BLACK, GOLD_DARK)
    if lb >= 1:
        P.dust_burst(c, W / 2 - 100, 110, (lb - 1) * BEAT, "td1", n=14, speed=80, colors=((255, 255, 255), (255, 230, 150)))
        P.dust_burst(c, W / 2 + 100, 110, (lb - 1) * BEAT, "td2", n=14, speed=80, colors=((255, 255, 255), (255, 230, 150)))
        for i in range(8):
            r = P.rng("tsp", i)
            P.sparkle(c, W / 2 + r.uniform(-170, 170), 78 + r.uniform(-40, 30), (t * 0.9 + r.random()) % 1.0,
                      (255, 255, 220), 4)
    P.pop_text(c, "Pokemon Henshin", W / 2, 138, lb - 2, WHITE, 2)
    if lb >= 3:
        P.chip(c, "泰拉瑞亚 tModLoader 模组", W / 2 - 4, 162, WHITE, (40, 46, 110), GOLD, "rm", 1,
               P.clamp01((lb - 3) / 0.2))
    if lb >= 3.5:
        P.chip(c, "需配合 灾厄 Calamity", W / 2 + 4, 162, (255, 220, 220), (90, 30, 40), RED_EDGE, "lm", 1,
               P.clamp01((lb - 3.5) / 0.2))
    for code, x, at in TITLE_TEAM:
        a = lb - at
        if a < 0:
            continue
        fy = G - 80 * (1 - P.ease_out_back(a / 0.5)) if a < 0.5 else G
        A.draw_form(c, code, x, fy, t, anim="Idle" if code == "L04_F01" else None)
        if a < 1:
            P.dust_burst(c, x, G - 2, a * BEAT - 0.2, ("land", code), n=10, speed=50,
                         colors=((200, 170, 120), (255, 255, 255)), angle=-math.pi / 2, spread=2.4)
    if lb < 0.4:
        P.flash(c, 0.6 * (1 - lb / 0.4))


TITLE_SFX = ([(0, "whoosh", 0.7, 1.0), (1, "thud", 1.0, 1.0), (1.05, "star", 0.5, 1.0), (2, "pop", 0.6, 1.2),
              (3, "tick", 0.4, 1.0), (3.5, "tick", 0.4, 1.2)]
             + [(at + 0.4, "pop", 0.55, 0.9 + i * 0.12) for i, (_, _, at) in enumerate(TITLE_TEAM)])


# ---------------------------------------------------------------- 3. core rule (7.2-14.4s)

def _rule_cam(lb):
    if lb < 3:
        return 0.0
    run = min(lb, 8.5) - 3
    return run * BEAT * 140


def rule(c, lb):
    t = lb * BEAT
    cam = _rule_cam(lb)
    draw_world(c, "day", cam, t)
    sel = 0 if lb < 2 else 1 if lb < 9 else 0 if lb < 14 else 2
    icons = [A.item_sprite("sword"), A.icon("L04_F01", 22), A.icon("L01_F01", 22), A.item_sprite("pickaxe"),
             A.item_sprite("torch"), A.item_sprite("potion")]
    names = ["铜阔剑", "皮卡丘之力", "小火龙之力"]
    P.hotbar(c, 10, 22, icons, sel, 26, 2)
    P.draw_text(c, names[sel], 10, 12, GOLD if sel else WHITE, 1, "lm")
    x = 200
    # enemy slime for the weak sword / ember demo
    if 9 <= lb < 16.5:
        sx = 330 - min(lb - 9, 1.0) * 40
        sq = 1.0 - 0.15 * abs(math.sin(t * 6))
        hurt = any(0 <= lb - h < 0.15 for h in (10.6, 11.6))
        knock = sum(6 * P.ease_out((lb - h) / 0.3) for h in (10.6, 11.6) if lb >= h)
        A.draw_slime(c, sx + knock, G, sq, "green", k=1.5, sil=(255, 255, 255) if hurt else None)
    if lb >= 16.5:
        P.dust_burst(c, 302, G - 10, (lb - 16.5) * BEAT, "rgel", n=16, speed=80, colors=SLIME_DUST["green"])
    hits(c, lb, [(10.6, 3, 300, G - 34, False), (11.6, 4, 306, G - 34, False), (15.6, 86, 304, G - 40, False),
                 (16.1, 92, 304, G - 46, True)])
    if lb < 2:
        A.draw_player(c, x, G, t, 1, "stand", A.item_sprite("sword"))
    elif lb < 9:
        anim = "Run" if 3 <= lb < 8.5 else "Idle"
        A.draw_form(c, "L04_F01", x, G, t, anim=anim)
        if anim == "Run":
            for k in range(3):
                ph = (t * 3 + k / 3) % 1.0
                P.draw(c).rectangle((int(x - 14 - ph * 24), int(G - 2 - ph * 6), int(x - 12 - ph * 24), int(G - ph * 6)),
                                    fill=(200, 170, 120, int(255 * (1 - ph))))
        transform_fx(c, x, G - 24, lb - 2, (255, 240, 120))
    elif lb < 14:
        if 10.3 <= lb < 12:
            sk = ((lb - 10.3) % 1.0) / 0.45
            A.draw_player(c, x, G, t, 1, "swing", A.item_sprite("sword"), swing_k=P.clamp01(sk))
        else:
            A.draw_player(c, x, G, t, 1, "stand", A.item_sprite("sword"))
        P.dust_burst(c, x, G - 22, (lb - 9) * BEAT, "poof", n=22, speed=70, colors=((200, 200, 210), (150, 150, 160)),
                     life=0.6, gravity=-40, size=4)
    else:
        A.draw_form(c, "L01_F01", x, G, t)
        transform_fx(c, x, G - 24, lb - 14, (255, 180, 90))
        if 15 <= lb < 16.5:
            F.fire_stream(c, (x + 18, G - 34), (x + 104, G - 18), t, 3, strength=min(1.0, (lb - 15) * 2), rate=3, n=14)
    chips = [("持握被动", -78, 4), ("技能1", -26, 5), ("技能2", 26, 6), ("能量大招", 78, 7)]
    for text, dx, at in chips:
        a = lb - at
        if 0 <= a < 9 - at:
            yy = G - 76 - (8 * (1 - P.ease_out_back(a / 0.4)))
            P.chip(c, text, x + dx, yy, WHITE, (40, 46, 110), GOLD, "mm", 1, P.clamp01((9 - lb) / 0.3))
    caption(c, "持握即变身", lb, 2, life=6.9)
    sub(c, "外观 · 被动 · 技能 · 大招 同时生效", lb, 2.5, life=6.4)
    caption(c, "切换即解除", lb, 9, life=4.9, color=(200, 220, 255))
    sub(c, "松手立刻失效，回到原样", lb, 9.5, life=4.4)
    caption(c, "随时切换形态", lb, 14, color=FIRE_TXT)
    if 2 <= lb < 2.4:
        P.flash(c, 0.6 * (1 - (lb - 2) / 0.4))
    if 14 <= lb < 14.4:
        P.flash(c, 0.6 * (1 - (lb - 14) / 0.4), (255, 220, 180))


RULE_SFX = [(2, "tick", 0.5, 1.0), (2, "transform", 0.8, 1.15), (4, "pop", 0.5, 1.0), (5, "pop", 0.5, 1.1),
            (6, "pop", 0.5, 1.2), (7, "pop", 0.5, 1.35), (9, "tick", 0.5, 1.0), (9, "poof", 0.7, 1.0),
            (10.3, "swing", 0.6, 1.0), (10.6, "hit", 0.5, 1.0), (11.3, "swing", 0.6, 1.05), (11.6, "hit", 0.5, 1.05),
            (14, "tick", 0.5, 1.0), (14, "transform", 0.8, 0.95), (15, "fire_short", 0.6, 1.2), (15.6, "hit", 0.5, 1.0),
            (16.1, "hit", 0.6, 1.1), (16.5, "kill", 0.6, 1.0)]


# ---------------------------------------------------------------- 4a. thunderbolt

TB_ENEMIES = [(230, "green"), (282, "blue"), (334, "purple"), (386, "blue"), (438, "red")]


def thunderbolt(c, lb):
    t = lb * BEAT
    draw_world(c, "dusk", 300, t)
    px, py = 80, G
    A.draw_form(c, "L04_F01", px, py, t, anim="Idle")
    die = 5.0
    zaps = [1, 2, 3, 4]
    hit_now = set()
    for zi, z in enumerate(zaps):
        a = lb - z
        if 0 <= a < 0.55 and int(a * 12) % 3 != 2:
            chain = [(px + 22, py - 30)]
            for j in range(3):
                ex, _ = TB_ENEMIES[(zi + j) % 5]
                chain.append((ex, G - 12))
                hit_now.add((zi + j) % 5)
            for j in range(len(chain) - 1):
                F.lightning(c, chain[j], chain[j + 1], (zi, j, int(a * 12) // 2), SPARK, width=2, amp=8)
    for i, (ex, col) in enumerate(TB_ENEMIES):
        if lb < die:
            dx, sy, sq = A.slime_pos(0, G, t, seed=i + 3, hop_period=0.6, hop_dx=0, hop_h=6)
            A.draw_slime(c, ex + dx, sy, sq, col, k=1.4, sil=(255, 240, 120) if i in hit_now and int(t * 20) % 2 else None)
        else:
            P.dust_burst(c, ex, G - 12, (lb - die) * BEAT, ("tbg", i), n=14, speed=90, colors=SLIME_DUST[col])
        F.sky_strike(c, ex, G, (lb - die - i * 0.08) * BEAT, i + 20)
    nums = []
    for zi, z in enumerate(zaps):
        for j in range(3):
            ex, _ = TB_ENEMIES[(zi + j) % 5]
            nums.append((z + j * 0.08, 140 + (zi * 37 + j * 53) % 90, ex + (j * 7) % 11 - 5, G - 36 - j * 3, False))
    for i, (ex, _) in enumerate(TB_ENEMIES):
        nums.append((die + i * 0.08, 400 + i * 23, ex, G - 44, True))
    hits(c, lb, nums)
    skill_chip(c, "技能1 · 十万伏特", lb, 0)
    P.pop_text(c, "十万伏特！", W / 2, 62, lb - 0.25, YELLOW, 5, GOLD_DARK, life=4.5)
    caption(c, "连锁电击 · 感电", lb, 5.2, band=True, color=YELLOW)
    if die <= lb < die + 0.4:
        P.flash(c, 0.5 * (1 - (lb - die) / 0.4), (255, 250, 200))


TB_SFX = ([(0, "whoosh", 0.4, 1.3)] + [(z, "zap", 0.7, 1.0 + i * 0.06) for i, z in enumerate([1, 2, 3, 4])]
          + [(z + 0.05, "hit", 0.4, 1.1) for z in [1, 2, 3, 4]]
          + [(5, "thunder", 1.0, 0.9), (5.1, "kill", 0.6, 1.0), (5.3, "kill", 0.5, 1.2)])


# ---------------------------------------------------------------- 4b. flamethrower

def flamethrower(c, lb):
    t = lb * BEAT
    draw_world(c, "night", 600, t)
    cx, cy = 92, G - 46
    A.draw_form(c, "L01_F03", cx, cy + math.sin(t * 5) * 3, t, bob=False)
    die = 6.0
    ex, ey = 360, 118
    if lb < die:
        hurt = 1.0 if 1 <= lb and int(t * 15) % 2 else 0.0
        A.draw_eoc(c, ex + math.sin(t * 7) * 2 * (lb > 1), ey, t, hurt=hurt * 0.8)
    else:
        F.explosion(c, ex, ey, (lb - die) * BEAT, 110, 1)
        F.explosion(c, ex - 30, ey + 20, (lb - die - 0.25) * BEAT, 70, 2)
        P.dust_burst(c, ex, ey, (lb - die) * BEAT, "eocgore", n=28, speed=160, colors=((200, 30, 40), (255, 230, 220)),
                     life=1.0, size=3)
    if 1 <= lb < die:
        F.fire_stream(c, (cx + 36, cy - 52), (ex - 30, ey), t, 7, strength=min(1.0, (lb - 1) * 1.5))
    nums = [(1.2 + i * 0.32, 230 + (i * 97) % 160, ex - 10 + (i * 23) % 34 - 17, ey - 30 - (i % 3) * 6, i % 5 == 4)
            for i in range(15)]
    hits(c, lb, nums)
    boss_bar(c, "克苏鲁之眼", 1 - P.clamp01((lb - 1) / (die - 1)), P.clamp01((die + 0.3 - lb) / 0.3) if lb > 0.3 else lb / 0.3)
    skill_chip(c, "技能2 · 喷射火焰", lb, 0)
    P.pop_text(c, "喷射火焰！", W / 2, 50, lb - 0.25, FIRE_TXT, 5, (220, 60, 30), life=4.5)
    caption(c, "击败 Boss！", lb, die, color=FIRE_TXT, band=False, y=200)
    if die <= lb < die + 0.4:
        P.flash(c, 0.7 * (1 - (lb - die) / 0.4), (255, 200, 140))


FT_SFX = ([(0, "roar", 0.5, 1.4), (1, "fire", 0.9, 1.0)] + [(1.2 + i * 0.32, "hit", 0.32, 0.9 + (i % 4) * 0.08)
                                                          for i in range(15)]
          + [(6, "explosion", 1.0, 1.0), (6.25, "explosion", 0.7, 1.2), (6.1, "kill", 0.6, 0.8)])


# ---------------------------------------------------------------- 4c. hydro pump

HP_SLIMES = [(250, "blue"), (300, "green"), (350, "blue")]


def _crown(c, x, y):
    d = P.draw(c)
    for i in range(3):
        d.rectangle((x - 12 + i * 10, y - 8 - (4 if i == 1 else 0), x - 8 + i * 10, y), fill=(255, 210, 60, 255))
    d.rectangle((x - 14, y - 2, x + 14, y + 2), fill=(255, 210, 60, 255))
    d.rectangle((x - 2, y - 1, x + 1, y + 1), fill=(230, 40, 70, 255))


def hydropump(c, lb):
    t = lb * BEAT
    draw_world(c, "day", 200, t)
    bx = 78
    A.draw_form(c, "L02_F03", bx, G, t)
    by = G - 48
    if lb < 2:
        F.charge_orb(c, bx + 34, by, lb / 2, t, (120, 200, 255), 4)
    fire0, fire1 = 2.0, 5.5
    beam_on = fire0 <= lb < fire1
    if beam_on:
        k = P.ease_out((lb - fire0) / 0.3) * P.clamp01((fire1 - lb) / 0.3)
        F.water_beam(c, bx + 34, by, W + 10, t, 24, k)
    for i, (sx, col) in enumerate(HP_SLIMES):
        if lb < fire0 + 0.2:
            dx, sy, sq = A.slime_pos(0, G, t, seed=i + 7, hop_period=0.7, hop_dx=-6, hop_h=10)
            A.draw_slime(c, sx + dx, sy, sq, col, k=1.4)
        else:
            a = (lb - fire0 - 0.2) * BEAT
            fx_, fy_ = sx - 6 + a * 380, G - 10 - a * 240 + a * a * 300
            if a < 0.9:
                A.draw_slime(c, fx_, fy_, 1.0, col, k=1.4, sil=(255, 255, 255) if a < 0.1 else None)
            P.dust_burst(c, sx, G - 12, a, ("hpd", i), n=10, speed=70, colors=((180, 220, 255), (255, 255, 255)))
    kx = 410
    kdie = 5.5
    if lb < kdie:
        hurt = beam_on and int(t * 15) % 2
        A.draw_slime(c, kx + (2 if hurt else 0), G, 1.0 - 0.08 * abs(math.sin(t * 3)), "blue", k=4.2,
                     sil=(255, 255, 255) if hurt and lb > 2.6 else None)
        _crown(c, kx, G - 68)
    else:
        a = (lb - kdie) * BEAT
        P.dust_burst(c, kx, G - 34, a, "kgel", n=40, speed=200, colors=SLIME_DUST["blue"], life=1.0, size=4)
        F.explosion(c, kx, G - 34, a, 96, 5)
        P.dust_burst(c, kx, G - 70, a, "crown", n=4, speed=120, colors=((255, 210, 60),), life=1.2, size=4)
    nums = [(fire0 + 0.25 + i * 0.1, 180 + i * 31, sx, G - 40, False) for i, (sx, _) in enumerate(HP_SLIMES)]
    nums += [(2.6 + i * 0.3, 260 + (i * 71) % 140, kx - 10 + (i * 13) % 22, G - 92 - (i % 3) * 6, i % 4 == 3)
             for i in range(10)]
    hits(c, lb, nums)
    boss_bar(c, "史莱姆王", 1 - P.clamp01((lb - 2.4) / (kdie - 2.4)), P.clamp01((kdie + 0.3 - lb) / 0.3),
             (60, 120, 255))
    skill_chip(c, "技能1 · 水炮", lb, 0)
    P.pop_text(c, "水炮！", W / 2, 50, lb - 0.25, BLUE_TXT, 6, (40, 110, 230), life=5.0)
    if kdie <= lb < kdie + 0.4:
        P.flash(c, 0.6 * (1 - (lb - kdie) / 0.4), (210, 235, 255))


HP_SFX = ([(0, "charge", 0.6, 0.8), (2, "water", 0.9, 1.0), (2.25, "hit", 0.5, 1.0), (2.35, "hit", 0.5, 1.1),
           (2.45, "hit", 0.5, 1.2)] + [(2.6 + i * 0.3, "hit", 0.3, 0.85 + (i % 3) * 0.1) for i in range(10)]
          + [(5.5, "explosion", 0.9, 1.3), (5.55, "kill", 0.8, 0.7)])


# ---------------------------------------------------------------- 4d. ultimate

ULT_TARGETS = [(300, 100, "eoc"), (400, 70, "eoc"), (270, G, "purple"), (330, G, "red"), (390, G, "purple"),
               (440, G, "red")]


def _ray_path(tt):
    k = P.clamp01(tt / (BEAT * 1.0))
    k = k * k
    return 60 + k * 520, 190 - k * 260


def ultimate(c, lb):
    t = lb * BEAT
    draw_world(c, "corrupt", 900, t)
    boom = 5.0
    if lb < boom:
        for i, (x, y, kind) in enumerate(ULT_TARGETS):
            if kind == "eoc":
                A.draw_eoc(c, x, y, t + i, k=0.7)
            else:
                dx, sy, sq = A.slime_pos(0, G, t, seed=i, hop_period=0.7, hop_dx=0, hop_h=8)
                A.draw_slime(c, x, sy, sq, kind, k=1.4)
    else:
        a = (lb - boom) * BEAT
        for i, (x, y, kind) in enumerate(ULT_TARGETS):
            yy = y if kind == "eoc" else G - 12
            F.explosion(c, x, yy, a - i * 0.06, 84, 10 + i)
        F.shockwave(c, 300, 130, a, (120, 255, 150), 260, 0.7, 4)
        F.shockwave(c, 300, 130, a - 0.1, (255, 255, 200), 200, 0.6, 2)
    ray = A.form("L17_F01", True)
    if lb < 4:
        hx, hy = 72, 160 + math.sin(t * 4) * 4
        P.paste(c, ray, hx, hy, "mm")
        if lb < 3:
            P.dust_burst(c, hx, hy, (t % 0.5), ("aura", int(t / 0.5)), n=8, speed=40, colors=((120, 255, 150),),
                         gravity=-80, life=0.5)
    elif lb < 5.2:
        tt = (lb - 4) * BEAT
        F.afterimages(c, ray, _ray_path, tt, n=8, spacing=0.03)
        x, y = _ray_path(tt)
        P.paste(c, ray, x, y, "mm")
    elif lb >= 7:
        a = lb - 7
        P.paste(c, ray, 72, 160 - 60 * (1 - P.ease_out(a / 0.6)) + math.sin(t * 4) * 4, "mm", P.clamp01(a / 0.3))
    # energy bar fills, then the cut-in
    if lb < 4:
        k = P.clamp01(lb / 3)
        full = lb >= 3
        col = (120, 255, 150) if not full or int(t * 12) % 2 else (255, 255, 255)
        P.rounded_box(c, (130, 238, 350, 262), (20, 20, 30), GOLD if full else (90, 90, 110), 200, 3, 1)
        P.draw_text(c, "能量", 152, 250, WHITE, 1, "mm")
        P.bar(c, 170, 246, 170, 8, k, col, (30, 40, 30))
        if full:
            P.pop_text(c, "MAX!", 300, 226, lb - 3, GOLD, 2)
    if 3 <= lb < 4.2:
        a = lb - 3
        P.darken(c, 0.55)
        band_h = int(46 * P.ease_out(a / 0.2))
        d = P.draw(c)
        d.rectangle((0, 135 - band_h, W, 135 + band_h), fill=(10, 40, 20, 235))
        d.rectangle((0, 135 - band_h, W, 135 - band_h + 1), fill=(120, 255, 150, 255))
        d.rectangle((0, 135 + band_h - 2, W, 135 + band_h - 1), fill=(120, 255, 150, 255))
        speed_lines(c, a * BEAT, 135 - band_h + 4, 135 + band_h - 4, (160, 255, 180), seed=4)
        big = P.scale_nearest(ray, 1.4)
        P.paste(c, big, W + 80 - 240 * P.ease_out(a / 0.3) - a * 30, 135, "mm")
        P.pop_text(c, "画龙点睛！", 170, 135, a - 0.1, (190, 255, 190), 5, (40, 200, 90))
    if boom <= lb < boom + 0.5:
        P.flash(c, 1 - (lb - boom) / 0.5)
    if lb >= boom:
        P.damage_number(c, "28650", 300, 112, (lb - boom) * 0.6, crit=True, scale=4)
    skill_chip(c, "能量大招 · 画龙点睛", lb, 0, (120, 255, 150))
    caption(c, "能量满 · 一键大招", lb, 6, color=(170, 255, 180))


ULT_SFX = ([(i * 0.5, "energy", 0.45, 1.0 + i * 0.08) for i in range(6)] + [(3, "ready", 0.8, 1.0),
           (3, "cutin", 0.9, 1.0), (4, "dash", 0.9, 1.0), (5, "boom", 1.0, 1.0), (5.2, "explosion", 0.7, 1.1),
           (5.4, "explosion", 0.6, 0.9), (7, "whoosh", 0.4, 1.4)])


# ---------------------------------------------------------------- 5. growth & evolution (28-36.8s)

def _evo_sprite(lb, a_code, b_code, start, end):
    """Classic flicker: alternate white silhouettes, accelerating."""
    span = end - start
    k = (lb - start) / span
    period = 0.5 * (1 - k) + 0.05
    phase = int(((lb - start) ** 1.0) / period)
    return b_code if phase % 2 else a_code


def growth(c, lb):
    t = lb * BEAT
    draw_world(c, "day", 1200, t)
    x = 240
    evo1, evo1_end = 7.0, 11.0
    evo2, evo2_end = 12.0, 15.5
    if evo1 <= lb < evo2_end:
        P.darken(c, 0.55 * P.clamp01((lb - evo1) / 0.4), (10, 10, 30))
        light_rays(c, x, G - 34, t, (255, 255, 230), 12, 50)
    if lb < evo1:
        code, sil = "L01_F01", None
    elif lb < evo1_end:
        code, sil = _evo_sprite(lb, "L01_F01", "L01_F02", evo1, evo1_end), (255, 255, 255)
    elif lb < evo2:
        code, sil = "L01_F02", None
    elif lb < evo2_end:
        code, sil = _evo_sprite(lb, "L01_F02", "L01_F03", evo2, evo2_end), (255, 255, 255)
    else:
        code, sil = "L01_F03", None
    A.draw_form(c, code, x, G, t, sil=sil, bob=sil is None)
    for at, col in ((evo1_end, FIRE_TXT), (evo2_end, GOLD)):
        transform_fx(c, x, G - 40, lb - at, (255, 230, 160))
        if at <= lb < at + 0.5:
            P.flash(c, 1 - (lb - at) / 0.5)
    for i in range(10):
        r = P.rng("evs", i)
        if lb >= evo2_end:
            P.sparkle(c, x + r.uniform(-70, 70), G - r.uniform(10, 110), (t * 1.3 + r.random()) % 1.0, (255, 240, 180), 3)
    # level & xp
    if lb < evo1:
        lvl_up = 3.0
        xp = P.lerp(0.3, 1.0, P.clamp01(lb / lvl_up)) if lb < lvl_up else P.clamp01((lb - lvl_up) * 0.2)
        lv = 14 if lb < lvl_up else 15
        P.draw_text(c, f"Lv.{lv}", x, G - 70, GOLD if lv == 15 else WHITE, 1, "mm")
        P.bar(c, x - 30, G - 62, 60, 4, xp, (90, 230, 110), (20, 40, 20))
        for i in range(6):
            at = 0.3 + i * 0.45
            a = (lb - at) * BEAT
            if 0 <= a < 0.45:
                r = P.rng("orb", i)
                sx, sy = r.choice((-40, 520)), r.uniform(40, 200)
                k = P.ease_in(a / 0.45)
                ox, oy = sx + (x - sx) * k, sy + (G - 30 - sy) * k
                P.draw(c).rectangle((int(ox) - 2, int(oy) - 2, int(ox) + 2, int(oy) + 2), fill=(140, 255, 140, 255))
                P.draw(c).rectangle((int(ox) - 1, int(oy) - 1, int(ox), int(oy)), fill=(255, 255, 255, 255))
            P.damage_number(c, f"EXP +{[38, 52, 41, 77, 60, 45][i]}", x + 66, G - 26, (lb - at - 1.1) * 1.4,
                            color=GREEN, scale=1)
        P.pop_text(c, "Lv.UP!", x, G - 92, lb - lvl_up, GOLD, 3, GOLD_DARK, life=3.6)
        P.pop_text(c, "攻击 +12", x - 64, G - 40, lb - 3.6, (255, 150, 120), 1, life=3)
        P.pop_text(c, "防御 +8", x + 62, G - 40, lb - 4.0, (150, 200, 255), 1, life=2.6)
        caption(c, "每件之力 独立升级", lb, 0.2, life=6.6)
        # evolution conditions panel
        if lb >= 4.6:
            a = lb - 4.6
            P.rounded_box(c, (300, 40, 470, 98), (30, 36, 90), GOLD, int(220 * P.clamp01(a / 0.2)), 4, 1)
            P.draw_text(c, "进化条件", 385, 52, GOLD, 1, "mm")
            P.draw_text(c, "击败对应 Boss", 312, 70, WHITE, 1, "lm")
            P.draw_text(c, "等级达到要求", 312, 86, WHITE, 1, "lm")
            P.pop_text(c, "√", 452, 70, lb - 5.3, GREEN, 2)
            P.pop_text(c, "√", 452, 86, lb - 6.0, GREEN, 2)
    P.pop_text(c, "火恐龙！", x, 52, lb - evo1_end, FIRE_TXT, 4, (220, 60, 30), life=1.0)
    P.pop_text(c, "喷火龙！", x, 52, lb - evo2_end, GOLD, 5, GOLD_DARK)
    caption(c, "Boss进度 + 等级 → 进化", lb, 7)


GROWTH_SFX = ([(0.3 + i * 0.45 + 0.4, "orb", 0.45, 1.0 + i * 0.07) for i in range(6)]
              + [(3, "levelup", 0.9, 1.0), (3.6, "pop", 0.4, 1.0), (4.0, "pop", 0.4, 1.1), (5.3, "ding", 0.6, 1.0),
                 (6.0, "ding", 0.6, 1.2), (7, "evolve", 0.8, 1.0), (11, "evolved", 0.9, 1.0),
                 (12, "evolve", 0.8, 1.15), (15.5, "evolved", 1.0, 1.0), (15.5, "roar", 0.7, 1.1)])


# ---------------------------------------------------------------- 6. roster & accessories (36.8-46.4s)

ROSTER_START, ROSTER_STEP = 0.4, 0.27
LEGENDS = ("L14_F01", "L16_F01", "L17_F01")


def roster(c, lb):
    t = lb * BEAT
    draw_world(c, "night", 1500, t)
    P.darken(c, 0.35, (10, 10, 40))
    acc_phase = lb >= 13
    if not acc_phase:
        cols, size, gap = 9, 40, 4
        x0 = (W - (cols * (size + gap) - gap)) // 2
        y0 = 66
        P.rounded_box(c, (x0 - 8, y0 - 8, x0 + cols * (size + gap) + 4, y0 + 4 * (size + gap) + 4), (30, 40, 100),
                      (120, 140, 220), 170, 6, 2)
        shown = 0
        for i, code in enumerate(A.ALL_FORMS):
            sx = x0 + (i % cols) * (size + gap)
            sy = y0 + (i // cols) * (size + gap)
            a = lb - (ROSTER_START + i * ROSTER_STEP)
            legend = code in LEGENDS and lb >= 11
            P.slot(c, sx, sy, size, legend and int(t * 6) % 2 == 0, alpha=180)
            if a >= 0:
                shown += 1
                ic = A.icon(code, 36)
                if a < 0.15:
                    ic = P.scale_nearest(ic, 1.25)
                P.paste(c, ic, sx + size / 2, sy + size / 2, "mm")
                if a < 0.3:
                    P.paste(c, A.silhouette(A.icon(code, 36)), sx + size / 2, sy + size / 2, "mm", 1 - a / 0.3)
        P.draw_text(c, f"{shown} 种形态", W / 2, 34, GOLD, 4 if lb >= 10.3 and lb < 10.45 else 3, "mm", 1, P.BLACK,
                    GOLD_DARK)
        if lb >= 11:
            P.pop_text(c, "含传说之力", W / 2, 252, lb - 11, (255, 200, 255), 2)
    else:
        a0 = lb - 13
        cols, size, gap = 10, 36, 4
        x0 = (W - (cols * (size + gap) - gap)) // 2
        y0 = 84
        P.rounded_box(c, (x0 - 8, y0 - 8, x0 + cols * (size + gap) + 4, y0 + 3 * (size + gap) + 4), (60, 36, 90),
                      (200, 150, 240), 170, 6, 2)
        accs = sorted(p.stem for p in (P.ASSETS / "Accessories").glob("A??.png"))
        for i, code in enumerate(accs):
            sx = x0 + (i % cols) * (size + gap)
            sy = y0 + (i // cols) * (size + gap)
            P.slot(c, sx, sy, size, False, alpha=170, fill=(90, 60, 130))
            at = 0.3 + i * 0.24
            a = a0 - at
            if a < 0:
                continue
            ic = _acc_icon(code)
            fall = 1 - P.ease_out(a / 0.35)
            P.paste(c, ic, sx + size / 2, sy + size / 2 - fall * 120, "mm", P.clamp01(a / 0.1))
        P.pop_text(c, "29 件变身专属饰品", W / 2, 44, a0, (240, 200, 255), 3, (190, 120, 255))
        sub(c, "仅变身时生效 · 与之力搭配构筑", lb, 21, y=230, scale=1, color=WHITE)
        P.pop_text(c, "搭配流派 随你构筑", W / 2, 252, lb - 21.2, GOLD, 2)
    if 13 <= lb < 13.35:
        P.flash(c, 0.8 * (1 - (lb - 13) / 0.35), (230, 200, 255))


@lru_cache(maxsize=None)
def _acc_icon(code):
    img = Image.open(P.ASSETS / "Accessories" / f"{code}.png").convert("RGBA")
    img = img.crop(img.getbbox())
    return A.shrink(img, 30 / max(img.size))


ROSTER_SFX = ([(ROSTER_START + i * ROSTER_STEP, "tick", 0.28, 0.9 + i * 0.025) for i in range(36)]
              + [(10.3, "levelup", 0.7, 1.2), (11, "star", 0.6, 1.0), (13, "whoosh", 0.6, 1.0)]
              + [(13 + 0.3 + i * 0.24 + 0.35, "coin", 0.22, 0.95 + (i % 6) * 0.06) for i in range(29)]
              + [(21.2, "pop", 0.5, 1.0)])


# ---------------------------------------------------------------- 7. legends vs boss + multiplayer (46.4-53.6s)

LEG_POS = [("L14_F01", 70, 170, 1.5, "超梦", (230, 160, 255)), ("L16_F01", 140, 110, 2.5, "洛奇亚", (190, 220, 255)),
           ("L17_F01", 70, 70, 3.5, "烈空坐", (140, 255, 160))]
MP_PLAYERS = [(84, "小智", "L04_F01", (255, 230, 120), 0.5), (190, "小霞", "L09_F02", (140, 200, 255), 1.0),
              (296, "小刚", "L13_F01", (255, 180, 120), 1.5), (400, "小茂", "L02_F03", (200, 160, 255), 2.0)]
MP_HAIR = [{"H": (40, 30, 30), "h": (25, 20, 20), "T": (60, 90, 200)}, {"H": (230, 120, 40), "h": (190, 90, 30), "T": (240, 220, 80)},
           {"H": (60, 40, 30), "h": (40, 28, 20), "T": (90, 140, 70)}, {"H": (190, 140, 80), "h": (150, 105, 60), "T": (110, 70, 150)}]


def legends(c, lb):
    t = lb * BEAT
    if lb < 10:
        draw_world(c, "corrupt", 1800, t, ground=False)
        rise = P.ease_out(lb / 1.0)
        bx, by = 350, 330 - 180 * rise
        hurt = 1.0 if 6 <= lb < 8 and int(t * 14) % 2 else 0.0
        A.draw_boss(c, bx, by, t, eye_glow=1.0, hurt=hurt)
        draw_ground(c, "corrupt", 1800, trees=False)
        fire = 6 <= lb < 8
        for code, x, y, at, name, col in LEG_POS:
            a = lb - at
            if a < 0:
                continue
            img = A.form(code, True)
            yy = y + math.sin(t * 3 + x) * 3
            P.paste(c, img, x, yy - 18 * (1 - P.ease_out_back(a / 0.4)), "mm")
            transform_fx(c, x, yy, a, col)
            P.draw_text(c, name, x, yy + img.height // 2 + 6, col, 1, "mm", P.clamp01(a / 0.3))
            if fire:
                targets = {"L14_F01": (bx, by - 79), "L16_F01": (bx - 124, by - 81), "L17_F01": (bx + 124, by - 81)}
                F.psy_beam(c, (x + 20, yy), targets[code], t, col, 6)
        if 8 <= lb:
            a = (lb - 8) * BEAT
            F.explosion(c, bx, by - 79, a, 120, 3)
            F.explosion(c, bx - 124, by - 81, a - 0.08, 80, 4)
            F.explosion(c, bx + 124, by - 81, a - 0.16, 80, 5)
        hits(c, lb, [(6.1 + i * 0.25, 1800 + (i * 677) % 2400, bx + (i * 41) % 120 - 60, by - 100 - (i % 3) * 8, i % 3 == 2)
                     for i in range(8)] + [(8, 12888, bx, by - 130, True)])
        caption(c, "传说之力 压轴登场", lb, 1.5, life=3.0, color=GOLD, color2=GOLD_DARK)
        caption(c, "数值对齐灾厄全流程", lb, 4.5, life=5.4, color=(255, 200, 200))
        if 8 <= lb < 8.4:
            P.flash(c, 0.9 * (1 - (lb - 8) / 0.4))
    else:
        a0 = lb - 10
        draw_world(c, "day", 2100 + a0 * BEAT * 10, t)
        for i, (x, name, code, col, at) in enumerate(MP_PLAYERS):
            a = a0 - at
            if a < 0:
                A.draw_player(c, x, G, t + i, 1 if x < 240 else -1, "stand", palette=MP_HAIR[i])
                top = G - 46
            else:
                img = A.draw_form(c, code, x, G, t + i * 0.3, face_right=x < 240, anim="Idle" if code == "L04_F01" else None)
                transform_fx(c, x, G - 24, a, col)
                top = G - img.height - 4
                if 2.2 <= a0 and int((a0 - 2.2) / 0.5) % 2 == i % 2:
                    P.sparkle(c, x + 20, top + 8, (t * 2) % 1.0, col, 3)
            P.draw_text(c, name, x, top - 4, col, 1, "mb")
        P.pop_text(c, "支持多人联机", W / 2, 40, a0 - 0.2, WHITE, 4, (160, 210, 255))
        sub(c, "形态 · 能量 · 等级 全部同步", lb, 10.8, y=70, color=(220, 230, 255))
        P.chat_line(c, 8, 246, "小智", "皮卡丘，就决定是你了！", a0 - 2.6)
        P.chat_line(c, 8, 260, "小刚", "大岩蛇，上吧！", a0 - 4.0)


LEG_SFX = ([(0, "roar", 1.0, 0.8), (1.5, "transform", 0.6, 1.2), (2.5, "transform", 0.6, 1.0),
            (3.5, "transform", 0.6, 0.85), (6, "beam", 0.9, 1.0)]
           + [(6.1 + i * 0.25, "hit", 0.35, 0.9 + (i % 3) * 0.1) for i in range(8)]
           + [(8, "boom", 1.0, 0.9), (8.1, "roar", 0.6, 0.6)]
           + [(10 + at, "transform", 0.55, 1.0 + i * 0.1) for i, (_, _, _, _, at) in enumerate(MP_PLAYERS)]
           + [(12.6, "chat", 0.5, 1.0), (14.0, "chat", 0.5, 1.0)])
LEG_SHAKES = [(0, 1.5, 4), (8, 1.2, 7)]


# ---------------------------------------------------------------- 8. call to action (53.6-60s)

CTA_LINE = [("L03_F03", 60), ("L02_F03", 125), ("L01_F03", 196), ("L04_F01", 262), ("L14_F01", 318),
            ("L15_F02", 380), ("L17_F01", 444)]
SEARCH_TEXT = "Pokemon Henshin"


def cta(c, lb):
    t = lb * BEAT
    draw_world(c, "dusk", 2400, t)
    for i, (code, x) in enumerate(CTA_LINE):
        a = lb - i * 0.15
        if a < 0:
            continue
        fy = G - 60 * (1 - P.ease_out_back(a / 0.5)) if a < 0.5 else G
        A.draw_form(c, code, x, fy, t + i * 0.2, face_right=x < 240, anim="Idle" if code == "L04_F01" else None)
    P.pop_text(c, "宝可梦之力", W / 2, 48, lb - 0.2, GOLD, 5, GOLD_DARK)
    P.pop_text(c, "Pokemon Henshin", W / 2, 86, lb - 1.2, WHITE, 2)
    if lb >= 2.5:
        a = lb - 2.5
        al = P.clamp01(a / 0.2)
        P.rounded_box(c, (110, 104, 370, 146), (24, 28, 70), GOLD, int(225 * al), 5, 2)
        P.draw_text(c, "tModLoader 模组浏览器 搜索", W / 2, 114, WHITE, 1, "mm", al)
        P.draw_text(c, SEARCH_TEXT, W / 2, 133, YELLOW, 2, "mm", al)
    if lb >= 4:
        P.chip(c, "前置：灾厄 Calamity Mod", W / 2, 158, (255, 220, 220), (90, 30, 40), RED_EDGE, "mm", 1,
               P.clamp01((lb - 4) / 0.2))
    if lb >= 6:
        a = lb - 6
        slide = P.ease_out_back(a / 0.4, 1.2)
        px = W + 10 - 196 * slide
        py = 168
        P.rounded_box(c, (px, py, px + 186, py + 34), (20, 22, 40), GOLD, 235, 4, 2)
        P.slot(c, px + 4, py + 4, 26, True, A.item_sprite("ball"))
        P.draw_text(c, "成就达成！", px + 36, py + 11, GOLD, 1, "lm")
        P.draw_text(c, "宝可梦大师", px + 36, py + 25, WHITE, 1, "lm")
        for i in range(5):
            r = P.rng("ach", i)
            P.sparkle(c, px + r.uniform(0, 186), py + r.uniform(0, 34), (t * 1.6 + r.random()) % 1.0, (255, 240, 160), 2)
    caption(c, "关注作者 获取更新", lb, 9, color=WHITE, y=242)
    if lb >= 15:
        P.darken(c, P.clamp01((lb - 15) / 1.0))


CTA_SFX = [(0.2, "thud", 0.8, 1.1), (1.2, "pop", 0.5, 1.0), (2.5, "tick", 0.5, 1.0), (4, "tick", 0.4, 1.2),
           (6, "achievement", 1.0, 1.0), (9, "pop", 0.5, 1.1)]
