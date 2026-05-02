#!/usr/bin/env python3
"""Generates procedural sounds (menu music, button SFX, combat SFX).

All audio is original synthesized waveforms — no copyrighted material.
Output: Assets/Resources/Sounds/*.wav (16-bit PCM, 22050 Hz mono)
"""
import os, math, struct, wave, random

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT = os.path.join(ROOT, "Assets", "Resources", "Sounds")
os.makedirs(OUT, exist_ok=True)

SR = 22050  # sample rate


def write_wav(name, samples, sr=SR):
    path = os.path.join(OUT, name + ".wav")
    # clip and convert to int16
    out = []
    for s in samples:
        v = max(-1.0, min(1.0, s))
        out.append(int(v * 32760))
    with wave.open(path, "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(sr)
        w.writeframes(struct.pack("<" + "h" * len(out), *out))
    print(f"{name}.wav saved ({len(out)/sr:.2f}s)")


def env_adsr(n, a=0.01, d=0.05, s_level=0.6, r=0.05, sr=SR):
    """ADSR envelope of n samples."""
    a_n = max(1, int(a * sr))
    d_n = max(1, int(d * sr))
    r_n = max(1, int(r * sr))
    s_n = max(0, n - a_n - d_n - r_n)
    out = []
    for i in range(a_n):
        out.append(i / a_n)
    for i in range(d_n):
        t = i / d_n
        out.append(1.0 * (1 - t) + s_level * t)
    for i in range(s_n):
        out.append(s_level)
    for i in range(r_n):
        out.append(s_level * (1 - i / r_n))
    return out[:n] + [0.0] * (n - len(out)) if len(out) < n else out[:n]


def sine(freq, dur, amp=0.5, sr=SR, phase=0.0):
    n = int(dur * sr)
    return [amp * math.sin(2 * math.pi * freq * (i / sr) + phase) for i in range(n)]


def square(freq, dur, amp=0.4, sr=SR, duty=0.5):
    n = int(dur * sr)
    out = []
    for i in range(n):
        t = (freq * i / sr) % 1.0
        out.append(amp if t < duty else -amp)
    return out


def saw(freq, dur, amp=0.4, sr=SR):
    n = int(dur * sr)
    return [amp * (2 * ((freq * i / sr) % 1.0) - 1) for i in range(n)]


def triangle(freq, dur, amp=0.4, sr=SR):
    n = int(dur * sr)
    out = []
    for i in range(n):
        t = (freq * i / sr) % 1.0
        out.append(amp * (4 * abs(t - 0.5) - 1))
    return out


def noise(dur, amp=0.5, sr=SR, seed=None):
    n = int(dur * sr)
    rng = random.Random(seed)
    return [amp * (rng.random() * 2 - 1) for _ in range(n)]


def add(a, b):
    n = max(len(a), len(b))
    out = [0.0] * n
    for i, v in enumerate(a):
        out[i] += v
    for i, v in enumerate(b):
        out[i] += v
    return out


def mix(a, b, gain_a=1.0, gain_b=1.0):
    n = max(len(a), len(b))
    out = [0.0] * n
    for i, v in enumerate(a):
        out[i] += v * gain_a
    for i, v in enumerate(b):
        out[i] += v * gain_b
    return out


def apply_env(s, env):
    return [s[i] * env[i] for i in range(min(len(s), len(env)))]


def lowpass(samples, cutoff_freq, sr=SR):
    """Simple 1-pole lowpass."""
    if not samples:
        return samples
    rc = 1.0 / (2 * math.pi * cutoff_freq)
    dt = 1.0 / sr
    a = dt / (rc + dt)
    out = [0.0] * len(samples)
    out[0] = samples[0] * a
    for i in range(1, len(samples)):
        out[i] = out[i - 1] + a * (samples[i] - out[i - 1])
    return out


# Notes (Hz) — C major-ish pentatonic, easy listening
NOTES = {
    "C4": 261.63, "D4": 293.66, "E4": 329.63, "F4": 349.23, "G4": 392.00, "A4": 440.00,
    "B4": 493.88, "C5": 523.25, "D5": 587.33, "E5": 659.25, "F5": 698.46, "G5": 783.99,
    "A5": 880.00, "C3": 130.81, "D3": 146.83, "E3": 164.81, "F3": 174.61, "G3": 196.00,
    "A3": 220.00, "B3": 246.94,
}


def play_note(freq, dur, waveform="sine", amp=0.4, attack=0.01, release=0.08):
    if waveform == "sine":
        s = sine(freq, dur, amp)
    elif waveform == "square":
        s = square(freq, dur, amp)
    elif waveform == "saw":
        s = saw(freq, dur, amp)
    elif waveform == "triangle":
        s = triangle(freq, dur, amp)
    else:
        s = sine(freq, dur, amp)
    e = env_adsr(len(s), a=attack, d=0.04, s_level=0.7, r=release)
    return apply_env(s, e)


def play_chord(freqs, dur, waveform="triangle", amp=0.18):
    """Multiple notes layered."""
    base = [0.0] * int(dur * SR)
    for f in freqs:
        n = play_note(f, dur, waveform, amp, attack=0.02, release=0.15)
        for i, v in enumerate(n):
            if i < len(base):
                base[i] += v
    return base


def silence(dur, sr=SR):
    return [0.0] * int(dur * sr)


def concat(*args):
    out = []
    for a in args:
        out.extend(a)
    return out


# ---------- Menu music: lighthearted loop with bass + melody + arpeggio ----------

def gen_menu_music():
    bpm = 110
    beat = 60.0 / bpm  # quarter note duration

    # Chord progression: I - V - vi - IV (C - G - Am - F)
    progression = [
        ([NOTES["C3"], NOTES["E3"], NOTES["G3"]], [NOTES["C5"], NOTES["E5"], NOTES["G5"]]),
        ([NOTES["G3"], NOTES["B3"], NOTES["D4"]], [NOTES["G4"], NOTES["B4"], NOTES["D5"]]),
        ([NOTES["A3"], NOTES["C4"], NOTES["E4"]], [NOTES["A4"], NOTES["C5"], NOTES["E5"]]),
        ([NOTES["F3"], NOTES["A3"], NOTES["C4"]], [NOTES["F4"], NOTES["A4"], NOTES["C5"]]),
    ]

    melody_lines = [
        ["C5", "E5", "G5", "E5", "C5", "G4", "E5", "G5"],
        ["D5", "G5", "B4", "G5", "D5", "G4", "B4", "D5"],
        ["E5", "C5", "A4", "C5", "E5", "A4", "C5", "E5"],
        ["C5", "F5", "A4", "F5", "C5", "A4", "F5", "C5"],
    ]

    full = []
    for loop in range(2):
        for chord_idx, ((bass_freqs, _), mel) in enumerate(zip(progression, melody_lines)):
            # 1 bar = 4 beats
            bar_dur = beat * 4
            # Chord pad (triangle, soft)
            pad = play_chord(bass_freqs, bar_dur, waveform="triangle", amp=0.14)
            # Bass: root note for 1 beat then silence + low octave
            bass_root = bass_freqs[0]
            bass_line = []
            for b in range(4):
                bass_line.extend(play_note(bass_root * 0.5, beat, "saw", amp=0.18, attack=0.005, release=0.05))
            # Melody: 8 eighth notes
            mel_line = []
            for note_name in mel:
                f = NOTES[note_name]
                mel_line.extend(play_note(f, beat / 2, "square", amp=0.12, attack=0.005, release=0.04))

            # Mix bar
            bar = [0.0] * int(bar_dur * SR)
            for arr in (pad, bass_line, mel_line):
                for i, v in enumerate(arr):
                    if i < len(bar):
                        bar[i] += v
            # Soft lowpass for warmth
            bar = lowpass(bar, 4500)
            full.extend(bar)

    # Final clip ~ 2 * 4 * 4*60/110 ~ 17.4 sec, looped by Unity
    # Normalize
    peak = max(abs(s) for s in full) or 1.0
    full = [s / peak * 0.8 for s in full]
    write_wav("menu_music", full)


# ---------- Battle music: tense loop ----------

def gen_battle_music():
    bpm = 130
    beat = 60.0 / bpm
    # Minor progression: i - VI - III - VII (Am - F - C - G)
    chords = [
        [NOTES["A3"], NOTES["C4"], NOTES["E4"]],
        [NOTES["F3"], NOTES["A3"], NOTES["C4"]],
        [NOTES["C3"], NOTES["E3"], NOTES["G3"]],
        [NOTES["G3"], NOTES["B3"], NOTES["D4"]],
    ]
    melody_pattern = ["A4", "C5", "E5", "C5", "A4", "G4", "E4", "G4"]

    full = []
    for loop in range(2):
        for chord_idx, chord in enumerate(chords):
            bar_dur = beat * 4
            pad = play_chord(chord, bar_dur, waveform="saw", amp=0.1)
            # Driving bass (8th notes on root)
            bass_line = []
            for b in range(8):
                bass_line.extend(play_note(chord[0] * 0.5, beat / 2, "square", amp=0.18, attack=0.005, release=0.03))
            # Melody (offset between chords)
            mel_line = []
            shift_idx = chord_idx
            for note_name in melody_pattern:
                f = NOTES[note_name]
                mel_line.extend(play_note(f, beat / 2, "triangle", amp=0.12, attack=0.005, release=0.04))
            # Drum-ish kick on beats 1 and 3
            drum = []
            for b in range(4):
                if b in (0, 2):
                    drum.extend(play_note(80, 0.08, "sine", amp=0.5, attack=0.001, release=0.05))
                    drum.extend(silence(beat - 0.08))
                else:
                    drum.extend(silence(beat))

            bar = [0.0] * int(bar_dur * SR)
            for arr in (pad, bass_line, mel_line, drum):
                for i, v in enumerate(arr):
                    if i < len(bar):
                        bar[i] += v
            bar = lowpass(bar, 5000)
            full.extend(bar)

    peak = max(abs(s) for s in full) or 1.0
    full = [s / peak * 0.85 for s in full]
    write_wav("battle_music", full)


# ---------- Button click ----------

def gen_card_play():
    # Two pitched clicks with reverb-ish tail
    s1 = play_note(880, 0.06, "triangle", amp=0.5, attack=0.001, release=0.04)
    s2 = play_note(1320, 0.08, "sine", amp=0.45, attack=0.001, release=0.06)
    out = concat(s1, s2)
    out = lowpass(out, 6000)
    write_wav("card_play", out)


def gen_match_start():
    # Whistle-like ascending tone
    n = int(0.5 * SR)
    out = [0.0] * n
    for i in range(n):
        t = i / SR
        # frequency rises 600 -> 1200
        f = 600 + 600 * (t / 0.5)
        out[i] = 0.5 * math.sin(2 * math.pi * f * t)
    e = env_adsr(n, a=0.02, d=0.05, s_level=0.7, r=0.1)
    out = [out[i] * e[i] for i in range(n)]
    write_wav("match_start", out)


def gen_victory():
    # I-IV-V-I fanfare
    notes = [(NOTES["C5"], 0.18), (NOTES["E5"], 0.18), (NOTES["G5"], 0.18), (NOTES["C5"] * 2, 0.4)]
    out = []
    for f, d in notes:
        s = play_note(f, d, "saw", amp=0.4, attack=0.005, release=0.08)
        out.extend(s)
    write_wav("victory", out)


def gen_defeat():
    notes = [(NOTES["E4"], 0.25), (NOTES["D4"], 0.25), (NOTES["C4"], 0.25), (NOTES["A3"], 0.5)]
    out = []
    for f, d in notes:
        s = play_note(f, d, "triangle", amp=0.4, attack=0.005, release=0.1)
        out.extend(s)
    write_wav("defeat", out)


def gen_attack_swing():
    # White-noise burst, lowpassed
    n = int(0.12 * SR)
    out = noise(0.12, amp=0.5, seed=42)
    e = env_adsr(n, a=0.001, d=0.04, s_level=0.4, r=0.05)
    out = [out[i] * e[i] for i in range(min(len(out), n))]
    out = lowpass(out, 1500)
    write_wav("attack_swing", out)


def gen_unit_death():
    # Descending pitch + noise
    n = int(0.3 * SR)
    out = [0.0] * n
    for i in range(n):
        t = i / SR
        f = 440 * (1 - t / 0.3)
        out[i] = 0.4 * math.sin(2 * math.pi * f * t)
    no = noise(0.3, amp=0.2, seed=11)
    out = [out[i] + no[i] for i in range(n)]
    e = env_adsr(n, a=0.005, d=0.05, s_level=0.5, r=0.15)
    out = [out[i] * e[i] for i in range(n)]
    write_wav("unit_death", out)


def gen_tower_shoot():
    # Punchy laser-ish blip
    n = int(0.18 * SR)
    out = [0.0] * n
    for i in range(n):
        t = i / SR
        f = 1200 - 800 * (t / 0.18)
        out[i] = 0.45 * math.sin(2 * math.pi * f * t)
    e = env_adsr(n, a=0.001, d=0.04, s_level=0.5, r=0.06)
    out = [out[i] * e[i] for i in range(n)]
    write_wav("tower_shoot", out)


def gen_tower_destroyed():
    # Big rumble with crash
    n = int(1.0 * SR)
    rumble = noise(1.0, amp=0.6, seed=99)
    rumble = lowpass(rumble, 200)
    boom = sine(80, 0.8, amp=0.6)
    out = []
    for i in range(n):
        a = rumble[i] if i < len(rumble) else 0
        b = boom[i] if i < len(boom) else 0
        out.append(a + b)
    e = env_adsr(n, a=0.01, d=0.1, s_level=0.7, r=0.5)
    out = [out[i] * e[i] for i in range(n)]
    write_wav("tower_destroyed", out)


def gen_fireball_boom():
    n = int(0.6 * SR)
    sweep = [0.0] * n
    for i in range(n):
        t = i / SR
        f = 80 + 60 * math.sin(t * 30)
        sweep[i] = 0.5 * math.sin(2 * math.pi * f * t)
    no = noise(0.6, amp=0.5, seed=7)
    no = lowpass(no, 400)
    out = [sweep[i] + no[i] * 0.7 for i in range(n)]
    e = env_adsr(n, a=0.005, d=0.08, s_level=0.7, r=0.4)
    out = [out[i] * e[i] for i in range(n)]
    write_wav("fireball_boom", out)


def gen_voice(name, freqs, dur=0.4):
    """Cartoon voice-line: jumpy frequency pattern, square wave."""
    n = int(dur * SR)
    out = [0.0] * n
    seg = n // len(freqs)
    for s, f in enumerate(freqs):
        for i in range(seg):
            t = i / SR
            idx = s * seg + i
            if idx < n:
                out[idx] = 0.4 * (1 if math.sin(2 * math.pi * f * t) > 0 else -1)
    e = env_adsr(n, a=0.005, d=0.05, s_level=0.7, r=0.05)
    out = [out[i] * e[i] for i in range(n)]
    write_wav(name, out)


def gen_voices():
    gen_voice("knight_spawn", [220, 280, 220, 180], 0.4)
    gen_voice("pig_oink", [320, 220, 380, 200], 0.4)
    gen_voice("skibidi", [440, 330, 440, 330, 600], 0.5)
    gen_voice("pocoyo_yay", [600, 800, 1000, 800], 0.4)
    gen_voice("sus", [200, 300, 200], 0.3)
    gen_voice("cheems_borks", [180, 220, 180, 220, 180], 0.5)
    gen_voice("shrek_swamp", [110, 90, 130, 90], 0.4)
    gen_voice("sigma", [280, 230, 280, 350], 0.4)
    gen_voice("nyan", [880, 988, 1108, 988, 880], 0.5)


if __name__ == "__main__":
    gen_menu_music()
    gen_battle_music()
    gen_card_play()
    gen_match_start()
    gen_victory()
    gen_defeat()
    gen_attack_swing()
    gen_unit_death()
    gen_tower_shoot()
    gen_tower_destroyed()
    gen_fireball_boom()
    gen_voices()
    print("All sounds generated.")
