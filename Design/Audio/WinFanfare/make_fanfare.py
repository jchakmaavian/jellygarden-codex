"""Royal Throne win fanfare: an original herald-trumpet flourish, synthesised from scratch (no samples, no third-party audio).

Timed to RoyalWinSequence.RunCelebration, which starts this clip 0.70 s into the celebration (clip time 0 = celebration 0.70):
  0.05-0.23  "ta-ta-ta" pickups as the pennants unroll
  0.32       first A-major chord
  0.62-0.89  rising arpeggio; the held top note lands as the four trumpets swing in (celebration 1.05-1.43)
  1.23-1.86  cadence IV - I - ii - I6 - V with a snare roll building under the fireworks
  1.95       final A-major chord with cymbal, timpani and a glockenspiel sparkle, peaking with the logo's swell (celebration 2.70)
  2.45       release, so the chord rings out through the cut (celebration 3.05) into the booster session.

Run with numpy: python make_fanfare.py <output.wav>
"""
import sys, wave
import numpy as np

SR = 44100
rng = np.random.default_rng(1307)
out = np.zeros((2, int(4.2 * SR)))

SEMI = {'C': -9, 'C#': -8, 'D': -7, 'D#': -6, 'E': -5, 'F': -4, 'F#': -3, 'G': -2, 'G#': -1, 'A': 0, 'A#': 1, 'B': 2}
def hz(name): return 440.0 * 2 ** ((SEMI[name[:-1]] + (int(name[-1]) - 4) * 12) / 12)

def add(sig, start, pan):
    i = int(start * SR); j = min(out.shape[1], i + len(sig))
    out[0, i:j] += sig[:j - i] * np.cos((pan + 1) * np.pi / 4)
    out[1, i:j] += sig[:j - i] * np.sin((pan + 1) * np.pi / 4)

def smooth(x, n): return np.convolve(x, np.ones(n) / n, 'same')

def brass(f0, dur, dark=0.0, rel=None, cresc=0.0):
    """One brass player: the spectrum opens up with loudness, the pitch scoops into the note, long notes get vibrato."""
    long = dur > 0.2
    rel = rel or (0.07 if not long else 0.10)
    t = np.arange(int((dur + rel) * SR)) / SR
    att = 0.020 if not long else 0.032
    a = np.clip(t / att, 0, 1); a = a * a * (3 - 2 * a)
    sus = 0.80 + cresc * np.clip((t - 0.06) / max(dur - 0.06, 1e-3), 0, 1)
    accent = np.where(t > att, np.exp(-(t - att) / 0.07), 1.0)
    env = a * (sus + (1.0 - sus) * accent) * np.where(t > dur, np.exp(-(t - dur) / (rel / 4)), 1.0)
    bright = np.clip(env, 0, 1.2) ** 0.75
    vib = 0.0035 * np.clip((t - 0.16) / 0.25, 0, 1) * np.sin(2 * np.pi * 5.3 * t + rng.uniform(0, 6.28)) if long else 0
    f = f0 * (1 - 0.028 * np.exp(-t / 0.016)) * (1 + vib)
    phase = 2 * np.pi * np.cumsum(f) / SR
    cut = (1.4 + (5.5 if dark else 8.0) * bright)
    sig = np.zeros_like(t)
    for n in range(1, int(min(26, 9500 / f0)) + 1):
        sig += n ** -0.6 / (1 + (n / cut) ** 3) * np.sin(n * phase)
    breath = smooth(rng.standard_normal(len(t)), 4) * np.exp(-t / 0.02) * 0.08
    return (sig / 3.2 + breath) * env

def section(note, start, dur, vel, pan, players=2, **kw):
    for _ in range(players):
        sig = brass(hz(note) * 2 ** (rng.uniform(-6, 6) / 1200), dur, **kw)
        add(sig * vel / players ** 0.5, start + rng.uniform(0, 0.009), float(np.clip(pan + rng.uniform(-.12, .12), -1, 1)))

def chord(start, dur, notes, vel=1.0, bass=None, horns=None, **kw):
    # Three trumpet parts (melody in the centre), trombones and horns underneath.
    for note, pan, v in zip(notes, (0.0, -0.38, 0.38), (1.0, .78, .70)):
        section(note, start, dur, vel * v, pan, **kw)
    if bass: section(bass, start, dur, vel * .85, 0.05, players=2, dark=1, **kw)
    for h in horns or []: section(h, start + .01, dur, vel * .45, -0.2, players=2, dark=1, **kw)

def timpani(note, start, vel):
    t = np.arange(int(2.2 * SR)) / SR; f0 = hz(note)
    s = sum(a * np.exp(-t / d) * np.sin(2 * np.pi * f0 * r * t * (1 + .012 * np.exp(-t / .03)))
            for r, a, d in ((1, 1, 1.1), (1.505, .45, .7), (1.995, .3, .5), (2.49, .15, .35), (2.98, .08, .25)))
    s += smooth(rng.standard_normal(len(t)), 24) * np.exp(-t / .012) * 3
    add(s * vel * .55, start, 0.15)

def snare(start, vel):
    t = np.arange(int(.25 * SR)) / SR
    nz = rng.standard_normal(len(t)); nz -= smooth(nz, 6)
    s = nz * np.exp(-t / .05) * .55 + np.sin(2 * np.pi * 185 * t) * np.exp(-t / .025) * .5
    add(s * vel * .35, start, -0.1)

def cymbal(start, vel, decay):
    t = np.arange(int(min(decay * 3.5, 2.2) * SR)) / SR
    env = (1 - np.exp(-t / .002)) * np.exp(-t / decay)
    i = int(start * SR); j = min(out.shape[1], i + len(t))
    for ch in range(2):
        nz = rng.standard_normal(len(t)); nz = smooth(nz - smooth(nz, 7), 2)
        out[ch, i:j] += (nz * env * vel * .12)[:j - i]

def glock(note, start, vel):
    t = np.arange(int(1.4 * SR)) / SR; f = hz(note)
    s = sum(a * np.exp(-t / d) * np.sin(2 * np.pi * f * r * t) for r, a, d in ((1, 1, .9), (2.756, .22, .22), (5.404, .07, .07)))
    add(s * (1 - np.exp(-t / .001)) * vel * .16, start, 0.3)

# ------------------------------------------------------------------ the score (clip seconds)
for s in (.05, .14, .23):                                     # ta-ta-ta
    chord(s, .072, ('E4', 'C#4', 'A3'), .80)
chord(.32, .22, ('A4', 'E4', 'C#4'), 1.0, bass='A2', horns=('A3',))
timpani('A2', .32, .9); cymbal(.32, .45, .35)
for s, n in ((.62, ('E4', 'C#4', 'A3')), (.71, ('A4', 'E4', 'C#4')), (.80, ('C#5', 'A4', 'E4'))):
    chord(s, .075, n, .85)
chord(.89, .29, ('E5', 'C#5', 'A4'), 1.05, bass='A2', horns=('E4', 'C#4'), cresc=.15)  # the trumpets arrive
timpani('A2', .89, 1.0); timpani('E2', .89 + .16, .5); cymbal(.89, .75, .6)
cadence = ((1.23, ('F#5', 'D5', 'A4'), 'D3'), (1.33, ('E5', 'C#5', 'A4'), 'A2'), (1.43, ('D5', 'B4', 'F#4'), 'B2'),
           (1.53, ('C#5', 'A4', 'E4'), 'C#3'))
for s, n, b in cadence:
    chord(s, .085, n, .90, bass=b)
chord(1.63, .12, ('B4', 'G#4', 'E4'), .95, bass='E2', horns=('B3',))
for s in (1.77, 1.86):
    chord(s, .072, ('E5', 'B4', 'G#4'), .95, bass='E2')
k = 0
for s in np.arange(1.60, 1.94, .034):                          # the roll into the final chord
    snare(s, .25 + .75 * (s - 1.60) / .34 * (1.0 if k % 2 else .85)); k += 1
chord(1.95, .50, ('A5', 'E5', 'C#5'), 1.25, bass='A2', horns=('A4', 'E4', 'A3'), rel=.42, cresc=.25)
section('A1', 1.95, .50, .55, 0.0, dark=1, rel=.42)
timpani('A2', 1.95, 1.25); timpani('E2', 2.12, .55); timpani('A2', 2.29, .7)
cymbal(1.95, 1.0, .8)
for i, n in enumerate(('A5', 'C#6', 'E6', 'A6', 'C#7')):     # the sparkle
    glock(n, 1.97 + i * .045, 1 - i * .1)

# ------------------------------------------------------------------ room and master
def fftconv(x, h):
    n = 1 << int(np.ceil(np.log2(len(x) + len(h))))
    return np.fft.irfft(np.fft.rfft(x, n) * np.fft.rfft(h, n), n)[:len(x)]

t = np.arange(int(1.8 * SR)) / SR
wet = np.zeros_like(out)
for ch in range(2):
    nz = rng.standard_normal(len(t))
    ir = (nz * np.exp(-t / .10) * .5 + smooth(nz, 9) * 3 * np.exp(-t / .36)) * (t > .011 + .004 * ch)
    wet[ch] = fftconv(out[ch], ir / np.sqrt((ir ** 2).sum()))
mix = out + wet * (.27 * np.sqrt((out ** 2).mean() / (wet ** 2).mean()))

# EQ: low cut, a little brass presence around 1.3 kHz, softer top.
n = 1 << int(np.ceil(np.log2(mix.shape[1])))
f = np.fft.rfftfreq(n, 1 / SR)
g = (f / 40) ** 2 / (1 + (f / 40) ** 2) * (1 + .35 * np.exp(-((np.log2(np.maximum(f, 1) / 1300)) ** 2) / .5)) \
    * (1 / (1 + (f / 6000) ** 2)) ** .5
mix = np.array([np.fft.irfft(np.fft.rfft(c, n) * g, n)[:mix.shape[1]] for c in mix])

mix /= np.abs(mix).max()
mix = np.tanh(1.4 * mix) / np.tanh(1.4)                       # gentle brass "glue"
# The ring-out stops at 3.5 s so it does not hang over the booster session.
end = min(int(3.5 * SR), np.max(np.nonzero(np.abs(mix).max(0) > 10 ** (-58 / 20))))
mix = mix[:, :end]; fade = int(.6 * SR); mix[:, -fade:] *= np.linspace(1, 0, fade) ** 2
mix *= 10 ** (-1 / 20) / np.abs(mix).max()                     # -1 dBFS peak

path = sys.argv[1] if len(sys.argv) > 1 else 'royal-fanfare.wav'
pcm = (mix.T * 32767).astype('<i2')
with wave.open(path, 'wb') as w:
    w.setnchannels(2); w.setsampwidth(2); w.setframerate(SR); w.writeframes(pcm.tobytes())
print(f'{path}: {mix.shape[1] / SR:.2f} s')
