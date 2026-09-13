#!/usr/bin/env python3
"""Measure removed green pixels for Area 1's approved native vine transition.

Only writes a small particle table; original paintings stay non-readable and
unchanged. Positions and timing follow the standalone approved canvas preview.
"""
import json
import math
from pathlib import Path
from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
ART = ROOT / 'Assets/JellyGarden/Resources/Story/TreasureHunt/Art'
OUTPUT = ART.parent / 'Transitions/area_001_task1_vines.json'


def rand(seed):
    value = math.sin(seed * 127.1 + 311.7) * 43758.5453123
    return value - math.floor(value)


def main():
    a, b = [Image.open(ART / f'area_001_state{s}.png').convert('RGB')
            .resize((720, 1280), Image.Resampling.BILINEAR) for s in (0, 1)]
    particles, n = [], 0
    for y in range(270, 900, 14):
        for x in range(5, 715, 14):
            xx, yy = math.floor(x + rand(n + 1) * 10 + .5), math.floor(y + rand(n + 2) * 10 + .5)
            n += 1
            r, g, blue = a.getpixel((xx, yy))
            diff = sum(abs(v - w) for v, w in zip((r, g, blue), b.getpixel((xx, yy))))
            if diff < 100 or not (g > r * .92 and g > blue * 1.23 and blue < 140) or rand(n + 13) < .65:
                continue
            arrival = .08 + .76 * max(0, min(1, (.69 - yy / 1280) / .43)) + .09 * (1 - abs(xx / 720 - .5) * 2)
            start = 1.02 + 2.55 * (arrival + .022 * math.sin(xx * .12)) + .04
            particles.append(dict(x=xx, y=yy, start=start, life=.82 + rand(n) * .67,
                                  seed=n, size=5 + rand(n + 2) * 6.5, side=-1 if xx < 360 else 1,
                                  rotation=rand(n + 3) * 6.28, spin=(rand(n + 4) - .5) * 7,
                                  bright=rand(n + 5) > .6, sparkle=False))
            if rand(n + 7) > .56:
                particles.append(dict(x=xx, y=yy, start=start - .06, seed=n,
                                      size=2 + rand(n + 9) * 2, sparkle=True))
    OUTPUT.parent.mkdir(parents=True, exist_ok=True)
    OUTPUT.write_text(json.dumps({'particles': particles}, separators=(',', ':')) + '\n')
    print(f'{len(particles)} vine particles → {OUTPUT.relative_to(ROOT)}')


if __name__ == '__main__':
    main()
