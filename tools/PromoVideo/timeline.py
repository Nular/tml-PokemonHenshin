"""Beat-based edit decision list: 150 BPM, 150 beats = 60 s."""
from __future__ import annotations

import pixel as P
import scenes as S
from pixel import BEAT

SCENES: list[S.Scene] = [
    S.Scene("hook", 8, S.hook, S.HOOK_SFX, [(3, 0.9, 6), (4, 0.4, 3), (4.5, 0.4, 3), (5, 0.4, 3), (5.5, 0.4, 3)], True,
            overlay=S.hook_overlay, zoom=S.hook_zoom),
    S.Scene("title", 10, S.title, S.TITLE_SFX, [(1, 0.6, 5)]),
    S.Scene("rule", 18, S.rule, S.RULE_SFX, [(2, 0.4, 2), (14, 0.4, 2)], True),
    S.Scene("thunderbolt", 8, S.thunderbolt, S.TB_SFX, [(5, 0.8, 5)]),
    S.Scene("flamethrower", 8, S.flamethrower, S.FT_SFX, [(6, 1.0, 6)]),
    S.Scene("hydropump", 8, S.hydropump, S.HP_SFX, [(2, 0.5, 2), (5.5, 0.8, 5)]),
    S.Scene("ultimate", 10, S.ultimate, S.ULT_SFX, [(4, 0.5, 2), (5, 1.6, 8)], True),
    S.Scene("growth", 22, S.growth, S.GROWTH_SFX, [(11, 0.5, 3), (15.5, 0.9, 5)], True),
    S.Scene("roster", 24, S.roster, S.ROSTER_SFX, [(13, 0.3, 2)], True),
    S.Scene("legends", 18, S.legends, S.LEG_SFX, S.LEG_SHAKES, True),
    S.Scene("cta", 16, S.cta, S.CTA_SFX, [(0.2, 0.4, 3)]),
]

STARTS = []
_acc = 0.0
for _s in SCENES:
    STARTS.append(_acc)
    _acc += _s.beats
TOTAL_BEATS = _acc
DURATION = TOTAL_BEATS * BEAT
WIPE = 0.5


def locate(beat: float) -> tuple[int, float]:
    for i in range(len(SCENES) - 1, -1, -1):
        if beat >= STARTS[i]:
            return i, beat - STARTS[i]
    return 0, beat


def render_frame(t: float):
    beat = t / BEAT
    i, lb = locate(beat)
    sc = SCENES[i]
    c = P.new_canvas()
    sc.draw(c, lb)
    z = sc.zoom(lb) if sc.zoom else None
    if z:
        k, cx, cy = z
        zw, zh = P.W // k, P.H // k
        x0 = int(min(max(cx - zw // 2, 0), P.W - zw))
        y0 = int(min(max(cy - zh // 2, 0), P.H - zh))
        c = c.crop((x0, y0, x0 + zw, y0 + zh)).resize((P.W, P.H), P.Image.NEAREST)
    dx = dy = 0
    for at, dur, amp in sc.shakes:
        ox, oy = P.shake_offset(lb * BEAT, at * BEAT, dur * BEAT, amp)
        dx += ox
        dy += oy
    c = P.apply_shake(c, dx, dy)
    if sc.overlay:
        sc.overlay(c, lb)
    if sc.wipe_out and lb > sc.beats - WIPE:
        P.block_wipe(c, (lb - (sc.beats - WIPE)) / WIPE, seed=i)
    if i > 0 and SCENES[i - 1].wipe_out and lb < WIPE:
        P.block_wipe(c, 1 - lb / WIPE, seed=i, direction=-1)
    return c


def sfx_events() -> list[tuple[float, str, float, float]]:
    """Absolute (seconds, name, gain, pitch)."""
    ev = []
    for start, sc in zip(STARTS, SCENES):
        for item in sc.sfx:
            lb, name, gain, pitch = item
            ev.append(((start + lb) * BEAT, name, gain, pitch))
        if sc.wipe_out:
            ev.append(((start + sc.beats - WIPE) * BEAT, "wipe", 0.35, 1.0))
    return sorted(ev)
