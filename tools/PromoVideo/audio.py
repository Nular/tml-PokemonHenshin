"""Synthesized Terraria-flavoured SFX + an original 150 BPM chiptune track, mixed with sidechain-style ducking."""
from __future__ import annotations

import wave
from functools import lru_cache
from pathlib import Path

import numpy as np

from pixel import BEAT

SR = 44100
RNG = np.random.default_rng(7)


# ---------------------------------------------------------------- primitives

def _t(dur):
    return np.arange(int(dur * SR)) / SR


def _phase(freq, n):
    f = np.broadcast_to(np.asarray(freq, dtype=np.float64), (n,))
    return np.cumsum(f) / SR


def square(freq, dur, duty=0.5):
    n = int(dur * SR)
    ph = _phase(freq, n) % 1.0
    return np.where(ph < duty, 1.0, -1.0)


def triangle(freq, dur):
    n = int(dur * SR)
    ph = _phase(freq, n) % 1.0
    tri = 4 * np.abs(ph - 0.5) - 1
    return np.round(tri * 8) / 8  # 4-bit stepped like the NES triangle


def saw(freq, dur):
    n = int(dur * SR)
    return 2 * (_phase(freq, n) % 1.0) - 1


def sine(freq, dur):
    n = int(dur * SR)
    return np.sin(2 * np.pi * _phase(freq, n))


def nes_noise(dur, rate=8000.0):
    """Sample-and-hold noise like the NES noise channel; rate sets its 'pitch'."""
    n = int(dur * SR)
    hold = max(1, int(SR / rate))
    vals = RNG.choice([-1.0, 1.0], size=n // hold + 2)
    return np.repeat(vals, hold)[:n]


def white(dur):
    return RNG.uniform(-1, 1, int(dur * SR))


def lowpass(x, k):
    k = max(1, int(k))
    if k == 1:
        return x
    return np.convolve(x, np.ones(k) / k, mode="same")


def env(n, attack=0.002, decay=0.1, curve=1.0):
    t = np.arange(n) / SR
    a = np.clip(t / max(attack, 1e-4), 0, 1)
    d = np.exp(-np.maximum(t - attack, 0) / max(decay, 1e-4)) ** curve
    return a * d


def bell(n):
    return np.sin(np.linspace(0, np.pi, n))


def sweep(f0, f1, dur, shape="exp"):
    n = int(dur * SR)
    k = np.linspace(0, 1, n)
    if shape == "exp":
        return f0 * (f1 / f0) ** k
    return f0 + (f1 - f0) * k


def mix(*parts):
    n = max(len(p) for p in parts)
    out = np.zeros(n)
    for p in parts:
        out[:len(p)] += p
    return out


def seq(notes, step, fn):
    """notes: list of freq (or None); each rendered with fn(freq, step) and placed at i*step."""
    out = np.zeros(int((len(notes) * step + 1.0) * SR))
    for i, f in enumerate(notes):
        if f is None:
            continue
        s = fn(f, step)
        st = int(i * step * SR)
        out[st:st + len(s)] += s
    return out


def midi(m):
    return 440.0 * 2 ** ((m - 69) / 12)


# ---------------------------------------------------------------- SFX bank

def _sfx(name: str, p: float) -> np.ndarray:
    if name == "tick":
        d = 0.03
        return square(1800 * p, d, 0.5) * env(int(d * SR), 0.001, 0.01) * 0.5
    if name == "pop":
        d = 0.08
        return square(sweep(420 * p, 980 * p, d), d, 0.25) * env(int(d * SR), 0.001, 0.035) * 0.6
    if name == "grab":
        a = square(660 * p, 0.05, 0.5) * env(int(0.05 * SR), 0.001, 0.02)
        b = square(990 * p, 0.07, 0.5) * env(int(0.07 * SR), 0.001, 0.03)
        return np.concatenate([a, b]) * 0.5
    if name == "coin":
        a = square(1320 * p, 0.05, 0.5) * 0.8
        b = square(1760 * p, 0.14, 0.5) * env(int(0.14 * SR), 0.001, 0.06)
        return np.concatenate([a, b]) * 0.4
    if name == "orb":
        d = 0.12
        return triangle(sweep(880 * p, 1500 * p, d), d) * env(int(d * SR), 0.002, 0.05) * 0.7
    if name == "ding":
        d = 0.35
        return (square(1046 * p, d, 0.25) * 0.6 + triangle(1568 * p, d)) * env(int(d * SR), 0.001, 0.12) * 0.5
    if name == "star":
        notes = [midi(m) * p for m in (96, 100, 103, 108, 103, 108)]
        return seq(notes, 0.045, lambda f, s: triangle(f, s * 2) * env(int(s * 2 * SR), 0.001, 0.04)) * 0.5
    if name in ("swing", "whoosh", "wipe"):
        d = {"swing": 0.18, "whoosh": 0.45, "wipe": 0.22}[name]
        n = int(d * SR)
        x = white(d)
        k0, k1 = (22, 4) if name != "wipe" else (10, 3)
        ks = np.linspace(k0, k1, 6).astype(int)
        seg = np.array_split(np.arange(n), len(ks))
        out = np.zeros(n)
        for k, idx in zip(ks, seg):
            lp = lowpass(x, k)
            out[idx] = (x - lp)[idx] * 0.4 + lp[idx]
        return out * bell(n) * (0.5 if name == "swing" else 0.55) * (0.6 if name == "wipe" else 1)
    if name == "hit":
        d = 0.09
        n = int(d * SR)
        tone = square(sweep(240 * p, 70 * p, d), d, 0.5) * env(n, 0.001, 0.03)
        nz = lowpass(nes_noise(d, 6000), 3) * env(n, 0.001, 0.015)
        return (tone * 0.5 + nz * 0.6) * 0.8
    if name == "kill":
        d = 0.28
        n = int(d * SR)
        tone = square(sweep(320 * p, 55 * p, d), d, 0.5) * env(n, 0.002, 0.08)
        nz = lowpass(white(d), 12) * env(n, 0.002, 0.1) * 2.2
        return (tone * 0.4 + nz * 0.6) * 0.8
    if name == "thud":
        d = 0.45
        n = int(d * SR)
        return (sine(sweep(110, 38, d), d) * env(n, 0.002, 0.15) * 1.0
                + lowpass(white(d), 20) * env(n, 0.001, 0.05) * 2.0) * 0.8
    if name == "poof":
        d = 0.35
        n = int(d * SR)
        return lowpass(white(d), 14) * env(n, 0.01, 0.12) * 1.6 * p
    if name == "thunder":
        d = 0.9
        n = int(d * SR)
        crack = nes_noise(d, 5000 * p) * env(n, 0.001, 0.06)
        flutter = (np.abs(np.sin(_t(d) * 2 * np.pi * 23)) > 0.4).astype(float) * 0.5 + 0.5
        rumble = lowpass(white(d), 60) * env(n, 0.01, 0.35) * 6
        return (crack * flutter * 0.55 + rumble * 0.6 + sine(sweep(80, 40, d), d) * env(n, 0.002, 0.2) * 0.5) * 0.9
    if name == "zap":
        d = 0.38
        n = int(d * SR)
        buzz = square(60 * p, d, 0.5) * square(7 * p, d, 0.5) * 0.3
        hi = nes_noise(d, 9000) * (0.5 + 0.5 * square(32, d, 0.5))
        dn = square(sweep(1400 * p, 300 * p, d), d, 0.25)
        return (buzz + hi * 0.35 + dn * 0.35) * env(n, 0.001, 0.14) * 0.8
    if name in ("fire", "fire_short"):
        d = 2.1 if name == "fire" else 0.7
        n = int(d * SR)
        x = lowpass(nes_noise(d, 2600 * p), 3)
        flutter = 0.7 + 0.3 * np.sin(_t(d) * 2 * np.pi * 11) * np.sin(_t(d) * 2 * np.pi * 3.3)
        shape = np.clip(np.arange(n) / (0.05 * SR), 0, 1) * np.clip((n - np.arange(n)) / (0.25 * SR), 0, 1)
        return x * flutter * shape * 0.45
    if name == "water":
        d = 1.45
        n = int(d * SR)
        x = lowpass(white(d), 6)
        gurgle = 0.6 + 0.4 * np.sin(_t(d) * 2 * np.pi * (14 + 6 * np.sin(_t(d) * 3)))
        tone = triangle(sweep(300, 220, d) * (1 + 0.05 * np.sin(_t(d) * 2 * np.pi * 20)), d) * 0.15
        shape = np.clip(np.arange(n) / (0.04 * SR), 0, 1) * np.clip((n - np.arange(n)) / (0.2 * SR), 0, 1)
        return (x * gurgle * 1.4 + tone) * shape * 0.6
    if name == "charge":
        d = 0.75
        n = int(d * SR)
        f = sweep(180 * p, 1300 * p, d) * (1 + 0.03 * np.sin(_t(d) * 2 * np.pi * 18))
        trem = 0.6 + 0.4 * square(np.linspace(8, 30, n), d, 0.5)
        return (square(f, d, 0.25) * 0.4 + triangle(f * 0.5, d) * 0.4) * trem * np.linspace(0.2, 1, n) * 0.45
    if name == "transform":
        d1 = 0.28
        rise = square(sweep(300 * p, 1600 * p, d1), d1, 0.25) * np.linspace(0.3, 1, int(d1 * SR)) * 0.35
        chord = seq([midi(m) * p for m in (72, 76, 79, 84, 88)], 0.05,
                    lambda f, s: square(f, 0.6, 0.25) * env(int(0.6 * SR), 0.001, 0.2) * 0.22)
        sh = nes_noise(0.8, 12000) * env(int(0.8 * SR), 0.001, 0.25) * 0.2
        return mix(rise, np.concatenate([np.zeros(int(d1 * SR)), chord]),
                   np.concatenate([np.zeros(int(d1 * SR)), sh]))
    if name == "explosion":
        d = 0.8
        n = int(d * SR)
        return (lowpass(nes_noise(d, 1800 * p), 4) * env(n, 0.001, 0.22) * 0.8
                + sine(sweep(90, 30, d), d) * env(n, 0.001, 0.2) * 0.8) * 0.85
    if name == "boom":
        d = 1.6
        n = int(d * SR)
        return (lowpass(nes_noise(d, 1400), 4) * env(n, 0.001, 0.45) * 0.8
                + sine(sweep(70, 25, d), d) * env(n, 0.002, 0.5) * 1.0
                + nes_noise(d, 9000) * env(n, 0.001, 0.08) * 0.4) * 0.95
    if name == "roar":
        d = 1.1
        n = int(d * SR)
        f = sweep(95 * p, 55 * p, d) * (1 + 0.08 * np.sin(_t(d) * 2 * np.pi * 9))
        x = np.tanh(3 * (saw(f, d) + 0.5 * saw(f * 1.01, d))) * 0.5 + lowpass(white(d), 8) * 0.5
        return x * np.clip(np.arange(n) / (0.08 * SR), 0, 1) * env(n, 0.05, 0.45) * 0.8
    if name == "energy":
        d = 0.07
        return square(620 * p, d, 0.5) * env(int(d * SR), 0.001, 0.03) * 0.45
    if name == "ready":
        d = 0.35
        n = int(d * SR)
        return sum(square(midi(m), d, 0.25) for m in (81, 85, 88)) * env(n, 0.001, 0.15) * 0.22
    if name == "cutin":
        d = 0.5
        n = int(d * SR)
        riser = white(d) * np.linspace(0, 1, n) ** 2 * 0.3
        stab = sum(square(midi(m), 0.5, 0.5) for m in (57, 64, 69)) * env(n, 0.001, 0.2) * 0.2
        return np.concatenate([riser * 0.6, stab]) * 0.9
    if name == "dash":
        d = 0.45
        n = int(d * SR)
        x = white(d)
        hp = x - lowpass(x, 6)
        return (hp * bell(n) * 0.6 + square(sweep(1600, 180, d), d, 0.25) * env(n, 0.001, 0.15) * 0.3) * 0.9
    if name == "beam":
        d = 0.85
        n = int(d * SR)
        f = 680 * p * (1 + 0.12 * square(30, d, 0.5))
        return (square(f, d, 0.25) * 0.3 + nes_noise(d, 7000) * 0.2) * np.clip((n - np.arange(n)) / (0.2 * SR), 0, 1) * 0.7
    if name == "levelup":
        notes = [midi(m) * p for m in (72, 76, 79, 84, 88, 91)]
        return seq(notes, 0.07, lambda f, s: (square(f, 0.35, 0.25) * 0.5 + triangle(f, 0.35) * 0.5)
                   * env(int(0.35 * SR), 0.001, 0.12)) * 0.5
    if name == "evolve":
        d = 1.45
        out = np.zeros(int(d * SR))
        tpos, i = 0.0, 0
        while tpos < d - 0.03:
            k = tpos / d
            step = 0.16 * (1 - k) + 0.035
            f = midi(76 if i % 2 == 0 else 83) * p * (1 + k * 0.25)
            s = square(f, step, 0.25) * env(int(step * SR), 0.001, step * 0.6) * 0.35
            st = int(tpos * SR)
            out[st:st + len(s)] += s[:len(out) - st]
            tpos += step
            i += 1
        return out
    if name == "evolved":
        mel = seq([midi(m) * p for m in (67, 72, 76, 79)], 0.08,
                  lambda f, s: square(f, 0.12, 0.25) * env(int(0.12 * SR), 0.001, 0.06) * 0.4)
        chord = sum(square(midi(m) * p, 0.9, 0.5) * 0.12 for m in (72, 76, 79, 84)) * env(int(0.9 * SR), 0.003, 0.35)
        return mix(mel, np.concatenate([np.zeros(int(0.32 * SR)), chord]),
                   np.concatenate([np.zeros(int(0.32 * SR)), nes_noise(0.6, 11000) * env(int(0.6 * SR), 0.001, 0.2) * 0.2]))
    if name == "achievement":
        notes = [midi(m) for m in (79, 84, 88, 91, 96)]
        arp = seq(notes, 0.06, lambda f, s: (triangle(f, 0.9) * 0.6 + sine(f * 2, 0.9) * 0.3) * env(int(0.9 * SR), 0.001, 0.35))
        return arp * 0.45
    if name == "chat":
        a = square(1200, 0.035, 0.5) * env(int(0.035 * SR), 0.001, 0.015)
        b = square(1500, 0.05, 0.5) * env(int(0.05 * SR), 0.001, 0.02)
        return np.concatenate([a, b]) * 0.35
    raise KeyError(name)


@lru_cache(maxsize=None)
def sfx(name: str, pitch: float = 1.0) -> np.ndarray:
    return _sfx(name, pitch).astype(np.float64)


DUCKERS = {"transform": 0.55, "thunder": 0.45, "boom": 0.75, "explosion": 0.4, "evolved": 0.6, "roar": 0.5,
           "levelup": 0.45, "achievement": 0.5, "evolve": 0.45, "cutin": 0.6, "thud": 0.4}


# ---------------------------------------------------------------- music

CHORDS = [  # (bass midi, arp tones) per bar: Am F C G
    (45, (57, 60, 64)), (41, (53, 57, 60)), (48, (55, 60, 64)), (43, (55, 59, 62)),
]
MEL_A = [76, 0, 76, 79, 81, 0, 79, 76, 77, 0, 76, 72, -1, 72, 74, 76,
         79, 0, 76, -1, 72, 74, 76, 79, 74, 0, 71, -1, 67, 0, 0, -1]
MEL_B = [81, 0, 84, 0, 83, 81, 79, 76, 77, 0, 81, 0, 79, 77, 76, 77,
         79, 0, 84, 0, 83, 0, 79, -1, 83, 0, 81, 0, 79, 0, 74, -1]


def _section(beat: float) -> str:
    if beat < 3:
        return "intro"
    if 77 <= beat < 85.5:
        return "evolve"
    if beat >= 148:
        return "end"
    if 116 <= beat < 126:
        return "boss"
    if 36 <= beat < 70 or 92 <= beat < 116:
        return "combat"
    return "main"


def _place(buf, x, at, gain=1.0):
    st = int(at * SR)
    if st >= len(buf):
        return
    end = min(len(buf), st + len(x))
    buf[st:end] += x[:end - st] * gain


def build_music(duration: float) -> np.ndarray:
    n = int((duration + 2) * SR)
    lead, bass, arp, drums = (np.zeros(n) for _ in range(4))
    eighth, six = BEAT / 2, BEAT / 4
    total_beats = int(duration / BEAT)
    kick = sine(sweep(130, 42, 0.14), 0.14) * env(int(0.14 * SR), 0.001, 0.05) * 1.1
    snare = (nes_noise(0.14, 6500) * env(int(0.14 * SR), 0.001, 0.05) * 0.5
             + triangle(sweep(240, 160, 0.14), 0.14) * env(int(0.14 * SR), 0.001, 0.03) * 0.5)
    hat = nes_noise(0.035, 14000) * env(int(0.035 * SR), 0.001, 0.01) * 0.25
    crash = nes_noise(1.2, 11000) * env(int(1.2 * SR), 0.001, 0.4) * 0.3

    for b in range(total_beats):
        sec = _section(b)
        bar = b // 4
        root, tones = CHORDS[bar % 4]
        t0 = b * BEAT
        # drums
        if sec in ("main", "combat", "boss"):
            pos = b % 4
            if pos in (0, 2):
                _place(drums, kick, t0)
            if pos in (1, 3):
                _place(drums, snare, t0)
            if sec != "main" and pos == 3:
                _place(drums, kick, t0 + eighth, 0.8)
            for h in range(2 if sec == "main" else 4):
                _place(drums, hat, t0 + h * (eighth if sec == "main" else six), 1.0 if h % 2 == 0 else 0.6)
        elif sec == "intro":
            _place(drums, hat, t0, 0.8)
            if b == 2:
                for r in range(4):
                    _place(drums, snare, t0 + r * six, 0.4 + r * 0.15)
        elif sec == "evolve" and b % 2 == 0:
            _place(drums, kick, t0, 0.6)
        # bass: octave pumping eighths
        if sec != "end":
            for e in range(2):
                m = root + (12 if e % 2 else 0)
                note = triangle(midi(m), eighth * 0.9) * env(int(eighth * 0.9 * SR), 0.002, 0.25)
                _place(bass, note, t0 + e * eighth, 0.55 if sec != "intro" else 0.35)
        # arp
        if sec in ("combat", "boss"):
            for s in range(4):
                m = tones[(b * 4 + s) % 3] + 12
                note = square(midi(m), six * 0.8, 0.125) * env(int(six * 0.8 * SR), 0.001, 0.06)
                _place(arp, note, t0 + s * six, 0.6)
        # lead melody (8ths); phrase repeats every 16 beats, alternating A/B
        if sec in ("main", "combat", "boss"):
            phrase = MEL_A if (b // 16) % 2 == 0 else MEL_B
            for e in range(2):
                idx = (b % 16) * 2 + e
                m = phrase[idx]
                if m <= 0:
                    continue
                hold = 1
                while idx + hold < 32 and phrase[idx + hold] == 0:
                    hold += 1
                dur = eighth * hold * 0.92
                if sec == "boss":
                    m -= 12
                vib = 1 + 0.012 * np.sin(_t(dur) * 2 * np.pi * 6) * np.clip(_t(dur) / 0.12, 0, 1)
                note = square(midi(m) * vib, dur, 0.25) * env(int(dur * SR), 0.003, 0.35)
                _place(lead, note, t0 + e * eighth, 1.0)
        if b in (4, 8, 36, 70, 86, 92, 116, 126, 134):
            _place(drums, crash, t0)
    # final chord ring-out
    end_t = 148 * BEAT
    chord = sum(square(midi(m), 2.0, 0.25) for m in (57, 64, 69, 72)) * env(int(2.0 * SR), 0.003, 0.8) * 0.18
    _place(lead, chord, end_t)
    _place(bass, triangle(midi(33), 2.0) * env(int(2.0 * SR), 0.003, 0.8), end_t, 0.6)
    _place(drums, crash, end_t, 1.2)
    _place(drums, kick, end_t, 1.0)
    return lead * 0.16 + bass * 0.32 + arp * 0.06 + drums * 0.42


# ---------------------------------------------------------------- mixdown

def build(events, duration: float, out_path: Path):
    n = int((duration + 0.5) * SR)
    music = build_music(duration)[:n]
    fx_bus = np.zeros(n)
    duck = np.zeros(n)
    for at, name, gain, pitch in events:
        s = sfx(name, round(pitch, 3))
        _place(fx_bus, s, at, gain)
        if name in DUCKERS:
            st = int(at * SR)
            ln = int(0.5 * SR)
            curve = DUCKERS[name] * np.concatenate([np.linspace(0, 1, int(0.01 * SR)), np.linspace(1, 0, ln)])
            end = min(n, st + len(curve))
            duck[st:end] = np.maximum(duck[st:end], curve[:end - st])
    total = music * (1 - 0.6 * duck) + fx_bus * 0.75
    fade = int(0.8 * SR)
    total[-fade:] *= np.linspace(1, 0, fade)
    peak = np.max(np.abs(total)) or 1
    total = np.tanh(total / peak * 1.7) / np.tanh(1.7) * 0.93
    pcm = (total * 32767).astype(np.int16)
    stereo = np.repeat(pcm[:, None], 2, axis=1)
    out_path.parent.mkdir(parents=True, exist_ok=True)
    with wave.open(str(out_path), "wb") as w:
        w.setnchannels(2)
        w.setsampwidth(2)
        w.setframerate(SR)
        w.writeframes(stereo.tobytes())
    print(f"audio: {out_path} ({duration:.1f}s, {len(events)} sfx)")
